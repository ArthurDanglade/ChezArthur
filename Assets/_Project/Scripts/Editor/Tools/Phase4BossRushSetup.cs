#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.BossRush;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Hub.Pages;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Phase 4 — câble BossRushManager (Hub), BossRushRunController (Game), overlay Jouer.
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
                "• Overlay Jouer (Normale / Boss Rush) sur PageAccueil\n" +
                "• BossRushRunController sur scène Game : " + (gameOk ? "OK" : "échec") + "\n" +
                "• Hub : " + (hubOk ? "OK" : "échec") + "\n\n" +
                "Test : tuer un miniboss/boss en run → Jouer → Boss Rush.",
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

            BuildModeSelectOverlay(accueil);

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

        private static void BuildModeSelectOverlay(PageAccueilUI accueil)
        {
            SerializedObject so = new SerializedObject(accueil);
            SerializedProperty rootProp = so.FindProperty("modeSelectRoot");

            GameObject root;
            if (rootProp != null && rootProp.objectReferenceValue is GameObject existing && existing != null)
            {
                root = existing;
            }
            else
            {
                Transform parent = accueil.transform;
                root = new GameObject("ModeSelectOverlay", typeof(RectTransform));
                root.transform.SetParent(parent, false);
                RectTransform rt = root.GetComponent<RectTransform>();
                StretchFull(rt);

                Image dim = root.AddComponent<Image>();
                dim.color = new Color(0f, 0f, 0f, 0.65f);

                GameObject panel = new GameObject("Panel", typeof(RectTransform));
                panel.transform.SetParent(root.transform, false);
                RectTransform panelRt = panel.GetComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.5f, 0.5f);
                panelRt.anchorMax = new Vector2(0.5f, 0.5f);
                panelRt.sizeDelta = new Vector2(420f, 360f);
                panelRt.anchoredPosition = Vector2.zero;
                Image panelBg = panel.AddComponent<Image>();
                panelBg.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);

                VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(24, 24, 24, 24);
                vlg.spacing = 12f;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;

                CreateTmp(panel.transform, "Title", "Choisir un mode", 28f, FontStyles.Bold);
                Button btnNormal = CreateButton(panel.transform, "BtnNormal", "Run normale");
                Button btnRush = CreateButton(panel.transform, "BtnBossRush", "Boss Rush");
                TextMeshProUGUI locked = CreateTmp(panel.transform, "LockedLabel",
                    "Bats au moins un boss pour débloquer le boss rush", 16f, FontStyles.Normal);
                TextMeshProUGUI info = CreateTmp(panel.transform, "InfoLabel", "", 14f, FontStyles.Italic);
                Button btnCancel = CreateButton(panel.transform, "BtnCancel", "Annuler");

                so.FindProperty("modeSelectRoot").objectReferenceValue = root;
                so.FindProperty("buttonModeNormal").objectReferenceValue = btnNormal;
                so.FindProperty("buttonModeBossRush").objectReferenceValue = btnRush;
                so.FindProperty("buttonModeCancel").objectReferenceValue = btnCancel;
                so.FindProperty("bossRushLockedLabel").objectReferenceValue = locked;
                so.FindProperty("bossRushInfoLabel").objectReferenceValue = info;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            root.SetActive(false);
            EditorUtility.SetDirty(accueil);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, float size, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = size + 8f;
            le.preferredHeight = size + 12f;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.45f, 0.75f, 1f);
            Button btn = go.AddComponent<Button>();
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = 56f;
            le.preferredHeight = 56f;

            GameObject textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            StretchFull(textGo.GetComponent<RectTransform>());
            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return btn;
        }
    }
}
#endif
