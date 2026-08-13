using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Backend;
using ChezArthur.Core;
using ChezArthur.Localization;
using ChezArthur.Meta;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Overlay page saison (hors nav bas). Ordre de lecture contrat §10.2.
    /// </summary>
    public class SeasonPageUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Racine")]
        [SerializeField] private GameObject panelRoot;

        [Header("Bloc 1 — Où j'en suis")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI statsText;

        [Header("Bloc 2 — Ce qui manque")]
        [SerializeField] private TextMeshProUGUI missingText;

        [Header("Bloc 3 — Temps")]
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Bloc 4 — Piste")]
        [SerializeField] private ScrollRect trackScroll;
        [SerializeField] private SeasonTierEntryUI[] tierEntries = new SeasonTierEntryUI[12];
        [SerializeField] private TextMeshProUGUI prestigeLabel;
        [SerializeField] private Button prestigeClaimButton;

        [Header("Actions")]
        [SerializeField] private Button reviewRecapButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private SeasonRecapUI seasonRecap;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private PersistentManager _boundManager;
        private Coroutine _countdownRoutine;
        private bool _isOpen;

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
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (reviewRecapButton != null)
                reviewRecapButton.onClick.AddListener(OnReviewRecapClicked);
            if (prestigeClaimButton != null)
                prestigeClaimButton.onClick.AddListener(OnPrestigeClaimClicked);
        }

        private void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (reviewRecapButton != null)
                reviewRecapButton.onClick.RemoveListener(OnReviewRecapClicked);
            if (prestigeClaimButton != null)
                prestigeClaimButton.onClick.RemoveListener(OnPrestigeClaimClicked);

            StopCountdown();
            Unsubscribe();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Ouvre l'overlay et rafraîchit tout le contenu.
        /// </summary>
        public void Open()
        {
            _isOpen = true;
            if (panelRoot != null)
                panelRoot.SetActive(true);
            else
                gameObject.SetActive(true);

            Subscribe();
            RefreshAll();
            CenterTrackOnCurrentTier();
            StartCountdown();
        }

        /// <summary>
        /// Ferme l'overlay.
        /// </summary>
        public void Close()
        {
            _isOpen = false;
            StopCountdown();
            Unsubscribe();

            if (panelRoot != null)
                panelRoot.SetActive(false);
            else
                gameObject.SetActive(false);
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
            _boundManager.OnDataChanged += OnDataChanged;
            RemoteTuning.OnTuningApplied += OnTuningApplied;
        }

        private void Unsubscribe()
        {
            RemoteTuning.OnTuningApplied -= OnTuningApplied;
            if (_boundManager == null)
                return;

            _boundManager.OnDataChanged -= OnDataChanged;
            _boundManager = null;
        }

        private void OnDataChanged()
        {
            if (_isOpen)
                RefreshAll();
        }

        private void OnTuningApplied()
        {
            if (_isOpen)
                RefreshAll();
        }

        private void RefreshAll()
        {
            PersistentManager pm = PersistentManager.Instance;
            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            if (pm == null || config == null)
                return;

            int score = pm.BestScoreThisSeason;
            int eligible = SeasonRewards.GetHighestEligibleTierNumber();

            if (scoreText != null)
            {
                scoreText.text = Loc.Format(
                    "ui.saison.score",
                    "Score : {0}",
                    score);
            }

            if (progressText != null)
            {
                progressText.text = Loc.Format(
                    "ui.saison.dernier_palier",
                    "Dernier palier franchi : {0}/12",
                    eligible);
            }

            if (statsText != null)
            {
                statsText.text = Loc.Format(
                    "ui.saison.stats",
                    "Meilleur étage : {0} ×{1} · Runs : {2}",
                    pm.BestStageThisSeason,
                    pm.BestTierThisSeason,
                    pm.RunsThisSeason);
            }

            RefreshMissingBlock(pm, config, score, eligible);
            RefreshCountdownLabel();
            RefreshTrack();
            RefreshPrestige(pm);
            RefreshReviewButton(pm);
        }

        private void RefreshMissingBlock(
            PersistentManager pm,
            SeasonRewardsConfig config,
            int score,
            int eligible)
        {
            if (missingText == null)
                return;

            if (eligible < config.TierCount)
            {
                SeasonTier next = config.GetTier(eligible);
                if (next == null)
                {
                    missingText.text = "";
                    return;
                }

                int need = Mathf.Max(0, next.scoreRequired - score);
                string reward = next.grantsLrLevel
                    ? Loc.Format("ui.saison.palier_reward_lr", "{0} Tals + LR", next.talsReward)
                    : Loc.Format("ui.saison.palier_reward_tals", "{0} Tals", next.talsReward);

                missingText.text = Loc.Format(
                    "ui.saison.manque",
                    "Encore {0} points → palier {1} ({2})",
                    need,
                    eligible + 1,
                    reward);
            }
            else
            {
                int step = config.PrestigeStep;
                int lastReq = config.GetTier(config.TierCount - 1)?.scoreRequired ?? 500;
                int nextPrestigeAt = lastReq + (pm.PrestigeTiersClaimed + 1) * step;
                int need = Mathf.Max(0, nextPrestigeAt - score);
                missingText.text = Loc.Format(
                    "ui.saison.manque_prestige",
                    "Encore {0} points → prochain prestige (+{1} Tals)",
                    need,
                    config.PrestigeTalsReward);
            }
        }

        private void RefreshCountdownLabel()
        {
            if (countdownText == null)
                return;

            TimeSpan remaining = SeasonRotationManager.GetTimeUntilSeasonEnd();
            string formatted;
            if (remaining.TotalHours >= 24.0)
            {
                int days = (int)remaining.TotalDays;
                int hours = remaining.Hours;
                formatted = Loc.Format(
                    "ui.saison.temps_jh",
                    "{0} j {1} h",
                    days,
                    hours);
            }
            else
            {
                int hours = (int)remaining.TotalHours;
                int minutes = remaining.Minutes;
                formatted = Loc.Format(
                    "ui.saison.temps_hm",
                    "{0} h {1} min",
                    hours,
                    minutes);
            }

            countdownText.text = Loc.Format(
                "ui.saison.temps_restant",
                "Fin de saison dans {0}",
                formatted);

            // Indicateur discret hors ligne (MT2-G6) — temps de confiance absent.
            if (!GameClock.HasTrustedTime)
            {
                countdownText.text += "\n" + Loc.Tr(
                    "ui.saison.offline",
                    "Hors ligne — progression locale, synchronisation à la reconnexion");
            }

            // Kill-switch / message remote (MT4-G3).
            if (!RemoteTuning.SeasonEnabled || !string.IsNullOrEmpty(RemoteTuning.InfoMessage))
            {
                string msg = !string.IsNullOrEmpty(RemoteTuning.InfoMessage)
                    ? RemoteTuning.InfoMessage
                    : Loc.Tr(
                        "ui.saison.maintenance",
                        "Saison en maintenance — revenez plus tard");
                countdownText.text += "\n" + msg;
            }
        }

        private void RefreshTrack()
        {
            if (tierEntries == null)
                return;

            for (int i = 0; i < tierEntries.Length; i++)
            {
                if (tierEntries[i] != null)
                    tierEntries[i].Refresh();
            }
        }

        private void RefreshPrestige(PersistentManager pm)
        {
            int claimable = SeasonRewards.GetPrestigeClaimableCount();
            if (prestigeLabel != null)
            {
                prestigeLabel.text = Loc.Format(
                    "ui.saison.prestige",
                    "Prestige claimable : {0} (déjà {1})",
                    claimable,
                    pm.PrestigeTiersClaimed);
            }

            if (prestigeClaimButton != null)
            {
                prestigeClaimButton.interactable = claimable > 0;
                prestigeClaimButton.gameObject.SetActive(true);
            }
        }

        private void RefreshReviewButton(PersistentManager pm)
        {
            if (reviewRecapButton == null)
                return;

            SeasonRecapData recap = pm.PendingSeasonRecap;
            bool hasRecap = recap != null
                && !string.IsNullOrEmpty(recap.seasonId)
                && (recap.pending || recap.rewardsCredited);
            reviewRecapButton.gameObject.SetActive(hasRecap);
        }

        private void CenterTrackOnCurrentTier()
        {
            if (trackScroll == null || tierEntries == null || tierEntries.Length == 0)
                return;

            int eligible = SeasonRewards.GetHighestEligibleTierNumber();
            int focusIndex = Mathf.Clamp(eligible > 0 ? eligible - 1 : 0, 0, tierEntries.Length - 1);
            float t = tierEntries.Length <= 1
                ? 1f
                : 1f - (focusIndex / (float)(tierEntries.Length - 1));
            trackScroll.verticalNormalizedPosition = Mathf.Clamp01(t);
        }

        private void StartCountdown()
        {
            StopCountdown();
            if (!isActiveAndEnabled)
                return;
            _countdownRoutine = StartCoroutine(CountdownLoop());
        }

        private void StopCountdown()
        {
            if (_countdownRoutine == null)
                return;
            StopCoroutine(_countdownRoutine);
            _countdownRoutine = null;
        }

        private IEnumerator CountdownLoop()
        {
            while (_isOpen)
            {
                RefreshCountdownLabel();
                yield return new WaitForSecondsRealtime(60f);
            }
        }

        private void OnReviewRecapClicked()
        {
            if (seasonRecap != null)
                seasonRecap.OpenForConsultation();
        }

        private void OnPrestigeClaimClicked()
        {
            SeasonRewards.ClaimAllPrestige();
            RefreshAll();
        }
    }
}
