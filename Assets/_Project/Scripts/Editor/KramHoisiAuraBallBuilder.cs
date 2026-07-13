#if UNITY_EDITOR
using UnityEditor;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Raccourcis menu pour Kram Hoisi — délègue à CombatAuraBallBuilder.
    /// </summary>
    public static class KramHoisiAuraBallBuilder
    {
        private const string KramCharacterId = "kramhoisi";

        [MenuItem("Chez Arthur/UI/Build Kram Hoisi Aura Ball")]
        public static void Build()
        {
            CombatAuraBallBuilder.BuildOrRefreshById(KramCharacterId, fullRebuild: true);
        }

        [MenuItem("Chez Arthur/UI/Refresh Kram aura frames")]
        public static void RefreshAuraFramesOnly()
        {
            CombatAuraBallBuilder.BuildOrRefreshById(KramCharacterId, fullRebuild: false);
        }
    }
}
#endif
