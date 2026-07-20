#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PixelBattleText;
using ChezArthur.Gameplay;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Monte Pixel Battle Text + câble FloatingNumberSpawner (presets dégâts/crit/KO/soin…).
    /// </summary>
    public static class BattleTextSetupBuilder
    {
        private const string ControllerName = "PixelBattleTextController";
        private const string CanvasName = "BattleTextCanvas";
        private const int SortingOrder = 250;

        private const string PresetRoot = "Assets/PixelBattleText/Animation Presets/";

        [MenuItem("Chez Arthur/UI/Setup Battle Text (Pixel Battle Text)")]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[BattleText] Aucune scène active.");
                return;
            }

            Undo.SetCurrentGroupName("Setup Battle Text");
            int undoGroup = Undo.GetCurrentGroup();

            FloatingNumberSpawner spawner = Object.FindObjectOfType<FloatingNumberSpawner>(true);
            if (spawner == null)
            {
                GameObject spawnerGo = new GameObject("FloatingNumberSpawner");
                Undo.RegisterCreatedObjectUndo(spawnerGo, "Create FloatingNumberSpawner");
                SceneManager.MoveGameObjectToScene(spawnerGo, scene);
                spawner = spawnerGo.AddComponent<FloatingNumberSpawner>();
            }

            RectTransform canvasRect = EnsureBattleTextCanvas(scene);
            PixelBattleTextController controller = EnsureController(canvasRect);

            WireSpawner(spawner, canvasRect, controller);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = spawner.gameObject;
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                "[BattleText] Setup OK — PixelBattleTextController + presets câblés sur FloatingNumberSpawner. " +
                "Sauvegarde la scène (Ctrl+S), lance une run pour tester dégâts / crit / KO / soins.");
        }

        private static RectTransform EnsureBattleTextCanvas(Scene scene)
        {
            GameObject existing = GameObject.Find(CanvasName);
            if (existing != null)
            {
                RectTransform rt = existing.GetComponent<RectTransform>();
                if (rt != null)
                    return rt;
            }

            GameObject canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create BattleTextCanvas");
            SceneManager.MoveGameObjectToScene(canvasGo, scene);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rect = canvasGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static PixelBattleTextController EnsureController(RectTransform canvasRect)
        {
            PixelBattleTextController existing = Object.FindObjectOfType<PixelBattleTextController>(true);
            if (existing != null)
            {
                SerializedObject so = new SerializedObject(existing);
                SerializedProperty canvasProp = so.FindProperty("canvas");
                if (canvasProp != null)
                    canvasProp.objectReferenceValue = canvasRect;
                SerializedProperty snapProp = so.FindProperty("snapToPixelGrid");
                if (snapProp != null)
                    snapProp.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existing);
                return existing;
            }

            GameObject go = new GameObject(ControllerName);
            Undo.RegisterCreatedObjectUndo(go, "Create PixelBattleTextController");
            go.transform.SetParent(canvasRect, false);
            PixelBattleTextController controller = go.AddComponent<PixelBattleTextController>();

            SerializedObject cso = new SerializedObject(controller);
            cso.FindProperty("canvas").objectReferenceValue = canvasRect;
            cso.FindProperty("snapToPixelGrid").boolValue = true;
            cso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void WireSpawner(
            FloatingNumberSpawner spawner,
            RectTransform canvasRect,
            PixelBattleTextController controller)
        {
            SerializedObject so = new SerializedObject(spawner);
            Undo.RecordObject(spawner, "Wire Battle Text");

            SetRef(so, "battleTextCanvas", canvasRect);
            SetBool(so, "preferPixelBattleText", true);
            SetFloat(so, "textSizeMul", 4f);
            SetFloat(so, "durationMul", 1.85f);
            SetFloat(so, "spacingMul", 3.2f);
            SetFloat(so, "motionMul", 1.6f);
            SetInt(so, "minDamageToShow", 5);
            SetInt(so, "minHealToShow", 8);
            SetBool(so, "skipDamagePopupOnKill", true);
            SetInt(so, "maxSimultaneousPopups", 5);
            SetBool(so, "useLegacyFallback", false);

            Camera cam = Camera.main;
            if (cam == null)
            {
                ArenaCamera arenaCam = Object.FindObjectOfType<ArenaCamera>(true);
                if (arenaCam != null)
                    cam = arenaCam.GetComponent<Camera>();
            }

            if (cam == null)
                cam = Object.FindObjectOfType<Camera>(true);

            if (cam != null)
                SetRef(so, "worldCamera", cam);

            SetRef(so, "damageAnim", LoadAnim("textAnim_damage.asset"));
            SetRef(so, "critNumberAnim", LoadAnim("textAnim_crit.asset"));
            SetRef(so, "critLabelAnim", LoadAnim("textAnim_critText.asset"));
            SetRef(so, "healAnim", LoadAnim("textAnim_healing.asset"));
            SetRef(so, "koAnim", LoadAnim("textAnim_KO.asset"));
            SetRef(so, "allyDamageAnim", LoadAnim("textAnim_damage.asset"));
            SetRef(so, "burnAnim", LoadAnim("textAnim_pyro.asset"));
            SetRef(so, "poisonAnim", LoadAnim("textAnim_venom.asset"));
            SetRef(so, "labelAnim", LoadAnim("textAnim_critText.asset"));

            // Conserve le prefab legacy s'il était déjà câblé.
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawner);

            // Force singleton controller awake order: controller must exist before play.
            if (controller != null)
                EditorUtility.SetDirty(controller);
        }

        private static TextAnimation LoadAnim(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<TextAnimation>(PresetRoot + fileName);
        }

        private static void SetRef(SerializedObject so, string field, Object value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null)
                p.objectReferenceValue = value;
        }

        private static void SetBool(SerializedObject so, string field, bool value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null)
                p.boolValue = value;
        }

        private static void SetFloat(SerializedObject so, string field, float value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null)
                p.floatValue = value;
        }

        private static void SetInt(SerializedObject so, string field, int value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null)
                p.intValue = value;
        }
    }
}
#endif
