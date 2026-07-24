#if UNITY_EDITOR
using System.IO;
using ChezArthur.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Scène sandbox UI Kit (Gate 1.3a/b) : PanelSurface, HubButtonUI, TabBarUI.
    /// Idempotent, Undo-safe. Hors Build Settings.
    /// </summary>
    public static class UIKitSandboxBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string SceneFolder = "Assets/_Project/Scenes/Dev";
        private const string ScenePath = SceneFolder + "/UIKitSandbox.unity";
        private const string RootName = "UIKitSandboxRoot";
        private const string UndoLabel = "UI Kit Sandbox";
        private const float PanelSampleHeight = 140f;

        private struct PanelSample
        {
            public string Label;
            public PanelSurface.SurfaceVariant Variant;
            public PanelSurface.SurfaceBorder Border;

            public PanelSample(string label, PanelSurface.SurfaceVariant variant, PanelSurface.SurfaceBorder border)
            {
                Label = label;
                Variant = variant;
                Border = border;
            }
        }

        private static readonly PanelSample[] PanelSamples =
        {
            new PanelSample("Panel / Subtle", PanelSurface.SurfaceVariant.Panel, PanelSurface.SurfaceBorder.Subtle),
            new PanelSample("Card / Subtle", PanelSurface.SurfaceVariant.Card, PanelSurface.SurfaceBorder.Subtle),
            new PanelSample("Pill / Subtle", PanelSurface.SurfaceVariant.Pill, PanelSurface.SurfaceBorder.Subtle),
            new PanelSample("Card / AccentAmber", PanelSurface.SurfaceVariant.Card, PanelSurface.SurfaceBorder.AccentAmber),
            new PanelSample("Card / AccentGold", PanelSurface.SurfaceVariant.Card, PanelSurface.SurfaceBorder.AccentGold),
        };

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI Kit/Construire la sandbox")]
        public static void Build()
        {
            RoundedRectSpriteGenerator.GenerateAll();

            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteL = RoundedRectSpriteGenerator.LoadSpriteL();
            if (spriteS == null || spriteM == null || spriteL == null)
            {
                EditorUtility.DisplayDialog(
                    "Sprites manquants",
                    "Impossible de charger RoundedRect_S/M/L après génération.",
                    "OK");
                return;
            }

            EnsureFolder(SceneFolder);
            Scene scene = OpenOrCreateSandboxScene();
            EnsureCamera(scene);
            EnsureEventSystem(scene);
            Canvas canvas = FindOrCreateCanvas(scene);
            RebuildContent(canvas, spriteS, spriteM, spriteL);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"[UIKitSandboxBuilder] Sandbox prête : `{ScenePath}` (hors Build Settings).");
        }

        // ═══════════════════════════════════════════
        // SCÈNE
        // ═══════════════════════════════════════════

        private static Scene OpenOrCreateSandboxScene()
        {
            if (File.Exists(Path.GetFullPath(ScenePath)))
                return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void EnsureCamera(Scene scene)
        {
            Camera[] cams = Object.FindObjectsOfType<Camera>();
            for (int i = 0; i < cams.Length; i++)
            {
                if (cams[i] != null && cams[i].gameObject.scene == scene)
                {
                    ApplySandboxCamera(cams[i]);
                    return;
                }
            }

            GameObject camGo = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGo, UndoLabel);
            SceneManager.MoveGameObjectToScene(camGo, scene);
            camGo.tag = "MainCamera";
            ApplySandboxCamera(Undo.AddComponent<Camera>(camGo));
        }

        private static void ApplySandboxCamera(Camera cam)
        {
            Undo.RecordObject(cam, UndoLabel);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiTheme.BgDeep;
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.depth = -1f;
            cam.cullingMask = 0;
            cam.allowHDR = false;
            cam.allowMSAA = false;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            EventSystem[] systems = Object.FindObjectsOfType<EventSystem>();
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].gameObject.scene == scene)
                    return;
            }

            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, UndoLabel);
            SceneManager.MoveGameObjectToScene(es, scene);
        }

        private static Canvas FindOrCreateCanvas(Scene scene)
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].gameObject.scene == scene)
                    return canvases[i];
            }

            GameObject canvasGo = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, UndoLabel);
            SceneManager.MoveGameObjectToScene(canvasGo, scene);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            SerializedObject so = new SerializedObject(scaler);
            so.FindProperty("m_UiScaleMode").enumValueIndex = (int)CanvasScaler.ScaleMode.ScaleWithScreenSize;
            so.FindProperty("m_ReferenceResolution").vector2Value = new Vector2(1080f, 1920f);
            so.FindProperty("m_ScreenMatchMode").enumValueIndex = (int)CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            so.FindProperty("m_MatchWidthOrHeight").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();

            StretchFull((RectTransform)canvasGo.transform);
            return canvas;
        }

        // ═══════════════════════════════════════════
        // CONTENU
        // ═══════════════════════════════════════════

        private static void RebuildContent(Canvas canvas, Sprite spriteS, Sprite spriteM, Sprite spriteL)
        {
            Transform canvasTx = canvas.transform;

            Transform existing = canvasTx.Find(RootName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            EnsureBackground(canvasTx);

            GameObject root = new GameObject(RootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, UndoLabel);
            Undo.SetTransformParent(root.transform, canvasTx, false, UndoLabel);
            StretchFull((RectTransform)root.transform);

            // Scroll pour tout voir sur tall screens
            ScrollRect scroll = Undo.AddComponent<ScrollRect>(root);
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            Undo.RegisterCreatedObjectUndo(viewport, UndoLabel);
            Undo.SetTransformParent(viewport.transform, root.transform, false, UndoLabel);
            StretchFull((RectTransform)viewport.transform);
            Image vpImg = viewport.GetComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);
            vpImg.raycastTarget = true;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(content, UndoLabel);
            Undo.SetTransformParent(content.transform, viewport.transform, false, UndoLabel);
            RectTransform contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(0f, 0f);
            contentRt.offsetMax = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = Undo.AddComponent<VerticalLayoutGroup>(content);
            vlg.padding = new RectOffset(
                (int)UiTheme.Space5, (int)UiTheme.Space5,
                (int)UiTheme.Space5, (int)UiTheme.Space5);
            vlg.spacing = UiTheme.Space5;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter fitter = Undo.AddComponent<ContentSizeFitter>(content);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = contentRt;

            Transform col = content.transform;

            CreateSectionTitle(col, "Panneaux (PanelSurface)");
            for (int i = 0; i < PanelSamples.Length; i++)
                CreatePanelSample(col, PanelSamples[i], spriteS, spriteM, spriteL);

            CreateSectionTitle(col, "Boutons (HubButtonUI)");
            CreateHubButton(col, "LANCER UNE RUN", HubButtonUI.ButtonVariant.Primary, locked: false, null, spriteS, spriteM, spriteL);
            CreateHubButton(col, "Boss Rush", HubButtonUI.ButtonVariant.Secondary, locked: false, null, spriteS, spriteM, spriteL);
            CreateHubButton(col, "Boss Rush", HubButtonUI.ButtonVariant.Secondary, locked: true,
                "Bats au moins un boss pour débloquer", spriteS, spriteM, spriteL);
            CreateHubButton(col, "Magasin", HubButtonUI.ButtonVariant.Secondary, locked: true,
                "Bientôt disponible", spriteS, spriteM, spriteL);

            CreateSectionTitle(col, "Onglets (TabBarUI)");
            TabBarUI tabBar = CreateTabBarSample(col, spriteS);

            AttachBootstrap(root.transform, tabBar);

            // Force rebuild layout
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }

        private static void EnsureBackground(Transform canvasTx)
        {
            const string bgName = "Background";
            Transform existing = canvasTx.Find(bgName);
            Image bgImage;
            if (existing != null)
            {
                bgImage = existing.GetComponent<Image>() ?? Undo.AddComponent<Image>(existing.gameObject);
            }
            else
            {
                GameObject bgGo = new GameObject(bgName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(bgGo, UndoLabel);
                Undo.SetTransformParent(bgGo.transform, canvasTx, false, UndoLabel);
                bgImage = bgGo.GetComponent<Image>();
            }

            StretchFull((RectTransform)bgImage.transform);
            bgImage.color = UiTheme.BgDeep;
            bgImage.raycastTarget = false;
            bgImage.transform.SetAsFirstSibling();
        }

        private static void CreateSectionTitle(Transform parent, string title)
        {
            GameObject go = new GameObject("Section_" + SanitizeName(title), typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = 56f;
            le.preferredHeight = 56f;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = UiTypography.Title;
            tmp.color = UiTheme.TextSecondary;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        private static void CreatePanelSample(
            Transform parent,
            PanelSample spec,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL)
        {
            GameObject panelGo = new GameObject(
                SanitizeName(spec.Label),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(panelGo, UndoLabel);
            Undo.SetTransformParent(panelGo.transform, parent, false, UndoLabel);

            LayoutElement le = Undo.AddComponent<LayoutElement>(panelGo);
            le.minHeight = PanelSampleHeight;
            le.preferredHeight = PanelSampleHeight;
            le.flexibleWidth = 1f;

            PanelSurface surface = Undo.AddComponent<PanelSurface>(panelGo);
            SerializedObject so = new SerializedObject(surface);
            so.FindProperty("variant").enumValueIndex = (int)spec.Variant;
            so.FindProperty("borderStyle").enumValueIndex = (int)spec.Border;
            so.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            so.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            so.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            so.FindProperty("blocksRaycasts").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            surface.ApplyStyle();

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelGo, UndoLabel);
            Undo.SetTransformParent(labelGo.transform, panelGo.transform, false, UndoLabel);
            StretchFull((RectTransform)labelGo.transform);

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = spec.Label;
            tmp.fontSize = UiTypography.Body;
            tmp.color = UiTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        private static void CreateHubButton(
            Transform parent,
            string label,
            HubButtonUI.ButtonVariant variant,
            bool locked,
            string subLabel,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL)
        {
            string name = "Btn_" + SanitizeName(label) + (locked ? "_Locked" : "");
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            HubButtonUI hubBtn = Undo.AddComponent<HubButtonUI>(go);
            SerializedObject so = new SerializedObject(hubBtn);
            so.FindProperty("variant").enumValueIndex = (int)variant;
            so.FindProperty("locked").boolValue = locked;
            so.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            so.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            so.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            so.ApplyModifiedPropertiesWithoutUndo();

            hubBtn.ApplyStyle();
            hubBtn.SetLabel(label);
            hubBtn.SetSubLabel(subLabel);
            hubBtn.ApplyStyle();
        }

        private static TabBarUI CreateTabBarSample(Transform parent, Sprite spriteS)
        {
            GameObject barGo = new GameObject(
                "TabBar",
                typeof(RectTransform),
                typeof(TabBarUI));
            Undo.RegisterCreatedObjectUndo(barGo, UndoLabel);
            Undo.SetTransformParent(barGo.transform, parent, false, UndoLabel);

            LayoutElement barLe = Undo.AddComponent<LayoutElement>(barGo);
            barLe.minHeight = UiTheme.TouchTargetMin;
            barLe.preferredHeight = UiTheme.TouchTargetMin;
            barLe.flexibleWidth = 1f;

            // Template
            GameObject template = new GameObject(
                "TabItemTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(template, UndoLabel);
            Undo.SetTransformParent(template.transform, barGo.transform, false, UndoLabel);
            template.SetActive(false);

            Image border = template.GetComponent<Image>();
            border.sprite = spriteS;
            border.type = Image.Type.Sliced;
            border.color = UiTheme.BorderSubtle;

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(fillGo, UndoLabel);
            Undo.SetTransformParent(fillGo.transform, template.transform, false, UndoLabel);
            Image fill = fillGo.GetComponent<Image>();
            fill.sprite = spriteS;
            fill.type = Image.Type.Sliced;
            RectTransform fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            float inset = UiTheme.BorderThin;
            fillRt.offsetMin = new Vector2(inset, inset);
            fillRt.offsetMax = new Vector2(-inset, -inset);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelGo, UndoLabel);
            Undo.SetTransformParent(labelGo.transform, template.transform, false, UndoLabel);
            StretchFull((RectTransform)labelGo.transform);
            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = UiTypography.Label;
            tmp.color = UiTheme.TextSecondary;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            TabBarUI tabBar = barGo.GetComponent<TabBarUI>();
            SerializedObject so = new SerializedObject(tabBar);
            so.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            so.FindProperty("tabItemTemplate").objectReferenceValue = template;
            so.ApplyModifiedPropertiesWithoutUndo();
            return tabBar;
        }

        /// <summary>
        /// Attache le bootstrap Play Mode sur le root sandbox.
        /// </summary>
        private static void AttachBootstrap(Transform root, TabBarUI tabBar)
        {
            var bootstrap = root.GetComponent<ChezArthur.UI.Dev.UIKitSandboxBootstrap>();
            if (bootstrap == null)
                bootstrap = Undo.AddComponent<ChezArthur.UI.Dev.UIKitSandboxBootstrap>(root.gameObject);

            SerializedObject so = new SerializedObject(bootstrap);
            so.FindProperty("tabBar").objectReferenceValue = tabBar;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static string SanitizeName(string label)
        {
            return label.Replace(" / ", "_").Replace(" ", "_").Replace("'", "");
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;

            string[] parts = assetFolder.Split('/');
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
