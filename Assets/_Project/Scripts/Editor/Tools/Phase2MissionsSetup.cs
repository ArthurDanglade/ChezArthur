#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Missions;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Phase 2 — crée MissionData (daily + permanents faciles), catalogue,
    /// et câble MissionManager sur PersistentManager (Hub).
    /// </summary>
    public static class Phase2MissionsSetup
    {
        private const string MissionsFolder = "Assets/_Project/ScriptableObjects/Missions";
        private const string DailyFolder = MissionsFolder + "/Daily";
        private const string PermanentFolder = MissionsFolder + "/Permanent";
        private const string CatalogPath = MissionsFolder + "/MissionCatalog.asset";
        private const string HubScenePath = "Assets/_Project/Scenes/Hub.unity";

        [MenuItem("Chez Arthur/Missions/Phase 2 — Appliquer Missions Core (Data+Manager)")]
        public static void ApplyPhase2()
        {
            EnsureFolder(DailyFolder);
            EnsureFolder(PermanentFolder);

            List<MissionData> all = new List<MissionData>(24);
            all.AddRange(CreateDailyMissions());
            all.AddRange(CreatePermanentMissions());

            MissionCatalog catalog = EnsureCatalog(all);
            bool wired = WireMissionManager(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Phase 2 Missions",
                "Terminé.\n\n" +
                "• " + all.Count + " MissionData créés/màj\n" +
                "• Catalogue : " + CatalogPath + "\n" +
                "• MissionManager sur PersistentManager (Hub) : " +
                (wired ? "câblé" : "déjà OK / vérifie la scène Hub") + "\n\n" +
                "Play Hub puis Game : DebugMenu → MISSIONS.",
                "OK");

            Debug.Log("[Phase2MissionsSetup] OK — missions=" + all.Count + ", wired=" + wired);
        }

        // ═══════════════════════════════════════════
        // CRÉATION DATA
        // ═══════════════════════════════════════════

        private static List<MissionData> CreateDailyMissions()
        {
            var list = new List<MissionData>
            {
                Upsert(DailyFolder, "daily_stage_5", "Atteindre l'étage 5",
                    "Survis jusqu'à l'étage 5.", MissionLayer.Daily,
                    MissionTriggerType.StageReached, 5, 150, false, 10),
                Upsert(DailyFolder, "daily_super_lancer_5", "Réussir 5 Super Lancers",
                    "Cumule 5 Super Lancers dans la journée.", MissionLayer.Daily,
                    MissionTriggerType.SuperLancerSuccess, 5, 150, false, 20),
                Upsert(DailyFolder, "daily_kills_15", "Tuer 15 ennemis",
                    "Élimine 15 ennemis (toutes runs).", MissionLayer.Daily,
                    MissionTriggerType.EnemyKilled, 15, 150, false, 30),
                Upsert(DailyFolder, "daily_valise_1", "Obtenir une valise",
                    "Choisis une valise dans l'écran de bonus.", MissionLayer.Daily,
                    MissionTriggerType.ValiseObtained, 1, 150, false, 40),
                Upsert(DailyFolder, "daily_item_1", "Obtenir un item",
                    "Choisis un item dans l'écran de bonus.", MissionLayer.Daily,
                    MissionTriggerType.ItemObtained, 1, 150, false, 50),
                Upsert(DailyFolder, "daily_bonus_all", "Bonus : 5 missions du jour",
                    "Complète les 5 missions quotidiennes.", MissionLayer.Daily,
                    MissionTriggerType.LayerCompletionBonus, 5, 300, false, 100)
            };
            return list;
        }

        private static List<MissionData> CreatePermanentMissions()
        {
            var list = new List<MissionData>
            {
                Upsert(PermanentFolder, "perm_first_pull", "Première invocation",
                    "Effectue ton premier tirage.", MissionLayer.Permanent,
                    MissionTriggerType.GachaPull, 1, 200, true, 10),
                Upsert(PermanentFolder, "perm_first_ssr", "Premier SSR",
                    "Obtiens ton premier personnage SSR.", MissionLayer.Permanent,
                    MissionTriggerType.CharacterObtained, 1, 500, true, 20,
                    rarityFilter: true, rarity: CharacterRarity.SSR),
                Upsert(PermanentFolder, "perm_first_super_lancer", "Premier Super Lancer",
                    "Réussis ton premier Super Lancer.", MissionLayer.Permanent,
                    MissionTriggerType.SuperLancerSuccess, 1, 150, true, 30),
                Upsert(PermanentFolder, "perm_stage_20", "Étage 20",
                    "Atteins l'étage 20 pour la première fois.", MissionLayer.Permanent,
                    MissionTriggerType.BestStageReached, 20, 300, true, 40),
                Upsert(PermanentFolder, "perm_stage_50", "Étage 50",
                    "Atteins l'étage 50 pour la première fois.", MissionLayer.Permanent,
                    MissionTriggerType.BestStageReached, 50, 600, true, 50),
                Upsert(PermanentFolder, "perm_stage_100", "Étage 100",
                    "Atteins l'étage 100 pour la première fois.", MissionLayer.Permanent,
                    MissionTriggerType.BestStageReached, 100, 1500, true, 60),
                Upsert(PermanentFolder, "perm_stage_101", "Entrée post-game",
                    "Atteins l'étage 101.", MissionLayer.Permanent,
                    MissionTriggerType.BestStageReached, 101, 2000, true, 70),
                Upsert(PermanentFolder, "perm_first_lr", "Premier LR",
                    "Obtiens ton premier personnage LR.", MissionLayer.Permanent,
                    MissionTriggerType.CharacterObtained, 1, 1000, true, 80,
                    rarityFilter: true, rarity: CharacterRarity.LR)
            };
            return list;
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
            bool rarityFilter = false,
            CharacterRarity rarity = CharacterRarity.SSR)
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
                firstTime, order, rarityFilter, rarity);
            return data;
        }

        private static MissionCatalog EnsureCatalog(List<MissionData> missions)
        {
            MissionCatalog catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MissionCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSetMissions(missions);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static bool WireMissionManager(MissionCatalog catalog)
        {
            if (!File.Exists(HubScenePath))
            {
                Debug.LogError("[Phase2MissionsSetup] Hub introuvable : " + HubScenePath);
                return false;
            }

            Scene scene = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
            PersistentManager pm = Object.FindObjectOfType<PersistentManager>(true);
            if (pm == null)
            {
                Debug.LogError("[Phase2MissionsSetup] PersistentManager introuvable dans Hub.");
                return false;
            }

            MissionManager manager = pm.GetComponent<MissionManager>();
            if (manager == null)
                manager = pm.gameObject.AddComponent<MissionManager>();

            SerializedObject so = new SerializedObject(manager);
            SerializedProperty catalogProp = so.FindProperty("catalog");
            if (catalogProp == null)
            {
                Debug.LogError("[Phase2MissionsSetup] Champ catalog introuvable sur MissionManager.");
                return false;
            }

            bool changed = catalogProp.objectReferenceValue != catalog;
            catalogProp.objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return changed;
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
