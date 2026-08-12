#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using ChezArthur.Hub;
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
    /// Builder idempotent MT2-G4 : header Saison + page overlay + récap gate.
    /// Leçon HF1 : purge listeners + LocalizedText sur clones (non négociable).
    /// </summary>
    public static class SeasonPageBuilder
    {
        private const string UndoLabel = "Build Season Page";
        private const string PageRootName = "SeasonPageOverlay";
        private const string RecapRootName = "SeasonRecapOverlay";
        private const string SeasonBtnName = "BtnSaison";
        private const string TablePath = "Assets/_Project/Data/Localization/Table_UI.asset";

        [MenuItem("Chez Arthur/Meta/Build Season Page (Hub)")]
        public static void Build()
        {
            BuildInternal(exitBatch: false);
        }

        /// <summary>
        /// Entrée batchmode : ouvre Hub, build, sauve, quitte.
        /// </summary>
        public static void BuildBatch()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hub.unity");
            BuildInternal(exitBatch: true);
        }

        private static void BuildInternal(bool exitBatch)
        {
            var report = new StringBuilder(8192);
            report.AppendLine("# Season Page Builder");
            report.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "Hub")
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "Season Page",
                        "Ouvre Hub.unity (scène propre, sans Play) puis relance.",
                        "OK");
                }

                report.AppendLine("- ✗ Scène Hub requise — abort");
                WriteReport(report);
                if (exitBatch)
                    EditorApplication.Exit(1);
                return;
            }

            report.AppendLine($"Scène : {scene.name}");

            HubHeaderUI header = UnityEngine.Object.FindObjectOfType<HubHeaderUI>(true);
            HubManager hub = UnityEngine.Object.FindObjectOfType<HubManager>(true);
            if (header == null || hub == null)
            {
                report.AppendLine("- ✗ HubHeaderUI ou HubManager introuvable — abort");
                WriteReport(report);
                if (exitBatch)
                    EditorApplication.Exit(1);
                return;
            }

            Canvas canvas = header.GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                report.AppendLine("- ✗ Canvas parent introuvable — abort");
                WriteReport(report);
                if (exitBatch)
                    EditorApplication.Exit(1);
                return;
            }

            Undo.SetCurrentGroupName(UndoLabel);
            int undoGroup = Undo.GetCurrentGroup();

            SeasonRecapUI recap = EnsureRecapOverlay(canvas.transform, report);
            SeasonPageUI page = EnsureSeasonPage(canvas.transform, recap, report);
            PatchHeader(header, page, report);
            BindHubManager(hub, recap, report);
            EnsureLocKeys(report);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            report.AppendLine();
            report.AppendLine("## Résumé");
            report.AppendLine("- Header / page / récap câblés");
            report.AppendLine("- Hub.unity sauvegardé");
            report.AppendLine("- Relancer = idempotent");

            WriteReport(report);
            AssetDatabase.SaveAssets();
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Season Page",
                    "Build OK — Hub sauvé.\nRapport : Audits/season_page_build.txt",
                    "OK");
            }

            if (exitBatch)
                EditorApplication.Exit(0);
        }

        private static void PatchHeader(HubHeaderUI header, SeasonPageUI page, StringBuilder report)
        {
            SerializedObject so = new SerializedObject(header);
            SerializedProperty bestProp = so.FindProperty("bestStageText");
            TextMeshProUGUI bestTmp = bestProp?.objectReferenceValue as TextMeshProUGUI;
            if (bestTmp != null && bestTmp.gameObject.activeSelf)
            {
                Undo.RecordObject(bestTmp.gameObject, UndoLabel);
                bestTmp.gameObject.SetActive(false);
                report.AppendLine("- Record bestStage désactivé (D8)");
            }
            else
            {
                report.AppendLine("- Record déjà masqué / absent");
            }

            Transform headerRt = header.transform;
            Transform existingBtn = FindDeepChild(headerRt, SeasonBtnName);
            Button seasonBtn;
            TextMeshProUGUI scoreTmp;

            if (existingBtn != null)
            {
                report.AppendLine("- BtnSaison déjà présent");
                seasonBtn = existingBtn.GetComponent<Button>();
                scoreTmp = existingBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            else
            {
                Button template = header.GetComponentInChildren<Button>(true);
                GameObject clone;
                if (template != null)
                {
                    clone = PrefabUtility.InstantiatePrefab(template.gameObject) as GameObject;
                    if (clone == null)
                        clone = UnityEngine.Object.Instantiate(template.gameObject);
                }
                else
                {
                    clone = new GameObject(SeasonBtnName, typeof(RectTransform), typeof(Image), typeof(Button));
                }

                Undo.RegisterCreatedObjectUndo(clone, UndoLabel);
                clone.name = SeasonBtnName;
                clone.transform.SetParent(headerRt, false);
                PurgeCloneInheritance(clone, report);

                RectTransform rt = clone.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.35f, 0.15f);
                rt.anchorMax = new Vector2(0.65f, 0.85f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                seasonBtn = clone.GetComponent<Button>();
                if (seasonBtn == null)
                    seasonBtn = clone.AddComponent<Button>();

                scoreTmp = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                if (scoreTmp == null)
                {
                    GameObject labelGo = new GameObject("ScoreLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                    Undo.RegisterCreatedObjectUndo(labelGo, UndoLabel);
                    labelGo.transform.SetParent(clone.transform, false);
                    StretchFull(labelGo.GetComponent<RectTransform>());
                    scoreTmp = labelGo.GetComponent<TextMeshProUGUI>();
                    scoreTmp.alignment = TextAlignmentOptions.Center;
                    scoreTmp.fontSize = 28f;
                    scoreTmp.color = Color.white;
                    scoreTmp.raycastTarget = false;
                }

                scoreTmp.text = "0";
                EnsureLocalizedText(scoreTmp.gameObject, "ui.saison.header_score", "0", report);
                // Score dynamique : pas de LT qui écrase — on retire LT du score.
                LocalizedText loc = scoreTmp.GetComponent<LocalizedText>();
                if (loc != null)
                    Undo.DestroyObjectImmediate(loc);

                // Titre secondaire
                GameObject titleGo = new GameObject("SeasonTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(titleGo, UndoLabel);
                titleGo.transform.SetParent(clone.transform, false);
                RectTransform titleRt = titleGo.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 0.55f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.offsetMin = Vector2.zero;
                titleRt.offsetMax = Vector2.zero;
                TextMeshProUGUI titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.fontSize = 22f;
                titleTmp.color = Color.white;
                titleTmp.raycastTarget = false;
                EnsureLocalizedText(titleGo, "ui.saison.header_btn", "Saison", report);

                scoreTmp.rectTransform.anchorMin = new Vector2(0f, 0f);
                scoreTmp.rectTransform.anchorMax = new Vector2(1f, 0.55f);
                scoreTmp.rectTransform.offsetMin = Vector2.zero;
                scoreTmp.rectTransform.offsetMax = Vector2.zero;

                report.AppendLine("- BtnSaison créé au centre du header");
            }

            so.FindProperty("seasonButton").objectReferenceValue = seasonBtn;
            so.FindProperty("seasonButtonScoreText").objectReferenceValue = scoreTmp;
            so.FindProperty("seasonPage").objectReferenceValue = page;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(header);
            report.AppendLine("- Bind HubHeaderUI saison posé");
        }

        private static SeasonPageUI EnsureSeasonPage(
            Transform canvasTf,
            SeasonRecapUI recap,
            StringBuilder report)
        {
            Transform existing = canvasTf.Find(PageRootName);
            if (existing != null)
            {
                report.AppendLine("- SeasonPageOverlay déjà présent");
                SeasonPageUI ui = existing.GetComponent<SeasonPageUI>();
                BindPageRecapRef(ui, recap, report);
                return ui;
            }

            GameObject root = new GameObject(PageRootName, typeof(RectTransform), typeof(SeasonPageUI));
            Undo.RegisterCreatedObjectUndo(root, UndoLabel);
            root.transform.SetParent(canvasTf, false);
            StretchFull(root.GetComponent<RectTransform>());
            root.transform.SetAsLastSibling();

            // Scrim
            GameObject scrim = CreateUiObject("Scrim", root.transform, typeof(Image), typeof(Button));
            StretchFull(scrim.GetComponent<RectTransform>());
            Image scrimImg = scrim.GetComponent<Image>();
            scrimImg.color = new Color(0f, 0f, 0f, 0.75f);
            Button scrimBtn = scrim.GetComponent<Button>();
            scrimBtn.transition = Selectable.Transition.None;

            // Panel
            GameObject panel = CreateUiObject("Panel", root.transform, typeof(Image), typeof(VerticalLayoutGroup));
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.05f, 0.08f);
            panelRt.anchorMax = new Vector2(0.95f, 0.92f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.09f, 0.09f, 0.12f, 0.98f);
            VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 24, 24);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            TextMeshProUGUI score = CreateLabel(panel.transform, "ScoreText", 34f, true);
            TextMeshProUGUI progress = CreateLabel(panel.transform, "ProgressText", 24f, false);
            TextMeshProUGUI stats = CreateLabel(panel.transform, "StatsText", 22f, false);
            TextMeshProUGUI missing = CreateLabel(panel.transform, "MissingText", 22f, false);
            TextMeshProUGUI countdown = CreateLabel(panel.transform, "CountdownText", 22f, false);

            // Scroll track
            GameObject scrollGo = CreateUiObject("TrackScroll", panel.transform, typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.GetComponent<LayoutElement>().flexibleHeight = 1f;
            scrollGo.GetComponent<LayoutElement>().minHeight = 360f;
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject viewport = CreateUiObject("Viewport", scrollGo.transform, typeof(RectTransform), typeof(Image), typeof(Mask));
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = CreateUiObject("Content", viewport.transform, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup contentVlg = content.GetComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 8f;
            contentVlg.childControlWidth = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childControlHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;

            SeasonTierEntryUI[] entries = new SeasonTierEntryUI[12];
            for (int i = 0; i < 12; i++)
                entries[i] = CreateTierEntry(content.transform, i, report);

            TextMeshProUGUI prestige = CreateLabel(panel.transform, "PrestigeLabel", 22f, false);

            Button prestigeBtn = CreateTextButton(panel.transform, "BtnClaimPrestige", "Réclamer prestige", "ui.saison.claim_prestige", report);
            Button reviewBtn = CreateTextButton(panel.transform, "BtnReviewRecap", "Revoir le dernier bilan", "ui.saison.revoir_bilan", report);
            Button closeBtn = CreateTextButton(panel.transform, "BtnCloseSeason", "Fermer", "ui.saison.fermer", report);

            // Scrim ferme la page
            SeasonPageUI pageUi = root.GetComponent<SeasonPageUI>();
            SerializedObject so = new SerializedObject(pageUi);
            so.FindProperty("panelRoot").objectReferenceValue = root;
            so.FindProperty("scoreText").objectReferenceValue = score;
            so.FindProperty("progressText").objectReferenceValue = progress;
            so.FindProperty("statsText").objectReferenceValue = stats;
            so.FindProperty("missingText").objectReferenceValue = missing;
            so.FindProperty("countdownText").objectReferenceValue = countdown;
            so.FindProperty("trackScroll").objectReferenceValue = scroll;
            SerializedProperty arr = so.FindProperty("tierEntries");
            arr.arraySize = 12;
            for (int i = 0; i < 12; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            so.FindProperty("prestigeLabel").objectReferenceValue = prestige;
            so.FindProperty("prestigeClaimButton").objectReferenceValue = prestigeBtn;
            so.FindProperty("reviewRecapButton").objectReferenceValue = reviewBtn;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("seasonRecap").objectReferenceValue = recap;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Wire scrim close at runtime via page closeButton pattern — add listener component-free:
            // SeasonPageUI Close is private API via closeButton; add second close on scrim in code:
            // Use UnityEvent persistent via SerializedObject on Button
            SerializedObject scrimSo = new SerializedObject(scrimBtn);
            // Runtime: SeasonPageUI listens only closeButton — duplicate ref by also assigning close to scrim via a tiny helper
            // Simpler: set closeButton also used; add SeasonPageScrimClose equivalent — assign closeButton and also wire scrim in OnEnable of page.
            // Patch SeasonPageUI to accept optional scrim — already have closeButton. Add serialized scrim in page? For speed, set closeButton = closeBtn and leave scrim without close OR make scrim invoke same.
            // Store scrim as closeButton alternative: use UnityEditor events
            UnityEditor.Events.UnityEventTools.AddPersistentListener(scrimBtn.onClick, pageUi.Close);

            root.SetActive(false);
            report.AppendLine("- SeasonPageOverlay créé (12 paliers)");
            return pageUi;
        }

        private static void BindPageRecapRef(SeasonPageUI ui, SeasonRecapUI recap, StringBuilder report)
        {
            if (ui == null)
                return;
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("seasonRecap").objectReferenceValue = recap;
            so.ApplyModifiedProperties();
            report.AppendLine("- Re-bind SeasonPageUI.seasonRecap");
        }

        private static SeasonTierEntryUI CreateTierEntry(Transform parent, int index, StringBuilder report)
        {
            GameObject row = CreateUiObject($"Tier_{index + 1}", parent, typeof(Image), typeof(LayoutElement), typeof(CanvasGroup), typeof(SeasonTierEntryUI));
            row.GetComponent<LayoutElement>().minHeight = 88f;
            row.GetComponent<LayoutElement>().preferredHeight = 96f;
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

            TextMeshProUGUI title = CreateLabel(row.transform, "Title", 22f, true);
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.03f, 0.45f);
            titleRt.anchorMax = new Vector2(0.7f, 0.95f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            TextMeshProUGUI reward = CreateLabel(row.transform, "Reward", 18f, false);
            RectTransform rewardRt = reward.rectTransform;
            rewardRt.anchorMin = new Vector2(0.03f, 0.05f);
            rewardRt.anchorMax = new Vector2(0.55f, 0.45f);
            rewardRt.offsetMin = Vector2.zero;
            rewardRt.offsetMax = Vector2.zero;

            TextMeshProUGUI status = CreateLabel(row.transform, "Status", 18f, false);
            RectTransform statusRt = status.rectTransform;
            statusRt.anchorMin = new Vector2(0.55f, 0.05f);
            statusRt.anchorMax = new Vector2(0.72f, 0.45f);
            statusRt.offsetMin = Vector2.zero;
            statusRt.offsetMax = Vector2.zero;

            Button claim = CreateTextButton(row.transform, "BtnClaim", "Réclamer", "ui.saison.reclamer", report);
            RectTransform claimRt = claim.transform as RectTransform;
            claimRt.anchorMin = new Vector2(0.74f, 0.15f);
            claimRt.anchorMax = new Vector2(0.97f, 0.85f);
            claimRt.offsetMin = Vector2.zero;
            claimRt.offsetMax = Vector2.zero;
            LayoutElement claimLe = claim.GetComponent<LayoutElement>();
            if (claimLe != null)
                UnityEngine.Object.DestroyImmediate(claimLe);

            SeasonTierEntryUI entry = row.GetComponent<SeasonTierEntryUI>();
            entry.BindIndex(index);
            SerializedObject so = new SerializedObject(entry);
            so.FindProperty("tierIndex").intValue = index;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("rewardText").objectReferenceValue = reward;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("claimButton").objectReferenceValue = claim;
            so.FindProperty("canvasGroup").objectReferenceValue = row.GetComponent<CanvasGroup>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return entry;
        }

        private static SeasonRecapUI EnsureRecapOverlay(Transform canvasTf, StringBuilder report)
        {
            Transform existing = canvasTf.Find(RecapRootName);
            if (existing != null)
            {
                report.AppendLine("- SeasonRecapOverlay déjà présent");
                return existing.GetComponent<SeasonRecapUI>();
            }

            GameObject root = new GameObject(RecapRootName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(SeasonRecapUI));
            Undo.RegisterCreatedObjectUndo(root, UndoLabel);
            root.transform.SetParent(canvasTf, false);
            StretchFull(root.GetComponent<RectTransform>());
            root.transform.SetAsLastSibling();

            Canvas c = root.GetComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 500;
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject scrim = CreateUiObject("Scrim", root.transform, typeof(Image));
            StretchFull(scrim.GetComponent<RectTransform>());
            scrim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            scrim.GetComponent<Image>().raycastTarget = true;

            GameObject card = CreateUiObject("Card", root.transform, typeof(Image), typeof(VerticalLayoutGroup));
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.1f, 0.22f);
            cardRt.anchorMax = new Vector2(0.9f, 0.78f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 1f);
            VerticalLayoutGroup vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(28, 28, 28, 28);
            vlg.spacing = 14f;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;

            TextMeshProUGUI title = CreateLabel(card.transform, "Title", 36f, true);
            TextMeshProUGUI body = CreateLabel(card.transform, "Body", 24f, false);
            body.enableWordWrapping = true;
            TextMeshProUGUI rewards = CreateLabel(card.transform, "Rewards", 24f, false);
            rewards.enableWordWrapping = true;

            Button primary = CreateTextButton(card.transform, "BtnPrimary", "Continuer", "ui.saison.recap_continuer", report);
            TextMeshProUGUI primaryLabel = primary.GetComponentInChildren<TextMeshProUGUI>(true);

            SeasonRecapUI ui = root.GetComponent<SeasonRecapUI>();
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("panelRoot").objectReferenceValue = root;
            so.FindProperty("rootCanvas").objectReferenceValue = c;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.FindProperty("rewardsText").objectReferenceValue = rewards;
            so.FindProperty("primaryButton").objectReferenceValue = primary;
            so.FindProperty("primaryButtonLabel").objectReferenceValue = primaryLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            report.AppendLine("- SeasonRecapOverlay créé (sorting 500)");
            return ui;
        }

        private static void BindHubManager(HubManager hub, SeasonRecapUI recap, StringBuilder report)
        {
            SerializedObject so = new SerializedObject(hub);
            SerializedProperty prop = so.FindProperty("seasonRecapUI");
            if (prop == null)
            {
                report.AppendLine("- ✗ HubManager.seasonRecapUI absent (recompile ?)");
                return;
            }

            prop.objectReferenceValue = recap;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hub);
            report.AppendLine("- Bind HubManager.seasonRecapUI");
        }

        private static Button CreateTextButton(
            Transform parent,
            string name,
            string fr,
            string key,
            StringBuilder report)
        {
            GameObject go = CreateUiObject(name, parent, typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<LayoutElement>().minHeight = 72f;
            go.GetComponent<LayoutElement>().preferredHeight = 80f;
            go.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.7f, 1f);
            Button btn = go.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();

            TextMeshProUGUI label = CreateLabel(go.transform, "Label", 26f, true);
            StretchFull(label.rectTransform);
            label.raycastTarget = false;
            EnsureLocalizedText(label.gameObject, key, fr, report);
            return btn;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, float size, bool bold)
        {
            GameObject go = CreateUiObject(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.GetComponent<LayoutElement>().minHeight = size + 12f;
            go.GetComponent<LayoutElement>().preferredHeight = size + 18f;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            go.transform.SetParent(parent, false);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == typeof(RectTransform))
                    continue;
                if (go.GetComponent(components[i]) == null)
                    go.AddComponent(components[i]);
            }

            return go;
        }

        private static void PurgeCloneInheritance(GameObject clone, StringBuilder report)
        {
            Button btn = clone.GetComponent<Button>();
            if (btn != null)
                btn.onClick = new Button.ButtonClickedEvent();

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

        private static void EnsureLocalizedText(GameObject target, string key, string frDefault, StringBuilder report)
        {
            LocalizedText loc = target.GetComponent<LocalizedText>();
            if (loc == null)
                loc = Undo.AddComponent<LocalizedText>(target);
            loc.EditorSetup(key, frDefault);
            EditorUtility.SetDirty(loc);
        }

        private static void EnsureLocKeys(StringBuilder report)
        {
            LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            if (table == null)
            {
                report.AppendLine("- Table_UI absente — clés non ajoutées");
                return;
            }

            string[] keys =
            {
                "ui.saison.header_btn", "ui.saison.score", "ui.saison.dernier_palier", "ui.saison.stats",
                "ui.saison.manque", "ui.saison.manque_prestige", "ui.saison.temps_restant", "ui.saison.temps_jh",
                "ui.saison.temps_hm", "ui.saison.prestige", "ui.saison.claim_prestige", "ui.saison.revoir_bilan",
                "ui.saison.fermer", "ui.saison.reclamer", "ui.saison.palier_titre", "ui.saison.palier_reward_tals",
                "ui.saison.palier_reward_lr", "ui.saison.etat_reclame", "ui.saison.etat_verrouille",
                "ui.saison.recap_titre", "ui.saison.recap_corps", "ui.saison.recap_rewards_titre",
                "ui.saison.recap_reward_tals", "ui.saison.recap_reward_lr", "ui.saison.recap_continuer",
                "ui.saison.recap_fermer"
            };

            for (int i = 0; i < keys.Length; i++)
                EnsureTableKey(table, keys[i], report);

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
            report.AppendLine($"- Clé Table_UI : {key}");
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void WriteReport(StringBuilder report)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "season_page_build.txt");
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
            Debug.Log($"[SeasonPageBuilder] Rapport : {path}");
        }
    }
}
#endif
