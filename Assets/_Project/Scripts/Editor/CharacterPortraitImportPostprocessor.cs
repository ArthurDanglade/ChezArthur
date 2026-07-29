#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Preset d'import des portraits Resources (pixel art).
    /// Dossier : Assets/_Project/Art/Resources/CharacterPortraits/
    /// Texture Default : compatible PortraitLoader (Resources.Load Texture2D).
    /// Point + RGBA32 + sans mipmaps (règle maison Android).
    /// </summary>
    public class CharacterPortraitImportPostprocessor : AssetPostprocessor
    {
        internal const string PortraitsFolder = "Assets/_Project/Art/Resources/CharacterPortraits/";
        private const int MaxTextureSize = 4096;
        private const FilterMode PortraitFilter = FilterMode.Point;
        private const string PlatformAndroid = "Android";

        private void OnPreprocessTexture()
        {
            if (!IsCharacterPortrait(assetPath)) return;
            ApplyCharacterPortraitPreset((TextureImporter)assetImporter);
        }

        internal static bool IsCharacterPortrait(string path)
            => !string.IsNullOrEmpty(path) && path.Replace('\\', '/').StartsWith(PortraitsFolder);

        internal static void ApplyCharacterPortraitPreset(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = PortraitFilter;
            importer.maxTextureSize = MaxTextureSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;

            TextureImporterPlatformSettings def = importer.GetDefaultPlatformTextureSettings();
            def.maxTextureSize = MaxTextureSize;
            def.format = TextureImporterFormat.RGBA32;
            def.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(def);

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings(PlatformAndroid);
            android.overridden = true;
            android.maxTextureSize = MaxTextureSize;
            android.format = TextureImporterFormat.RGBA32;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(android);
        }

        [MenuItem("Chez Arthur/Art/Forcer preset portraits personnages")]
        private static void ForcePresetOnCharacterPortraitsMenu()
        {
            CharacterIconImportPostprocessor.ReimportFolder(
                PortraitsFolder,
                ApplyCharacterPortraitPreset,
                "portraits personnages");
        }
    }
}
#endif
