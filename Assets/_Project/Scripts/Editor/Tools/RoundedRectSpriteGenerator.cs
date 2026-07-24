#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère les sprites UI 9-slice arrondis (S/M/L) pour PanelSurface.
    /// Courbes anti-aliasées — hors règles pixel-art du projet.
    /// </summary>
    public static class RoundedRectSpriteGenerator
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        public const string GeneratedFolder = "Assets/_Project/Art/UI/Generated";
        public const string SpriteSPath = GeneratedFolder + "/RoundedRect_S.png";
        public const string SpriteMPath = GeneratedFolder + "/RoundedRect_M.png";
        public const string SpriteLPath = GeneratedFolder + "/RoundedRect_L.png";

        private const int TextureSize = 64;
        private const float RadiusS = 8f;
        private const float RadiusM = 12f;
        private const float RadiusL = 16f;

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI Kit/Générer les sprites arrondis")]
        public static void GenerateMenu()
        {
            GenerateAll();
            Debug.Log($"[RoundedRectSpriteGenerator] Sprites générés dans `{GeneratedFolder}`.");
        }

        /// <summary>
        /// Génère / réimporte les 3 sprites. Idempotent.
        /// </summary>
        public static void GenerateAll()
        {
            EnsureFolder(GeneratedFolder);
            WriteAndConfigure(SpriteSPath, RadiusS);
            WriteAndConfigure(SpriteMPath, RadiusM);
            WriteAndConfigure(SpriteLPath, RadiusL);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static Sprite LoadSpriteS() => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteSPath);
        public static Sprite LoadSpriteM() => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteMPath);
        public static Sprite LoadSpriteL() => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteLPath);

        // ═══════════════════════════════════════════
        // GÉNÉRATION PNG
        // ═══════════════════════════════════════════

        private static void WriteAndConfigure(string assetPath, float radius)
        {
            Texture2D tex = BuildRoundedRectTexture(TextureSize, radius);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? GeneratedFolder);
            File.WriteAllBytes(fullPath, png);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(assetPath, radius);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// Sprite blanc opaque, coins arrondis AA via SDF (distance signed).
        /// </summary>
        private static Texture2D BuildRoundedRectTexture(int size, float radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float half = size * 0.5f;
            // Boîte qui touche les bords : half-size = half, coins de rayon `radius`.
            Vector2 halfBox = new Vector2(half, half);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Coordonnées centrées
                    Vector2 p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float sdf = SdRoundedBox(p, halfBox, radius);
                    // AA ~1 px : alpha 1 à l'intérieur, fondu sur le bord
                    float alpha = Mathf.Clamp01(0.5f - sdf);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply(false, false);
            return tex;
        }

        /// <summary>
        /// SDF boîte arrondie (Inigo Quilez). Négatif = intérieur.
        /// </summary>
        private static float SdRoundedBox(Vector2 p, Vector2 halfSize, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize + new Vector2(radius, radius);
            float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
            return outside + inside - radius;
        }

        // ═══════════════════════════════════════════
        // IMPORTER
        // ═══════════════════════════════════════════

        private static void ConfigureImporter(string assetPath, float radius)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[RoundedRectSpriteGenerator] Importer introuvable : {assetPath}");
                return;
            }

            float border = radius + 2f;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(border, border, border, border); // L B R T

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
