#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Applique le preset d'import « sprite combat pixel art » (PNG, fond transparent).
    /// Cible Assets/_Project/Art/Combat/ (Characters, Enemies, Bosses) — pas Auras.
    /// Bosses : maxTextureSize 512. Idle sheets : Multiple + découpe option B + max 2048.
    /// </summary>
    public class CombatSpriteImportPostprocessor : AssetPostprocessor
    {
        // ── Source unique du preset ──
        private const string CombatArtFolder = "Assets/_Project/Art/Combat/";
        private const string CombatAurasFolder = "Assets/_Project/Art/Combat/Auras/";
        private const string BossesPathToken = "/Bosses/";
        private const string EnemiesPathToken = "/Enemies/";
        private const string IdlePathToken = "/Idle/";
        private const int PixelsPerUnit = 256;
        private const int MaxTextureSize = 256;
        private const int MaxBossTextureSize = 512;
        /// <summary> Sheets idle horizontales (larges) — au-dessus du plafond combat standard. </summary>
        private const int MaxIdleTextureSize = 2048;
        private const FilterMode CombatFilter = FilterMode.Point;

        // ═══════════════════════════════════════════
        // AUTO — à chaque import / réimport
        // ═══════════════════════════════════════════
        private void OnPreprocessTexture()
        {
            if (!IsCombatSprite(assetPath))
                return;

            var importer = (TextureImporter)assetImporter;

            if (IsEnemyIdleSheet(assetPath))
            {
                ApplyIdleSheetPreset(importer);
                SliceIdleSheet(importer, assetPath);
                Debug.Log("[CombatSpriteImport] Idle sheet découpée : " + Path.GetFileName(assetPath));
                return;
            }

            int maxTextureSize = GetMaxTextureSizeForPath(assetPath);
            ApplyCombatPreset(importer, maxTextureSize);

            string fileName = Path.GetFileName(assetPath);
            Debug.Log("[CombatSpriteImport] Réglages appliqués (" + maxTextureSize + ") : " + fileName);
        }

        private static bool IsCombatSprite(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith(CombatArtFolder))
                return false;

            if (normalized.StartsWith(CombatAurasFolder))
                return false;

            return true;
        }

        private static bool IsEnemyIdleSheet(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalized = path.Replace('\\', '/');
            return normalized.StartsWith(CombatArtFolder)
                   && normalized.Contains(EnemiesPathToken)
                   && normalized.Contains(IdlePathToken);
        }

        private static int GetMaxTextureSizeForPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return MaxTextureSize;

            string normalized = path.Replace('\\', '/');
            if (IsEnemyIdleSheet(normalized))
                return MaxIdleTextureSize;

            return normalized.Contains(BossesPathToken) ? MaxBossTextureSize : MaxTextureSize;
        }

        // ═══════════════════════════════════════════
        // PRESET (source unique)
        // ═══════════════════════════════════════════
        internal static void ApplyCombatPreset(TextureImporter importer)
        {
            ApplyCombatPreset(importer, MaxTextureSize);
        }

        internal static void ApplyCombatPreset(TextureImporter importer, int maxTextureSize)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // PPU 256 partout : les boss sont plus grands par canvas, pas par densité de pixels.
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = CombatFilter;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = maxTextureSize;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }

        public static void ApplyIdleSheetPreset(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = CombatFilter;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = MaxIdleTextureSize;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }

        /// <summary>
        /// Découpe idle : détecte les îlots de contenu (gouttières noires), N = nb d'îlots,
        /// cellules à largeur égale W/N × H (évite la dérive du packing power-of-2).
        /// Repli : floor(W/H) si aucun îlot détecté.
        /// </summary>
        public static void SliceIdleSheet(TextureImporter importer, string assetPath)
        {
            if (!TryLoadSourcePixels(assetPath, out int width, out int height, out Color32[] pixels))
            {
                Debug.LogError("[CombatSpriteImport] Impossible de lire le PNG source : " + assetPath);
                return;
            }

            if (height <= 0 || width <= 0)
            {
                Debug.LogError("[CombatSpriteImport] Dimensions invalides : " + assetPath);
                return;
            }

            int nUtile = DetectContentFrameCount(pixels, width, height);
            int nBrutSquare = height > 0 ? width / height : 0;
            string mode = "content-runs";

            if (nUtile <= 0)
            {
                // Repli option B historique.
                mode = "floor(W/H)";
                nUtile = nBrutSquare;
                while (nUtile > 1 && IsFrameEmpty(pixels, width, height, nUtile - 1, height, height))
                    nUtile--;
            }

            if (nUtile < 1)
                nUtile = 1;

            string baseName = Path.GetFileNameWithoutExtension(assetPath);
            var rects = new SpriteRect[nUtile];
            var namePairs = new List<SpriteNameFileIdPair>(nUtile);
            for (int i = 0; i < nUtile; i++)
            {
                // Partition entière égale — zéro dérive cumulative (ex. 1024/9 → 113/114).
                int x0 = (i * width) / nUtile;
                int x1 = ((i + 1) * width) / nUtile;
                int fw = x1 - x0;
                GUID spriteId = GUID.Generate();
                string spriteName = baseName + "_" + i;
                rects[i] = new SpriteRect
                {
                    name = spriteName,
                    spriteID = spriteId,
                    rect = new Rect(x0, 0, fw, height),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center
                };
                namePairs.Add(new SpriteNameFileIdPair(spriteName, spriteId));
            }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider =
                factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
            {
                Debug.LogError("[CombatSpriteImport] ISpriteEditorDataProvider indisponible : " + assetPath);
                return;
            }

            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(rects);
            ISpriteNameFileIdDataProvider nameIdProvider =
                dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameIdProvider != null)
                nameIdProvider.SetNameFileIdPairs(namePairs);
            dataProvider.Apply();

            int paddingPx = nBrutSquare > 0 ? width - (nBrutSquare * height) : 0;
            Debug.Log(
                "[CombatSpriteImport] Idle découpe " + baseName +
                " : " + width + "×" + height +
                ", mode=" + mode +
                ", N_utile=" + nUtile +
                ", cell≈" + (width / nUtile) + "×" + height +
                ", padding sheet (" + paddingPx + " px vs square-pack)");
        }

        /// <summary> Chemin disque absolu d'un asset Unity (Assets/…). </summary>
        public static string GetProjectAbsolutePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return assetPath;

            string normalized = assetPath.Replace('\\', '/');
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (normalized.StartsWith("Assets/", StringComparison.Ordinal)
                && !string.IsNullOrEmpty(projectRoot))
            {
                return Path.GetFullPath(
                    Path.Combine(projectRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
            }

            return Path.GetFullPath(assetPath);
        }

        private static bool TryLoadSourcePixels(
            string assetPath,
            out int width,
            out int height,
            out Color32[] pixels)
        {
            width = 0;
            height = 0;
            pixels = null;

            string fullPath = GetProjectAbsolutePath(assetPath);
            if (!File.Exists(fullPath))
                return false;

            byte[] bytes = File.ReadAllBytes(fullPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes, false))
            {
                UnityEngine.Object.DestroyImmediate(tex);
                return false;
            }

            width = tex.width;
            height = tex.height;
            pixels = tex.GetPixels32();
            UnityEngine.Object.DestroyImmediate(tex);
            return pixels != null && pixels.Length == width * height;
        }

        /// <summary>
        /// Compte les îlots de colonnes avec contenu (séparés par gouttières vides/noires).
        /// Ignore le bruit (runs trop étroits) — ex. disciple : 7 vrais frames + micro-îlots 2 px.
        /// </summary>
        private static int DetectContentFrameCount(Color32[] pixels, int width, int height)
        {
            int colThresh = Mathf.Max(2, height / 50);
            // Un vrai personnage occupe une largeur proche de H ; le bruit est << H/2.
            int minRunWidth = Mathf.Max(8, height / 2);
            bool inRun = false;
            int runStart = 0;
            int runs = 0;

            for (int x = 0; x < width; x++)
            {
                int content = 0;
                for (int y = 0; y < height; y++)
                {
                    Color32 p = pixels[y * width + x];
                    if (IsVisibleContentPixel(p))
                        content++;
                }

                bool has = content >= colThresh;
                if (has && !inRun)
                {
                    runStart = x;
                    inRun = true;
                }
                else if (!has && inRun)
                {
                    int runWidth = x - runStart;
                    if (runWidth >= minRunWidth)
                        runs++;
                    inRun = false;
                }
            }

            if (inRun)
            {
                int runWidth = width - runStart;
                if (runWidth >= minRunWidth)
                    runs++;
            }

            return runs;
        }

        private static bool IsVisibleContentPixel(Color32 p)
        {
            if (p.a <= 10)
                return false;
            return p.r >= EmptyBlackRgbThreshold
                   || p.g >= EmptyBlackRgbThreshold
                   || p.b >= EmptyBlackRgbThreshold;
        }

        /// <summary>
        /// Frame sans contenu utile : transparente ou uniquement noir opaque.
        /// </summary>
        private const byte EmptyBlackRgbThreshold = 12;

        private static bool IsFrameEmpty(
            Color32[] pixels,
            int texWidth,
            int texHeight,
            int frameIndex,
            int frameWidth,
            int frameHeight)
        {
            int x0 = frameIndex * frameWidth;
            if (x0 + frameWidth > texWidth || frameHeight > texHeight)
                return true;

            for (int y = 0; y < frameHeight; y++)
            {
                int row = y * texWidth;
                for (int x = 0; x < frameWidth; x++)
                {
                    if (IsVisibleContentPixel(pixels[row + x0 + x]))
                        return false;
                }
            }

            return true;
        }

        [MenuItem("Chez Arthur/Art/Forcer preset sprites combat")]
        private static void ForcePresetOnCombatSpritesMenu()
        {
            string folder = CombatArtFolder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning("[CombatSpriteImport] Dossier introuvable : " + folder);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            int count = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!IsCombatSprite(path))
                        continue;

                    if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                    {
                        if (IsEnemyIdleSheet(path))
                        {
                            ApplyIdleSheetPreset(importer);
                            SliceIdleSheet(importer, path);
                        }
                        else
                        {
                            ApplyCombatPreset(importer, GetMaxTextureSizeForPath(path));
                        }

                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log("[CombatSpriteImport] Preset appliqué à " + count + " texture(s) sous " + CombatArtFolder);
        }
    }
}
#endif
