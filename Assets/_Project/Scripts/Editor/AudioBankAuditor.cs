#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools.Audit
{
    /// <summary>
    /// Audit lecture seule de la banque SFX combat (nommage, couverture charte, hygiène).
    /// </summary>
    public static class AudioBankAuditor
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string CombatSfxRoot = "Assets/_Project/Audio/SFX/Combat";
        private static readonly Regex NamingRegex =
            new Regex(@"^sfx_[a-z0-9]+(_[a-z0-9]+)*_[0-9]+$", RegexOptions.Compiled);

        private static readonly string[] ExpectedSlots =
        {
            "heal",
            "buff_up",
            "debuff_down",
            "shield_gain",
            "shield_hit",
            "shield_break",
            "burn_apply",
            "burn_tick",
            "poison_tick",
            "stun_apply",
            "freeze_apply",
            "freeze_end",
            "enemy_windup",
            "enemy_hit_ally",
            "turn_relay",
            "victory_sting",
            "spec_switch",
            "summon_spawn",
            "zone_place",
            "zone_cross"
        };

        private static readonly HashSet<string> FrequentSlots = new HashSet<string>
        {
            "heal",
            "buff_up",
            "debuff_down",
            "burn_tick",
            "poison_tick",
            "enemy_hit_ally"
        };

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Audio/Audit Banque SFX")]
        public static void RunAudit()
        {
            var sb = new StringBuilder(8192);
            DateTime now = DateTime.Now;
            sb.AppendLine("# Audit banque SFX combat");
            sb.AppendLine();
            sb.AppendLine($"- **Date** : {now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **Racine** : `{CombatSfxRoot}`");
            sb.AppendLine();

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { CombatSfxRoot });
            var clips = new List<ClipInfo>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ClipInfo info = BuildClipInfo(path);
                if (info != null)
                    clips.Add(info);
            }

            AppendInventory(sb, clips);
            AppendNaming(sb, clips);
            AppendCoverage(sb, clips);
            AppendHygiene(sb, clips);

            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fileName = $"AudioBank_{now:yyyyMMdd_HHmm}.md";
            string fullPath = Path.Combine(auditsRoot, fileName);
            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[AudioBankAuditor] Rapport écrit : {fullPath} — {clips.Count} clips");
            EditorUtility.RevealInFinder(fullPath);
        }

        // ═══════════════════════════════════════════
        // SECTIONS
        // ═══════════════════════════════════════════

        private static void AppendInventory(StringBuilder sb, List<ClipInfo> clips)
        {
            sb.AppendLine("## 1. Inventaire");
            sb.AppendLine();
            sb.AppendLine($"- Clips trouvés : **{clips.Count}**");
            sb.AppendLine();

            var byFolder = new Dictionary<string, List<ClipInfo>>();
            for (int i = 0; i < clips.Count; i++)
            {
                ClipInfo c = clips[i];
                string folder = Path.GetDirectoryName(c.AssetPath)?.Replace('\\', '/') ?? CombatSfxRoot;
                if (!byFolder.TryGetValue(folder, out List<ClipInfo> list))
                {
                    list = new List<ClipInfo>();
                    byFolder[folder] = list;
                }

                list.Add(c);
            }

            foreach (KeyValuePair<string, List<ClipInfo>> pair in byFolder)
            {
                sb.AppendLine($"### `{pair.Key}`");
                sb.AppendLine();
                sb.AppendLine("| Fichier | Ko | Mono forcé | LoadType |");
                sb.AppendLine("|---|---:|---|---|");
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    ClipInfo c = pair.Value[i];
                    sb.AppendLine(
                        $"| `{c.FileName}` | {c.SizeKb:0.#} | {(c.ForceToMono ? "oui" : "non")} | `{c.LoadType}` |");
                }

                sb.AppendLine();
            }
        }

        private static void AppendNaming(StringBuilder sb, List<ClipInfo> clips)
        {
            sb.AppendLine("## 2. Nommage");
            sb.AppendLine();
            sb.AppendLine("Pattern attendu : `sfx_<slot>_<n>` (`^sfx_[a-z0-9]+(_[a-z0-9]+)*_[0-9]+$`).");
            sb.AppendLine();

            int violations = 0;
            for (int i = 0; i < clips.Count; i++)
            {
                ClipInfo c = clips[i];
                string stem = Path.GetFileNameWithoutExtension(c.FileName);
                if (!NamingRegex.IsMatch(stem))
                {
                    violations++;
                    sb.AppendLine($"- ❌ `{c.AssetPath}`");
                }
            }

            if (violations == 0)
                sb.AppendLine("_Aucune violation._");

            sb.AppendLine();
            sb.AppendLine($"- Violations : **{violations}**");
            sb.AppendLine();
        }

        private static void AppendCoverage(StringBuilder sb, List<ClipInfo> clips)
        {
            sb.AppendLine("## 3. Couverture charte (20 slots)");
            sb.AppendLine();
            sb.AppendLine("| Slot | Variations | Verdict |");
            sb.AppendLine("|---|---:|---|");

            var counts = new Dictionary<string, int>();
            for (int i = 0; i < ExpectedSlots.Length; i++)
                counts[ExpectedSlots[i]] = 0;

            for (int i = 0; i < clips.Count; i++)
            {
                string slot = ExtractSlot(Path.GetFileNameWithoutExtension(clips[i].FileName));
                if (slot != null && counts.ContainsKey(slot))
                    counts[slot]++;
            }

            int missing = 0;
            int thin = 0;
            for (int i = 0; i < ExpectedSlots.Length; i++)
            {
                string slot = ExpectedSlots[i];
                int n = counts[slot];
                string verdict;
                if (n <= 0)
                {
                    missing++;
                    verdict = "❌ absent";
                }
                else if (FrequentSlots.Contains(slot) && n < 2)
                {
                    thin++;
                    verdict = "⚠️ 1 seule variation";
                }
                else
                {
                    verdict = "✅";
                }

                sb.AppendLine($"| `{slot}` | {n} | {verdict} |");
            }

            sb.AppendLine();
            sb.AppendLine($"- Slots absents : **{missing}**");
            sb.AppendLine($"- Slots fréquents à 1 variation : **{thin}**");
            sb.AppendLine();
        }

        private static void AppendHygiene(StringBuilder sb, List<ClipInfo> clips)
        {
            sb.AppendLine("## 4. Hygiène");
            sb.AppendLine();

            int stereo = 0;
            int oversized = 0;
            int badExt = 0;

            for (int i = 0; i < clips.Count; i++)
            {
                ClipInfo c = clips[i];
                string stem = Path.GetFileNameWithoutExtension(c.FileName);
                string ext = Path.GetExtension(c.FileName).ToLowerInvariant();

                if (!c.ForceToMono)
                {
                    stereo++;
                    sb.AppendLine($"- ❌ Stéréo résiduelle (forceToMono=false) : `{c.AssetPath}`");
                }

                bool isVictory = stem.StartsWith("sfx_victory_sting_", StringComparison.Ordinal);
                if (!isVictory && c.SizeBytes > 1024L * 1024L)
                {
                    oversized++;
                    sb.AppendLine($"- ⚠️ Clip > 1 Mo hors victory_sting : `{c.AssetPath}` ({c.SizeKb:0.#} Ko)");
                }

                if (ext != ".wav" && ext != ".ogg")
                {
                    badExt++;
                    sb.AppendLine($"- ⚠️ Extension inattendue : `{c.AssetPath}`");
                }
            }

            if (stereo == 0 && oversized == 0 && badExt == 0)
                sb.AppendLine("_Aucune alerte._");

            sb.AppendLine();
            sb.AppendLine($"- Stéréo : **{stereo}** · Oversized : **{oversized}** · Ext. : **{badExt}**");
            sb.AppendLine();
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private sealed class ClipInfo
        {
            public string AssetPath;
            public string FileName;
            public long SizeBytes;
            public float SizeKb;
            public bool ForceToMono;
            public string LoadType;
        }

        private static ClipInfo BuildClipInfo(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            var info = new ClipInfo
            {
                AssetPath = assetPath.Replace('\\', '/'),
                FileName = Path.GetFileName(assetPath)
            };

            string fullPath = Path.GetFullPath(assetPath);
            if (File.Exists(fullPath))
            {
                info.SizeBytes = new FileInfo(fullPath).Length;
                info.SizeKb = info.SizeBytes / 1024f;
            }

            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer != null)
            {
                info.ForceToMono = importer.forceToMono;
                info.LoadType = importer.defaultSampleSettings.loadType.ToString();
            }
            else
            {
                info.ForceToMono = false;
                info.LoadType = "?";
            }

            return info;
        }

        /// <summary>
        /// Extrait le slot depuis `sfx_<slot>_<n>` (retire le préfixe sfx_ et le suffixe _n).
        /// </summary>
        private static string ExtractSlot(string stem)
        {
            if (string.IsNullOrEmpty(stem) || !stem.StartsWith("sfx_", StringComparison.Ordinal))
                return null;

            string body = stem.Substring(4);
            int lastUnderscore = body.LastIndexOf('_');
            if (lastUnderscore <= 0 || lastUnderscore >= body.Length - 1)
                return null;

            string suffix = body.Substring(lastUnderscore + 1);
            for (int i = 0; i < suffix.Length; i++)
            {
                if (suffix[i] < '0' || suffix[i] > '9')
                    return null;
            }

            return body.Substring(0, lastUnderscore);
        }
    }
}
#endif
