#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using ChezArthur.UI;
using ChezArthur.Hub.Pages;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// OUTIL DESTRUCTIF — fenêtre d'usage : Gate 3 de la refonte fiche
    /// personnage uniquement. Reconstruit le CONTENU des prefabs fiche.
    /// NE JAMAIS ré-exécuter après les ajustements manuels post-Gate 3 :
    /// toute retouche manuelle serait écrasée.
    /// </summary>
    public static class CharacterDetailPopupBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string PopupGuid = "4a21464fbb824924ab517e38b180d7a1";
        private const string TabPrefabPath = "Assets/_Project/Prefabs/UI/SpecTabButton.prefab";
        private const string PassivePrefabPath = "Assets/_Project/Prefabs/UI/PassiveEntry.prefab";
        private const string SepPrefabPath = "Assets/_Project/Prefabs/UI/SeparatorUI.prefab";
        private const string SpriteCardRoundedPath =
            "Assets/_Project/Sprites/Placeholders/card_rounded.png";
        private const string SpriteWhitePixelPath = "Assets/_Project/Art/UI/ui_white_pixel.png";
        private const string SpriteArrowDownPath =
            "Assets/_Project/Sprites/CharacterCard/Team page- down.png";
        private const string SpriteArrowUpPath =
            "Assets/_Project/Sprites/CharacterCard/Team page- up.png";

        private const float PanelClosedHeight = 440f;
        private const float HeaderHeight = 240f;
        private const float StatsPanelHeight = 440f;
        private const float FooterHeight = 110f;
        private const float FooterBottomPad = 16f;
        private const float TabBarHeight = 88f;
        private const float StatsRowHeight = 124f;
        private const float UnderlineHeight = 6f;
        private const float AccentBorderWidth = 5f;
        private const float PanelTopBorderHeight = 5f;
        private const float HitPadExtra = 40f;
        private const float ChipWidth = 120f;
        private const float ChipHeight = 52f;
        /// <summary> Offset bas de TabBar depuis le haut du panneau. </summary>
        private const float TabBarFromTop = -18f;
        /// <summary> Offset bas de StatsRow depuis le haut (sous les tabs). </summary>
        private const float StatsRowFromTop = -118f;
        /// <summary> Inset haut ExpandedZone (sous tabs+stats). </summary>
        private const float ExpandedTopInset = 250f;

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI/Reconstruire fiche personnage (fenêtre Gate 3)")]
        private static void RebuildMenu()
        {
            bool ok = EditorUtility.DisplayDialog(
                "Reconstruire fiche personnage",
                "OUTIL DESTRUCTIF — reconstruit le contenu des prefabs fiche.\n\n" +
                "Ne pas ré-exécuter après des retouches manuelles post-Gate 3.\n\nContinuer ?",
                "Reconstruire",
                "Annuler");
            if (!ok)
                return;

            try
            {
                RebuildAll();
                Debug.Log("[CharacterDetailPopupBuilder] Reconstruction terminée.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CharacterDetailPopupBuilder] Échec : {ex}");
            }
        }

        private static void RebuildAll()
        {
            Sprite card = LoadSprite(SpriteCardRoundedPath);
            // Force le pixel blanc réel (évite le bug « grisé rouge » si le PNG était teinté).
            Sprite pixel = UiGen.SolidWhite ?? LoadSprite(SpriteWhitePixelPath);
            Sprite arrowDown = LoadSprite(SpriteArrowDownPath);
            Sprite arrowUp = LoadSprite(SpriteArrowUpPath);
            TMP_FontAsset font = UiGen.LoadFont();

            RebuildTabPrefab(pixel, font);
            RebuildPassivePrefab(card, pixel, font);
            RebuildSeparatorPrefab(pixel);
            RebuildPopupPrefab(card, pixel, arrowDown, arrowUp, font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ═══════════════════════════════════════════
        // POPUP
        // ═══════════════════════════════════════════

        private static void RebuildPopupPrefab(
            Sprite card, Sprite pixel, Sprite arrowDown, Sprite arrowUp, TMP_FontAsset font)
        {
            string popupPath = AssetDatabase.GUIDToAssetPath(PopupGuid);
            if (string.IsNullOrEmpty(popupPath))
            {
                Debug.LogError("[CharacterDetailPopupBuilder] Prefab popup introuvable (guid).");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(popupPath);
            try
            {
                CharacterDetailPopup popup = root.GetComponent<CharacterDetailPopup>();
                if (popup == null)
                    popup = root.AddComponent<CharacterDetailPopup>();

                CanvasGroup cg = root.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = root.AddComponent<CanvasGroup>();

                Image rootImage = root.GetComponent<Image>();
                if (rootImage == null)
                    rootImage = root.AddComponent<Image>();
                rootImage.sprite = pixel;
                rootImage.color = UiTheme.CardArtworkDim;
                rootImage.raycastTarget = true;
                rootImage.type = Image.Type.Simple;

                RectTransform rootRt = root.GetComponent<RectTransform>();
                StretchFull(rootRt);

                DestroyAllChildren(root.transform);

                // Artwork
                GameObject artworkGo = CreateChild(root.transform, "Artwork");
                StretchFull(artworkGo.GetComponent<RectTransform>());
                RawImage raw = artworkGo.AddComponent<RawImage>();
                raw.texture = null;
                raw.raycastTarget = false;
                CharacterArtworkView artworkView = artworkGo.AddComponent<CharacterArtworkView>();
                SetObject(artworkView, "rawImage", raw);

                // ArtworkDimOverlay
                GameObject dimGo = CreateChild(root.transform, "ArtworkDimOverlay");
                StretchFull(dimGo.GetComponent<RectTransform>());
                Image dimImg = dimGo.AddComponent<Image>();
                dimImg.sprite = pixel;
                dimImg.color = UiTheme.CardArtworkDim;
                dimImg.raycastTarget = false;
                dimGo.SetActive(false);

                // Header
                GameObject headerGo = CreateChild(root.transform, "Header");
                RectTransform headerRt = headerGo.GetComponent<RectTransform>();
                SetAnchors(headerRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
                headerRt.anchoredPosition = Vector2.zero;
                headerRt.sizeDelta = new Vector2(0f, HeaderHeight);
                Image headerImg = headerGo.AddComponent<Image>();
                headerImg.sprite = pixel;
                headerImg.color = UiTheme.CardHeaderScrim;
                headerImg.raycastTarget = false;

                TextMeshProUGUI nameText = CreateTmp(
                    headerGo.transform, "NameText", font, UiTheme.CardFontName, UiTheme.TextPrimary,
                    TextAlignmentOptions.Left);
                RectTransform nameRt = nameText.rectTransform;
                SetAnchors(nameRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
                nameRt.anchoredPosition = new Vector2(40f, -28f);
                nameRt.sizeDelta = new Vector2(620f, 56f);

                TextMeshProUGUI levelText = CreateTmp(
                    headerGo.transform, "LevelText", font, UiTheme.CardFontMeta, UiTheme.TextSecondary,
                    TextAlignmentOptions.Left);
                RectTransform levelRt = levelText.rectTransform;
                SetAnchors(levelRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
                levelRt.anchoredPosition = new Vector2(40f, -100f);
                levelRt.sizeDelta = new Vector2(180f, 36f);

                TextMeshProUGUI typeText = CreateTmp(
                    headerGo.transform, "TypeText", font, UiTheme.CardFontMeta, UiTheme.TextSecondary,
                    TextAlignmentOptions.Left);
                RectTransform typeRt = typeText.rectTransform;
                SetAnchors(typeRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
                typeRt.anchoredPosition = new Vector2(230f, -100f);
                typeRt.sizeDelta = new Vector2(400f, 36f);

                // RarityChip : cadre rareté (runtime) + fill sombre + texte clair
                GameObject chipGo = CreateChild(headerGo.transform, "RarityChip");
                RectTransform chipRt = chipGo.GetComponent<RectTransform>();
                SetAnchors(chipRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
                chipRt.anchoredPosition = new Vector2(40f, -150f);
                chipRt.sizeDelta = new Vector2(ChipWidth, ChipHeight);
                Image chipFrame = chipGo.AddComponent<Image>();
                chipFrame.sprite = card;
                chipFrame.type = Image.Type.Sliced;
                chipFrame.color = Color.white;

                GameObject chipFillGo = CreateChild(chipGo.transform, "Fill");
                RectTransform chipFillRt = chipFillGo.GetComponent<RectTransform>();
                StretchFull(chipFillRt);
                chipFillRt.offsetMin = new Vector2(3f, 3f);
                chipFillRt.offsetMax = new Vector2(-3f, -3f);
                Image chipFill = chipFillGo.AddComponent<Image>();
                chipFill.sprite = card;
                chipFill.type = Image.Type.Sliced;
                chipFill.color = UiTheme.CardPanel;
                chipFill.raycastTarget = false;

                TextMeshProUGUI chipText = CreateTmp(
                    chipGo.transform, "RarityChipText", font, UiTheme.CardFontChip, UiTheme.TextPrimary,
                    TextAlignmentOptions.Center);
                StretchFull(chipText.rectTransform);
                chipText.text = "SSR";

                // SwitchArtworkButton
                GameObject switchGo = CreateChild(headerGo.transform, "SwitchArtworkButton");
                RectTransform switchRt = switchGo.GetComponent<RectTransform>();
                SetAnchors(switchRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
                switchRt.anchoredPosition = new Vector2(-40f, -36f);
                switchRt.sizeDelta = new Vector2(96f, 96f);
                Image switchImg = switchGo.AddComponent<Image>();
                switchImg.sprite = card;
                switchImg.type = Image.Type.Sliced;
                switchImg.color = UiTheme.CardBorderMuted;
                Button switchBtn = switchGo.AddComponent<Button>();
                switchBtn.interactable = false;
                switchBtn.targetGraphic = switchImg;
                CreateIcon(switchGo.transform, "ArrowDownIcon", arrowDown, new Vector2(-18f, 0f));
                CreateIcon(switchGo.transform, "ArrowUpIcon", arrowUp, new Vector2(18f, 0f));

                // StatsPanel
                GameObject panelGo = CreateChild(root.transform, "StatsPanel");
                RectTransform panelRt = panelGo.GetComponent<RectTransform>();
                SetAnchors(panelRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
                panelRt.anchoredPosition = Vector2.zero;
                panelRt.sizeDelta = new Vector2(0f, StatsPanelHeight);
                Image panelImg = panelGo.AddComponent<Image>();
                panelImg.sprite = card;
                panelImg.type = Image.Type.Sliced;
                panelImg.color = UiTheme.CardPanelCollapsed;

                // PanelTopBorder
                GameObject topBorderGo = CreateChild(panelGo.transform, "PanelTopBorder");
                RectTransform topBorderRt = topBorderGo.GetComponent<RectTransform>();
                SetAnchors(topBorderRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
                topBorderRt.anchoredPosition = Vector2.zero;
                topBorderRt.sizeDelta = new Vector2(0f, PanelTopBorderHeight);
                Image topBorderImg = topBorderGo.AddComponent<Image>();
                topBorderImg.sprite = pixel;
                topBorderImg.color = Color.white;

                // ExpandButton
                GameObject expandGo = CreateChild(panelGo.transform, "ExpandButton");
                RectTransform expandRt = expandGo.GetComponent<RectTransform>();
                SetAnchors(expandRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f));
                expandRt.anchoredPosition = new Vector2(0f, 4f);
                expandRt.sizeDelta = new Vector2(150f, 64f);
                Image expandImg = expandGo.AddComponent<Image>();
                expandImg.sprite = card;
                expandImg.type = Image.Type.Sliced;
                expandImg.color = UiTheme.CardPanel;
                Button expandBtn = expandGo.AddComponent<Button>();
                expandBtn.targetGraphic = expandImg;

                GameObject hitPad = CreateChild(expandGo.transform, "HitPad");
                RectTransform hitRt = hitPad.GetComponent<RectTransform>();
                StretchFull(hitRt);
                hitRt.offsetMin = new Vector2(0f, -HitPadExtra);
                hitRt.offsetMax = Vector2.zero;
                Image hitImg = hitPad.AddComponent<Image>();
                hitImg.sprite = pixel;
                hitImg.color = Color.clear;
                hitImg.raycastTarget = true;

                GameObject arrowGo = CreateChild(expandGo.transform, "ExpandArrowIcon");
                RectTransform arrowRt = arrowGo.GetComponent<RectTransform>();
                SetAnchors(arrowRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                arrowRt.sizeDelta = new Vector2(40f, 40f);
                Image arrowImg = arrowGo.AddComponent<Image>();
                arrowImg.sprite = arrowDown;
                arrowImg.color = UiTheme.Gold;
                arrowImg.raycastTarget = false;

                // TabBar (haut du panneau — toujours visible plié/déplié)
                GameObject tabBarGo = CreateChild(panelGo.transform, "TabBar");
                RectTransform tabBarRt = tabBarGo.GetComponent<RectTransform>();
                SetAnchors(tabBarRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
                tabBarRt.anchoredPosition = new Vector2(0f, TabBarFromTop);
                tabBarRt.sizeDelta = new Vector2(0f, TabBarHeight);
                HorizontalLayoutGroup tabHlg = tabBarGo.AddComponent<HorizontalLayoutGroup>();
                tabHlg.spacing = UiTheme.SpacingRow;
                tabHlg.padding = new RectOffset(UiTheme.PadCard, UiTheme.PadCard, 0, 0);
                tabHlg.childForceExpandWidth = true;
                tabHlg.childForceExpandHeight = true;
                tabHlg.childControlWidth = true;
                tabHlg.childControlHeight = true;

                // StatsRow — au-dessus du footer en plié (panneau 440 : marge claire)
                GameObject statsRowGo = CreateChild(panelGo.transform, "StatsRow");
                RectTransform statsRowRt = statsRowGo.GetComponent<RectTransform>();
                SetAnchors(statsRowRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
                statsRowRt.anchoredPosition = new Vector2(0f, StatsRowFromTop);
                statsRowRt.sizeDelta = new Vector2(-(UiTheme.PadCard * 2), StatsRowHeight);
                Image statsRowImg = statsRowGo.AddComponent<Image>();
                statsRowImg.sprite = card;
                statsRowImg.type = Image.Type.Sliced;
                statsRowImg.color = UiTheme.CardPanelEntry;
                HorizontalLayoutGroup statsHlg = statsRowGo.AddComponent<HorizontalLayoutGroup>();
                statsHlg.childForceExpandWidth = true;
                statsHlg.childForceExpandHeight = true;
                statsHlg.childControlWidth = true;
                statsHlg.childControlHeight = true;
                statsHlg.spacing = 0f;
                statsHlg.padding = new RectOffset(8, 8, 8, 8);

                TextMeshProUGUI hpValue = CreateStatColumn(statsRowGo.transform, "PV", font);
                CreateStatHairline(statsRowGo.transform, pixel);
                TextMeshProUGUI atkValue = CreateStatColumn(statsRowGo.transform, "ATK", font);
                CreateStatHairline(statsRowGo.transform, pixel);
                TextMeshProUGUI defValue = CreateStatColumn(statsRowGo.transform, "DEF", font);
                CreateStatHairline(statsRowGo.transform, pixel);
                TextMeshProUGUI speedValue = CreateStatColumn(statsRowGo.transform, "VIT", font);

                // Pas de teaser lore en plié — lore uniquement en déplié.
                TextMeshProUGUI preview = null;

                // ExpandedZone — stretch entre stats et footer (scroll au-dessus des boutons)
                GameObject expandedGo = CreateChild(panelGo.transform, "ExpandedZone");
                RectTransform expandedRt = expandedGo.GetComponent<RectTransform>();
                SetAnchors(expandedRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
                expandedRt.offsetMin = new Vector2(0f, FooterHeight + FooterBottomPad + 8f);
                expandedRt.offsetMax = new Vector2(0f, -ExpandedTopInset);
                expandedGo.SetActive(false);

                GameObject scrollGo = CreateChild(expandedGo.transform, "ContentScrollView");
                StretchFull(scrollGo.GetComponent<RectTransform>());
                ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;

                GameObject viewportGo = CreateChild(scrollGo.transform, "Viewport");
                RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
                SetAnchors(viewportRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
                viewportRt.offsetMin = Vector2.zero;
                viewportRt.offsetMax = Vector2.zero;
                Image viewportImg = viewportGo.AddComponent<Image>();
                viewportImg.sprite = pixel;
                viewportImg.color = Color.clear;
                viewportImg.raycastTarget = true;
                viewportGo.AddComponent<RectMask2D>();

                GameObject contentGo = CreateChild(viewportGo.transform, "ContentContainer");
                RectTransform contentRt = contentGo.GetComponent<RectTransform>();
                SetAnchors(contentRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = new Vector2(0f, 0f);
                VerticalLayoutGroup contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
                contentVlg.spacing = 20f;
                // Padding bas généreux pour que le dernier passif ne soit pas collé au footer.
                contentVlg.padding = new RectOffset(
                    UiTheme.PadCard, UiTheme.PadCard, 8, 48);
                contentVlg.childForceExpandWidth = true;
                contentVlg.childForceExpandHeight = false;
                contentVlg.childControlWidth = true;
                contentVlg.childControlHeight = true;
                ContentSizeFitter contentCsf = contentGo.AddComponent<ContentSizeFitter>();
                contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Bloc lore (guillemets + liseré) — séparé des passifs
                GameObject loreGo = CreateChild(contentGo.transform, "LoreBlock");
                Image loreBg = loreGo.AddComponent<Image>();
                loreBg.sprite = card;
                loreBg.type = Image.Type.Sliced;
                loreBg.color = UiTheme.CardPanelEntry;
                VerticalLayoutGroup loreVlg = loreGo.AddComponent<VerticalLayoutGroup>();
                loreVlg.padding = new RectOffset(20, 20, 16, 16);
                loreVlg.spacing = 8f;
                loreVlg.childForceExpandWidth = true;
                loreVlg.childControlHeight = true;
                loreVlg.childControlWidth = true;
                LayoutElement loreLe = loreGo.AddComponent<LayoutElement>();
                loreLe.minHeight = 80f;

                GameObject loreAccentGo = CreateChild(loreGo.transform, "LoreAccent");
                RectTransform loreAccentRt = loreAccentGo.GetComponent<RectTransform>();
                SetAnchors(loreAccentRt, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
                loreAccentRt.sizeDelta = new Vector2(4f, 0f);
                Image loreAccent = loreAccentGo.AddComponent<Image>();
                loreAccent.sprite = pixel;
                loreAccent.color = UiTheme.RoleNeutral; // teinté runtime selon la spé
                LayoutElement loreAccentLe = loreAccentGo.AddComponent<LayoutElement>();
                loreAccentLe.ignoreLayout = true;

                TextMeshProUGUI loreLabel = CreateTmp(
                    loreGo.transform, "LoreLabel", font, UiTheme.CardFontStatLabel, UiTheme.Gold,
                    TextAlignmentOptions.Left);
                loreLabel.text = "LORE";
                loreLabel.fontStyle = FontStyles.Bold;

                TextMeshProUGUI backstoryFull = CreateTmp(
                    loreGo.transform, "BackstoryText", font, UiTheme.CardFontBody,
                    UiTheme.TextSecondary, TextAlignmentOptions.TopLeft);
                backstoryFull.fontStyle = FontStyles.Italic;
                backstoryFull.enableWordWrapping = true;
                LayoutElement backstoryLe = backstoryFull.gameObject.AddComponent<LayoutElement>();
                backstoryLe.minHeight = 48f;
                StretchWidthPreferred(backstoryFull.rectTransform);

                scroll.viewport = viewportRt;
                scroll.content = contentRt;

                // Footer opaque (rien ne fuit dessous)
                GameObject footerGo = CreateChild(panelGo.transform, "Footer");
                RectTransform footerRt = footerGo.GetComponent<RectTransform>();
                SetAnchors(footerRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
                footerRt.anchoredPosition = new Vector2(0f, FooterBottomPad);
                footerRt.sizeDelta = new Vector2(0f, FooterHeight);
                Image footerBg = footerGo.AddComponent<Image>();
                footerBg.sprite = pixel;
                footerBg.color = UiTheme.CardPanel;
                footerBg.raycastTarget = true;
                HorizontalLayoutGroup footerHlg = footerGo.AddComponent<HorizontalLayoutGroup>();
                footerHlg.padding = new RectOffset(UiTheme.PadCard, UiTheme.PadCard, 0, 0);
                footerHlg.spacing = 20f;
                footerHlg.childForceExpandHeight = true;
                footerHlg.childControlWidth = true;
                footerHlg.childControlHeight = true;

                Image primaryFrame;
                TextMeshProUGUI addText;
                Button addBtn = CreateFooterButton(
                    footerGo.transform, "AddToTeamButton", "Ajouter à l'équipe",
                    card, pixel, font, 1.4f, out primaryFrame, out addText, true);

                Image closeFrameUnused;
                TextMeshProUGUI closeText;
                Button closeBtn = CreateFooterButton(
                    footerGo.transform, "CloseButton", "Fermer",
                    card, pixel, font, 1f, out closeFrameUnused, out closeText, false);
                closeFrameUnused.color = UiTheme.CardBorderMuted;
                closeText.color = UiTheme.TextSecondary;

                SpecTabButton tabPrefab =
                    AssetDatabase.LoadAssetAtPath<SpecTabButton>(TabPrefabPath);
                PassiveEntryUI passivePrefab =
                    AssetDatabase.LoadAssetAtPath<PassiveEntryUI>(PassivePrefabPath);
                SeparatorUI sepPrefab =
                    AssetDatabase.LoadAssetAtPath<SeparatorUI>(SepPrefabPath);

                SerializedObject so = new SerializedObject(popup);
                SetObj(so, "nameText", nameText);
                SetObj(so, "levelText", levelText);
                SetObj(so, "typeText", typeText);
                SetObj(so, "artworkView", artworkView);
                SetObj(so, "statsPanel", panelRt);
                SetObj(so, "statsPanelBackground", panelImg);
                SetFloat(so, "panelClosedHeight", PanelClosedHeight);
                SetObj(so, "backstoryPreviewText", preview);
                SetObj(so, "tabBar", tabBarGo);
                SetObj(so, "specTabButtonPrefab", tabPrefab);
                SetObj(so, "expandedZone", expandedGo);
                SetObj(so, "expandedZoneRect", expandedRt);
                SetObj(so, "contentContainer", contentGo.transform);
                SetObj(so, "backstoryTextInContainer", backstoryFull);
                SetObj(so, "hpText", hpValue);
                SetObj(so, "atkText", atkValue);
                SetObj(so, "defText", defValue);
                SetObj(so, "speedText", speedValue);
                SetObj(so, "passiveEntryPrefab", passivePrefab);
                SetObj(so, "separatorPrefab", sepPrefab);
                SetObj(so, "expandButton", expandBtn);
                SetObj(so, "expandArrowIcon", arrowImg);
                SetObj(so, "arrowExpandDown", arrowDown);
                SetObj(so, "arrowExpandUp", arrowUp);
                SetObj(so, "addToTeamButton", addBtn);
                SetObj(so, "addToTeamButtonText", addText);
                SetObj(so, "closeButton", closeBtn);
                SetObj(so, "canvasGroup", cg);
                // teamPageUI : ne pas toucher
                SetObj(so, "panelTopBorder", topBorderImg);
                SetObj(so, "loreAccentBorder", loreAccent);
                SetObj(so, "rarityChipText", chipText);
                SetObj(so, "rarityChipFrame", chipFrame);
                SetObj(so, "primaryButtonFrame", primaryFrame);
                SetObj(so, "switchArtworkButton", switchBtn);
                SetObj(so, "artworkDimOverlay", dimImg);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, popupPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ═══════════════════════════════════════════
        // TAB / PASSIVE / SEPARATOR
        // ═══════════════════════════════════════════

        private static void RebuildTabPrefab(Sprite pixel, TMP_FontAsset font)
        {
            EditPrefab(TabPrefabPath, root =>
            {
                DestroyAllChildren(root.transform);
                foreach (Component c in root.GetComponents<Component>())
                {
                    if (c is Transform || c is RectTransform || c is SpecTabButton
                        || c is CanvasRenderer || c is Image || c is Button || c is LayoutElement)
                        continue;
                    Object.DestroyImmediate(c);
                }

                SpecTabButton tab = root.GetComponent<SpecTabButton>();
                if (tab == null)
                    tab = root.AddComponent<SpecTabButton>();

                Image bg = root.GetComponent<Image>();
                if (bg == null)
                    bg = root.AddComponent<Image>();
                bg.sprite = pixel;
                bg.color = Color.clear;
                bg.raycastTarget = true;

                Button btn = root.GetComponent<Button>();
                if (btn == null)
                    btn = root.AddComponent<Button>();
                btn.targetGraphic = bg;

                LayoutElement le = root.GetComponent<LayoutElement>();
                if (le == null)
                    le = root.AddComponent<LayoutElement>();
                le.minHeight = 88f;
                le.flexibleWidth = 1f;

                TextMeshProUGUI label = CreateTmp(
                    root.transform, "Label", font, UiTheme.CardFontTab, UiTheme.TextMuted,
                    TextAlignmentOptions.Center);
                StretchFull(label.rectTransform);

                GameObject underlineGo = CreateChild(root.transform, "Underline");
                RectTransform underRt = underlineGo.GetComponent<RectTransform>();
                SetAnchors(underRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
                underRt.anchoredPosition = Vector2.zero;
                underRt.sizeDelta = new Vector2(0f, UnderlineHeight);
                Image underImg = underlineGo.AddComponent<Image>();
                underImg.sprite = pixel;
                underImg.color = UiTheme.RoleNeutral;
                underlineGo.SetActive(false);

                SerializedObject so = new SerializedObject(tab);
                SetObj(so, "button", btn);
                SetObj(so, "labelText", label);
                SetObj(so, "backgroundImage", bg);
                SetObj(so, "underlineImage", underImg);
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void RebuildPassivePrefab(Sprite card, Sprite pixel, TMP_FontAsset font)
        {
            EditPrefab(PassivePrefabPath, root =>
            {
                DestroyAllChildren(root.transform);
                foreach (Component c in root.GetComponents<Component>())
                {
                    if (c is Transform || c is RectTransform || c is PassiveEntryUI
                        || c is CanvasRenderer || c is Image || c is CanvasGroup
                        || c is VerticalLayoutGroup || c is ContentSizeFitter)
                        continue;
                    Object.DestroyImmediate(c);
                }

                PassiveEntryUI entry = root.GetComponent<PassiveEntryUI>();
                if (entry == null)
                    entry = root.AddComponent<PassiveEntryUI>();

                Image bg = root.GetComponent<Image>();
                if (bg == null)
                    bg = root.AddComponent<Image>();
                bg.sprite = pixel;
                bg.color = UiTheme.CardPanelEntry;

                CanvasGroup cg = root.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = root.AddComponent<CanvasGroup>();

                VerticalLayoutGroup vlg = root.GetComponent<VerticalLayoutGroup>();
                if (vlg == null)
                    vlg = root.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(20, 20, 18, 18);
                vlg.spacing = 12f;
                vlg.childForceExpandWidth = true;
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;

                ContentSizeFitter csf = root.GetComponent<ContentSizeFitter>();
                if (csf == null)
                    csf = root.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                GameObject accentGo = CreateChild(root.transform, "AccentBorder");
                RectTransform accentRt = accentGo.GetComponent<RectTransform>();
                SetAnchors(accentRt, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
                accentRt.anchoredPosition = Vector2.zero;
                accentRt.sizeDelta = new Vector2(AccentBorderWidth, 0f);
                Image accentImg = accentGo.AddComponent<Image>();
                accentImg.sprite = pixel;
                accentImg.color = UiTheme.RoleNeutral;
                LayoutElement accentLe = accentGo.AddComponent<LayoutElement>();
                accentLe.ignoreLayout = true;

                GameObject titleRow = CreateChild(root.transform, "TitleRow");
                HorizontalLayoutGroup titleHlg = titleRow.AddComponent<HorizontalLayoutGroup>();
                titleHlg.spacing = 8f;
                titleHlg.childAlignment = TextAnchor.MiddleLeft;
                titleHlg.childForceExpandWidth = false;
                titleHlg.childForceExpandHeight = true;
                titleHlg.childControlWidth = true;
                titleHlg.childControlHeight = true;
                LayoutElement titleRowLe = titleRow.AddComponent<LayoutElement>();
                titleRowLe.minHeight = 32f;

                TextMeshProUGUI nameText = CreateTmp(
                    titleRow.transform, "NameText", font, UiTheme.CardFontTab, UiTheme.TextPrimary,
                    TextAlignmentOptions.Left);
                LayoutElement nameLe = nameText.gameObject.AddComponent<LayoutElement>();
                nameLe.flexibleWidth = 1f;

                GameObject levelChip = CreateChild(titleRow.transform, "LevelChip");
                Image levelChipImg = levelChip.AddComponent<Image>();
                levelChipImg.sprite = card;
                levelChipImg.type = Image.Type.Sliced;
                levelChipImg.color = UiTheme.CardBorderMuted;
                LayoutElement levelChipLe = levelChip.AddComponent<LayoutElement>();
                levelChipLe.preferredWidth = 88f;
                levelChipLe.preferredHeight = 34f;
                TextMeshProUGUI levelText = CreateTmp(
                    levelChip.transform, "LevelLabel", font, UiTheme.CardFontStatLabel, UiTheme.TextSecondary,
                    TextAlignmentOptions.Center);
                StretchFull(levelText.rectTransform);

                Image lockIcon = CreateSimpleIcon(titleRow.transform, "LockIcon", pixel, 32f);
                Image unlockIcon = CreateSimpleIcon(titleRow.transform, "UnlockIcon", pixel, 32f);
                unlockIcon.gameObject.SetActive(false);

                TextMeshProUGUI desc = CreateTmp(
                    root.transform, "DescriptionText", font, UiTheme.CardFontBody, UiTheme.TextSecondary,
                    TextAlignmentOptions.TopLeft);
                desc.enableWordWrapping = true;
                LayoutElement descLe = desc.gameObject.AddComponent<LayoutElement>();
                descLe.minHeight = 24f;
                descLe.flexibleWidth = 1f;

                SerializedObject so = new SerializedObject(entry);
                SetObj(so, "nameText", nameText);
                SetObj(so, "descriptionText", desc);
                SetObj(so, "levelLabelText", levelText);
                SetObj(so, "lockIcon", lockIcon);
                SetObj(so, "unlockIcon", unlockIcon);
                SetObj(so, "canvasGroup", cg);
                SetObj(so, "accentBorder", accentImg);
                SetObj(so, "backgroundImage", bg);
                SetObj(so, "levelChipBackground", levelChipImg);
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void RebuildSeparatorPrefab(Sprite pixel)
        {
            EditPrefab(SepPrefabPath, root =>
            {
                DestroyAllChildren(root.transform);
                foreach (Component c in root.GetComponents<Component>())
                {
                    if (c is Transform || c is RectTransform || c is SeparatorUI
                        || c is CanvasRenderer || c is Image || c is LayoutElement)
                        continue;
                    Object.DestroyImmediate(c);
                }

                SeparatorUI sep = root.GetComponent<SeparatorUI>();
                if (sep == null)
                    sep = root.AddComponent<SeparatorUI>();

                Image img = root.GetComponent<Image>();
                if (img == null)
                    img = root.AddComponent<Image>();
                img.sprite = pixel;
                img.color = UiTheme.CardHairline;

                LayoutElement le = root.GetComponent<LayoutElement>();
                if (le == null)
                    le = root.AddComponent<LayoutElement>();
                le.preferredHeight = 8f;
                le.minHeight = 8f;
                le.flexibleWidth = 1f;

                RectTransform rt = root.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0f, 8f);

                SerializedObject so = new SerializedObject(sep);
                SetObj(so, "separatorImage", img);
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        // ═══════════════════════════════════════════
        // HELPERS UI
        // ═══════════════════════════════════════════

        private static void EditPrefab(string path, System.Action<GameObject> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Button CreateFooterButton(
            Transform parent,
            string name,
            string label,
            Sprite card,
            Sprite pixel,
            TMP_FontAsset font,
            float flexibleWidth,
            out Image frameImage,
            out TextMeshProUGUI labelTmp,
            bool primary)
        {
            GameObject go = CreateChild(parent, name);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = flexibleWidth;
            le.preferredHeight = FooterHeight;
            le.minHeight = FooterHeight;

            frameImage = go.AddComponent<Image>();
            frameImage.sprite = card;
            frameImage.type = Image.Type.Sliced;
            frameImage.color = primary ? Color.white : UiTheme.CardBorderMuted;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = frameImage;

            GameObject fillGo = CreateChild(go.transform, "Fill");
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            StretchFull(fillRt);
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = card;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = UiTheme.CardPanel;
            fillImg.raycastTarget = false;

            labelTmp = CreateTmp(
                go.transform, "Text", font, UiTheme.CardFontButton,
                primary ? UiTheme.TextPrimary : UiTheme.TextSecondary,
                TextAlignmentOptions.Center);
            labelTmp.text = label;
            StretchFull(labelTmp.rectTransform);
            return btn;
        }

        private static TextMeshProUGUI CreateStatColumn(
            Transform parent, string label, TMP_FontAsset font)
        {
            GameObject col = CreateChild(parent, label + "Col");
            LayoutElement colLe = col.AddComponent<LayoutElement>();
            colLe.flexibleWidth = 1f;
            VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 4f;

            TextMeshProUGUI labelTmp = CreateTmp(
                col.transform, "Label", font, UiTheme.CardFontStatLabel, UiTheme.TextMuted,
                TextAlignmentOptions.Center);
            labelTmp.text = label;

            TextMeshProUGUI valueTmp = CreateTmp(
                col.transform, "Value", font, UiTheme.CardFontStatValue, UiTheme.TextPrimary,
                TextAlignmentOptions.Center);
            valueTmp.text = "—";
            return valueTmp;
        }

        private static void CreateStatHairline(Transform parent, Sprite pixel)
        {
            GameObject go = CreateChild(parent, "Hairline");
            Image img = go.AddComponent<Image>();
            img.sprite = pixel;
            img.color = UiTheme.CardHairline;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 2f;
            le.flexibleWidth = 0f;
            le.minWidth = 2f;
        }

        private static void CreateIcon(Transform parent, string name, Sprite sprite, Vector2 pos)
        {
            GameObject go = CreateChild(parent, name);
            RectTransform rt = go.GetComponent<RectTransform>();
            SetAnchors(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(28f, 28f);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = UiTheme.CardBorderMuted;
            img.raycastTarget = false;
        }

        private static Image CreateSimpleIcon(Transform parent, string name, Sprite sprite, float size)
        {
            GameObject go = CreateChild(parent, name);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = UiTheme.TextMuted;
            return img;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float size,
            Color color,
            TextAlignmentOptions align)
        {
            GameObject go = CreateChild(parent, name);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void DestroyAllChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void StretchFull(RectTransform rt)
        {
            if (rt == null)
                return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchWidthPreferred(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 45f);
        }

        private static void SetAnchors(
            RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetObj(SerializedObject so, string field, Object value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null)
                p.objectReferenceValue = value;
        }

        private static void SetFloat(SerializedObject so, string field, float value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p != null)
                p.floatValue = value;
        }

        private static void SetObject(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SetObj(so, field, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
