#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Preset d'import des sheets portraits SSR/LR et flipbooks génériques
    /// (pixel art, RGBA32 non compressé).
    /// Dossiers :
    /// - Assets/_Project/Art/Resources/CharacterPortraitsSSR/
    /// - Assets/_Project/Art/Resources/Flipbooks/
    /// Ne pas confondre avec le preset SR (CharacterPortraitImportPostprocessor).
    /// </summary>
    public class CharacterPortraitSheetImportPostprocessor : AssetPostprocessor
    {
        internal const string SheetsFolder =
            "Assets/_Project/Art/Resources/CharacterPortraitsSSR/";

        internal const string FlipbooksFolder =
            "Assets/_Project/Art/Resources/Flipbooks/";

        private const int MaxTextureSize = 2048;
        private const FilterMode SheetFilter = FilterMode.Point;
        private const string PlatformAndroid = "Android";
        private const string PlatformIPhone = "iPhone";

        private void OnPreprocessTexture()
        {
            if (!IsCharacterPortraitSheet(assetPath))
                return;
            ApplyCharacterPortraitSheetPreset((TextureImporter)assetImporter);
        }

        internal static bool IsCharacterPortraitSheet(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalized = path.Replace('\\', '/');
            return normalized.StartsWith(SheetsFolder)
                   || normalized.StartsWith(FlipbooksFolder);
        }

        internal static void ApplyCharacterPortraitSheetPreset(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = SheetFilter;
            importer.maxTextureSize = MaxTextureSize;
            importer.npotScale = TextureImporterNPOTScale.None;

            TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
            settings.format = TextureImporterFormat.RGBA32;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(settings);

            importer.ClearPlatformTextureSettings(PlatformAndroid);
            importer.ClearPlatformTextureSettings(PlatformIPhone);
        }

        [MenuItem("Chez Arthur/Art/Forcer preset sheets portraits SSR")]
        private static void ForcePresetOnCharacterPortraitSheetsMenu()
        {
            CharacterIconImportPostprocessor.ReimportFolder(
                SheetsFolder,
                ApplyCharacterPortraitSheetPreset,
                "sheets portraits SSR");

            CharacterIconImportPostprocessor.ReimportFolder(
                FlipbooksFolder,
                ApplyCharacterPortraitSheetPreset,
                "sheets flipbooks");
        }
    }
}
#endif
