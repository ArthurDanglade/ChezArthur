#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Applique les import settings audio Artwork (charte AW4 §5) — idempotent.
    /// </summary>
    public static class ArtworkAudioImportBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string ArtworkFolder = "Assets/_Project/Audio/SFX/Artwork";
        private const string BurnPath = "Assets/_Project/Audio/SFX/Gacha/sfx_gacha_burn.wav";
        private const string ReportRelPath = "Audits/artwork_audio_import.txt";
        private const float VorbisQuality = 0.7f;

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI/Configurer imports audio Artwork (AW4)")]
        public static void BuildMenu()
        {
            Build();
        }

        /// <summary>
        /// Point d'entrée idempotent (MenuItem + appel scripté).
        /// </summary>
        public static void Build()
        {
            var report = new StringBuilder(4096);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" IMPORT AUDIO Artwork AW4 (charte §5)");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();

            int changed = 0;
            int unchanged = 0;
            int missing = 0;

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { ArtworkFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ApplyOne(path, report, ref changed, ref unchanged, ref missing);
            }

            ApplyOne(BurnPath, report, ref changed, ref unchanged, ref missing);

            report.AppendLine();
            report.AppendLine("───────────────────────────────────────────");
            report.AppendLine($" SYNTHÈSE : changed={changed}  unchanged={unchanged}  missing={missing}");
            report.AppendLine("───────────────────────────────────────────");
            report.AppendLine(" Fin du rapport");
            report.AppendLine("═══════════════════════════════════════════");

            string text = report.ToString();
            Debug.Log(text);
            WriteReport(text);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ═══════════════════════════════════════════
        // APPLICATION
        // ═══════════════════════════════════════════

        private static void ApplyOne(
            string assetPath,
            StringBuilder report,
            ref int changed,
            ref int unchanged,
            ref int missing)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(ToFullPath(assetPath)))
            {
                missing++;
                report.AppendLine($"  ❌ Manquant : {assetPath}");
                return;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
            {
                missing++;
                report.AppendLine($"  ❌ Pas un AudioImporter : {assetPath}");
                return;
            }

            bool decompress = WantsDecompressOnLoad(assetPath);
            AudioClipLoadType loadType = decompress
                ? AudioClipLoadType.DecompressOnLoad
                : AudioClipLoadType.CompressedInMemory;

            if (NeedsUpdate(importer, loadType))
            {
                ApplySettings(importer, loadType);
                importer.SaveAndReimport();
                // Re-applique après OnPreprocessAudio (taille) qui peut écraser.
                importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (importer != null && NeedsUpdate(importer, loadType))
                {
                    ApplySettings(importer, loadType);
                    // Évite une boucle SaveAndReimport : WriteImportSettingsIfDirty suffit
                    // si le postprocessor Artwork a déjà tourné (callbackOrder > SFX générique).
                    EditorUtility.SetDirty(importer);
                    AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                }

                changed++;
                report.AppendLine(
                    $"  ✅ UPDATED {assetPath} → loadType={loadType}, " +
                    $"forceToMono=true, Vorbis q={VorbisQuality:0.##}, preload=true");
            }
            else
            {
                unchanged++;
                report.AppendLine(
                    $"  · OK {assetPath} (loadType={loadType}, mono, Vorbis q{VorbisQuality:0.##}, preload)");
            }
        }

        /// <summary>
        /// One-shots courts → DecompressOnLoad ; boucles + riser → CompressedInMemory.
        /// </summary>
        public static bool WantsDecompressOnLoad(string assetPath)
        {
            string name = Path.GetFileName(assetPath).ToLowerInvariant();
            if (name.Contains("shimmer") || name.Contains("crackle")
                || name.Contains("reforge") || name.Contains("riser"))
                return false;

            // whoosh, pulse, climax, burn (sting/ignite), autres one-shots
            return true;
        }

        public static void ApplySettings(AudioImporter importer, AudioClipLoadType loadType)
        {
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.ambisonic = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = loadType;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = VorbisQuality;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
        }

        public static bool NeedsUpdate(AudioImporter importer, AudioClipLoadType loadType)
        {
            if (!importer.forceToMono)
                return true;
            if (importer.loadInBackground)
                return true;
            if (importer.ambisonic)
                return true;

            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            if (s.loadType != loadType)
                return true;
            if (s.compressionFormat != AudioCompressionFormat.Vorbis)
                return true;
            if (Mathf.Abs(s.quality - VorbisQuality) > 0.01f)
                return true;
            if (!s.preloadAudioData)
                return true;

            return false;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static string ToFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static void WriteReport(string text)
        {
            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, "artwork_audio_import.txt");
            File.WriteAllText(fullPath, text, Encoding.UTF8);
            Debug.Log($"[ArtworkAudioImportBuilder] Rapport : {fullPath}");
        }
    }

    /// <summary>
    /// Sur-réimport Artwork / burn : force la charte AW4 après le postprocessor SFX générique.
    /// </summary>
    public class ArtworkAudioImportPostprocessor : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 100;

        private void OnPreprocessAudio()
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            string normalized = assetPath.Replace('\\', '/');
            bool artwork = normalized.StartsWith("Assets/_Project/Audio/SFX/Artwork/");
            bool burn = normalized.Equals(
                "Assets/_Project/Audio/SFX/Gacha/sfx_gacha_burn.wav",
                System.StringComparison.OrdinalIgnoreCase);
            if (!artwork && !burn)
                return;

            AudioImporter importer = assetImporter as AudioImporter;
            if (importer == null)
                return;

            bool decompress = ArtworkAudioImportBuilder.WantsDecompressOnLoad(normalized);
            AudioClipLoadType loadType = decompress
                ? AudioClipLoadType.DecompressOnLoad
                : AudioClipLoadType.CompressedInMemory;
            ArtworkAudioImportBuilder.ApplySettings(importer, loadType);
        }
    }
}
#endif
