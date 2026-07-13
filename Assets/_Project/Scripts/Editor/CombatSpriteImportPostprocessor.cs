#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Applique le preset d'import « sprite combat pixel art » (PNG, fond transparent).
    /// Cible Assets/_Project/Art/Combat/ (Characters, Enemies, Bosses) — pas Auras.
    /// Bosses : maxTextureSize 512 (canvas plus grand, PPU inchangé à 256).
    /// À placer dans Assets/_Project/Scripts/Editor/.
    /// </summary>
    public class CombatSpriteImportPostprocessor : AssetPostprocessor
    {
        // ── Source unique du preset ──
        private const string CombatArtFolder = "Assets/_Project/Art/Combat/";
        private const string CombatAurasFolder = "Assets/_Project/Art/Combat/Auras/";
        private const string BossesPathToken = "/Bosses/";
        private const int PixelsPerUnit = 256;
        private const int MaxTextureSize = 256;
        private const int MaxBossTextureSize = 512;
        private const FilterMode CombatFilter = FilterMode.Point;

        // ═══════════════════════════════════════════
        // AUTO — à chaque import / réimport
        // ═══════════════════════════════════════════
        private void OnPreprocessTexture()
        {
            if (!IsCombatSprite(assetPath)) return;

            var importer = (TextureImporter)assetImporter;
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

        private static int GetMaxTextureSizeForPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return MaxTextureSize;

            return path.Replace('\\', '/').Contains(BossesPathToken) ? MaxBossTextureSize : MaxTextureSize;
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
                        ApplyCombatPreset(importer, GetMaxTextureSizeForPath(path));
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
