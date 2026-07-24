#if UNITY_EDITOR
using System.Text;
using ChezArthur.Hub;
using ChezArthur.Hub.Pages;
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
    /// Gate 3.2+ — Accueil mockup : icônes Shop/News haut, Lancer+BossRush stack bas.
    /// Harnais v2. LOCK 2.1 / rig / framing / nav intacts.
    /// </summary>
    public static class HomeActionsBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string UndoLabel = "Home Actions 3.2";
        private const string RigName = "HomeIllustrationRig";
        private const string BottomZoneName = "BottomZone";
        private const string TopUtilityName = "TopUtilityRow";
        private const string MusicSlotName = "MusicPlayerSlot";
        private const string BtnLancerName = "BtnLancerRun";
        private const string SecondaryRowName = "SecondaryRow";
        private const string BtnBossRushName = "BtnBossRush";
        private const string BtnMagasinName = "BtnMagasin";
        private const string BtnNewsName = "BtnNews";
        private const string UiLayerName = "UILayer";
        private const string ModeSelectName = "ModeSelectOverlay";
        private const string NavName = "NavigationBar";
        private const float ExpectedBottomZoneH = 292f;
        private const float IconButtonSize = 96f;

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Refonte Hub/Construire les Actions Accueil (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Construire les Actions Accueil (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Actions Accueil Gate 3.2",
                    "Layout mockup :\n" +
                    "• TopUtility sous SafeRoot (sous Header) : Magasin / News\n" +
                    "• BottomZone : Lancer + Boss Rush + clearance nav\n" +
                    "• Nav tabs centrés dans l'encart footer\n" +
                    "LOCK 2.1 / framing / rig intacts.\nContinuer ?",
                    "Appliquer",
                    "Annuler"))
                return;

            Run(apply: true);
        }

        // ═══════════════════════════════════════════
        // PIPELINE
        // ═══════════════════════════════════════════

        private static void Run(bool apply)
        {
            var log = new StringBuilder(8192);
            string mode = apply ? "APPLIQUER" : "DRY RUN";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" HomeActionsBuilder — {mode}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine(" Layout mockup : utility haut + stack run bas");
            log.AppendLine(" LOCK 2.1 : header / nav / framing / rig non modifiés");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int todo = 0;
            int conforme = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[HomeActionsBuilder] Aucune scène active.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine();

            RectTransform page = FindPageAccueil(scene);
            if (page == null)
            {
                failed++;
                log.AppendLine("- ✗ PageAccueil introuvable — abort");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            log.AppendLine($"PageAccueil : `{GetPath(page)}`");
            PageAccueilUI pageUi = page.GetComponent<PageAccueilUI>();
            if (pageUi == null)
            {
                failed++;
                log.AppendLine("- ✗ PageAccueilUI absent — abort");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            RectTransform bottomZone = FindDirectChildNamed(page, BottomZoneName);
            if (bottomZone == null)
            {
                failed++;
                log.AppendLine("- ✗ BottomZone absent (gate 3.1 requis) — abort");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            RectTransform nav = FindNavigationBar(scene);
            float navH = BottomZoneNavClearance.ResolveNavHeight(nav);
            log.AppendLine(
                $"NavigationBar : {(nav != null ? GetPath(nav) : "—")} hauteur≈{navH:0.#}");
            log.AppendLine();

            log.AppendLine("## BottomZone layout + clearance nav");
            EnsureBottomZoneLayout(bottomZone, nav, navH, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            log.AppendLine("## TopUtilityRow (icônes Magasin / News)");
            ActionRefs utility = EnsureTopUtility(page, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            log.AppendLine("## BottomZone contenu (Lancer + Boss Rush)");
            ActionRefs bottom = EnsureBottomActions(
                bottomZone, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            ActionRefs actions = new ActionRefs
            {
                Lancer = bottom.Lancer,
                BossRush = bottom.BossRush,
                Magasin = utility.Magasin,
                News = utility.News
            };

            log.AppendLine("## PageAccueilUI refs");
            EnsurePageWiring(pageUi, actions, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            log.AppendLine("## Suppressions legacy");
            EnsureLegacyRemoved(page, bottomZone, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            log.AppendLine("## Ordre PageAccueil (Rig → BottomZone)");
            EnsurePageSiblingOrder(page, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            log.AppendLine("## Nav tabs centrés (encart footer)");
            EnsureNavTabsCentered(scene, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            log.AppendLine("## Vérif framing");
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bottomZone);
            float h = bottomZone.rect.height;
            if (h < 0.01f)
                h = bottomZone.sizeDelta.y;
            log.AppendLine(
                $"- BottomZone hauteur ≈ {h:0.#} px (attendu ~{ExpectedBottomZoneH})");
            log.AppendLine(
                $"- BottomZone posY ≈ {bottomZone.anchoredPosition.y:0.#} (nav ≈ {navH:0.#})");
            log.AppendLine(
                "- HomeIllustrationFraming suit seul via BottomZone — aucune action.");
            if (Mathf.Abs(h - ExpectedBottomZoneH) <= 8f)
            {
                conforme++;
                log.AppendLine("- Hauteur BottomZone dans la tolérance ✓");
            }
            else if (!apply)
            {
                log.AppendLine("- Hauteur hors cible en DRY (normal si contenu pas encore créé)");
            }
            else
            {
                failed++;
                log.AppendLine($"- Hauteur BottomZone hors cible ✗ (got {h:0.#})");
            }

            if (apply)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                log.AppendLine();
                log.AppendLine("Scène marquée dirty — Ctrl+S.");
                log.AppendLine("Sprites Shop/News : assigner sur BtnMagasin/BtnNews → Icon.");
            }

            AppendCounter(log, todo, conforme, failed);
            Debug.Log(log.ToString());

            if (apply && failed == 0 && todo == 0)
                Debug.Log($"[HomeActionsBuilder] APPLIQUER OK — CONFORMES={conforme}.");
            else if (apply && failed > 0)
                Debug.LogError($"[HomeActionsBuilder] APPLIQUER INCOMPLET — échecs={failed}.");
            else if (!apply && todo == 0 && failed == 0)
                Debug.Log($"[HomeActionsBuilder] DRY RUN — convergence OK (CONFORMES={conforme}).");
        }

        private static void AppendCounter(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine();
            log.AppendLine("## COMPTEUR D'ACTIONS (harnais v2)");
            log.AppendLine($"- À FAIRE : {todo}");
            log.AppendLine($"- CONFORMES : {conforme}");
            log.AppendLine($"- ÉCHECS : {failed}");
            log.AppendLine(todo == 0 && failed == 0
                ? "- Convergence : OUI (À FAIRE = 0)"
                : "- Convergence : NON");
        }

        // ═══════════════════════════════════════════
        // BOTTOM ZONE
        // ═══════════════════════════════════════════

        private static void EnsureBottomZoneLayout(
            RectTransform zone,
            RectTransform nav,
            float navH,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            bool layoutOk = IsBottomZoneLayoutOk(zone);
            BottomZoneNavClearance clearance = zone.GetComponent<BottomZoneNavClearance>();
            bool clearanceOk = clearance != null
                               && IsClearanceWired(clearance, nav)
                               && Mathf.Approximately(zone.anchoredPosition.y, navH);

            if (layoutOk && clearanceOk)
            {
                conforme++;
                log.AppendLine($"- BottomZone VLG+CSF + clearance nav ({navH:0.#}) ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    $"- [DRY] Aligner BottomZone VLG+CSF + BottomZoneNavClearance (posY={navH:0.#}) — À FAIRE");
                return;
            }

            Undo.RecordObject(zone, UndoLabel);
            zone.anchorMin = new Vector2(0f, 0f);
            zone.anchorMax = new Vector2(1f, 0f);
            zone.pivot = new Vector2(0.5f, 0f);
            zone.anchoredPosition = new Vector2(0f, navH);
            zone.sizeDelta = new Vector2(0f, zone.sizeDelta.y);
            EditorUtility.SetDirty(zone);

            VerticalLayoutGroup vlg = zone.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = Undo.AddComponent<VerticalLayoutGroup>(zone.gameObject);
            Undo.RecordObject(vlg, UndoLabel);
            int padLR = Mathf.RoundToInt(UiTheme.Space4);
            int padTB = Mathf.RoundToInt(UiTheme.Space3);
            vlg.padding = new RectOffset(padLR, padLR, padTB, padTB);
            vlg.spacing = UiTheme.Space3;
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            EditorUtility.SetDirty(vlg);

            ContentSizeFitter csf = zone.GetComponent<ContentSizeFitter>();
            if (csf == null)
                csf = Undo.AddComponent<ContentSizeFitter>(zone.gameObject);
            Undo.RecordObject(csf, UndoLabel);
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EditorUtility.SetDirty(csf);

            if (clearance == null)
                clearance = Undo.AddComponent<BottomZoneNavClearance>(zone.gameObject);
            clearance.BindNavigationBar(nav);
            EditorUtility.SetDirty(clearance);

            conforme++;
            log.AppendLine($"- BottomZone layout + clearance (posY={navH:0.#}) ✓ → conforme");
        }

        private static bool IsBottomZoneLayoutOk(RectTransform zone)
        {
            VerticalLayoutGroup vlg = zone.GetComponent<VerticalLayoutGroup>();
            ContentSizeFitter csf = zone.GetComponent<ContentSizeFitter>();
            if (vlg == null || csf == null)
                return false;

            return vlg.childControlWidth
                   && vlg.childControlHeight
                   && !vlg.childForceExpandHeight
                   && Mathf.Approximately(vlg.spacing, UiTheme.Space3)
                   && vlg.padding.left == Mathf.RoundToInt(UiTheme.Space4)
                   && csf.horizontalFit == ContentSizeFitter.FitMode.Unconstrained
                   && csf.verticalFit == ContentSizeFitter.FitMode.PreferredSize;
        }

        private static bool IsClearanceWired(BottomZoneNavClearance clearance, RectTransform nav)
        {
            if (clearance == null)
                return false;
            SerializedObject so = new SerializedObject(clearance);
            return so.FindProperty("navigationBar").objectReferenceValue == nav;
        }

        private struct ActionRefs
        {
            public HubButtonUI Lancer;
            public HubButtonUI BossRush;
            public HubButtonUI Magasin;
            public HubButtonUI News;
            public bool BottomComplete => Lancer != null && BossRush != null;
            public bool UtilityComplete => Magasin != null && News != null;
            public bool Complete => BottomComplete && UtilityComplete;
        }

        private static ActionRefs EnsureBottomActions(
            RectTransform zone,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            HubButtonUI lancer = FindButton(zone, BtnLancerName);
            HubButtonUI boss = FindButton(zone, BtnBossRushName);
            // Boss Rush peut encore être sous SecondaryRow legacy.
            if (boss == null)
            {
                RectTransform row = FindDirectChildNamed(zone, SecondaryRowName);
                if (row != null)
                    boss = FindButton(row, BtnBossRushName);
            }

            RectTransform music = FindDirectChildNamed(zone, MusicSlotName);
            bool ok = lancer != null && boss != null && music != null
                      && boss.transform.parent == zone
                      && FindDirectChildNamed(zone, SecondaryRowName) == null;

            if (ok)
            {
                conforme++;
                log.AppendLine("- Contenu bas (Music + Lancer + BossRush) conforme ✓");
                return new ActionRefs { Lancer = lancer, BossRush = boss };
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    "- [DRY] BottomZone = MusicPlayerSlot + BtnLancerRun + BtnBossRush (stack) — À FAIRE");
                return new ActionRefs { Lancer = lancer, BossRush = boss };
            }

            if (music == null)
            {
                GameObject musicGo = new GameObject(MusicSlotName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(musicGo, UndoLabel);
                musicGo.transform.SetParent(zone, false);
                music = (RectTransform)musicGo.transform;
            }

            LayoutElement musicLe = music.GetComponent<LayoutElement>();
            if (musicLe == null)
                musicLe = Undo.AddComponent<LayoutElement>(music.gameObject);
            Undo.RecordObject(musicLe, UndoLabel);
            musicLe.preferredHeight = 0f;
            musicLe.minHeight = 0f;
            musicLe.flexibleWidth = 1f;
            EditorUtility.SetDirty(musicLe);

            if (lancer == null)
            {
                lancer = UiKitFactory.CreateButton(
                    zone,
                    HubButtonUI.ButtonVariant.Primary,
                    "LANCER UNE RUN",
                    null,
                    UiTheme.ButtonPrimaryH,
                    locked: false,
                    objectName: BtnLancerName);
            }
            else
            {
                ApplyExistingButton(
                    lancer, HubButtonUI.ButtonVariant.Primary, "LANCER UNE RUN", null,
                    UiTheme.ButtonPrimaryH, locked: false);
            }

            // Remonter Boss Rush hors SecondaryRow si besoin.
            if (boss != null && boss.transform.parent != zone)
            {
                Undo.SetTransformParent(boss.transform, zone, UndoLabel);
            }

            if (boss == null)
            {
                boss = UiKitFactory.CreateButton(
                    zone,
                    HubButtonUI.ButtonVariant.Secondary,
                    "Boss Rush",
                    null,
                    UiTheme.ButtonSecondaryH,
                    locked: false,
                    objectName: BtnBossRushName);
            }
            else
            {
                ApplyExistingButton(
                    boss, HubButtonUI.ButtonVariant.Secondary, "Boss Rush", null,
                    UiTheme.ButtonSecondaryH, locked: false);
            }

            music.SetSiblingIndex(0);
            if (lancer != null)
                lancer.transform.SetSiblingIndex(1);
            if (boss != null)
                boss.transform.SetSiblingIndex(2);

            // Purge SecondaryRow legacy (Magasin/News déplacés en TopUtility).
            RectTransform legacyRow = FindDirectChildNamed(zone, SecondaryRowName);
            if (legacyRow != null)
            {
                log.AppendLine($"- SUPPRIMER SecondaryRow legacy `{GetPath(legacyRow)}`");
                // Déplacer Magasin/News hors row avant destroy si encore dedans.
                HubButtonUI oldMag = FindButton(legacyRow, BtnMagasinName);
                HubButtonUI oldNews = FindButton(legacyRow, BtnNewsName);
                if (oldMag != null)
                    Undo.DestroyObjectImmediate(oldMag.gameObject);
                if (oldNews != null)
                    Undo.DestroyObjectImmediate(oldNews.gameObject);
                Undo.DestroyObjectImmediate(legacyRow.gameObject);
            }

            if (lancer != null && boss != null)
            {
                conforme++;
                log.AppendLine("- Contenu bas stack créé/aligné ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- Contenu bas — ÉCHEC ✗");
            }

            return new ActionRefs { Lancer = lancer, BossRush = boss };
        }

        // ═══════════════════════════════════════════
        // TOP UTILITY
        // ═══════════════════════════════════════════

        private static ActionRefs EnsureTopUtility(
            RectTransform page,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            RectTransform safeRoot = FindSafeRoot(page);
            RectTransform header = FindHeader(safeRoot);
            float headerInset = TopUtilityHeaderClearance.ResolveHeaderBottomInset(header);
            float utilityGap = UiTheme.Space3;
            float rowH = IconButtonSize + UiTheme.Space1 + UiTheme.Space5; // icône + marge + caption

            // Préférer SafeRoot (après Header) — sinon masqué par le Header sibling.
            RectTransform rowParent = safeRoot != null ? safeRoot : page;
            RectTransform row = FindDirectChildNamed(rowParent, TopUtilityName);
            if (row == null && page != null)
                row = FindDirectChildNamed(page, TopUtilityName);

            HubButtonUI magasin = row != null ? FindButton(row, BtnMagasinName) : null;
            HubButtonUI news = row != null ? FindButton(row, BtnNewsName) : null;
            TopUtilityHeaderClearance clearance = row != null
                ? row.GetComponent<TopUtilityHeaderClearance>()
                : null;

            bool parentOk = row != null && safeRoot != null && row.parent == safeRoot;
            bool clearanceOk = clearance != null;
            bool captionsOk = row != null
                             && row.Find(BtnMagasinName + "Caption") != null
                             && row.Find(BtnNewsName + "Caption") != null;
            bool posOk = row != null
                         && Mathf.Approximately(
                             row.anchoredPosition.y,
                             -(headerInset + utilityGap));

            if (row != null && parentOk && clearanceOk && captionsOk && posOk
                && magasin != null && news != null)
            {
                conforme++;
                log.AppendLine(
                    $"- TopUtilityRow sous SafeRoot (sous header inset≈{headerInset:0.#} + gap {utilityGap}) ✓");
                return new ActionRefs { Magasin = magasin, News = news };
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    $"- [DRY] TopUtility → SafeRoot posY=-{headerInset + utilityGap:0.#} + captions Shop/News — À FAIRE");
                return new ActionRefs { Magasin = magasin, News = news };
            }

            if (safeRoot == null)
            {
                failed++;
                log.AppendLine("- SafeRoot introuvable pour TopUtility ✗");
                return new ActionRefs { Magasin = magasin, News = news };
            }

            if (row == null)
            {
                GameObject rowGo = new GameObject(TopUtilityName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(rowGo, UndoLabel);
                rowGo.transform.SetParent(safeRoot, false);
                row = (RectTransform)rowGo.transform;
            }
            else if (row.parent != safeRoot)
            {
                Undo.SetTransformParent(row, safeRoot, UndoLabel);
            }

            Undo.RecordObject(row, UndoLabel);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -(headerInset + utilityGap));
            row.sizeDelta = new Vector2(0f, rowH);
            row.localScale = Vector3.one;
            int headerIdx = header != null ? header.GetSiblingIndex() : 0;
            row.SetSiblingIndex(headerIdx + 1);
            EditorUtility.SetDirty(row);

            if (clearance == null)
                clearance = Undo.AddComponent<TopUtilityHeaderClearance>(row.gameObject);
            SerializedObject clearSo = new SerializedObject(clearance);
            clearSo.FindProperty("header").objectReferenceValue = header;
            clearSo.FindProperty("gap").floatValue = utilityGap;
            clearSo.ApplyModifiedPropertiesWithoutUndo();
            clearance.BindHeader(header);
            EditorUtility.SetDirty(clearance);

            if (magasin == null)
            {
                magasin = UiKitFactory.CreateIconButton(
                    row, BtnMagasinName, IconButtonSize, locked: true);
            }

            if (news == null)
            {
                news = UiKitFactory.CreateIconButton(
                    row, BtnNewsName, IconButtonSize, locked: true);
            }

            PlaceIconButton(magasin, left: true);
            PlaceIconButton(news, left: false);
            EnsureIconCaption(row, magasin, BtnMagasinName + "Caption", "Shop");
            EnsureIconCaption(row, news, BtnNewsName + "Caption", "News");

            // Visibilité : locked alpha 0.55 + icon fantôme — forcer cadre lisible.
            BoostIconVisibility(magasin);
            BoostIconVisibility(news);

            if (magasin != null && news != null)
            {
                conforme++;
                log.AppendLine(
                    $"- TopUtilityRow SafeRoot + captions (posY=-{headerInset + utilityGap:0.#}) ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- TopUtilityRow — ÉCHEC ✗");
            }

            return new ActionRefs { Magasin = magasin, News = news };
        }

        private static void EnsureIconCaption(
            RectTransform row,
            HubButtonUI btn,
            string captionName,
            string text)
        {
            if (row == null || btn == null)
                return;

            Transform existing = row.Find(captionName);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(captionName, typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(row, false);
            }
            else
            {
                go = existing.gameObject;
            }

            RectTransform rt = (RectTransform)go.transform;
            RectTransform btnRt = btn.transform as RectTransform;
            Undo.RecordObject(rt, UndoLabel);
            bool left = btnRt != null && btnRt.anchorMin.x < 0.5f;
            if (left)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(
                    UiTheme.Space4,
                    -(IconButtonSize + UiTheme.Space1));
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(
                    -UiTheme.Space4,
                    -(IconButtonSize + UiTheme.Space1));
            }

            rt.sizeDelta = new Vector2(IconButtonSize + UiTheme.Space4, UiTheme.Space5);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(tmp, UndoLabel);
            tmp.text = text;
            tmp.fontSize = UiTypography.Caption;
            tmp.alignment = left ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight;
            tmp.color = UiTheme.TextSecondary;
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);
            EditorUtility.SetDirty(rt);
        }

        private static void BoostIconVisibility(HubButtonUI btn)
        {
            if (btn == null)
                return;

            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                Undo.RecordObject(cg, UndoLabel);
                // Locked reste non-interactable via HubButtonUI, mais lisible.
                cg.alpha = 0.85f;
                EditorUtility.SetDirty(cg);
            }

            Transform icon = btn.transform.Find("Icon");
            if (icon != null)
            {
                Image img = icon.GetComponent<Image>();
                if (img != null)
                {
                    Undo.RecordObject(img, UndoLabel);
                    img.color = new Color(1f, 1f, 1f, 0.9f);
                    EditorUtility.SetDirty(img);
                }
            }
        }

        private static void PlaceIconButton(HubButtonUI btn, bool left)
        {
            if (btn == null)
                return;
            RectTransform rt = btn.transform as RectTransform;
            if (rt == null)
                return;

            Undo.RecordObject(rt, UndoLabel);
            float margin = UiTheme.Space4;
            if (left)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(margin, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-margin, 0f);
            }

            rt.sizeDelta = new Vector2(IconButtonSize, IconButtonSize);
            EditorUtility.SetDirty(rt);

            LayoutElement le = btn.GetComponent<LayoutElement>();
            if (le != null)
            {
                Undo.RecordObject(le, UndoLabel);
                le.minWidth = IconButtonSize;
                le.preferredWidth = IconButtonSize;
                le.minHeight = IconButtonSize;
                le.preferredHeight = IconButtonSize;
                EditorUtility.SetDirty(le);
            }
        }

        private static bool IsTopUtilityOk(RectTransform row)
        {
            return row != null
                   && Mathf.Approximately(row.anchorMin.y, 1f)
                   && Mathf.Approximately(row.anchorMax.y, 1f)
                   && Mathf.Approximately(row.pivot.y, 1f);
        }

        // ═══════════════════════════════════════════
        // WIRING / CLEANUP / ORDER
        // ═══════════════════════════════════════════

        private static void ApplyExistingButton(
            HubButtonUI btn,
            HubButtonUI.ButtonVariant variant,
            string label,
            string subLabel,
            float height,
            bool locked)
        {
            SerializedObject so = new SerializedObject(btn);
            so.FindProperty("variant").enumValueIndex = (int)variant;
            so.FindProperty("locked").boolValue = locked;
            Sprite s = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite m = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite l = RoundedRectSpriteGenerator.LoadSpriteL();
            if (s != null) so.FindProperty("roundedSpriteS").objectReferenceValue = s;
            if (m != null) so.FindProperty("roundedSpriteM").objectReferenceValue = m;
            if (l != null) so.FindProperty("roundedSpriteL").objectReferenceValue = l;
            so.ApplyModifiedPropertiesWithoutUndo();
            btn.ApplyStyle();
            btn.SetLabel(label);
            btn.SetSubLabel(subLabel);
            btn.ApplyStyle();

            LayoutElement le = btn.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(btn.gameObject);
            Undo.RecordObject(le, UndoLabel);
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            EditorUtility.SetDirty(le);
        }

        private static void EnsurePageWiring(
            PageAccueilUI pageUi,
            ActionRefs actions,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (!actions.Complete)
            {
                if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] Recâbler PageAccueilUI (refs manquantes) — À FAIRE");
                }
                else
                {
                    failed++;
                    log.AppendLine("- Recâblage PageAccueilUI impossible ✗");
                }

                return;
            }

            SerializedObject so = new SerializedObject(pageUi);
            bool wired =
                so.FindProperty("buttonLancerRun").objectReferenceValue == actions.Lancer
                && so.FindProperty("buttonBossRush").objectReferenceValue == actions.BossRush
                && so.FindProperty("buttonMagasin").objectReferenceValue == actions.Magasin
                && so.FindProperty("buttonNews").objectReferenceValue == actions.News;

            if (wired)
            {
                conforme++;
                log.AppendLine("- PageAccueilUI 4 refs câblées ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Recâbler PageAccueilUI (4 HubButtonUI) — À FAIRE");
                return;
            }

            so.FindProperty("buttonLancerRun").objectReferenceValue = actions.Lancer;
            so.FindProperty("buttonBossRush").objectReferenceValue = actions.BossRush;
            so.FindProperty("buttonMagasin").objectReferenceValue = actions.Magasin;
            so.FindProperty("buttonNews").objectReferenceValue = actions.News;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pageUi);

            conforme++;
            log.AppendLine("- PageAccueilUI recâblé ✓ → conforme");
        }

        private static void EnsureLegacyRemoved(
            RectTransform page,
            RectTransform bottomZone,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            RectTransform uiLayer = FindDirectChildNamed(page, UiLayerName);
            Transform modeSelect = page.Find(ModeSelectName);
            RectTransform secondaryRow = FindDirectChildNamed(bottomZone, SecondaryRowName);

            bool clean = uiLayer == null && modeSelect == null && secondaryRow == null;
            if (clean)
            {
                conforme++;
                log.AppendLine("- Legacy UILayer / ModeSelect / SecondaryRow absents ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                if (uiLayer != null)
                    log.AppendLine($"- [DRY] SUPPRIMER `{GetPath(uiLayer)}` — À FAIRE");
                if (modeSelect != null)
                    log.AppendLine($"- [DRY] SUPPRIMER `{GetPath(modeSelect)}` — À FAIRE");
                if (secondaryRow != null)
                    log.AppendLine($"- [DRY] SUPPRIMER `{GetPath(secondaryRow)}` — À FAIRE");
                return;
            }

            if (uiLayer != null)
            {
                log.AppendLine($"- SUPPRIMER `{GetPath(uiLayer)}`");
                Undo.DestroyObjectImmediate(uiLayer.gameObject);
            }

            if (modeSelect != null)
            {
                log.AppendLine($"- SUPPRIMER `{GetPath(modeSelect)}`");
                Undo.DestroyObjectImmediate(modeSelect.gameObject);
            }

            if (secondaryRow != null)
            {
                log.AppendLine($"- SUPPRIMER `{GetPath(secondaryRow)}`");
                Undo.DestroyObjectImmediate(secondaryRow.gameObject);
            }

            conforme++;
            log.AppendLine("- Suppressions legacy ✓ → conforme");
        }

        private static void EnsurePageSiblingOrder(
            RectTransform page,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            RectTransform rig = FindDirectChildNamed(page, RigName);
            RectTransform zone = FindDirectChildNamed(page, BottomZoneName);
            // Ancien TopUtility sous page → à remonter (fait dans EnsureTopUtility).
            RectTransform strayUtility = FindDirectChildNamed(page, TopUtilityName);

            if (rig == null || zone == null)
            {
                failed++;
                log.AppendLine("- Ordre impossible (Rig ou BottomZone manquant) ✗");
                return;
            }

            bool orderOk = strayUtility == null
                           && page.childCount == 2
                           && rig.GetSiblingIndex() == 0
                           && zone.GetSiblingIndex() == 1;

            if (orderOk)
            {
                conforme++;
                log.AppendLine("- Ordre Rig → BottomZone (TopUtility hors page) ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    $"- [DRY] Ordre Rig → BottomZone (enfants={page.childCount}, strayUtility={(strayUtility != null)}) — À FAIRE");
                return;
            }

            if (strayUtility != null)
                log.AppendLine("- TopUtility encore sous page — EnsureTopUtility le remonte");

            rig.SetSiblingIndex(0);
            zone.SetSiblingIndex(1);
            conforme++;
            log.AppendLine("- Ordre Rig → BottomZone appliqué ✓ → conforme");
        }

        private static void EnsureNavTabsCentered(
            Scene scene,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            RectTransform nav = FindNavigationBar(scene);
            if (nav == null)
            {
                failed++;
                log.AppendLine("- NavigationBar introuvable pour centrage tabs ✗");
                return;
            }

            Transform template = nav.Find("TabTemplate");
            RectTransform line = template != null
                ? template.Find("ActiveTopLine") as RectTransform
                : null;
            RectTransform slot = template != null
                ? template.Find("IconSlot") as RectTransform
                : null;

            bool ok = line != null
                      && slot != null
                      && Mathf.Approximately(slot.anchorMin.y, 0.38f)
                      && Mathf.Approximately(line.anchorMin.y, 0.62f);

            if (ok)
            {
                conforme++;
                log.AppendLine("- TabTemplate abaissé (IconSlot 0.38 / ActiveTopLine 0.62) ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Abaisser TabTemplate (icônes + barre orange) — À FAIRE");
                return;
            }

            if (template == null || line == null || slot == null)
            {
                failed++;
                log.AppendLine("- TabTemplate incomplet ✗ — relancer HubNavBuilder");
                return;
            }

            Undo.RecordObject(line, UndoLabel);
            line.anchorMin = new Vector2(0.18f, 0.62f);
            line.anchorMax = new Vector2(0.82f, 0.62f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = Vector2.zero;
            line.sizeDelta = new Vector2(0f, UiTheme.BorderFocus);
            EditorUtility.SetDirty(line);

            Undo.RecordObject(slot, UndoLabel);
            slot.anchorMin = new Vector2(0.5f, 0.38f);
            slot.anchorMax = new Vector2(0.5f, 0.38f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = Vector2.zero;
            EditorUtility.SetDirty(slot);

            Transform labelTx = template.Find("Label");
            if (labelTx != null)
            {
                RectTransform labelRt = labelTx as RectTransform;
                Undo.RecordObject(labelRt, UndoLabel);
                labelRt.anchorMin = new Vector2(0f, 0.02f);
                labelRt.anchorMax = new Vector2(1f, 0.22f);
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                EditorUtility.SetDirty(labelRt);
            }

            HubNavBarUI bar = nav.GetComponent<HubNavBarUI>();
            if (bar != null)
            {
                bar.Rebuild();
                EditorUtility.SetDirty(bar);
            }

            HubNavSafeBleed bleed = nav.GetComponent<HubNavSafeBleed>();
            if (bleed != null)
                bleed.Refresh();

            conforme++;
            log.AppendLine("- TabTemplate recentré + HubNavBarUI.Rebuild ✓ → conforme");
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static HubButtonUI FindButton(Transform parent, string name)
        {
            RectTransform rt = FindDirectChildNamed(parent, name);
            return rt != null ? rt.GetComponent<HubButtonUI>() : null;
        }

        private static RectTransform FindSafeRoot(RectTransform page)
        {
            if (page == null)
                return null;
            Transform t = page;
            while (t != null)
            {
                if (t.name == "SafeRoot")
                    return t as RectTransform;
                t = t.parent;
            }

            return null;
        }

        private static RectTransform FindHeader(RectTransform safeRoot)
        {
            if (safeRoot == null)
                return null;
            return FindDirectChildNamed(safeRoot, "Header");
        }

        private static RectTransform FindNavigationBar(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                RectTransform found = FindInChildren(roots[i].transform, NavName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static RectTransform FindInChildren(Transform root, string name)
        {
            if (root.name == name)
                return root as RectTransform;
            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform found = FindInChildren(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static RectTransform FindPageAccueil(Scene scene)
        {
            HubManager hub = Object.FindObjectOfType<HubManager>();
            if (hub != null && hub.AccueilPage != null)
                return hub.AccueilPage.transform as RectTransform;

            PageAccueilUI ui = Object.FindObjectOfType<PageAccueilUI>(true);
            return ui != null ? ui.transform as RectTransform : null;
        }

        private static RectTransform FindDirectChildNamed(Transform parent, string name)
        {
            if (parent == null)
                return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c != null && c.name == name)
                    return c as RectTransform;
            }

            return null;
        }

        private static string GetPath(Transform t)
        {
            if (t == null)
                return "null";
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }

            return path;
        }
    }
}
#endif
