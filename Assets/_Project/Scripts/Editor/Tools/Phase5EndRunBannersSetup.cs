#if UNITY_EDITOR
using UnityEditor;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Phase 5 historique — redirige vers le polish 5b (bandeaux pro + purge chiffres).
    /// </summary>
    public static class Phase5EndRunBannersSetup
    {
        [MenuItem("Chez Arthur/Missions/Phase 5 — Appliquer Bandeaux Fin de Run")]
        public static void ApplyPhase5()
        {
            Phase5bEndRunPolishSetup.Apply();
        }
    }
}
#endif
