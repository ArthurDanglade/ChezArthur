using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;

namespace ChezArthur.Meta
{
    /// <summary>
    /// État d'un palier de piste.
    /// </summary>
    public enum SeasonTierState
    {
        Locked = 0,
        Claimable = 1,
        Claimed = 2
    }

    /// <summary>
    /// Cerveau de la piste de saison : claims, prestige, entitlements rollover, crédit récap.
    /// </summary>
    public static class SeasonRewards
    {
        /// <summary>
        /// État du palier (index 0-based).
        /// </summary>
        public static SeasonTierState GetTierState(int index)
        {
            PersistentManager pm = PersistentManager.Instance;
            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            if (pm == null || config == null)
                return SeasonTierState.Locked;

            SeasonTier tier = config.GetTier(index);
            if (tier == null)
                return SeasonTierState.Locked;

            if (IsTierClaimed(pm, index))
                return SeasonTierState.Claimed;

            if (pm.BestScoreThisSeason >= tier.scoreRequired)
                return SeasonTierState.Claimable;

            return SeasonTierState.Locked;
        }

        /// <summary>
        /// Réclame un palier éligible : Tals (+ LR via AddCharacter si grantsLrLevel).
        /// </summary>
        public static bool TryClaim(int index)
        {
            PersistentManager pm = PersistentManager.Instance;
            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            if (pm == null || config == null)
                return false;

            if (GetTierState(index) != SeasonTierState.Claimable)
                return false;

            SeasonTier tier = config.GetTier(index);
            if (tier == null)
                return false;

            if (tier.talsReward > 0)
                pm.AddTals(tier.talsReward);

            if (tier.grantsLrLevel)
            {
                string lrId = config.GetLrIdForSeason(pm.SeasonId);
                if (!string.IsNullOrEmpty(lrId) && pm.Characters != null)
                {
                    bool isNew = pm.Characters.AddCharacter(lrId);
                    pm.SaveGame();
                    Debug.Log(
                        $"[Season] Claim palier {index + 1} → LR '{lrId}' " +
                        (isNew ? "(nouveau)" : "(doublon / +niveau)"));
                }
            }

            if (!pm.TryClaimSeasonTier(index))
                return false;

            Debug.Log(
                $"[Season] Claim palier {index + 1} : +{tier.talsReward} Tals " +
                $"(score={pm.BestScoreThisSeason})");
            return true;
        }

        /// <summary>
        /// Nombre de paliers prestige encore claimables.
        /// </summary>
        public static int GetPrestigeClaimableCount()
        {
            PersistentManager pm = PersistentManager.Instance;
            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            if (pm == null || config == null)
                return 0;

            return ComputePrestigeClaimable(
                pm.BestScoreThisSeason,
                pm.PrestigeTiersClaimed,
                config);
        }

        /// <summary>
        /// Réclame tout le prestige disponible. Retourne le nombre de paliers crédités.
        /// </summary>
        public static int ClaimAllPrestige()
        {
            PersistentManager pm = PersistentManager.Instance;
            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            if (pm == null || config == null)
                return 0;

            int n = GetPrestigeClaimableCount();
            if (n <= 0)
                return 0;

            int tals = n * config.PrestigeTalsReward;
            if (tals > 0)
                pm.AddTals(tals);

            pm.IncrementPrestigeClaimed(n);
            Debug.Log($"[Season] Prestige ×{n} : +{tals} Tals");
            return n;
        }

        /// <summary>
        /// Calcule les entitlements non réclamés de la saison finie (appelé avant reset).
        /// </summary>
        public static void ComputeRolloverEntitlements(SeasonRecapData recap)
        {
            if (recap == null)
                return;

            PersistentManager pm = PersistentManager.Instance;
            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            if (pm == null || config == null)
                return;

            int score = recap.finalScore;
            int pendingTals = 0;
            int pendingLr = 0;
            int lastTier = 0;

            for (int i = 0; i < config.TierCount; i++)
            {
                SeasonTier tier = config.GetTier(i);
                if (tier == null)
                    continue;

                if (score < tier.scoreRequired)
                    break;

                lastTier = i + 1;
                if (IsTierClaimed(pm, i))
                    continue;

                pendingTals += tier.talsReward;
                if (tier.grantsLrLevel)
                    pendingLr++;
            }

            int prestigeLeft = ComputePrestigeClaimable(score, pm.PrestigeTiersClaimed, config);
            pendingTals += prestigeLeft * config.PrestigeTalsReward;

            recap.pendingTals = pendingTals;
            recap.pendingLrLevels = pendingLr;
            recap.lrCharacterId = config.GetLrIdForSeason(recap.seasonId);
            recap.lastTierReached = lastTier;
            recap.rewardsCredited = false;
            recap.pending = true;
        }

        /// <summary>
        /// Crédite le récap pending (affichage G4 / DebugMenu G3). Une seule fois.
        /// </summary>
        public static void CreditPendingRecap()
        {
            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
                return;

            SeasonRecapData recap = pm.PendingSeasonRecap;
            if (recap == null || !recap.pending || recap.rewardsCredited)
                return;

            if (recap.pendingTals > 0)
                pm.AddTals(recap.pendingTals);

            if (recap.pendingLrLevels > 0
                && !string.IsNullOrEmpty(recap.lrCharacterId)
                && pm.Characters != null)
            {
                for (int i = 0; i < recap.pendingLrLevels; i++)
                    pm.Characters.AddCharacter(recap.lrCharacterId);
                pm.SaveGame();
            }

            pm.MarkRecapRewardsCredited();
            Debug.Log(
                $"[Season] Récap crédité : +{recap.pendingTals} Tals, " +
                $"LR×{recap.pendingLrLevels} ({recap.lrCharacterId})");
        }

        /// <summary>
        /// True si ce LR est dans le portail cumulatif (saisons passées).
        /// </summary>
        public static bool IsLrUnlockedForPortal(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return false;

            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
                return false;

            IReadOnlyList<string> past = pm.PastSeasonLrIds;
            if (past == null)
                return false;

            for (int i = 0; i < past.Count; i++)
            {
                if (past[i] == characterId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Index du prochain palier claimable, ou -1.
        /// </summary>
        public static int GetNextClaimableTierIndex()
        {
            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            if (config == null)
                return -1;

            for (int i = 0; i < config.TierCount; i++)
            {
                if (GetTierState(i) == SeasonTierState.Claimable)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Plus haut palier éligible (1-based) selon le score courant, 0 si aucun.
        /// </summary>
        public static int GetHighestEligibleTierNumber()
        {
            PersistentManager pm = PersistentManager.Instance;
            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            if (pm == null || config == null)
                return 0;

            int last = 0;
            for (int i = 0; i < config.TierCount; i++)
            {
                SeasonTier tier = config.GetTier(i);
                if (tier == null)
                    continue;
                if (pm.BestScoreThisSeason < tier.scoreRequired)
                    break;
                last = i + 1;
            }

            return last;
        }

        private static bool IsTierClaimed(PersistentManager pm, int index)
        {
            IReadOnlyList<int> claimed = pm.ClaimedTiers;
            if (claimed == null)
                return false;

            for (int i = 0; i < claimed.Count; i++)
            {
                if (claimed[i] == index)
                    return true;
            }

            return false;
        }

        private static int ComputePrestigeClaimable(
            int score,
            int prestigeAlreadyClaimed,
            SeasonRewardsConfig config)
        {
            SeasonTier last = config.GetTier(config.TierCount - 1);
            if (last == null || score < last.scoreRequired)
                return 0;

            int raw = (score - last.scoreRequired) / config.PrestigeStep;
            int left = raw - prestigeAlreadyClaimed;
            return left > 0 ? left : 0;
        }
    }
}
