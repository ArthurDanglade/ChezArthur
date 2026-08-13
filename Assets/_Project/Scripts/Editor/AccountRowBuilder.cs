#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
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
    /// Ajoute la ligne « Compte » sous LanguageRow dans SettingsPanel (gabarit + purge HF1).
    /// </summary>
    public static class AccountRowBuilder
    {
        private const string TablePath = "Assets/_Project/Data/Localization/Table_UI.asset";

        [MenuItem("Chez Arthur/Backend/Build Account Row (scène ouverte)")]
        public static void Build()
        {
            var report = new StringBuilder(2048);
            report.AppendLine("# Account Row Builder");
            report.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Scène : {SceneManager.GetActiveScene().name}");
            report.AppendLine();

            SettingsPanelUI settings = UnityEngine.Object.FindObjectOfType<SettingsPanelUI>(true);
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Account Row", "Aucun SettingsPanelUI dans la scène ouverte.", "OK");
                return;
            }

            LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            int keysAdded = 0;
            if (table != null)
            {
                keysAdded += EnsureKey(table, "ui.compte.non_lie");
                keysAdded += EnsureKey(table, "ui.compte.lie");
                keysAdded += EnsureKey(table, "ui.compte.lie_anon");
                keysAdded += EnsureKey(table, "ui.compte.lier");
                keysAdded += EnsureKey(table, "ui.compte.lier_editor");
                keysAdded += EnsureKey(table, "ui.compte.lier_offline");
                keysAdded += EnsureKey(table, "ui.compte.lier_hint");
                keysAdded += EnsureKey(table, "ui.compte.bascule_confirm");
                keysAdded += EnsureKey(table, "ui.compte.bascule_hint");
                keysAdded += EnsureKey(table, "ui.compte.basculer");
                EditorUtility.SetDirty(table);
                report.AppendLine($"Clés Table_UI ajoutées : {keysAdded}");
            }
            else
            {
                report.AppendLine("WARN — Table_UI introuvable, clés Loc non écrites.");
            }

            SerializedObject so = new SerializedObject(settings);
            SerializedProperty statusProp = so.FindProperty("accountStatusText");
            SerializedProperty linkProp = so.FindProperty("linkButton");
            if (statusProp == null || linkProp == null)
            {
                report.AppendLine("ERREUR — champs accountStatusText / linkButton absents (recompile ?).");
                WriteReport(report);
                return;
            }

            if (statusProp.objectReferenceValue != null && linkProp.objectReferenceValue != null)
            {
                report.AppendLine("Ligne Compte déjà bindée — heal labels + purge LocalizedText.");
                Button existingBtn = linkProp.objectReferenceValue as Button;
                if (existingBtn != null)
                    PurgeLocalizedTexts(existingBtn.gameObject);
                AssetDatabase.SaveAssets();
                WriteReport(report);
                EditorUtility.DisplayDialog("Account Row", "Déjà présent — heal OK.\nVoir Audits/account_row_build.txt", "OK");
                return;
            }

            SerializedProperty frProp = so.FindProperty("frButton");
            Button frBtn = frProp != null ? frProp.objectReferenceValue as Button : null;
            SerializedProperty restartProp = so.FindProperty("restartButton");
            Button template = restartProp != null ? restartProp.objectReferenceValue as Button : null;

            Transform parent = settings.transform;
            Transform languageRow = null;
            if (frBtn != null && frBtn.transform.parent != null)
            {
                languageRow = frBtn.transform.parent;
                parent = languageRow.parent != null ? languageRow.parent : settings.transform;
            }
            else if (template != null && template.transform.parent != null)
            {
                parent = template.transform.parent;
            }

            GameObject row = new GameObject("AccountRow", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(row, "Create AccountRow");
            row.layer = 5;
            row.transform.SetParent(parent, false);
            if (languageRow != null)
                row.transform.SetSiblingIndex(languageRow.GetSiblingIndex() + 1);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 0f);
            rowRt.anchorMax = new Vector2(1f, 0f);
            rowRt.pivot = new Vector2(0.5f, 0f);
            float y = languageRow != null
                ? ((RectTransform)languageRow).anchoredPosition.y + 64f
                : 76f;
            rowRt.anchoredPosition = new Vector2(0f, y);
            rowRt.sizeDelta = new Vector2(0f, 72f);

            VerticalLayoutGroup vlg = row.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(8, 8, 4, 4);

            // Statut
            GameObject statusGo = new GameObject("AccountStatus", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(statusGo, "AccountStatus");
            statusGo.layer = 5;
            statusGo.transform.SetParent(row.transform, false);
            TMP_Text statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "Compte non lié — progression sur cet appareil uniquement";
            statusTmp.fontSize = 22f;
            statusTmp.alignment = TextAlignmentOptions.Center;
            statusTmp.color = Color.white;
            LayoutElement statusLe = statusGo.AddComponent<LayoutElement>();
            statusLe.preferredHeight = 28f;
            statusLe.flexibleWidth = 1f;

            // Bouton (clone gabarit)
            int purged = 0;
            Button linkBtn;
            if (template != null)
            {
                GameObject go = UnityEngine.Object.Instantiate(template.gameObject, row.transform);
                go.name = "BtnLinkGoogle";
                Undo.RegisterCreatedObjectUndo(go, "Clone link button");
                linkBtn = go.GetComponent<Button>();
                ClearPersistentOnClick(linkBtn);
                purged += PurgeLocalizedTexts(go);
                AssertLabel(go, "Lier à Google");
            }
            else
            {
                GameObject go = new GameObject("BtnLinkGoogle", typeof(RectTransform), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(go, "Create link button");
                go.layer = 5;
                go.transform.SetParent(row.transform, false);
                go.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.55f, 1f);
                linkBtn = go.GetComponent<Button>();
                GameObject labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                TMP_Text tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "Lier à Google";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 24f;
                RectTransform lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            LayoutElement btnLe = linkBtn.GetComponent<LayoutElement>();
            if (btnLe == null)
                btnLe = linkBtn.gameObject.AddComponent<LayoutElement>();
            btnLe.preferredHeight = 48f;
            btnLe.flexibleWidth = 1f;

            statusProp.objectReferenceValue = statusTmp;
            linkProp.objectReferenceValue = linkBtn;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            report.AppendLine("AccountRow créée sous LanguageRow (ou parent boutons).");
            report.AppendLine($"LocalizedText parasites purgés : {purged}");
            report.AppendLine("Refs SettingsPanelUI bindées (accountStatusText, linkButton).");
            WriteReport(report);
            EditorUtility.DisplayDialog(
                "Account Row",
                "Ligne Compte créée.\nSauvegarde la scène (commit séparé).\nVoir Audits/account_row_build.txt",
                "OK");
        }

        private static int EnsureKey(LocalizationTable table, string key)
        {
            if (table.EntriesMutable == null)
                return 0;
            for (int i = 0; i < table.EntriesMutable.Count; i++)
            {
                if (table.EntriesMutable[i] != null
                    && string.Equals(table.EntriesMutable[i].key, key, StringComparison.Ordinal))
                    return 0;
            }

            table.EntriesMutable.Add(new LocalizationEntry { key = key, english = "" });
            return 1;
        }

        private static void ClearPersistentOnClick(Button button)
        {
            if (button == null)
                return;
            button.onClick = new Button.ButtonClickedEvent();
        }

        private static int PurgeLocalizedTexts(GameObject root)
        {
            if (root == null)
                return 0;
            LocalizedText[] parasites = root.GetComponentsInChildren<LocalizedText>(true);
            int count = 0;
            for (int i = 0; i < parasites.Length; i++)
            {
                if (parasites[i] == null)
                    continue;
                Undo.DestroyObjectImmediate(parasites[i]);
                count++;
            }

            return count;
        }

        private static void AssertLabel(GameObject go, string label)
        {
            TMP_Text tmp = go.GetComponentInChildren<TMP_Text>(true);
            Text legacy = go.GetComponentInChildren<Text>(true);
            if (tmp != null)
            {
                Undo.RecordObject(tmp, "Assert account label");
                tmp.text = label;
                EditorUtility.SetDirty(tmp);
            }
            else if (legacy != null)
            {
                Undo.RecordObject(legacy, "Assert account label");
                legacy.text = label;
                EditorUtility.SetDirty(legacy);
            }
        }

        private static void WriteReport(StringBuilder report)
        {
            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string path = Path.Combine(auditsRoot, "account_row_build.txt");
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
            Debug.Log("[AccountRowBuilder] Rapport : " + path);
            EditorUtility.RevealInFinder(path);
        }
    }
}
#endif
