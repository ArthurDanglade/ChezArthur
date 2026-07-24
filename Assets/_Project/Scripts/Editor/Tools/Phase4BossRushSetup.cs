#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChezArthur.BossRush;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Hub.Pages;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Phase 4 — câble BossRushManager (Hub) + BossRushRunController (Game).
    /// Overlay Mode Select retiré (gate 3.2 → HomeActionsBuilder).
    /// </summary>
    public static class Phase4BossRushSetup
    {
        private const string HubScenePath = "Assets/_Project/Scenes/Hub.unity";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Chez Arthur/Missions/Phase 4 — Appliquer Boss Rush")]
        public static void ApplyPhase4()
        {
            List<EnemyData> catalog = CollectEnemyCatalogFromGame();
            bool hubOk = SetupHub(catalog);
            bool gameOk = SetupGame();

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Phase 4 Boss Rush",
                "Terminé.\n\n" +
                "• BossRushManager sur PersistentManager (Hub) — catalogue " + catalog.Count + " ennemis\n" +
                "• UI Accueil : HomeActionsBuilder (gate 3.2) — plus d'overlay Mode Select\n" +
                "• BossRushRunController sur scène Game : " + (gameOk ? "OK" : "échec") + "\n" +
                "• Hub : " + (hubOk ? "OK" : "échec") + "\n\n" +
                "Test : tuer un miniboss/boss en run → Hub → Boss Rush.",
                "OK");

            Debug.Log($"[Phase4BossRushSetup] hub={hubOk} game={gameOk} catalog={catalog.Count}");
        }

        private static List<EnemyData> CollectEnemyCatalogFromGame()
        {
            var list = new List<EnemyData>();
            if (!File.Exists(GameScenePath))
                return list;

            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            StageGenerator gen = Object.FindObjectOfType<StageGenerator>(true);
            if (gen != null)
                list = gen.GetAllEnemyDataCopy();
            return list;
        }

        private static bool SetupHub(List<EnemyData> catalog)
        {
            if (!File.Exists(HubScenePath))
                return false;

            Scene scene = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
            PersistentManager pm = Object.FindObjectOfType<PersistentManager>(true);
            if (pm == null)
            {
                Debug.LogError("[Phase4] PersistentManager introuvable.");
                return false;
            }

            BossRushManager rush = pm.GetComponent<BossRushManager>();
            if (rush == null)
                rush = pm.gameObject.AddComponent<BossRushManager>();
            rush.EditorSetCatalog(catalog);

            PageAccueilUI accueil = Object.FindObjectOfType<PageAccueilUI>(true);
            if (accueil == null)
            {
                Debug.LogError("[Phase4] PageAccueilUI introuvable.");
                return false;
            }

            Debug.Log(
                "[Phase4] Overlay Mode Select obsolète (gate 3.2). " +
                "Utiliser Chez Arthur → Refonte Hub → Construire les Actions Accueil.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }

        private static bool SetupGame()
        {
            if (!File.Exists(GameScenePath))
                return false;

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            RunManager run = Object.FindObjectOfType<RunManager>(true);
            StageGenerator gen = Object.FindObjectOfType<StageGenerator>(true);
            if (run == null)
            {
                Debug.LogError("[Phase4] RunManager introuvable.");
                return false;
            }

            BossRushRunController controller = Object.FindObjectOfType<BossRushRunController>(true);
            if (controller == null)
                controller = run.gameObject.AddComponent<BossRushRunController>();

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty genProp = so.FindProperty("stageGenerator");
            if (genProp != null && gen != null)
                genProp.objectReferenceValue = gen;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }
    }
}
#endif
