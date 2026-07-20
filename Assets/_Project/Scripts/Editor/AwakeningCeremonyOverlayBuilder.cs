#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Gameplay;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// OUTIL DESTRUCTIF — fenêtre Gate 4 éveil.
    /// Construit le prefab AwakeningCeremonyOverlay (Canvas dédié + refs).
    /// </summary>
    public static class AwakeningCeremonyOverlayBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES — Canvas (P2 : Game principal + au-dessus des overlays)
        // ═══════════════════════════════════════════
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;
        private const float MatchWidthOrHeight = 0f;
        private const int SortingOrder = 300; // > BattleTextCanvas (250)

        private const float PortraitHeightRatio = 0.62f;
        private const float PortraitAspect = 228f / 532f;
        private const float TitleTopOffset = -120f;
        private const float NameTopOffset = -200f;
        private const float TitleFontSize = 48f;
        private const float NameFontSize = 36f;

        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/AwakeningCeremonyOverlay.prefab";
        private const string WhitePixelPath = "Assets/_Project/Art/UI/ui_white_pixel.png";

        [MenuItem("Chez Arthur/UI/Construire overlay cérémonie éveil (fenêtre Gate 4)")]
        public static void BuildMenu()
        {
            bool ok = EditorUtility.DisplayDialog(
                "Overlay cérémonie éveil",
                "OUTIL DESTRUCTIF — fenêtre Gate 4 éveil.\n\n" +
                "Écrase AwakeningCeremonyOverlay.prefab s'il existe.\n\nContinuer ?",
                "Construire",
                "Annuler");
            if (!ok)
                return;

            try
            {
                BuildPrefab();
                Debug.Log($"[AwakeningCeremonyOverlayBuilder] Prefab OK : {PrefabPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AwakeningCeremonyOverlayBuilder] Échec : {ex}");
            }
        }

        private static void BuildPrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");
            }

            Sprite pixel = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePixelPath)
                ?? UiGen.SolidWhite;
            TMP_FontAsset font = UiGen.LoadFont();

            GameObject root = new GameObject(
                "AwakeningCeremonyOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(AwakeningCeremonyView));

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = MatchWidthOrHeight;

            CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();
            rootGroup.alpha = 1f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;

            // Background
            GameObject bgGo = CreateUi("Background", root.transform);
            StretchFull(bgGo.GetComponent<RectTransform>());
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = pixel;
            bgImg.color = UiTheme.CardPanel;
            bgImg.raycastTarget = true;

            // TitleText
            TextMeshProUGUI titleTmp = CreateTmp(
                root.transform, "TitleText", font, TitleFontSize, UiTheme.Gold,
                TextAlignmentOptions.Center);
            titleTmp.text = "Éveil";
            RectTransform titleRt = titleTmp.rectTransform;
            SetAnchors(titleRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            titleRt.anchoredPosition = new Vector2(0f, TitleTopOffset);
            titleRt.sizeDelta = new Vector2(900f, 80f);

            // NameText
            TextMeshProUGUI nameTmp = CreateTmp(
                root.transform, "NameText", font, NameFontSize, UiTheme.TextPrimary,
                TextAlignmentOptions.Center);
            nameTmp.text = string.Empty;
            RectTransform nameRt = nameTmp.rectTransform;
            SetAnchors(nameRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            nameRt.anchoredPosition = new Vector2(0f, NameTopOffset);
            nameRt.sizeDelta = new Vector2(900f, 60f);

            // PortraitContainer
            GameObject containerGo = CreateUi("PortraitContainer", root.transform);
            RectTransform containerRt = containerGo.GetComponent<RectTransform>();
            SetAnchors(containerRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            float portraitH = RefHeight * PortraitHeightRatio;
            float portraitW = portraitH * PortraitAspect;
            containerRt.sizeDelta = new Vector2(portraitW, portraitH);

            CharacterArtworkView primeView = CreateArtworkChild(containerGo.transform, "PrimeView", pixel);
            CharacterArtworkView dechuView = CreateArtworkChild(containerGo.transform, "DechuView", pixel);
            RawImage dechuRaw = dechuView.GetComponentInChildren<RawImage>(true);

            // TapCatcher (au-dessus pour recevoir les taps)
            GameObject tapGo = CreateUi("TapCatcher", root.transform);
            StretchFull(tapGo.GetComponent<RectTransform>());
            Image tapImg = tapGo.AddComponent<Image>();
            tapImg.sprite = pixel;
            tapImg.color = new Color(0f, 0f, 0f, 0f);
            tapImg.raycastTarget = true;
            Button tapBtn = tapGo.AddComponent<Button>();
            tapBtn.targetGraphic = tapImg;
            tapBtn.transition = Selectable.Transition.None;

            // Wire AwakeningCeremonyView
            AwakeningCeremonyView view = root.GetComponent<AwakeningCeremonyView>();
            SerializedObject so = new SerializedObject(view);
            SetObj(so, "canvasGroup", rootGroup);
            SetObj(so, "titleText", titleTmp);
            SetObj(so, "nameText", nameTmp);
            SetObj(so, "primeView", primeView);
            SetObj(so, "dechuView", dechuView);
            SetObj(so, "dechuRawImage", dechuRaw);
            SetObj(so, "tapButton", tapBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Mode Fit sur les deux CharacterArtworkView
            SetArtworkModeFit(primeView);
            SetArtworkModeFit(dechuView);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }

        private static CharacterArtworkView CreateArtworkChild(
            Transform parent, string name, Sprite pixel)
        {
            GameObject go = CreateUi(name, parent);
            StretchFull(go.GetComponent<RectTransform>());

            GameObject rawGo = CreateUi("RawImage", go.transform);
            StretchFull(rawGo.GetComponent<RectTransform>());
            RawImage raw = rawGo.AddComponent<RawImage>();
            raw.texture = null;
            raw.color = Color.white;
            raw.raycastTarget = false;

            CharacterArtworkView artwork = go.AddComponent<CharacterArtworkView>();
            SerializedObject so = new SerializedObject(artwork);
            SetObj(so, "rawImage", raw);
            so.ApplyModifiedPropertiesWithoutUndo();
            return artwork;
        }

        private static void SetArtworkModeFit(CharacterArtworkView view)
        {
            SerializedObject so = new SerializedObject(view);
            SerializedProperty modeProp = so.FindProperty("mode");
            if (modeProp != null)
                modeProp.enumValueIndex = (int)CharacterArtworkView.DisplayMode.Fit;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateUi(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float size,
            Color color,
            TextAlignmentOptions align)
        {
            GameObject go = CreateUi(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetAnchors(
            RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
        }

        private static void SetObj(SerializedObject so, string field, Object value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null)
                p.objectReferenceValue = value;
            else
                Debug.LogWarning($"[AwakeningCeremonyOverlayBuilder] Champ '{field}' introuvable.");
        }
    }
}
#endif
