#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Preset d'import des portraits Resources (ref. portraitkramhoisi).
    /// Dossier : Assets/_Project/Art/Resources/CharacterPortraits/
    /// Texture Default + mipmaps : compatible PortraitLoader (Resources.Load Texture2D).
    /// </summary>
    public class CharacterPortraitImportPostprocessor : AssetPostprocessor
    {
        internal const string PortraitsFolder = "Assets/_Project/Art/Resources/CharacterPortraits/";
        private const int MaxTextureSize = 4096;
        private const FilterMode PortraitFilter = FilterMode.Bilinear;

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
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = PortraitFilter;
            importer.maxTextureSize = MaxTextureSize;
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
