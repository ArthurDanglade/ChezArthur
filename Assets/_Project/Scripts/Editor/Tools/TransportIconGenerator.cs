#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère les icônes transport du lecteur lofi (prev / play / pause / next).
    /// Blanc sur alpha, formes géométriques AA — intérim jusqu'aux assets Dharu.
    /// </summary>
    public static class TransportIconGenerator
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        public const string GeneratedFolder = "Assets/_Project/Art/UI/Generated";
        public const string PrevPath = GeneratedFolder + "/icon_prev.png";
        public const string PlayPath = GeneratedFolder + "/icon_play.png";
        public const string PausePath = GeneratedFolder + "/icon_pause.png";
        public const string NextPath = GeneratedFolder + "/icon_next.png";

        private const int Size = 64;
        private const int Margin = 12;

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI Kit/Générer les icônes transport")]
        public static void GenerateMenu()
        {
            GenerateAll();
            Debug.Log(
                $"[TransportIconGenerator] icon_prev/play/pause/next → `{GeneratedFolder}`.\n" +
                "TODO Dharu : versions pixel-art natives (nav, étage, shop).");
        }

        /// <summary> Génère / réimporte les 4 icônes. Idempotent. </summary>
        public static void GenerateAll()
        {
            EnsureFolder(GeneratedFolder);
            WriteAndConfigure(PrevPath, BuildPrev);
            WriteAndConfigure(PlayPath, BuildPlay);
            WriteAndConfigure(PausePath, BuildPause);
            WriteAndConfigure(NextPath, BuildNext);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static Sprite LoadPrev() => LoadOrGenerate(PrevPath, BuildPrev);
        public static Sprite LoadPlay() => LoadOrGenerate(PlayPath, BuildPlay);
        public static Sprite LoadPause() => LoadOrGenerate(PausePath, BuildPause);
        public static Sprite LoadNext() => LoadOrGenerate(NextPath, BuildNext);

        /// <summary> Assure les 4 assets puis les charge (builder). </summary>
        public static void EnsureLoaded(
            out Sprite prev,
            out Sprite play,
            out Sprite pause,
            out Sprite next)
        {
            prev = LoadPrev();
            play = LoadPlay();
            pause = LoadPause();
            next = LoadNext();
        }

        // ═══════════════════════════════════════════
        // GÉNÉRATION
        // ═══════════════════════════════════════════

        private delegate void ShapePainter(Color[] pixels);

        private static Sprite LoadOrGenerate(string path, ShapePainter paint)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            WriteAndConfigure(path, paint);
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void WriteAndConfigure(string assetPath, ShapePainter paint)
        {
            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(1f, 1f, 1f, 0f);

            paint(pixels);
            tex.SetPixels(pixels);
            tex.Apply(false, false);

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? GeneratedFolder);
            File.WriteAllBytes(fullPath, png);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void BuildPlay(Color[] px)
        {
            // Triangle pointant à droite, marge 12.
            float x0 = Margin + 4f;
            float x1 = Size - Margin - 2f;
            float y0 = Margin;
            float y1 = Size - Margin;
            float cy = Size * 0.5f;
            FillTriangle(px, new Vector2(x0, y0), new Vector2(x0, y1), new Vector2(x1, cy));
        }

        private static void BuildPause(Color[] px)
        {
            float barW = 10f;
            float gap = 8f;
            float y0 = Margin;
            float y1 = Size - Margin;
            float cx = Size * 0.5f;
            FillRect(px, cx - gap * 0.5f - barW, y0, barW, y1 - y0);
            FillRect(px, cx + gap * 0.5f, y0, barW, y1 - y0);
        }

        private static void BuildPrev(Color[] px)
        {
            // Barre gauche + double chevron (triangle) vers la gauche.
            float barW = 6f;
            float xBar = Margin;
            FillRect(px, xBar, Margin, barW, Size - Margin * 2);

            float tip = Margin + barW + 2f;
            float baseX = Size - Margin;
            float cy = Size * 0.5f;
            // Deux triangles empilés horizontalement (double chevron).
            float mid = tip + (baseX - tip) * 0.52f;
            FillTriangle(px, new Vector2(baseX, Margin), new Vector2(baseX, Size - Margin), new Vector2(mid, cy));
            FillTriangle(px, new Vector2(mid + 2f, Margin), new Vector2(mid + 2f, Size - Margin), new Vector2(tip, cy));
        }

        private static void BuildNext(Color[] px)
        {
            // Miroir de prev : double chevron droite + barre droite.
            float barW = 6f;
            float xBar = Size - Margin - barW;
            FillRect(px, xBar, Margin, barW, Size - Margin * 2);

            float tip = xBar - 2f;
            float baseX = Margin;
            float cy = Size * 0.5f;
            float mid = tip - (tip - baseX) * 0.52f;
            FillTriangle(px, new Vector2(baseX, Margin), new Vector2(baseX, Size - Margin), new Vector2(mid, cy));
            FillTriangle(px, new Vector2(mid - 2f, Margin), new Vector2(mid - 2f, Size - Margin), new Vector2(tip, cy));
        }

        // ═══════════════════════════════════════════
        // RASTER AA
        // ═══════════════════════════════════════════

        private static void FillRect(Color[] px, float x, float y, float w, float h)
        {
            for (int py = 0; py < Size; py++)
            {
                for (int px_ = 0; px_ < Size; px_++)
                {
                    float cx = px_ + 0.5f;
                    float cy = py + 0.5f;
                    float dx = Mathf.Max(x - cx, cx - (x + w));
                    float dy = Mathf.Max(y - cy, cy - (y + h));
                    float d = Mathf.Max(dx, dy);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f)
                        continue;
                    int i = py * Size + px_;
                    px[i] = Blend(px[i], a);
                }
            }
        }

        private static void FillTriangle(Color[] px, Vector2 a, Vector2 b, Vector2 c)
        {
            for (int py = 0; py < Size; py++)
            {
                for (int px_ = 0; px_ < Size; px_++)
                {
                    Vector2 p = new Vector2(px_ + 0.5f, py + 0.5f);
                    float d = SdTriangle(p, a, b, c);
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (alpha <= 0f)
                        continue;
                    int i = py * Size + px_;
                    px[i] = Blend(px[i], alpha);
                }
            }
        }

        private static Color Blend(Color existing, float alpha)
        {
            float a = Mathf.Max(existing.a, alpha);
            return new Color(1f, 1f, 1f, a);
        }

        /// <summary> SDF triangle (Inigo Quilez, signe intérieur négatif). </summary>
        private static float SdTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 e0 = b - a;
            Vector2 e1 = c - b;
            Vector2 e2 = a - c;
            Vector2 v0 = p - a;
            Vector2 v1 = p - b;
            Vector2 v2 = p - c;

            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));

            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d = Min2(
                new Vector2(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                new Vector2(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x)));
            d = Min2(d, new Vector2(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }

        private static Vector2 Min2(Vector2 a, Vector2 b)
        {
            return new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
        }

        // ═══════════════════════════════════════════
        // IMPORTER
        // ═══════════════════════════════════════════

        private static void ConfigureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[TransportIconGenerator] Importer introuvable : {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            platform.format = TextureImporterFormat.RGBA32;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            platform.maxTextureSize = 64;
            importer.SetPlatformTextureSettings(platform);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
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
