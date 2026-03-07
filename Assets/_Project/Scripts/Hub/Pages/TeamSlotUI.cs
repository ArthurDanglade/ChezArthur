using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Characters;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Slot d'équipe affichant un personnage ou un emplacement vide.
    /// </summary>
    public class TeamSlotUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("UI Éléments")]
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject filledState;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Button slotButton;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private string _characterId;
        private bool _isEmpty = true;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (slotButton != null)
            {
                slotButton.onClick.AddListener(OnSlotClicked);
            }
        }

        private void OnDestroy()
        {
            if (slotButton != null)
            {
                slotButton.onClick.RemoveListener(OnSlotClicked);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche un personnage dans ce slot.
        /// </summary>
        public void SetCharacter(CharacterData data, OwnedCharacter owned)
        {
            if (data == null || owned == null)
            {
                SetEmpty();
                return;
            }

            _characterId = owned.characterId;
            _isEmpty = false;

            if (emptyState != null) emptyState.SetActive(false);
            if (filledState != null) filledState.SetActive(true);

            if (iconImage != null && data.Icon != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = true;
            }

            if (levelText != null)
            {
                levelText.text = "Nv." + owned.level.ToString();
            }

            if (rarityBorder != null)
            {
                rarityBorder.color = GetRarityColor(data.Rarity);
            }
        }

        /// <summary>
        /// Affiche un slot vide.
        /// </summary>
        public void SetEmpty()
        {
            _characterId = null;
            _isEmpty = true;

            if (emptyState != null) emptyState.SetActive(true);
            if (filledState != null) filledState.SetActive(false);

            if (iconImage != null)
            {
                iconImage.enabled = false;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnSlotClicked()
        {
            if (_isEmpty || string.IsNullOrEmpty(_characterId)) return;

            // Retirer de l'équipe
            if (PersistentManager.Instance != null && PersistentManager.Instance.Characters != null)
            {
                PersistentManager.Instance.Characters.RemoveFromTeam(_characterId);
                PersistentManager.Instance.SaveGame();
            }
        }

        private Color GetRarityColor(CharacterRarity rarity)
        {
            return rarity switch
            {
                CharacterRarity.SR => new Color(0.6f, 0.8f, 1f),   // Bleu clair
                CharacterRarity.SSR => new Color(1f, 0.84f, 0f),   // Or
                CharacterRarity.LR => new Color(0.8f, 0.5f, 1f),   // Violet
                _ => Color.white
            };
        }
    }
}
