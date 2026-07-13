#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Preset d'import des icônes personnage (pixel art UI, ref. icon_kram_hoisi).
    /// Dossier : Assets/_Project/Art/CharacterIcons/
    /// </summary>
    public class CharacterIconImportPostprocessor : AssetPostprocessor
    {
        internal const string IconsFolder = "Assets/_Project/Art/CharacterIcons/";
        private const int MaxTextureSize = 512;
        private const int PixelsPerUnit = 100;
        private const FilterMode IconFilter = FilterMode.Point;

        private void OnPreprocessTexture()
        {
            if (!IsCharacterIcon(assetPath)) return;
            ApplyCharacterIconPreset((TextureImporter)assetImporter);
        }

        internal static bool IsCharacterIcon(string path)
            => !string.IsNullOrEmpty(path) && path.Replace('\\', '/').StartsWith(IconsFolder);

        internal static void ApplyCharacterIconPreset(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = IconFilter;
            importer.maxTextureSize = MaxTextureSize;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteGenerateFallbackPhysicsShape = true;
            importer.SetTextureSettings(settings);
        }

        [MenuItem("Chez Arthur/Art/Forcer preset icônes personnages")]
        private static void ForcePresetOnCharacterIconsMenu()
        {
            ReimportFolder(IconsFolder, ApplyCharacterIconPreset, "icônes personnages");
        }

        internal static int ReimportFolder(string folder, System.Action<TextureImporter> applyPreset, string label)
        {
            string trimmed = folder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(trimmed))
            {
                Debug.LogWarning($"[CharacterVisual] Dossier introuvable : {trimmed}");
                return 0;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { trimmed });
            int count = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                    {
                        applyPreset(importer);
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[CharacterVisual] Preset {label} appliqué à {count} texture(s).");
            return count;
        }
    }
}
#endif
