using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gacha;
using ChezArthur.Characters;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Popup affichant les personnages disponibles (rate up).
    /// </summary>
    public class RateUpPopupUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Composants")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform charactersContainer;
        [SerializeField] private RateUpCharacterEntryUI characterEntryPrefab;

        [Header("Sections")]
        [SerializeField] private TextMeshProUGUI ssrTitleText;
        [SerializeField] private TextMeshProUGUI srTitleText;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<RateUpCharacterEntryUI> _spawnedEntries = new List<RateUpCharacterEntryUI>();

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

            // Clear
            foreach (var entry in _spawnedEntries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }
            _spawnedEntries.Clear();

            // SSR Rate Up
            if (banner.RateUpSSR != null)
            {
                var entry = Instantiate(characterEntryPrefab, charactersContainer);
                entry.Setup(banner.RateUpSSR, true);
                _spawnedEntries.Add(entry);
            }

            // SR Pool
            foreach (var sr in banner.SRPool)
            {
                if (sr == null) continue;
                var entry = Instantiate(characterEntryPrefab, charactersContainer);
                entry.Setup(sr, false);
                _spawnedEntries.Add(entry);
            }

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
