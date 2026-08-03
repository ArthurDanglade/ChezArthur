#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using ChezArthur.Gameplay.Feedback;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools.Audit
{
    /// <summary>
    /// Audit lecture seule du FeedbackCatalog (couverture, bornes, VFX non-loop).
    /// </summary>
    public static class FeedbackCatalogAuditor
    {
        private const string CatalogPath = "Assets/_Project/Data/Feedback/FeedbackCatalog.asset";

        [MenuItem("Chez Arthur/Feedback/Audit Catalogue Feedback")]
        public static void RunAudit()
        {
            var sb = new StringBuilder(8192);
            DateTime now = DateTime.Now;
            sb.AppendLine("# Audit FeedbackCatalog");
            sb.AppendLine();
            sb.AppendLine($"- **Date** : {now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            FeedbackCatalog catalog = AssetDatabase.LoadAssetAtPath<FeedbackCatalog>(CatalogPath);
            if (catalog == null)
            {
                sb.AppendLine($"❌ Catalogue introuvable : `{CatalogPath}`");
                Write(sb, now);
                return;
            }

            var byId = new FeedbackCatalog.Entry[FeedbackCatalog.EventCount];
            var entries = catalog.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                FeedbackCatalog.Entry e = entries[i];
                if (e == null)
                    continue;
                int idx = (int)e.eventId;
                if (idx >= 0 && idx < FeedbackCatalog.EventCount)
                    byId[idx] = e;
            }

            sb.AppendLine("## Couverture enum (40)");
            sb.AppendLine();
            int missingEntries = 0;
            for (int i = 0; i < FeedbackCatalog.EventCount; i++)
            {
                if (byId[i] == null || byId[i].bundle == null)
                {
                    missingEntries++;
                    sb.AppendLine($"- ❌ Entrée absente : `{(FeedbackEventId)i}`");
                }
            }

            if (missingEntries == 0)
                sb.AppendLine("_40/40 entrées présentes._");
            sb.AppendLine();

            sb.AppendLine("## Qualité bundles");
            sb.AppendLine();
            int nullClips = 0, boundFails = 0, loopVfx = 0;
            int withSfxA = 0, withVfxA = 0, withSfxB = 0, withVfxB = 0, withSfxC = 0, withVfxC = 0;

            for (int i = 0; i < FeedbackCatalog.EventCount; i++)
            {
                FeedbackCatalog.Entry e = byId[i];
                if (e?.bundle == null)
                    continue;

                FeedbackBundle b = e.bundle;
                if (b.clips != null)
                {
                    for (int c = 0; c < b.clips.Length; c++)
                    {
                        if (b.clips[c] == null)
                        {
                            nullClips++;
                            sb.AppendLine($"- ❌ Clip null dans `{(FeedbackEventId)i}` [{c}]");
                        }
                    }
                }

                if (b.emphasis < 1 || b.emphasis > 6 ||
                    b.cooldownMs < 0 || b.cooldownMs > 2000 ||
                    b.pitchMin < 0.5f || b.pitchMax > 2f || b.pitchMin > b.pitchMax)
                {
                    boundFails++;
                    sb.AppendLine(
                        $"- ❌ Bornes hors contrat : `{(FeedbackEventId)i}` " +
                        $"(emphase={b.emphasis}, cd={b.cooldownMs}, pitch={b.pitchMin}-{b.pitchMax})");
                }

                if (b.vfxPrefab != null && b.vfxPrefab.main.loop)
                {
                    loopVfx++;
                    sb.AppendLine($"- ❌ VFX loop=true interdit dans ce pool : `{(FeedbackEventId)i}`");
                }

                bool hasSfx = b.HasSfx;
                bool hasVfx = b.HasVfx;
                if (i <= 8)
                {
                    if (hasSfx) withSfxA++;
                    if (hasVfx) withVfxA++;
                }
                else if (i <= 26)
                {
                    if (hasSfx) withSfxB++;
                    if (hasVfx) withVfxB++;
                }
                else
                {
                    if (hasSfx) withSfxC++;
                    if (hasVfx) withVfxC++;
                }
            }

            sb.AppendLine();
            sb.AppendLine($"- Clips null : **{nullClips}** · Bornes : **{boundFails}** · VFX loop : **{loopVfx}**");
            sb.AppendLine();

            sb.AppendLine("## Overrides");
            sb.AppendLine();
            int badOverrides = 0;
            var overrides = catalog.Overrides;
            for (int i = 0; i < overrides.Count; i++)
            {
                FeedbackCatalog.CharacterOverride o = overrides[i];
                if (o == null)
                    continue;
                if (string.IsNullOrEmpty(o.characterId))
                {
                    badOverrides++;
                    sb.AppendLine($"- ❌ Override characterId vide (event `{o.eventId}`)");
                }
            }

            if (badOverrides == 0)
                sb.AppendLine("_Aucun override orphelin._");
            sb.AppendLine();

            sb.AppendLine("## Récap par groupe");
            sb.AppendLine();
            sb.AppendLine($"- **A** (0–8) : son={withSfxA} · visuel={withVfxA}");
            sb.AppendLine($"- **B** (9–26) : son={withSfxB} · visuel={withVfxB}");
            sb.AppendLine($"- **C** (27–39) : son={withSfxC} · visuel={withVfxC}");
            sb.AppendLine();

            Write(sb, now);
        }

        private static void Write(StringBuilder sb, DateTime now)
        {
            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, $"FeedbackCatalogAudit_{now:yyyyMMdd_HHmm}.md");
            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[FeedbackCatalogAuditor] Rapport : {fullPath}");
            EditorUtility.RevealInFinder(fullPath);
        }
    }
}
#endif
