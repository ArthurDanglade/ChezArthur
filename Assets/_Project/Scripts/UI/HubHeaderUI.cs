using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Core;
using ChezArthur.Hub.Pages;

namespace ChezArthur.UI
{
    /// <summary>
    /// Header Hub (Gate 2.1 + MT2-D8) : pseudo, bouton Saison (score), Tals.
    /// Le record d'étage n'est plus écrit ici (GO désactivé par le builder).
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

        [Header("Saison (MT2-G4)")]
        [SerializeField] private Button seasonButton;
        [SerializeField] private TextMeshProUGUI seasonButtonScoreText;
        [SerializeField] private SeasonPageUI seasonPage;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private PersistentManager _boundManager;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            if (seasonButton != null)
                seasonButton.onClick.AddListener(OnSeasonClicked);

            Subscribe();
            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (seasonButton != null)
                seasonButton.onClick.RemoveListener(OnSeasonClicked);

            Unsubscribe();
        }

        private void OnDestroy()
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
                    PersistentManager.Instance.Tals,
                    PersistentManager.Instance.BestScoreThisSeason);
            }
            else
            {
                UpdateTexts("Voyageur", 0, 0, 0);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnSeasonClicked()
        {
            if (seasonPage != null)
                seasonPage.Open();
            else
                Debug.LogWarning("[HubHeader] SeasonPageUI non assignée.");
        }

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

        private void UpdateTexts(string name, int bestStage, int tals, int seasonScore)
        {
            if (playerNameText != null)
                playerNameText.text = name;

            // Record : plus jamais écrit ici (D8) — bestStageText conservé pour la ref scène.
            _ = bestStage;

            if (talsText != null)
                talsText.text = tals.ToString("N0", TalsNumberFormat);

            if (seasonButtonScoreText != null)
                seasonButtonScoreText.text = seasonScore.ToString("N0", TalsNumberFormat);
        }

        private static NumberFormatInfo CreateTalsNumberFormat()
        {
            var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            nfi.NumberGroupSeparator = " ";
            return nfi;
        }
    }
}
