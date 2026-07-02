#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Gameplay;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère les textures blanches de l'anneau Super Lancer et le prefab world-space associé.
    /// Idempotent : relançable sans dupliquer, GUID du prefab préservé.
    /// Menu : Chez Arthur > Super Lancer > (Re)générer Ring Prefab.
    /// </summary>
    public static class SuperLancerRingBuilder
    {
        private const string ArtFolder = "Assets/_Project/Art/UI/SuperLancer";
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/SuperLancerRing.prefab";

        private const string TrackTexturePath = ArtFolder + "/ring_track.png";
        private const string ArcTexturePath = ArtFolder + "/ring_arc.png";
        private const string MarkerTexturePath = ArtFolder + "/ring_marker.png";

        private const int CanvasSizePx = 512;
        private const int MarkerTextureWidthPx = 48;
        private const int MarkerTextureHeightPx = 112;
        private const float MarkerEdgeSoftPx = 2f;
        private const float AntiAliasPx = 1.5f;

        [MenuItem("Chez Arthur/Super Lancer/(Re)générer Ring Prefab")]
        public static void Regenerate()
        {
            EnsureFolder(ArtFolder);
            EnsureFolder(PrefabFolder);

            GenerateTextures();
            AssetDatabase.Refresh();

            ConfigureTextureImporter(TrackTexturePath);
            ConfigureTextureImporter(ArcTexturePath);
            ConfigureTextureImporter(MarkerTexturePath);

            Sprite trackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TrackTexturePath);
            Sprite arcSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArcTexturePath);
            Sprite markerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerTexturePath);

            BuildPrefab(trackSprite, arcSprite, markerSprite);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SuperLancerRingBuilder] Textures + prefab régénérés : {PrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }

        // ═══════════════════════════════════════════
        // ÉTAPE A — TEXTURES
        // ═══════════════════════════════════════════

        private static void GenerateTextures()
        {
            WritePng(TrackTexturePath, GenerateRingTexture(CanvasSizePx, outerRadius: 250f, thickness: 14f));
            WritePng(ArcTexturePath, GenerateRingTexture(CanvasSizePx, outerRadius: 252f, thickness: 36f));
            WritePng(MarkerTexturePath, GenerateMarkerTexture(MarkerTextureWidthPx, MarkerTextureHeightPx, MarkerEdgeSoftPx));
        }

        private static byte[] GenerateRingTexture(int size, float outerRadius, float thickness)
        {
            float innerRadius = outerRadius - thickness;
            float center = (size - 1) * 0.5f;
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = ToByte(RingAlpha(dist, innerRadius, outerRadius));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            return EncodePixels(pixels, size, size);
        }

        /// <summary> Encoche radiale : rectangle vertical plein à bords adoucis (~2 px d'anti-aliasing). </summary>
        private static byte[] GenerateMarkerTexture(int width, int height, float edgeSoftPx)
        {
            float centerX = (width - 1) * 0.5f;
            float centerY = (height - 1) * 0.5f;
            float halfW = (width - 1) * 0.5f;
            float halfH = (height - 1) * 0.5f;
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Abs(x - centerX);
                    float dy = Mathf.Abs(y - centerY);

                    float alphaX = 1f - Smoothstep(halfW - edgeSoftPx, halfW + edgeSoftPx * 0.5f, dx);
                    float alphaY = 1f - Smoothstep(halfH - edgeSoftPx, halfH + edgeSoftPx * 0.5f, dy);
                    float alpha = alphaX * alphaY;

                    pixels[y * width + x] = new Color32(255, 255, 255, ToByte(alpha));
                }
            }

            return EncodePixels(pixels, width, height);
        }

        private static float RingAlpha(float dist, float innerRadius, float outerRadius)
        {
            float outer = 1f - Smoothstep(outerRadius - AntiAliasPx, outerRadius + AntiAliasPx, dist);
            float inner = Smoothstep(innerRadius - AntiAliasPx, innerRadius + AntiAliasPx, dist);
            return outer * inner;
        }

        private static float Smoothstep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
        }

        private static byte[] EncodePixels(Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }

        private static void WritePng(string assetPath, byte[] pngBytes)
        {
            string fullPath = Path.GetFullPath(assetPath);
            File.WriteAllBytes(fullPath, pngBytes);
        }

        private static void ConfigureTextureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        // ═══════════════════════════════════════════
        // ÉTAPE B — PREFAB
        // ═══════════════════════════════════════════

        private static void BuildPrefab(Sprite trackSprite, Sprite arcSprite, Sprite markerSprite)
        {
            var root = new GameObject("SuperLancerRing", typeof(RectTransform));
            var rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(CanvasSizePx, CanvasSizePx);

            var view = root.AddComponent<SuperLancerRingView>();
            var canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Canvas world-space
            var canvasGo = NewUI("Canvas", rootRt, out RectTransform canvasRt);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            canvasRt.sizeDelta = new Vector2(CanvasSizePx, CanvasSizePx);
            canvasRt.localScale = Vector3.one;

            // Piste
            var ringBaseGo = NewUI("RingBase", canvasRt, out RectTransform ringBaseRt);
            StretchFull(ringBaseRt);
            var ringBase = ringBaseGo.AddComponent<Image>();
            ringBase.sprite = trackSprite;
            ringBase.color = UiTheme.SuperLancerTrack;
            ringBase.raycastTarget = false;

            // Zone (arc rempli)
            var zoneArcGo = NewUI("ZoneArc", canvasRt, out RectTransform zoneArcRt);
            StretchFull(zoneArcRt);
            var zoneArc = zoneArcGo.AddComponent<Image>();
            zoneArc.sprite = arcSprite;
            zoneArc.color = UiTheme.SuperLancerZone;
            zoneArc.type = Image.Type.Filled;
            zoneArc.fillMethod = Image.FillMethod.Radial360;
            zoneArc.fillOrigin = (int)Image.Origin360.Top;
            zoneArc.fillClockwise = true;
            zoneArc.fillAmount = 0.18f;
            zoneArc.raycastTarget = false;

            // Pivot indicateur
            var indicatorPivotGo = NewUI("IndicatorPivot", canvasRt, out RectTransform indicatorPivotRt);
            indicatorPivotRt.anchorMin = new Vector2(0.5f, 0.5f);
            indicatorPivotRt.anchorMax = new Vector2(0.5f, 0.5f);
            indicatorPivotRt.pivot = new Vector2(0.5f, 0.5f);
            indicatorPivotRt.anchoredPosition = Vector2.zero;
            indicatorPivotRt.sizeDelta = new Vector2(CanvasSizePx, CanvasSizePx);

            var indicatorMarkerGo = NewUI("IndicatorMarker", indicatorPivotRt, out RectTransform markerRt);
            markerRt.anchorMin = new Vector2(0.5f, 0.5f);
            markerRt.anchorMax = new Vector2(0.5f, 0.5f);
            markerRt.pivot = new Vector2(0.5f, 0.5f);
            markerRt.sizeDelta = new Vector2(34f, 96f);
            markerRt.anchoredPosition = new Vector2(0f, 239f);
            var indicatorMarker = indicatorMarkerGo.AddComponent<Image>();
            indicatorMarker.sprite = markerSprite;
            indicatorMarker.color = UiTheme.SuperLancerIndicator;
            indicatorMarker.raycastTarget = false;

            WireView(view, canvasGroup, canvas, ringBase, zoneArc, indicatorPivotRt);

            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void WireView(
            SuperLancerRingView view,
            CanvasGroup canvasGroup,
            Canvas canvas,
            Image ringBase,
            Image zoneArc,
            RectTransform indicatorPivot)
        {
            var so = new SerializedObject(view);
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("canvas").objectReferenceValue = canvas;
            so.FindProperty("ringBase").objectReferenceValue = ringBase;
            so.FindProperty("zoneArc").objectReferenceValue = zoneArc;
            so.FindProperty("indicatorPivot").objectReferenceValue = indicatorPivot;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetFolder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static GameObject NewUI(string name, RectTransform parent, out RectTransform rt)
        {
            var go = new GameObject(name, typeof(RectTransform));
            rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
