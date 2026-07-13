using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.Core;

namespace ChezArthur.UI
{
    /// <summary>
    /// Entrée de classement fin de run : portrait, rang, réplique et stats agrégées.
    /// </summary>
    public class EndRunCharacterEntryUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Affichage")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI quoteText;
        [SerializeField] private TextMeshProUGUI damageDealtText;
        [SerializeField] private TextMeshProUGUI damageTakenText;
        [SerializeField] private TextMeshProUGUI healingText;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Configure l'entrée de classement. rank commence à 1 ;
        /// teamSize = taille de l'équipe de la run (1 à 4).
        /// </summary>
        public void Setup(CharacterData data, int rank, int teamSize, CharacterRunStats stats)
        {
            if (data == null)
                return;

            if (iconImage != null && data.Icon != null)
                iconImage.sprite = data.Icon;

            if (rarityBorder != null)
                rarityBorder.color = CharacterRarityPalette.GetColor(data.Rarity);

            if (rankText != null)
            {
                rankText.text = rank == 1 ? "1er" : $"{rank}ème";
                rankText.color = rank == 1 ? UiTheme.Gold : UiTheme.TextPrimary;
            }

            string quote = ResolveQuote(data, rank, teamSize);
            if (quoteText != null)
            {
                if (string.IsNullOrEmpty(quote))
                {
                    quoteText.gameObject.SetActive(false);
                }
                else
                {
                    quoteText.gameObject.SetActive(true);
                    quoteText.text = $"« {quote} »";
                    quoteText.color = UiTheme.TextSecondary;
                }
            }

            if (damageDealtText != null)
            {
                damageDealtText.color = UiTheme.TextMuted;
                damageDealtText.text = $"Dégâts infligés : {UiNumberFormat.Thousands(stats.DamageDealt)}";
            }

            if (damageTakenText != null)
            {
                damageTakenText.color = UiTheme.TextMuted;
                damageTakenText.text = $"Dégâts encaissés : {UiNumberFormat.Thousands(stats.DamageTaken)}";
            }

            if (healingText != null)
            {
                healingText.color = UiTheme.TextMuted;
                healingText.text = $"Soins appliqués : {UiNumberFormat.Thousands(stats.HealingDone)}";
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private static string ResolveQuote(CharacterData data, int rank, int teamSize)
        {
            if (rank == 1)
                return data.EndRunQuoteRank1;

            if (rank == teamSize)
                return data.EndRunQuoteLast;

            return data.EndRunQuoteMid;
        }
    }
}
