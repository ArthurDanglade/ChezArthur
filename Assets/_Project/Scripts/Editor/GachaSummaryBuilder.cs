#if UNITY_EDITOR
using ChezArthur.Gacha;
using ChezArthur.Hub;
using ChezArthur.Hub.Pages;
using ChezArthur.Hub.Pages.Invocation;
using ChezArthur.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// OUTIL DESTRUCTIF — fenêtre Gate 5 gacha.
    /// Reconstruit les prefabs de carte récap + le contenu de SummaryScene, puis câble le controller.
    /// </summary>
    public static class GachaSummaryBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;
        /// <summary> Doit rester aligné avec GachaAnimationController.NAV_CLEARANCE. </summary>
        private const float NavClearance = 280f;
        private const float TopClearance = 48f;
        private const float GridCardAspect = 1.55f; // H/W — cartes portrait lisibles
        private const float SingleCardW = 520f;
        private const float SingleCardH = 720f;
        private const float SingleIconSize = 420f;
        private const float RarityBorderH = 4f;
        private const float HeaderHeight = 88f;
        private const float HintHeight = 48f;
        private const float FooterHeight = 120f;
        private const float FooterBtnHeight = 96f;

        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string GridPrefabPath = PrefabFolder + "/PullResultEntry.prefab";
        private const string SinglePrefabPath = PrefabFolder + "/PullResultSingleCard.prefab";
        private const string RadialGlowPath = "Assets/_Project/Art/FX/fx_radial_glow.png";
        private const string GlowMatPath = "Assets/_Project/Art/FX/AwakeningGlow.mat";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI/Reconstruire récap gacha (fenêtre Gate 5)")]
        private static void BuildMenu()
        {
            bool ok = EditorUtility.DisplayDialog(
                "Récap gacha Gate 5",
                "OUTIL DESTRUCTIF — fenêtre Gate 5 gacha.\n\n" +
                "• Écrase PullResultEntry.prefab\n" +
                "• Crée / écrase PullResultSingleCard.prefab\n" +
                "• Reconstruit SummaryScene + câble GachaAnimationController\n\n" +
                "Ouvre Hub.unity avant de continuer.\n\nContinuer ?",
                "Reconstruire",
                "Annuler");
            if (!ok)
                return;

            try
            {
                BuildAll();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GachaSummaryBuilder] Échec : " + ex);
            }
        }

        // ═══════════════════════════════════════════
        // BUILD
        // ═══════════════════════════════════════════

        private static void BuildAll()
        {
            Sprite card = UiGen.Card;
            Sprite pixel = UiGen.SolidWhite;
            Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RadialGlowPath);
            Material glowMat = AssetDatabase.LoadAssetAtPath<Material>(GlowMatPath);
            TMP_FontAsset font = UiGen.LoadFont();

            if (card == null || pixel == null)
            {
                Debug.LogError("[GachaSummaryBuilder] Sprites card_rounded / ui_white_pixel introuvables.");
                return;
            }

            ComputeGridCell(out float cellW, out float cellH);

            PullResultEntryUI gridPrefab = BuildGridPrefab(
                card, pixel, glowSprite, glowMat, font, cellW, cellH);
            PullResultEntryUI singlePrefab = BuildSinglePrefab(card, pixel, glowSprite, glowMat, font);

            GachaAnimationController gac = Object.FindObjectOfType<GachaAnimationController>(true);
            if (gac == null)
            {
                Debug.LogError(
                    "[GachaSummaryBuilder] GachaAnimationController introuvable — ouvre Hub.unity.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(gac.gameObject, "Gate 5 — récap gacha");

            RebuildSummaryScene(gac, card, pixel, font, gridPrefab, singlePrefab, cellW, cellH);

            EditorSceneManager.MarkSceneDirty(gac.gameObject.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[GachaSummaryBuilder] Gate 5 OK — prefabs + SummaryScene + câblage controller.");
            EditorGUIUtility.PingObject(gac);
        }

        /// <summary>
        /// Cellules 5×2 : largeur max @1080, hauteur pour remplir la zone médiane (moins de vide).
        /// </summary>
        private static void ComputeGridCell(out float cellW, out float cellH)
        {
            float summaryH = RefHeight - NavClearance - TopClearance;
            float headerBlock = HeaderHeight + UiTheme.PadCard + UiTheme.SpacingRow;
            float footerBlock = FooterHeight + HintHeight + UiTheme.PadCard * 2;
            float gridH = summaryH - headerBlock - footerBlock;

            float contentW = RefWidth - UiTheme.PadCompact * 2f;
            cellW = (contentW - UiTheme.SpacingRow * 4f) / 5f;

            float fillH = (gridH - UiTheme.SpacingRow - UiTheme.PadCompact * 2f) / 2f;
            // Portrait lisible : au moins aspect 1.55, jusqu'à remplir la zone (cap 1.85).
            cellH = Mathf.Clamp(fillH, cellW * GridCardAspect, cellW * 1.85f);
        }

        // ═══════════════════════════════════════════
        // PREFAB GRILLE
        // ═══════════════════════════════════════════

        private static PullResultEntryUI BuildGridPrefab(
            Sprite card,
            Sprite pixel,
            Sprite glowSprite,
            Material glowMat,
            TMP_FontAsset font,
            float cardW,
            float cardH)
        {
            GameObject root = new GameObject("PullResultEntry", typeof(RectTransform));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(cardW, cardH);

            Image bg = root.AddComponent<Image>();
            bg.sprite = card;
            bg.type = Image.Type.Sliced;
            bg.color = UiTheme.CardPanelEntry;
            bg.raycastTarget = true;

            Button btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;

            CanvasGroup cg = root.AddComponent<CanvasGroup>();
            PullResultEntryUI entry = root.AddComponent<PullResultEntryUI>();

            Image ssrGlow = CreateGlow(root.transform, glowSprite, glowMat, cardW * 1.2f);

            Image rarityTop = CreateTopBorder(root.transform, pixel, RarityBorderH);

            float iconSize = cardW - UiTheme.PadCompact * 2f;
            Image iconImg;
            TextMeshProUGUI fallback;
            CreateIconBlock(
                root.transform, iconSize, font, UiTheme.FontName,
                out iconImg, out fallback);

            float nameH = 36f;
            float chipH = 34f;
            float bottomStack = nameH + chipH + UiTheme.PadCompact * 2f + 8f;

            TextMeshProUGUI name = CreateTmp(
                root.transform, "NameText", font, UiTheme.FontBody, UiTheme.TextSecondary,
                TextAlignmentOptions.Center);
            name.text = "Nom";
            name.enableWordWrapping = false;
            name.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform nameRt = name.rectTransform;
            SetAnchors(nameRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            nameRt.anchoredPosition = new Vector2(0f, chipH + UiTheme.PadCompact + 10f);
            nameRt.sizeDelta = new Vector2(-(UiTheme.PadCompact * 2), nameH);

            Image chipFrame;
            TextMeshProUGUI chipText;
            CreateStatusChip(
                root.transform, font, UiTheme.FontLabel, out chipFrame, out chipText);
            RectTransform chipRt = chipFrame.rectTransform;
            SetAnchors(chipRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            chipRt.anchoredPosition = new Vector2(0f, UiTheme.PadCompact);
            chipRt.sizeDelta = new Vector2(cardW - UiTheme.PadCompact * 2f, chipH);

            // Icône : occupe l'espace au-dessus du stack nom+chip
            RectTransform iconAreaRt = iconImg.transform.parent as RectTransform;
            if (iconAreaRt != null)
            {
                float topPad = RarityBorderH + UiTheme.PadCompact;
                float maxIcon = cardH - topPad - bottomStack - 4f;
                float finalIcon = Mathf.Min(iconSize, maxIcon);
                iconAreaRt.sizeDelta = new Vector2(finalIcon, finalIcon);
                iconAreaRt.anchoredPosition = new Vector2(0f, -topPad);
            }

            GameObject rateUp = CreateRateUpBadge(root.transform, font, UiTheme.FontLabel);

            WireEntry(
                entry, iconImg, fallback, rarityTop, name, chipText, chipFrame,
                rateUp, btn, ssrGlow, cg);

            EnsureFolder(PrefabFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GridPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<PullResultEntryUI>();
        }

        // ═══════════════════════════════════════════
        // PREFAB SINGLE (×2)
        // ═══════════════════════════════════════════

        private static PullResultEntryUI BuildSinglePrefab(
            Sprite card,
            Sprite pixel,
            Sprite glowSprite,
            Material glowMat,
            TMP_FontAsset font)
        {
            GameObject root = new GameObject("PullResultSingleCard", typeof(RectTransform));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(SingleCardW, SingleCardH);

            Image bg = root.AddComponent<Image>();
            bg.sprite = card;
            bg.type = Image.Type.Sliced;
            bg.color = UiTheme.CardPanelEntry;
            bg.raycastTarget = true;

            Button btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;

            CanvasGroup cg = root.AddComponent<CanvasGroup>();
            PullResultEntryUI entry = root.AddComponent<PullResultEntryUI>();

            Image ssrGlow = CreateGlow(root.transform, glowSprite, glowMat, SingleIconSize * 1.25f);

            Image rarityTop = CreateTopBorder(root.transform, pixel, RarityBorderH);

            Image iconImg;
            TextMeshProUGUI fallback;
            CreateIconBlock(
                root.transform, SingleIconSize, font, UiTheme.FontName,
                out iconImg, out fallback);
            RectTransform iconAreaRt = iconImg.transform.parent as RectTransform;
            if (iconAreaRt != null)
            {
                // Grande carte : icône un peu plus basse sous le liseré
                iconAreaRt.anchoredPosition = new Vector2(
                    0f, -(RarityBorderH + UiTheme.PadCard + 8f));
            }

            TextMeshProUGUI name = CreateTmp(
                root.transform, "NameText", font, UiTheme.FontName, UiTheme.TextPrimary,
                TextAlignmentOptions.Center);
            name.text = "Nom";
            name.enableWordWrapping = false;
            name.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform nameRt = name.rectTransform;
            SetAnchors(nameRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            nameRt.anchoredPosition = new Vector2(0f, 110f);
            nameRt.sizeDelta = new Vector2(-(UiTheme.PadCard * 2), 40f);

            Image chipFrame;
            TextMeshProUGUI chipText;
            CreateStatusChip(
                root.transform, font, UiTheme.FontLabel, out chipFrame, out chipText);
            RectTransform chipRt = chipFrame.rectTransform;
            SetAnchors(chipRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            chipRt.anchoredPosition = new Vector2(0f, 62f);
            chipRt.sizeDelta = new Vector2(SingleCardW - UiTheme.PadCard * 2f, 36f);

            TextMeshProUGUI hint = CreateTmp(
                root.transform, "TouchHint", font, UiTheme.FontBody, UiTheme.TextMuted,
                TextAlignmentOptions.Center);
            hint.text = "Toucher pour ouvrir la fiche";
            RectTransform hintRt = hint.rectTransform;
            SetAnchors(hintRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            hintRt.anchoredPosition = new Vector2(0f, 28f);
            hintRt.sizeDelta = new Vector2(-(UiTheme.PadCard * 2), 36f);

            GameObject rateUp = CreateRateUpBadge(root.transform, font, UiTheme.FontLabel);

            WireEntry(
                entry, iconImg, fallback, rarityTop, name, chipText, chipFrame,
                rateUp, btn, ssrGlow, cg);

            EnsureFolder(PrefabFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SinglePrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<PullResultEntryUI>();
        }

        // ═══════════════════════════════════════════
        // SUMMARY SCENE
        // ═══════════════════════════════════════════

        private static void RebuildSummaryScene(
            GachaAnimationController gac,
            Sprite card,
            Sprite pixel,
            TMP_FontAsset font,
            PullResultEntryUI gridPrefab,
            PullResultEntryUI singlePrefab,
            float cellW,
            float cellH)
        {
            Transform rootTf = gac.transform;
            GameObject summaryGo = FindOrCreateChild(rootTf, "SummaryScene");
            summaryGo.SetActive(false);

            RectTransform summaryRt = summaryGo.GetComponent<RectTransform>();
            if (summaryRt == null)
                summaryRt = summaryGo.AddComponent<RectTransform>();
            summaryRt.anchorMin = Vector2.zero;
            summaryRt.anchorMax = Vector2.one;
            summaryRt.pivot = new Vector2(0.5f, 0.5f);
            summaryRt.offsetMin = new Vector2(0f, NavClearance);
            summaryRt.offsetMax = new Vector2(0f, -TopClearance);

            ClearChildren(summaryGo.transform);

            // Fond charbon (stage) — moins « dalle plate »
            GameObject bgGo = CreateUi(summaryGo.transform, "Background");
            StretchFull(bgGo.GetComponent<RectTransform>());
            Image bgImg = bgGo.GetComponent<Image>();
            bgImg.sprite = pixel;
            bgImg.color = UiTheme.GachaStageCharcoal;
            bgImg.raycastTarget = true;

            // Vignette douce (SurfaceBar semi-opaque) — marges PadCard
            GameObject vignetteGo = CreateUi(summaryGo.transform, "Vignette");
            RectTransform vigRt = vignetteGo.GetComponent<RectTransform>();
            StretchFull(vigRt);
            vigRt.offsetMin = new Vector2(UiTheme.PadCard, UiTheme.PadCard);
            vigRt.offsetMax = new Vector2(-UiTheme.PadCard, -UiTheme.PadCard);
            Image vigImg = vignetteGo.GetComponent<Image>();
            vigImg.sprite = card;
            vigImg.type = Image.Type.Sliced;
            Color vigCol = UiTheme.SurfaceBar;
            vigCol.a = 0.72f;
            vigImg.color = vigCol;
            vigImg.raycastTarget = false;

            // Filet or haut (accent)
            GameObject topAccent = CreateUi(summaryGo.transform, "TopGoldAccent");
            RectTransform accentRt = topAccent.GetComponent<RectTransform>();
            SetAnchors(accentRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            accentRt.anchoredPosition = Vector2.zero;
            accentRt.sizeDelta = new Vector2(0f, 2f);
            Image accentImg = topAccent.GetComponent<Image>();
            accentImg.sprite = pixel;
            accentImg.color = UiTheme.Gold;
            accentImg.raycastTarget = false;

            // Header
            GameObject headerGo = CreateUi(summaryGo.transform, "Header");
            RectTransform headerRt = headerGo.GetComponent<RectTransform>();
            SetAnchors(headerRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            headerRt.anchoredPosition = new Vector2(0f, -UiTheme.PadCard);
            headerRt.sizeDelta = new Vector2(-(UiTheme.PadCard * 2), HeaderHeight);
            Object.DestroyImmediate(headerGo.GetComponent<Image>());

            TextMeshProUGUI title = CreateTmp(
                headerGo.transform, "TitleText", font, UiTheme.FontName, UiTheme.TextPrimary,
                TextAlignmentOptions.Center);
            title.text = "Résultat de l'invocation";
            title.fontStyle = FontStyles.Bold;
            RectTransform titleRt = title.rectTransform;
            SetAnchors(titleRt, new Vector2(0f, 0.3f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            GameObject lineGo = CreateUi(headerGo.transform, "GoldLine");
            RectTransform lineRt = lineGo.GetComponent<RectTransform>();
            SetAnchors(lineRt, new Vector2(0.12f, 0f), new Vector2(0.88f, 0f), new Vector2(0.5f, 0f));
            lineRt.anchoredPosition = Vector2.zero;
            lineRt.sizeDelta = new Vector2(0f, 2f);
            Image lineImg = lineGo.GetComponent<Image>();
            lineImg.sprite = pixel;
            lineImg.color = UiTheme.Gold;
            lineImg.raycastTarget = false;

            // Panneau grille (CardPanel) — marges latérales PadCard (jamais bord à bord)
            float topInset = UiTheme.PadCard + HeaderHeight + UiTheme.SpacingRow;
            float bottomInset = FooterHeight + HintHeight + UiTheme.PadCard * 2f;

            GameObject panelGo = CreateUi(summaryGo.transform, "GridPanel");
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            SetAnchors(panelRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
            panelRt.offsetMin = new Vector2(UiTheme.PadCard, bottomInset);
            panelRt.offsetMax = new Vector2(-UiTheme.PadCard, -topInset);
            Image panelImg = panelGo.GetComponent<Image>();
            panelImg.sprite = card;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = UiTheme.CardPanel;
            panelImg.raycastTarget = false;

            // Filet or fin en haut du panneau
            GameObject panelLine = CreateUi(panelGo.transform, "PanelGoldLine");
            RectTransform panelLineRt = panelLine.GetComponent<RectTransform>();
            SetAnchors(panelLineRt, new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0.5f, 1f));
            panelLineRt.anchoredPosition = new Vector2(0f, -6f);
            panelLineRt.sizeDelta = new Vector2(0f, 1f);
            Image panelLineImg = panelLine.GetComponent<Image>();
            panelLineImg.sprite = pixel;
            panelLineImg.color = UiTheme.Gold;
            panelLineImg.raycastTarget = false;

            GameObject gridGo = CreateUi(panelGo.transform, "GridContainer");
            RectTransform gridRt = gridGo.GetComponent<RectTransform>();
            Object.DestroyImmediate(gridGo.GetComponent<Image>());
            StretchFull(gridRt);
            // Pas d'inset supplémentaire : GachaSummaryGridFitter gère padding = PadCard.

            GridLayoutGroup grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cellW, cellH); // placeholder — Fit() au runtime
            grid.spacing = new Vector2(UiTheme.SpacingRow, UiTheme.SpacingRow);
            grid.padding = new RectOffset(
                UiTheme.PadCard, UiTheme.PadCard, UiTheme.PadCard, UiTheme.PadCard);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            GachaSummaryGridFitter fitter = gridGo.AddComponent<GachaSummaryGridFitter>();
            SerializedObject fitterSo = new SerializedObject(fitter);
            SerializedProperty gridProp = fitterSo.FindProperty("grid");
            if (gridProp != null)
                gridProp.objectReferenceValue = grid;
            fitterSo.ApplyModifiedPropertiesWithoutUndo();

            // Single container (centré, inactif)
            GameObject singleGo = CreateUi(summaryGo.transform, "SingleContainer");
            RectTransform singleRt = singleGo.GetComponent<RectTransform>();
            Object.DestroyImmediate(singleGo.GetComponent<Image>());
            SetAnchors(singleRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            singleRt.sizeDelta = new Vector2(SingleCardW + UiTheme.PadCard, SingleCardH + UiTheme.PadCard);
            singleRt.anchoredPosition = new Vector2(0f, 20f);
            singleGo.SetActive(false);

            // Hint
            TextMeshProUGUI hint = CreateTmp(
                summaryGo.transform, "HintText", font, UiTheme.FontBody, UiTheme.TextMuted,
                TextAlignmentOptions.Center);
            hint.text = "Touchez un personnage pour ouvrir sa fiche";
            RectTransform hintRt = hint.rectTransform;
            SetAnchors(hintRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            hintRt.anchoredPosition = new Vector2(0f, FooterHeight + UiTheme.PadCompact);
            hintRt.sizeDelta = new Vector2(-(UiTheme.PadCard * 2), HintHeight);

            // Footer
            GameObject footerGo = CreateUi(summaryGo.transform, "Footer");
            RectTransform footerRt = footerGo.GetComponent<RectTransform>();
            Object.DestroyImmediate(footerGo.GetComponent<Image>());
            SetAnchors(footerRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            footerRt.anchoredPosition = new Vector2(0f, UiTheme.PadCompact);
            footerRt.sizeDelta = new Vector2(-(UiTheme.PadCard * 2), FooterHeight);

            HorizontalLayoutGroup footerHlg = footerGo.AddComponent<HorizontalLayoutGroup>();
            footerHlg.spacing = UiTheme.SpacingRow;
            footerHlg.childForceExpandHeight = true;
            footerHlg.childForceExpandWidth = true;
            footerHlg.childControlWidth = true;
            footerHlg.childControlHeight = true;
            footerHlg.padding = new RectOffset(0, 0, 8, 8);

            Button repullBtn;
            TextMeshProUGUI repullLabel;
            TextMeshProUGUI repullCost;
            CreateRepullButton(
                footerGo.transform, card, font, out repullBtn, out repullLabel, out repullCost);

            Button closeBtn;
            TextMeshProUGUI closeLabelUnused;
            CreateGhostButton(
                footerGo.transform, "CloseButton", "Fermer", card, font,
                out closeBtn, out closeLabelUnused);

            HubManager hubManager = Object.FindObjectOfType<HubManager>(true);
            CharacterDetailPopup detailPopup = Object.FindObjectOfType<CharacterDetailPopup>(true);

            SerializedObject so = new SerializedObject(gac);
            SetObj(so, "summaryScene", summaryGo);
            SetObj(so, "gridContainer", gridGo.transform);
            SetObj(so, "singleContainer", singleGo.transform);
            SetObj(so, "summaryEntryPrefab", gridPrefab);
            SetObj(so, "singleCardPrefab", singlePrefab);
            SetObj(so, "closeButton", closeBtn);
            SetObj(so, "repullButton", repullBtn);
            SetObj(so, "repullLabelText", repullLabel);
            SetObj(so, "repullCostText", repullCost);
            SetObj(so, "hintText", hint);
            SetObj(so, "hubManager", hubManager);
            SetObj(so, "characterDetailPopup", detailPopup);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gac);

            Debug.Log(
                "[GachaSummaryBuilder] SummaryScene reconstruit — " +
                "cell=" + cellW.ToString("F1") + "x" + cellH.ToString("F1") +
                " navClearance=" + NavClearance +
                " hubManager=" + (hubManager != null) +
                " detailPopup=" + (detailPopup != null));
        }

        // ═══════════════════════════════════════════
        // UI HELPERS — BOUTONS
        // ═══════════════════════════════════════════

        private static void CreateRepullButton(
            Transform parent,
            Sprite card,
            TMP_FontAsset font,
            out Button button,
            out TextMeshProUGUI label,
            out TextMeshProUGUI cost)
        {
            GameObject go = CreateUi(parent, "RepullButton");
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1.5f;
            le.preferredHeight = FooterBtnHeight;
            le.minHeight = FooterBtnHeight;

            Image frame = go.GetComponent<Image>();
            frame.sprite = card;
            frame.type = Image.Type.Sliced;
            frame.color = UiTheme.Gold;

            button = go.AddComponent<Button>();
            button.targetGraphic = frame;

            GameObject fillGo = CreateUi(go.transform, "Fill");
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            StretchFull(fillRt);
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);
            Image fill = fillGo.GetComponent<Image>();
            fill.sprite = card;
            fill.type = Image.Type.Sliced;
            fill.color = UiTheme.CardPanel;
            fill.raycastTarget = false;

            GameObject texts = CreateUi(go.transform, "Labels");
            Object.DestroyImmediate(texts.GetComponent<Image>());
            RectTransform textsRt = texts.GetComponent<RectTransform>();
            StretchFull(textsRt);
            VerticalLayoutGroup vlg = texts.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(8, 8, 6, 6);

            label = CreateTmp(
                texts.transform, "RepullLabel", font, UiTheme.CardFontButton, UiTheme.TextPrimary,
                TextAlignmentOptions.Center);
            label.text = "Invoquer à nouveau ×10";
            LayoutElement labelLe = label.gameObject.AddComponent<LayoutElement>();
            labelLe.preferredHeight = 36f;

            cost = CreateTmp(
                texts.transform, "RepullCost", font, UiTheme.FontLabel, UiTheme.Gold,
                TextAlignmentOptions.Center);
            cost.text = "1000 Tals";
            LayoutElement costLe = cost.gameObject.AddComponent<LayoutElement>();
            costLe.preferredHeight = 26f;
        }

        private static void CreateGhostButton(
            Transform parent,
            string name,
            string label,
            Sprite card,
            TMP_FontAsset font,
            out Button button,
            out TextMeshProUGUI labelTmp)
        {
            GameObject go = CreateUi(parent, name);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = FooterBtnHeight;
            le.minHeight = FooterBtnHeight;

            Image frame = go.GetComponent<Image>();
            frame.sprite = card;
            frame.type = Image.Type.Sliced;
            frame.color = UiTheme.CardBorderMuted;

            button = go.AddComponent<Button>();
            button.targetGraphic = frame;

            GameObject fillGo = CreateUi(go.transform, "Fill");
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            StretchFull(fillRt);
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);
            Image fill = fillGo.GetComponent<Image>();
            fill.sprite = card;
            fill.type = Image.Type.Sliced;
            fill.color = UiTheme.CardPanel;
            fill.raycastTarget = false;

            labelTmp = CreateTmp(
                go.transform, "Text", font, UiTheme.CardFontButton, UiTheme.TextSecondary,
                TextAlignmentOptions.Center);
            labelTmp.text = label;
            StretchFull(labelTmp.rectTransform);
        }

        // ═══════════════════════════════════════════
        // UI HELPERS — CARTE
        // ═══════════════════════════════════════════

        private static Image CreateGlow(
            Transform parent, Sprite glowSprite, Material glowMat, float size)
        {
            GameObject go = CreateUi(parent, "SsrGlow");
            RectTransform rt = go.GetComponent<RectTransform>();
            SetAnchors(rt, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(size, size);
            Image img = go.GetComponent<Image>();
            img.sprite = glowSprite;
            if (glowMat != null)
                img.material = glowMat;
            Color c = UiTheme.RaritySSR;
            c.a = 0f;
            img.color = c;
            img.raycastTarget = false;
            go.SetActive(false);
            return img;
        }

        private static Image CreateTopBorder(Transform parent, Sprite pixel, float height)
        {
            GameObject go = CreateUi(parent, "RarityTopBorder");
            RectTransform rt = go.GetComponent<RectTransform>();
            SetAnchors(rt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
            Image img = go.GetComponent<Image>();
            img.sprite = pixel;
            img.color = UiTheme.RaritySR;
            img.raycastTarget = false;
            return img;
        }

        private static void CreateIconBlock(
            Transform parent,
            float size,
            TMP_FontAsset font,
            float fallbackFontSize,
            out Image iconImg,
            out TextMeshProUGUI fallback)
        {
            GameObject area = CreateUi(parent, "IconArea");
            RectTransform areaRt = area.GetComponent<RectTransform>();
            SetAnchors(areaRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            areaRt.anchoredPosition = new Vector2(0f, -(RarityBorderH + UiTheme.PadCompact));
            areaRt.sizeDelta = new Vector2(size, size);

            // Fond fallback (jamais blanc)
            Image areaBg = area.GetComponent<Image>();
            areaBg.sprite = UiGen.Card;
            areaBg.type = Image.Type.Sliced;
            areaBg.color = UiTheme.CardPanelEntry;
            areaBg.raycastTarget = false;

            GameObject iconGo = CreateUi(area.transform, "IconImage");
            StretchFull(iconGo.GetComponent<RectTransform>());
            iconImg = iconGo.GetComponent<Image>();
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            fallback = CreateTmp(
                area.transform, "IconFallbackText", font, fallbackFontSize, UiTheme.TextSecondary,
                TextAlignmentOptions.Center);
            fallback.text = "?";
            fallback.fontStyle = FontStyles.Bold;
            StretchFull(fallback.rectTransform);
            fallback.gameObject.SetActive(false);
        }

        private static void CreateStatusChip(
            Transform parent,
            TMP_FontAsset font,
            float fontSize,
            out Image frame,
            out TextMeshProUGUI text)
        {
            GameObject go = CreateUi(parent, "StatusChip");
            frame = go.GetComponent<Image>();
            frame.sprite = UiGen.Card;
            frame.type = Image.Type.Sliced;
            Color fc = UiTheme.BadgeNew;
            fc.a = 0.28f;
            frame.color = fc;
            frame.raycastTarget = false;

            text = CreateTmp(
                go.transform, "StatusChipText", font, fontSize, UiTheme.BadgeNew,
                TextAlignmentOptions.Center);
            text.text = "NOUVEAU !";
            StretchFull(text.rectTransform);
            RectTransform textRt = text.rectTransform;
            textRt.offsetMin = new Vector2(6f, 2f);
            textRt.offsetMax = new Vector2(-6f, -2f);
        }

        private static GameObject CreateRateUpBadge(Transform parent, TMP_FontAsset font, float fontSize)
        {
            GameObject go = CreateUi(parent, "RateUpBadge");
            RectTransform rt = go.GetComponent<RectTransform>();
            SetAnchors(rt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            rt.anchoredPosition = new Vector2(-10f, -12f);
            rt.sizeDelta = new Vector2(88f, 32f);

            Image img = go.GetComponent<Image>();
            img.sprite = UiGen.Card;
            img.type = Image.Type.Sliced;
            img.color = UiTheme.Gold;
            img.raycastTarget = false;

            TextMeshProUGUI tmp = CreateTmp(
                go.transform, "Label", font, fontSize, UiTheme.CardPanel,
                TextAlignmentOptions.Center);
            tmp.text = "UP";
            tmp.fontStyle = FontStyles.Bold;
            StretchFull(tmp.rectTransform);

            go.SetActive(false);
            return go;
        }

        private static void WireEntry(
            PullResultEntryUI entry,
            Image iconImg,
            TextMeshProUGUI fallback,
            Image rarityTop,
            TextMeshProUGUI name,
            TextMeshProUGUI chipText,
            Image chipFrame,
            GameObject rateUp,
            Button btn,
            Image ssrGlow,
            CanvasGroup cg)
        {
            SerializedObject so = new SerializedObject(entry);
            SetObj(so, "iconImage", iconImg);
            SetObj(so, "iconFallbackText", fallback);
            SetObj(so, "rarityTopBorder", rarityTop);
            SetObj(so, "nameText", name);
            SetObj(so, "statusChipText", chipText);
            SetObj(so, "statusChipFrame", chipFrame);
            SetObj(so, "rateUpBadge", rateUp);
            SetObj(so, "cardButton", btn);
            SetObj(so, "ssrGlow", ssrGlow);
            SetObj(so, "canvasGroup", cg);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ═══════════════════════════════════════════
        // UTILITAIRES
        // ═══════════════════════════════════════════

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null)
                return t.gameObject;

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateUi(Transform parent, string name)
        {
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetAnchors(
            RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float size,
            Color color,
            TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetObj(SerializedObject so, string prop, Object value)
        {
            SerializedProperty p = so.FindProperty(prop);
            if (p == null)
            {
                Debug.LogWarning("[GachaSummaryBuilder] Propriété introuvable : " + prop);
                return;
            }

            p.objectReferenceValue = value;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
