using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gacha;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Popup affichant les taux de drop.
    /// </summary>
    public class RatesPopupUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Composants")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI srRateText;
        [SerializeField] private TextMeshProUGUI ssrRateText;
        [SerializeField] private TextMeshProUGUI lrRateText;
        [SerializeField] private TextMeshProUGUI pityText;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            HideImmediate();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void Show(BannerData banner)
        {
            if (banner == null) return;

            if (srRateText != null)
                srRateText.text = "SR : " + banner.RateSR.ToString() + "%";

            if (ssrRateText != null)
                ssrRateText.text = "SSR : " + banner.RateSSR.ToString() + "%";

            if (lrRateText != null)
                lrRateText.text = "LR : " + banner.RateLR.ToString() + "%";

            if (pityText != null)
                pityText.text = "SSR garanti après " + banner.PityThreshold.ToString() + " multi x10";

            ShowPopup();
        }

        public void Hide()
        {
            HidePopup();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ShowPopup()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void HidePopup()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void HideImmediate()
        {
            HidePopup();
        }
    }
}
