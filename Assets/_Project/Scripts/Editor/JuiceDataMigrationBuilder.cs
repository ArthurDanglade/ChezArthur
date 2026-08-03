#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Feedback;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Migration data groupe A JuiceDirector → FeedbackCatalog (F2-P2b).
    /// Copie uniquement — n'invente ni ne corrige. Zéro écriture scène sur Migrer.
    /// </summary>
    public static class JuiceDataMigrationBuilder
    {
        private const string CatalogPath = "Assets/_Project/Data/Feedback/FeedbackCatalog.asset";

        private struct MirrorMeta
        {
            public FeedbackBundle.VoiceFamily Family;
            public int Emphasis;
            public bool CopyVolume;
        }

        [MenuItem("Chez Arthur/Feedback/Migrer JuiceDirector vers Catalogue")]
        public static void MigrateJuiceDirectorToCatalog()
        {
            JuiceDirector juice = UnityEngine.Object.FindObjectOfType<JuiceDirector>(true);
            if (juice == null)
            {
                Debug.LogError("[JuiceDataMigration] Aucun JuiceDirector dans la scène active.");
                return;
            }

            FeedbackCatalog catalog = AssetDatabase.LoadAssetAtPath<FeedbackCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[JuiceDataMigration] Catalogue introuvable : {CatalogPath}");
                return;
            }

            SerializedObject so = new SerializedObject(juice);
            AudioClip[] hitClips = ReadClipArray(so, "_hitClips");
            AudioClip critClip = ReadClip(so, "_critClip");
            AudioClip[] wallBounceClips = ReadClipArray(so, "_wallBounceClips");
            AudioClip killClip = ReadClip(so, "_killClip");
            AudioClip launchClip = ReadClip(so, "_launchClip");
            float launchVolume = so.FindProperty("_launchVolume") != null
                ? so.FindProperty("_launchVolume").floatValue
                : 0.7f;
            AudioClip defeatStampClip = ReadClip(so, "_defeatStampClip");
            float defeatStampVolume = so.FindProperty("_defeatStampVolume") != null
                ? so.FindProperty("_defeatStampVolume").floatValue
                : 0.85f;
            ParticleSystem impactBurst = ReadParticle(so, "_impactBurstPrefab");
            ParticleSystem launchBurst = ReadParticle(so, "_launchBurstPrefab");
            ParticleSystem deathBurst = ReadParticle(so, "_deathBurstPrefab");

            Undo.RecordObject(catalog, "Migrer JuiceDirector vers Catalogue");

            var report = new StringBuilder();
            report.AppendLine("# Juice Data Migration — Groupe A → Catalogue");
            report.AppendLine();
            report.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine($"Scène : {SceneManager.GetActiveScene().path}");
            report.AppendLine($"Catalogue : {CatalogPath}");
            report.AppendLine();
            report.AppendLine("| Entrée | Statut | Détail |");
            report.AppendLine("|---|---|---|");

            int migrated = 0, intact = 0, leftEmpty = 0;

            MigrateEntry(
                catalog,
                FeedbackEventId.AllyLaunch,
                WrapSingle(launchClip),
                launchBurst,
                launchVolume,
                new MirrorMeta { Family = FeedbackBundle.VoiceFamily.Moments, Emphasis = 3, CopyVolume = true },
                report,
                ref migrated, ref intact, ref leftEmpty);

            MigrateEntry(
                catalog,
                FeedbackEventId.WallBounce,
                wallBounceClips,
                null,
                0f,
                new MirrorMeta { Family = FeedbackBundle.VoiceFamily.Impacts, Emphasis = 2, CopyVolume = false },
                report,
                ref migrated, ref intact, ref leftEmpty);

            MigrateEntry(
                catalog,
                FeedbackEventId.HitEnemy,
                hitClips,
                impactBurst,
                0f,
                new MirrorMeta { Family = FeedbackBundle.VoiceFamily.Impacts, Emphasis = 4, CopyVolume = false },
                report,
                ref migrated, ref intact, ref leftEmpty);

            MigrateEntry(
                catalog,
                FeedbackEventId.Crit,
                WrapSingle(critClip),
                null,
                0f,
                new MirrorMeta { Family = FeedbackBundle.VoiceFamily.Impacts, Emphasis = 5, CopyVolume = false },
                report,
                ref migrated, ref intact, ref leftEmpty);

            MigrateEntry(
                catalog,
                FeedbackEventId.Kill,
                WrapSingle(killClip),
                deathBurst,
                0f,
                new MirrorMeta { Family = FeedbackBundle.VoiceFamily.Impacts, Emphasis = 5, CopyVolume = false },
                report,
                ref migrated, ref intact, ref leftEmpty);

            MigrateEntry(
                catalog,
                FeedbackEventId.DefeatBeat,
                WrapSingle(defeatStampClip),
                null,
                defeatStampVolume,
                new MirrorMeta { Family = FeedbackBundle.VoiceFamily.Moments, Emphasis = 6, CopyVolume = true },
                report,
                ref migrated, ref intact, ref leftEmpty);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            report.AppendLine();
            report.AppendLine($"**Résumé** : {migrated} MIGRÉES · {intact} INTACTES · {leftEmpty} LAISSÉES VIDES");

            string auditsDir = Path.Combine(Application.dataPath, "..", "Audits");
            Directory.CreateDirectory(auditsDir);
            string reportPath = Path.Combine(
                auditsDir,
                $"JuiceDataMigration_{DateTime.Now:yyyyMMdd_HHmm}.md");
            File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);

            Debug.Log(
                $"[JuiceDataMigration] Terminé — {migrated} MIGRÉES, {intact} INTACTES, {leftEmpty} LAISSÉES VIDES. Rapport : {reportPath}");
        }

        [MenuItem("Chez Arthur/Feedback/Câbler Catalogue sur JuiceDirector (Scène)")]
        public static void WireCatalogOnJuiceDirector()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[JuiceDataMigration] Aucune scène active chargée.");
                return;
            }

            JuiceDirector juice = UnityEngine.Object.FindObjectOfType<JuiceDirector>(true);
            if (juice == null)
            {
                Debug.LogError("[JuiceDataMigration] Aucun JuiceDirector dans la scène active.");
                return;
            }

            FeedbackCatalog catalog = AssetDatabase.LoadAssetAtPath<FeedbackCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[JuiceDataMigration] Catalogue introuvable : {CatalogPath}");
                return;
            }

            SerializedObject so = new SerializedObject(juice);
            SerializedProperty catalogProp = so.FindProperty("_feedbackCatalog");
            if (catalogProp == null)
            {
                Debug.LogError("[JuiceDataMigration] Champ _feedbackCatalog introuvable (passe 1 manquante ?).");
                return;
            }

            if (catalogProp.objectReferenceValue == catalog)
            {
                Debug.Log("[JuiceDataMigration] Catalogue déjà câblé — aucun changement.");
                return;
            }

            Undo.RecordObject(juice, "Câbler Catalogue sur JuiceDirector");
            catalogProp.objectReferenceValue = catalog;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[JuiceDataMigration] Catalogue câblé sur JuiceDirector — sauver la scène (Ctrl+S).");
        }

        [MenuItem("Chez Arthur/Feedback/Re-sérialiser Scène Combat (purge P2b)")]
        public static void ReserializeCombatSceneForPurge()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[JuiceDataMigration] Aucune scène active chargée.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[JuiceDataMigration] Scène re-sérialisée : {scene.path}");
        }

        // ═══════════════════════════════════════════
        // MIGRATION HELPERS
        // ═══════════════════════════════════════════

        private static void MigrateEntry(
            FeedbackCatalog catalog,
            FeedbackEventId eventId,
            AudioClip[] clips,
            ParticleSystem vfx,
            float volumeScale,
            MirrorMeta meta,
            StringBuilder report,
            ref int migrated,
            ref int intact,
            ref int leftEmpty)
        {
            FeedbackCatalog.Entry entry = FindOrCreateEntry(catalog, eventId);
            if (entry.bundle == null)
                entry.bundle = new FeedbackBundle();

            FeedbackBundle bundle = entry.bundle;
            bool already = bundle.HasSfx || bundle.HasVfx;
            if (already)
            {
                intact++;
                report.AppendLine($"| {eventId} | INTACTE | HasSfx={bundle.HasSfx} HasVfx={bundle.HasVfx} |");
                return;
            }

            AudioClip[] cleanClips = FilterNullClips(clips);
            bool hasSfx = cleanClips != null && cleanClips.Length > 0;
            bool hasVfx = vfx != null;

            if (!hasSfx && !hasVfx)
            {
                leftEmpty++;
                report.AppendLine($"| {eventId} | LAISSÉE VIDE | Champ scène null / tableaux vides |");
                return;
            }

            if (hasSfx)
                bundle.clips = cleanClips;
            if (hasVfx)
                bundle.vfxPrefab = vfx;
            if (meta.CopyVolume && hasSfx)
                bundle.volumeScale = volumeScale;

            // Métadonnées miroir (documentation mapping P2a) — uniquement avec payload
            bundle.voiceFamily = meta.Family;
            bundle.emphasis = meta.Emphasis;

            migrated++;
            string detail = DescribePayload(cleanClips, vfx, meta.CopyVolume ? volumeScale : (float?)null);
            report.AppendLine($"| {eventId} | MIGRÉE | {detail} |");
        }

        private static FeedbackCatalog.Entry FindOrCreateEntry(FeedbackCatalog catalog, FeedbackEventId eventId)
        {
            List<FeedbackCatalog.Entry> entries = catalog.EntriesMutable;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].eventId == eventId)
                    return entries[i];
            }

            var created = new FeedbackCatalog.Entry
            {
                eventId = eventId,
                bundle = new FeedbackBundle()
            };
            entries.Add(created);
            return created;
        }

        private static string DescribePayload(AudioClip[] clips, ParticleSystem vfx, float? volume)
        {
            var sb = new StringBuilder();
            if (clips != null && clips.Length > 0)
            {
                sb.Append("clips=[");
                for (int i = 0; i < clips.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(clips[i] != null ? $"{clips[i].name} ({AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clips[i]))})" : "null");
                }
                sb.Append(']');
            }

            if (vfx != null)
            {
                if (sb.Length > 0) sb.Append("; ");
                string path = AssetDatabase.GetAssetPath(vfx);
                sb.Append($"vfx={vfx.name} ({AssetDatabase.AssetPathToGUID(path)})");
            }

            if (volume.HasValue)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append($"volumeScale={volume.Value:0.###}");
            }

            return sb.ToString();
        }

        private static AudioClip[] WrapSingle(AudioClip clip)
        {
            if (clip == null)
                return null;
            return new[] { clip };
        }

        private static AudioClip[] FilterNullClips(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            int count = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    count++;
            }

            if (count == 0)
                return null;

            if (count == clips.Length)
                return clips;

            var clean = new AudioClip[count];
            int w = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    clean[w++] = clips[i];
            }

            return clean;
        }

        private static AudioClip ReadClip(SerializedObject so, string name)
        {
            SerializedProperty p = so.FindProperty(name);
            return p != null ? p.objectReferenceValue as AudioClip : null;
        }

        private static AudioClip[] ReadClipArray(SerializedObject so, string name)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null || !p.isArray)
                return null;

            int n = p.arraySize;
            if (n <= 0)
                return Array.Empty<AudioClip>();

            var arr = new AudioClip[n];
            for (int i = 0; i < n; i++)
                arr[i] = p.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip;
            return arr;
        }

        private static ParticleSystem ReadParticle(SerializedObject so, string name)
        {
            SerializedProperty p = so.FindProperty(name);
            return p != null ? p.objectReferenceValue as ParticleSystem : null;
        }
    }
}
#endif
