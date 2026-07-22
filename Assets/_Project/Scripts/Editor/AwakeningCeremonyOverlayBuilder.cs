#if UNITY_EDITOR
using System.Collections.Generic;
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
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;
        private const float MatchWidthOrHeight = 0f;
        private const int SortingOrder = 300;

        private const float PortraitAspect = 228f / 532f;
        private const float BannerAnchorY = 0.90f;
        private const float BannerWidthRatio = 0.9f;
        private const float BannerHeight = 160f;
        private const float BannerInset = 3f;
        private const float AmbientGlowScale = 2.6f;
        private const float RaysScale = 2.4f;
        private const float RimBloomOverflow = 0.22f;
        private const int MoteCount = 14;
        private const float HintBottomOffset = 80f;
        private const float HintHeight = 48f;

        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/AwakeningCeremonyOverlay.prefab";
        private const string WhitePixelPath = "Assets/_Project/Art/UI/ui_white_pixel.png";
        private const string RoundedSpritePath =
            "Assets/_Project/Sprites/Placeholders/card_rounded.png";
        private const string RadialGlowPath = "Assets/_Project/Art/FX/fx_radial_glow.png";
        private const string MotePath = "Assets/_Project/Art/FX/fx_mote.png";
        private const string CardBloomPath = "Assets/_Project/Art/FX/fx_card_bloom.png";
        private const string RaysPath = "Assets/_Project/Art/FX/fx_rays.png";
        private const string GlowMatPath = "Assets/_Project/Art/FX/AwakeningGlow.mat";

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
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");

            Sprite pixel = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePixelPath)
                ?? UiGen.SolidWhite;
            Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath)
                ?? LoadSpriteByName("card_rounded")
                ?? pixel;
            Sprite radialGlow = AssetDatabase.LoadAssetAtPath<Sprite>(RadialGlowPath) ?? pixel;
            Sprite moteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MotePath) ?? radialGlow;
            Sprite cardBloom = AssetDatabase.LoadAssetAtPath<Sprite>(CardBloomPath) ?? radialGlow;
            Sprite raysSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RaysPath) ?? radialGlow;
            Material glowMat = AssetDatabase.LoadAssetAtPath<Material>(GlowMatPath);
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

            float portraitH = RefHeight;
            float portraitW = portraitH * PortraitAspect;

            // AmbientGlow — souffle très large (évite le halo « coupé » rectangle)
            GameObject ambientGo = CreateUi("AmbientGlow", root.transform);
            RectTransform ambientRt = ambientGo.GetComponent<RectTransform>();
            SetAnchors(ambientRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            float ambientSize = Mathf.Max(RefWidth, RefHeight) * AmbientGlowScale;
            ambientRt.sizeDelta = new Vector2(ambientSize, ambientSize);
            Image ambientImg = MakeGlowImage(ambientGo, radialGlow, glowMat, UiTheme.CeremonyLight);

            // RaysRoot — diametre ≥ diagonale carte × scale, pour déborder hors cadre
            GameObject raysGo = CreateUi("RaysRoot", root.transform);
            RectTransform raysRt = raysGo.GetComponent<RectTransform>();
            SetAnchors(raysRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            float raysSize = Mathf.Max(portraitW, portraitH) * RaysScale;
            raysRt.sizeDelta = new Vector2(raysSize, raysSize);
            Image raysImg = MakeGlowImage(raysGo, raysSprite, glowMat, UiTheme.CeremonyLight);

            // PortraitContainer
            GameObject containerGo = CreateUi("PortraitContainer", root.transform);
            RectTransform containerRt = containerGo.GetComponent<RectTransform>();
            SetAnchors(containerRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            containerRt.sizeDelta = new Vector2(portraitW, portraitH);

            AspectRatioFitter fitter = containerGo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fitter.aspectRatio = PortraitAspect;

            CanvasGroup portraitGroup = containerGo.AddComponent<CanvasGroup>();
            portraitGroup.alpha = 1f;
            portraitGroup.blocksRaycasts = false;
            portraitGroup.interactable = false;

            // RimBloom — PREMIER enfant (derrière les vues), overflow -12%
            GameObject rimGo = CreateUi("RimBloom", containerGo.transform);
            RectTransform rimRt = rimGo.GetComponent<RectTransform>();
            StretchFull(rimRt);
            float overflowX = portraitW * RimBloomOverflow;
            float overflowY = portraitH * RimBloomOverflow;
            rimRt.offsetMin = new Vector2(-overflowX, -overflowY);
            rimRt.offsetMax = new Vector2(overflowX, overflowY);
            Image rimImg = MakeGlowImage(rimGo, cardBloom, glowMat, UiTheme.CeremonyLight);

            CharacterArtworkView primeView = CreateArtworkChild(containerGo.transform, "PrimeView", pixel);
            CharacterArtworkView dechuView = CreateArtworkChild(containerGo.transform, "DechuView", pixel);
            RawImage primeRaw = primeView.GetComponentInChildren<RawImage>(true);
            RawImage dechuRaw = dechuView.GetComponentInChildren<RawImage>(true);
            if (primeRaw != null)
                primeRaw.enabled = false;
            if (dechuRaw != null)
                dechuRaw.enabled = false;

            // GlowFront — dernier enfant du portrait (voile frontal)
            GameObject glowFrontGo = CreateUi("GlowFront", containerGo.transform);
            StretchFull(glowFrontGo.GetComponent<RectTransform>());
            Image glowFrontImg = MakeGlowImage(glowFrontGo, radialGlow, glowMat, UiTheme.CeremonyLight);

            // MoteRoot
            GameObject moteRootGo = CreateUi("MoteRoot", root.transform);
            StretchFull(moteRootGo.GetComponent<RectTransform>());
            var moteImages = new List<Image>(MoteCount);
            for (int i = 0; i < MoteCount; i++)
            {
                GameObject moteGo = CreateUi($"Mote_{i}", moteRootGo.transform);
                RectTransform moteRt = moteGo.GetComponent<RectTransform>();
                SetAnchors(moteRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                float size = 12f + (i % 5) * 4f;
                moteRt.sizeDelta = new Vector2(size, size);
                float angle = (i / (float)MoteCount) * Mathf.PI * 2f;
                float radius = 180f + (i % 7) * 35f;
                moteRt.anchoredPosition = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius * 1.4f);

                Image moteImg = MakeGlowImage(moteGo, moteSprite, glowMat, UiTheme.Gold);
                moteImages.Add(moteImg);
            }

            // FlashOverlay
            GameObject flashGo = CreateUi("FlashOverlay", root.transform);
            StretchFull(flashGo.GetComponent<RectTransform>());
            Image flashImg = flashGo.AddComponent<Image>();
            flashImg.sprite = pixel;
            Color flashColor = UiTheme.CeremonyLight;
            flashColor.a = 0f;
            flashImg.color = flashColor;
            flashImg.raycastTarget = false;
            flashGo.SetActive(false);

            // BannerRoot
            GameObject bannerGo = CreateUi("BannerRoot", root.transform);
            RectTransform bannerRt = bannerGo.GetComponent<RectTransform>();
            SetAnchors(
                bannerRt,
                new Vector2(0.5f, BannerAnchorY),
                new Vector2(0.5f, BannerAnchorY),
                new Vector2(0.5f, 0.5f));
            bannerRt.sizeDelta = new Vector2(RefWidth * BannerWidthRatio, BannerHeight);

            GameObject bannerFlashGo = CreateUi("BannerFlash", bannerGo.transform);
            RectTransform bannerFlashRt = bannerFlashGo.GetComponent<RectTransform>();
            SetAnchors(bannerFlashRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            bannerFlashRt.sizeDelta = new Vector2(RefWidth * 0.7f, RefWidth * 0.35f);
            Image bannerFlashImg = MakeGlowImage(bannerFlashGo, radialGlow, glowMat, UiTheme.CeremonyLight);

            GameObject bannerPanelGo = CreateUi("BannerPanel", bannerGo.transform);
            StretchFull(bannerPanelGo.GetComponent<RectTransform>());

            Image bannerFrame = bannerPanelGo.AddComponent<Image>();
            bannerFrame.sprite = rounded;
            bannerFrame.type = Image.Type.Sliced;
            bannerFrame.color = UiTheme.Gold;
            bannerFrame.raycastTarget = false;

            GameObject bannerFillGo = CreateUi("Fill", bannerPanelGo.transform);
            RectTransform fillRt = bannerFillGo.GetComponent<RectTransform>();
            StretchFull(fillRt);
            fillRt.offsetMin = new Vector2(BannerInset, BannerInset);
            fillRt.offsetMax = new Vector2(-BannerInset, -BannerInset);
            Image bannerFill = bannerFillGo.AddComponent<Image>();
            bannerFill.sprite = rounded;
            bannerFill.type = Image.Type.Sliced;
            bannerFill.color = UiTheme.CardPanel;
            bannerFill.raycastTarget = false;

            TextMeshProUGUI bannerTmp = CreateTmp(
                bannerPanelGo.transform, "BannerText", font, UiTheme.FontCelebration, UiTheme.Gold,
                TextAlignmentOptions.Center);
            bannerTmp.text = string.Empty;
            bannerTmp.enableAutoSizing = false;
            bannerTmp.enableWordWrapping = true;
            bannerTmp.maxVisibleLines = 2;
            StretchFull(bannerTmp.rectTransform);
            RectTransform bannerTextRt = bannerTmp.rectTransform;
            bannerTextRt.offsetMin = new Vector2(24f, 12f);
            bannerTextRt.offsetMax = new Vector2(-24f, -12f);
            bannerGo.SetActive(false);

            // HintText
            TextMeshProUGUI hintTmp = CreateTmp(
                root.transform, "HintText", font, UiTheme.FontCaption, UiTheme.TextMuted,
                TextAlignmentOptions.Center);
            hintTmp.text = "Toucher pour continuer";
            RectTransform hintRt = hintTmp.rectTransform;
            SetAnchors(hintRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            hintRt.anchoredPosition = new Vector2(0f, HintBottomOffset);
            hintRt.sizeDelta = new Vector2(900f, HintHeight);
            hintTmp.gameObject.SetActive(false);

            // TapCatcher
            GameObject tapGo = CreateUi("TapCatcher", root.transform);
            StretchFull(tapGo.GetComponent<RectTransform>());
            Image tapImg = tapGo.AddComponent<Image>();
            tapImg.sprite = pixel;
            Color tapColor = UiTheme.CardPanel;
            tapColor.a = 0f;
            tapImg.color = tapColor;
            tapImg.raycastTarget = true;
            Button tapBtn = tapGo.AddComponent<Button>();
            tapBtn.targetGraphic = tapImg;
            tapBtn.transition = Selectable.Transition.None;

            // Wire View
            AwakeningCeremonyView view = root.GetComponent<AwakeningCeremonyView>();
            SerializedObject so = new SerializedObject(view);
            SetObj(so, "canvasGroup", rootGroup);
            SetObj(so, "primeView", primeView);
            SetObj(so, "dechuView", dechuView);
            SetObj(so, "primeRawImage", primeRaw);
            SetObj(so, "dechuRawImage", dechuRaw);
            SetObj(so, "portraitContainer", containerRt);
            SetObj(so, "ambientGlow", ambientImg);
            SetObj(so, "raysRoot", raysRt);
            SetObj(so, "raysImage", raysImg);
            SetObj(so, "rimBloom", rimImg);
            SetObj(so, "glowFront", glowFrontImg);
            SetObj(so, "moteRoot", moteRootGo.GetComponent<RectTransform>());
            SetObj(so, "flashOverlay", flashImg);
            SetObj(so, "bannerRoot", bannerGo);
            SetObj(so, "bannerText", bannerTmp);
            SetObj(so, "bannerFlash", bannerFlashImg);
            SetObj(so, "hintText", hintTmp);
            SetObj(so, "tapButton", tapBtn);

            SerializedProperty motesProp = so.FindProperty("moteImages");
            if (motesProp != null)
            {
                motesProp.ClearArray();
                motesProp.arraySize = moteImages.Count;
                for (int i = 0; i < moteImages.Count; i++)
                    motesProp.GetArrayElementAtIndex(i).objectReferenceValue = moteImages[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            SetArtworkModeFit(primeView);
            SetArtworkModeFit(dechuView);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }

        private static Image MakeGlowImage(
            GameObject go, Sprite sprite, Material mat, Color rgb)
        {
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            if (mat != null)
                img.material = mat;
            Color c = rgb;
            c.a = 0f;
            img.color = c;
            img.raycastTarget = false;
            return img;
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
            raw.color = UiTheme.TextPrimary;
            raw.raycastTarget = false;
            raw.enabled = false;

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

        private static Sprite LoadSpriteByName(string spriteName)
        {
            string[] guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null && s.name == spriteName)
                    return s;
            }

            return null;
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
