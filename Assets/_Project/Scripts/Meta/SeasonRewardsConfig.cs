using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Meta
{
    /// <summary>
    /// Palier de la piste de saison (score requis, Tals, éventuelle montée LR).
    /// </summary>
    [Serializable]
    public class SeasonTier
    {
        public int scoreRequired;
        public int talsReward;
        public bool grantsLrLevel;
    }

    /// <summary>
    /// LR associé à un index de saison calendaire (S1 → seasonIndex 1).
    /// </summary>
    [Serializable]
    public class SeasonLrEntry
    {
        public int seasonIndex = 1;
        public string lrCharacterId = "goat";
    }

    /// <summary>
    /// Grille de récompenses de saison (MT2-D3/D7/D10). Resources/SeasonRewardsConfig.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SeasonRewardsConfig",
        menuName = "Chez Arthur/Meta/Season Rewards Config",
        order = 12)]
    public class SeasonRewardsConfig : ScriptableObject
    {
        private const string ResourcesPath = "SeasonRewardsConfig";

        private static readonly int[] DefaultScores =
        {
            20, 40, 60, 80, 100, 130, 160, 200, 250, 320, 400, 500
        };

        // Index 0-based des paliers qui donnent une montée LR (5/8/10/12 → 4/7/9/11).
        private static readonly int[] LrTierIndices = { 4, 7, 9, 11 };

        [SerializeField] private List<SeasonTier> tiers = new List<SeasonTier>();
        [SerializeField] private int prestigeStep = 150;
        [SerializeField] private int prestigeTalsReward = 50;
        [SerializeField] private List<SeasonLrEntry> seasonLrEntries = new List<SeasonLrEntry>
        {
            new SeasonLrEntry { seasonIndex = 1, lrCharacterId = "goat" }
        };

        private static SeasonRewardsConfig _cached;
        private static bool _missingWarned;

        public int PrestigeStep => prestigeStep > 0 ? prestigeStep : 150;
        public int PrestigeTalsReward => prestigeTalsReward > 0 ? prestigeTalsReward : 50;

        public int TierCount
        {
            get
            {
                EnsureTiers();
                return tiers.Count;
            }
        }

        public static SeasonRewardsConfig LoadDefault()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<SeasonRewardsConfig>(ResourcesPath);
            if (_cached != null)
            {
                _cached.EnsureTiers();
                return _cached;
            }

            if (!_missingWarned)
            {
                _missingWarned = true;
                Debug.LogWarning(
                    "[Season] SeasonRewardsConfig absent de Resources — défauts codés en dur.");
            }

            _cached = CreateInstance<SeasonRewardsConfig>();
            _cached.EnsureTiers();
            return _cached;
        }

        public SeasonTier GetTier(int index)
        {
            EnsureTiers();
            if (index < 0 || index >= tiers.Count)
                return null;
            return tiers[index];
        }

        /// <summary>
        /// Id LR pour un seasonId "S{n}". Fallback = dernier connu ; vide si rien.
        /// </summary>
        public string GetLrIdForSeason(string seasonId)
        {
            if (seasonLrEntries == null || seasonLrEntries.Count == 0)
                return "";

            int seasonIndex = ParseSeasonIndex(seasonId);
            string fallback = "";
            for (int i = 0; i < seasonLrEntries.Count; i++)
            {
                SeasonLrEntry e = seasonLrEntries[i];
                if (e == null || string.IsNullOrEmpty(e.lrCharacterId))
                    continue;

                fallback = e.lrCharacterId;
                if (e.seasonIndex == seasonIndex)
                    return e.lrCharacterId;
            }

            return fallback ?? "";
        }

        public void EnsureTiers()
        {
            if (tiers != null && tiers.Count == DefaultScores.Length)
                return;

            tiers = new List<SeasonTier>(DefaultScores.Length);
            for (int i = 0; i < DefaultScores.Length; i++)
            {
                tiers.Add(new SeasonTier
                {
                    scoreRequired = DefaultScores[i],
                    talsReward = 100 * (i + 1),
                    grantsLrLevel = IsDefaultLrTier(i)
                });
            }

            if (seasonLrEntries == null || seasonLrEntries.Count == 0)
            {
                seasonLrEntries = new List<SeasonLrEntry>
                {
                    new SeasonLrEntry { seasonIndex = 1, lrCharacterId = "goat" }
                };
            }
        }

        private static bool IsDefaultLrTier(int index)
        {
            for (int i = 0; i < LrTierIndices.Length; i++)
            {
                if (LrTierIndices[i] == index)
                    return true;
            }

            return false;
        }

        private static int ParseSeasonIndex(string seasonId)
        {
            if (string.IsNullOrEmpty(seasonId) || seasonId.Length < 2 || seasonId[0] != 'S')
                return 1;

            if (int.TryParse(seasonId.Substring(1), out int n) && n > 0)
                return n;
            return 1;
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            EnsureTiers();
        }
#endif
    }
}
