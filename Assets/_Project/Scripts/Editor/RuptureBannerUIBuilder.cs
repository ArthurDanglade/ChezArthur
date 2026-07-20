#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.Gameplay;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Construit le canvas RuptureBanner (style SynergyBanner) et câble PressureRupturePresentation.
    /// Idempotent si le canvas existe déjà — re-câble seulement la présentation.
    /// </summary>
    public static class RuptureBannerUIBuilder
    {
        private const string CanvasName = "RuptureBannerCanvas";
        private const string BannerName = "Banner";
        private const int SortingOrder = 120;

        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;
        private const float BannerWidthRatio = 0.86f;
        private const float BannerTopOffsetRatio = 0.22f;
        private const float BannerHeight = 220f;

        [MenuItem("Chez Arthur/UI/Build Rupture Banner")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[RuptureBannerUIBuilder] Aucune scène active.");
                return;
            }

            Undo.SetCurrentGroupName("Build Rupture Banner");
            int undoGroup = Undo.GetCurrentGroup();

            GameObject canvasGo = FindRootByName(scene, CanvasName);
            RuptureBannerUI bannerUi;

            if (canvasGo == null)
            {
                canvasGo = CreateCanvas(scene);
                bannerUi = canvasGo.GetComponent<RuptureBannerUI>();
                BuildBannerHierarchy(canvasGo.transform, bannerUi);
                Debug.Log(
                    $"[RuptureBannerUIBuilder] '{CanvasName}' créé (sortingOrder={SortingOrder}).");
            }
            else
            {
                bannerUi = canvasGo.GetComponent<RuptureBannerUI>();
                Debug.Log(
                    $"[RuptureBannerUIBuilder] '{CanvasName}' existe déjà — re-câblage présentation.");
            }

            WirePresentation(bannerUi);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = canvasGo;
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                "[RuptureBannerUIBuilder] Terminé. Sauvegarde la scène (Ctrl+S), " +
                "puis teste une Rupture.");
        }

        private static GameObject CreateCanvas(Scene scene)
        {
            GameObject canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(RuptureBannerUI));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create RuptureBannerCanvas");
            SceneManager.MoveGameObjectToScene(canvasGo, scene);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            StretchFull(canvasGo.GetComponent<RectTransform>());
            return canvasGo;
        }

        private static void BuildBannerHierarchy(Transform canvasTransform, RuptureBannerUI bannerUi)
        {
            // Flash plein écran (sous le bandeau)
            Image flash = CreateImage(canvasTransform, "Flash", null);
            flash.color = new Color(0.85f, 0.12f, 0.08f, 0f);
            flash.raycastTarget = false;
            StretchFull(flash.rectTransform);
            flash.gameObject.SetActive(false);

            GameObject bannerGo = CreateUiChild(canvasTransform, BannerName);
            RectTransform bannerRect = bannerGo.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.sizeDelta = new Vector2(RefWidth * BannerWidthRatio, BannerHeight);
            bannerRect.anchoredPosition = new Vector2(0f, -RefHeight * BannerTopOffsetRatio);

            CanvasGroup canvasGroup = bannerGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Image background = CreateImage(bannerGo.transform, "Background", UiGen.Card);
            background.type = Image.Type.Sliced;
            Color bg = UiTheme.SurfaceBar;
            bg.a = 0.94f;
            background.color = bg;
            background.raycastTarget = false;
            StretchFull(background.rectTransform);

            // Filet danger en haut du bandeau
            Image accent = CreateImage(bannerGo.transform, "DangerAccent", null);
            accent.color = UiTheme.Negative;
            accent.raycastTarget = false;
            RectTransform accentRt = accent.rectTransform;
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.pivot = new Vector2(0.5f, 1f);
            accentRt.anchoredPosition = Vector2.zero;
            accentRt.sizeDelta = new Vector2(0f, 6f);

            TMP_FontAsset font = LoadFont();
            TextMeshProUGUI title = CreateTmp(bannerGo.transform, "Title", font, UiTheme.FontTitle + 8f);
            title.text = "RUPTURE !";
            title.color = UiTheme.Negative;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = false;
            AnchorTopStretch(title.rectTransform, top: 28f, height: 56f);

            TextMeshProUGUI subtitle = CreateTmp(bannerGo.transform, "Subtitle", font, UiTheme.FontBody);
            subtitle.text = "La pression explose…";
            subtitle.color = UiTheme.TextSecondary;
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.raycastTarget = false;
            AnchorTopStretch(subtitle.rectTransform, top: 96f, height: 44f);

            SerializedObject so = new SerializedObject(bannerUi);
            UiGen.Wire(so, "bannerGroup", canvasGroup);
            UiGen.Wire(so, "bannerRect", bannerRect);
            UiGen.Wire(so, "titleText", title);
            UiGen.Wire(so, "subtitleText", subtitle);
            UiGen.Wire(so, "backgroundImage", background);
            UiGen.Wire(so, "flashImage", flash);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePresentation(RuptureBannerUI bannerUi)
        {
            PressureRupturePresentation presentation =
                Object.FindObjectOfType<PressureRupturePresentation>(true);
            if (presentation == null)
            {
                Debug.LogWarning(
                    "[RuptureBannerUIBuilder] PressureRupturePresentation introuvable — " +
                    "lance d'abord Chez Arthur/UI/Monter systèmes Pression.");
                return;
            }

            SerializedObject so = new SerializedObject(presentation);
            SerializedProperty bannerProp = so.FindProperty("ruptureBanner");
            if (bannerProp != null)
                bannerProp.objectReferenceValue = bannerUi;

            CameraShake shake = Object.FindObjectOfType<CameraShake>(true);
            SerializedProperty shakeProp = so.FindProperty("cameraShake");
            if (shakeProp != null && shake != null)
                shakeProp.objectReferenceValue = shake;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presentation);
            Debug.Log("[RuptureBannerUIBuilder] PressureRupturePresentation câblé.");
        }

        private static GameObject CreateUiChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            GameObject go = CreateUiChild(parent, name);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        private static TextMeshProUGUI CreateTmp(Transform parent, string name, TMP_FontAsset font, float fontSize)
        {
            GameObject go = CreateUiChild(parent, name);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AnchorTopStretch(RectTransform rt, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(0f, height);
        }

        private static TMP_FontAsset LoadFont()
        {
            TMP_FontAsset font = UiGen.LoadFont();
            if (font != null)
                return font;

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        private static GameObject FindRootByName(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                    return roots[i];
            }

            return null;
        }
    }
}
#endif
