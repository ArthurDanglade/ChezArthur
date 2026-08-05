#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChezArthur.Gameplay.Feedback;
using ChezArthur.UI;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Importe le pack Asset Store StateEffect → boucles Resources + one-shots catalogue.
    /// Idempotent (GUID conservés via SaveAsPrefabAsset sans Delete).
    /// </summary>
    public static class StateEffectLoopImporter
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string PackPrefabFolder = "Assets/StateEffect/EffectPrefabs";
        private const string LoopFolder = "Assets/_Project/Resources/VFX/Feedback/Loops";
        private const string OneShotFolder = "Assets/_Project/Prefabs/VFX/Feedback/StateEffect";
        private const string CatalogPath = "Assets/_Project/Data/Feedback/FeedbackCatalog.asset";

        private struct LoopMap
        {
            public string TargetName;
            public string SourcePrefab;
            public float DesignSize;
            public float Padding;
        }

        private struct OneShotMap
        {
            public string TargetName;
            public string SourcePrefab;
            public FeedbackEventId EventId;
            public FeedbackCause Cause;
            public float DesignSize;
            public float Duration;
        }

        private static readonly LoopMap[] Loops =
        {
            new LoopMap { TargetName = "LoopBurn", SourcePrefab = "effect_state_burn", DesignSize = 1.15f, Padding = 1.25f },
            new LoopMap { TargetName = "LoopPoison", SourcePrefab = "effect_state_poisoning_2", DesignSize = 1.15f, Padding = 1.25f },
            new LoopMap { TargetName = "LoopFreeze", SourcePrefab = "effect_state_coldSnow", DesignSize = 1.3f, Padding = 1.2f },
            new LoopMap { TargetName = "LoopStun", SourcePrefab = "effect_state_stuned", DesignSize = 1.0f, Padding = 1.15f },
            new LoopMap { TargetName = "LoopShield", SourcePrefab = "effect_state_energy", DesignSize = 1.2f, Padding = 1.2f },
        };

        private static readonly OneShotMap[] OneShots =
        {
            new OneShotMap
            {
                TargetName = "FxStateHeal", SourcePrefab = "effect_state_healGreen",
                EventId = FeedbackEventId.HealReceived, Cause = FeedbackCause.Heal,
                DesignSize = 1.15f, Duration = 1.4f
            },
            new OneShotMap
            {
                TargetName = "FxStateBuffUp", SourcePrefab = "effect_state_powerUp",
                EventId = FeedbackEventId.BuffApplied, Cause = FeedbackCause.BuffUp,
                DesignSize = 1.15f, Duration = 1.35f
            },
            new OneShotMap
            {
                TargetName = "FxStateDebuffDown", SourcePrefab = "effect_state_slowDown",
                EventId = FeedbackEventId.DebuffApplied, Cause = FeedbackCause.DebuffDown,
                DesignSize = 1.15f, Duration = 1.35f
            },
        };

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Feedback/Importer StateEffect (boucles + heal/buff/debuff)")]
        public static void ImportAll()
        {
            RunImport();
        }

        /// <summary> Entrée batchmode : -executeMethod ChezArthur.EditorTools.StateEffectLoopImporter.ImportAllBatch </summary>
        public static void ImportAllBatch()
        {
            try
            {
                RunImport();
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StateEffectLoopImporter] Échec batch : {ex}");
                EditorApplication.Exit(1);
            }
        }

        private static void RunImport()
        {
            var report = new StringBuilder();
            report.AppendLine("# StateEffect → Feedback");
            report.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine();

            EnsureFolder("Assets/_Project/Resources");
            EnsureFolder("Assets/_Project/Resources/VFX");
            EnsureFolder("Assets/_Project/Resources/VFX/Feedback");
            EnsureFolder(LoopFolder);
            EnsureFolder("Assets/_Project/Prefabs/VFX/Feedback");
            EnsureFolder(OneShotFolder);

            report.AppendLine("## Boucles (présence)");
            for (int i = 0; i < Loops.Length; i++)
            {
                LoopMap map = Loops[i];
                string path = ImportWrapped(
                    map.SourcePrefab,
                    LoopFolder + "/" + map.TargetName + ".prefab",
                    isLoop: true,
                    designSize: map.DesignSize,
                    padding: map.Padding,
                    oneShotDuration: 5f,
                    report);
                report.AppendLine(path != null
                    ? $"- ✅ {map.TargetName} ← {map.SourcePrefab}"
                    : $"- ❌ {map.TargetName} ← {map.SourcePrefab} INTROUVABLE");
            }

            report.AppendLine();
            report.AppendLine("## One-shots (changement de stat / heal)");
            ParticleSystem[] oneShotPs = new ParticleSystem[OneShots.Length];
            for (int i = 0; i < OneShots.Length; i++)
            {
                OneShotMap map = OneShots[i];
                string path = ImportWrapped(
                    map.SourcePrefab,
                    OneShotFolder + "/" + map.TargetName + ".prefab",
                    isLoop: false,
                    designSize: map.DesignSize,
                    padding: 1.2f,
                    oneShotDuration: map.Duration,
                    report);
                if (path != null)
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    oneShotPs[i] = go != null ? go.GetComponent<ParticleSystem>() : null;
                    report.AppendLine($"- ✅ {map.TargetName} ← {map.SourcePrefab}");
                }
                else
                {
                    report.AppendLine($"- ❌ {map.TargetName} ← {map.SourcePrefab} INTROUVABLE");
                }
            }

            report.AppendLine();
            report.AppendLine("## Catalogue");
            WireCatalog(oneShotPs, report);

            string reportPath = $"Audits/StateEffectImport_{DateTime.Now:yyyyMMdd_HHmm}.md";
            EnsureFolder("Audits");
            File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[StateEffectLoopImporter] Terminé. Rapport : {reportPath}");
        }

        // ═══════════════════════════════════════════
        // IMPORT
        // ═══════════════════════════════════════════

        private static string ImportWrapped(
            string sourceName,
            string targetPath,
            bool isLoop,
            float designSize,
            float padding,
            float oneShotDuration,
            StringBuilder report)
        {
            string sourcePath = PackPrefabFolder + "/" + sourceName + ".prefab";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
                return null;

            GameObject sourceInstance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (sourceInstance == null)
                sourceInstance = UnityEngine.Object.Instantiate(source);
            else
                PrefabUtility.UnpackPrefabInstance(
                    sourceInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            sourceInstance.name = Path.GetFileNameWithoutExtension(targetPath);

            // Conducteur root (Play/Stop withChildren + pool Callback).
            ParticleSystem conductor = sourceInstance.GetComponent<ParticleSystem>();
            if (conductor == null)
                conductor = sourceInstance.AddComponent<ParticleSystem>();

            ConfigureConductor(conductor, isLoop, oneShotDuration);

            ParticleSystemRenderer rootRenderer = sourceInstance.GetComponent<ParticleSystemRenderer>();
            if (rootRenderer == null)
                rootRenderer = sourceInstance.AddComponent<ParticleSystemRenderer>();
            rootRenderer.enabled = false;

            if (!isLoop)
                ConvertHierarchyToOneShot(sourceInstance, oneShotDuration);

            StatusFxFitProfile profile = sourceInstance.GetComponent<StatusFxFitProfile>();
            if (profile == null)
                profile = sourceInstance.AddComponent<StatusFxFitProfile>();
            profile.designSize = designSize;
            profile.padding = padding;
            profile.remapYUpToXy = true;
            profile.sortingOrderOffset = 2;
            profile.isLoop = isLoop;
            profile.oneShotDuration = oneShotDuration;

            // playOnAwake off partout — le pool / driver joue.
            ParticleSystem[] all = sourceInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                var main = all[i].main;
                main.playOnAwake = false;
            }

            PrefabUtility.SaveAsPrefabAsset(sourceInstance, targetPath);
            UnityEngine.Object.DestroyImmediate(sourceInstance);
            return targetPath;
        }

        private static void ConfigureConductor(ParticleSystem ps, bool isLoop, float duration)
        {
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = isLoop;
            main.duration = Mathf.Max(0.2f, duration);
            main.startLifetime = 0.05f;
            main.startSize = 0f;
            main.maxParticles = 1;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.cullingMode = ParticleSystemCullingMode.Automatic;
            if (!isLoop)
                main.stopAction = ParticleSystemStopAction.Callback;

            var emission = ps.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = false;
        }

        private static void ConvertHierarchyToOneShot(GameObject root, float duration)
        {
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null || ps.gameObject == root)
                    continue;

                var main = ps.main;
                main.loop = false;
                // Garde une durée visible premium sans coller éternellement.
                if (main.duration > duration)
                    main.duration = duration;
                main.playOnAwake = false;
            }
        }

        private static void WireCatalog(ParticleSystem[] oneShots, StringBuilder report)
        {
            FeedbackCatalog catalog = AssetDatabase.LoadAssetAtPath<FeedbackCatalog>(CatalogPath);
            if (catalog == null)
            {
                report.AppendLine("- ❌ FeedbackCatalog introuvable");
                return;
            }

            List<FeedbackCatalog.Entry> entries = catalog.EntriesMutable;
            for (int i = 0; i < OneShots.Length; i++)
            {
                OneShotMap map = OneShots[i];
                ParticleSystem prefab = oneShots[i];
                if (prefab == null)
                    continue;

                FeedbackCatalog.Entry entry = null;
                for (int e = 0; e < entries.Count; e++)
                {
                    if (entries[e] != null && entries[e].eventId == map.EventId)
                    {
                        entry = entries[e];
                        break;
                    }
                }

                if (entry == null || entry.bundle == null)
                {
                    report.AppendLine($"- ❌ Catalogue {map.EventId} non trouvé");
                    continue;
                }

                entry.bundle.vfxPrefab = prefab;
                entry.bundle.attachMode = FeedbackBundle.AttachMode.FollowTarget;
                entry.bundle.tintMode = FeedbackBundle.TintMode.None;
                entry.bundle.tintCause = map.Cause;
                entry.bundle.vfxScale = 1f;
                report.AppendLine($"- ✅ Catalogue {map.EventId} → {map.TargetName} (FollowTarget, Tint None)");
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
