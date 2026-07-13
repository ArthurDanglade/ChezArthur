#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Gameplay;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Monte StageTransitionUI + branche RunManager pour les transitions inter-étages.
    /// </summary>
    public static class StageTransitionUIBuilder
    {
        private const string RootName = "StageTransitionUI";
        private const string TickClipPath = "Assets/_Project/Audio/SFX/Talsound1.mp3";

        [MenuItem("Chez Arthur/UI/Monter StageTransitionUI (ascenseur étages)")]
        public static void Build()
        {
            Undo.SetCurrentGroupName("Monter StageTransitionUI");
            int undoGroup = Undo.GetCurrentGroup();

            Canvas canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogError("[StageTransitionUIBuilder] Aucun Canvas trouvé dans la scène.");
                return;
            }

            StageTransitionUI transition = EnsureTransitionUI(canvas);
            WireRunManager(transition);
            WireElevatorRefs(transition);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = transition.gameObject;
            Debug.Log("[StageTransitionUIBuilder] Montage terminé (ascenseur caméra + swap sur noir).");
        }

        private static Canvas FindMainCanvas()
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].renderMode != RenderMode.WorldSpace)
                    return canvases[i];
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static StageTransitionUI EnsureTransitionUI(Canvas canvas)
        {
            StageTransitionUI existing = Object.FindObjectOfType<StageTransitionUI>(true);
            if (existing != null)
            {
                UpgradeExistingLayout(existing);
                WireElevatorRefs(existing);
                HideImmediateOn(existing);
                return existing;
            }

            GameObject root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup), typeof(StageTransitionUI));
            Undo.RegisterCreatedObjectUndo(root, "Créer StageTransitionUI");
            root.transform.SetParent(canvas.transform, false);
            root.SetActive(true);

            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.SetAsLastSibling();

            CanvasGroup overlay = root.GetComponent<CanvasGroup>();
            overlay.alpha = 0f;
            overlay.blocksRaycasts = false;
            overlay.interactable = false;

            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0.07f, 0.08f, 0.12f, 0.72f);
            dim.raycastTarget = true;

            RectTransform panelRt = CreateCenteredPanel(root.transform);
            TextMeshProUGUI label;
            TextMeshProUGUI number;
            BuildPanelTexts(panelRt, out label, out number);

            StageTransitionUI ui = root.GetComponent<StageTransitionUI>();
            AudioClip tick = AssetDatabase.LoadAssetAtPath<AudioClip>(TickClipPath);

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("overlayGroup").objectReferenceValue = overlay;
            so.FindProperty("panelRoot").objectReferenceValue = panelRt;
            so.FindProperty("labelText").objectReferenceValue = label;
            so.FindProperty("stageNumberText").objectReferenceValue = number;
            so.FindProperty("tickClip").objectReferenceValue = tick;
            so.ApplyModifiedPropertiesWithoutUndo();

            HideImmediateOn(ui);
            return ui;
        }

        private static void UpgradeExistingLayout(StageTransitionUI ui)
        {
            Transform panel = ui.transform.Find("Panel");
            if (panel == null)
                return;

            RectTransform panelRt = panel as RectTransform;
            if (panelRt == null)
                return;

            Undo.RecordObject(panelRt, "Recentrer panel transition");
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(520f, 260f);

            VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = Undo.AddComponent<VerticalLayoutGroup>(panel.gameObject);

            Undo.RecordObject(vlg, "Configurer VLG transition");
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 4;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            TextMeshProUGUI label = panel.Find("Label")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI number = panel.Find("StageNumber")?.GetComponent<TextMeshProUGUI>();

            if (label != null)
            {
                Undo.RecordObject(label, "Configurer label transition");
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 30f;
                LayoutElement le = label.GetComponent<LayoutElement>();
                if (le == null)
                    le = Undo.AddComponent<LayoutElement>(label.gameObject);
                le.preferredHeight = 42f;
            }

            if (number != null)
            {
                Undo.RecordObject(number, "Configurer numéro transition");
                number.alignment = TextAlignmentOptions.Center;
                number.fontSize = 104f;
                LayoutElement le = number.GetComponent<LayoutElement>();
                if (le == null)
                    le = Undo.AddComponent<LayoutElement>(number.gameObject);
                le.preferredHeight = 120f;
            }

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("panelRoot").objectReferenceValue = panelRt;
            so.FindProperty("labelText").objectReferenceValue = label;
            so.FindProperty("stageNumberText").objectReferenceValue = number;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform CreateCenteredPanel(Transform parent)
        {
            GameObject panel = new GameObject("Panel", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panel, "Créer panel transition");
            panel.transform.SetParent(parent, false);

            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(520f, 260f);

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            return panelRt;
        }

        private static void BuildPanelTexts(RectTransform panelRt, out TextMeshProUGUI label, out TextMeshProUGUI number)
        {
            label = CreateText("Label", panelRt, "ÉTAGE", 30f, UiTheme.TextSecondary, FontStyles.Normal);
            label.alignment = TextAlignmentOptions.Center;
            LayoutElement labelLe = label.gameObject.AddComponent<LayoutElement>();
            labelLe.preferredHeight = 42f;

            number = CreateText("StageNumber", panelRt, "1", 104f, UiTheme.Gold, FontStyles.Bold);
            number.alignment = TextAlignmentOptions.Center;
            LayoutElement numberLe = number.gameObject.AddComponent<LayoutElement>();
            numberLe.preferredHeight = 120f;
        }

        private static void WireElevatorRefs(StageTransitionUI transition)
        {
            if (transition == null)
                return;

            Arena arena = Object.FindObjectOfType<Arena>();
            TurnManager turnManager = Object.FindObjectOfType<TurnManager>();

            CameraShake shake = null;
            Camera main = Camera.main;
            if (main != null)
                shake = main.GetComponent<CameraShake>();

            SerializedObject so = new SerializedObject(transition);
            so.FindProperty("arena").objectReferenceValue = arena;
            so.FindProperty("turnManager").objectReferenceValue = turnManager;
            so.FindProperty("cameraShake").objectReferenceValue = shake;
            so.FindProperty("overlayMaxAlpha").floatValue = 0.95f;
            so.FindProperty("fadeInDuration").floatValue = 0.45f;
            so.FindProperty("tickInterval").floatValue = 0.28f;
            so.FindProperty("preScrollSettleDuration").floatValue = 0.3f;
            so.FindProperty("scrollDuration").floatValue = 1.3f;
            so.FindProperty("blackHoldDuration").floatValue = 0.45f;
            so.FindProperty("fadeOutDuration").floatValue = 0.55f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void HideImmediateOn(StageTransitionUI ui)
        {
            SerializedObject so = new SerializedObject(ui);
            CanvasGroup group = so.FindProperty("overlayGroup").objectReferenceValue as CanvasGroup;
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }

        private static TextMeshProUGUI CreateText(
            string name,
            RectTransform parent,
            string text,
            float size,
            Color color,
            FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = UiGen.LoadFont();
            if (font != null)
                tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static void WireRunManager(StageTransitionUI transition)
        {
            RunManager runManager = Object.FindObjectOfType<RunManager>(true);
            if (runManager == null)
            {
                Debug.LogWarning("[StageTransitionUIBuilder] RunManager introuvable — assigne stageTransitionUI manuellement.");
                return;
            }

            SerializedObject so = new SerializedObject(runManager);
            so.FindProperty("stageTransitionUI").objectReferenceValue = transition;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
