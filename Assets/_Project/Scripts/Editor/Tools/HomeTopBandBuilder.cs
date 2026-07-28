#if UNITY_EDITOR
using System.Text;
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
    /// Gate 3.3 — bande Accueil sous header : Shop · LofiPlayerBar · News.
    /// Fond BgPanel + hairline (option B). Harnais v2. LOCK 2.1. Framing intact.
    /// </summary>
    public static class HomeTopBandBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string UndoLabel = "Home Top Band 3.3";
        private const string TopUtilityName = "TopUtilityRow";
        private const string BandBackdropName = "BandBackdrop";
        private const string BandHairlineName = "BandHairline";
        private const string ShopClusterName = "ShopCluster";
        private const string NewsClusterName = "NewsCluster";
        private const string BtnMagasinName = "BtnMagasin";
        private const string BtnNewsName = "BtnNews";
        private const string MagasinCaptionName = "BtnMagasinCaption";
        private const string NewsCaptionName = "BtnNewsCaption";
        private const string LofiBarName = "LofiPlayerBar";
        private const string BottomZoneName = "BottomZone";
        private const string MusicSlotName = "MusicPlayerSlot";
        // PageMusique : intouchée (gate Missions) — plus de lecture built-in.

        private const float IconSize = 64f;
        private const float BarHeight = 112f;
        private const float ArtworkSlotSize = 80f;
        private const float ControlIconSize = 64f;
        private const float ProgressH = 3f;

        private const string ShopSpritePath =
            "Assets/_Project/Sprites/Icon valise & item/Porte monnaie.png";
        private const string NewsSpritePath =
            "Assets/_Project/Sprites/UI/UI - info frame.png";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Refonte Hub/Construire bande Accueil Lofi (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Construire bande Accueil Lofi (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Bande Accueil Gate 3.3-fix",
                    "• FIX chrome : backdrop stretch + hairline BorderStrong + gap 0\n" +
                    "• Icônes transport générées (prev/play/pause/next)\n" +
                    "• News : label seul si cadre (info frame)\n" +
                    "LOCK 2.1 Header / AudioManager / framing intacts.\nContinuer ?",
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
            log.AppendLine($" HomeTopBandBuilder — {mode}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine(" Gate 3.3 — Lofi dans bande Shop/News");
            log.AppendLine(" LOCK 2.1 : header / nav / framing / AudioManager intacts");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int todo = 0;
            int conforme = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[HomeTopBandBuilder] Aucune scène active.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine();

            RectTransform safeRoot = FindSafeRoot(scene);
            RectTransform header = FindHeader(safeRoot);
            RectTransform row = FindTopUtility(safeRoot);
            if (row == null)
            {
                failed++;
                log.AppendLine("- ✗ TopUtilityRow introuvable sous SafeRoot — abort (Gate 3.2 requis)");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            log.AppendLine($"SafeRoot : `{GetPath(safeRoot)}`");
            log.AppendLine($"TopUtilityRow : `{GetPath(row)}` (parent SafeRoot ✓)");
            log.AppendLine();

            Sprite shopSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopSpritePath);
            Sprite newsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(NewsSpritePath);
            log.AppendLine("## Proposition sprites Shop / News (DRY)");
            log.AppendLine(
                shopSprite != null
                    ? $"- Shop → `{ShopSpritePath}` ({shopSprite.rect.width:0}×{shopSprite.rect.height:0}) ✓ downscale÷2 OK"
                    : $"- Shop → `{ShopSpritePath}` ABSENT — label seul + TODO Dharu");

            bool newsIsFrame = newsSprite != null && IsNonSquareGlyph(newsSprite);
            if (newsSprite == null)
            {
                log.AppendLine($"- News → `{NewsSpritePath}` ABSENT — label seul + TODO Dharu");
            }
            else if (newsIsFrame)
            {
                log.AppendLine(
                    $"- News → `{NewsSpritePath}` ({newsSprite.rect.width:0}×{newsSprite.rect.height:0}) = CADRE, " +
                    "écrasé en 64×64 → VERDICT : label seul Caption TextMuted, pas d'icône (TODO Dharu)");
                newsSprite = null; // ne pas assigner le cadre
            }
            else
            {
                log.AppendLine(
                    $"- News → `{NewsSpritePath}` ({newsSprite.rect.width:0}×{newsSprite.rect.height:0}) ✓");
            }
            log.AppendLine();

            log.AppendLine("## Icônes transport");
            Sprite tp = AssetDatabase.LoadAssetAtPath<Sprite>(TransportIconGenerator.PrevPath);
            Sprite ty = AssetDatabase.LoadAssetAtPath<Sprite>(TransportIconGenerator.PlayPath);
            Sprite ta = AssetDatabase.LoadAssetAtPath<Sprite>(TransportIconGenerator.PausePath);
            Sprite tn = AssetDatabase.LoadAssetAtPath<Sprite>(TransportIconGenerator.NextPath);
            bool transportOk = tp != null && ty != null && ta != null && tn != null;
            if (transportOk && !apply)
            {
                conforme++;
                log.AppendLine("- icon_prev/play/pause/next présents ✓ — TODO Dharu pixel-art");
            }
            else if (!apply)
            {
                todo++;
                log.AppendLine(
                    "- [DRY] Générer/câbler icon_prev/play/pause/next (Art/UI/Generated) — À FAIRE");
            }
            else
            {
                TransportIconGenerator.GenerateAll();
                TransportIconGenerator.EnsureLoaded(out _, out _, out _, out _);
                conforme++;
                log.AppendLine(
                    "- TransportIconGenerator : 4 PNG générés ✓ — TODO Dharu pixel-art natives");
            }
            log.AppendLine();

            log.AppendLine("## Structure bande (HLG + fond B jointif)");
            EnsureBandChrome(row, header, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            log.AppendLine("## Clusters Shop / News + LofiPlayerBar");
            EnsureBandContent(
                row, shopSprite, newsSprite, scene, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            log.AppendLine("## Purge MusicPlayerSlot (BottomZone)");
            EnsureMusicSlotRemoved(scene, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            log.AppendLine("## PageMusique");
            log.AppendLine("- PageMusique / MusicPlayerUI : recyclage Gate 4.a (voir MissionsPageBuilder) ✓");
            conforme++;
            log.AppendLine();

            if (apply)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                log.AppendLine("Scène marquée dirty — Ctrl+S.");
            }

            AppendCounter(log, todo, conforme, failed);
            Debug.Log(log.ToString());

            if (apply && failed == 0 && todo == 0)
                Debug.Log($"[HomeTopBandBuilder] APPLIQUER OK — CONFORMES={conforme}.");
            else if (apply && failed > 0)
                Debug.LogError($"[HomeTopBandBuilder] APPLIQUER INCOMPLET — échecs={failed}.");
            else if (!apply && todo == 0 && failed == 0)
                Debug.Log($"[HomeTopBandBuilder] DRY RUN — convergence OK (CONFORMES={conforme}).");
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
        // BANDE CHROME
        // ═══════════════════════════════════════════

        private static void EnsureBandChrome(
            RectTransform row,
            RectTransform header,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            Image backdrop = row.Find(BandBackdropName)?.GetComponent<Image>();
            Image hairline = row.Find(BandHairlineName)?.GetComponent<Image>();
            TopUtilityHeaderClearance clearance = row.GetComponent<TopUtilityHeaderClearance>();

            float bandH = BarHeight + UiTheme.Space4 * 2f;
            float headerInset = TopUtilityHeaderClearance.ResolveHeaderBottomInset(header);
            float expectedPosY = -headerInset; // gap 0 — jointure Header

            bool hlgOk = hlg != null
                         && hlg.padding.left == Mathf.RoundToInt(UiTheme.Space4)
                         && Mathf.Approximately(hlg.spacing, UiTheme.Space3)
                         && hlg.childAlignment == TextAnchor.MiddleCenter;
            bool backdropOk = IsBackdropStretched(backdrop);
            bool hairlineOk = IsHairlineOk(hairline);
            bool heightOk = Mathf.Abs(row.sizeDelta.y - bandH) <= 1f;
            bool gapOk = clearance != null
                         && Mathf.Abs(GetClearanceGap(clearance)) < 0.01f
                         && Mathf.Abs(row.anchoredPosition.y - expectedPosY) <= 1f;

            if (hlgOk && backdropOk && hairlineOk && heightOk && gapOk)
            {
                conforme++;
                log.AppendLine(
                    $"- Bande chrome jointive (posY={expectedPosY:0.#}, h={bandH:0.#}, backdrop stretch, hairline BorderStrong) ✓");
                LogArtSeamCheck(row, headerInset, bandH, log);
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    $"- [DRY] FIX chrome : backdrop stretch + hairline BorderStrong + gap 0 (posY={expectedPosY:0.#}, h={bandH:0.#}) — À FAIRE");
                if (backdrop != null && !backdropOk)
                    log.AppendLine("- [DRY] BandBackdrop actuellement collapsed (size 0) — bug documenté");
                return;
            }

            Undo.RecordObject(row, UndoLabel);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, bandH);
            row.anchoredPosition = new Vector2(0f, expectedPosY);
            EditorUtility.SetDirty(row);

            if (clearance == null)
                clearance = Undo.AddComponent<TopUtilityHeaderClearance>(row.gameObject);
            SerializedObject clearSo = new SerializedObject(clearance);
            clearSo.FindProperty("header").objectReferenceValue = header;
            clearSo.FindProperty("gap").floatValue = 0f; // jointure Header — plus de trou 12 px
            clearSo.ApplyModifiedPropertiesWithoutUndo();
            clearance.BindHeader(header);
            EditorUtility.SetDirty(clearance);

            if (hlg == null)
                hlg = Undo.AddComponent<HorizontalLayoutGroup>(row.gameObject);
            Undo.RecordObject(hlg, UndoLabel);
            int pad = Mathf.RoundToInt(UiTheme.Space4);
            hlg.padding = new RectOffset(pad, pad, pad, pad);
            hlg.spacing = UiTheme.Space3;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            EditorUtility.SetDirty(hlg);

            backdrop = EnsureChildImage(row, BandBackdropName, UiTheme.BgPanel, raycast: false);
            ForceBackdropStretch(backdrop.rectTransform);
            backdrop.rectTransform.SetSiblingIndex(0);

            hairline = EnsureChildImage(row, BandHairlineName, UiTheme.BorderStrong, raycast: false);
            ForceHairline(hairline.rectTransform);
            hairline.rectTransform.SetSiblingIndex(1);

            // Re-forcer après rebuild layout (HLG ne doit plus toucher ignoreLayout).
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            ForceBackdropStretch(backdrop.rectTransform);
            ForceHairline(hairline.rectTransform);
            clearance.BindHeader(header);

            conforme++;
            log.AppendLine(
                $"- Bande chrome jointive appliquée (posY={row.anchoredPosition.y:0.#}, h={bandH:0.#}) ✓ → conforme");
            LogArtSeamCheck(row, headerInset, bandH, log);
        }

        private static void LogArtSeamCheck(
            RectTransform row,
            float headerInset,
            float bandH,
            StringBuilder log)
        {
            // Bande jointive au header : masque [headerInset .. headerInset+bandH] depuis le haut SafeRoot.
            log.AppendLine(
                $"- Vérif art : bande couvre y=[-{headerInset:0.#} .. -{headerInset + bandH:0.#}] (SafeRoot). " +
                "Si liseré base.png encore visible sous la bande → signaler mesure pour clamp framing.");
        }

        private static float GetClearanceGap(TopUtilityHeaderClearance clearance)
        {
            SerializedObject so = new SerializedObject(clearance);
            return so.FindProperty("gap").floatValue;
        }

        private static bool IsBackdropStretched(Image backdrop)
        {
            if (backdrop == null)
                return false;
            if (!ColorsApprox(backdrop.color, UiTheme.BgPanel))
                return false;
            RectTransform rt = backdrop.rectTransform;
            LayoutElement le = backdrop.GetComponent<LayoutElement>();
            return le != null && le.ignoreLayout
                   && Mathf.Approximately(rt.anchorMin.x, 0f)
                   && Mathf.Approximately(rt.anchorMin.y, 0f)
                   && Mathf.Approximately(rt.anchorMax.x, 1f)
                   && Mathf.Approximately(rt.anchorMax.y, 1f)
                   && Mathf.Approximately(rt.offsetMin.x, 0f)
                   && Mathf.Approximately(rt.offsetMin.y, 0f)
                   && Mathf.Approximately(rt.offsetMax.x, 0f)
                   && Mathf.Approximately(rt.offsetMax.y, 0f);
        }

        private static bool IsHairlineOk(Image hairline)
        {
            if (hairline == null)
                return false;
            if (!ColorsApprox(hairline.color, UiTheme.BorderStrong))
                return false;
            RectTransform rt = hairline.rectTransform;
            LayoutElement le = hairline.GetComponent<LayoutElement>();
            return le != null && le.ignoreLayout
                   && Mathf.Approximately(rt.anchorMin.y, 0f)
                   && Mathf.Approximately(rt.anchorMax.y, 0f)
                   && Mathf.Abs(rt.sizeDelta.y - UiTheme.BorderThin) <= 0.1f;
        }

        private static void ForceBackdropStretch(RectTransform bdRt)
        {
            if (bdRt == null)
                return;
            Undo.RecordObject(bdRt, UndoLabel);
            IgnoreLayout(bdRt);
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.pivot = new Vector2(0.5f, 0.5f);
            bdRt.anchoredPosition = Vector2.zero;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            bdRt.sizeDelta = Vector2.zero;
            EditorUtility.SetDirty(bdRt);
        }

        private static void ForceHairline(RectTransform hairRt)
        {
            if (hairRt == null)
                return;
            Undo.RecordObject(hairRt, UndoLabel);
            IgnoreLayout(hairRt);
            hairRt.anchorMin = new Vector2(0f, 0f);
            hairRt.anchorMax = new Vector2(1f, 0f);
            hairRt.pivot = new Vector2(0.5f, 0f);
            hairRt.anchoredPosition = Vector2.zero;
            hairRt.sizeDelta = new Vector2(0f, UiTheme.BorderThin);
            EditorUtility.SetDirty(hairRt);
            Image img = hairRt.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, UndoLabel);
                img.color = UiTheme.BorderStrong;
                EditorUtility.SetDirty(img);
            }
        }

        // ═══════════════════════════════════════════
        // CONTENU
        // ═══════════════════════════════════════════

        private static void EnsureBandContent(
            RectTransform row,
            Sprite shopSprite,
            Sprite newsSprite,
            Scene scene,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            RectTransform shopCluster = FindDirect(row, ShopClusterName);
            RectTransform newsCluster = FindDirect(row, NewsClusterName);
            RectTransform lofi = FindDirect(row, LofiBarName);
            LofiPlayerBarUI barUi = lofi != null ? lofi.GetComponent<LofiPlayerBarUI>() : null;

            bool ok = shopCluster != null
                      && newsCluster != null
                      && barUi != null
                      && FindInChildren(shopCluster, BtnMagasinName) != null
                      && FindInChildren(newsCluster, BtnNewsName) != null;

            if (ok)
            {
                // Sync sprites + transport même si structure déjà là (fix 3.3).
                if (apply)
                {
                    ApplyIconSprite(FindInChildren(shopCluster, BtnMagasinName), shopSprite, "Shop", log);
                    ApplyIconSprite(FindInChildren(newsCluster, BtnNewsName), newsSprite, "News", log);
                    WireTransportIcons(lofi, log);
                }
                else
                {
                    log.AppendLine("- [DRY] Contenu déjà là — APPLY resync sprites + icônes transport générées");
                }

                conforme++;
                log.AppendLine("- Contenu Shop · Lofi · News conforme ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Créer/aligner ShopCluster · LofiPlayerBar · NewsCluster — À FAIRE");
                return;
            }

            // Récupérer boutons existants (absolus) avant reparent.
            HubButtonUI shopBtn = FindButtonAnywhere(row, BtnMagasinName);
            HubButtonUI newsBtn = FindButtonAnywhere(row, BtnNewsName);
            TextMeshProUGUI shopCap = FindTmpAnywhere(row, MagasinCaptionName);
            TextMeshProUGUI newsCap = FindTmpAnywhere(row, NewsCaptionName);

            shopCluster = EnsureCluster(row, ShopClusterName);
            newsCluster = EnsureCluster(row, NewsClusterName);

            if (shopBtn == null)
            {
                shopBtn = UiKitFactory.CreateIconButton(shopCluster, BtnMagasinName, IconSize, locked: true);
            }
            else if (shopBtn.transform.parent != shopCluster)
            {
                Undo.SetTransformParent(shopBtn.transform, shopCluster, UndoLabel);
            }

            if (newsBtn == null)
            {
                newsBtn = UiKitFactory.CreateIconButton(newsCluster, BtnNewsName, IconSize, locked: true);
            }
            else if (newsBtn.transform.parent != newsCluster)
            {
                Undo.SetTransformParent(newsBtn.transform, newsCluster, UndoLabel);
            }

            SizeIconButton(shopBtn, IconSize);
            SizeIconButton(newsBtn, IconSize);
            ApplyLockedVisual(shopBtn);
            ApplyLockedVisual(newsBtn);

            EnsureCaptionUnder(shopCluster, shopCap, MagasinCaptionName, "Shop");
            EnsureCaptionUnder(newsCluster, newsCap, NewsCaptionName, "News");

            ApplyIconSprite(shopBtn, shopSprite, "Shop", log);
            ApplyIconSprite(newsBtn, newsSprite, "News", log);

            lofi = EnsureLofiPlayerBar(row, scene, log);
            barUi = lofi != null ? lofi.GetComponent<LofiPlayerBarUI>() : null;

            // Ordre siblings : backdrop, hairline, shop, lofi, news.
            Transform bd = row.Find(BandBackdropName);
            Transform hair = row.Find(BandHairlineName);
            int idx = 0;
            if (bd != null)
                bd.SetSiblingIndex(idx++);
            if (hair != null)
                hair.SetSiblingIndex(idx++);
            shopCluster.SetSiblingIndex(idx++);
            if (lofi != null)
                lofi.SetSiblingIndex(idx++);
            newsCluster.SetSiblingIndex(idx++);

            // PageAccueilUI refs Shop/News (SerializedObject).
            WirePageAccueilIcons(scene, shopBtn, newsBtn, log);

            if (barUi != null && shopBtn != null && newsBtn != null)
            {
                conforme++;
                log.AppendLine("- Contenu bande créé/câblé ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- Contenu bande — ÉCHEC ✗");
            }
        }

        private static RectTransform EnsureCluster(RectTransform row, string name)
        {
            RectTransform cluster = FindDirect(row, name);
            if (cluster == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(row, false);
                cluster = (RectTransform)go.transform;
            }

            VerticalLayoutGroup vlg = cluster.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = Undo.AddComponent<VerticalLayoutGroup>(cluster.gameObject);
            Undo.RecordObject(vlg, UndoLabel);
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = Mathf.RoundToInt(UiTheme.Space1);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            EditorUtility.SetDirty(vlg);

            LayoutElement le = cluster.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(cluster.gameObject);
            Undo.RecordObject(le, UndoLabel);
            le.minWidth = IconSize;
            le.preferredWidth = IconSize;
            le.flexibleWidth = 0f;
            le.minHeight = IconSize + UiTheme.Space1 + UiTheme.Space5;
            le.preferredHeight = le.minHeight;
            EditorUtility.SetDirty(le);

            return cluster;
        }

        private static void SizeIconButton(HubButtonUI btn, float size)
        {
            if (btn == null)
                return;

            RectTransform rt = btn.transform as RectTransform;
            Undo.RecordObject(rt, UndoLabel);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
            EditorUtility.SetDirty(rt);

            SerializedObject so = new SerializedObject(btn);
            so.FindProperty("overrideHeight").floatValue = size;
            so.FindProperty("locked").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            btn.ApplyStyle();

            LayoutElement le = btn.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(btn.gameObject);
            Undo.RecordObject(le, UndoLabel);
            le.minWidth = size;
            le.preferredWidth = size;
            le.minHeight = size;
            le.preferredHeight = size;
            le.flexibleWidth = 0f;
            EditorUtility.SetDirty(le);

            Transform labelTx = btn.transform.Find("Label");
            if (labelTx != null)
                labelTx.gameObject.SetActive(false);
            Transform subTx = btn.transform.Find("SubLabel");
            if (subTx != null)
                subTx.gameObject.SetActive(false);
        }

        private static void ApplyLockedVisual(HubButtonUI btn)
        {
            if (btn == null)
                return;
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = Undo.AddComponent<CanvasGroup>(btn.gameObject);
            Undo.RecordObject(cg, UndoLabel);
            cg.alpha = 0.55f;
            cg.interactable = false;
            cg.blocksRaycasts = true;
            EditorUtility.SetDirty(cg);

            Button b = btn.GetComponent<Button>();
            if (b != null)
            {
                Undo.RecordObject(b, UndoLabel);
                b.interactable = false;
                EditorUtility.SetDirty(b);
            }
        }

        private static void EnsureCaptionUnder(
            RectTransform cluster,
            TextMeshProUGUI existing,
            string captionName,
            string text)
        {
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                if (go.transform.parent != cluster)
                    Undo.SetTransformParent(go.transform, cluster, UndoLabel);
                go.name = captionName;
            }
            else
            {
                Transform tx = cluster.Find(captionName);
                if (tx != null)
                {
                    go = tx.gameObject;
                }
                else
                {
                    go = new GameObject(captionName, typeof(RectTransform), typeof(TextMeshProUGUI));
                    Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                    go.transform.SetParent(cluster, false);
                }
            }

            RectTransform rt = (RectTransform)go.transform;
            Undo.RecordObject(rt, UndoLabel);
            rt.sizeDelta = new Vector2(IconSize + UiTheme.Space2, UiTheme.Space5);

            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = UiTheme.Space5;
            le.preferredHeight = UiTheme.Space5;
            le.flexibleWidth = 1f;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(tmp, UndoLabel);
            tmp.text = text;
            tmp.fontSize = UiTypography.Caption;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UiTheme.TextMuted;
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);
        }

        private static void ApplyIconSprite(
            HubButtonUI btn,
            Sprite sprite,
            string label,
            StringBuilder log)
        {
            if (btn == null)
                return;

            Transform iconTx = btn.transform.Find("Icon");
            if (iconTx == null)
            {
                log.AppendLine($"- {label} : slot Icon absent ✗");
                return;
            }

            Image img = iconTx.GetComponent<Image>();
            if (img == null)
                return;

            Undo.RecordObject(img, UndoLabel);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = new Color(1f, 1f, 1f, 0.9f);
                img.preserveAspect = true;
                log.AppendLine($"- {label} sprite assigné ✓");
            }
            else
            {
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0f);
                log.AppendLine($"- {label} : pas de sprite — label seul (TODO Dharu)");
            }

            EditorUtility.SetDirty(img);
        }

        private static void ApplyIconSprite(
            Transform btnTx,
            Sprite sprite,
            string label,
            StringBuilder log)
        {
            if (btnTx == null)
                return;
            ApplyIconSprite(btnTx.GetComponent<HubButtonUI>(), sprite, label, log);
        }

        // ═══════════════════════════════════════════
        // LOFI PLAYER BAR
        // ═══════════════════════════════════════════

        private static RectTransform EnsureLofiPlayerBar(
            RectTransform row,
            Scene scene,
            StringBuilder log)
        {
            RectTransform barRt = FindDirect(row, LofiBarName);
            GameObject barGo;
            if (barRt == null)
            {
                barGo = new GameObject(
                    LofiBarName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                Undo.RegisterCreatedObjectUndo(barGo, UndoLabel);
                barGo.transform.SetParent(row, false);
                barRt = (RectTransform)barGo.transform;
            }
            else
            {
                barGo = barRt.gameObject;
            }

            LayoutElement rootLe = barGo.GetComponent<LayoutElement>();
            if (rootLe == null)
                rootLe = Undo.AddComponent<LayoutElement>(barGo);
            Undo.RecordObject(rootLe, UndoLabel);
            rootLe.minHeight = BarHeight;
            rootLe.preferredHeight = BarHeight;
            rootLe.flexibleWidth = 1f;
            rootLe.minWidth = 200f;
            EditorUtility.SetDirty(rootLe);

            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteL = RoundedRectSpriteGenerator.LoadSpriteL();

            PanelSurface surface = barGo.GetComponent<PanelSurface>();
            if (surface == null)
                surface = Undo.AddComponent<PanelSurface>(barGo);
            SerializedObject surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("variant").enumValueIndex = (int)PanelSurface.SurfaceVariant.Pill;
            surfaceSo.FindProperty("borderStyle").enumValueIndex =
                (int)PanelSurface.SurfaceBorder.Subtle;
            surfaceSo.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            surfaceSo.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            surfaceSo.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            surfaceSo.FindProperty("blocksRaycasts").boolValue = true;
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();
            surface.ApplyStyle();
            Transform fillTx = barGo.transform.Find("Fill");
            if (fillTx != null)
            {
                LayoutElement fillLe = fillTx.GetComponent<LayoutElement>();
                if (fillLe == null)
                    fillLe = Undo.AddComponent<LayoutElement>(fillTx.gameObject);
                fillLe.ignoreLayout = true;
            }

            // Contenu interne.
            RectTransform content = EnsureNamedRt(barRt, "ContentRow");
            StretchWithBottomPad(content, ProgressH + 2f);
            HorizontalLayoutGroup contentHlg = content.GetComponent<HorizontalLayoutGroup>();
            if (contentHlg == null)
                contentHlg = Undo.AddComponent<HorizontalLayoutGroup>(content.gameObject);
            int pad = Mathf.RoundToInt(UiTheme.Space3);
            contentHlg.padding = new RectOffset(pad, pad, pad / 2, pad / 2);
            contentHlg.spacing = UiTheme.Space2;
            contentHlg.childAlignment = TextAnchor.MiddleLeft;
            contentHlg.childControlWidth = true;
            contentHlg.childControlHeight = true;
            contentHlg.childForceExpandWidth = false;
            contentHlg.childForceExpandHeight = false;

            // ArtworkSlot réservé (désactivé).
            RectTransform artRt = EnsureNamedRt(content, "ArtworkSlot");
            artRt.sizeDelta = new Vector2(ArtworkSlotSize, ArtworkSlotSize);
            LayoutElement artLe = artRt.GetComponent<LayoutElement>();
            if (artLe == null)
                artLe = Undo.AddComponent<LayoutElement>(artRt.gameObject);
            artLe.minWidth = ArtworkSlotSize;
            artLe.preferredWidth = ArtworkSlotSize;
            artLe.minHeight = ArtworkSlotSize;
            artLe.preferredHeight = ArtworkSlotSize;
            artLe.flexibleWidth = 0f;
            if (artRt.GetComponent<Image>() == null)
            {
                Image artImg = Undo.AddComponent<Image>(artRt.gameObject);
                artImg.color = new Color(1f, 1f, 1f, 0.08f);
                artImg.raycastTarget = false;
            }

            artRt.gameObject.SetActive(false);

            // Zone titre.
            RectTransform titleZone = EnsureNamedRt(content, "TitleZone");
            LayoutElement titleLe = titleZone.GetComponent<LayoutElement>();
            if (titleLe == null)
                titleLe = Undo.AddComponent<LayoutElement>(titleZone.gameObject);
            titleLe.flexibleWidth = 1f;
            titleLe.minWidth = 80f;
            titleLe.minHeight = 64f;
            titleLe.preferredHeight = 72f;

            VerticalLayoutGroup titleVlg = titleZone.GetComponent<VerticalLayoutGroup>();
            if (titleVlg == null)
                titleVlg = Undo.AddComponent<VerticalLayoutGroup>(titleZone.gameObject);
            titleVlg.spacing = 2f;
            titleVlg.childAlignment = TextAnchor.MiddleLeft;
            titleVlg.childControlWidth = true;
            titleVlg.childControlHeight = true;
            titleVlg.childForceExpandWidth = true;
            titleVlg.childForceExpandHeight = false;
            titleVlg.padding = new RectOffset(0, 0, 0, 0);

            RectTransform viewport = EnsureNamedRt(titleZone, "TrackNameViewport");
            LayoutElement vpLe = viewport.GetComponent<LayoutElement>();
            if (vpLe == null)
                vpLe = Undo.AddComponent<LayoutElement>(viewport.gameObject);
            vpLe.minHeight = 34f;
            vpLe.preferredHeight = 36f;
            vpLe.flexibleWidth = 1f;
            if (viewport.GetComponent<RectMask2D>() == null)
                Undo.AddComponent<RectMask2D>(viewport.gameObject);

            TextMeshProUGUI trackTmp = EnsureTmp(viewport, "TrackName", "—", UiTypography.Label,
                UiTheme.TextPrimary, TextAlignmentOptions.Left);
            RectTransform trackRt = trackTmp.rectTransform;
            trackRt.anchorMin = new Vector2(0f, 0f);
            trackRt.anchorMax = new Vector2(0f, 1f);
            trackRt.pivot = new Vector2(0f, 0.5f);
            trackRt.anchoredPosition = Vector2.zero;
            trackRt.sizeDelta = new Vector2(800f, 0f);
            trackTmp.enableWordWrapping = false;
            trackTmp.overflowMode = TextOverflowModes.Overflow;

            TextMeshProUGUI subTmp = EnsureTmp(titleZone, "Subtitle", "Lofi du train",
                UiTypography.Caption, UiTheme.TextMuted, TextAlignmentOptions.Left);
            LayoutElement subLe = subTmp.GetComponent<LayoutElement>();
            if (subLe == null)
                subLe = Undo.AddComponent<LayoutElement>(subTmp.gameObject);
            subLe.minHeight = 24f;
            subLe.preferredHeight = 26f;

            // Contrôles.
            RectTransform controls = EnsureNamedRt(content, "Controls");
            LayoutElement ctrlLe = controls.GetComponent<LayoutElement>();
            if (ctrlLe == null)
                ctrlLe = Undo.AddComponent<LayoutElement>(controls.gameObject);
            float controlsW = UiTheme.TouchTargetMin * 3f;
            ctrlLe.minWidth = controlsW;
            ctrlLe.preferredWidth = controlsW;
            ctrlLe.minHeight = UiTheme.TouchTargetMin;
            ctrlLe.preferredHeight = UiTheme.TouchTargetMin;
            ctrlLe.flexibleWidth = 0f;

            HorizontalLayoutGroup ctrlHlg = controls.GetComponent<HorizontalLayoutGroup>();
            if (ctrlHlg == null)
                ctrlHlg = Undo.AddComponent<HorizontalLayoutGroup>(controls.gameObject);
            ctrlHlg.spacing = 0f;
            ctrlHlg.childAlignment = TextAnchor.MiddleCenter;
            ctrlHlg.childControlWidth = true;
            ctrlHlg.childControlHeight = true;
            ctrlHlg.childForceExpandWidth = false;
            ctrlHlg.childForceExpandHeight = false;
            ctrlHlg.padding = new RectOffset(0, 0, 0, 0);

            Sprite prevSprite;
            Sprite nextSprite;
            Sprite playSprite;
            Sprite pauseSprite;
            TransportIconGenerator.EnsureLoaded(
                out prevSprite, out playSprite, out pauseSprite, out nextSprite);

            Button btnPrev = EnsureControlButton(controls, "BtnPrev", prevSprite, Accent: false);
            Button btnPlay = EnsureControlButton(controls, "BtnPlayPause", playSprite, Accent: true);
            Button btnNext = EnsureControlButton(controls, "BtnNext", nextSprite, Accent: false);

            // Progression.
            Image trackImg = EnsureChildImage(barRt, "ProgressTrack", UiTheme.BorderSubtle, raycast: false);
            RectTransform trackBarRt = trackImg.rectTransform;
            trackBarRt.anchorMin = new Vector2(0f, 0f);
            trackBarRt.anchorMax = new Vector2(1f, 0f);
            trackBarRt.pivot = new Vector2(0.5f, 0f);
            trackBarRt.anchoredPosition = Vector2.zero;
            trackBarRt.sizeDelta = new Vector2(0f, ProgressH);
            IgnoreLayout(trackBarRt);

            Image fillImg = EnsureChildImage(trackBarRt, "ProgressFill", UiTheme.AccentAmber, raycast: false);
            RectTransform fillRt = fillImg.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            // Ordre content : art, title, controls.
            artRt.SetSiblingIndex(0);
            titleZone.SetSiblingIndex(1);
            controls.SetSiblingIndex(2);

            LofiPlayerBarUI ui = barGo.GetComponent<LofiPlayerBarUI>();
            if (ui == null)
                ui = Undo.AddComponent<LofiPlayerBarUI>(barGo);

            SerializedObject uiSo = new SerializedObject(ui);
            uiSo.FindProperty("trackNameText").objectReferenceValue = trackTmp;
            uiSo.FindProperty("trackNameRt").objectReferenceValue = trackRt;
            uiSo.FindProperty("trackNameViewport").objectReferenceValue = viewport;
            uiSo.FindProperty("subtitleText").objectReferenceValue = subTmp;
            uiSo.FindProperty("btnPrevious").objectReferenceValue = btnPrev;
            uiSo.FindProperty("btnPlayPause").objectReferenceValue = btnPlay;
            uiSo.FindProperty("btnNext").objectReferenceValue = btnNext;
            uiSo.FindProperty("btnPlayPauseImage").objectReferenceValue =
                btnPlay != null
                    ? (btnPlay.transform.Find("Icon")?.GetComponent<Image>()
                       ?? btnPlay.GetComponent<Image>())
                    : null;
            uiSo.FindProperty("iconPlay").objectReferenceValue = playSprite;
            uiSo.FindProperty("iconPause").objectReferenceValue = pauseSprite != null ? pauseSprite : playSprite;
            uiSo.FindProperty("progressTrack").objectReferenceValue = trackImg;
            uiSo.FindProperty("progressFill").objectReferenceValue = fillImg;
            uiSo.FindProperty("artworkSlot").objectReferenceValue = artRt.gameObject;
            uiSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            log.AppendLine(
                playSprite != null && pauseSprite != null && prevSprite != null && nextSprite != null
                    ? "- LofiPlayerBar : icônes transport générées (Art/UI/Generated) ✓ — TODO Dharu pixel-art"
                    : "- LofiPlayerBar : icônes transport manquantes ✗");

            return barRt;
        }

        /// <summary> Resync icônes transport sur une barre déjà créée. </summary>
        private static void WireTransportIcons(RectTransform lofi, StringBuilder log)
        {
            if (lofi == null)
                return;

            TransportIconGenerator.EnsureLoaded(
                out Sprite prev, out Sprite play, out Sprite pause, out Sprite next);

            Transform controls = lofi.Find("ContentRow/Controls");
            if (controls == null)
                controls = FindDeep(lofi, "Controls");

            if (controls != null)
            {
                SetControlIcon(controls.Find("BtnPrev"), prev, Accent: false);
                SetControlIcon(controls.Find("BtnPlayPause"), play, Accent: true);
                SetControlIcon(controls.Find("BtnNext"), next, Accent: false);
            }

            LofiPlayerBarUI ui = lofi.GetComponent<LofiPlayerBarUI>();
            if (ui != null)
            {
                SerializedObject uiSo = new SerializedObject(ui);
                uiSo.FindProperty("iconPlay").objectReferenceValue = play;
                uiSo.FindProperty("iconPause").objectReferenceValue = pause;
                Transform playBtn = controls != null ? controls.Find("BtnPlayPause") : null;
                Image playIcon = playBtn != null
                    ? playBtn.Find("Icon")?.GetComponent<Image>()
                    : null;
                if (playIcon != null)
                    uiSo.FindProperty("btnPlayPauseImage").objectReferenceValue = playIcon;
                uiSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ui);
            }

            log.AppendLine(
                "- Transport icons câblés (prev/play/pause/next générés) ✓ — TODO Dharu pixel-art");
        }

        private static void SetControlIcon(Transform btnTx, Sprite sprite, bool Accent)
        {
            if (btnTx == null || sprite == null)
                return;
            Transform iconTx = btnTx.Find("Icon");
            Image iconImg = iconTx != null ? iconTx.GetComponent<Image>() : null;
            if (iconImg == null)
                return;
            Undo.RecordObject(iconImg, UndoLabel);
            iconImg.sprite = sprite;
            iconImg.preserveAspect = true;
            iconImg.color = Accent ? UiTheme.AccentAmber : UiTheme.TextPrimary;
            EditorUtility.SetDirty(iconImg);
        }

        private static bool IsNonSquareGlyph(Sprite sprite)
        {
            if (sprite == null)
                return true;
            float w = sprite.rect.width;
            float h = sprite.rect.height;
            if (h < 1f)
                return true;
            float ratio = w / h;
            // Cadre / bandeau (ex. 160×48 ≈ 3.33) ≠ glyphe carré.
            return ratio > 1.35f || ratio < 0.75f;
        }

        // ResolveTransportSprites (PageMusique built-in) — retiré Gate 3.3-fix.

        private static Button EnsureControlButton(
            RectTransform parent,
            string name,
            Sprite sprite,
            bool Accent)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
                if (go.GetComponent<Button>() == null)
                    Undo.AddComponent<Button>(go);
                if (go.GetComponent<Image>() == null)
                    Undo.AddComponent<Image>(go);
            }

            RectTransform rt = (RectTransform)go.transform;
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(go);
            // Tap target ≥ TouchTargetMin ; icône visuelle 64 via padding Image.
            le.minWidth = UiTheme.TouchTargetMin;
            le.preferredWidth = UiTheme.TouchTargetMin;
            le.minHeight = UiTheme.TouchTargetMin;
            le.preferredHeight = UiTheme.TouchTargetMin;
            le.flexibleWidth = 0f;

            Image img = go.GetComponent<Image>();
            Undo.RecordObject(img, UndoLabel);
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = true;
            img.color = Accent ? UiTheme.AccentAmber : UiTheme.TextPrimary;
            // Padding visuel ≈ (96-64)/2 = 16 via simple scale of drawn sprite:
            // on utilise un enfant Icon 64 centré pour respecter le brief.
            EditorUtility.SetDirty(img);

            // Hit = Image racine transparente ; glyph = enfant Icon.
            img.color = new Color(1f, 1f, 1f, 0.001f);
            Transform iconTx = go.transform.Find("Icon");
            GameObject iconGo;
            if (iconTx == null)
            {
                iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(iconGo, UndoLabel);
                iconGo.transform.SetParent(go.transform, false);
            }
            else
            {
                iconGo = iconTx.gameObject;
            }

            RectTransform iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(ControlIconSize, ControlIconSize);
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = sprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = Accent ? UiTheme.AccentAmber : UiTheme.TextPrimary;

            Button btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;

            return btn;
        }

        // ═══════════════════════════════════════════
        // PURGE + WIRING
        // ═══════════════════════════════════════════

        private static void EnsureMusicSlotRemoved(
            Scene scene,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            RectTransform page = FindPageAccueil(scene);
            RectTransform zone = page != null ? FindDirect(page, BottomZoneName) : null;
            RectTransform slot = zone != null ? FindDirect(zone, MusicSlotName) : null;

            if (slot == null)
            {
                conforme++;
                log.AppendLine("- MusicPlayerSlot absent ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine($"- [DRY] SUPPRIMER `{GetPath(slot)}` — À FAIRE");
                return;
            }

            log.AppendLine($"- SUPPRIMER `{GetPath(slot)}`");
            Undo.DestroyObjectImmediate(slot.gameObject);
            conforme++;
            log.AppendLine("- MusicPlayerSlot purgé ✓ → conforme");
        }

        private static void WirePageAccueilIcons(
            Scene scene,
            HubButtonUI shop,
            HubButtonUI news,
            StringBuilder log)
        {
            RectTransform page = FindPageAccueil(scene);
            PageAccueilUI ui = page != null ? page.GetComponent<PageAccueilUI>() : null;
            if (ui == null)
            {
                log.AppendLine("- PageAccueilUI absent — skip wiring Shop/News");
                return;
            }

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("buttonMagasin").objectReferenceValue = shop;
            so.FindProperty("buttonNews").objectReferenceValue = news;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
            log.AppendLine("- PageAccueilUI.buttonMagasin / buttonNews câblés ✓");
        }

        // ═══════════════════════════════════════════
        // HELPERS UI
        // ═══════════════════════════════════════════

        private static Image EnsureChildImage(
            Transform parent,
            string name,
            Color color,
            bool raycast)
        {
            Transform tx = parent.Find(name);
            GameObject go;
            if (tx == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = tx.gameObject;
                if (go.GetComponent<Image>() == null)
                    Undo.AddComponent<Image>(go);
            }

            Image img = go.GetComponent<Image>();
            Undo.RecordObject(img, UndoLabel);
            img.color = color;
            img.raycastTarget = raycast;
            img.sprite = null;
            EditorUtility.SetDirty(img);
            return img;
        }

        private static RectTransform EnsureNamedRt(Transform parent, string name)
        {
            Transform tx = parent.Find(name);
            if (tx != null)
                return tx as RectTransform;

            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static TextMeshProUGUI EnsureTmp(
            Transform parent,
            string name,
            string text,
            float size,
            Color color,
            TextAlignmentOptions align)
        {
            Transform tx = parent.Find(name);
            GameObject go;
            if (tx == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = tx.gameObject;
                if (go.GetComponent<TextMeshProUGUI>() == null)
                    Undo.AddComponent<TextMeshProUGUI>(go);
            }

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(tmp, UndoLabel);
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = false;
            EditorUtility.SetDirty(tmp);
            return tmp;
        }

        private static void StretchWithBottomPad(RectTransform rt, float bottomPad)
        {
            Undo.RecordObject(rt, UndoLabel);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, bottomPad);
            rt.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(rt);
        }

        private static void IgnoreLayout(RectTransform rt)
        {
            LayoutElement le = rt.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(rt.gameObject);
            le.ignoreLayout = true;
        }

        private static bool ColorsApprox(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f
                   && Mathf.Abs(a.g - b.g) < 0.02f
                   && Mathf.Abs(a.b - b.b) < 0.02f
                   && Mathf.Abs(a.a - b.a) < 0.02f;
        }

        // ═══════════════════════════════════════════
        // FINDERS
        // ═══════════════════════════════════════════

        private static RectTransform FindSafeRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform t = FindDeep(root.transform, "SafeRoot");
                if (t != null)
                    return t as RectTransform;
            }

            return null;
        }

        private static RectTransform FindHeader(RectTransform safeRoot)
        {
            return safeRoot != null ? FindDirect(safeRoot, "Header") : null;
        }

        private static RectTransform FindTopUtility(RectTransform safeRoot)
        {
            return safeRoot != null ? FindDirect(safeRoot, TopUtilityName) : null;
        }

        private static RectTransform FindPageAccueil(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform t = FindDeep(root.transform, "PageAccueil");
                if (t != null)
                    return t as RectTransform;
            }

            return null;
        }

        private static Transform FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform t = FindDeep(root.transform, name);
                if (t != null)
                    return t;
            }

            return null;
        }

        private static RectTransform FindDirect(Transform parent, string name)
        {
            if (parent == null)
                return null;
            Transform t = parent.Find(name);
            return t as RectTransform;
        }

        private static Transform FindInChildren(Transform parent, string name)
        {
            if (parent == null)
                return null;
            return parent.Find(name);
        }

        private static Transform FindDeep(Transform root, string name)
        {
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

        private static HubButtonUI FindButtonAnywhere(Transform root, string name)
        {
            Transform t = FindDeep(root, name);
            return t != null ? t.GetComponent<HubButtonUI>() : null;
        }

        private static TextMeshProUGUI FindTmpAnywhere(Transform root, string name)
        {
            Transform t = FindDeep(root, name);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
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
