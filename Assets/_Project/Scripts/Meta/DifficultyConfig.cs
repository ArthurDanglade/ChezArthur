using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Meta
{
    /// <summary>
    /// Un cran de difficulté (label affichable + multiplicateur de score/scaling).
    /// </summary>
    [Serializable]
    public class DifficultyTier
    {
        public string label = "x1";
        public float multiplier = 1f;
    }

    /// <summary>
    /// Config des crans de saison (MT2-D6). Chargée depuis Resources.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DifficultyConfig",
        menuName = "Chez Arthur/Meta/Difficulty Config",
        order = 11)]
    public class DifficultyConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string ResourcesPath = "DifficultyConfig";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private List<DifficultyTier> tiers = new List<DifficultyTier>
        {
            new DifficultyTier { label = "x1", multiplier = 1f },
            new DifficultyTier { label = "x1,5", multiplier = 1.5f },
            new DifficultyTier { label = "x2", multiplier = 2f },
            new DifficultyTier { label = "x3", multiplier = 3f },
            new DifficultyTier { label = "x5", multiplier = 5f }
        };

        [Tooltip("Étage requis dans le cran N pour débloquer N+1.")]
        [SerializeField] private int unlockStage = 50;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static DifficultyConfig _cached;
        private static bool _missingWarned;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public int UnlockStage => unlockStage > 0 ? unlockStage : 50;

        public int TierCount
        {
            get
            {
                EnsureTiers();
                return tiers.Count;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Charge (et cache) l'asset Resources. Absent → défauts runtime + 1 warning.
        /// </summary>
        public static DifficultyConfig LoadDefault()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<DifficultyConfig>(ResourcesPath);
            if (_cached != null)
                return _cached;

            if (!_missingWarned)
            {
                _missingWarned = true;
                Debug.LogWarning(
                    "[Season] DifficultyConfig absent de Resources — défauts codés en dur.");
            }

            _cached = CreateInstance<DifficultyConfig>();
            _cached.EnsureTiers();
            return _cached;
        }

        public string GetLabel(int index)
        {
            EnsureTiers();
            if (index < 0 || index >= tiers.Count)
                return tiers.Count > 0 ? tiers[0].label : "x1";
            return string.IsNullOrEmpty(tiers[index].label) ? "x1" : tiers[index].label;
        }

        public float GetMultiplier(int index)
        {
            EnsureTiers();
            if (index < 0 || index >= tiers.Count)
                return 1f;
            float m = tiers[index].multiplier;
            return m > 0f ? m : 1f;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureTiers()
        {
            if (tiers != null && tiers.Count > 0)
                return;

            tiers = new List<DifficultyTier>
            {
                new DifficultyTier { label = "x1", multiplier = 1f },
                new DifficultyTier { label = "x1,5", multiplier = 1.5f },
                new DifficultyTier { label = "x2", multiplier = 2f },
                new DifficultyTier { label = "x3", multiplier = 3f },
                new DifficultyTier { label = "x5", multiplier = 5f }
            };
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            EnsureTiers();
        }
#endif
    }
}
