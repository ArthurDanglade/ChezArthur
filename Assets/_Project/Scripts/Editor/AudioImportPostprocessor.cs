#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Le postprocessor fait foi sur les réglages d'import audio du projet —
    /// ne pas régler à la main dans l'Inspector.
    /// </summary>
    public class AudioImportPostprocessor : AssetPostprocessor
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string AudioRoot = "Assets/_Project/Audio/";
        private const long SfxDecompressThresholdBytes = 200L * 1024L;

        // ═══════════════════════════════════════════
        // UNITY CALLBACK
        // ═══════════════════════════════════════════

        private void OnPreprocessAudio()
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(AudioRoot))
                return;

            AudioImporter importer = assetImporter as AudioImporter;
            if (importer == null)
                return;

            string normalized = assetPath.Replace('\\', '/');

            if (normalized.Contains("/SFX/"))
            {
                ApplySfxSettings(importer, normalized);
                return;
            }

            if (normalized.Contains("/Music/") || normalized.Contains("/Ambiance/"))
            {
                ApplyStreamingSettings(importer);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private static void ApplySfxSettings(AudioImporter importer, string path)
        {
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.ambisonic = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;

            long sizeBytes = 0L;
            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                sizeBytes = new FileInfo(fullPath).Length;

            settings.loadType = sizeBytes > 0L && sizeBytes < SfxDecompressThresholdBytes
                ? AudioClipLoadType.DecompressOnLoad
                : AudioClipLoadType.CompressedInMemory;

            // Si le fichier n'existe pas encore (premier import), privilégier DecompressOnLoad
            // pour les SFX courts — le seuil se corrige au réimport suivant.
            if (sizeBytes <= 0L)
                settings.loadType = AudioClipLoadType.DecompressOnLoad;

            importer.defaultSampleSettings = settings;
        }

        private static void ApplyStreamingSettings(AudioImporter importer)
        {
            importer.forceToMono = false;
            importer.loadInBackground = false;
            importer.ambisonic = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.65f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.loadType = AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = settings;
        }
    }
}
#endif
