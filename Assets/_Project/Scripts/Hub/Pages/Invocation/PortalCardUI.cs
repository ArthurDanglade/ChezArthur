using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Gacha;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Carte portail Invocation (Gate 6.b) — artwork, timer, x1/x10, Taux.
    /// </summary>
    public class PortalCardUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Artwork")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private Image placeholderTint;

        [Header("Bandeau")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Actions")]
        [SerializeField] private Button pullSingleButton;
        [SerializeField] private TextMeshProUGUI pullSingleLabel;
        [SerializeField] private TextMeshProUGUI pullSingleCostText;
        [SerializeField] private Image pullSingleTalsIcon;
        [SerializeField] private CanvasGroup pullSingleVisual;

        [SerializeField] private Button pullMultiButton;
        [SerializeField] private TextMeshProUGUI pullMultiLabel;
        [SerializeField] private TextMeshProUGUI pullMultiCostText;
        [SerializeField] private Image pullMultiTalsIcon;
        [SerializeField] private CanvasGroup pullMultiVisual;

        [SerializeField] private Button ratesButton;
        [SerializeField] private Button charactersButton;
        [SerializeField] private TextMeshProUGUI charactersLabel;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private BannerData _banner;
        private Action<BannerData> _onPullSingle;
        private Action<BannerData> _onPullMulti;
        private Action<BannerData> _onShowRates;
        private Action<BannerData> _onShowCharacters;
        private Color _placeholderColor = new Color(0.32f, 0.36f, 0.48f, 0.95f);

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            RefreshSeasonTimer();
            RefreshAffordability();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        public void Bind(
            BannerData data,
            Action<BannerData> onPullSingle,
            Action<BannerData> onPullMulti,
            Action<BannerData> onShowRates,
            Action<BannerData> onShowCharacters,
            Color placeholderTintColor)
        {
            _banner = data;
            _onPullSingle = onPullSingle;
            _onPullMulti = onPullMulti;
            _onShowRates = onShowRates;
            _onShowCharacters = onShowCharacters;
            _placeholderColor = placeholderTintColor;

            WireButtons();
            RefreshAll();
        }

        public void RefreshAll()
        {
            RefreshArtwork();
            RefreshTitle();
            RefreshSeasonTimer();
            RefreshCosts();
            RefreshCharactersButton();
            RefreshAffordability();
        }

        public void RefreshAffordability()
        {
            if (_banner == null)
                return;

            int tals = PersistentManager.Instance != null
                ? PersistentManager.Instance.Tals
                : 0;

            // Jamais interactable=false silencieux : grise via tokens, cout reste lisible.
            ApplyAffordVisual(pullSingleVisual, pullSingleCostText, tals >= _banner.CostSingle);
            ApplyAffordVisual(pullMultiVisual, pullMultiCostText, tals >= _banner.CostMulti);
        }

        // ═══════════════════════════════════════════
        // PRIVÉ — refresh
        // ═══════════════════════════════════════════

        private void RefreshArtwork()
        {
            Sprite art = _banner != null ? _banner.Artwork : null;
            bool hasArt = art != null;

            if (artworkImage != null)
            {
                artworkImage.sprite = art;
                artworkImage.enabled = hasArt;
                artworkImage.color = Color.white;
                artworkImage.preserveAspect = false;
            }

            if (placeholderTint != null)
            {
                placeholderTint.enabled = !hasArt;
                placeholderTint.color = _placeholderColor;
            }
        }

        private void RefreshTitle()
        {
            if (titleText == null)
                return;
            titleText.text = _banner != null ? _banner.DisplayTitle : string.Empty;
        }

        private void RefreshSeasonTimer()
        {
            if (timerText == null)
                return;

            if (_banner == null || !_banner.HasDuration || _banner.DateFinSaisonTicks <= 0)
            {
                timerText.text = string.Empty;
                timerText.gameObject.SetActive(false);
                return;
            }

            timerText.gameObject.SetActive(true);
            TimeSpan left = _banner.GetTimeRemaining();
            if (left.TotalSeconds <= 0)
            {
                timerText.text = "Termine";
                return;
            }

            int days = Mathf.CeilToInt((float)left.TotalDays);
            if (days <= 1)
                timerText.text = "Dernier jour";
            else
                timerText.text = "J-" + days;
        }

        private void RefreshCosts()
        {
            if (_banner == null)
                return;

            if (pullSingleLabel != null)
                pullSingleLabel.text = "INVOQUER x1";
            if (pullMultiLabel != null)
                pullMultiLabel.text = "INVOQUER x10";

            if (pullSingleCostText != null)
                pullSingleCostText.text = _banner.CostSingle.ToString();
            if (pullMultiCostText != null)
                pullMultiCostText.text = _banner.CostMulti.ToString();
        }

        private void RefreshCharactersButton()
        {
            if (charactersLabel == null || _banner == null)
                return;

            int n = BannerRoster.TotalCount(_banner);
            charactersLabel.text = "Personnages (" + n + ")";
            if (charactersButton != null)
                charactersButton.interactable = n > 0 && _onShowCharacters != null;
        }

        private static void ApplyAffordVisual(
            CanvasGroup visual,
            TextMeshProUGUI costText,
            bool canAfford)
        {
            if (visual != null)
                visual.alpha = canAfford ? 1f : 0.45f;

            if (costText != null)
                costText.color = canAfford ? UiTheme.Gold : UiTheme.TextMuted;
        }

        // ═══════════════════════════════════════════
        // PRIVÉ — boutons
        // ═══════════════════════════════════════════

        private void WireButtons()
        {
            UnwireButtons();

            if (pullSingleButton != null)
                pullSingleButton.onClick.AddListener(OnPullSingleClicked);
            if (pullMultiButton != null)
                pullMultiButton.onClick.AddListener(OnPullMultiClicked);
            if (ratesButton != null)
                ratesButton.onClick.AddListener(OnRatesClicked);

            if (charactersButton != null)
            {
                charactersButton.onClick.RemoveListener(OnCharactersClicked);
                charactersButton.onClick.AddListener(OnCharactersClicked);
            }
        }

        private void UnwireButtons()
        {
            if (pullSingleButton != null)
                pullSingleButton.onClick.RemoveListener(OnPullSingleClicked);
            if (pullMultiButton != null)
                pullMultiButton.onClick.RemoveListener(OnPullMultiClicked);
            if (ratesButton != null)
                ratesButton.onClick.RemoveListener(OnRatesClicked);
            if (charactersButton != null)
                charactersButton.onClick.RemoveListener(OnCharactersClicked);
        }

        private void OnPullSingleClicked()
        {
            if (_banner == null)
                return;

            int tals = PersistentManager.Instance != null
                ? PersistentManager.Instance.Tals
                : 0;
            if (tals < _banner.CostSingle)
            {
                Debug.Log(
                    "[PortalCard] x1 refuse — Tals insuffisants (" + tals + "/" + _banner.CostSingle + ").",
                    this);
                RefreshAffordability();
                return;
            }

            _onPullSingle?.Invoke(_banner);
            RefreshAffordability();
        }

        private void OnPullMultiClicked()
        {
            if (_banner == null)
                return;

            int tals = PersistentManager.Instance != null
                ? PersistentManager.Instance.Tals
                : 0;
            if (tals < _banner.CostMulti)
            {
                Debug.Log(
                    "[PortalCard] x10 refuse — Tals insuffisants (" + tals + "/" + _banner.CostMulti + ").",
                    this);
                RefreshAffordability();
                return;
            }

            _onPullMulti?.Invoke(_banner);
            RefreshAffordability();
        }

        private void OnRatesClicked()
        {
            _onShowRates?.Invoke(_banner);
        }

        private void OnCharactersClicked()
        {
            _onShowCharacters?.Invoke(_banner);
        }
    }
}
