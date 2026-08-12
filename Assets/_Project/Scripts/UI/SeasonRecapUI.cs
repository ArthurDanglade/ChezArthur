using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Core;
using ChezArthur.Localization;
using ChezArthur.Meta;

namespace ChezArthur.UI
{
    /// <summary>
    /// Écran récap de fin de saison : gate bloquant (crédit à l'affichage) ou consultation.
    /// </summary>
    public class SeasonRecapUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Racine")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Canvas rootCanvas;

        [Header("Contenu")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private TextMeshProUGUI rewardsText;

        [Header("Actions")]
        [SerializeField] private Button primaryButton;
        [SerializeField] private TextMeshProUGUI primaryButtonLabel;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _gateMode;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (primaryButton != null)
                primaryButton.onClick.AddListener(OnPrimaryClicked);
        }

        private void OnDisable()
        {
            if (primaryButton != null)
                primaryButton.onClick.RemoveListener(OnPrimaryClicked);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Gate Hub : crédite à l'affichage (v2 §9.2), bouton Continuer → MarkRecapShown.
        /// </summary>
        public void OpenAsGate()
        {
            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
                return;

            SeasonRecapData recap = pm.PendingSeasonRecap;
            if (recap == null || !recap.pending || recap.rewardsCredited)
                return;

            _gateMode = true;
            SeasonRewards.CreditPendingRecap();
            Show(recap, isGate: true);
            Debug.Log("[Season] Récap gate ouvert — récompenses créditées à l'affichage.");
        }

        /// <summary>
        /// Consultation depuis la page saison — aucun crédit.
        /// </summary>
        public void OpenForConsultation()
        {
            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
                return;

            SeasonRecapData recap = pm.PendingSeasonRecap;
            if (recap == null || string.IsNullOrEmpty(recap.seasonId))
                return;

            _gateMode = false;
            Show(recap, isGate: false);
        }

        /// <summary>
        /// Ferme l'overlay.
        /// </summary>
        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Show(SeasonRecapData recap, bool isGate)
        {
            BringToFront();

            if (titleText != null)
            {
                titleText.text = Loc.Format(
                    "ui.saison.recap_titre",
                    "Bilan {0}",
                    recap.seasonId);
            }

            if (bodyText != null)
            {
                bodyText.text = Loc.Format(
                    "ui.saison.recap_corps",
                    "Score final : {0}\nMeilleur étage : {1} ×{2}\nRuns : {3}\nDernier palier : {4}",
                    recap.finalScore,
                    recap.bestStage,
                    recap.bestTier,
                    recap.runs,
                    recap.lastTierReached);
            }

            if (rewardsText != null)
            {
                var sb = new StringBuilder(128);
                sb.AppendLine(Loc.Tr("ui.saison.recap_rewards_titre", "Récompenses :"));
                sb.AppendLine(Loc.Format(
                    "ui.saison.recap_reward_tals",
                    "• {0} Tals",
                    recap.pendingTals));
                if (recap.pendingLrLevels > 0)
                {
                    sb.AppendLine(Loc.Format(
                        "ui.saison.recap_reward_lr",
                        "• LR ×{0} ({1})",
                        recap.pendingLrLevels,
                        string.IsNullOrEmpty(recap.lrCharacterId) ? "—" : recap.lrCharacterId));
                }

                rewardsText.text = sb.ToString();
            }

            if (primaryButtonLabel != null)
            {
                primaryButtonLabel.text = isGate
                    ? Loc.Tr("ui.saison.recap_continuer", "Continuer")
                    : Loc.Tr("ui.saison.recap_fermer", "Fermer");
            }

            if (panelRoot != null)
                panelRoot.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        private void OnPrimaryClicked()
        {
            if (_gateMode)
            {
                PersistentManager.Instance?.MarkRecapShown();
                Debug.Log("[Season] Récap gate fermé (MarkRecapShown).");
            }

            Close();
        }

        private void BringToFront()
        {
            if (rootCanvas != null)
            {
                rootCanvas.overrideSorting = true;
                rootCanvas.sortingOrder = 500;
            }

            transform.SetAsLastSibling();
        }
    }
}
