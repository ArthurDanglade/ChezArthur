#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ChezArthur.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Audit lecture seule de la couverture EN (clés code + scène vs tables).
    /// </summary>
    public static class LocalizationAuditor
    {
        [MenuItem("Chez Arthur/Localization/Audit Couverture")]
        public static void RunAudit()
        {
            var referenced = new HashSet<string>();
            var tableKeys = new Dictionary<string, string>(); // key → english
            var sceneKeys = new HashSet<string>();

            ScanCodeKeys(referenced);
            ScanTables(tableKeys);
            ScanSceneLocalizedTexts(sceneKeys, referenced);

            int withEnglish = 0;
            int emptyEnglish = 0;
            foreach (var kv in tableKeys)
            {
                if (string.IsNullOrEmpty(kv.Value))
                    emptyEnglish++;
                else
                    withEnglish++;
            }

            var missingInTable = new List<string>();
            foreach (string key in referenced)
            {
                if (!tableKeys.ContainsKey(key))
                    missingInTable.Add(key);
            }

            missingInTable.Sort(StringComparer.Ordinal);

            var sb = new StringBuilder(8192);
            sb.AppendLine("# Audit couverture localisation");
            sb.AppendLine();
            sb.AppendLine($"- **Date** : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **Scène ouverte** : {SceneManager.GetActiveScene().name}");
            sb.AppendLine($"- **Clés référencées** (code + LocalizedText) : {referenced.Count}");
            sb.AppendLine($"- **Clés en table** : {tableKeys.Count}");
            sb.AppendLine($"- **EN rempli** : {withEnglish}");
            sb.AppendLine($"- **EN vide (à traduire)** : {emptyEnglish}");
            float coverage = tableKeys.Count > 0
                ? (100f * withEnglish / tableKeys.Count)
                : 0f;
            sb.AppendLine($"- **Couverture EN (tables)** : {coverage:F1} %");
            sb.AppendLine($"- **Référencées absentes des tables** : {missingInTable.Count}");
            sb.AppendLine();

            sb.AppendLine("## Clés référencées absentes des tables");
            sb.AppendLine();
            if (missingInTable.Count == 0)
                sb.AppendLine("(aucune)");
            else
            {
                for (int i = 0; i < missingInTable.Count; i++)
                    sb.AppendLine($"- `{missingInTable[i]}`");
            }

            sb.AppendLine();
            sb.AppendLine("## Entrées EN vides (feuille de traduction)");
            sb.AppendLine();
            var emptyList = new List<string>();
            foreach (var kv in tableKeys)
            {
                if (string.IsNullOrEmpty(kv.Value))
                    emptyList.Add(kv.Key);
            }

            emptyList.Sort(StringComparer.Ordinal);
            if (emptyList.Count == 0)
                sb.AppendLine("(aucune)");
            else
            {
                for (int i = 0; i < emptyList.Count; i++)
                    sb.AppendLine($"- `{emptyList[i]}`");
            }

            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, "localization_coverage.txt");
            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[Loc] Audit couverture écrit : {fullPath}");
            EditorUtility.RevealInFinder(fullPath);
        }

        private static void ScanCodeKeys(HashSet<string> referenced)
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "_Project", "Scripts");
            if (!Directory.Exists(scriptsRoot))
                return;

            string[] files = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories);
            var trRegex = new Regex(
                @"Loc\.(?:Tr|Format)\s*\(\s*""([^""]+)""",
                RegexOptions.Compiled);

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i].Replace('\\', '/');
                if (path.Contains("/Editor/"))
                    continue;

                string text = File.ReadAllText(files[i], Encoding.UTF8);
                MatchCollection matches = trRegex.Matches(text);
                for (int m = 0; m < matches.Count; m++)
                    referenced.Add(matches[m].Groups[1].Value);
            }
        }

        private static void ScanTables(Dictionary<string, string> tableKeys)
        {
            string[] guids = AssetDatabase.FindAssets("t:LocalizationTable");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(assetPath);
                if (table == null || table.Entries == null)
                    continue;

                for (int e = 0; e < table.Entries.Count; e++)
                {
                    LocalizationEntry entry = table.Entries[e];
                    if (entry == null || string.IsNullOrEmpty(entry.key))
                        continue;
                    tableKeys[entry.key] = entry.english ?? "";
                }
            }
        }

        private static void ScanSceneLocalizedTexts(HashSet<string> sceneKeys, HashSet<string> referenced)
        {
            LocalizedText[] all = UnityEngine.Object.FindObjectsOfType<LocalizedText>(true);
            for (int i = 0; i < all.Length; i++)
            {
                LocalizedText lt = all[i];
                if (lt == null || string.IsNullOrEmpty(lt.Key))
                    continue;
                sceneKeys.Add(lt.Key);
                referenced.Add(lt.Key);
            }
        }
    }
}
#endif
