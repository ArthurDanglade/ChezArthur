#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Applique le preset d'import « icône pixel art » à tout sprite du dossier d'icônes.
    /// Deux modes :
    ///  - AUTO au PREMIER import (n'écrase pas un réglage manuel fait ensuite).
    ///  - MENU « Take Five Games > Art > Forcer preset icônes » pour (re)stamper l'existant
    ///    ou répercuter un changement de preset sur toutes les icônes.
    /// Source unique du preset = les constantes ci-dessous (modifie ici, puis relance le menu).
    /// À placer dans Assets/_Project/Scripts/Editor/.
    /// </summary>
    public class IconImportPostprocessor : AssetPostprocessor
    {
        // ── Source unique du preset ──
        private const string IconsFolder = "Assets/_Project/Sprites/Icon valise & item/";
        private const int AndroidMaxSize = 128;
        private const TextureImporterFormat AndroidFormat = TextureImporterFormat.RGBA32; // non compressé = net ; ASTC_4x4 pour serrer la mémoire
        private const FilterMode IconFilter = FilterMode.Point;                            // pixel art

        // ═══════════════════════════════════════════
        // AUTO — au premier import uniquement
        // ═══════════════════════════════════════════
        private void OnPreprocessTexture()
        {
            if (!IsIcon(assetPath)) return;

            var importer = (TextureImporter)assetImporter;

            // On ne stampe qu'au tout premier import (pas de .meta réglé) → ne contrarie pas un ajustement manuel ultérieur.
            if (!importer.importSettingsMissing) return;

            ApplyIconPreset(importer);
        }

        private static bool IsIcon(string path)
            => !string.IsNullOrEmpty(path) && path.Replace('\\', '/').StartsWith(IconsFolder);

        // ═══════════════════════════════════════════
        // PRESET (source unique, appliqué partout)
        // ═══════════════════════════════════════════
        private static void ApplyIconPreset(TextureImporter importer)
        {
            importer.textureType        = TextureImporterType.Sprite;
            importer.spriteImportMode   = SpriteImportMode.Single;
            importer.sRGBTexture        = true;
            importer.alphaIsTransparency = true;
            importer.isReadable         = false;
            importer.mipmapEnabled      = false;
            importer.wrapMode           = TextureWrapMode.Clamp;
            importer.filterMode         = IconFilter;

            // Mesh type → Full Rect (via TextureImporterSettings)
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            // Override plateforme Android
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden     = true;
            android.maxTextureSize = AndroidMaxSize;
            android.format         = AndroidFormat;
            importer.SetPlatformTextureSettings(android);
        }

        // ═══════════════════════════════════════════
        // MENU — (re)stamper l'existant / après changement de preset
        // ═══════════════════════════════════════════
        [MenuItem("Take Five Games/Art/Forcer preset icônes")]
        private static void ForcePresetOnIcons()
        {
            string folder = IconsFolder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog("Dossier introuvable",
                    $"Le dossier '{folder}' n'existe pas. Crée-le ou ajuste la constante IconsFolder.", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            int count = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                    {
                        ApplyIconPreset(importer);
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[Icons] Preset appliqué à {count} icône(s) dans {IconsFolder}.");
        }
    }
}
#endif
