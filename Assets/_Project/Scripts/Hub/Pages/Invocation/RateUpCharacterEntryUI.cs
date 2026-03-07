using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Entrée d'un personnage dans le popup rate up.
    /// </summary>
    public class RateUpCharacterEntryUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Affichage")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private GameObject rateUpBadge;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void Setup(CharacterData data, bool isRateUp)
        {
            if (data == null) return;

            if (iconImage != null && data.Icon != null)
                iconImage.sprite = data.Icon;

            if (nameText != null)
                nameText.text = data.CharacterName;

            if (rarityText != null)
                rarityText.text = data.Rarity.ToString();

            if (rarityBorder != null)
                rarityBorder.color = GetRarityColor(data.Rarity);

            if (rateUpBadge != null)
                rateUpBadge.SetActive(isRateUp);
        }

        private Color GetRarityColor(CharacterRarity rarity)
        {
            return rarity switch
            {
                CharacterRarity.SR => new Color(0.6f, 0.8f, 1f),
                CharacterRarity.SSR => new Color(1f, 0.84f, 0f),
                CharacterRarity.LR => new Color(0.8f, 0.5f, 1f),
                _ => Color.white
            };
        }
    }
}
