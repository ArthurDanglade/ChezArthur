using System.Globalization;
using TMPro;
using UnityEngine;
using ChezArthur.Core;

namespace ChezArthur.UI
{
    /// <summary>
    /// Header Hub définitif (Gate 2.1) : pseudo, meilleur étage, Tals.
    /// Remplace InfoBarUI. Se met à jour via PersistentManager.OnDataChanged.
    /// Option A : HubHeaderSafeBleed prolonge le bandeau dans l'encoche.
    /// </summary>
    public class HubHeaderUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private static readonly NumberFormatInfo TalsNumberFormat = CreateTalsNumberFormat();

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Textes")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI bestStageText;
        [SerializeField] private TextMeshProUGUI talsText;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private PersistentManager _boundManager;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            Subscribe();
            RefreshDisplay();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Met à jour l'affichage depuis PersistentManager (ou fallback si absent).
        /// </summary>
        public void RefreshDisplay()
        {
            Subscribe();

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

        private void Subscribe()
        {
            if (_boundManager != null)
                return;
            if (PersistentManager.Instance == null)
                return;

            _boundManager = PersistentManager.Instance;
            _boundManager.OnDataChanged += RefreshDisplay;
        }

        private void Unsubscribe()
        {
            if (_boundManager == null)
                return;

            _boundManager.OnDataChanged -= RefreshDisplay;
            _boundManager = null;
        }

        private void UpdateTexts(string name, int bestStage, int tals)
        {
            if (playerNameText != null)
                playerNameText.text = name;
            if (bestStageText != null)
                bestStageText.text = "Étage " + bestStage.ToString();
            if (talsText != null)
                talsText.text = tals.ToString("N0", TalsNumberFormat);
        }

        private static NumberFormatInfo CreateTalsNumberFormat()
        {
            var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            nfi.NumberGroupSeparator = " ";
            return nfi;
        }
    }
}
