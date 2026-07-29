#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Corrige en lot les textures pixel art pour Android :
    /// Filter Point, Compression None, Format RGBA32, MipMaps off.
    /// Exclut les FX soft (glow, vignette, bloom…) qui restent en Bilinear.
    /// </summary>
    public static class PixelArtAndroidBatchFixer
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string MenuRoot = "Chez Arthur/Art/";
        private const string PlatformAndroid = "Android";

        private static readonly string[] ScanRoots =
        {
            "Assets/_Project/Sprites",
            "Assets/_Project/Art",
            "Assets/_Project/Data/Portraits",
            "Assets/_Project/Data/Characters"
        };

        /// <summary> Dossiers FX soft exclus entièrement. </summary>
        private static readonly string[] SoftFxFolders =
        {
            "Assets/_Project/Art/FX/",
            "Assets/_Project/Art/Effects/"
        };

        /// <summary> Tokens de nom exclus (glow, vignette…). </summary>
        private static readonly string[] SoftFxNameTokens =
        {
            "glow",
            "vignette",
            "bloom",
            "mote",
            "rays",
            "beam_light",
            "dust_mote"
        };

        // ═══════════════════════════════════════════
        // MENUS
        // ═══════════════════════════════════════════

        [MenuItem(MenuRoot + "Android — Audit pixel art (DRY RUN)")]
        private static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem(MenuRoot + "Android — Forcer pixel art (Point/RGBA32)")]
        private static void ApplyFix()
        {
            if (!EditorUtility.DisplayDialog(
                    "Pixel art Android",
                    "Appliquer Point + RGBA32 + Compression None + MipMaps off\n" +
                    "sur les textures non conformes (FX soft exclus) ?",
                    "Appliquer",
                    "Annuler"))
            {
                return;
            }

            Run(apply: true);
        }

        // ═══════════════════════════════════════════
        // LOGIQUE
        // ═══════════════════════════════════════════

        private static void Run(bool apply)
        {
            List<string> targets = CollectNonCompliantTextures();
            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Pixel art Android",
                    "Aucune texture non conforme trouvée dans les dossiers scannés.",
                    "OK");
                return;
            }

            var log = new StringBuilder(2048);
            log.AppendLine(apply
                ? $"[PixelArtAndroid] Correction de {targets.Count} texture(s)…"
                : $"[PixelArtAndroid] DRY RUN — {targets.Count} texture(s) non conforme(s) :");

            int fixedCount = 0;

            try
            {
                if (apply)
                    AssetDatabase.StartAssetEditing();

                for (int i = 0; i < targets.Count; i++)
                {
                    string path = targets[i];
                    if (apply)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Pixel art Android",
                            path,
                            (float)i / targets.Count);

                        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                        {
                            ApplyPixelArtAndroidSettings(importer);
                            importer.SaveAndReimport();
                            fixedCount++;
                        }
                    }

                    log.AppendLine(" - " + path);
                }
            }
            finally
            {
                if (apply)
                {
                    AssetDatabase.StopAssetEditing();
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.Refresh();
                }
            }

            Debug.Log(log.ToString());

            if (apply)
            {
                EditorUtility.DisplayDialog(
                    "Pixel art Android",
                    $"Terminé. {fixedCount} texture(s) corrigée(s).\nVoir la Console pour le détail.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Pixel art Android — DRY RUN",
                    $"{targets.Count} texture(s) non conforme(s).\nListe complète dans la Console.",
                    "OK");
            }
        }

        private static List<string> CollectNonCompliantTextures()
        {
            var result = new List<string>(256);

            for (int r = 0; r < ScanRoots.Length; r++)
            {
                string root = ScanRoots[r];
                if (!AssetDatabase.IsValidFolder(root))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (ShouldSkipSoftFx(path))
                        continue;

                    if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                        continue;

                    if (IsNonCompliant(importer))
                        result.Add(path);
                }
            }

            return result;
        }

        private static bool ShouldSkipSoftFx(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            string normalized = path.Replace('\\', '/');

            for (int i = 0; i < SoftFxFolders.Length; i++)
            {
                if (normalized.StartsWith(SoftFxFolders[i]))
                    return true;
            }

            string fileName = System.IO.Path.GetFileNameWithoutExtension(normalized).ToLowerInvariant();
            for (int i = 0; i < SoftFxNameTokens.Length; i++)
            {
                if (fileName.Contains(SoftFxNameTokens[i]))
                    return true;
            }

            return false;
        }

        private static bool IsNonCompliant(TextureImporter importer)
        {
            if (importer.filterMode != FilterMode.Point)
                return true;
            if (importer.mipmapEnabled)
                return true;

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings(PlatformAndroid);
            if (android.overridden)
            {
                if (android.format != TextureImporterFormat.RGBA32)
                    return true;
                if (android.textureCompression != TextureImporterCompression.Uncompressed)
                    return true;
                return false;
            }

            TextureImporterPlatformSettings def = importer.GetDefaultPlatformTextureSettings();
            if (def.textureCompression != TextureImporterCompression.Uncompressed)
                return true;
            if (def.format != TextureImporterFormat.Automatic && def.format != TextureImporterFormat.RGBA32)
                return true;

            // Sans override Android, le switch peut réappliquer une compression → on force l'override.
            return true;
        }

        /// <summary>
        /// Applique la règle maison pixel art (ne touche pas au type Sprite/Default ni au maxSize existant).
        /// </summary>
        internal static void ApplyPixelArtAndroidSettings(TextureImporter importer)
        {
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;

            TextureImporterPlatformSettings def = importer.GetDefaultPlatformTextureSettings();
            int maxSize = def.maxTextureSize > 0 ? def.maxTextureSize : 2048;
            def.format = TextureImporterFormat.RGBA32;
            def.textureCompression = TextureImporterCompression.Uncompressed;
            def.maxTextureSize = maxSize;
            importer.SetPlatformTextureSettings(def);

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings(PlatformAndroid);
            android.overridden = true;
            android.format = TextureImporterFormat.RGBA32;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            android.maxTextureSize = maxSize;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
#endif
