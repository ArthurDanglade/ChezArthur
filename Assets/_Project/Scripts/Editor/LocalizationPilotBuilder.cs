#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ChezArthur.Hub.Pages;
using ChezArthur.Localization;
using ChezArthur.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Builder idempotent : assets loc + LocalizedText sur pilotes Accueil/Paramètres + sélecteur FR/EN.
    /// </summary>
    public static class LocalizationPilotBuilder
    {
        private const string TablePath = "Assets/_Project/Data/Localization/Table_UI.asset";
        private const string CatalogPath = "Assets/_Project/Resources/LocalizationCatalog.asset";

        [MenuItem("Chez Arthur/Localization/Build Pilote (scène ouverte)")]
        public static void BuildPilot()
        {
            var report = new StringBuilder(4096);
            report.AppendLine("# Localization Pilot Builder");
            report.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Scène : {SceneManager.GetActiveScene().name}");
            report.AppendLine();

            LocalizationTable table = EnsureAssets(report);
            if (table == null)
            {
                EditorUtility.DisplayDialog("Localization", "Impossible de créer/charger Table_UI.", "OK");
                return;
            }

            PageAccueilUI accueil = UnityEngine.Object.FindObjectOfType<PageAccueilUI>(true);
            SettingsPanelUI settings = UnityEngine.Object.FindObjectOfType<SettingsPanelUI>(true);

            if (accueil == null && settings == null)
            {
                EditorUtility.DisplayDialog(
                    "Localization",
                    "Aucun PageAccueilUI ni SettingsPanelUI dans la scène ouverte.",
                    "OK");
                return;
            }

            var usedKeys = new HashSet<string>();
            int added = 0;
            int updated = 0;
            int tableKeysAdded = 0;

            if (accueil != null)
            {
                report.AppendLine("## Passe Accueil");
                ProcessRoot(accueil.gameObject, "ui.accueil", table, usedKeys, report, ref added, ref updated, ref tableKeysAdded);
            }

            if (settings != null)
            {
                report.AppendLine("## Passe Paramètres");
                ProcessRoot(settings.gameObject, "ui.settings", table, usedKeys, report, ref added, ref updated, ref tableKeysAdded);
                EnsureLanguageSelector(settings, report);
            }

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            report.AppendLine();
            report.AppendLine("## Résumé");
            report.AppendLine($"- LocalizedText ajoutés : {added}");
            report.AppendLine($"- LocalizedText mis à jour (frDefault) : {updated}");
            report.AppendLine($"- Clés ajoutées à Table_UI : {tableKeysAdded}");

            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string sceneSlug = SanitizeSlug(SceneManager.GetActiveScene().name);
            string reportPath = Path.Combine(auditsRoot, $"localization_pilot_{sceneSlug}.txt");
            File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
            Debug.Log($"[LocalizationBuilder] Rapport : {reportPath}");
            EditorUtility.RevealInFinder(reportPath);
        }

        private static LocalizationTable EnsureAssets(StringBuilder report)
        {
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Localization");
            EnsureFolder("Assets/_Project/Resources");

            LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<LocalizationTable>();
                AssetDatabase.CreateAsset(table, TablePath);
                report.AppendLine($"- Créé : {TablePath}");
            }
            else
            {
                report.AppendLine($"- Table existante : {TablePath}");
            }

            LocalizationCatalog catalog = AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LocalizationCatalog>();
                catalog.TablesMutable.Add(table);
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                report.AppendLine($"- Créé : {CatalogPath}");
            }
            else
            {
                report.AppendLine($"- Catalog existant : {CatalogPath}");
                if (catalog.TablesMutable != null
                    && !catalog.TablesMutable.Contains(table))
                {
                    Undo.RecordObject(catalog, "Link Table_UI to catalog");
                    catalog.TablesMutable.Add(table);
                    EditorUtility.SetDirty(catalog);
                    report.AppendLine("- Table_UI ajoutée au catalog");
                }
            }

            AssetDatabase.SaveAssets();
            return table;
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

        private static void ProcessRoot(
            GameObject root,
            string keyPrefix,
            LocalizationTable table,
            HashSet<string> usedKeys,
            StringBuilder report,
            ref int added,
            ref int updated,
            ref int tableKeysAdded)
        {
            var texts = new List<Component>();
            texts.AddRange(root.GetComponentsInChildren<TMP_Text>(true));
            texts.AddRange(root.GetComponentsInChildren<Text>(true));

            for (int i = 0; i < texts.Count; i++)
            {
                Component comp = texts[i];
                if (comp == null)
                    continue;

                string current = GetTextValue(comp);
                if (string.IsNullOrWhiteSpace(current))
                    continue;
                if (IsPurelyNumeric(current))
                    continue;

                LocalizedText loc = comp.GetComponent<LocalizedText>();
                if (loc != null)
                {
                    // Idempotent : maj frDefault seulement, jamais de re-key
                    Undo.RecordObject(loc, "Update LocalizedText frDefault");
                    loc.EditorSetup(loc.Key, current);
                    EditorUtility.SetDirty(loc);
                    updated++;
                    if (!string.IsNullOrEmpty(loc.Key))
                        usedKeys.Add(loc.Key);
                    EnsureTableKey(table, loc.Key, ref tableKeysAdded, report);
                    continue;
                }

                string slug = SanitizeSlug(comp.gameObject.name);
                string key = $"{keyPrefix}.{slug}";
                key = DeduplicateKey(key, usedKeys);
                usedKeys.Add(key);

                loc = Undo.AddComponent<LocalizedText>(comp.gameObject);
                loc.EditorSetup(key, current);
                EditorUtility.SetDirty(loc);
                added++;
                report.AppendLine($"- + LocalizedText « {key} » sur {GetPath(comp.transform)}");
                EnsureTableKey(table, key, ref tableKeysAdded, report);
            }
        }

        private static void EnsureTableKey(
            LocalizationTable table,
            string key,
            ref int tableKeysAdded,
            StringBuilder report)
        {
            if (string.IsNullOrEmpty(key) || table == null || table.EntriesMutable == null)
                return;

            for (int i = 0; i < table.EntriesMutable.Count; i++)
            {
                LocalizationEntry e = table.EntriesMutable[i];
                if (e != null && e.key == key)
                    return;
            }

            Undo.RecordObject(table, "Add localization key");
            table.EntriesMutable.Add(new LocalizationEntry { key = key, english = "" });
            tableKeysAdded++;
            report.AppendLine($"  · clé table ajoutée : {key}");
        }

        private static void EnsureLanguageSelector(SettingsPanelUI settings, StringBuilder report)
        {
            SerializedObject so = new SerializedObject(settings);
            SerializedProperty frProp = so.FindProperty("frButton");
            SerializedProperty enProp = so.FindProperty("enButton");
            if (frProp == null || enProp == null)
            {
                report.AppendLine("- ERREUR : champs frButton/enButton introuvables sur SettingsPanelUI");
                return;
            }

            if (frProp.objectReferenceValue != null && enProp.objectReferenceValue != null)
            {
                report.AppendLine("- Sélecteur FR/EN : déjà présent (idempotent)");
                return;
            }

            SerializedProperty restartProp = so.FindProperty("restartButton");
            Button template = restartProp != null
                ? restartProp.objectReferenceValue as Button
                : null;

            Transform parent = settings.transform;
            if (template != null)
                parent = template.transform.parent != null ? template.transform.parent : settings.transform;

            GameObject row = new GameObject("LanguageRow", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(row, "Create LanguageRow");
            row.transform.SetParent(parent, false);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0f, 48f);

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            Button frBtn = CreateLangButton(row.transform, template, "BtnLangFR", "FR");
            Button enBtn = CreateLangButton(row.transform, template, "BtnLangEN", "EN");

            frProp.objectReferenceValue = frBtn;
            enProp.objectReferenceValue = enBtn;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            report.AppendLine("- Sélecteur FR/EN créé et bindé");
        }

        private static Button CreateLangButton(Transform parent, Button template, string name, string label)
        {
            GameObject go;
            if (template != null)
            {
                go = UnityEngine.Object.Instantiate(template.gameObject, parent);
                go.name = name;
                Undo.RegisterCreatedObjectUndo(go, "Clone lang button");
                ClearPersistentOnClick(go.GetComponent<Button>());
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(go, "Create lang button");
                go.transform.SetParent(parent, false);
                Image img = go.GetComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            }

            // Label
            TMP_Text tmp = go.GetComponentInChildren<TMP_Text>(true);
            Text legacy = go.GetComponentInChildren<Text>(true);
            if (tmp != null)
                tmp.text = label;
            else if (legacy != null)
                legacy.text = label;
            else
            {
                GameObject labelGo = new GameObject("Label", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(labelGo, "Create lang label");
                labelGo.transform.SetParent(go.transform, false);
                Text t = labelGo.AddComponent<Text>();
                t.text = label;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                RectTransform lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            return go.GetComponent<Button>();
        }

        private static void ClearPersistentOnClick(Button button)
        {
            if (button == null)
                return;

            SerializedObject so = new SerializedObject(button);
            SerializedProperty onClick = so.FindProperty("m_OnClick");
            if (onClick != null)
            {
                // Vide les persistent calls (m_PersistentCalls.m_Calls)
                SerializedProperty calls = onClick.FindPropertyRelative("m_PersistentCalls.m_Calls");
                if (calls != null && calls.isArray)
                {
                    calls.ClearArray();
                    so.ApplyModifiedProperties();
                }
            }

            button.onClick = new Button.ButtonClickedEvent();
        }

        private static string GetTextValue(Component comp)
        {
            if (comp is TMP_Text tmp)
                return tmp.text ?? "";
            if (comp is Text legacy)
                return legacy.text ?? "";
            return "";
        }

        private static bool IsPurelyNumeric(string text)
        {
            string t = text.Trim();
            if (t.Length == 0)
                return false;
            for (int i = 0; i < t.Length; i++)
            {
                char c = t[i];
                if (!(char.IsDigit(c) || c == '.' || c == ',' || c == '%' || c == '+' || c == '-' || c == ' '))
                    return false;
            }

            return true;
        }

        private static string SanitizeSlug(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "label";

            string lower = raw.ToLowerInvariant();
            var sb = new StringBuilder(lower.Length);
            for (int i = 0; i < lower.Length; i++)
            {
                char c = lower[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    sb.Append(c);
                else
                    sb.Append('_');
            }

            string slug = Regex.Replace(sb.ToString(), "_+", "_").Trim('_');
            return string.IsNullOrEmpty(slug) ? "label" : slug;
        }

        private static string DeduplicateKey(string key, HashSet<string> used)
        {
            if (!used.Contains(key))
                return key;

            int n = 2;
            while (used.Contains(key + "_" + n))
                n++;
            return key + "_" + n;
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }
    }
}
#endif
