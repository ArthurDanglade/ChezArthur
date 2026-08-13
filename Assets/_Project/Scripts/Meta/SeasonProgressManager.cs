using UnityEngine;
using ChezArthur.Backend;
using ChezArthur.Core;

namespace ChezArthur.Meta
{
    /// <summary>
    /// Progression de saison : ensure id courant, report de score par étage, rollover.
    /// Statique — pas de MonoBehaviour.
    /// </summary>
    public static class SeasonProgressManager
    {
        /// <summary>
        /// Aligne la save sur la saison calendaire courante. Rollover si id différent.
        /// </summary>
        public static void EnsureSeasonCurrent()
        {
            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
                return;

            string currentId = SeasonRotationManager.CurrentSeasonId;
            string savedId = pm.SeasonId ?? "";

            if (string.IsNullOrEmpty(savedId))
            {
                pm.SetSeasonId(currentId);
                Debug.Log($"[Season] Première init seasonId = {currentId}");
                return;
            }

            if (savedId == currentId)
                return;

            // Kill-switch remote : pas de rollover si saison désactivée.
            if (!RemoteTuning.SeasonEnabled)
            {
                Debug.Log(
                    "[Season] Rollover différé — saison désactivée (remote)");
                return;
            }

            // Live (MT2-G6) : pas de rollover sans temps de confiance (offline pur).
            if (!GameClock.HasTrustedTime)
            {
                Debug.Log(
                    "[Season] Rollover différé — temps de confiance indisponible (offline)");
                return;
            }

            SeasonRecapData recap = new SeasonRecapData
            {
                seasonId = savedId,
                finalScore = pm.BestScoreThisSeason,
                bestStage = pm.BestStageThisSeason,
                bestTier = pm.BestTierThisSeason,
                runs = pm.RunsThisSeason,
                lastTierReached = 0,
                pending = true,
                rewardsCredited = false
            };

            SeasonRewards.ComputeRolloverEntitlements(recap);

            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            string lrId = config != null ? config.GetLrIdForSeason(savedId) : "";
            if (!string.IsNullOrEmpty(lrId))
                pm.AddPastSeasonLr(lrId);

            Debug.Log(
                $"[Season] Rollover {savedId} → {currentId} " +
                $"(score={recap.finalScore}, stage={recap.bestStage}, runs={recap.runs}, " +
                $"pendingTals={recap.pendingTals}, pendingLr={recap.pendingLrLevels}, " +
                $"lastTier={recap.lastTierReached}, portalLr={lrId})");
            pm.ApplySeasonRollover(currentId, recap);
        }

        /// <summary>
        /// Enregistre le score d'étage atteint (étage × multiplicateur de cran).
        /// Ignoré en Boss Rush ou run tainted.
        /// </summary>
        public static void ReportStageReached(
            int stage,
            float tierMultiplier,
            bool isBossRush,
            bool tainted)
        {
            if (isBossRush || tainted)
                return;

            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
                return;

            int score = Mathf.RoundToInt(stage * tierMultiplier);
            if (pm.TryImproveSeasonScore(score, stage, tierMultiplier))
            {
                Debug.Log(
                    $"[Season] Nouveau record score={score} (ét. {stage} ×{tierMultiplier})");
            }
        }
    }
}
