#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Preset d'import des spritesheets d'aura (GIF découpé en Multiple sprites).
    /// Dossier : Assets/_Project/Art/Combat/Auras/
    /// </summary>
    public class AuraSpriteImportPostprocessor : AssetPostprocessor
    {
        internal const string AurasFolder = "Assets/_Project/Art/Combat/Auras/";
        private const int MaxTextureSize = 512;
        private const int PixelsPerUnit = 256;
        private const FilterMode AuraFilter = FilterMode.Point;

        private void OnPreprocessTexture()
        {
            if (!IsAuraSprite(assetPath))
                return;

            ApplyAuraPreset((TextureImporter)assetImporter);
        }

        internal static bool IsAuraSprite(string path)
            => !string.IsNullOrEmpty(path) && path.Replace('\\', '/').StartsWith(AurasFolder);

        internal static void ApplyAuraPreset(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = AuraFilter;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = MaxTextureSize;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }

        [MenuItem("Chez Arthur/Art/Forcer preset auras combat")]
        private static void ForcePresetOnAuras()
        {
            CharacterIconImportPostprocessor.ReimportFolder(
                AurasFolder.TrimEnd('/'),
                ApplyAuraPreset,
                "auras combat");
        }
    }
}
#endif
