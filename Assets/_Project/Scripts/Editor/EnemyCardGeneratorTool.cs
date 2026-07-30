#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère la fiche d'inspection ennemi v2 et désactive l'ancien panneau.
    /// </summary>
    public static class EnemyCardGeneratorTool
    {
        private const string PanelName = "EnemyCardPanel_v2";

        [MenuItem("Chez Arthur/UI/Réparer layout Fiche Ennemi v2")]
        public static void RepairLayout()
        {
            EnemyCardUI ui = Object.FindObjectOfType<EnemyCardUI>(true);
            if (ui == null)
            {
                Debug.LogError("[EnemyCardGeneratorTool] Aucun EnemyCardUI trouvé en scène.");
                return;
            }

            Undo.SetCurrentGroupName("Réparer layout Fiche Ennemi v2 (G2)");
            int undoGroup = Undo.GetCurrentGroup();
            Undo.RecordObject(ui.gameObject, "Réparer layout Fiche Ennemi v2");
            RepairPanelLayout(ui);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log(
                "[EnemyCardGeneratorTool] Layout fiche ennemi v2 réparé — IntentBlock + ModifiersBlock câblés (G2).");
        }

        [MenuItem("Chez Arthur/UI/Générer Fiche Ennemi v2")]
        public static void Build()
        {
            Undo.SetCurrentGroupName("Générer Fiche Ennemi v2");
            int undoGroup = Undo.GetCurrentGroup();

            EnemyCardUI legacy = Object.FindObjectOfType<EnemyCardUI>(true);
            if (legacy == null)
            {
                Debug.LogError("[EnemyCardGeneratorTool] Aucun EnemyCardUI trouvé en scène.");
                return;
            }

            SerializedObject legacySo = new SerializedObject(legacy);
            TextMeshProUGUI legacyNameText =
                legacySo.FindProperty("nameText").objectReferenceValue as TextMeshProUGUI;
            TMP_FontAsset font = legacyNameText != null ? legacyNameText.font : UiGen.LoadFont();
            Sprite panelSprite = ReadPanelSprite(legacySo, legacy);
            Transform canvasParent = ResolveTargetParent(legacy, legacySo);
            if (canvasParent == null)
            {
                Debug.LogError(
                    "[EnemyCardGeneratorTool] Canvas HUD introuvable — place EnemyCardUI sous un Canvas ou ouvre la scène Game.");
                return;
            }

            DestroyExistingV2(canvasParent);
            DisableLegacyVisual(legacy, legacySo);

            BuiltRefs refs = BuildPanel(canvasParent, font, panelSprite);
            EnemyCardUI ui = refs.Panel.AddComponent<EnemyCardUI>();
            WireEnemyCardUI(ui, refs);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = refs.Panel;
            Debug.Log(
                "[EnemyCardGeneratorTool] Fiche v2 générée — refs câblées, ancien visuel désactivé.");
        }

        private static void DisableLegacyVisual(EnemyCardUI legacy, SerializedObject legacySo)
        {
            SerializedProperty cardRootProp = legacySo.FindProperty("cardRoot");
            if (cardRootProp != null && cardRootProp.objectReferenceValue is GameObject rootGo)
            {
                Undo.RecordObject(rootGo, "Désactiver ancienne fiche ennemi");
                rootGo.SetActive(false);
            }

            Undo.DestroyObjectImmediate(legacy);
        }

        private sealed class BuiltRefs
        {
            public GameObject Panel;
            public CanvasGroup CanvasGroup;
            public Image SpriteFrame;
            public Image SpriteImage;
            public TextMeshProUGUI NameText;
            public Image TypeBadgeBg;
            public TextMeshProUGUI TypeText;
            public GameObject DescriptionBlock;
            public TextMeshProUGUI DescriptionText;
            public TextMeshProUGUI HpText;
            public TextMeshProUGUI AtkText;
            public TextMeshProUGUI DefText;
            public TextMeshProUGUI SpdText;
            public GameObject PassiveBlock;
            public TextMeshProUGUI PassiveTitleText;
            public TextMeshProUGUI PassivesText;
            public GameObject IntentBlock;
            public TextMeshProUGUI IntentTitleText;
            public TextMeshProUGUI IntentText;
            public GameObject ModifiersBlock;
            public TextMeshProUGUI ModifiersTitleText;
            public TextMeshProUGUI ModifiersText;
        }

        private static Transform ResolveTargetParent(EnemyCardUI legacy, SerializedObject legacySo)
        {
            if (legacy == null)
                return null;

            Transform directParent = legacy.transform.parent;
            if (directParent != null)
                return directParent;

            SerializedProperty cardRootProp = legacySo.FindProperty("cardRoot");
            if (cardRootProp != null && cardRootProp.objectReferenceValue is GameObject rootGo)
            {
                Transform rootParent = rootGo.transform.parent;
                if (rootParent != null)
                    return rootParent;
            }

            Canvas ancestorCanvas = FindCanvasInAncestors(legacy.transform);
            if (ancestorCanvas != null)
                return ancestorCanvas.transform;

            Canvas mainCanvas = FindMainCanvas();
            return mainCanvas != null ? mainCanvas.transform : null;
        }

        private static Canvas FindCanvasInAncestors(Transform t)
        {
            while (t != null)
            {
                Canvas canvas = t.GetComponent<Canvas>();
                if (canvas != null)
                    return canvas;

                t = t.parent;
            }

            return null;
        }

        private static Canvas FindMainCanvas()
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].renderMode != RenderMode.WorldSpace)
                    return canvases[i];
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static Sprite ReadPanelSprite(SerializedObject legacySo, EnemyCardUI legacy)
        {
            SerializedProperty cardRootProp = legacySo.FindProperty("cardRoot");
            if (cardRootProp != null && cardRootProp.objectReferenceValue is GameObject rootGo)
            {
                Image img = rootGo.GetComponent<Image>();
                if (img != null && img.sprite != null)
                    return img.sprite;
            }

            Image selfImg = legacy.GetComponent<Image>();
            if (selfImg != null && selfImg.sprite != null)
                return selfImg.sprite;

            return UiGen.Card;
        }

        private static void DestroyExistingV2(Transform canvasParent)
        {
            Transform existing = canvasParent.Find(PanelName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);
        }

        private static BuiltRefs BuildPanel(Transform parent, TMP_FontAsset font, Sprite panelSprite)
        {
            var refs = new BuiltRefs();

            GameObject panel = new GameObject(PanelName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(panel, "Créer EnemyCardPanel_v2");
            panel.transform.SetParent(parent, false);
            panel.SetActive(true);
            refs.Panel = panel;

            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(EnemyCardStyle.HorizontalMargin, 1f);
            panelRt.anchorMax = new Vector2(1f - EnemyCardStyle.HorizontalMargin, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, EnemyCardStyle.TopOffsetY);
            panelRt.sizeDelta = Vector2.zero;

            Image panelImg = panel.GetComponent<Image>();
            panelImg.sprite = panelSprite != null ? panelSprite : UiGen.Card;
            panelImg.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Sliced;
            Color bg = UiTheme.Surface;
            bg.a = 0.95f;
            panelImg.color = bg;
            panelImg.raycastTarget = false;

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            refs.CanvasGroup = group;

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(
                EnemyCardStyle.PanelPaddingLeft,
                EnemyCardStyle.PanelPaddingRight,
                EnemyCardStyle.PanelPaddingTop,
                EnemyCardStyle.PanelPaddingBottom);
            vlg.spacing = EnemyCardStyle.PanelSectionSpacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = panel.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildHeader(panelRt, font, refs);
            BuildDescription(panelRt, font, refs);
            BuildStatsRow(panelRt, font, refs);
            BuildIntentBlock(panelRt, font, refs);
            BuildPassiveBlock(panelRt, font, refs);
            BuildModifiersBlock(panelRt, font, refs);

            return refs;
        }

        private static void BuildHeader(RectTransform parent, TMP_FontAsset font, BuiltRefs refs)
        {
            GameObject header = CreateUI("Header", parent);
            PrepareLayoutChild(header.GetComponent<RectTransform>());
            HorizontalLayoutGroup hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = EnemyCardStyle.HeaderSpacing;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            LayoutElement headerLe = header.AddComponent<LayoutElement>();
            headerLe.minHeight = EnemyCardStyle.HeaderHeight;
            headerLe.preferredHeight = EnemyCardStyle.HeaderHeight;

            GameObject spriteFrameGo = CreateUI("SpriteFrame", header.transform as RectTransform);
            LayoutElement frameLe = spriteFrameGo.AddComponent<LayoutElement>();
            frameLe.minWidth = EnemyCardStyle.SpriteFrameSize;
            frameLe.preferredWidth = EnemyCardStyle.SpriteFrameSize;
            frameLe.minHeight = EnemyCardStyle.SpriteFrameSize;
            frameLe.preferredHeight = EnemyCardStyle.SpriteFrameSize;

            refs.SpriteFrame = spriteFrameGo.AddComponent<Image>();
            refs.SpriteFrame.sprite = UiGen.Card;
            refs.SpriteFrame.type = Image.Type.Sliced;
            refs.SpriteFrame.color = UiTheme.EnemyTypeNormalColor;
            refs.SpriteFrame.raycastTarget = false;

            GameObject spriteImageGo = CreateUI("SpriteImage", spriteFrameGo.transform as RectTransform);
            RectTransform spriteImageRt = spriteImageGo.GetComponent<RectTransform>();
            spriteImageRt.anchorMin = Vector2.zero;
            spriteImageRt.anchorMax = Vector2.one;
            spriteImageRt.offsetMin = new Vector2(12f, 12f);
            spriteImageRt.offsetMax = new Vector2(-12f, -12f);

            refs.SpriteImage = spriteImageGo.AddComponent<Image>();
            refs.SpriteImage.preserveAspect = true;
            refs.SpriteImage.raycastTarget = false;

            GameObject headerInfo = CreateUI("HeaderInfo", header.transform as RectTransform);
            VerticalLayoutGroup infoVlg = headerInfo.AddComponent<VerticalLayoutGroup>();
            infoVlg.spacing = EnemyCardStyle.HeaderInfoSpacing;
            infoVlg.childAlignment = TextAnchor.UpperLeft;
            infoVlg.childControlWidth = true;
            infoVlg.childControlHeight = true;
            infoVlg.childForceExpandWidth = true;
            infoVlg.childForceExpandHeight = false;

            LayoutElement infoLe = headerInfo.AddComponent<LayoutElement>();
            infoLe.flexibleWidth = 1f;

            refs.NameText = CreateText(
                "NameText",
                headerInfo.transform as RectTransform,
                font,
                "Ennemi",
                EnemyCardStyle.NameFontSize,
                UiTheme.TextPrimary,
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft);

            GameObject typeBadge = CreateUI("TypeBadge", headerInfo.transform as RectTransform);
            LayoutElement badgeLe = typeBadge.AddComponent<LayoutElement>();
            badgeLe.preferredWidth = EnemyCardStyle.TypeBadgeWidth;
            badgeLe.minHeight = EnemyCardStyle.TypeBadgeHeight;
            badgeLe.preferredHeight = EnemyCardStyle.TypeBadgeHeight;

            refs.TypeBadgeBg = typeBadge.AddComponent<Image>();
            refs.TypeBadgeBg.sprite = UiGen.Card;
            refs.TypeBadgeBg.type = Image.Type.Sliced;
            Color badgeColor = UiTheme.EnemyTypeNormalColor;
            badgeColor.a = 0.22f;
            refs.TypeBadgeBg.color = badgeColor;
            refs.TypeBadgeBg.raycastTarget = false;

            refs.TypeText = CreateText(
                "TypeText",
                typeBadge.transform as RectTransform,
                font,
                "NORMAL",
                EnemyCardStyle.TypeFontSize,
                UiTheme.EnemyTypeNormalColor,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

            RectTransform typeTextRt = refs.TypeText.rectTransform;
            typeTextRt.anchorMin = Vector2.zero;
            typeTextRt.anchorMax = Vector2.one;
            typeTextRt.offsetMin = Vector2.zero;
            typeTextRt.offsetMax = Vector2.zero;
        }

        private static void BuildDescription(RectTransform parent, TMP_FontAsset font, BuiltRefs refs)
        {
            refs.DescriptionBlock = CreateSection(
                "DescriptionBlock",
                parent,
                EnemyCardStyle.DescriptionSectionSpacing);
            refs.DescriptionText = CreateText(
                "DescriptionText",
                refs.DescriptionBlock.transform as RectTransform,
                font,
                "Description…",
                EnemyCardStyle.DescriptionFontSize,
                UiTheme.TextSecondary,
                FontStyles.Italic,
                TextAlignmentOptions.TopLeft);
            refs.DescriptionText.lineSpacing = EnemyCardStyle.DescriptionLineSpacing;
            ConfigureWrappingText(refs.DescriptionText);
        }

        private static void BuildStatsRow(RectTransform parent, TMP_FontAsset font, BuiltRefs refs)
        {
            GameObject statsRow = CreateUI("StatsRow", parent);
            PrepareLayoutChild(statsRow.GetComponent<RectTransform>());

            HorizontalLayoutGroup hlg = statsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = EnemyCardStyle.StatsRowSpacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;

            LayoutElement rowLe = statsRow.AddComponent<LayoutElement>();
            rowLe.minHeight = EnemyCardStyle.StatsRowHeight;
            rowLe.preferredHeight = EnemyCardStyle.StatsRowHeight;
            rowLe.flexibleWidth = 1f;

            refs.HpText = CreateStatBlock(statsRow.transform as RectTransform, font, "HP", out _);
            refs.AtkText = CreateStatBlock(statsRow.transform as RectTransform, font, "ATK", out _);
            refs.DefText = CreateStatBlock(statsRow.transform as RectTransform, font, "DEF", out _);
            refs.SpdText = CreateStatBlock(statsRow.transform as RectTransform, font, "SPD", out _);
        }

        private static void BuildIntentBlock(RectTransform parent, TMP_FontAsset font, BuiltRefs refs)
        {
            refs.IntentBlock = CreateSection("IntentBlock", parent, EnemyCardStyle.IntentSectionSpacing);

            refs.IntentTitleText = CreateText(
                "IntentTitle",
                refs.IntentBlock.transform as RectTransform,
                font,
                EnemyCardStyle.IntentSectionTitle,
                EnemyCardStyle.PassiveTitleFontSize,
                UiTheme.EnemyTypeNormalColor,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            refs.IntentTitleText.characterSpacing = 4f;

            refs.IntentText = CreateText(
                "IntentText",
                refs.IntentBlock.transform as RectTransform,
                font,
                "—",
                EnemyCardStyle.PassiveBodyFontSize,
                UiTheme.TextPrimary,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            refs.IntentText.lineSpacing = EnemyCardStyle.PassiveLineSpacing;
            ConfigureWrappingText(refs.IntentText);
        }

        private static void BuildPassiveBlock(RectTransform parent, TMP_FontAsset font, BuiltRefs refs)
        {
            refs.PassiveBlock = CreateSection("PassiveBlock", parent, EnemyCardStyle.PassiveSectionSpacing);

            refs.PassiveTitleText = CreateText(
                "PassiveTitle",
                refs.PassiveBlock.transform as RectTransform,
                font,
                "PASSIFS",
                EnemyCardStyle.PassiveTitleFontSize,
                UiTheme.EnemyTypeNormalColor,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            refs.PassiveTitleText.characterSpacing = 4f;

            refs.PassivesText = CreateText(
                "PassivesText",
                refs.PassiveBlock.transform as RectTransform,
                font,
                string.Empty,
                EnemyCardStyle.PassiveBodyFontSize,
                UiTheme.TextPrimary,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            refs.PassivesText.lineSpacing = EnemyCardStyle.PassiveLineSpacing;
            ConfigureWrappingText(refs.PassivesText);
        }

        private static void BuildModifiersBlock(RectTransform parent, TMP_FontAsset font, BuiltRefs refs)
        {
            refs.ModifiersBlock = CreateSection("ModifiersBlock", parent, EnemyCardStyle.ModifiersSectionSpacing);

            refs.ModifiersTitleText = CreateText(
                "ModifiersTitle",
                refs.ModifiersBlock.transform as RectTransform,
                font,
                EnemyCardStyle.ModifiersSectionTitle,
                EnemyCardStyle.PassiveTitleFontSize,
                UiTheme.EnemyTypeNormalColor,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            refs.ModifiersTitleText.characterSpacing = 4f;

            refs.ModifiersText = CreateText(
                "ModifiersText",
                refs.ModifiersBlock.transform as RectTransform,
                font,
                string.Empty,
                EnemyCardStyle.PassiveBodyFontSize,
                UiTheme.TextPrimary,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            refs.ModifiersText.lineSpacing = EnemyCardStyle.PassiveLineSpacing;
            ConfigureWrappingText(refs.ModifiersText);
        }

        private static GameObject CreateSection(string name, RectTransform parent, float spacing)
        {
            GameObject section = CreateUI(name, parent);
            PrepareLayoutChild(section.GetComponent<RectTransform>());

            VerticalLayoutGroup vlg = section.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = section.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement le = section.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            return section;
        }

        private static void PrepareLayoutChild(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private static void ConfigureWrappingText(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;

            LayoutElement le = tmp.gameObject.GetComponent<LayoutElement>();
            if (le == null)
                le = tmp.gameObject.AddComponent<LayoutElement>();

            le.flexibleWidth = 1f;

            ContentSizeFitter csf = tmp.gameObject.GetComponent<ContentSizeFitter>();
            if (csf == null)
                csf = tmp.gameObject.AddComponent<ContentSizeFitter>();

            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void RepairPanelLayout(EnemyCardUI ui)
        {
            SerializedObject so = new SerializedObject(ui);
            TMP_FontAsset font = ResolveFont(so);

            EnsureG2Sections(ui.transform as RectTransform, font, so);

            RepairSection(
                so.FindProperty("descriptionBlock").objectReferenceValue as GameObject,
                EnemyCardStyle.DescriptionSectionSpacing);
            RepairSection(
                so.FindProperty("intentBlock").objectReferenceValue as GameObject,
                EnemyCardStyle.IntentSectionSpacing);
            RepairSection(
                so.FindProperty("passiveBlock").objectReferenceValue as GameObject,
                EnemyCardStyle.PassiveSectionSpacing);
            RepairSection(
                so.FindProperty("modifiersBlock").objectReferenceValue as GameObject,
                EnemyCardStyle.ModifiersSectionSpacing);

            ConfigureWrappingText(so.FindProperty("descriptionText").objectReferenceValue as TextMeshProUGUI);
            ConfigureWrappingText(so.FindProperty("intentText").objectReferenceValue as TextMeshProUGUI);
            ConfigureWrappingText(so.FindProperty("intentTitleText").objectReferenceValue as TextMeshProUGUI);
            ConfigureWrappingText(so.FindProperty("passivesText").objectReferenceValue as TextMeshProUGUI);
            ConfigureWrappingText(so.FindProperty("passiveTitleText").objectReferenceValue as TextMeshProUGUI);
            ConfigureWrappingText(so.FindProperty("modifiersText").objectReferenceValue as TextMeshProUGUI);
            ConfigureWrappingText(so.FindProperty("modifiersTitleText").objectReferenceValue as TextMeshProUGUI);

            Transform statsRow = ui.transform.Find("StatsRow");
            if (statsRow != null)
            {
                PrepareLayoutChild(statsRow as RectTransform);
                LayoutElement rowLe = statsRow.GetComponent<LayoutElement>() ?? statsRow.gameObject.AddComponent<LayoutElement>();
                rowLe.minHeight = EnemyCardStyle.StatsRowHeight;
                rowLe.preferredHeight = EnemyCardStyle.StatsRowHeight;
            }

            ApplySectionOrder(ui.transform);

            EnemyCardStyle.Apply(
                ui.gameObject,
                so.FindProperty("nameText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("typeText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("descriptionText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("hpText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("atkText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("defText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("spdText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("passiveTitleText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("passivesText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("descriptionBlock").objectReferenceValue as GameObject,
                so.FindProperty("passiveBlock").objectReferenceValue as GameObject,
                so.FindProperty("spriteFrame").objectReferenceValue as Image,
                so.FindProperty("intentTitleText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("intentText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("intentBlock").objectReferenceValue as GameObject,
                so.FindProperty("modifiersTitleText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("modifiersText").objectReferenceValue as TextMeshProUGUI,
                so.FindProperty("modifiersBlock").objectReferenceValue as GameObject);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
            LayoutRebuilder.ForceRebuildLayoutImmediate(ui.transform as RectTransform);
        }

        private static TMP_FontAsset ResolveFont(SerializedObject so)
        {
            TextMeshProUGUI name =
                so.FindProperty("nameText").objectReferenceValue as TextMeshProUGUI;
            return name != null && name.font != null ? name.font : UiGen.LoadFont();
        }

        /// <summary>
        /// Find-or-create IntentBlock / ModifiersBlock (G2) — exécution ×2 = zéro diff.
        /// </summary>
        private static void EnsureG2Sections(RectTransform panelRt, TMP_FontAsset font, SerializedObject so)
        {
            if (panelRt == null)
                return;

            var refs = new BuiltRefs();

            Transform intentT = panelRt.Find("IntentBlock");
            if (intentT == null)
            {
                BuildIntentBlock(panelRt, font, refs);
                Undo.RegisterCreatedObjectUndo(refs.IntentBlock, "Créer IntentBlock");
            }
            else
            {
                refs.IntentBlock = intentT.gameObject;
                refs.IntentTitleText = intentT.Find("IntentTitle")?.GetComponent<TextMeshProUGUI>();
                refs.IntentText = intentT.Find("IntentText")?.GetComponent<TextMeshProUGUI>();
                if (refs.IntentTitleText == null || refs.IntentText == null)
                {
                    // Bloc incomplet → reconstruit proprement.
                    Undo.DestroyObjectImmediate(intentT.gameObject);
                    BuildIntentBlock(panelRt, font, refs);
                    Undo.RegisterCreatedObjectUndo(refs.IntentBlock, "Recréer IntentBlock");
                }
            }

            Transform modifiersT = panelRt.Find("ModifiersBlock");
            if (modifiersT == null)
            {
                BuildModifiersBlock(panelRt, font, refs);
                Undo.RegisterCreatedObjectUndo(refs.ModifiersBlock, "Créer ModifiersBlock");
            }
            else
            {
                refs.ModifiersBlock = modifiersT.gameObject;
                refs.ModifiersTitleText = modifiersT.Find("ModifiersTitle")?.GetComponent<TextMeshProUGUI>();
                refs.ModifiersText = modifiersT.Find("ModifiersText")?.GetComponent<TextMeshProUGUI>();
                if (refs.ModifiersTitleText == null || refs.ModifiersText == null)
                {
                    Undo.DestroyObjectImmediate(modifiersT.gameObject);
                    BuildModifiersBlock(panelRt, font, refs);
                    Undo.RegisterCreatedObjectUndo(refs.ModifiersBlock, "Recréer ModifiersBlock");
                }
            }

            so.FindProperty("intentBlock").objectReferenceValue = refs.IntentBlock;
            so.FindProperty("intentTitleText").objectReferenceValue = refs.IntentTitleText;
            so.FindProperty("intentText").objectReferenceValue = refs.IntentText;
            so.FindProperty("modifiersBlock").objectReferenceValue = refs.ModifiersBlock;
            so.FindProperty("modifiersTitleText").objectReferenceValue = refs.ModifiersTitleText;
            so.FindProperty("modifiersText").objectReferenceValue = refs.ModifiersText;
        }

        /// <summary>
        /// Ordre vertical : Header → Description → Stats → Intention → Passifs → Effets actifs.
        /// </summary>
        private static void ApplySectionOrder(Transform panel)
        {
            if (panel == null)
                return;

            SetSiblingIfFound(panel, "Header", 0);
            SetSiblingIfFound(panel, "DescriptionBlock", 1);
            SetSiblingIfFound(panel, "StatsRow", 2);
            SetSiblingIfFound(panel, "IntentBlock", 3);
            SetSiblingIfFound(panel, "PassiveBlock", 4);
            SetSiblingIfFound(panel, "ModifiersBlock", 5);
        }

        private static void SetSiblingIfFound(Transform panel, string childName, int index)
        {
            Transform child = panel.Find(childName);
            if (child != null)
                child.SetSiblingIndex(index);
        }

        private static void RepairSection(GameObject section, float spacing)
        {
            if (section == null)
                return;

            PrepareLayoutChild(section.transform as RectTransform);

            VerticalLayoutGroup vlg = section.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = section.AddComponent<VerticalLayoutGroup>();

            vlg.spacing = spacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = section.GetComponent<ContentSizeFitter>();
            if (csf == null)
                csf = section.AddComponent<ContentSizeFitter>();

            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement le = section.GetComponent<LayoutElement>();
            if (le == null)
                le = section.AddComponent<LayoutElement>();

            le.flexibleWidth = 1f;
        }

        private static TextMeshProUGUI CreateStatBlock(
            RectTransform parent,
            TMP_FontAsset font,
            string label,
            out GameObject block)
        {
            block = CreateUI($"Stat_{label}", parent);
            LayoutElement le = block.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            VerticalLayoutGroup vlg = block.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = EnemyCardStyle.StatsBlockSpacing;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateText(
                $"StatLabel_{label}",
                block.transform as RectTransform,
                font,
                label,
                EnemyCardStyle.StatLabelFontSize,
                UiTheme.TextMuted,
                FontStyles.Normal,
                TextAlignmentOptions.Center);

            return CreateText(
                $"StatValue_{label}",
                block.transform as RectTransform,
                font,
                "—",
                EnemyCardStyle.StatValueFontSize,
                UiTheme.TextPrimary,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
        }

        private static void WireEnemyCardUI(EnemyCardUI ui, BuiltRefs refs)
        {
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("cardRoot").objectReferenceValue = refs.Panel;
            so.FindProperty("canvasGroup").objectReferenceValue = refs.CanvasGroup;
            so.FindProperty("spriteFrame").objectReferenceValue = refs.SpriteFrame;
            so.FindProperty("spriteImage").objectReferenceValue = refs.SpriteImage;
            so.FindProperty("nameText").objectReferenceValue = refs.NameText;
            so.FindProperty("typeBadgeBg").objectReferenceValue = refs.TypeBadgeBg;
            so.FindProperty("typeText").objectReferenceValue = refs.TypeText;
            so.FindProperty("descriptionBlock").objectReferenceValue = refs.DescriptionBlock;
            so.FindProperty("descriptionText").objectReferenceValue = refs.DescriptionText;
            so.FindProperty("hpText").objectReferenceValue = refs.HpText;
            so.FindProperty("atkText").objectReferenceValue = refs.AtkText;
            so.FindProperty("defText").objectReferenceValue = refs.DefText;
            so.FindProperty("spdText").objectReferenceValue = refs.SpdText;
            so.FindProperty("passiveBlock").objectReferenceValue = refs.PassiveBlock;
            so.FindProperty("passiveTitleText").objectReferenceValue = refs.PassiveTitleText;
            so.FindProperty("passivesText").objectReferenceValue = refs.PassivesText;
            so.FindProperty("intentBlock").objectReferenceValue = refs.IntentBlock;
            so.FindProperty("intentTitleText").objectReferenceValue = refs.IntentTitleText;
            so.FindProperty("intentText").objectReferenceValue = refs.IntentText;
            so.FindProperty("modifiersBlock").objectReferenceValue = refs.ModifiersBlock;
            so.FindProperty("modifiersTitleText").objectReferenceValue = refs.ModifiersTitleText;
            so.FindProperty("modifiersText").objectReferenceValue = refs.ModifiersText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateUI(string name, RectTransform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            RectTransform parent,
            TMP_FontAsset font,
            string text,
            float size,
            Color color,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            GameObject go = CreateUI(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
#endif
