using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gacha;
using ChezArthur.Characters;
using ChezArthur.Core;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Popup affichant les résultats d'un tirage.
    /// </summary>
    public class PullResultPopupUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Composants")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform resultsContainer;
        [SerializeField] private PullResultEntryUI resultEntryPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Références")]
        [SerializeField] private CharacterDatabase characterDatabase;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<PullResultEntryUI> _spawnedEntries = new List<PullResultEntryUI>();

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

        /// <summary>
        /// Affiche les résultats du tirage.
        /// </summary>
        public void Show(GachaPullResult result)
        {
            if (result == null) return;

            // Clear
            foreach (var entry in _spawnedEntries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }
            _spawnedEntries.Clear();

            // Titre
            if (titleText != null)
            {
                int count = result.characters.Count;
                titleText.text = count == 1 ? "Invocation x1" : "Invocation x" + count.ToString();
            }

            // Créer les entrées
            foreach (var pulled in result.characters)
            {
                CharacterData data = characterDatabase != null
                    ? characterDatabase.GetById(pulled.characterId)
                    : null;

                PullResultEntryUI entry = Instantiate(resultEntryPrefab, resultsContainer);
                entry.Setup(data, pulled);
                _spawnedEntries.Add(entry);
            }

            // Afficher
            ShowPopup();
        }

        /// <summary>
        /// Cache le popup.
        /// </summary>
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
            else
            {
                gameObject.SetActive(true);
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
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void HideImmediate()
        {
            HidePopup();
        }
    }
}
