using UnityEngine;
using TMPro;
using ChezArthur.Core;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Affiche la barre d'info du Hub (pseudo, meilleur étage, Tals). Se met à jour via PersistentManager.
    /// </summary>
    public class InfoBarUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Textes")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI bestStageText;
        [SerializeField] private TextMeshProUGUI talsText;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            if (PersistentManager.Instance != null)
            {
                PersistentManager.Instance.OnDataChanged += RefreshDisplay;
            }
            RefreshDisplay();
        }

        private void OnDestroy()
        {
            if (PersistentManager.Instance != null)
            {
                PersistentManager.Instance.OnDataChanged -= RefreshDisplay;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Met à jour l'affichage avec les données de PersistentManager (ou valeurs par défaut si absent).
        /// </summary>
        public void RefreshDisplay()
        {
            if (PersistentManager.Instance != null)
            {
                UpdateTexts(
                    PersistentManager.Instance.PlayerName,
                    PersistentManager.Instance.BestStage,
                    PersistentManager.Instance.Tals);
            }
            else
            {
                UpdateTexts("Voyageur", 0, 0);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Assigne les valeurs aux textes (avec vérification null).
        /// </summary>
        private void UpdateTexts(string name, int bestStage, int tals)
        {
            if (playerNameText != null)
                playerNameText.text = name;
            if (bestStageText != null)
                bestStageText.text = "Étage " + bestStage.ToString();
            if (talsText != null)
                talsText.text = tals.ToString();
        }
    }
}
