using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Localization;
using ChezArthur.Meta;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Une ligne de la piste de saison (palier 1–12).
    /// </summary>
    public class SeasonTierEntryUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private int tierIndex;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button claimButton;
        [SerializeField] private CanvasGroup canvasGroup;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            if (claimButton != null)
                claimButton.onClick.AddListener(OnClaimClicked);
        }

        private void OnDisable()
        {
            if (claimButton != null)
                claimButton.onClick.RemoveListener(OnClaimClicked);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Pose l'index de palier (0-based) une fois à la construction.
        /// </summary>
        public void BindIndex(int index)
        {
            tierIndex = index;
        }

        /// <summary>
        /// Rafraîchit labels + état claim selon SeasonRewards.
        /// </summary>
        public void Refresh()
        {
            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            SeasonTier tier = config != null ? config.GetTier(tierIndex) : null;
            SeasonTierState state = SeasonRewards.GetTierState(tierIndex);

            if (titleText != null)
            {
                int scoreReq = tier != null ? tier.scoreRequired : 0;
                titleText.text = Loc.Format(
                    "ui.saison.palier_titre",
                    "Palier {0} — {1} pts",
                    tierIndex + 1,
                    scoreReq);
            }

            if (rewardText != null)
            {
                if (tier == null)
                {
                    rewardText.text = "";
                }
                else if (tier.grantsLrLevel)
                {
                    rewardText.text = Loc.Format(
                        "ui.saison.palier_reward_lr",
                        "{0} Tals + LR",
                        tier.talsReward);
                }
                else
                {
                    rewardText.text = Loc.Format(
                        "ui.saison.palier_reward_tals",
                        "{0} Tals",
                        tier.talsReward);
                }
            }

            bool claimable = state == SeasonTierState.Claimable;
            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(claimable);
                claimButton.interactable = claimable;
            }

            if (statusText != null)
            {
                switch (state)
                {
                    case SeasonTierState.Claimed:
                        statusText.text = Loc.Tr("ui.saison.etat_reclame", "Réclamé");
                        break;
                    case SeasonTierState.Claimable:
                        statusText.text = "";
                        break;
                    default:
                        statusText.text = Loc.Tr("ui.saison.etat_verrouille", "Verrouillé");
                        break;
                }
            }

            if (canvasGroup != null)
                canvasGroup.alpha = state == SeasonTierState.Locked ? 0.55f : 1f;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnClaimClicked()
        {
            SeasonRewards.TryClaim(tierIndex);
            Refresh();
        }
    }
}
