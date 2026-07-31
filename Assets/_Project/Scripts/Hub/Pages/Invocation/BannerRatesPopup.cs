using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gacha;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Popup Taux d'apparition (Gate 6.b) — affichage pur, pas de tirage.
    /// </summary>
    public class BannerRatesPopup : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Racine")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image scrim;
        [SerializeField] private Button backButton;

        [Header("Contenu")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI srLabel;
        [SerializeField] private TextMeshProUGUI srRate;
        [SerializeField] private TextMeshProUGUI ssrLabel;
        [SerializeField] private TextMeshProUGUI ssrRate;
        [SerializeField] private TextMeshProUGUI lrLabel;
        [SerializeField] private TextMeshProUGUI lrRate;
        [SerializeField] private GameObject featuredRow;
        [SerializeField] private TextMeshProUGUI featuredLabel;
        [SerializeField] private TextMeshProUGUI featuredRate;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            WireBackButton();
            // Scrim bloque les clics sous le popup, sans fermer.
            if (scrim != null)
            {
                scrim.raycastTarget = true;
                Button scrimBtn = scrim.GetComponent<Button>();
                if (scrimBtn != null)
                    Destroy(scrimBtn);
            }

            HideImmediate();
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(Close);
        }

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        public void Show(BannerData banner)
        {
            if (banner == null)
                return;

            if (titleText != null)
                titleText.text = "Taux d'apparition";

            BindRarityRow(srLabel, srRate, "SR", banner.RateSR, CharacterRarityPalette.SR);
            BindRarityRow(ssrLabel, ssrRate, "SSR", banner.RateSSR, CharacterRarityPalette.SSR);
            BindRarityRow(lrLabel, lrRate, "LR", banner.RateLR, CharacterRarityPalette.LR);

            bool showFeatured = banner.FeaturedRatePercent > 0.0001f;
            if (featuredRow != null)
                featuredRow.SetActive(showFeatured);
            if (showFeatured)
            {
                if (featuredLabel != null)
                {
                    featuredLabel.text = "Vedette";
                    featuredLabel.color = UiTheme.Gold;
                }

                if (featuredRate != null)
                {
                    featuredRate.text = FormatPercent(banner.FeaturedRatePercent);
                    featuredRate.color = UiTheme.TextPrimary;
                }
            }

            WireBackButton();
            SetVisible(true);
        }

        public void Close()
        {
            SetVisible(false);
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private static void BindRarityRow(
            TextMeshProUGUI label,
            TextMeshProUGUI rate,
            string name,
            float percent,
            Color rarityColor)
        {
            if (label != null)
            {
                label.text = name;
                label.color = rarityColor;
            }

            if (rate != null)
            {
                rate.text = FormatPercent(percent);
                rate.color = UiTheme.TextPrimary;
            }
        }

        private static string FormatPercent(float percent)
        {
            // ASCII : "9%" ou "7.1%"
            if (Mathf.Approximately(percent, Mathf.Round(percent)))
                return Mathf.RoundToInt(percent).ToString() + "%";
            return percent.ToString("0.##") + "%";
        }

        private void WireBackButton()
        {
            if (backButton == null)
            {
                Transform header = transform.Find("Panel/Header");
                if (header == null)
                    header = transform.Find("Header");
                Transform backTx = header != null ? header.Find("BackButton") : null;
                if (backTx != null)
                    backButton = backTx.GetComponent<Button>();
            }

            if (backButton == null)
                return;

            backButton.onClick.RemoveListener(Close);
            backButton.onClick.AddListener(Close);
            backButton.transition = Selectable.Transition.None;
            backButton.interactable = true;

            PanelSurface surface = backButton.GetComponent<PanelSurface>();
            if (surface != null)
                surface.BlocksRaycasts = true;

            if (backButton.targetGraphic != null)
                backButton.targetGraphic.raycastTarget = true;

            Image[] images = backButton.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                    images[i].raycastTarget = true;
            }

            Transform icon = backButton.transform.Find("Icon");
            if (icon != null)
            {
                Image iconImg = icon.GetComponent<Image>();
                if (iconImg != null)
                    iconImg.raycastTarget = false;
            }
        }

        private void HideImmediate()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
