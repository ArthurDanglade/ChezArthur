using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gacha;
using ChezArthur.Characters;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Entrée d'un personnage dans le popup de résultats.
    /// </summary>
    public class PullResultEntryUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Affichage")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject newBadge;
        [SerializeField] private GameObject rateUpBadge;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Configure l'entrée avec les données du personnage tiré.
        /// </summary>
        public void Setup(CharacterData data, PulledCharacter pulled)
        {
            if (data != null)
            {
                if (iconImage != null && data.Icon != null)
                    iconImage.sprite = data.Icon;

                if (nameText != null)
                    nameText.text = data.CharacterName;

                if (rarityBorder != null)
                    rarityBorder.color = GetRarityColor(data.Rarity);
            }
            else
            {
                if (nameText != null)
                    nameText.text = pulled.characterId;
            }

            // Statut
            if (statusText != null)
            {
                if (pulled.isNew)
                {
                    statusText.text = "NOUVEAU !";
                    statusText.color = Color.green;
                }
                else
                {
                    statusText.text = "Nv." + pulled.previousLevel.ToString() + " → Nv." + pulled.newLevel.ToString();
                    statusText.color = Color.yellow;
                }
            }

            // Badges
            if (newBadge != null)
                newBadge.SetActive(pulled.isNew);

            if (rateUpBadge != null)
                rateUpBadge.SetActive(pulled.isRateUp);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

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
