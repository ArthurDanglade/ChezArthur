#if UNITY_EDITOR
using System.Text;
using ChezArthur.Hub;
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
    /// Gate 2.2 — NavigationBar définitive + HubNavSafeBleed + PageTransitionController.
    /// Harnais v2 : À FAIRE / CONFORMES / ÉCHECS. LOCK 2.1 : ne touche pas le haut.
    /// </summary>
    public static class HubNavBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string UndoLabel = "Hub Nav 2.2";
        private const string SafeRootName = "SafeRoot";
        private const string PageContainerName = "PageContainer";
        private const string NavName = "NavigationBar";
        private const string FooterGuid = "c2c3b242caf01594692cca9c8eaf0562";
        private const float IconSlotSize = 64f;
        private const float BadgeSize = 16f;
        private static readonly Color MotifTint = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Vector3 MotifScale = new Vector3(1.28f, 1.32f, 1f);

        private static readonly string[] IconPaths =
        {
            "Assets/_Project/Sprites/UI/UI - home.png",
            "Assets/_Project/Sprites/UI/UI - team.png",
            "Assets/_Project/Sprites/UI/UI - invocation.png",
            "Assets/_Project/Sprites/UI/UI 0 badge.png"
        };

        private static readonly string[] TabIds = { "accueil", "equipe", "invocation", "missions" };
        private static readonly string[] TabLabels = { "Accueil", "Équipe", "Invocation", "Missions" };
        private static readonly string[] PageNames =
        {
            "PageAccueil", "PageEquipe", "PageInvocation", "PageMissions"
        };

            // "Background" typé legacy uniquement (pas NavBackdrop).
            private static readonly string[] LegacyChildNames =
            {
                "Backgroud", "BtnAccueil", "BtnEquipe", "BtnInvocation", "BtnMusique",
                "IndicatorAccueil", "IndicatorEquipe", "IndicatorInvocation", "IndicatorMusique"
            };

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Refonte Hub/Construire la Navigation (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Construire la Navigation (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Construire la Navigation Hub",
                    "Va rebuild NavigationBar (Gate 2.2) sous SafeRoot.\n" +
                    "LOCK 2.1 : header / SafeBleed haut / ScreenSafeArea intacts.\nContinuer ?",
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
            log.AppendLine($" HubNavBuilder — {mode}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine(" Convergence = À FAIRE : 0");
            log.AppendLine(" LOCK 2.1 : haut d'écran non modifié");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int todo = 0;
            int conforme = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[HubNavBuilder] Aucune scène active chargée.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine();

            RectTransform safeRoot = FindSafeRoot(scene);
            if (safeRoot == null)
            {
                failed++;
                log.AppendLine("- ✗ SafeRoot introuvable — abort");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            // 1. Importers pixel-art icônes
            log.AppendLine("## Importers icônes nav (Point, no compression)");
            EnsureIconImporters(apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 2. SafeRoot conformBottom = false (sans toucher conformTop)
            log.AppendLine("## SafeRoot.conformBottom = false (bas physique)");
            EnsureConformBottom(safeRoot, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 3. PageContainer full-bleed (vérif seule)
            log.AppendLine("## PageContainer full-bleed (vérif)");
            VerifyPageContainer(safeRoot, log, ref conforme, ref failed);
            log.AppendLine();

            // 4. NavigationBar structure
            log.AppendLine("## NavigationBar (structure Gate 2.2)");
            RectTransform nav = EnsureNavigationBar(safeRoot, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 5. Purge legacy
            log.AppendLine("## Purge enfants legacy (Backgroud / Btn* / indicateurs)");
            PurgeLegacyChildren(nav, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 6. Visuels + template + TabsRow
            log.AppendLine("## Visuels + TabTemplate + TabsRow");
            EnsureNavVisualsAndTemplate(nav, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 7. HubNavSafeBleed
            log.AppendLine("## HubNavSafeBleed (miroir bas)");
            EnsureNavSafeBleed(nav, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 8. HubNavBarUI
            log.AppendLine("## HubNavBarUI (data-driven)");
            EnsureHubNavBarUI(nav, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 9. CanvasGroups pages + PageTransitionController
            log.AppendLine("## Pages CanvasGroup + PageTransitionController");
            EnsurePageTransitions(scene, safeRoot, nav, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 10. HubNavigationUI absent = conforme
            log.AppendLine("## HubNavigationUI (legacy)");
            EnsureLegacyNavRemoved(nav, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 11. Ordre sibling
            log.AppendLine("## Ordre SafeRoot (PageContainer → Header → NavigationBar)");
            EnsureSiblingOrder(safeRoot, apply, log, ref todo, ref conforme, ref failed);

            if (apply)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                log.AppendLine();
                log.AppendLine("Scène marquée dirty — pense à sauvegarder (Ctrl+S).");
            }

            AppendCounter(log, todo, conforme, failed);
            Debug.Log(log.ToString());

            if (apply && failed > 0)
            {
                Debug.LogError(
                    $"[HubNavBuilder] APPLIQUER INCOMPLET — {failed} échec(s), " +
                    $"À FAIRE={todo}, CONFORMES={conforme}. Voir log.");
            }
            else if (apply && todo > 0)
            {
                Debug.LogError(
                    $"[HubNavBuilder] APPLIQUER ÉCART — À FAIRE restant = {todo} (attendu 0).");
            }
            else if (apply)
            {
                Debug.Log(
                    $"[HubNavBuilder] APPLIQUER OK — À FAIRE=0, CONFORMES={conforme}, ÉCHECS=0.");
            }
            else if (todo == 0 && failed == 0)
            {
                Debug.Log(
                    $"[HubNavBuilder] DRY RUN — convergence OK (À FAIRE=0, CONFORMES={conforme}).");
            }
        }

        private static void AppendCounter(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine();
            log.AppendLine("## COMPTEUR D'ACTIONS (harnais v2)");
            log.AppendLine($"- À FAIRE : {todo}");
            log.AppendLine($"- CONFORMES : {conforme}");
            log.AppendLine($"- ÉCHECS : {failed}");
            if (todo == 0 && failed == 0)
                log.AppendLine("- Convergence : OUI (À FAIRE = 0)");
            else
                log.AppendLine("- Convergence : NON");
        }

        // ═══════════════════════════════════════════
        // ÉTAPES
        // ═══════════════════════════════════════════

        private static void EnsureIconImporters(
            bool apply, StringBuilder log, ref int todo, ref int conforme, ref int failed)
        {
            bool allOk = true;
            for (int i = 0; i < IconPaths.Length; i++)
            {
                string path = IconPaths[i];
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    failed++;
                    allOk = false;
                    log.AppendLine($"- Icône introuvable : `{path}` ✗");
                    continue;
                }

                bool ok =
                    importer.filterMode == FilterMode.Point
                    && importer.textureCompression == TextureImporterCompression.Uncompressed;

                if (ok)
                {
                    log.AppendLine($"- `{path}` : Point + Uncompressed ✓");
                    continue;
                }

                allOk = false;
                if (!apply)
                {
                    todo++;
                    log.AppendLine(
                        $"- [DRY] Corriger importer `{path}` " +
                        $"(filter={importer.filterMode}, comp={importer.textureCompression}) — À FAIRE");
                    continue;
                }

                Undo.RecordObject(importer, UndoLabel);
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                log.AppendLine($"- Importer corrigé `{path}` ✓ → conforme");
            }

            if (allOk)
            {
                conforme++;
                log.AppendLine("- 4 importers icônes nav — conforme ✓");
            }
            else if (apply)
            {
                bool recheck = true;
                for (int i = 0; i < IconPaths.Length; i++)
                {
                    var importer = AssetImporter.GetAtPath(IconPaths[i]) as TextureImporter;
                    if (importer == null
                        || importer.filterMode != FilterMode.Point
                        || importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        recheck = false;
                        break;
                    }
                }

                if (recheck)
                    conforme++;
                else
                {
                    failed++;
                    log.AppendLine("- Importers icônes — ÉCHEC ✗");
                }
            }
        }

        private static void EnsureConformBottom(
            RectTransform safeRoot,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            SafeAreaFitter fitter = safeRoot.GetComponent<SafeAreaFitter>();
            if (fitter == null)
            {
                failed++;
                log.AppendLine("- SafeAreaFitter absent ✗");
                return;
            }

            // LOCK 2.1 : journaliser ConformTop sans le modifier.
            log.AppendLine(
                fitter.ConformTop
                    ? "- ConformTop = true ⚠ (attendu false depuis Gate 2.1 — non modifié ici)"
                    : "- ConformTop = false ✓ (LOCK 2.1 respecté, non touché)");

            if (!fitter.ConformBottom)
            {
                conforme++;
                log.AppendLine("- SafeAreaFitter.conformBottom = false ✓ (bas physique)");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] SET conformBottom = false — À FAIRE");
                return;
            }

            Undo.RecordObject(fitter, UndoLabel);
            fitter.ConformBottom = false;
            EditorUtility.SetDirty(fitter);

            if (!fitter.ConformBottom)
            {
                conforme++;
                log.AppendLine("- SafeAreaFitter.conformBottom = false ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- conformBottom — ÉCHEC ✗");
            }
        }

        private static void VerifyPageContainer(
            RectTransform safeRoot, StringBuilder log, ref int conforme, ref int failed)
        {
            RectTransform page = FindDirectChild(safeRoot, PageContainerName);
            if (page == null)
            {
                failed++;
                log.AppendLine("- PageContainer introuvable ✗");
                return;
            }

            // Tolérance 0,5 px UI — évite faux échec sur résidus float qui s'affichent "0.00".
            const float eps = 0.5f;
            bool fullBleed =
                Mathf.Abs(page.offsetMin.x) <= eps
                && Mathf.Abs(page.offsetMin.y) <= eps
                && Mathf.Abs(page.offsetMax.x) <= eps
                && Mathf.Abs(page.offsetMax.y) <= eps;

            if (fullBleed)
            {
                conforme++;
                log.AppendLine("- PageContainer full-bleed (offsets ≈ 0) ✓ — aucune modification");
            }
            else
            {
                // Hors scope Gate 2.2 : on signale sans bloquer la convergence (pas d'ÉCHEC).
                log.AppendLine(
                    $"- PageContainer offsets non nuls ({page.offsetMin} / {page.offsetMax}) ⚠ " +
                    "(hors scope nav — non modifié)");
            }
        }

        private static RectTransform EnsureNavigationBar(
            RectTransform safeRoot,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            RectTransform nav = FindDirectChild(safeRoot, NavName);
            if (nav == null)
            {
                if (!apply)
                {
                    todo++;
                    log.AppendLine($"- [DRY] CRÉER `{NavName}` sous SafeRoot — À FAIRE");
                    return null;
                }

                GameObject go = new GameObject(NavName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                nav = go.GetComponent<RectTransform>();
                nav.SetParent(safeRoot, false);
                go.layer = safeRoot.gameObject.layer;
            }

            bool okLayout =
                Mathf.Approximately(nav.anchorMin.x, 0f)
                && Mathf.Approximately(nav.anchorMin.y, 0f)
                && Mathf.Approximately(nav.anchorMax.x, 1f)
                && Mathf.Approximately(nav.anchorMax.y, 0f)
                && Mathf.Approximately(nav.pivot.x, 0.5f)
                && Mathf.Approximately(nav.pivot.y, 0f)
                && Mathf.Approximately(nav.anchoredPosition.x, 0f)
                && Mathf.Approximately(nav.anchoredPosition.y, 0f)
                && nav.sizeDelta.y >= UiTheme.NavHeight - 0.5f;

            RectMask2D mask = nav.GetComponent<RectMask2D>();
            bool hasMask = mask != null;

            if (okLayout && hasMask)
            {
                conforme++;
                log.AppendLine(
                    $"- NavigationBar layout OK (H≥{UiTheme.NavHeight}, pivot bas, RectMask2D) ✓");
                return nav;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    $"- [DRY] NavigationBar layout/mask → pivot bas, H={UiTheme.NavHeight}, RectMask2D — À FAIRE");
                return nav;
            }

            Undo.RecordObject(nav, UndoLabel);
            nav.anchorMin = new Vector2(0f, 0f);
            nav.anchorMax = new Vector2(1f, 0f);
            nav.pivot = new Vector2(0.5f, 0f);
            nav.anchoredPosition = Vector2.zero;
            nav.sizeDelta = new Vector2(0f, UiTheme.NavHeight);
            EditorUtility.SetDirty(nav);

            if (mask == null)
                Undo.AddComponent<RectMask2D>(nav.gameObject);

            conforme++;
            log.AppendLine($"- NavigationBar layout + RectMask2D ✓ → conforme");
            return nav;
        }

        private static void PurgeLegacyChildren(
            RectTransform nav,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (nav == null)
            {
                log.AppendLine("- Nav absente — skip purge");
                return;
            }

            int found = 0;
            for (int i = 0; i < LegacyChildNames.Length; i++)
            {
                if (FindDirectChild(nav, LegacyChildNames[i]) != null)
                    found++;
            }

            // Ancien fond parfois nommé "Background" (sans typo).
            if (FindDirectChild(nav, "Background") != null)
                found++;

            if (found == 0
                && FindDirectChild(nav, "Backgroud") == null
                && FindDirectChild(nav, "BtnAccueil") == null)
            {
                conforme++;
                log.AppendLine("- Aucun enfant legacy Backgroud/Btn* — conforme ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine($"- [DRY] SUPPRIMER enfants legacy (≈{found}) — À FAIRE");
                return;
            }

            for (int i = nav.childCount - 1; i >= 0; i--)
            {
                Transform child = nav.GetChild(i);
                if (child == null)
                    continue;
                if (!IsLegacyNavChild(child.name))
                    continue;
                log.AppendLine($"- SUPPRIMER `{child.name}`");
                Undo.DestroyObjectImmediate(child.gameObject);
            }

            if (FindDirectChild(nav, "Backgroud") == null && FindDirectChild(nav, "BtnAccueil") == null)
            {
                conforme++;
                log.AppendLine("- Purge legacy ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- Purge legacy — ÉCHEC ✗");
            }
        }

        private static bool FindInLegacyList(string name)
        {
            for (int i = 0; i < LegacyChildNames.Length; i++)
            {
                if (LegacyChildNames[i] == name)
                    return true;
            }
            return false;
        }

        private static bool IsLegacyNavChild(string name)
        {
            if (name == "NavBackdrop" || name == "FooterMotif" || name == "TopHairline"
                || name == "TabsRow" || name == "TabTemplate")
                return false;
            if (name.StartsWith("Tab_"))
                return false;
            if (name == "Background")
                return true;
            return FindInLegacyList(name) || name.StartsWith("Btn") || name.StartsWith("Indicator");
        }

        private static void EnsureNavVisualsAndTemplate(
            RectTransform nav,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (nav == null)
            {
                if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] Visuels nav impossibles sans NavigationBar — À FAIRE");
                }
                return;
            }

            Sprite footer = LoadSpriteByGuid(FooterGuid);
            if (footer == null)
            {
                failed++;
                log.AppendLine("- Footer sprite introuvable (UI - New footer.png) ✗");
                return;
            }

            bool hasBackdrop = IsNavBackdropConforme(FindDirectChild(nav, "NavBackdrop"));
            bool hasMotif = IsFooterMotifConforme(FindDirectChild(nav, "FooterMotif"));
            bool hasHair = FindDirectChild(nav, "TopHairline") != null;
            bool hasRow = FindDirectChild(nav, "TabsRow") != null;
            bool hasTemplate = IsTabTemplateConforme(FindDirectChild(nav, "TabTemplate"));

            if (hasBackdrop && hasMotif && !hasHair && hasRow && hasTemplate)
            {
                conforme++;
                log.AppendLine("- NavBackdrop transparent / FooterMotif plein cadre / TabsRow / TabTemplate ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Aligner visuels nav (motif plein cadre, sans TopHairline, tabs dans plateau) — À FAIRE");
                if (hasHair)
                    log.AppendLine("- [DRY] SUPPRIMER TopHairline (séparateur inutile) — À FAIRE");
                if (FindDirectChild(nav, "NavBackdrop") != null && !hasBackdrop)
                    log.AppendLine("- [DRY] NavBackdrop → alpha 0 — À FAIRE");
                if (FindDirectChild(nav, "FooterMotif") != null && !hasMotif)
                    log.AppendLine("- [DRY] FooterMotif → stretch plein + scale cover — À FAIRE");
                return;
            }

            // Fond transparent : décor de page visible ; raycast bloque les clics.
            Color backdropClear = new Color(UiTheme.BgPanel.r, UiTheme.BgPanel.g, UiTheme.BgPanel.b, 0f);
            EnsureChildImage(nav, "NavBackdrop", backdropClear, raycast: true, stretch: true, null);
            EnsureFooterMotif(nav, footer);

            // Séparateur TopHairline : retiré (redondant avec le liseré du sprite).
            Transform hair = FindDirectChild(nav, "TopHairline");
            if (hair != null)
            {
                log.AppendLine("- SUPPRIMER `TopHairline`");
                Undo.DestroyObjectImmediate(hair.gameObject);
            }

            EnsureTabsRow(nav);
            EnsureTabTemplate(nav);

            HubNavBarUI navUi = nav.GetComponent<HubNavBarUI>();
            if (navUi != null)
                navUi.Rebuild();

            HubNavSafeBleed bleed = nav.GetComponent<HubNavSafeBleed>();
            if (bleed != null)
                bleed.Refresh();

            if (IsNavBackdropConforme(FindDirectChild(nav, "NavBackdrop"))
                && IsFooterMotifConforme(FindDirectChild(nav, "FooterMotif"))
                && FindDirectChild(nav, "TopHairline") == null
                && FindDirectChild(nav, "TabsRow") != null
                && IsTabTemplateConforme(FindDirectChild(nav, "TabTemplate")))
            {
                conforme++;
                log.AppendLine("- Visuels nav alignés (motif plein, tabs plateau) ✓ → conforme");
                SetSiblingFirst(nav, "NavBackdrop");
                SetSiblingAfter(nav, "FooterMotif", "NavBackdrop");
                SetSiblingAfter(nav, "TabsRow", "FooterMotif");
                Transform tmpl = FindDirectChild(nav, "TabTemplate");
                if (tmpl != null)
                    tmpl.SetAsLastSibling();
            }
            else
            {
                failed++;
                log.AppendLine("- Visuels nav — ÉCHEC ✗");
            }
        }

        private static void EnsureNavSafeBleed(
            RectTransform nav,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (nav == null)
            {
                log.AppendLine("- Nav absente — HubNavSafeBleed impossible");
                return;
            }

            HubNavSafeBleed bleed = nav.GetComponent<HubNavSafeBleed>();
            RectTransform tabsRow = FindDirectChild(nav, "TabsRow") as RectTransform;
            bool wired = bleed != null && IsSafeBleedWired(bleed, tabsRow);

            if (wired)
            {
                conforme++;
                log.AppendLine("- HubNavSafeBleed présent + TabsRow câblé ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    bleed == null
                        ? "- [DRY] AJOUTER HubNavSafeBleed + câbler TabsRow — À FAIRE"
                        : "- [DRY] Recâbler HubNavSafeBleed.safeBandContent — À FAIRE");
                return;
            }

            if (bleed == null)
                bleed = Undo.AddComponent<HubNavSafeBleed>(nav.gameObject);

            SerializedObject so = new SerializedObject(bleed);
            SerializedProperty prop = so.FindProperty("safeBandContent");
            if (prop == null)
            {
                failed++;
                log.AppendLine("- HubNavSafeBleed.safeBandContent introuvable ✗");
                return;
            }

            prop.arraySize = 1;
            prop.GetArrayElementAtIndex(0).objectReferenceValue = tabsRow;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bleed);
            bleed.Refresh();

            if (IsSafeBleedWired(bleed, tabsRow))
            {
                conforme++;
                log.AppendLine("- HubNavSafeBleed câblé (TabsRow) ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- HubNavSafeBleed câblage — ÉCHEC ✗");
            }
        }

        private static bool IsSafeBleedWired(HubNavSafeBleed bleed, RectTransform tabsRow)
        {
            if (bleed == null || tabsRow == null)
                return false;
            SerializedObject so = new SerializedObject(bleed);
            SerializedProperty prop = so.FindProperty("safeBandContent");
            if (prop == null || !prop.isArray || prop.arraySize < 1)
                return false;
            return prop.GetArrayElementAtIndex(0).objectReferenceValue == tabsRow;
        }

        private static void EnsureHubNavBarUI(
            RectTransform nav,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (nav == null)
                return;

            Sprite[] icons = LoadNavIcons();
            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null)
                {
                    failed++;
                    log.AppendLine($"- Sprite onglet manquant : `{IconPaths[i]}` ✗");
                    return;
                }
            }

            HubNavBarUI ui = nav.GetComponent<HubNavBarUI>();
            RectTransform tabsRow = FindDirectChild(nav, "TabsRow") as RectTransform;
            Transform template = FindDirectChild(nav, "TabTemplate");
            bool wired = ui != null && IsNavBarWired(ui, tabsRow, template, icons);

            if (wired)
            {
                conforme++;
                log.AppendLine("- HubNavBarUI câblé (4 tabs Accueil…Missions) ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] AJOUTER/câbler HubNavBarUI (4 TabDefinition) — À FAIRE");
                return;
            }

            if (ui == null)
                ui = Undo.AddComponent<HubNavBarUI>(nav.gameObject);

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("tabsRow").objectReferenceValue = tabsRow;
            so.FindProperty("tabTemplate").objectReferenceValue =
                template != null ? template.gameObject : null;

            SerializedProperty tabsProp = so.FindProperty("tabs");
            tabsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                SerializedProperty el = tabsProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("id").stringValue = TabIds[i];
                el.FindPropertyRelative("label").stringValue = TabLabels[i];
                el.FindPropertyRelative("icon").objectReferenceValue = icons[i];
                el.FindPropertyRelative("pageIndex").intValue = i;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            // Preview éditeur : génère les onglets une fois
            ui.Rebuild();

            if (IsNavBarWired(ui, tabsRow, template, icons))
            {
                conforme++;
                log.AppendLine("- HubNavBarUI câblé ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- HubNavBarUI câblage — ÉCHEC ✗");
            }
        }

        private static bool IsNavBarWired(
            HubNavBarUI ui,
            RectTransform tabsRow,
            Transform template,
            Sprite[] icons)
        {
            if (ui == null || tabsRow == null || template == null)
                return false;

            SerializedObject so = new SerializedObject(ui);
            if (so.FindProperty("tabsRow").objectReferenceValue != tabsRow)
                return false;
            if (so.FindProperty("tabTemplate").objectReferenceValue as GameObject != template.gameObject)
                return false;

            SerializedProperty tabsProp = so.FindProperty("tabs");
            if (tabsProp == null || tabsProp.arraySize != 4)
                return false;

            for (int i = 0; i < 4; i++)
            {
                SerializedProperty el = tabsProp.GetArrayElementAtIndex(i);
                if (el.FindPropertyRelative("id").stringValue != TabIds[i])
                    return false;
                if (el.FindPropertyRelative("label").stringValue != TabLabels[i])
                    return false;
                if (el.FindPropertyRelative("pageIndex").intValue != i)
                    return false;
                if (el.FindPropertyRelative("icon").objectReferenceValue != icons[i])
                    return false;
            }

            return true;
        }

        private static void EnsurePageTransitions(
            Scene scene,
            RectTransform safeRoot,
            RectTransform nav,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            HubManager hub = Object.FindObjectOfType<HubManager>();
            if (hub == null)
            {
                failed++;
                log.AppendLine("- HubManager introuvable ✗");
                return;
            }

            GameObject[] pages = new GameObject[4];
            CanvasGroup[] groups = new CanvasGroup[4];
            bool pagesOk = true;
            for (int i = 0; i < 4; i++)
            {
                pages[i] = FindPageGo(safeRoot, PageNames[i]);
                if (pages[i] == null)
                {
                    pagesOk = false;
                    log.AppendLine($"- `{PageNames[i]}` introuvable ✗");
                }
            }

            if (!pagesOk)
            {
                failed++;
                return;
            }

            bool groupsReady = true;
            for (int i = 0; i < 4; i++)
            {
                groups[i] = pages[i].GetComponent<CanvasGroup>();
                if (groups[i] == null)
                    groupsReady = false;
            }

            HubNavBarUI navUi = nav != null ? nav.GetComponent<HubNavBarUI>() : null;
            PageTransitionController ptc = safeRoot.GetComponent<PageTransitionController>();
            bool ptcWired = ptc != null && IsPtcWired(ptc, hub, navUi, groups);

            if (groupsReady && ptcWired)
            {
                conforme++;
                log.AppendLine("- CanvasGroup ×4 + PageTransitionController câblé ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] CanvasGroup pages + PageTransitionController — À FAIRE");
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                if (pages[i].GetComponent<CanvasGroup>() == null)
                    Undo.AddComponent<CanvasGroup>(pages[i]);
                groups[i] = pages[i].GetComponent<CanvasGroup>();
                groups[i].alpha = i == 0 ? 1f : 0f;
                groups[i].interactable = i == 0;
                groups[i].blocksRaycasts = i == 0;
                EditorUtility.SetDirty(groups[i]);
            }

            if (ptc == null)
                ptc = Undo.AddComponent<PageTransitionController>(safeRoot.gameObject);

            SerializedObject so = new SerializedObject(ptc);
            so.FindProperty("hubManager").objectReferenceValue = hub;
            so.FindProperty("navBar").objectReferenceValue = navUi;
            SerializedProperty arr = so.FindProperty("pageGroups");
            arr.arraySize = 4;
            for (int i = 0; i < 4; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = groups[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ptc);

            if (IsPtcWired(ptc, hub, navUi, groups))
            {
                conforme++;
                log.AppendLine("- PageTransitionController + CanvasGroups ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- PageTransitionController câblage — ÉCHEC ✗");
            }
        }

        private static bool IsPtcWired(
            PageTransitionController ptc,
            HubManager hub,
            HubNavBarUI navUi,
            CanvasGroup[] groups)
        {
            if (ptc == null || hub == null || navUi == null || groups == null || groups.Length != 4)
                return false;

            SerializedObject so = new SerializedObject(ptc);
            if (so.FindProperty("hubManager").objectReferenceValue != hub)
                return false;
            if (so.FindProperty("navBar").objectReferenceValue != navUi)
                return false;

            SerializedProperty arr = so.FindProperty("pageGroups");
            if (arr == null || arr.arraySize != 4)
                return false;
            for (int i = 0; i < 4; i++)
            {
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue != groups[i])
                    return false;
            }

            return true;
        }

        private static void EnsureLegacyNavRemoved(
            RectTransform nav,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (nav == null)
            {
                conforme++;
                log.AppendLine("- HubNavigationUI absent (nav null) — conforme ✓");
                return;
            }

            Component legacy = null;
            Component[] comps = nav.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null)
                    continue;
                if (comps[i].GetType().Name == "HubNavigationUI")
                {
                    legacy = comps[i];
                    break;
                }
            }

            int missingCount = CountMissingScripts(nav.gameObject);

            if (legacy == null && missingCount == 0)
            {
                conforme++;
                log.AppendLine("- HubNavigationUI absent — conforme ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                if (legacy != null)
                    log.AppendLine("- [DRY] SUPPRIMER composant HubNavigationUI — À FAIRE");
                if (missingCount > 0)
                    log.AppendLine($"- [DRY] SUPPRIMER {missingCount} Missing Script(s) sur NavigationBar — À FAIRE");
                return;
            }

            if (legacy != null)
                Undo.DestroyObjectImmediate(legacy);

            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(nav.gameObject);
            if (removed > 0)
                log.AppendLine($"- Missing scripts purgés : {removed}");

            conforme++;
            log.AppendLine("- HubNavigationUI / missing scripts nettoyés ✓ → conforme");
        }

        private static int CountMissingScripts(GameObject go)
        {
            int count = 0;
            MonoBehaviour[] monos = go.GetComponents<MonoBehaviour>();
            for (int i = 0; i < monos.Length; i++)
            {
                if (monos[i] == null)
                    count++;
            }

            return count;
        }

        private static void EnsureSiblingOrder(
            RectTransform safeRoot,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            Transform page = FindDirectChild(safeRoot, PageContainerName);
            Transform header = FindDirectChild(safeRoot, "Header");
            Transform nav = FindDirectChild(safeRoot, NavName);

            if (page == null || nav == null)
            {
                failed++;
                log.AppendLine("- Ordre siblings — PageContainer ou NavigationBar manquant ✗");
                return;
            }

            bool ok =
                page.GetSiblingIndex() < nav.GetSiblingIndex()
                && (header == null || (page.GetSiblingIndex() < header.GetSiblingIndex()
                                       && header.GetSiblingIndex() < nav.GetSiblingIndex()));

            // Sous SafeRoot, nav en dernier sibling.
            bool navLast = nav.GetSiblingIndex() == safeRoot.childCount - 1;

            if (ok && navLast)
            {
                conforme++;
                log.AppendLine("- Ordre siblings PageContainer → Header → NavigationBar (dernier) ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Réordonner siblings (nav dernier) — À FAIRE");
                return;
            }

            Undo.SetTransformParent(page, safeRoot, UndoLabel);
            if (header != null)
                header.SetSiblingIndex(1);
            page.SetAsFirstSibling();
            nav.SetAsLastSibling();

            conforme++;
            log.AppendLine("- Ordre siblings corrigé ✓ → conforme");
        }

        // ═══════════════════════════════════════════
        // FABRICATION UI
        // ═══════════════════════════════════════════

        private static void EnsureChildImage(
            RectTransform parent,
            string name,
            Color color,
            bool raycast,
            bool stretch,
            Sprite sprite)
        {
            Transform existing = FindDirectChild(parent, name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(parent, false);
                go.layer = parent.gameObject.layer;
            }
            else
            {
                go = existing.gameObject;
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
            }

            Image img = go.GetComponent<Image>();
            if (img == null)
                img = Undo.AddComponent<Image>(go);
            img.color = color;
            img.raycastTarget = raycast;
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            EditorUtility.SetDirty(img);
            EditorUtility.SetDirty(rt);
        }

        private static void EnsureFooterMotif(RectTransform nav, Sprite footer)
        {
            EnsureChildImage(nav, "FooterMotif", MotifTint, raycast: false, stretch: true, footer);
            RectTransform rt = FindDirectChild(nav, "FooterMotif") as RectTransform;
            if (rt == null)
                return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = MotifScale;
            Image img = rt.GetComponent<Image>();
            if (img != null)
            {
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = false;
            }
            EditorUtility.SetDirty(rt);
        }

        private static void EnsureTabsRow(RectTransform nav)
        {
            Transform existing = FindDirectChild(nav, "TabsRow");
            GameObject go;
            if (existing == null)
            {
                go = new GameObject("TabsRow", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(nav, false);
                go.layer = nav.gameObject.layer;
            }
            else
            {
                go = existing.gameObject;
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            EditorUtility.SetDirty(rt);
        }

        private static void EnsureTabTemplate(RectTransform nav)
        {
            Transform existing = FindDirectChild(nav, "TabTemplate");
            GameObject go;
            if (existing == null)
            {
                go = new GameObject("TabTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(nav, false);
                go.layer = nav.gameObject.layer;
            }
            else
            {
                go = existing.gameObject;
            }

            go.SetActive(false);

            RectTransform root = go.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Image hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            Button btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;

            // ActiveTopLine — au-dessus de l'icône, abaissée dans l'encart (pas collée au haut).
            EnsureNamedChildImage(go.transform, "ActiveTopLine", UiTheme.AccentAmber, false);
            RectTransform line = go.transform.Find("ActiveTopLine") as RectTransform;
            if (line != null)
            {
                line.anchorMin = new Vector2(0.18f, 0.62f);
                line.anchorMax = new Vector2(0.82f, 0.62f);
                line.pivot = new Vector2(0.5f, 0.5f);
                line.anchoredPosition = Vector2.zero;
                line.sizeDelta = new Vector2(0f, UiTheme.BorderFocus);
                Image lineImg = line.GetComponent<Image>();
                if (lineImg != null)
                    lineImg.enabled = false;
            }

            // IconSlot 64×64 — bas-milieu de l'encart tabs.
            Transform slotTx = go.transform.Find("IconSlot");
            GameObject slotGo;
            if (slotTx == null)
            {
                slotGo = new GameObject("IconSlot", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(slotGo, UndoLabel);
                slotGo.transform.SetParent(go.transform, false);
            }
            else
            {
                slotGo = slotTx.gameObject;
            }

            RectTransform slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0.5f, 0.38f);
            slotRt.anchorMax = new Vector2(0.5f, 0.38f);
            slotRt.pivot = new Vector2(0.5f, 0.5f);
            slotRt.anchoredPosition = Vector2.zero;
            slotRt.sizeDelta = new Vector2(IconSlotSize, IconSlotSize);

            EnsureNamedChildImage(slotGo.transform, "Icon", UiTheme.TextMuted, false);
            RectTransform iconRt = slotGo.transform.Find("Icon") as RectTransform;
            if (iconRt != null)
            {
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.sizeDelta = new Vector2(IconSlotSize, IconSlotSize);
            }

            // Badge
            EnsureNamedChildImage(slotGo.transform, "Badge", UiTheme.AccentGold, false);
            RectTransform badgeRt = slotGo.transform.Find("Badge") as RectTransform;
            if (badgeRt != null)
            {
                badgeRt.anchorMin = new Vector2(1f, 1f);
                badgeRt.anchorMax = new Vector2(1f, 1f);
                badgeRt.pivot = new Vector2(0.5f, 0.5f);
                badgeRt.anchoredPosition = new Vector2(-2f, -2f);
                badgeRt.sizeDelta = new Vector2(BadgeSize, BadgeSize);
                badgeRt.gameObject.SetActive(false);
            }

            // Label
            Transform labelTx = go.transform.Find("Label");
            GameObject labelGo;
            if (labelTx == null)
            {
                labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelGo, UndoLabel);
                labelGo.transform.SetParent(go.transform, false);
            }
            else
            {
                labelGo = labelTx.gameObject;
                if (labelGo.GetComponent<TextMeshProUGUI>() == null)
                    Undo.AddComponent<TextMeshProUGUI>(labelGo);
            }

            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.02f);
            labelRt.anchorMax = new Vector2(1f, 0.22f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "Tab";
            tmp.fontSize = UiTypography.Caption;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UiTheme.TextMuted;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            EditorUtility.SetDirty(tmp);
        }

        private static void EnsureNamedChildImage(Transform parent, string name, Color color, bool raycast)
        {
            Transform existing = parent.Find(name);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
                if (go.GetComponent<Image>() == null)
                    Undo.AddComponent<Image>(go);
            }

            Image img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            img.preserveAspect = false;
            EditorUtility.SetDirty(img);
        }

        private static bool IsNavBackdropConforme(Transform backdrop)
        {
            if (backdrop == null)
                return false;
            Image img = backdrop.GetComponent<Image>();
            if (img == null || !img.raycastTarget)
                return false;
            return img.color.a <= 0.01f;
        }

        private static bool IsFooterMotifConforme(Transform motif)
        {
            if (motif == null)
                return false;
            RectTransform rt = motif as RectTransform;
            if (rt == null)
                return false;
            Image img = motif.GetComponent<Image>();
            if (img == null || img.sprite == null || img.preserveAspect)
                return false;
            bool stretch =
                Mathf.Approximately(rt.anchorMin.x, 0f)
                && Mathf.Approximately(rt.anchorMin.y, 0f)
                && Mathf.Approximately(rt.anchorMax.x, 1f)
                && Mathf.Approximately(rt.anchorMax.y, 1f)
                && Mathf.Approximately(rt.sizeDelta.x, 0f)
                && Mathf.Approximately(rt.sizeDelta.y, 0f);
            bool scaled =
                Mathf.Abs(rt.localScale.x - MotifScale.x) < 0.02f
                && Mathf.Abs(rt.localScale.y - MotifScale.y) < 0.02f;
            return stretch && scaled;
        }

        private static bool IsTabTemplateConforme(Transform template)
        {
            if (template == null || template.gameObject.activeSelf)
                return false;
            if (template.Find("ActiveTopLine") == null)
                return false;
            if (template.Find("IconSlot/Icon") == null)
                return false;
            if (template.Find("IconSlot/Badge") == null)
                return false;
            if (template.Find("Label") == null)
                return false;
            if (template.GetComponent<Button>() == null)
                return false;
            return true;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static Sprite[] LoadNavIcons()
        {
            var icons = new Sprite[4];
            for (int i = 0; i < 4; i++)
                icons[i] = AssetDatabase.LoadAssetAtPath<Sprite>(IconPaths[i]);
            return icons;
        }

        private static Sprite LoadSpriteByGuid(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject FindPageGo(RectTransform safeRoot, string pageName)
        {
            Transform pageContainer = FindDirectChild(safeRoot, PageContainerName);
            if (pageContainer != null)
            {
                Transform page = FindDirectChild(pageContainer, pageName);
                if (page != null)
                    return page.gameObject;
            }

            Transform[] all = safeRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == pageName)
                    return all[i].gameObject;
            }

            return null;
        }

        private static RectTransform FindSafeRoot(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < all.Length; j++)
                {
                    if (all[j] != null && all[j].name == SafeRootName)
                        return all[j] as RectTransform;
                }
            }

            return null;
        }

        private static RectTransform FindDirectChild(Transform parent, string name)
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

        private static void SetSiblingFirst(RectTransform parent, string childName)
        {
            Transform t = FindDirectChild(parent, childName);
            if (t != null)
                t.SetAsFirstSibling();
        }

        private static void SetSiblingAfter(RectTransform parent, string childName, string afterName)
        {
            Transform child = FindDirectChild(parent, childName);
            Transform after = FindDirectChild(parent, afterName);
            if (child == null || after == null)
                return;
            child.SetSiblingIndex(after.GetSiblingIndex() + 1);
        }
    }
}
#endif
