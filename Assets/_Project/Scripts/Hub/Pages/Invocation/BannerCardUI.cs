using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gacha;
using ChezArthur.Core;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Carte d'une bannière dans la page invocation.
    /// </summary>
    public class BannerCardUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Affichage")]
        [SerializeField] private Image bannerImage;
        [SerializeField] private TextMeshProUGUI bannerNameText;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Boutons")]
        [SerializeField] private Button pullSingleButton;
        [SerializeField] private TextMeshProUGUI pullSingleCostText;
        [SerializeField] private Button pullMultiButton;
        [SerializeField] private TextMeshProUGUI pullMultiCostText;
        [SerializeField] private Button ratesButton;
        [SerializeField] private Button rateUpButton;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private BannerData _bannerData;
        private Action<BannerData> _onPullSingle;
        private Action<BannerData> _onPullMulti;
        private Action<BannerData> _onShowRates;
        private Action<BannerData> _onShowRateUp;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnDestroy()
        {
            if (pullSingleButton != null)
                pullSingleButton.onClick.RemoveAllListeners();
            if (pullMultiButton != null)
                pullMultiButton.onClick.RemoveAllListeners();
            if (ratesButton != null)
                ratesButton.onClick.RemoveAllListeners();
            if (rateUpButton != null)
                rateUpButton.onClick.RemoveAllListeners();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Configure la carte de bannière.
        /// </summary>
        public void Setup(
            BannerData data,
            Action<BannerData> onPullSingle,
            Action<BannerData> onPullMulti,
            Action<BannerData> onShowRates,
            Action<BannerData> onShowRateUp)
        {
            _bannerData = data;
            _onPullSingle = onPullSingle;
            _onPullMulti = onPullMulti;
            _onShowRates = onShowRates;
            _onShowRateUp = onShowRateUp;

            // Affichage
            if (bannerNameText != null)
                bannerNameText.text = data.BannerName;

            if (bannerImage != null && data.BannerImage != null)
                bannerImage.sprite = data.BannerImage;

            // Coûts
            if (pullSingleCostText != null)
                pullSingleCostText.text = "x1\n" + data.CostSingle.ToString() + " Tals";

            if (pullMultiCostText != null)
                pullMultiCostText.text = "x10\n" + data.CostMulti.ToString() + " Tals";

            // Timer
            UpdateTimer();

            // Boutons
            if (pullSingleButton != null)
            {
                pullSingleButton.onClick.RemoveAllListeners();
                pullSingleButton.onClick.AddListener(OnPullSingleClicked);
            }

            if (pullMultiButton != null)
            {
                pullMultiButton.onClick.RemoveAllListeners();
                pullMultiButton.onClick.AddListener(OnPullMultiClicked);
            }

            if (ratesButton != null)
            {
                ratesButton.onClick.RemoveAllListeners();
                ratesButton.onClick.AddListener(OnRatesClicked);
            }

            if (rateUpButton != null)
            {
                rateUpButton.onClick.RemoveAllListeners();
                rateUpButton.onClick.AddListener(OnRateUpClicked);
            }

            UpdateButtonStates();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void UpdateTimer()
        {
            if (timerText == null || _bannerData == null) return;

            if (!_bannerData.HasDuration)
            {
                timerText.text = "Permanent";
                return;
            }

            var remaining = _bannerData.GetTimeRemaining();
            if (remaining.TotalSeconds <= 0)
            {
                timerText.text = "Terminée";
            }
            else if (remaining.TotalDays >= 1)
            {
                timerText.text = (int)remaining.TotalDays + "j restants";
            }
            else
            {
                timerText.text = remaining.Hours + "h " + remaining.Minutes + "m restants";
            }
        }

        private void UpdateButtonStates()
        {
            if (_bannerData == null) return;

            int tals = PersistentManager.Instance != null ? PersistentManager.Instance.Tals : 0;

            if (pullSingleButton != null)
                pullSingleButton.interactable = tals >= _bannerData.CostSingle;

            if (pullMultiButton != null)
                pullMultiButton.interactable = tals >= _bannerData.CostMulti;
        }

        private void OnPullSingleClicked()
        {
            _onPullSingle?.Invoke(_bannerData);
            UpdateButtonStates();
        }

        private void OnPullMultiClicked()
        {
            _onPullMulti?.Invoke(_bannerData);
            UpdateButtonStates();
        }

        private void OnRatesClicked()
        {
            _onShowRates?.Invoke(_bannerData);
        }

        private void OnRateUpClicked()
        {
            _onShowRateUp?.Invoke(_bannerData);
        }
    }
}
