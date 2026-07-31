#if UNITY_EDITOR
using System.Text;
using ChezArthur.Hub.Pages;
using ChezArthur.Hub.Pages.Invocation;
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
    /// Gate 6.a/6.b/6.c — Portails Invocation + showcase Personnages.
    /// Idempotent, Undo-safe. DRY RUN / APPLIQUER.
    /// </summary>
    public static class InvocationPageRebuilder
    {
        private const string UndoLabel = "Invocation Page Rebuilder 6.c.5";
        private const float ShowcaseHeaderH = 72f;
        private const float ShowcaseSectionTitleH = 32f;
        private const float ShowcaseDotsRowH = 16f;
        private const string PageName = "PageInvocation";
        private const string RootName = "PortalRoot";
        private const string RatesPopupName = "BannerRatesPopup";
        private const string ShowcaseName = "BannerShowcasePanel";
        private const string Tals2Path = "Assets/_Project/Sprites/UI/Tals2.png";
        private const int PortalCount = 5;
        private const float ArrowSize = 48f;
        private const float DotSize = 12f;
        private const float DotGap = 10f;
        private const float HeightFraction = 0.62f;
        private const float SpacingFraction = 0.06f;
        private const float TitleBarH = 72f;
        private const float ActionRowH = 120f;

        private static readonly Color[] PlaceholderTints =
        {
            new Color(0.32f, 0.36f, 0.48f, 0.95f),
            new Color(0.42f, 0.32f, 0.44f, 0.95f),
            new Color(0.28f, 0.40f, 0.40f, 0.95f),
            new Color(0.44f, 0.38f, 0.28f, 0.95f),
            new Color(0.34f, 0.30f, 0.48f, 0.95f)
        };

        [MenuItem("Chez Arthur/Refonte Hub/Invocation — 6.c (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Invocation — 6.c (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Invocation 6.c.1",
                    "Showcase : titres aeres, carte, dechu, focal portrait.\n"
                    + "Page : Portails disponibles (Title dans zone grise).\n\n"
                    + "Ctrl+S Hub ensuite. Continuer ?",
                    "Appliquer",
                    "Annuler"))
                return;

            Run(apply: true);
        }

        private static void Run(bool apply)
        {
            var log = new StringBuilder(16384);
            string mode = apply ? "APPLIQUER" : "DRY RUN";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" InvocationPageRebuilder 6.c.5 — {mode}");
            log.AppendLine(" Harnais v2 — A FAIRE / CONFORMES / ECHECS");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int todo = 0;
            int conforme = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "Hub")
            {
                Debug.LogError("[InvocationPageRebuilder] Ouvre Hub.unity.");
                return;
            }

            log.AppendLine($"Scene : `{scene.name}`");
            log.AppendLine();

            Transform page = FindDeep(scene, PageName);
            if (page == null)
            {
                failed++;
                log.AppendLine("- X PageInvocation introuvable");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            log.AppendLine($"Page : `{GetPath(page)}`");
            log.AppendLine();

            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite iconUp = apply ? TransportIconGenerator.LoadUp() : null;
            Sprite iconDown = apply ? TransportIconGenerator.LoadDown() : null;
            Sprite iconBack = apply ? TransportIconGenerator.LoadBack() : null;
            Sprite talsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Tals2Path);

            // —— Fond ——
            log.AppendLine("## Fond page");
            ProcessSimpleBackground(page, scene, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— BannersScroll ——
            log.AppendLine("## BannersScroll (legacy)");
            ProcessHideNamed(page, "BannersScroll", apply, log, ref todo, ref conforme);
            log.AppendLine();

            // —— Assets ——
            log.AppendLine("## Assets UI");
            if (spriteM == null || spriteS == null)
            {
                failed++;
                log.AppendLine("- X RoundedRect_S/M manquants");
            }
            else
            {
                conforme++;
                log.AppendLine("- RoundedRect_S/M OK");
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Assurer icon_up/down/back + Tals2 — A FAIRE");
            }
            else if (iconUp == null || iconDown == null || iconBack == null)
            {
                failed++;
                log.AppendLine("- X icon_up/down/back manquants");
            }
            else
            {
                conforme++;
                log.AppendLine("- icon_up/down/back OK");
            }

            if (talsSprite == null)
            {
                failed++;
                log.AppendLine($"- X `{Tals2Path}` introuvable");
            }
            else
            {
                conforme++;
                log.AppendLine("- Tals2 OK");
            }

            log.AppendLine();

            // —— PortalRoot structure ——
            log.AppendLine("## PortalRoot (structure 6.a)");
            Transform existingRoot = page.Find(RootName);
            if (existingRoot == null)
            {
                if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] Creer PortalRoot + snap — A FAIRE");
                }
                else if (spriteM != null && iconUp != null && iconDown != null)
                {
                    BuildOrRefreshPortalRoot(
                        page, null, iconUp, iconDown, spriteM, log, ref conforme, ref failed);
                    existingRoot = page.Find(RootName);
                }
            }
            else
            {
                conforme++;
                log.AppendLine("- PortalRoot present (conserve snap) ");
                ValidateExisting(existingRoot, log, ref conforme, ref failed);
            }

            log.AppendLine();

            // —— Titre Portails disponibles ——
            log.AppendLine("## Titre Portails disponibles");
            if (existingRoot == null)
            {
                if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] Titre Portails disponibles — A FAIRE");
                }
            }
            else if (!apply)
            {
                Transform t = existingRoot.Find("PortalsTitle");
                if (t != null)
                {
                    conforme++;
                    log.AppendLine("- PortalsTitle deja present");
                }
                else
                {
                    todo++;
                    log.AppendLine("- [DRY] Creer PortalsTitle — A FAIRE");
                }
            }
            else
            {
                EnsurePortalsAvailableTitle(existingRoot, log, ref conforme);
            }

            log.AppendLine();

            // —— 6.b PortalCards (si pas encore PortalCardUI) ——
            log.AppendLine("## PortalCard UI (6.b)");
            bool cardsReady = existingRoot != null
                              && existingRoot.GetComponentInChildren<PortalCardUI>(true) != null;
            if (existingRoot == null)
            {
                if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] Restyle PortalCard_0..4 — A FAIRE");
                }
                else
                {
                    failed++;
                    log.AppendLine("- X PortalRoot absent — impossible de styler les cartes");
                }
            }
            else if (cardsReady && !apply)
            {
                conforme++;
                log.AppendLine("- PortalCardUI deja presents");
            }
            else if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Restyle PortalCard (artwork/actions/Taux) — A FAIRE");
            }
            else if (spriteM != null && spriteS != null && talsSprite != null)
            {
                if (!cardsReady)
                {
                    StyleAllPortalCards(
                        existingRoot, spriteM, spriteS, talsSprite, log, ref conforme, ref failed);
                }
                else
                {
                    conforme++;
                    log.AppendLine("- PortalCardUI deja presents (skip restyle)");
                }

                ApplyCellSizerFractions(existingRoot, log, ref conforme);
            }

            log.AppendLine();

            // —— BannerRatesPopup ——
            log.AppendLine("## BannerRatesPopup");
            Transform overlay = FindDeep(scene, "OverlayLayer");
            if (overlay == null)
            {
                failed++;
                log.AppendLine("- X OverlayLayer introuvable");
            }
            else if (!apply)
            {
                Transform existingPopup = overlay.Find(RatesPopupName);
                if (existingPopup != null)
                {
                    conforme++;
                    log.AppendLine("- BannerRatesPopup deja present");
                }
                else
                {
                    todo++;
                    log.AppendLine("- [DRY] Creer BannerRatesPopup sous OverlayLayer — A FAIRE");
                }
            }
            else if (spriteM != null && iconBack != null)
            {
                if (overlay.Find(RatesPopupName) == null)
                    BuildOrRefreshRatesPopup(
                        overlay, spriteM, iconBack, log, ref conforme, ref failed);
                else
                {
                    conforme++;
                    log.AppendLine("- BannerRatesPopup deja present");
                }
            }

            log.AppendLine();

            // —— 6.c Showcase ——
            log.AppendLine("## BannerShowcasePanel (6.c)");
            if (overlay == null)
            {
                failed++;
                log.AppendLine("- X OverlayLayer absent — skip showcase");
            }
            else if (!apply)
            {
                todo++;
                log.AppendLine(
                    "- [DRY] Rebuild showcase (titres + carte + dechu + crop) — A FAIRE");
            }
            else if (spriteM != null && spriteS != null && iconBack != null)
            {
                BuildOrRefreshShowcase(
                    overlay, existingRoot, spriteM, spriteS, iconBack, iconUp, iconDown,
                    log, ref conforme, ref failed);
            }

            log.AppendLine();

            // —— Wire page ——
            log.AppendLine("## InvocationPageUI bind");
            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Wire portalCards + rates + showcase + detailPopup — A FAIRE");
            }
            else
            {
                WireInvocationPage(page, scene, log, ref conforme, ref failed);
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

        // ═══════════════════════════════════════════
        // 6.b — Portal cards
        // ═══════════════════════════════════════════

        private static void StyleAllPortalCards(
            Transform root,
            Sprite spriteM,
            Sprite spriteS,
            Sprite talsSprite,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            Transform content = FindDeepUnder(root, "Content");
            if (content == null)
            {
                failed++;
                log.AppendLine("- X Content introuvable sous PortalRoot");
                return;
            }

            for (int i = 0; i < PortalCount; i++)
            {
                Transform cardTx = content.Find($"PortalCard_{i}");
                if (cardTx == null)
                {
                    failed++;
                    log.AppendLine($"- X PortalCard_{i} manquant");
                    continue;
                }

                StyleOnePortalCard(
                    cardTx.gameObject,
                    i,
                    spriteM,
                    spriteS,
                    talsSprite);
                conforme++;
                log.AppendLine($"- PortalCard_{i} restyle OK");
            }
        }

        /// <summary> Met a jour fractions hauteur/spacing + force layout (polish 6.b.1). </summary>
        private static void ApplyCellSizerFractions(
            Transform root,
            StringBuilder log,
            ref int conforme)
        {
            PortalCellSizer sizer = root.GetComponentInChildren<PortalCellSizer>(true);
            if (sizer == null)
            {
                log.AppendLine("- PortalCellSizer absent — skip fractions");
                return;
            }

            SerializedObject sizerSo = new SerializedObject(sizer);
            sizerSo.FindProperty("heightFraction").floatValue = HeightFraction;
            sizerSo.FindProperty("spacingFraction").floatValue = SpacingFraction;
            sizerSo.FindProperty("centerInViewport").boolValue = true;
            sizerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sizer);

            ScrollRect scroll = root.GetComponentInChildren<ScrollRect>(true);
            PortalSnapScroller snap = root.GetComponentInChildren<PortalSnapScroller>(true);
            if (scroll != null)
                sizer.Bind(scroll.viewport, scroll.content);
            if (snap != null)
            {
                snap.RecalculateMetrics();
                snap.SnapImmediate(snap.CurrentIndex);
            }

            conforme++;
            log.AppendLine(
                $"- CellSizer height={HeightFraction:0.##} spacing={SpacingFraction:0.##} (centre)");
        }

        private static void StyleOnePortalCard(
            GameObject cardGo,
            int index,
            Sprite spriteM,
            Sprite spriteS,
            Sprite talsSprite)
        {
            Undo.RegisterCompleteObjectUndo(cardGo, UndoLabel);

            // Purge enfants UI (garde LayoutElement / Image racine).
            for (int i = cardGo.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(cardGo.transform.GetChild(i).gameObject);

            Image rootImg = cardGo.GetComponent<Image>();
            if (rootImg == null)
                rootImg = Undo.AddComponent<Image>(cardGo);
            rootImg.sprite = spriteM;
            rootImg.type = Image.Type.Sliced;
            rootImg.color = Color.white;
            rootImg.raycastTarget = true;

            if (cardGo.GetComponent<RectMask2D>() == null)
                Undo.AddComponent<RectMask2D>(cardGo);

            LayoutElement le = cardGo.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(cardGo);
            le.flexibleHeight = 0f;

            // Placeholder tint (si pas d'artwork).
            Image placeholder = MakeImage(
                "PlaceholderTint", cardGo.transform, spriteM, PlaceholderTints[index % PlaceholderTints.Length]);
            StretchFull(placeholder.rectTransform);

            // Artwork plein cadre.
            Image artwork = MakeImage("Artwork", cardGo.transform, null, Color.white);
            StretchFull(artwork.rectTransform);
            artwork.preserveAspect = false;
            artwork.enabled = false;

            // Bandeau titre haut.
            GameObject titleBar = CreateUi("TitleBanner", cardGo.transform);
            RectTransform titleRt = titleBar.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, TitleBarH);
            titleRt.anchoredPosition = Vector2.zero;
            Image titleBg = titleBar.AddComponent<Image>();
            titleBg.sprite = spriteM;
            titleBg.type = Image.Type.Sliced;
            Color bar = UiTheme.SurfaceBar;
            bar.a = 0.82f;
            titleBg.color = bar;
            titleBg.raycastTarget = false;

            TextMeshProUGUI titleTmp = MakeTmp(
                "Title", titleBar.transform, UiTypography.Title * 0.55f, UiTheme.TextPrimary,
                TextAlignmentOptions.Left);
            RectTransform titleTextRt = titleTmp.rectTransform;
            titleTextRt.anchorMin = new Vector2(0f, 0f);
            titleTextRt.anchorMax = new Vector2(0.68f, 1f);
            titleTextRt.offsetMin = new Vector2(UiTheme.Space3, UiTheme.Space2);
            titleTextRt.offsetMax = new Vector2(-UiTheme.Space2, -UiTheme.Space2);

            TextMeshProUGUI timerTmp = MakeTmp(
                "Timer", titleBar.transform, UiTypography.Caption, UiTheme.Gold,
                TextAlignmentOptions.Right);
            RectTransform timerRt = timerTmp.rectTransform;
            timerRt.anchorMin = new Vector2(0.68f, 0f);
            timerRt.anchorMax = new Vector2(1f, 1f);
            timerRt.offsetMin = new Vector2(UiTheme.Space2, UiTheme.Space2);
            timerRt.offsetMax = new Vector2(-UiTheme.Space3, -UiTheme.Space2);

            // Rangée actions bas.
            GameObject actionRow = CreateUi("ActionRow", cardGo.transform);
            RectTransform actionRt = actionRow.GetComponent<RectTransform>();
            actionRt.anchorMin = new Vector2(0f, 0f);
            actionRt.anchorMax = new Vector2(1f, 0f);
            actionRt.pivot = new Vector2(0.5f, 0f);
            actionRt.sizeDelta = new Vector2(0f, ActionRowH);
            actionRt.anchoredPosition = new Vector2(0f, UiTheme.Space3);

            Image actionBg = actionRow.AddComponent<Image>();
            actionBg.sprite = spriteM;
            actionBg.type = Image.Type.Sliced;
            Color actionCol = UiTheme.Surface;
            actionCol.a = 0.88f;
            actionBg.color = actionCol;
            actionBg.raycastTarget = false;

            HorizontalLayoutGroup hlg = actionRow.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(
                (int)UiTheme.Space3, (int)UiTheme.Space3,
                (int)UiTheme.Space2, (int)UiTheme.Space2);
            hlg.spacing = UiTheme.Space5;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            BuildPullButton(
                actionRow.transform, "PullSingle", "INVOQUER x1", spriteS, talsSprite,
                out Button btnSingle, out TextMeshProUGUI singleLabel,
                out TextMeshProUGUI singleCost, out Image singleIcon, out CanvasGroup singleCg);

            BuildPullButton(
                actionRow.transform, "PullMulti", "INVOQUER x10", spriteS, talsSprite,
                out Button btnMulti, out TextMeshProUGUI multiLabel,
                out TextMeshProUGUI multiCost, out Image multiIcon, out CanvasGroup multiCg);

            // Bouton Taux — coin bas gauche de la carte.
            Button ratesBtn = BuildChipButton(
                "RatesButton", cardGo.transform, "Taux", spriteS,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(112f, 44f),
                new Vector2(UiTheme.Space3, ActionRowH + UiTheme.Space3));

            // Personnages (N) — coin bas droit — inactif 6.c.
            Button charsBtn = BuildChipButton(
                "CharactersButton", cardGo.transform, "Personnages (0)", spriteS,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(200f, 44f),
                new Vector2(-UiTheme.Space3, ActionRowH + UiTheme.Space3));
            charsBtn.interactable = false;
            TextMeshProUGUI charsLabel = charsBtn.GetComponentInChildren<TextMeshProUGUI>();

            PortalCardUI ui = cardGo.GetComponent<PortalCardUI>();
            if (ui == null)
                ui = Undo.AddComponent<PortalCardUI>(cardGo);

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("artworkImage").objectReferenceValue = artwork;
            so.FindProperty("placeholderTint").objectReferenceValue = placeholder;
            so.FindProperty("titleText").objectReferenceValue = titleTmp;
            so.FindProperty("timerText").objectReferenceValue = timerTmp;
            so.FindProperty("pullSingleButton").objectReferenceValue = btnSingle;
            so.FindProperty("pullSingleLabel").objectReferenceValue = singleLabel;
            so.FindProperty("pullSingleCostText").objectReferenceValue = singleCost;
            so.FindProperty("pullSingleTalsIcon").objectReferenceValue = singleIcon;
            so.FindProperty("pullSingleVisual").objectReferenceValue = singleCg;
            so.FindProperty("pullMultiButton").objectReferenceValue = btnMulti;
            so.FindProperty("pullMultiLabel").objectReferenceValue = multiLabel;
            so.FindProperty("pullMultiCostText").objectReferenceValue = multiCost;
            so.FindProperty("pullMultiTalsIcon").objectReferenceValue = multiIcon;
            so.FindProperty("pullMultiVisual").objectReferenceValue = multiCg;
            so.FindProperty("ratesButton").objectReferenceValue = ratesBtn;
            so.FindProperty("charactersButton").objectReferenceValue = charsBtn;
            so.FindProperty("charactersLabel").objectReferenceValue = charsLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
        }

        private static void BuildPullButton(
            Transform parent,
            string name,
            string label,
            Sprite spriteS,
            Sprite talsSprite,
            out Button button,
            out TextMeshProUGUI labelTmp,
            out TextMeshProUGUI costTmp,
            out Image talsIcon,
            out CanvasGroup visual)
        {
            GameObject go = CreateUi(name, parent);
            Image bg = go.AddComponent<Image>();
            bg.sprite = spriteS;
            bg.type = Image.Type.Sliced;
            bg.color = UiTheme.SurfaceBar;
            button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            visual = go.AddComponent<CanvasGroup>();

            VerticalLayoutGroup v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(8, 8, 8, 8);
            v.spacing = 4;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            labelTmp = MakeTmp("Label", go.transform, UiTypography.Caption, UiTheme.TextPrimary,
                TextAlignmentOptions.Center);
            labelTmp.text = label;
            LayoutElement lle = labelTmp.gameObject.AddComponent<LayoutElement>();
            lle.preferredHeight = 28f;

            GameObject costRow = CreateUi("CostRow", go.transform);
            LayoutElement cle = costRow.AddComponent<LayoutElement>();
            cle.preferredHeight = 32f;
            HorizontalLayoutGroup ch = costRow.AddComponent<HorizontalLayoutGroup>();
            ch.spacing = 6f;
            ch.childAlignment = TextAnchor.MiddleCenter;
            ch.childControlWidth = false;
            ch.childControlHeight = true;
            ch.childForceExpandWidth = false;
            ch.childForceExpandHeight = true;

            GameObject iconGo = CreateUi("TalsIcon", costRow.transform);
            LayoutElement ile = iconGo.AddComponent<LayoutElement>();
            ile.preferredWidth = 28f;
            ile.preferredHeight = 28f;
            talsIcon = iconGo.AddComponent<Image>();
            talsIcon.sprite = talsSprite;
            talsIcon.preserveAspect = true;
            talsIcon.raycastTarget = false;
            talsIcon.color = Color.white;

            costTmp = MakeTmp("Cost", costRow.transform, UiTypography.Caption, UiTheme.Gold,
                TextAlignmentOptions.Left);
            costTmp.text = "0";
            LayoutElement costLe = costTmp.gameObject.AddComponent<LayoutElement>();
            costLe.preferredWidth = 80f;
        }

        private static Button BuildChipButton(
            string name,
            Transform parent,
            string label,
            Sprite spriteS,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 size,
            Vector2 anchoredPos)
        {
            GameObject go = CreateUi(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            Image bg = go.AddComponent<Image>();
            bg.sprite = spriteS;
            bg.type = Image.Type.Sliced;
            Color c = UiTheme.SurfaceBar;
            c.a = 0.9f;
            bg.color = c;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;

            TextMeshProUGUI tmp = MakeTmp(
                "Label", go.transform, UiTypography.Caption * 0.9f, UiTheme.TextSecondary,
                TextAlignmentOptions.Center);
            StretchFull(tmp.rectTransform);
            tmp.text = label;
            tmp.raycastTarget = false;
            return btn;
        }

        // ═══════════════════════════════════════════
        // 6.b — Rates popup
        // ═══════════════════════════════════════════

        private static void BuildOrRefreshRatesPopup(
            Transform overlay,
            Sprite spriteM,
            Sprite iconBack,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            Transform existing = overlay.Find(RatesPopupName);
            GameObject root;
            if (existing != null)
            {
                root = existing.gameObject;
                Undo.RegisterCompleteObjectUndo(root, UndoLabel);
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Undo.DestroyObjectImmediate(root.transform.GetChild(i).gameObject);
                log.AppendLine("- BannerRatesPopup existant → rebuild");
            }
            else
            {
                root = new GameObject(RatesPopupName, typeof(RectTransform), typeof(CanvasGroup));
                Undo.RegisterCreatedObjectUndo(root, UndoLabel);
                root.transform.SetParent(overlay, false);
                log.AppendLine("- BannerRatesPopup cree");
            }

            RectTransform rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);
            CanvasGroup cg = root.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = Undo.AddComponent<CanvasGroup>(root);

            Image scrim = MakeImage("Scrim", root.transform, null, new Color(0f, 0f, 0f, 0.65f));
            StretchFull(scrim.rectTransform);
            scrim.raycastTarget = true;

            GameObject panel = CreateUi("Panel", root.transform);
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.08f, 0.28f);
            panelRt.anchorMax = new Vector2(0.92f, 0.72f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            Image panelImg = panel.AddComponent<Image>();
            panelImg.sprite = spriteM;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = UiTheme.Surface;
            panelImg.raycastTarget = true;

            // Header + Back
            GameObject header = CreateUi("Header", panel.transform);
            RectTransform headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 72f);
            headerRt.anchoredPosition = Vector2.zero;

            GameObject backGo = CreateUi("BackButton", header.transform);
            RectTransform backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0f, 0.5f);
            backRt.anchorMax = new Vector2(0f, 0.5f);
            backRt.pivot = new Vector2(0f, 0.5f);
            backRt.sizeDelta = new Vector2(64f, 64f);
            backRt.anchoredPosition = new Vector2(UiTheme.Space2, 0f);
            Image backImg = backGo.AddComponent<Image>();
            backImg.color = new Color(1f, 1f, 1f, 0.001f);
            backImg.raycastTarget = true;
            Button backBtn = backGo.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            backBtn.transition = Selectable.Transition.None;

            GameObject backIcon = CreateUi("Icon", backGo.transform);
            RectTransform biRt = backIcon.GetComponent<RectTransform>();
            StretchFull(biRt);
            biRt.offsetMin = new Vector2(12f, 12f);
            biRt.offsetMax = new Vector2(-12f, -12f);
            Image bi = backIcon.AddComponent<Image>();
            bi.sprite = iconBack;
            bi.color = UiTheme.TextPrimary;
            bi.preserveAspect = true;
            bi.raycastTarget = false;

            TextMeshProUGUI title = MakeTmp(
                "Title", header.transform, UiTypography.Title * 0.5f, UiTheme.TextPrimary,
                TextAlignmentOptions.Center);
            StretchFull(title.rectTransform);
            title.text = "Taux d'apparition";
            title.raycastTarget = false;

            // Liste
            GameObject list = CreateUi("List", panel.transform);
            RectTransform listRt = list.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0f, 0f);
            listRt.anchorMax = new Vector2(1f, 1f);
            listRt.offsetMin = new Vector2(UiTheme.Space4, UiTheme.Space4);
            listRt.offsetMax = new Vector2(-UiTheme.Space4, -80f);
            VerticalLayoutGroup vlg = list.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = UiTheme.Space3;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            BuildRateRow(list.transform, "RowSR", out TextMeshProUGUI srL, out TextMeshProUGUI srR);
            BuildRateRow(list.transform, "RowSSR", out TextMeshProUGUI ssrL, out TextMeshProUGUI ssrR);
            BuildRateRow(list.transform, "RowLR", out TextMeshProUGUI lrL, out TextMeshProUGUI lrR);
            GameObject featured = BuildRateRow(
                list.transform, "RowFeatured", out TextMeshProUGUI fL, out TextMeshProUGUI fR);

            BannerRatesPopup popup = root.GetComponent<BannerRatesPopup>();
            if (popup == null)
                popup = Undo.AddComponent<BannerRatesPopup>(root);

            SerializedObject so = new SerializedObject(popup);
            so.FindProperty("canvasGroup").objectReferenceValue = cg;
            so.FindProperty("scrim").objectReferenceValue = scrim;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("srLabel").objectReferenceValue = srL;
            so.FindProperty("srRate").objectReferenceValue = srR;
            so.FindProperty("ssrLabel").objectReferenceValue = ssrL;
            so.FindProperty("ssrRate").objectReferenceValue = ssrR;
            so.FindProperty("lrLabel").objectReferenceValue = lrL;
            so.FindProperty("lrRate").objectReferenceValue = lrR;
            so.FindProperty("featuredRow").objectReferenceValue = featured;
            so.FindProperty("featuredLabel").objectReferenceValue = fL;
            so.FindProperty("featuredRate").objectReferenceValue = fR;
            so.ApplyModifiedPropertiesWithoutUndo();

            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            EditorUtility.SetDirty(popup);
            conforme++;
            log.AppendLine("- BannerRatesPopup wire OK");
        }

        private static GameObject BuildRateRow(
            Transform parent,
            string name,
            out TextMeshProUGUI label,
            out TextMeshProUGUI rate)
        {
            GameObject row = CreateUi(name, parent);
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 48f;
            HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            label = MakeTmp("Label", row.transform, UiTypography.Body * 0.7f, UiTheme.TextPrimary,
                TextAlignmentOptions.Left);
            rate = MakeTmp("Rate", row.transform, UiTypography.Body * 0.7f, UiTheme.TextPrimary,
                TextAlignmentOptions.Right);
            return row;
        }

        // ═══════════════════════════════════════════
        // 6.c — Showcase
        // ═══════════════════════════════════════════

        private static void BuildOrRefreshShowcase(
            Transform overlay,
            Transform portalRoot,
            Sprite spriteM,
            Sprite spriteS,
            Sprite iconBack,
            Sprite iconUp,
            Sprite iconDown,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            Transform existing = overlay.Find(ShowcaseName);
            GameObject root;
            if (existing != null)
            {
                root = existing.gameObject;
                Undo.RegisterCompleteObjectUndo(root, UndoLabel);
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Undo.DestroyObjectImmediate(root.transform.GetChild(i).gameObject);
                log.AppendLine("- BannerShowcasePanel existant → rebuild");
            }
            else
            {
                root = new GameObject(
                    ShowcaseName, typeof(RectTransform), typeof(CanvasGroup));
                Undo.RegisterCreatedObjectUndo(root, UndoLabel);
                root.transform.SetParent(overlay, false);
                log.AppendLine("- BannerShowcasePanel cree");
            }

            RectTransform rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);
            CanvasGroup cg = root.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = Undo.AddComponent<CanvasGroup>(root);

            Image scrim = MakeImage("Scrim", root.transform, null, new Color(0f, 0f, 0f, 0.72f));
            StretchFull(scrim.rectTransform);
            scrim.raycastTarget = true;

            GameObject panel = CreateUi("Panel", root.transform);
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            StretchFull(panelRt);
            panelRt.offsetMin = new Vector2(UiTheme.Space3, UiTheme.Space3);
            panelRt.offsetMax = new Vector2(-UiTheme.Space3, -UiTheme.Space3);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.sprite = spriteM;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = UiTheme.Surface;
            panelImg.raycastTarget = true;
            // Pile verticale : zero chevauchement, air garanti entre featured et liste.
            VerticalLayoutGroup panelVlg = panel.AddComponent<VerticalLayoutGroup>();
            panelVlg.padding = new RectOffset(
                Mathf.RoundToInt(UiTheme.Space3),
                Mathf.RoundToInt(UiTheme.Space3),
                Mathf.RoundToInt(UiTheme.Space3),
                Mathf.RoundToInt(UiTheme.Space3));
            panelVlg.spacing = UiTheme.Space5;
            panelVlg.childAlignment = TextAnchor.UpperCenter;
            panelVlg.childControlWidth = true;
            panelVlg.childControlHeight = true;
            panelVlg.childForceExpandWidth = true;
            panelVlg.childForceExpandHeight = false;

            // Header + Back
            GameObject header = CreateUi("Header", panel.transform);
            StretchFull(header.GetComponent<RectTransform>());
            LayoutElement headerLe = header.AddComponent<LayoutElement>();
            headerLe.preferredHeight = ShowcaseHeaderH;
            headerLe.minHeight = ShowcaseHeaderH;
            headerLe.flexibleHeight = 0f;
            Button backBtn = BuildShowcaseBack(header.transform, iconBack);

            TextMeshProUGUI title = MakeTmp(
                "Title", header.transform, UiTypography.Title * 0.5f, UiTheme.TextPrimary,
                TextAlignmentOptions.Center);
            StretchFull(title.rectTransform);
            title.text = "Personnages";
            title.raycastTarget = false;

            // Titre section vedettes
            TextMeshProUGUI featuredTitle = MakeTmp(
                "FeaturedSectionTitle", panel.transform, UiTypography.Caption, UiTheme.Gold,
                TextAlignmentOptions.Left);
            StretchFull(featuredTitle.rectTransform);
            LayoutElement ftLe = featuredTitle.gameObject.AddComponent<LayoutElement>();
            ftLe.preferredHeight = ShowcaseSectionTitleH;
            ftLe.minHeight = ShowcaseSectionTitleH;
            ftLe.flexibleHeight = 0f;
            featuredTitle.margin = new Vector4(UiTheme.Space2, 0f, UiTheme.Space2, 0f);
            featuredTitle.text = "SSR du portail";
            featuredTitle.raycastTarget = false;

            // Featured zone — flexible (poids gere par ShowcaseLayoutFitter).
            GameObject featuredZone = CreateUi("FeaturedZone", panel.transform);
            RectTransform fzRt = featuredZone.GetComponent<RectTransform>();
            StretchFull(fzRt);
            LayoutElement fzLe = featuredZone.AddComponent<LayoutElement>();
            fzLe.flexibleHeight = 1.35f;
            fzLe.minHeight = 220f;
            fzLe.preferredHeight = -1f;

            VerticalLayoutGroup fzVlg = featuredZone.AddComponent<VerticalLayoutGroup>();
            fzVlg.padding = new RectOffset(0, 0, 0, 0);
            fzVlg.spacing = UiTheme.Space2;
            fzVlg.childAlignment = TextAnchor.UpperCenter;
            fzVlg.childControlWidth = true;
            fzVlg.childControlHeight = true;
            fzVlg.childForceExpandWidth = true;
            fzVlg.childForceExpandHeight = false;

            GameObject featuredScrollGo = CreateUi("FeaturedScroll", featuredZone.transform);
            RectTransform fsRt = featuredScrollGo.GetComponent<RectTransform>();
            StretchFull(fsRt);
            LayoutElement fsLe = featuredScrollGo.AddComponent<LayoutElement>();
            fsLe.flexibleHeight = 1f;
            fsLe.minHeight = 180f;

            GameObject vpGo = CreateUi("Viewport", featuredScrollGo.transform);
            RectTransform vpRt = vpGo.GetComponent<RectTransform>();
            StretchFull(vpRt);
            Image vpImg = vpGo.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);
            vpImg.raycastTarget = true;
            vpGo.AddComponent<RectMask2D>();

            GameObject contentGo = CreateUi("Content", vpGo.transform);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            HorizontalLayoutGroup hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 0f;

            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect scroll = featuredScrollGo.AddComponent<ScrollRect>();
            scroll.viewport = vpRt;
            scroll.content = contentRt;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            PortalSnapScroller snap = featuredScrollGo.AddComponent<PortalSnapScroller>();
            SerializedObject snapSo = new SerializedObject(snap);
            snapSo.FindProperty("axis").enumValueIndex = (int)PortalSnapScroller.SnapAxis.Horizontal;
            snapSo.ApplyModifiedPropertiesWithoutUndo();

            // Fleches overlay (ignore layout) — centrees sur le scroll.
            Button btnPrev = BuildArrow("ArrowPrev", featuredScrollGo.transform, iconUp, true);
            RectTransform prevRt = btnPrev.GetComponent<RectTransform>();
            prevRt.anchorMin = new Vector2(0f, 0.5f);
            prevRt.anchorMax = new Vector2(0f, 0.5f);
            prevRt.pivot = new Vector2(0f, 0.5f);
            prevRt.anchoredPosition = Vector2.zero;
            btnPrev.GetComponent<Image>().sprite = iconBack;
            LayoutElement prevLe = btnPrev.gameObject.AddComponent<LayoutElement>();
            prevLe.ignoreLayout = true;

            Button btnNext = BuildArrow("ArrowNext", featuredScrollGo.transform, iconDown, false);
            RectTransform nextRt = btnNext.GetComponent<RectTransform>();
            nextRt.anchorMin = new Vector2(1f, 0.5f);
            nextRt.anchorMax = new Vector2(1f, 0.5f);
            nextRt.pivot = new Vector2(1f, 0.5f);
            nextRt.anchoredPosition = Vector2.zero;
            nextRt.localScale = new Vector3(-1f, 1f, 1f);
            btnNext.GetComponent<Image>().sprite = iconBack;
            LayoutElement nextLe = btnNext.gameObject.AddComponent<LayoutElement>();
            nextLe.ignoreLayout = true;

            // Dots — rangee collapsible (hauteur 0 si 1 seule vedette).
            GameObject dotsGo = CreateUi("DotIndicator", featuredZone.transform);
            RectTransform dotsRt = dotsGo.GetComponent<RectTransform>();
            StretchFull(dotsRt);
            LayoutElement dotsLe = dotsGo.AddComponent<LayoutElement>();
            dotsLe.preferredHeight = ShowcaseDotsRowH;
            dotsLe.minHeight = ShowcaseDotsRowH;
            dotsLe.flexibleHeight = 0f;
            HorizontalLayoutGroup dH = dotsGo.AddComponent<HorizontalLayoutGroup>();
            dH.spacing = DotGap;
            dH.childAlignment = TextAnchor.MiddleCenter;
            dH.childControlWidth = true;
            dH.childControlHeight = true;
            dH.childForceExpandWidth = false;
            dH.childForceExpandHeight = true;
            for (int i = 0; i < BannerRoster.MaxFeaturedPages; i++)
            {
                GameObject dot = CreateUi($"Dot_{i}", dotsGo.transform);
                LayoutElement dle = dot.AddComponent<LayoutElement>();
                dle.preferredWidth = DotSize;
                dle.preferredHeight = DotSize;
                Image dimg = dot.AddComponent<Image>();
                dimg.sprite = spriteS;
                dimg.type = Image.Type.Sliced;
                dimg.color = new Color(1f, 1f, 1f, 0.28f);
                dimg.raycastTarget = false;
                dot.SetActive(false);
            }

            PortalSnapChrome chrome = featuredZone.AddComponent<PortalSnapChrome>();
            SerializedObject chromeSo = new SerializedObject(chrome);
            chromeSo.FindProperty("scroller").objectReferenceValue = snap;
            chromeSo.FindProperty("arrowUp").objectReferenceValue = btnPrev;
            chromeSo.FindProperty("arrowDown").objectReferenceValue = btnNext;
            chromeSo.FindProperty("dotContainer").objectReferenceValue = dotsGo.transform;
            chromeSo.FindProperty("logSnaps").boolValue = false;
            chromeSo.ApplyModifiedPropertiesWithoutUndo();

            // Titre liste — sibling apres FeaturedZone => jamais de chevauchement.
            TextMeshProUGUI poolTitle = MakeTmp(
                "PoolSectionTitle", panel.transform, UiTypography.Caption, UiTheme.Gold,
                TextAlignmentOptions.Left);
            StretchFull(poolTitle.rectTransform);
            LayoutElement ptLe = poolTitle.gameObject.AddComponent<LayoutElement>();
            ptLe.preferredHeight = ShowcaseSectionTitleH;
            ptLe.minHeight = ShowcaseSectionTitleH;
            ptLe.flexibleHeight = 0f;
            poolTitle.margin = new Vector4(UiTheme.Space2, 0f, UiTheme.Space2, 0f);
            poolTitle.text = "Liste des personnages obtenables";
            poolTitle.raycastTarget = false;

            // Pool zone — flexible (reste de l'ecran).
            GameObject poolZone = CreateUi("PoolZone", panel.transform);
            RectTransform pzRt = poolZone.GetComponent<RectTransform>();
            StretchFull(pzRt);
            LayoutElement pzLe = poolZone.AddComponent<LayoutElement>();
            pzLe.flexibleHeight = 1f;
            pzLe.minHeight = 140f;
            pzLe.preferredHeight = -1f;

            GameObject poolScrollGo = CreateUi("PoolScroll", poolZone.transform);
            StretchFull(poolScrollGo.GetComponent<RectTransform>());
            GameObject poolVp = CreateUi("Viewport", poolScrollGo.transform);
            RectTransform poolVpRt = poolVp.GetComponent<RectTransform>();
            StretchFull(poolVpRt);
            Image poolVpImg = poolVp.AddComponent<Image>();
            poolVpImg.color = new Color(1f, 1f, 1f, 0.01f);
            poolVpImg.raycastTarget = true;
            poolVp.AddComponent<RectMask2D>();

            GameObject poolContent = CreateUi("Content", poolVp.transform);
            RectTransform poolCt = poolContent.GetComponent<RectTransform>();
            poolCt.anchorMin = new Vector2(0f, 1f);
            poolCt.anchorMax = new Vector2(1f, 1f);
            poolCt.pivot = new Vector2(0.5f, 1f);
            poolCt.anchoredPosition = Vector2.zero;
            poolCt.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup pV = poolContent.AddComponent<VerticalLayoutGroup>();
            pV.spacing = UiTheme.Space2;
            pV.childControlWidth = true;
            pV.childControlHeight = true;
            pV.childForceExpandWidth = true;
            pV.childForceExpandHeight = false;
            ContentSizeFitter pCsf = poolContent.AddComponent<ContentSizeFitter>();
            pCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect poolScroll = poolScrollGo.AddComponent<ScrollRect>();
            poolScroll.viewport = poolVpRt;
            poolScroll.content = poolCt;
            poolScroll.horizontal = false;
            poolScroll.vertical = true;
            poolScroll.movementType = ScrollRect.MovementType.Clamped;

            TextMeshProUGUI emptyLbl = MakeTmp(
                "EmptyLabel", poolContent.transform, UiTypography.Caption, UiTheme.TextMuted,
                TextAlignmentOptions.Center);
            emptyLbl.text = "Pool vide";
            LayoutElement emptyLe = emptyLbl.gameObject.AddComponent<LayoutElement>();
            emptyLe.preferredHeight = 40f;

            // Template featured page (inactive prefab scene)
            GameObject template = BuildFeaturedPageTemplate(root.transform, spriteM, spriteS);
            template.SetActive(false);
            ShowcaseFeaturedPage pageComp = template.GetComponent<ShowcaseFeaturedPage>();

            BannerShowcasePanel panelComp = root.GetComponent<BannerShowcasePanel>();
            if (panelComp == null)
                panelComp = Undo.AddComponent<BannerShowcasePanel>(root);

            ShowcaseLayoutFitter fitter = root.GetComponent<ShowcaseLayoutFitter>();
            if (fitter == null)
                fitter = Undo.AddComponent<ShowcaseLayoutFitter>(root);

            ScrollRect portalScroll = portalRoot != null
                ? portalRoot.GetComponentInChildren<ScrollRect>(true)
                : null;

            SerializedObject fitSo = new SerializedObject(fitter);
            fitSo.FindProperty("panel").objectReferenceValue = panelRt;
            fitSo.FindProperty("featuredZoneLe").objectReferenceValue = fzLe;
            fitSo.FindProperty("poolZoneLe").objectReferenceValue = pzLe;
            fitSo.FindProperty("dotsRowLe").objectReferenceValue = dotsLe;
            fitSo.FindProperty("featuredViewport").objectReferenceValue = vpRt;
            fitSo.FindProperty("featuredContent").objectReferenceValue = contentRt;
            fitSo.FindProperty("featuredSnap").objectReferenceValue = snap;
            fitSo.FindProperty("arrowPrev").objectReferenceValue = btnPrev;
            fitSo.FindProperty("arrowNext").objectReferenceValue = btnNext;
            fitSo.FindProperty("featuredMinHeight").floatValue = 220f;
            fitSo.FindProperty("poolMinHeight").floatValue = 140f;
            fitSo.FindProperty("dotsRowHeight").floatValue = ShowcaseDotsRowH;
            fitSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject so = new SerializedObject(panelComp);
            so.FindProperty("canvasGroup").objectReferenceValue = cg;
            so.FindProperty("scrim").objectReferenceValue = scrim;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("featuredZone").objectReferenceValue = fzRt;
            so.FindProperty("featuredScroll").objectReferenceValue = scroll;
            so.FindProperty("featuredSnap").objectReferenceValue = snap;
            so.FindProperty("featuredChrome").objectReferenceValue = chrome;
            so.FindProperty("featuredContent").objectReferenceValue = contentRt;
            so.FindProperty("featuredPagePrefab").objectReferenceValue = pageComp;
            so.FindProperty("poolContent").objectReferenceValue = poolCt;
            so.FindProperty("poolEmptyLabel").objectReferenceValue = emptyLbl;
            so.FindProperty("featuredSectionTitle").objectReferenceValue = featuredTitle;
            so.FindProperty("poolSectionTitle").objectReferenceValue = poolTitle;
            so.FindProperty("layoutFitter").objectReferenceValue = fitter;
            so.FindProperty("portalScrollToBlock").objectReferenceValue = portalScroll;
            so.ApplyModifiedPropertiesWithoutUndo();
            panelComp.SetRowSprite(spriteS);

            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            EditorUtility.SetDirty(panelComp);
            EditorUtility.SetDirty(fitter);
            conforme++;
            log.AppendLine("- BannerShowcasePanel VLG + ShowcaseLayoutFitter OK");
        }

        private static Button BuildShowcaseBack(Transform header, Sprite iconBack)
        {
            GameObject backGo = CreateUi("BackButton", header);
            RectTransform backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0f, 0.5f);
            backRt.anchorMax = new Vector2(0f, 0.5f);
            backRt.pivot = new Vector2(0f, 0.5f);
            backRt.sizeDelta = new Vector2(64f, 64f);
            backRt.anchoredPosition = new Vector2(UiTheme.Space2, 0f);
            Image backImg = backGo.AddComponent<Image>();
            backImg.color = new Color(1f, 1f, 1f, 0.001f);
            backImg.raycastTarget = true;
            Button backBtn = backGo.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            backBtn.transition = Selectable.Transition.None;

            GameObject backIcon = CreateUi("Icon", backGo.transform);
            RectTransform biRt = backIcon.GetComponent<RectTransform>();
            StretchFull(biRt);
            biRt.offsetMin = new Vector2(12f, 12f);
            biRt.offsetMax = new Vector2(-12f, -12f);
            Image bi = backIcon.AddComponent<Image>();
            bi.sprite = iconBack;
            bi.color = UiTheme.TextPrimary;
            bi.preserveAspect = true;
            bi.raycastTarget = false;
            return backBtn;
        }

        private static GameObject BuildFeaturedPageTemplate(
            Transform parent,
            Sprite spriteM,
            Sprite spriteS)
        {
            GameObject page = CreateUi("FeaturedPageTemplate", parent);
            LayoutElement pageLe = page.AddComponent<LayoutElement>();
            pageLe.preferredWidth = 900f;
            pageLe.flexibleWidth = 0f;

            // Carte cliquable : fond Surface + lisere amber leger.
            Image pageBg = page.AddComponent<Image>();
            pageBg.sprite = spriteM;
            pageBg.type = Image.Type.Sliced;
            Color cardCol = UiTheme.Surface;
            cardCol.a = 0.96f;
            pageBg.color = cardCol;
            pageBg.raycastTarget = true;

            GameObject borderGo = CreateUi("CardBorder", page.transform);
            RectTransform borderRt = borderGo.GetComponent<RectTransform>();
            StretchFull(borderRt);
            borderRt.offsetMin = new Vector2(2f, 2f);
            borderRt.offsetMax = new Vector2(-2f, -2f);
            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.sprite = spriteM;
            borderImg.type = Image.Type.Sliced;
            Color borderCol = UiTheme.AccentAmber;
            borderCol.a = 0.55f;
            borderImg.color = borderCol;
            borderImg.raycastTarget = false;

            GameObject inner = CreateUi("CardInner", page.transform);
            RectTransform innerRt = inner.GetComponent<RectTransform>();
            StretchFull(innerRt);
            innerRt.offsetMin = new Vector2(4f, 4f);
            innerRt.offsetMax = new Vector2(-4f, -4f);
            Image innerImg = inner.AddComponent<Image>();
            innerImg.sprite = spriteM;
            innerImg.type = Image.Type.Sliced;
            innerImg.color = cardCol;
            innerImg.raycastTarget = false;

            // Crop = grande part de la carte (artwork valorise, zero bande noire).
            GameObject crop = CreateUi("CropWindow", inner.transform);
            RectTransform cropRt = crop.GetComponent<RectTransform>();
            cropRt.anchorMin = new Vector2(0f, 0.38f);
            cropRt.anchorMax = new Vector2(1f, 1f);
            cropRt.offsetMin = Vector2.zero;
            cropRt.offsetMax = Vector2.zero;
            Image cropImg = crop.AddComponent<Image>();
            cropImg.color = cardCol;
            cropImg.raycastTarget = true;
            crop.AddComponent<RectMask2D>();

            GameObject artRoot = CreateUi("ArtworkRoot", crop.transform);
            RectTransform artRt = artRoot.GetComponent<RectTransform>();
            StretchFull(artRt);

            GameObject rawGo = CreateUi("RawImage", artRoot.transform);
            StretchFull(rawGo.GetComponent<RectTransform>());
            RawImage raw = rawGo.AddComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = Color.white;

            CharacterArtworkView view = artRoot.AddComponent<CharacterArtworkView>();
            SerializedObject viewSo = new SerializedObject(view);
            viewSo.FindProperty("rawImage").objectReferenceValue = raw;
            var modeProp = viewSo.FindProperty("mode");
            if (modeProp != null)
                modeProp.enumValueIndex = 0; // Cover
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            TextMeshProUGUI nameT = MakeTmp(
                "Name", inner.transform, UiTypography.Body * 0.65f, UiTheme.TextPrimary,
                TextAlignmentOptions.Left);
            PlaceText(nameT.rectTransform, 0.06f, 0.30f, 0.62f, 0.38f);

            TextMeshProUGUI rarityT = MakeTmp(
                "RarityBadge", inner.transform, UiTypography.Caption, UiTheme.TextPrimary,
                TextAlignmentOptions.Right);
            PlaceText(rarityT.rectTransform, 0.62f, 0.30f, 0.94f, 0.38f);

            // Chips spé (switchables runtime).
            GameObject chipsGo = CreateUi("SpecChips", inner.transform);
            RectTransform chipsRt = chipsGo.GetComponent<RectTransform>();
            PlaceText(chipsRt, 0.05f, 0.235f, 0.95f, 0.30f);
            HorizontalLayoutGroup chipsH = chipsGo.AddComponent<HorizontalLayoutGroup>();
            chipsH.spacing = 8f;
            chipsH.childAlignment = TextAnchor.MiddleLeft;
            chipsH.childControlWidth = true;
            chipsH.childControlHeight = true;
            chipsH.childForceExpandWidth = false;
            chipsH.childForceExpandHeight = true;
            chipsH.padding = new RectOffset(0, 0, 0, 0);

            GameObject stats = CreateUi("StatsRow", inner.transform);
            RectTransform statsRt = stats.GetComponent<RectTransform>();
            PlaceText(statsRt, 0.04f, 0.145f, 0.96f, 0.235f);
            HorizontalLayoutGroup sh = stats.AddComponent<HorizontalLayoutGroup>();
            sh.spacing = UiTheme.Space2;
            sh.childForceExpandWidth = true;
            sh.childForceExpandHeight = true;
            sh.childControlWidth = true;
            sh.childControlHeight = true;

            BuildStatCell(stats.transform, "HP", out TextMeshProUGUI hpV, out TextMeshProUGUI hpL);
            BuildStatCell(stats.transform, "ATK", out TextMeshProUGUI atkV, out TextMeshProUGUI atkL);
            BuildStatCell(stats.transform, "DEF", out TextMeshProUGUI defV, out TextMeshProUGUI defL);
            BuildStatCell(stats.transform, "VIT", out TextMeshProUGUI spdV, out TextMeshProUGUI spdL);

            TextMeshProUGUI passName = MakeTmp(
                "PassiveName", inner.transform, UiTypography.Caption, UiTheme.Gold,
                TextAlignmentOptions.Left);
            PlaceText(passName.rectTransform, 0.06f, 0.095f, 0.94f, 0.145f);

            TextMeshProUGUI passDesc = MakeTmp(
                "PassiveDesc", inner.transform, UiTypography.Caption * 0.85f, UiTheme.TextMuted,
                TextAlignmentOptions.TopLeft);
            PlaceText(passDesc.rectTransform, 0.06f, 0.04f, 0.94f, 0.095f);
            passDesc.enableWordWrapping = true;
            passDesc.overflowMode = TextOverflowModes.Ellipsis;
            passDesc.maxVisibleLines = 2;

            TextMeshProUGUI tapHint = MakeTmp(
                "TapHint", inner.transform, UiTypography.Caption * 0.9f, UiTheme.Gold,
                TextAlignmentOptions.Center);
            PlaceText(tapHint.rectTransform, 0.08f, 0.004f, 0.92f, 0.038f);
            tapHint.text = "Appuyer pour details";

            ShowcaseFeaturedPage feat = page.AddComponent<ShowcaseFeaturedPage>();
            SerializedObject so = new SerializedObject(feat);
            so.FindProperty("cropWindow").objectReferenceValue = cropRt;
            so.FindProperty("artworkRoot").objectReferenceValue = artRt;
            so.FindProperty("artworkView").objectReferenceValue = view;
            so.FindProperty("hpValue").objectReferenceValue = hpV;
            so.FindProperty("hpLabel").objectReferenceValue = hpL;
            so.FindProperty("atkValue").objectReferenceValue = atkV;
            so.FindProperty("atkLabel").objectReferenceValue = atkL;
            so.FindProperty("defValue").objectReferenceValue = defV;
            so.FindProperty("defLabel").objectReferenceValue = defL;
            so.FindProperty("speedValue").objectReferenceValue = spdV;
            so.FindProperty("speedLabel").objectReferenceValue = spdL;
            so.FindProperty("nameText").objectReferenceValue = nameT;
            so.FindProperty("rarityBadge").objectReferenceValue = rarityT;
            so.FindProperty("specChipsRow").objectReferenceValue = chipsRt;
            so.FindProperty("specChipSprite").objectReferenceValue = spriteS;
            so.FindProperty("passiveNameText").objectReferenceValue = passName;
            so.FindProperty("passiveDescText").objectReferenceValue = passDesc;
            so.FindProperty("tapHintText").objectReferenceValue = tapHint;
            so.FindProperty("cardFrame").objectReferenceValue = pageBg;
            so.FindProperty("parallaxFactor").floatValue = 0.15f;
            so.FindProperty("bustAnchor").vector2Value = new Vector2(0.5f, 0.25f);
            so.FindProperty("bustOffsetPx").vector2Value = Vector2.zero;
            so.FindProperty("artworkScale").floatValue = 1.75f;
            so.ApplyModifiedPropertiesWithoutUndo();

            return page;
        }

        /// <summary> Titre au-dessus du carrousel de portails (page Invocation). </summary>
        private static void EnsurePortalsAvailableTitle(
            Transform portalRoot,
            StringBuilder log,
            ref int conforme)
        {
            // Titre dans la zone grise morte au-dessus de la 1re carte (overlay).
            const float titleH = 56f;
            Transform existing = portalRoot.Find("PortalsTitle");
            TextMeshProUGUI title;
            RectTransform rt;
            if (existing == null)
            {
                GameObject go = CreateUi("PortalsTitle", portalRoot);
                rt = go.GetComponent<RectTransform>();
                title = go.AddComponent<TextMeshProUGUI>();
                log.AppendLine("- PortalsTitle cree");
            }
            else
            {
                rt = existing as RectTransform;
                title = existing.GetComponent<TextMeshProUGUI>();
                if (title == null)
                    title = Undo.AddComponent<TextMeshProUGUI>(existing.gameObject);
                log.AppendLine("- PortalsTitle deja present → refresh");
            }

            Undo.RecordObject(rt, UndoLabel);
            // Centre horizontal, dans la bande haute (padding snap ~ centre).
            rt.anchorMin = new Vector2(0.1f, 1f);
            rt.anchorMax = new Vector2(0.9f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, titleH);
            rt.anchoredPosition = new Vector2(0f, -72f);

            title.text = "Portails disponibles";
            title.fontSize = UiTypography.Title;
            title.fontStyle = FontStyles.Bold;
            title.color = UiTheme.TextPrimary;
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = false;

            // Scroll full-bleed : le titre flotte dans le padding gris, pas une bande reservee.
            Transform scroll = portalRoot.Find("PortalScroll");
            if (scroll != null)
            {
                RectTransform srt = scroll as RectTransform;
                Undo.RecordObject(srt, UndoLabel);
                srt.offsetMin = new Vector2(UiTheme.Space3, 0f);
                srt.offsetMax = new Vector2(-UiTheme.Space3, 0f);
                EditorUtility.SetDirty(srt);
            }

            Transform arrowUp = portalRoot.Find("ArrowUp");
            if (arrowUp != null)
            {
                RectTransform art = arrowUp as RectTransform;
                Undo.RecordObject(art, UndoLabel);
                art.anchoredPosition = new Vector2(0f, -UiTheme.Space3);
                EditorUtility.SetDirty(art);
            }

            // Titre au-dessus des fleches/dots dans la hierarchie visuelle.
            rt.SetAsLastSibling();
            EditorUtility.SetDirty(title);
            conforme++;
            log.AppendLine("- Titre 'Portails disponibles' (Title, zone grise) OK");
        }

        private static void BuildStatCell(
            Transform parent,
            string key,
            out TextMeshProUGUI value,
            out TextMeshProUGUI label)
        {
            GameObject cell = CreateUi("Stat_" + key, parent);
            VerticalLayoutGroup v = cell.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.spacing = 2f;
            value = MakeTmp("Value", cell.transform, UiTypography.Caption, UiTheme.TextPrimary,
                TextAlignmentOptions.Center);
            label = MakeTmp("Label", cell.transform, UiTypography.Caption * 0.85f, UiTheme.TextMuted,
                TextAlignmentOptions.Center);
            label.text = key;
        }

        private static void PlaceText(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void WireInvocationPage(
            Transform page,
            Scene scene,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            InvocationPageUI pageUi = page.GetComponent<InvocationPageUI>();
            if (pageUi == null)
            {
                failed++;
                log.AppendLine("- X InvocationPageUI absent");
                return;
            }

            Transform root = page.Find(RootName);
            Transform content = root != null ? FindDeepUnder(root, "Content") : null;
            var cards = new PortalCardUI[PortalCount];
            int found = 0;
            if (content != null)
            {
                for (int i = 0; i < PortalCount; i++)
                {
                    Transform c = content.Find($"PortalCard_{i}");
                    if (c == null)
                        continue;
                    cards[i] = c.GetComponent<PortalCardUI>();
                    if (cards[i] != null)
                        found++;
                }
            }

            Transform overlay = FindDeep(scene, "OverlayLayer");
            BannerRatesPopup rates = null;
            BannerShowcasePanel showcase = null;
            if (overlay != null)
            {
                Transform p = overlay.Find(RatesPopupName);
                if (p != null)
                    rates = p.GetComponent<BannerRatesPopup>();
                Transform s = overlay.Find(ShowcaseName);
                if (s != null)
                    showcase = s.GetComponent<BannerShowcasePanel>();
            }

            CharacterDetailPopup detail = null;
            Transform detailTx = FindDeep(scene, "CharacterDetailPopup");
            if (detailTx != null)
                detail = detailTx.GetComponent<CharacterDetailPopup>();

            if (showcase != null && detail != null)
                showcase.BindDetailPopup(detail);

            SerializedObject so = new SerializedObject(pageUi);
            SerializedProperty cardsProp = so.FindProperty("portalCards");
            cardsProp.arraySize = PortalCount;
            for (int i = 0; i < PortalCount; i++)
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            so.FindProperty("bannerRatesPopup").objectReferenceValue = rates;
            so.FindProperty("showcasePanel").objectReferenceValue = showcase;
            so.FindProperty("detailPopup").objectReferenceValue = detail;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pageUi);

            bool ok = found >= PortalCount && rates != null && showcase != null && detail != null;
            if (!ok)
            {
                failed++;
                log.AppendLine(
                    $"- X Wire incomplet (cards={found}/{PortalCount}, rates={(rates != null)}, "
                    + $"showcase={(showcase != null)}, detail={(detail != null)})");
            }
            else
            {
                conforme++;
                log.AppendLine("- InvocationPageUI wires (cards/rates/showcase/detail) OK");
            }
        }

        // ═══════════════════════════════════════════
        // Structure 6.a (si absente)
        // ═══════════════════════════════════════════

        private static void ProcessSimpleBackground(
            Transform page,
            Scene scene,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            Transform art = FindChildNamed(page, "InvocationBackground");
            if (art == null)
                art = FindDeepUnder(page, "InvocationBackground");

            if (art == null)
            {
                conforme++;
                log.AppendLine("- InvocationBackground absent — OK");
            }
            else if (!art.gameObject.activeSelf)
            {
                conforme++;
                log.AppendLine("- InvocationBackground deja masque");
            }
            else if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Masquer InvocationBackground — A FAIRE");
            }
            else
            {
                Undo.RecordObject(art.gameObject, UndoLabel);
                art.gameObject.SetActive(false);
                conforme++;
                log.AppendLine("- InvocationBackground masque");
            }

            Image pageImg = page.GetComponent<Image>();
            if (pageImg == null)
            {
                conforme++;
                log.AppendLine("- Pas d'Image page — OK");
            }
            else if (pageImg.color.a <= 0.001f)
            {
                conforme++;
                log.AppendLine("- Image PageInvocation deja transparente");
            }
            else if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Image PageInvocation alpha → 0 — A FAIRE");
            }
            else
            {
                Undo.RecordObject(pageImg, UndoLabel);
                Color c = pageImg.color;
                c.a = 0f;
                pageImg.color = c;
                pageImg.raycastTarget = false;
                EditorUtility.SetDirty(pageImg);
                conforme++;
                log.AppendLine("- Image PageInvocation alpha 0");
            }

            Transform bgLayer = FindDeep(scene, "BackgroundLayer");
            if (bgLayer != null)
            {
                conforme++;
                log.AppendLine("- BackgroundLayer scene present");
            }
            else
            {
                conforme++;
                log.AppendLine("- Pas de BackgroundLayer (fond page seul)");
            }
        }

        private static void ProcessHideNamed(
            Transform page,
            string name,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme)
        {
            Transform t = FindChildNamed(page, name);
            if (t == null)
                t = FindDeepUnder(page, name);

            if (t == null)
            {
                conforme++;
                log.AppendLine($"- {name} absent — OK");
            }
            else if (!t.gameObject.activeSelf)
            {
                conforme++;
                log.AppendLine($"- {name} deja inactif");
            }
            else if (!apply)
            {
                todo++;
                log.AppendLine($"- [DRY] Masquer {name} — A FAIRE");
            }
            else
            {
                Undo.RecordObject(t.gameObject, UndoLabel);
                t.gameObject.SetActive(false);
                conforme++;
                log.AppendLine($"- {name} masque");
            }
        }

        private static void BuildOrRefreshPortalRoot(
            Transform page,
            Transform existingRoot,
            Sprite iconUp,
            Sprite iconDown,
            Sprite spriteM,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            GameObject rootGo;
            if (existingRoot != null)
            {
                rootGo = existingRoot.gameObject;
                Undo.RegisterCompleteObjectUndo(rootGo, UndoLabel);
                for (int i = rootGo.transform.childCount - 1; i >= 0; i--)
                    Undo.DestroyObjectImmediate(rootGo.transform.GetChild(i).gameObject);
                log.AppendLine("- PortalRoot existant → rebuild enfants");
            }
            else
            {
                rootGo = new GameObject(RootName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(rootGo, UndoLabel);
                rootGo.transform.SetParent(page, false);
                log.AppendLine("- PortalRoot cree");
            }

            RectTransform rootRt = rootGo.GetComponent<RectTransform>();
            StretchFull(rootRt);

            if (rootGo.GetComponent<PageHeaderClearance>() == null)
                Undo.AddComponent<PageHeaderClearance>(rootGo);

            GameObject scrollGo = CreateUi("PortalScroll", rootGo.transform);
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            StretchFull(scrollRt);
            scrollRt.offsetMin = new Vector2(UiTheme.Space3, 0f);
            scrollRt.offsetMax = new Vector2(-UiTheme.Space3, 0f);

            GameObject viewportGo = CreateUi("Viewport", scrollGo.transform);
            RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
            StretchFull(viewportRt);
            Image vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);
            vpImg.raycastTarget = true;
            viewportGo.AddComponent<RectMask2D>();

            GameObject contentGo = CreateUi("Content", viewportGo.transform);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            PortalSnapScroller snap = scrollGo.AddComponent<PortalSnapScroller>();
            PortalCellSizer sizer = scrollGo.AddComponent<PortalCellSizer>();

            for (int i = 0; i < PortalCount; i++)
            {
                GameObject card = CreateUi($"PortalCard_{i}", contentGo.transform);
                LayoutElement le = card.AddComponent<LayoutElement>();
                le.preferredHeight = 600f;
                le.minHeight = 200f;
                Image cardImg = card.AddComponent<Image>();
                cardImg.sprite = spriteM;
                cardImg.type = Image.Type.Sliced;
                cardImg.color = PlaceholderTints[i % PlaceholderTints.Length];
                cardImg.raycastTarget = true;
            }

            Button btnUp = BuildArrow("ArrowUp", rootGo.transform, iconUp, true);
            Button btnDown = BuildArrow("ArrowDown", rootGo.transform, iconDown, false);

            GameObject dotsGo = CreateUi("DotIndicator", rootGo.transform);
            RectTransform dotsRt = dotsGo.GetComponent<RectTransform>();
            dotsRt.anchorMin = new Vector2(0.5f, 0f);
            dotsRt.anchorMax = new Vector2(0.5f, 0f);
            dotsRt.pivot = new Vector2(0.5f, 0f);
            float dotsWidth = PortalCount * DotSize + (PortalCount - 1) * DotGap;
            dotsRt.sizeDelta = new Vector2(dotsWidth, DotSize);
            dotsRt.anchoredPosition = new Vector2(0f, UiTheme.Space4);
            HorizontalLayoutGroup dH = dotsGo.AddComponent<HorizontalLayoutGroup>();
            dH.spacing = DotGap;
            dH.childAlignment = TextAnchor.MiddleCenter;
            dH.childControlWidth = true;
            dH.childControlHeight = true;
            for (int i = 0; i < PortalCount; i++)
            {
                GameObject dot = CreateUi($"Dot_{i}", dotsGo.transform);
                LayoutElement dle = dot.AddComponent<LayoutElement>();
                dle.preferredWidth = DotSize;
                dle.preferredHeight = DotSize;
                Image dimg = dot.AddComponent<Image>();
                dimg.sprite = spriteM;
                dimg.type = Image.Type.Sliced;
                dimg.color = i == 0
                    ? new Color(1f, 1f, 1f, 0.95f)
                    : new Color(1f, 1f, 1f, 0.28f);
                dimg.raycastTarget = false;
            }

            PortalSnapChrome chrome = rootGo.GetComponent<PortalSnapChrome>();
            if (chrome == null)
                chrome = Undo.AddComponent<PortalSnapChrome>(rootGo);
            SerializedObject chromeSo = new SerializedObject(chrome);
            chromeSo.FindProperty("scroller").objectReferenceValue = snap;
            chromeSo.FindProperty("arrowUp").objectReferenceValue = btnUp;
            chromeSo.FindProperty("arrowDown").objectReferenceValue = btnDown;
            chromeSo.FindProperty("dotContainer").objectReferenceValue = dotsGo.transform;
            chromeSo.FindProperty("logSnaps").boolValue = true;
            chromeSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject sizerSo = new SerializedObject(sizer);
            sizerSo.FindProperty("viewport").objectReferenceValue = viewportRt;
            sizerSo.FindProperty("content").objectReferenceValue = contentRt;
            sizerSo.FindProperty("heightFraction").floatValue = HeightFraction;
            sizerSo.FindProperty("spacingFraction").floatValue = SpacingFraction;
            sizerSo.FindProperty("centerInViewport").boolValue = true;
            sizerSo.ApplyModifiedPropertiesWithoutUndo();

            sizer.Bind(viewportRt, contentRt);
            snap.RecalculateMetrics();
            snap.SnapImmediate(0);
            conforme++;
            log.AppendLine("- PortalRoot structure creee");
        }

        private static Button BuildArrow(string name, Transform parent, Sprite icon, bool isUp)
        {
            GameObject go = CreateUi(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, isUp ? 1f : 0f);
            rt.anchorMax = new Vector2(0.5f, isUp ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, isUp ? 1f : 0f);
            rt.sizeDelta = new Vector2(ArrowSize, ArrowSize);
            float y = isUp ? -UiTheme.Space3 : (DotSize + UiTheme.Space4 + UiTheme.Space2);
            rt.anchoredPosition = new Vector2(0f, y);
            Image img = go.AddComponent<Image>();
            img.sprite = icon;
            img.color = new Color(
                UiTheme.TextPrimary.r, UiTheme.TextPrimary.g, UiTheme.TextPrimary.b, 0.75f);
            img.preserveAspect = true;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        private static void ValidateExisting(
            Transform root, StringBuilder log, ref int conforme, ref int failed)
        {
            if (root.Find("PortalScroll") == null)
            {
                failed++;
                log.AppendLine("- X PortalScroll manquant");
            }
            else
            {
                conforme++;
                log.AppendLine("- PortalScroll present");
            }

            if (root.GetComponentInChildren<PortalSnapScroller>(true) == null)
            {
                failed++;
                log.AppendLine("- X PortalSnapScroller manquant");
            }
            else
            {
                conforme++;
                log.AppendLine("- Snap detecte");
            }
        }

        // ═══════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════

        private static Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = CreateUi(name, parent);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            if (sprite != null)
                img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI MakeTmp(
            string name,
            Transform parent,
            float size,
            Color color,
            TextAlignmentOptions align)
        {
            GameObject go = CreateUi(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.text = string.Empty;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static GameObject CreateUi(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static Transform FindDeep(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform f = FindDeepUnder(roots[i].transform, name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private static Transform FindDeepUnder(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform f = FindDeepUnder(root.GetChild(i), name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private static Transform FindChildNamed(Transform parent, string name)
        {
            if (parent == null)
                return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == name)
                    return parent.GetChild(i);
            }

            return null;
        }

        private static string GetPath(Transform t)
        {
            if (t == null)
                return "(null)";
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private static void AppendCounter(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine("───────────────────────────────────────────");
            log.AppendLine($" A FAIRE : {todo} | CONFORMES : {conforme} | ECHECS : {failed}");
            log.AppendLine("───────────────────────────────────────────");
        }
    }
}
#endif
