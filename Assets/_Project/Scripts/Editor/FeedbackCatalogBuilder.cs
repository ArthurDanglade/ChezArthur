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
    /// Crée / met à jour le FeedbackCatalog (idempotent : n'écrase jamais une valeur non vide).
    /// </summary>
    public static class FeedbackCatalogBuilder
    {
        private const string CatalogPath = "Assets/_Project/Data/Feedback/FeedbackCatalog.asset";
        private const string PlaceholderPath = "Assets/_Project/Prefabs/VFX/Feedback/FxPlaceholder.prefab";
        private const string ImpactBurstPath = "Assets/_Project/Prefabs/ImpactBurst.prefab";
        private const string CombatSfxRoot = "Assets/_Project/Audio/SFX/Combat";

        private struct Seed
        {
            public string Slot;
            public FeedbackEventId EventId;
            public FeedbackBundle.VoiceFamily Family;
            public int CooldownMs;
            public int Emphasis;
            public float Volume;
            public bool FitPitchToDuration;
            public float ShakeTrauma;
            public int HitstopMs;
            public FeedbackBundle.HapticLevel Haptic;
        }

        private static readonly Seed[] Seeds =
        {
            new Seed { Slot = "heal", EventId = FeedbackEventId.HealReceived, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 3, Volume = 0.8f },
            new Seed { Slot = "buff_up", EventId = FeedbackEventId.BuffApplied, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 3, Volume = 0.75f },
            new Seed { Slot = "debuff_down", EventId = FeedbackEventId.DebuffApplied, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 3, Volume = 0.75f },
            new Seed { Slot = "shield_gain", EventId = FeedbackEventId.ShieldGained, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 3, Volume = 0.8f },
            new Seed { Slot = "shield_hit", EventId = FeedbackEventId.ShieldAbsorbed, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 90, Emphasis = 2, Volume = 0.7f },
            new Seed { Slot = "shield_break", EventId = FeedbackEventId.ShieldBroken, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 5, Volume = 0.9f, Haptic = FeedbackBundle.HapticLevel.Medium },
            new Seed { Slot = "burn_apply", EventId = FeedbackEventId.BurnApplied, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 3, Volume = 0.8f },
            new Seed { Slot = "burn_tick", EventId = FeedbackEventId.BurnTick, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 1, Volume = 0.6f },
            new Seed { Slot = "poison_tick", EventId = FeedbackEventId.PoisonTick, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 1, Volume = 0.6f },
            new Seed { Slot = "stun_apply", EventId = FeedbackEventId.StunApplied, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 4, Volume = 0.85f, Haptic = FeedbackBundle.HapticLevel.Light },
            new Seed { Slot = "freeze_apply", EventId = FeedbackEventId.FreezeApplied, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 4, Volume = 0.85f, Haptic = FeedbackBundle.HapticLevel.Light },
            new Seed { Slot = "freeze_end", EventId = FeedbackEventId.FreezeEnded, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 2, Volume = 0.7f },
            new Seed { Slot = "enemy_windup", EventId = FeedbackEventId.EnemyWindup, Family = FeedbackBundle.VoiceFamily.Moments, CooldownMs = 200, Emphasis = 2, Volume = 0.7f, FitPitchToDuration = true },
            // HitstopMs = 0 : hitstop ici gelait la bille allié avant son OnCollisionEnter2D
            // (ram → ennemi kinematic) → raw=1. Shake seul OK.
            new Seed { Slot = "enemy_hit_ally", EventId = FeedbackEventId.EnemyHitAlly, Family = FeedbackBundle.VoiceFamily.Impacts, CooldownMs = 70, Emphasis = 5, Volume = 0.85f, ShakeTrauma = 0.12f, HitstopMs = 0, Haptic = FeedbackBundle.HapticLevel.Light },
            new Seed { Slot = "enemy_launch", EventId = FeedbackEventId.EnemyLaunch, Family = FeedbackBundle.VoiceFamily.Moments, CooldownMs = 150, Emphasis = 2, Volume = 0.7f },
            new Seed { Slot = "enemy_wall_bounce", EventId = FeedbackEventId.EnemyWallBounce, Family = FeedbackBundle.VoiceFamily.Impacts, CooldownMs = 120, Emphasis = 1, Volume = 0.4f },
            new Seed { Slot = "boss_defeated", EventId = FeedbackEventId.BossDefeated, Family = FeedbackBundle.VoiceFamily.Moments, CooldownMs = 1000, Emphasis = 6, Volume = 0.9f, Haptic = FeedbackBundle.HapticLevel.Heavy },
            new Seed { Slot = "revive", EventId = FeedbackEventId.Revive, Family = FeedbackBundle.VoiceFamily.Moments, CooldownMs = 300, Emphasis = 4, Volume = 0.85f, Haptic = FeedbackBundle.HapticLevel.Medium },
            new Seed { Slot = "extra_turn", EventId = FeedbackEventId.ExtraTurn, Family = FeedbackBundle.VoiceFamily.UI, CooldownMs = 150, Emphasis = 2, Volume = 0.6f },
            new Seed { Slot = "turn_relay", EventId = FeedbackEventId.TurnRelay, Family = FeedbackBundle.VoiceFamily.UI, CooldownMs = 150, Emphasis = 1, Volume = 0.35f },
            new Seed { Slot = "victory_sting", EventId = FeedbackEventId.VictorySting, Family = FeedbackBundle.VoiceFamily.Moments, CooldownMs = 1000, Emphasis = 6, Volume = 0.9f, Haptic = FeedbackBundle.HapticLevel.Heavy },
            new Seed { Slot = "spec_switch", EventId = FeedbackEventId.SpecSwitch, Family = FeedbackBundle.VoiceFamily.UI, CooldownMs = 150, Emphasis = 2, Volume = 0.6f },
            new Seed { Slot = "summon_spawn", EventId = FeedbackEventId.SummonSpawned, Family = FeedbackBundle.VoiceFamily.Moments, CooldownMs = 150, Emphasis = 3, Volume = 0.8f },
            new Seed { Slot = "zone_place", EventId = FeedbackEventId.ZonePlaced, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 2, Volume = 0.7f },
            new Seed { Slot = "zone_cross", EventId = FeedbackEventId.ZoneCrossed, Family = FeedbackBundle.VoiceFamily.Statuts, CooldownMs = 120, Emphasis = 1, Volume = 0.6f },
            // Groupe A — porteurs de sync haptique (+ câblage sfx_crit si clips vides).
            new Seed { Slot = "crit", EventId = FeedbackEventId.Crit, Family = FeedbackBundle.VoiceFamily.Impacts, CooldownMs = 70, Emphasis = 5, Volume = 0.85f, Haptic = FeedbackBundle.HapticLevel.Medium },
            new Seed { Slot = "kill", EventId = FeedbackEventId.Kill, Family = FeedbackBundle.VoiceFamily.Impacts, CooldownMs = 120, Emphasis = 5, Volume = 0.9f, Haptic = FeedbackBundle.HapticLevel.Medium },
            new Seed { Slot = "defeat", EventId = FeedbackEventId.DefeatBeat, Family = FeedbackBundle.VoiceFamily.Moments, CooldownMs = 1000, Emphasis = 6, Volume = 0.9f, Haptic = FeedbackBundle.HapticLevel.Light },
        };

        [MenuItem("Chez Arthur/Feedback/Créer ou Mettre à Jour le Catalogue")]
        public static void BuildOrUpdate()
        {
            int created = 0, completed = 0, intact = 0, clipsWired = 0, slotsWithoutClip = 0;

            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Feedback");
            EnsureFolder("Assets/_Project/Prefabs/VFX");
            EnsureFolder("Assets/_Project/Prefabs/VFX/Feedback");

            FeedbackCatalog catalog = AssetDatabase.LoadAssetAtPath<FeedbackCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FeedbackCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                created++;
            }

            List<FeedbackCatalog.Entry> entries = catalog.EntriesMutable;
            var byId = new Dictionary<int, FeedbackCatalog.Entry>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null)
                    byId[(int)entries[i].eventId] = entries[i];
            }

            for (int i = 0; i < FeedbackCatalog.EventCount; i++)
            {
                var id = (FeedbackEventId)i;
                if (!byId.TryGetValue(i, out FeedbackCatalog.Entry entry))
                {
                    entry = new FeedbackCatalog.Entry
                    {
                        eventId = id,
                        bundle = new FeedbackBundle()
                    };
                    entries.Add(entry);
                    byId[i] = entry;
                    created++;
                }
                else if (entry.bundle == null)
                {
                    entry.bundle = new FeedbackBundle();
                    completed++;
                }
                else
                {
                    intact++;
                }
            }

            ParticleSystem placeholder = EnsurePlaceholder();
            WirePlaceholderIfEmpty(byId, FeedbackEventId.HealReceived, placeholder, FeedbackCause.Heal, ref completed);
            WirePlaceholderIfEmpty(byId, FeedbackEventId.BuffApplied, placeholder, FeedbackCause.BuffUp, ref completed);
            WirePlaceholderIfEmpty(byId, FeedbackEventId.DebuffApplied, placeholder, FeedbackCause.DebuffDown, ref completed);
            WirePlaceholderIfEmpty(byId, FeedbackEventId.ShieldBroken, placeholder, FeedbackCause.Shield, ref completed);

            Dictionary<string, List<AudioClip>> clipsBySlot = ScanCombatClips();
            for (int s = 0; s < Seeds.Length; s++)
            {
                Seed seed = Seeds[s];
                if (!byId.TryGetValue((int)seed.EventId, out FeedbackCatalog.Entry entry) || entry.bundle == null)
                {
                    slotsWithoutClip++;
                    continue;
                }

                FeedbackBundle b = entry.bundle;

                // Champs seedés toujours synchronisés (y compris entrées déjà clipées).
                if (seed.FitPitchToDuration)
                    b.fitPitchToDuration = true;
                if (seed.ShakeTrauma > 0f)
                    b.shakeTrauma = seed.ShakeTrauma;
                // EnemyHitAlly : forcer y compris 0 (évite de réintroduire hitstopMs=50).
                if (seed.EventId == FeedbackEventId.EnemyHitAlly || seed.HitstopMs > 0)
                    b.hitstopMs = seed.HitstopMs;
                if (seed.Haptic != FeedbackBundle.HapticLevel.None)
                    b.haptic = seed.Haptic;
                // EnemyHitAlly : emphase 5 pour voler une voix Impacts (thud ne doit pas se faire mute).
                if (seed.EventId == FeedbackEventId.EnemyHitAlly)
                    b.emphasis = seed.Emphasis;

                // Idempotence clips : ne câble que si encore vides.
                if (b.HasSfx)
                    continue;

                // Baseline bundle même sans clip (émetteurs silencieux propres en attendant courses).
                b.voiceFamily = seed.Family;
                b.cooldownMs = seed.CooldownMs;
                b.emphasis = seed.Emphasis;
                b.volumeScale = seed.Volume;
                b.pitchMin = 0.96f;
                b.pitchMax = 1.04f;

                if (!clipsBySlot.TryGetValue(seed.Slot, out List<AudioClip> list) || list.Count == 0)
                {
                    slotsWithoutClip++;
                    completed++;
                    continue;
                }

                b.clips = list.ToArray();
                clipsWired += list.Count;
                completed++;
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WriteReport(created, completed, intact, clipsWired, slotsWithoutClip, clipsBySlot);
            Debug.Log($"[FeedbackCatalogBuilder] Catalogue à jour — créées={created} complétées={completed} intactes={intact} clips={clipsWired}");
            EditorGUIUtility.PingObject(catalog);
        }

        private static void WirePlaceholderIfEmpty(
            Dictionary<int, FeedbackCatalog.Entry> byId,
            FeedbackEventId id,
            ParticleSystem placeholder,
            FeedbackCause cause,
            ref int completed)
        {
            if (placeholder == null)
                return;
            if (!byId.TryGetValue((int)id, out FeedbackCatalog.Entry entry) || entry.bundle == null)
                return;
            if (entry.bundle.vfxPrefab != null)
                return;

            entry.bundle.vfxPrefab = placeholder;
            entry.bundle.tintMode = FeedbackBundle.TintMode.Cause;
            entry.bundle.tintCause = cause;
            completed++;
        }

        private static ParticleSystem EnsurePlaceholder()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderPath);
            if (existing != null)
                return existing.GetComponent<ParticleSystem>();

            if (!File.Exists(ImpactBurstPath) && AssetDatabase.LoadAssetAtPath<GameObject>(ImpactBurstPath) == null)
            {
                Debug.LogWarning($"[FeedbackCatalogBuilder] Source manquante : {ImpactBurstPath}");
                return null;
            }

            if (!AssetDatabase.CopyAsset(ImpactBurstPath, PlaceholderPath))
            {
                Debug.LogWarning("[FeedbackCatalogBuilder] CopyAsset FxPlaceholder échoué.");
                return null;
            }

            AssetDatabase.ImportAsset(PlaceholderPath);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderPath);
            return go != null ? go.GetComponent<ParticleSystem>() : null;
        }

        private static Dictionary<string, List<AudioClip>> ScanCombatClips()
        {
            var map = new Dictionary<string, List<AudioClip>>();
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { CombatSfxRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string stem = Path.GetFileNameWithoutExtension(path);
                string slot = ExtractSlot(stem);
                if (slot == null)
                    continue;

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                    continue;

                if (!map.TryGetValue(slot, out List<AudioClip> list))
                {
                    list = new List<AudioClip>(4);
                    map[slot] = list;
                }

                list.Add(clip);
            }

            return map;
        }

        private static string ExtractSlot(string stem)
        {
            if (string.IsNullOrEmpty(stem) || !stem.StartsWith("sfx_", StringComparison.Ordinal))
                return null;

            string body = stem.Substring(4);
            int last = body.LastIndexOf('_');
            if (last <= 0 || last >= body.Length - 1)
                return null;

            string suffix = body.Substring(last + 1);
            for (int i = 0; i < suffix.Length; i++)
            {
                if (suffix[i] < '0' || suffix[i] > '9')
                    return null;
            }

            return body.Substring(0, last);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        private static void WriteReport(
            int created, int completed, int intact, int clipsWired, int slotsWithoutClip,
            Dictionary<string, List<AudioClip>> clipsBySlot)
        {
            var sb = new StringBuilder(2048);
            DateTime now = DateTime.Now;
            sb.AppendLine("# FeedbackCatalog — builder");
            sb.AppendLine();
            sb.AppendLine($"- **Date** : {now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Entrées créées : **{created}**");
            sb.AppendLine($"- Champs complétés : **{completed}**");
            sb.AppendLine($"- Intactes : **{intact}**");
            sb.AppendLine($"- Clips câblés : **{clipsWired}**");
            sb.AppendLine($"- Slots seed sans clip : **{slotsWithoutClip}**");
            sb.AppendLine();
            sb.AppendLine("## Slots banque scannés");
            sb.AppendLine();
            foreach (KeyValuePair<string, List<AudioClip>> kv in clipsBySlot)
                sb.AppendLine($"- `{kv.Key}` × {kv.Value.Count}");

            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, $"FeedbackCatalog_{now:yyyyMMdd_HHmm}.md");
            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[FeedbackCatalogBuilder] Rapport : {fullPath}");
        }
    }
}
#endif
