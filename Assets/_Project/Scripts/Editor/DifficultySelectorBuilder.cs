#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using ChezArthur.Hub.Pages;
using ChezArthur.Localization;
using ChezArthur.Meta;
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
    /// Builder idempotent : DifficultyConfig Resources + overlay sélecteur de cran sur Hub.
    /// Leçon HF1 : purge listeners + LocalizedText clonés avant réétiquetage (non négociable).
    /// </summary>
    public static class DifficultySelectorBuilder
    {
        private const string UndoLabel = "Build Difficulty Selector";
        private const string ConfigResourcesPath = "Assets/_Project/Resources/DifficultyConfig.asset";
        private const string PanelName = "DifficultySelectorOverlay";
        private const string TablePath = "Assets/_Project/Data/Localization/Table_UI.asset";
        private const string ReportRelPath = "Audits/difficulty_selector_build.txt";

        [MenuItem("Chez Arthur/Meta/Build Difficulty Selector (Hub)")]
        public static void Build()
        {
            var report = new StringBuilder(8192);
            report.AppendLine("# Difficulty Selector Builder");
            report.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "Hub")
            {
                EditorUtility.DisplayDialog(
                    "Difficulty Selector",
                    "Ouvre Hub.unity (scène propre, sans Play) puis relance.",
                    "OK");
                report.AppendLine("- ✗ Scène Hub requise — abort");
                WriteReport(report);
                return;
            }

            report.AppendLine($"Scène : {scene.name}");

            EnsureDifficultyConfigAsset(report);

            PageAccueilUI pageUi = UnityEngine.Object.FindObjectOfType<PageAccueilUI>(true);
            if (pageUi == null)
            {
                report.AppendLine("- ✗ PageAccueilUI introuvable — abort");
                WriteReport(report);
                EditorUtility.DisplayDialog("Difficulty Selector", "PageAccueilUI introuvable.", "OK");
                return;
            }

            RectTransform pageRt = pageUi.transform as RectTransform;
            if (pageRt == null)
            {
                report.AppendLine("- ✗ PageAccueil sans RectTransform — abort");
                WriteReport(report);
                return;
            }

            Transform existing = pageRt.Find(PanelName);
            if (existing != null)
            {
                report.AppendLine($"- Panel « {PanelName} » déjà présent — zéro changement structurel");
                WireExisting(pageUi, existing.gameObject, report);
                EnsureLocKeys(report);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.AppendLine("- Scène Hub sauvegardée (re-bind)");
                WriteReport(report);
                EditorUtility.DisplayDialog(
                    "Difficulty Selector",
                    "Déjà présent — bindings vérifiés + scène sauvée.\nPlay → Lancer → panel crans.",
                    "OK");
                return;
            }

            Undo.SetCurrentGroupName(UndoLabel);
            int undoGroup = Undo.GetCurrentGroup();

            HubButtonUI lancerTemplate = FindLancerTemplate(pageUi);
            if (lancerTemplate == null)
            {
                report.AppendLine("- ✗ BtnLancerRun / HubButtonUI template introuvable — abort");
                WriteReport(report);
                EditorUtility.DisplayDialog("Difficulty Selector", "Bouton Lancer introuvable.", "OK");
                return;
            }

            GameObject panelGo = BuildPanel(pageRt, lancerTemplate, report);
            DifficultySelectorUI selector = panelGo.GetComponent<DifficultySelectorUI>();
            BindPageAccueil(pageUi, selector, report);
            EnsureLocKeys(report);

            EditorSceneManager.MarkSceneDirty(scene);
            // Évite le cas « builder OK / scène non sauvée → Lancer bypass ».
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            report.AppendLine();
            report.AppendLine("## Résumé");
            report.AppendLine("- Panel créé + bindings PageAccueilUI");
            report.AppendLine("- Scène Hub sauvegardée automatiquement");
            report.AppendLine("- Relancer le menu = idempotent (déjà présent)");

            WriteReport(report);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Difficulty Selector",
                "Build OK — Hub.unity a été sauvé.\nHierarchy : PageAccueil → DifficultySelectorOverlay\nPlay → Lancer → panel crans.",
                "OK");
        }

        private static void EnsureDifficultyConfigAsset(StringBuilder report)
        {
            EnsureFolder("Assets/_Project/Resources");
            DifficultyConfig existing = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(ConfigResourcesPath);
            if (existing != null)
            {
                report.AppendLine($"- Config déjà présente : {ConfigResourcesPath}");
                return;
            }

            DifficultyConfig created = ScriptableObject.CreateInstance<DifficultyConfig>();
            AssetDatabase.CreateAsset(created, ConfigResourcesPath);
            EditorUtility.SetDirty(created);
            AssetDatabase.SaveAssets();
            report.AppendLine($"- Créé : {ConfigResourcesPath}");
        }

        private static HubButtonUI FindLancerTemplate(PageAccueilUI pageUi)
        {
            SerializedObject so = new SerializedObject(pageUi);
            SerializedProperty prop = so.FindProperty("buttonLancerRun");
            if (prop != null && prop.objectReferenceValue is HubButtonUI hub)
                return hub;

            HubButtonUI[] all = pageUi.GetComponentsInChildren<HubButtonUI>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name.Contains("Lancer"))
                    return all[i];
            }

            return all.Length > 0 ? all[0] : null;
        }

        private static GameObject BuildPanel(
            RectTransform pageRt,
            HubButtonUI template,
            StringBuilder report)
        {
            GameObject panelGo = new GameObject(PanelName, typeof(RectTransform), typeof(DifficultySelectorUI));
            Undo.RegisterCreatedObjectUndo(panelGo, UndoLabel);
            panelGo.transform.SetParent(pageRt, false);

            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            StretchFull(panelRt);
            panelRt.SetAsLastSibling();

            // Scrim
            GameObject scrimGo = new GameObject("Scrim", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(scrimGo, UndoLabel);
            scrimGo.transform.SetParent(panelGo.transform, false);
            StretchFull(scrimGo.GetComponent<RectTransform>());
            Image scrimImg = scrimGo.GetComponent<Image>();
            scrimImg.color = new Color(0f, 0f, 0f, 0.72f);
            scrimImg.raycastTarget = true;
            Button scrimBtn = scrimGo.GetComponent<Button>();
            scrimBtn.transition = Selectable.Transition.None;

            // Card
            GameObject cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(cardGo, UndoLabel);
            cardGo.transform.SetParent(panelGo.transform, false);
            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.1f, 0.18f);
            cardRt.anchorMax = new Vector2(0.9f, 0.82f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;
            Image cardImg = cardGo.GetComponent<Image>();
            cardImg.color = new Color(0.1f, 0.1f, 0.14f, 0.98f);
            cardImg.raycastTarget = true;

            VerticalLayoutGroup vlg = cardGo.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 28, 28);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            TextMeshProUGUI titleTmp = CreateTmp(cardGo.transform, "Title", 36f, FontStyles.Bold);
            EnsureLocalizedText(titleTmp.gameObject, "ui.accueil.diff_title", "Difficulté", report);

            // Rotation : texte dynamique (univers) — pas de LocalizedText (évite d'écraser le format).
            TextMeshProUGUI rotationTmp = CreateTmp(cardGo.transform, "RotationLabel", 22f, FontStyles.Normal);
            rotationTmp.text = "Pos. 1 cette semaine : …";

            HubButtonUI[] tiers = new HubButtonUI[5];
            for (int i = 0; i < 5; i++)
            {
                GameObject clone = PrefabUtility.InstantiatePrefab(template.gameObject) as GameObject;
                if (clone == null)
                    clone = UnityEngine.Object.Instantiate(template.gameObject);

                Undo.RegisterCreatedObjectUndo(clone, UndoLabel);
                clone.name = $"BtnCran_{i}";
                clone.transform.SetParent(cardGo.transform, false);

                // HF1 non négociable : purge listeners + LocalizedText hérités du clone.
                PurgeCloneInheritance(clone, report);

                HubButtonUI hub = clone.GetComponent<HubButtonUI>();
                if (hub != null)
                {
                    hub.Variant = HubButtonUI.ButtonVariant.Secondary;
                    hub.Locked = i > 0;
                    hub.SetLabel(DifficultyConfig.LoadDefault().GetLabel(i));
                    hub.SetSubLabel(string.Empty);
                    hub.ApplyStyle();
                }

                LayoutElement le = clone.GetComponent<LayoutElement>();
                if (le == null)
                    le = clone.AddComponent<LayoutElement>();
                le.minHeight = 88f;
                le.preferredHeight = 96f;

                tiers[i] = hub;
            }

            GameObject closeGo = PrefabUtility.InstantiatePrefab(template.gameObject) as GameObject;
            if (closeGo == null)
                closeGo = UnityEngine.Object.Instantiate(template.gameObject);
            Undo.RegisterCreatedObjectUndo(closeGo, UndoLabel);
            closeGo.name = "BtnCloseDiff";
            closeGo.transform.SetParent(cardGo.transform, false);
            PurgeCloneInheritance(closeGo, report);
            HubButtonUI closeHub = closeGo.GetComponent<HubButtonUI>();
            if (closeHub != null)
            {
                closeHub.Variant = HubButtonUI.ButtonVariant.Secondary;
                closeHub.Locked = false;
                closeHub.SetLabel("Fermer");
                closeHub.SetSubLabel(string.Empty);
                closeHub.ApplyStyle();
            }

            TextMeshProUGUI closeLabel = closeGo.GetComponentInChildren<TextMeshProUGUI>(true);
            if (closeLabel != null)
                EnsureLocalizedText(closeLabel.gameObject, "ui.accueil.diff_close", "Fermer", report);

            LayoutElement closeLe = closeGo.GetComponent<LayoutElement>();
            if (closeLe == null)
                closeLe = closeGo.AddComponent<LayoutElement>();
            closeLe.minHeight = 72f;
            closeLe.preferredHeight = 80f;

            DifficultySelectorUI ui = panelGo.GetComponent<DifficultySelectorUI>();
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("panelRoot").objectReferenceValue = panelGo;
            so.FindProperty("rotationLabel").objectReferenceValue = rotationTmp;
            SerializedProperty arr = so.FindProperty("tierButtons");
            arr.arraySize = 5;
            for (int i = 0; i < 5; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = tiers[i];
            so.FindProperty("closeButton").objectReferenceValue =
                closeHub != null ? closeHub.Button : closeGo.GetComponent<Button>();
            so.FindProperty("scrimButton").objectReferenceValue = scrimBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            panelGo.SetActive(false);
            report.AppendLine($"- Créé panel « {PanelName} » sous PageAccueil");
            return panelGo;
        }

        private static void WireExisting(PageAccueilUI pageUi, GameObject panelGo, StringBuilder report)
        {
            DifficultySelectorUI selector = panelGo.GetComponent<DifficultySelectorUI>();
            if (selector == null)
            {
                report.AppendLine("- ✗ DifficultySelectorUI manquant sur panel existant");
                return;
            }

            BindPageAccueil(pageUi, selector, report);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void BindPageAccueil(
            PageAccueilUI pageUi,
            DifficultySelectorUI selector,
            StringBuilder report)
        {
            SerializedObject so = new SerializedObject(pageUi);
            SerializedProperty prop = so.FindProperty("difficultySelector");
            if (prop == null)
            {
                report.AppendLine("- ✗ Champ difficultySelector absent sur PageAccueilUI (recompile ?)");
                return;
            }

            if (prop.objectReferenceValue == selector)
            {
                report.AppendLine("- Bind PageAccueilUI.difficultySelector déjà OK");
                return;
            }

            Undo.RecordObject(pageUi, UndoLabel);
            prop.objectReferenceValue = selector;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pageUi);
            EditorSceneManager.MarkSceneDirty(pageUi.gameObject.scene);
            report.AppendLine("- Bind PageAccueilUI.difficultySelector posé");
        }

        private static void PurgeCloneInheritance(GameObject clone, StringBuilder report)
        {
            Button btn = clone.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
            }

            LocalizedText[] locs = clone.GetComponentsInChildren<LocalizedText>(true);
            int purged = 0;
            for (int i = 0; i < locs.Length; i++)
            {
                if (locs[i] == null)
                    continue;
                Undo.DestroyObjectImmediate(locs[i]);
                purged++;
            }

            if (purged > 0)
                report.AppendLine($"- Purge LocalizedText sur {clone.name} : {purged}");
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            float fontSize,
            FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = fontSize + 16f;
            le.preferredHeight = fontSize + 24f;
            return tmp;
        }

        private static void EnsureLocalizedText(
            GameObject target,
            string key,
            string frDefault,
            StringBuilder report)
        {
            LocalizedText loc = target.GetComponent<LocalizedText>();
            if (loc == null)
                loc = Undo.AddComponent<LocalizedText>(target);

            loc.EditorSetup(key, frDefault);
            EditorUtility.SetDirty(loc);
            report.AppendLine($"- LocalizedText « {key} »");
        }

        private static void EnsureLocKeys(StringBuilder report)
        {
            LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            if (table == null)
            {
                report.AppendLine($"- Table_UI absente ({TablePath}) — clés non ajoutées");
                return;
            }

            EnsureTableKey(table, "ui.accueil.diff_title", report);
            EnsureTableKey(table, "ui.accueil.diff_rotation", report);
            EnsureTableKey(table, "ui.accueil.diff_lock_hint", report);
            EnsureTableKey(table, "ui.accueil.diff_close", report);
            EditorUtility.SetDirty(table);
        }

        private static void EnsureTableKey(LocalizationTable table, string key, StringBuilder report)
        {
            var list = table.EntriesMutable;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].key == key)
                    return;
            }

            list.Add(new LocalizationEntry { key = key, english = "" });
            report.AppendLine($"- Clé Table_UI ajoutée : {key}");
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
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

        private static void WriteReport(StringBuilder report)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "difficulty_selector_build.txt");
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
            Debug.Log($"[DifficultySelectorBuilder] Rapport : {path}");
        }
    }
}
#endif
