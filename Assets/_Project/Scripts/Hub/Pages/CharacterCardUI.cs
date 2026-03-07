using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Carte de personnage affichée dans la collection.
    /// </summary>
    public class CharacterCardUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("UI Éléments")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private GameObject inTeamIndicator;
        [SerializeField] private Button cardButton;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private string _characterId;
        private CharacterData _currentData;
        private OwnedCharacter _currentOwned;
        private Action<CharacterData, OwnedCharacter> _onClickCallback;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string CharacterId => _characterId;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Debug.Log($"[CharacterCardUI] Awake appelé, cardButton null? {cardButton == null}");
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(OnCardClicked);
                Debug.Log("[CharacterCardUI] Listener ajouté");
            }
        }

        private void OnDestroy()
        {
            if (cardButton != null)
            {
                cardButton.onClick.RemoveListener(OnCardClicked);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Configure la carte avec les données du personnage.
        /// </summary>
        public void Setup(CharacterData data, OwnedCharacter owned, Action<CharacterData, OwnedCharacter> onClickCallback)
        {
            Debug.Log($"[CharacterCardUI] Setup appelé pour {data?.CharacterName ?? "null"}, callback null? {onClickCallback == null}");
            if (data == null || owned == null) return;

            _characterId = owned.characterId;
            _currentData = data;
            _currentOwned = owned;
            _onClickCallback = onClickCallback;

            if (iconImage != null && data.Icon != null)
            {
                iconImage.sprite = data.Icon;
            }

            if (nameText != null)
            {
                nameText.text = data.CharacterName;
            }

            if (levelText != null)
            {
                levelText.text = "Nv." + owned.level.ToString();
            }

            if (rarityBorder != null)
            {
                rarityBorder.color = GetRarityColor(data.Rarity);
            }

            Debug.Log($"[CharacterCardUI] Setup terminé, cardButton null? {cardButton == null}, cardButton interactable? {cardButton?.interactable}");
        }

        /// <summary>
        /// Affiche/masque l'indicateur "dans l'équipe".
        /// </summary>
        public void SetInTeam(bool inTeam)
        {
            if (inTeamIndicator != null)
            {
                inTeamIndicator.SetActive(inTeam);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnCardClicked()
        {
            Debug.Log($"[CharacterCardUI] OnCardClicked appelé pour {_characterId}, callback null? {_onClickCallback == null}");
            _onClickCallback?.Invoke(_currentData, _currentOwned);
        }

        private Color GetRarityColor(CharacterRarity rarity)
        {
            return rarity switch
            {
                CharacterRarity.SR => new Color(0.6f, 0.8f, 1f),   // Bleu clair
                CharacterRarity.SSR => new Color(1f, 0.84f, 0f),  // Or
                CharacterRarity.LR => new Color(0.8f, 0.5f, 1f),  // Violet
                _ => Color.white
            };
        }
    }
}
