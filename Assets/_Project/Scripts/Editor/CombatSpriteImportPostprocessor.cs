#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Applique le preset d'import « sprite combat pixel art » (PNG 256×256, fond transparent).
    /// Cible uniquement Assets/_Project/Art/Combat/ — réglages distincts du preset UI.
    /// À placer dans Assets/_Project/Scripts/Editor/.
    /// </summary>
    public class CombatSpriteImportPostprocessor : AssetPostprocessor
    {
        // ── Source unique du preset ──
        private const string CombatArtFolder = "Assets/_Project/Art/Combat/";
        private const int PixelsPerUnit = 256;
        private const int MaxTextureSize = 256;
        private const FilterMode CombatFilter = FilterMode.Point;

        // ═══════════════════════════════════════════
        // AUTO — à chaque import / réimport
        // ═══════════════════════════════════════════
        private void OnPreprocessTexture()
        {
            if (!IsCombatSprite(assetPath)) return;

            var importer = (TextureImporter)assetImporter;
            ApplyCombatPreset(importer);

            string fileName = Path.GetFileName(assetPath);
            Debug.Log("[CombatSpriteImport] Réglages appliqués : " + fileName);
        }

        private static bool IsCombatSprite(string path)
            => !string.IsNullOrEmpty(path) && path.Replace('\\', '/').StartsWith(CombatArtFolder);

        // ═══════════════════════════════════════════
        // PRESET (source unique)
        // ═══════════════════════════════════════════
        private static void ApplyCombatPreset(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = CombatFilter;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = MaxTextureSize;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }
    }
}
#endif
