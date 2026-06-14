#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace ChezArthur.Debugging
{
    /// <summary>
    /// Flags globaux de triche debug (strippés en release).
    /// </summary>
    public static class DebugCheats
    {
        /// <summary> Les alliés ne prennent aucun dégât. </summary>
        public static bool GodMode;

        /// <summary> Tout dégât allié→ennemi devient létal. </summary>
        public static bool OneShot;

        /// <summary> Les ennemis ne meurent pas (bloqués à 1 PV minimum). </summary>
        public static bool EnemyGodMode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            GodMode = false;
            OneShot = false;
            EnemyGodMode = false;
        }
    }
}
#endif
