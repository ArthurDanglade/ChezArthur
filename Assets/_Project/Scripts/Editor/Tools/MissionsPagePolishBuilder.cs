#if UNITY_EDITOR
using System.Text;
using ChezArthur.Hub;
using ChezArthur.Hub.Pages.Missions;
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
    /// Polish page Missions — bandeau Accueil-only, clearance header, cartes lisibles.
    /// Idempotent, Undo-safe. DRY RUN / APPLIQUER (harnais v2).
    /// </summary>
    public static class MissionsPagePolishBuilder
    {
        private const string UndoLabel = "Missions Page Polish";
        private const string PageMissionsName = "PageMissions";
        private const float EntryHeight = 208f;
        private const float TabBarHeight = 72f;
        private const float TalsIconSize = 36f;
        private const float ProgressTrackH = 16f;

        [MenuItem("Chez Arthur/Refonte Hub/Page Missions — Polish lisibilité (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Page Missions — Polish lisibilité (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Polish Page Missions",
                    "Va : masquer TopUtility hors Accueil, caler MissionsRoot sous le header, "
                    + "reconstruire les cartes (layout lisible + icône Tals + carte cliquable).\n\n"
                    + "Ctrl+S Hub ensuite. Continuer ?",
                    "Appliquer",
                    "Annuler"))
                return;

            Run(apply: true);
        }

        private static void Run(bool apply)
        {
            var log = new StringBuilder(8192);
            string mode = apply ? "APPLIQUER" : "DRY RUN";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" MissionsPagePolishBuilder — {mode}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int todo = 0;
            int conforme = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "Hub")
            {
                Debug.LogError("[MissionsPagePolishBuilder] Ouvre Hub.unity.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine();

            HubManager hub = Object.FindObjectOfType<HubManager>();
            Transform page = FindDeep(scene, PageMissionsName);
            Transform root = page != null ? page.Find("MissionsRoot") : null;

            // —— 1. TopUtility Accueil-only ——
            log.AppendLine("## TopUtility Accueil-only");
            ProcessTopUtilityVisibility(hub, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— 2. PageHeaderClearance ——
            log.AppendLine("## PageHeaderClearance (MissionsRoot)");
            ProcessClearance(root, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— 3. Cartes + spacing ——
            log.AppendLine("## Cartes mission (layout + Tals + clic)");
            if (page == null || root == null)
            {
                failed++;
                log.AppendLine("- ✗ PageMissions / MissionsRoot introuvable");
            }
            else
            {
                ProcessCards(page, root, apply, log, ref todo, ref conforme, ref failed);
            }

            log.AppendLine();
            AppendCounter(log, todo, conforme, failed);

            if (apply)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
            }

            Debug.Log(log.ToString());
        }

        private static void ProcessTopUtilityVisibility(
            HubManager hub,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (hub == null)
            {
                failed++;
                log.AppendLine("- ✗ HubManager introuvable");
                return;
            }

            // HubManager est racine scène (hors HubCanvas) — chercher dans toute la scène.
            TopUtilityPageVisibility vis = hub.GetComponent<TopUtilityPageVisibility>();
            Transform row = FindDeep(SceneManager.GetActiveScene(), "TopUtilityRow");
            if (row == null)
            {
                failed++;
                log.AppendLine("- ✗ TopUtilityRow introuvable dans la scène");
                return;
            }

            log.AppendLine($"- TopUtilityRow : `{GetPath(row)}` ✓");

            if (vis != null)
            {
                conforme++;
                log.AppendLine("- TopUtilityPageVisibility déjà présent ✓");
            }
            else if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] AJOUTER TopUtilityPageVisibility sur HubManager — À FAIRE");
            }
            else
            {
                vis = Undo.AddComponent<TopUtilityPageVisibility>(hub.gameObject);
                SerializedObject so = new SerializedObject(vis);
                so.FindProperty("hub").objectReferenceValue = hub;
                so.FindProperty("topUtilityRow").objectReferenceValue = row.gameObject;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(vis);
                conforme++;
                log.AppendLine("- TopUtilityPageVisibility ajouté + câblé ✓");
            }

            // Runtime EnsureOn au Start HubManager — vérifier code
            string hubSrc = System.IO.File.ReadAllText(
                "Assets/_Project/Scripts/Hub/HubManager.cs");
            if (hubSrc.Contains("TopUtilityPageVisibility.EnsureOn"))
            {
                conforme++;
                log.AppendLine("- HubManager.Start appelle EnsureOn ✓");
            }
            else
            {
                failed++;
                log.AppendLine("- ✗ HubManager.Start sans TopUtilityPageVisibility.EnsureOn");
            }
        }

        private static void ProcessClearance(
            Transform root,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (root == null)
            {
                failed++;
                log.AppendLine("- ✗ MissionsRoot absent");
                return;
            }

            PageHeaderClearance clearance = root.GetComponent<PageHeaderClearance>();
            if (clearance != null)
            {
                conforme++;
                log.AppendLine("- PageHeaderClearance déjà sur MissionsRoot ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    "- [DRY] AJOUTER PageHeaderClearance (header + nav, gap Space5) — À FAIRE");
                return;
            }

            clearance = Undo.AddComponent<PageHeaderClearance>(root.gameObject);
            Transform header = FindDeep(root.root, "Header");
            Transform nav = FindDeep(root.root, "NavigationBar");
            clearance.Bind(
                header as RectTransform,
                nav as RectTransform);
            EditorUtility.SetDirty(clearance);
            conforme++;
            log.AppendLine("- PageHeaderClearance câblé (Header + NavigationBar) ✓");
        }

        private static void ProcessCards(
            Transform page,
            Transform root,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteL = RoundedRectSpriteGenerator.LoadSpriteL();
            Sprite tals = UiGen.LoadSprite(UiTheme.SpriteCoin);
            if (spriteS == null || spriteM == null || spriteL == null)
            {
                failed++;
                log.AppendLine("- ✗ RoundedRect manquants");
                return;
            }

            if (tals == null)
            {
                failed++;
                log.AppendLine($"- ✗ Sprite `{UiTheme.SpriteCoin}` introuvable");
                return;
            }

            MissionsPageUI pageUi = page.GetComponent<MissionsPageUI>();
            if (pageUi == null)
            {
                failed++;
                log.AppendLine("- ✗ MissionsPageUI absent");
                return;
            }

            // Détecte ancien layout (ClaimButton enfant = legacy)
            Transform oldTemplate = root.Find("MissionScroll/Viewport/Content/MissionEntryTemplate");
            bool needsRebuild = oldTemplate == null
                                || oldTemplate.Find("Body/RowTitle") == null;

            if (!needsRebuild)
            {
                conforme++;
                log.AppendLine("- Cartes déjà en layout polish (RowTitle) ✓");
                TuneRootSpacing(root, apply, log, ref todo, ref conforme);
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] REBUILD LayerBonusRow + MissionEntryTemplate (layout H) — À FAIRE");
                todo++;
                log.AppendLine("- [DRY] Spacing root Space4 / content Space3 + TabBar 72 — À FAIRE");
                return;
            }

            // TabBar height
            Transform tabBarTx = root.Find("TabBar");
            if (tabBarTx != null)
            {
                LayoutElement tabLe = tabBarTx.GetComponent<LayoutElement>();
                if (tabLe == null)
                    tabLe = Undo.AddComponent<LayoutElement>(tabBarTx.gameObject);
                Undo.RecordObject(tabLe, UndoLabel);
                tabLe.minHeight = TabBarHeight;
                tabLe.preferredHeight = TabBarHeight;
            }

            TuneRootSpacing(root, apply: true, log, ref todo, ref conforme);

            // Rebuild bonus
            Transform bonusOld = root.Find("LayerBonusRow");
            int bonusSibling = bonusOld != null ? bonusOld.GetSiblingIndex() : 1;
            if (bonusOld != null)
                Undo.DestroyObjectImmediate(bonusOld.gameObject);

            RectTransform bonusRoot = CreateBonusHolder(root, bonusSibling);
            MissionEntryUI bonusEntry = BuildMissionEntry(
                bonusRoot, "LayerBonusEntry", spriteS, spriteM, spriteL, tals);
            StretchFull((RectTransform)bonusEntry.transform);

            // Rebuild template
            Transform content = root.Find("MissionScroll/Viewport/Content");
            if (content == null)
            {
                failed++;
                log.AppendLine("- ✗ MissionScroll/Viewport/Content introuvable");
                return;
            }

            VerticalLayoutGroup contentVlg = content.GetComponent<VerticalLayoutGroup>();
            if (contentVlg != null)
            {
                Undo.RecordObject(contentVlg, UndoLabel);
                contentVlg.spacing = UiTheme.Space3;
            }

            for (int i = content.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(content.GetChild(i).gameObject);

            MissionEntryUI template = BuildMissionEntry(
                content, "MissionEntryTemplate", spriteS, spriteM, spriteL, tals);
            template.gameObject.SetActive(false);

            SerializedObject so = new SerializedObject(pageUi);
            so.FindProperty("layerBonusRoot").objectReferenceValue = bonusRoot;
            so.FindProperty("layerBonusEntry").objectReferenceValue = bonusEntry;
            so.FindProperty("listContent").objectReferenceValue = content;
            so.FindProperty("entryTemplate").objectReferenceValue = template;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pageUi);

            conforme++;
            log.AppendLine("- LayerBonusRow + MissionEntryTemplate rebuild ✓");
            conforme++;
            log.AppendLine("- MissionsPageUI refs re-câblées ✓");
        }

        private static void TuneRootSpacing(
            Transform root,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme)
        {
            VerticalLayoutGroup vlg = root.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                return;

            int pad = Mathf.RoundToInt(UiTheme.Space4);
            bool ok = vlg.spacing >= UiTheme.Space3 - 0.1f
                      && vlg.padding.left == pad;

            if (ok)
            {
                conforme++;
                log.AppendLine("- MissionsRoot spacing / padding OK ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Ajuster padding/spacing MissionsRoot — À FAIRE");
                return;
            }

            Undo.RecordObject(vlg, UndoLabel);
            vlg.padding = new RectOffset(pad, pad, pad, pad);
            vlg.spacing = UiTheme.Space3;
            EditorUtility.SetDirty(vlg);
            conforme++;
            log.AppendLine("- MissionsRoot padding/spacing ajustés ✓");
        }

        private static RectTransform CreateBonusHolder(Transform root, int siblingIndex)
        {
            GameObject go = new GameObject("LayerBonusRow", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, root, false, UndoLabel);
            go.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, root.childCount - 1));

            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = EntryHeight;
            le.preferredHeight = EntryHeight;
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;
            return (RectTransform)go.transform;
        }

        private static MissionEntryUI BuildMissionEntry(
            Transform parent,
            string name,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL,
            Sprite talsSprite)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = EntryHeight;
            le.preferredHeight = EntryHeight;
            le.flexibleWidth = 1f;

            PanelSurface surface = Undo.AddComponent<PanelSurface>(go);
            SerializedObject surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("variant").enumValueIndex = (int)PanelSurface.SurfaceVariant.Card;
            surfaceSo.FindProperty("borderStyle").enumValueIndex =
                (int)PanelSurface.SurfaceBorder.Subtle;
            surfaceSo.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            surfaceSo.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            surfaceSo.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            surfaceSo.FindProperty("blocksRaycasts").boolValue = false;
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();
            surface.ApplyStyle();

            Button btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            Image rootImg = go.GetComponent<Image>();
            btn.targetGraphic = rootImg;

            // Body
            GameObject body = new GameObject("Body", typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(body, UndoLabel);
            Undo.SetTransformParent(body.transform, go.transform, false, UndoLabel);
            RectTransform bodyRt = (RectTransform)body.transform;
            StretchFull(bodyRt);
            float inset = UiTheme.Space4;
            bodyRt.offsetMin = new Vector2(inset, inset);
            bodyRt.offsetMax = new Vector2(-inset, -inset);
            VerticalLayoutGroup bodyVlg = body.GetComponent<VerticalLayoutGroup>();
            bodyVlg.spacing = UiTheme.Space2;
            bodyVlg.childAlignment = TextAnchor.MiddleLeft;
            bodyVlg.childControlWidth = true;
            bodyVlg.childControlHeight = true;
            bodyVlg.childForceExpandWidth = true;
            bodyVlg.childForceExpandHeight = false;

            // Row title
            RectTransform rowTitle = CreateHRow(body.transform, "RowTitle", 44f);
            TextMeshProUGUI title = CreateTmp(
                rowTitle, "Title", "Mission", UiTypography.Body, UiTheme.TextPrimary,
                TextAlignmentOptions.Left, flex: true);
            title.fontStyle = FontStyles.Bold;
            title.enableWordWrapping = true;
            title.overflowMode = TextOverflowModes.Ellipsis;
            TextMeshProUGUI state = CreateTmp(
                rowTitle, "StateLabel", "EN COURS", UiTypography.Caption, UiTheme.TextSecondary,
                TextAlignmentOptions.Right, preferredW: 220f);
            state.enableWordWrapping = false;

            // Row progress
            RectTransform rowProg = CreateHRow(body.transform, "RowProgress", ProgressTrackH + 8f);
            GameObject trackGo = new GameObject(
                "ProgressTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(trackGo, UndoLabel);
            Undo.SetTransformParent(trackGo.transform, rowProg, false, UndoLabel);
            LayoutElement trackLe = Undo.AddComponent<LayoutElement>(trackGo);
            trackLe.flexibleWidth = 1f;
            trackLe.minHeight = ProgressTrackH;
            trackLe.preferredHeight = ProgressTrackH;
            Image trackImg = trackGo.GetComponent<Image>();
            trackImg.sprite = spriteS;
            trackImg.type = Image.Type.Sliced;
            trackImg.color = UiTheme.BorderSubtle;
            trackImg.raycastTarget = false;

            GameObject fillGo = new GameObject(
                "ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(fillGo, UndoLabel);
            Undo.SetTransformParent(fillGo.transform, trackGo.transform, false, UndoLabel);
            Image fillImg = fillGo.GetComponent<Image>();
            fillImg.color = UiTheme.AccentAmber;
            fillImg.raycastTarget = false;
            RectTransform fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0.5f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            TextMeshProUGUI progress = CreateTmp(
                rowProg, "ProgressText", "0/5", UiTypography.Label, UiTheme.TextPrimary,
                TextAlignmentOptions.Right, preferredW: 100f);
            progress.enableWordWrapping = false;

            // Row reward
            RectTransform rowReward = CreateHRow(body.transform, "RowReward", 44f);
            GameObject rewardCluster = new GameObject(
                "RewardRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(rewardCluster, UndoLabel);
            Undo.SetTransformParent(rewardCluster.transform, rowReward, false, UndoLabel);
            LayoutElement rewardLe = Undo.AddComponent<LayoutElement>(rewardCluster);
            rewardLe.flexibleWidth = 1f;
            rewardLe.minHeight = 44f;
            HorizontalLayoutGroup rewardH = rewardCluster.GetComponent<HorizontalLayoutGroup>();
            rewardH.spacing = UiTheme.Space2;
            rewardH.childAlignment = TextAnchor.MiddleLeft;
            rewardH.childControlWidth = true;
            rewardH.childControlHeight = true;
            rewardH.childForceExpandWidth = false;
            rewardH.childForceExpandHeight = true;

            GameObject iconGo = new GameObject(
                "TalsIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconGo, UndoLabel);
            Undo.SetTransformParent(iconGo.transform, rewardCluster.transform, false, UndoLabel);
            LayoutElement iconLe = Undo.AddComponent<LayoutElement>(iconGo);
            iconLe.minWidth = TalsIconSize;
            iconLe.preferredWidth = TalsIconSize;
            iconLe.minHeight = TalsIconSize;
            iconLe.preferredHeight = TalsIconSize;
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = talsSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            TextMeshProUGUI reward = CreateTmp(
                rewardCluster.transform, "RewardText", "+150", UiTypography.Label, UiTheme.AccentGold,
                TextAlignmentOptions.Left, preferredW: 160f);
            reward.enableWordWrapping = false;

            TextMeshProUGUI action = CreateTmp(
                rowReward, "ActionHint", "Réclamer", UiTypography.Label, UiTheme.AccentGold,
                TextAlignmentOptions.Right, preferredW: 200f);
            action.enableWordWrapping = false;
            action.fontStyle = FontStyles.Bold;

            GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(check, UndoLabel);
            Undo.SetTransformParent(check.transform, rowReward, false, UndoLabel);
            LayoutElement checkLe = Undo.AddComponent<LayoutElement>(check);
            checkLe.preferredWidth = 48f;
            TextMeshProUGUI checkTmp = check.GetComponent<TextMeshProUGUI>();
            checkTmp.text = "OK";
            checkTmp.fontSize = UiTypography.Label;
            checkTmp.color = UiTheme.Success;
            checkTmp.alignment = TextAlignmentOptions.Center;
            checkTmp.raycastTarget = false;
            check.SetActive(false);

            GameObject lockGo = new GameObject("LockIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(lockGo, UndoLabel);
            Undo.SetTransformParent(lockGo.transform, rowReward, false, UndoLabel);
            LayoutElement lockLe = Undo.AddComponent<LayoutElement>(lockGo);
            lockLe.preferredWidth = 120f;
            TextMeshProUGUI lockTmp = lockGo.GetComponent<TextMeshProUGUI>();
            lockTmp.text = "VERROU";
            lockTmp.fontSize = UiTypography.Caption;
            lockTmp.color = UiTheme.TextMuted;
            lockTmp.alignment = TextAlignmentOptions.Center;
            lockTmp.raycastTarget = false;
            lockGo.SetActive(false);

            // Disable raycasts on children — only root Button receives taps
            Graphic[] graphics = go.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null && graphics[i].gameObject != go)
                    graphics[i].raycastTarget = false;
            }

            rootImg.raycastTarget = true;

            MissionEntryUI entry = Undo.AddComponent<MissionEntryUI>(go);
            SerializedObject so = new SerializedObject(entry);
            so.FindProperty("surface").objectReferenceValue = surface;
            so.FindProperty("canvasGroup").objectReferenceValue = go.GetComponent<CanvasGroup>();
            so.FindProperty("cardButton").objectReferenceValue = btn;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("progressText").objectReferenceValue = progress;
            so.FindProperty("rewardText").objectReferenceValue = reward;
            so.FindProperty("stateLabel").objectReferenceValue = state;
            so.FindProperty("actionHint").objectReferenceValue = action;
            so.FindProperty("progressFill").objectReferenceValue = fillImg;
            so.FindProperty("progressTrack").objectReferenceValue = (RectTransform)trackGo.transform;
            so.FindProperty("talsIcon").objectReferenceValue = iconImg;
            so.FindProperty("rewardRow").objectReferenceValue = rewardCluster;
            so.FindProperty("checkmark").objectReferenceValue = check;
            so.FindProperty("lockIcon").objectReferenceValue = lockGo;
            so.ApplyModifiedPropertiesWithoutUndo();
            return entry;
        }

        private static RectTransform CreateHRow(Transform parent, string name, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);
            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            HorizontalLayoutGroup h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = UiTheme.Space2;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            return (RectTransform)go.transform;
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            string text,
            float size,
            Color color,
            TextAlignmentOptions align,
            bool flex = false,
            float preferredW = 0f)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);
            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            if (flex)
            {
                le.flexibleWidth = 1f;
                le.minWidth = 80f;
            }
            else if (preferredW > 0f)
            {
                le.preferredWidth = preferredW;
                le.minWidth = preferredW * 0.5f;
            }

            le.minHeight = size + 4f;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void AppendCounter(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine("## COMPTEUR D'ACTIONS (harnais v2)");
            log.AppendLine($"- À FAIRE : {todo}");
            log.AppendLine($"- CONFORMES : {conforme}");
            log.AppendLine($"- ÉCHECS : {failed}");
            log.AppendLine(todo == 0 && failed == 0
                ? "- Convergence : OUI"
                : "- Convergence : NON");
        }

        private static Transform FindDeep(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform f = FindDeep(root.transform, name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform f = FindDeep(root.GetChild(i), name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private static string GetPath(Transform t)
        {
            if (t == null)
                return "—";
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
