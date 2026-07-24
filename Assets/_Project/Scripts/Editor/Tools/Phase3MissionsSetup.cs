#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChezArthur.Core;
using ChezArthur.Missions;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Phase 3 — missions hebdo + composition. Merge dans le catalogue existant.
    /// </summary>
    public static class Phase3MissionsSetup
    {
        private const string MissionsFolder = "Assets/_Project/ScriptableObjects/Missions";
        private const string WeeklyFolder = MissionsFolder + "/Weekly";
        private const string CatalogPath = MissionsFolder + "/MissionCatalog.asset";
        private const string HubScenePath = "Assets/_Project/Scenes/Hub.unity";

        [MenuItem("Chez Arthur/Missions/Phase 3 — Appliquer Weekly + Composition")]
        public static void ApplyPhase3()
        {
            EnsureFolder(WeeklyFolder);

            List<MissionData> weekly = CreateWeeklyMissions();
            MissionCatalog catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
            if (catalog == null)
            {
                EditorUtility.DisplayDialog(
                    "Phase 3 Missions",
                    "Catalogue introuvable. Lance d'abord Phase 2.",
                    "OK");
                return;
            }

            List<MissionData> merged = new List<MissionData>();
            IReadOnlyList<MissionData> existing = catalog.Missions;
            for (int i = 0; i < existing.Count; i++)
            {
                if (existing[i] != null && existing[i].Layer != MissionLayer.Weekly)
                    merged.Add(existing[i]);
            }

            for (int i = 0; i < weekly.Count; i++)
                merged.Add(weekly[i]);

            catalog.EditorSetMissions(merged);
            EditorUtility.SetDirty(catalog);

            // Re-wire manager (au cas où)
            bool hubOk = EnsureMissionManagerHasCatalog(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Phase 3 Missions",
                "Terminé.\n\n" +
                "• " + weekly.Count + " missions Weekly (composition + univers slot 1)\n" +
                "• Catalogue mergé (" + merged.Count + " total)\n" +
                "• Hub MissionManager : " + (hubOk ? "OK" : "vérifier Hub") + "\n\n" +
                "Note : weekly_boss_rush_3 attend la Phase 4 (Boss Rush).\n" +
                "Test : équipe full rôle semaine + étage 15 ; finir étage 20 pour univers.",
                "OK");

            Debug.Log("[Phase3MissionsSetup] OK — weekly=" + weekly.Count + ", total=" + merged.Count);
        }

        private static List<MissionData> CreateWeeklyMissions()
        {
            return new List<MissionData>
            {
                Upsert(WeeklyFolder, "weekly_stage_10", "Atteindre l'étage 10",
                    "Atteins l'étage 10.", MissionLayer.Weekly,
                    MissionTriggerType.StageReached, 10, 400, false, 10),

                Upsert(WeeklyFolder, "weekly_universe_slot1", "Terminer l'univers (slot 1)",
                    "Vaincs le boss final de l'univers du slot 1 cette semaine.", MissionLayer.Weekly,
                    MissionTriggerType.UniverseCompleted, 1, 600, false, 20,
                    seasonSlot1: true),

                Upsert(WeeklyFolder, "weekly_stage15_full_role", "Étage 15 — full rôle",
                    "Équipe full ATK/DEF/SUP selon la semaine. Spé Hub.", MissionLayer.Weekly,
                    MissionTriggerType.StageReached, 15, 700, false, 30,
                    composition: MissionCompositionRequirement.FullSeasonRole,
                    minCompositionStage: 10),

                Upsert(WeeklyFolder, "weekly_boss_rush_3", "3 boss en Boss Rush",
                    "Vaincs 3 boss différents en Boss Rush (Phase 4).", MissionLayer.Weekly,
                    MissionTriggerType.BossRushBossDefeated, 3, 900, false, 40),

                Upsert(WeeklyFolder, "weekly_bonus_all", "Bonus : 4 missions de la semaine",
                    "Complète les 4 missions hebdomadaires.", MissionLayer.Weekly,
                    MissionTriggerType.LayerCompletionBonus, 4, 500, false, 100)
            };
        }

        private static MissionData Upsert(
            string folder,
            string id,
            string displayName,
            string description,
            MissionLayer layer,
            MissionTriggerType trigger,
            int target,
            int tals,
            bool firstTime,
            int order,
            bool seasonSlot1 = false,
            MissionCompositionRequirement composition = MissionCompositionRequirement.None,
            int minCompositionStage = 0)
        {
            string path = folder + "/Mission_" + id + ".asset";
            MissionData data = AssetDatabase.LoadAssetAtPath<MissionData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MissionData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.EditorApply(
                id, displayName, description, layer, trigger, target, tals,
                firstTime, order,
                seasonSlot1Universe: seasonSlot1,
                composition: composition,
                minCompositionStage: minCompositionStage);
            return data;
        }

        private static bool EnsureMissionManagerHasCatalog(MissionCatalog catalog)
        {
            if (!File.Exists(HubScenePath))
                return false;

            Scene scene = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
            PersistentManager pm = Object.FindObjectOfType<PersistentManager>(true);
            if (pm == null)
                return false;

            MissionManager manager = pm.GetComponent<MissionManager>();
            if (manager == null)
                manager = pm.gameObject.AddComponent<MissionManager>();

            SerializedObject so = new SerializedObject(manager);
            SerializedProperty catalogProp = so.FindProperty("catalog");
            if (catalogProp == null)
                return false;

            catalogProp.objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
