using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.UI;

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
                rarityBorder.color = CharacterRarityPalette.GetColor(data.Rarity);

            if (rateUpBadge != null)
                rateUpBadge.SetActive(isRateUp);
        }
    }
}
