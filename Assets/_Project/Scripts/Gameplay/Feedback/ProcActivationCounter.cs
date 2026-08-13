using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Compteur d'activations de procs par run (graine gacha F5-L2).
    /// Aucune UI — consommateur futur (récap fin de run).
    /// </summary>
    public static class ProcActivationCounter
    {
        private static readonly Dictionary<(int unitId, string passiveId), int> _counts =
            new Dictionary<(int, string), int>(32);

        private static string _lastLoggedKey;
        private static int _lastLoggedCount;

        /// <summary> Incrémente et retourne le total pour (unité, passif). </summary>
        public static int Increment(int sourceUnitId, string passiveId)
        {
            if (string.IsNullOrEmpty(passiveId))
                passiveId = "?";

            var key = (sourceUnitId, passiveId);
            if (!_counts.TryGetValue(key, out int n))
                n = 0;
            n++;
            _counts[key] = n;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Throttle : log seulement si la clé change ou le compte franchit ×5.
            if (passiveId != _lastLoggedKey || n == 1 || (n % 5) == 0)
            {
                _lastLoggedKey = passiveId;
                _lastLoggedCount = n;
                Debug.Log($"[Proc] {passiveId} ×{n}");
            }
#endif
            return n;
        }

        public static void Reset()
        {
            _counts.Clear();
            _lastLoggedKey = null;
            _lastLoggedCount = 0;
        }
    }
}
