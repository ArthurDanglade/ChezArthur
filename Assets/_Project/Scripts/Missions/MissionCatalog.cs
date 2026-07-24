using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Missions
{
    /// <summary>
    /// Catalogue de toutes les missions du jeu (réf. ScriptableObjects).
    /// </summary>
    [CreateAssetMenu(
        fileName = "MissionCatalog",
        menuName = "Chez Arthur/Missions/Mission Catalog",
        order = 21)]
    public class MissionCatalog : ScriptableObject
    {
        [SerializeField] private List<MissionData> missions = new List<MissionData>();

        public IReadOnlyList<MissionData> Missions => missions;

        public MissionData GetById(string missionId)
        {
            if (string.IsNullOrEmpty(missionId) || missions == null)
                return null;

            for (int i = 0; i < missions.Count; i++)
            {
                MissionData m = missions[i];
                if (m != null && m.MissionId == missionId)
                    return m;
            }

            return null;
        }

        public void CollectByLayer(MissionLayer layer, List<MissionData> results)
        {
            results.Clear();
            if (missions == null)
                return;

            for (int i = 0; i < missions.Count; i++)
            {
                MissionData m = missions[i];
                if (m != null && m.Layer == layer)
                    results.Add(m);
            }

            results.Sort(CompareSortOrder);
        }

#if UNITY_EDITOR
        public void EditorSetMissions(List<MissionData> list)
        {
            missions = list != null ? new List<MissionData>(list) : new List<MissionData>();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private static int CompareSortOrder(MissionData a, MissionData b)
        {
            int cmp = a.SortOrder.CompareTo(b.SortOrder);
            if (cmp != 0)
                return cmp;
            return string.CompareOrdinal(a.MissionId, b.MissionId);
        }
    }
}
