#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChezArthur.Localization;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Export / import CSV des tables EN (UTF-8 BOM, séparateur ;, Excel FR).
    /// </summary>
    public static class LocalizationCsvTool
    {
        private const string TablePath = "Assets/_Project/Data/Localization/Table_UI.asset";
        private const string CatalogPath = "Assets/_Project/Resources/LocalizationCatalog.asset";

        [MenuItem("Chez Arthur/Localization/Export CSV")]
        public static void ExportCsv()
        {
            var map = new SortedDictionary<string, string>(StringComparer.Ordinal);
            CollectFromCatalog(map);

            string path = EditorUtility.SaveFilePanel(
                "Export localisation CSV",
                Application.dataPath,
                "localization_en.csv",
                "csv");
            if (string.IsNullOrEmpty(path))
                return;

            var sb = new StringBuilder();
            sb.AppendLine("key;english");
            foreach (var kv in map)
            {
                sb.Append(Quote(kv.Key));
                sb.Append(';');
                sb.Append(Quote(kv.Value ?? ""));
                sb.AppendLine();
            }

            // UTF-8 avec BOM
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            File.WriteAllText(path, sb.ToString(), encoding);
            Debug.Log($"[Loc] CSV exporté : {path} ({map.Count} clés)");
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Chez Arthur/Localization/Import CSV")]
        public static void ImportCsv()
        {
            string path = EditorUtility.OpenFilePanel(
                "Import localisation CSV",
                Application.dataPath,
                "csv");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            if (table == null)
            {
                Debug.LogError("[Loc] Table_UI introuvable — lancez d'abord Build Pilote.");
                return;
            }

            EnsureCatalogLinksTable(table);

            string raw = File.ReadAllText(path, Encoding.UTF8);
            if (raw.Length > 0 && raw[0] == '\uFEFF')
                raw = raw.Substring(1);

            string[] lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int updated = 0;
            int added = 0;
            int ignored = 0;

            Undo.RecordObject(table, "Import localization CSV");

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                if (i == 0 && line.StartsWith("key", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!TryParseCsvLine(line, out string key, out string english))
                {
                    ignored++;
                    continue;
                }

                if (string.IsNullOrEmpty(key))
                {
                    ignored++;
                    continue;
                }

                LocalizationEntry existing = FindEntry(table, key);
                if (existing != null)
                {
                    if (existing.english != english)
                    {
                        existing.english = english;
                        updated++;
                    }
                }
                else
                {
                    table.EntriesMutable.Add(new LocalizationEntry
                    {
                        key = key,
                        english = english ?? ""
                    });
                    added++;
                }
            }

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Loc] CSV importé — mises à jour : {updated}, ajoutées : {added}, ignorées : {ignored}");
        }

        private static void CollectFromCatalog(SortedDictionary<string, string> map)
        {
            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(CatalogPath);
            if (catalog != null && catalog.Tables != null)
            {
                for (int t = 0; t < catalog.Tables.Count; t++)
                {
                    LocalizationTable table = catalog.Tables[t];
                    AddTableEntries(table, map);
                }
            }

            // Fallback : toutes les tables du projet
            if (map.Count == 0)
            {
                string[] guids = AssetDatabase.FindAssets("t:LocalizationTable");
                for (int i = 0; i < guids.Length; i++)
                {
                    LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                    AddTableEntries(table, map);
                }
            }
        }

        private static void AddTableEntries(LocalizationTable table, SortedDictionary<string, string> map)
        {
            if (table == null || table.Entries == null)
                return;

            for (int i = 0; i < table.Entries.Count; i++)
            {
                LocalizationEntry e = table.Entries[i];
                if (e == null || string.IsNullOrEmpty(e.key))
                    continue;
                map[e.key] = e.english ?? "";
            }
        }

        private static void EnsureCatalogLinksTable(LocalizationTable table)
        {
            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(CatalogPath);
            if (catalog == null || catalog.TablesMutable == null)
                return;
            if (catalog.TablesMutable.Contains(table))
                return;

            Undo.RecordObject(catalog, "Link table to catalog");
            catalog.TablesMutable.Add(table);
            EditorUtility.SetDirty(catalog);
        }

        private static LocalizationEntry FindEntry(LocalizationTable table, string key)
        {
            for (int i = 0; i < table.EntriesMutable.Count; i++)
            {
                LocalizationEntry e = table.EntriesMutable[i];
                if (e != null && e.key == key)
                    return e;
            }

            return null;
        }

        private static string Quote(string value)
        {
            if (value == null)
                value = "";
            string escaped = value.Replace("\"", "\"\"");
            return "\"" + escaped + "\"";
        }

        private static bool TryParseCsvLine(string line, out string key, out string english)
        {
            key = null;
            english = null;
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                        inQuotes = true;
                    else if (c == ';')
                    {
                        fields.Add(current.ToString());
                        current.Length = 0;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }

            fields.Add(current.ToString());
            if (fields.Count < 2)
                return false;

            key = fields[0].Trim();
            english = fields[1];
            return true;
        }
    }
}
#endif
