#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Applique le preset d'import « sprite UI fonctionnel » (panels, boutons Nine-Slice).
    /// Deux modes :
    ///  - AUTO au PREMIER import (n'écrase pas un réglage manuel fait ensuite).
    ///  - MENU « Take Five Games > Art > Forcer preset UI » pour (re)stamper l'existant
    ///    ou répercuter un changement de preset sur tous les sprites UI.
    /// Source unique du preset = <see cref="ApplyUIPreset"/> (modifie ici, puis relance le menu).
    /// Séparé de <see cref="IconImportPostprocessor"/>.
    /// À placer dans Assets/_Project/Scripts/Editor/.
    /// </summary>
    public class UIImportPostprocessor : AssetPostprocessor
    {
        // ── Source unique du preset ──
        private const string UIFolder = "Assets/_Project/Sprites/UI/";
        private const int MaxTextureSize = 256; // nos sprites font 214px max — ne jamais downscale
        private const FilterMode UIFilter = FilterMode.Point;

        // ═══════════════════════════════════════════
        // AUTO — au premier import uniquement
        // ═══════════════════════════════════════════
        private void OnPreprocessTexture()
        {
            if (!IsUISprite(assetPath)) return;

            var importer = (TextureImporter)assetImporter;

            // Ne stampe qu'au tout premier import → ne réécrase JAMAIS un réglage manuel
            // ultérieur (notamment les borders Nine-Slice posées à la main en Sprite Editor).
            if (!importer.importSettingsMissing) return;

            ApplyUIPreset(importer);
        }

        private static bool IsUISprite(string path)
            => !string.IsNullOrEmpty(path) && path.Replace('\\', '/').StartsWith(UIFolder);

        // ═══════════════════════════════════════════
        // PRESET (source unique, appliqué partout)
        // ═══════════════════════════════════════════
        private static void ApplyUIPreset(TextureImporter importer)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.sRGBTexture         = true;
            importer.alphaIsTransparency = true; // bordures nettes sur fond transparent
            importer.isReadable          = false;
            importer.mipmapEnabled       = false;
            importer.wrapMode            = TextureWrapMode.Clamp;
            importer.filterMode          = UIFilter;
            importer.textureCompression  = TextureImporterCompression.Uncompressed; // RGBA32, UI net
            importer.maxTextureSize      = MaxTextureSize;

            // Full Rect obligatoire pour le Nine-Slice (mesh via TextureImporterSettings).
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            // NE JAMAIS écrire spriteBorder ni toucher aux borders Nine-Slice :
            // elles sont réglées manuellement dans le Sprite Editor et seraient
            // écrasées silencieusement à chaque réimport si le postprocessor les modifiait.
            //
            // NE PAS traiter les assets hors de Assets/_Project/Sprites/UI/ (filtré dans OnPreprocessTexture).
        }

        // ═══════════════════════════════════════════
        // MENU — (re)stamper l'existant / après changement de preset
        // ═══════════════════════════════════════════
        [MenuItem("Take Five Games/Art/Forcer preset UI")]
        private static void ForcePresetOnUI()
        {
            string folder = UIFolder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog("Dossier introuvable",
                    $"Le dossier '{folder}' n'existe pas. Crée-le ou ajuste la constante UIFolder.", "OK");
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
                        ApplyUIPreset(importer);
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

            Debug.Log($"[UI] Preset appliqué à {count} sprite(s) dans {UIFolder}.");
        }
    }
}
#endif
