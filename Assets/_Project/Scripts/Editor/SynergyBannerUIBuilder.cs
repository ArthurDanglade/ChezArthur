#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Construit le canvas SynergyBanner dans la scène active (Game.unity attendue).
    /// Idempotent : abandonne si SynergyBannerCanvas existe déjà.
    /// </summary>
    public static class SynergyBannerUIBuilder
    {
        private const string CanvasName = "SynergyBannerCanvas";
        private const string BannerName = "Banner";
        private const int SortingOrder = 110;

        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;
        private const float BannerWidthRatio = 0.8f;
        private const float BannerTopOffsetRatio = 0.18f;
        private const float BannerHeight = 200f;

        [MenuItem("Chez Arthur/UI/Build Synergy Banner")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[SynergyBannerUIBuilder] Aucune scène active.");
                return;
            }

            if (FindRootByName(scene, CanvasName) != null)
            {
                Debug.LogWarning(
                    $"[SynergyBannerUIBuilder] '{CanvasName}' existe déjà dans '{scene.name}' — abandon (idempotent).");
                return;
            }

            Undo.SetCurrentGroupName("Build Synergy Banner");
            int undoGroup = Undo.GetCurrentGroup();

            GameObject canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SynergyBannerUI));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create SynergyBannerCanvas");

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            StretchFull(canvasRect);

            GameObject bannerGo = CreateUiChild(canvasGo.transform, BannerName);
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
            background.color = UiTheme.SurfaceBar;
            background.raycastTarget = false;
            StretchFull(background.rectTransform);

            TMP_FontAsset font = LoadSpecBannerFont();
            TextMeshProUGUI label = CreateTmp(bannerGo.transform, "Label", font, UiTheme.FontLabel);
            label.text = "SYNERGIE ACTIVÉE";
            label.color = UiTheme.TextPrimary;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            AnchorTopStretch(label.rectTransform, top: 16f, height: 36f);

            TextMeshProUGUI synergyName = CreateTmp(bannerGo.transform, "SynergyName", font, UiTheme.FontName);
            synergyName.text = "Nom synergie";
            synergyName.color = UiTheme.Gold;
            synergyName.fontStyle = FontStyles.Bold;
            synergyName.alignment = TextAlignmentOptions.Center;
            synergyName.raycastTarget = false;
            AnchorTopStretch(synergyName.rectTransform, top: 56f, height: 48f);

            SerializedObject so = new SerializedObject(canvasGo.GetComponent<SynergyBannerUI>());
            UiGen.Wire(so, "canvasGroup", canvasGroup);
            UiGen.Wire(so, "labelText", label);
            UiGen.Wire(so, "nameText", synergyName);
            UiGen.Wire(so, "backgroundImage", background);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = canvasGo;
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                $"[SynergyBannerUIBuilder] '{CanvasName}' créé (sortingOrder={SortingOrder}). " +
                "Branche les SFX par défaut sur SynergyBannerUI, puis sauvegarde la scène.");
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

        private static TMP_FontAsset LoadSpecBannerFont()
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
