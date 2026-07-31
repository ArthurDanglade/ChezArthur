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
    /// Polish layout PageEquipe 5.a.1 — double inset PageEquipe, scroll VLG, titre section.
    /// </summary>
    public static class TeamPageLayoutPolishBuilder
    {
        private const string UndoLabel = "Team Page Layout Polish 5.a.1";
        private const string PageName = "PageEquipe";
        private const string RootName = "EquipeRoot";
        private const float TeamDockHeight = 188f;
        private const float SortRowHeight = 56f;
        private const float SectionTitleHeight = 40f;

        [MenuItem("Chez Arthur/Refonte Hub/Page Équipe — Layout polish 5.a.1 (DRY RUN)")]
        public static void DryRun() => Run(apply: false);

        [MenuItem("Chez Arthur/Refonte Hub/Page Équipe — Layout polish 5.a.1 (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Layout Équipe 5.a.1",
                    "Corrige : PageEquipe full-bleed (plus de -280), scroll sous VLG, "
                    + "topGap réduit, titre « Équipe », collection jusqu'à la nav.\n\nCtrl+S ensuite.",
                    "Appliquer",
                    "Annuler"))
                return;

            Run(apply: true);
        }

        private static void Run(bool apply)
        {
            var log = new StringBuilder(4096);
            string mode = apply ? "APPLIQUER" : "DRY RUN";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" TeamPageLayoutPolish 5.a.1 — {mode}");
            log.AppendLine("═══════════════════════════════════════════");

            int todo = 0, conforme = 0, failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Hub")
            {
                Debug.LogError("[TeamPageLayoutPolish] Ouvre Hub.unity.");
                return;
            }

            Transform page = FindDeep(scene, PageName);
            Transform root = page != null ? page.Find(RootName) : null;
            if (page == null || root == null)
            {
                failed++;
                log.AppendLine("- ✗ PageEquipe / EquipeRoot introuvable");
                Debug.Log(log.ToString());
                return;
            }

            // —— 1. PageEquipe full stretch (bug sizeDelta.y = -280) ——
            log.AppendLine("## PageEquipe full-bleed");
            RectTransform pageRt = (RectTransform)page;
            bool pageWrong = !Mathf.Approximately(pageRt.sizeDelta.y, 0f)
                             || pageRt.anchoredPosition != Vector2.zero
                             || pageRt.anchorMin != Vector2.zero
                             || pageRt.anchorMax != Vector2.one;
            if (!pageWrong)
            {
                conforme++;
                log.AppendLine("- PageEquipe déjà full stretch ✓");
            }
            else if (!apply)
            {
                todo++;
                log.AppendLine(
                    $"- [DRY] RESET PageEquipe stretch (sizeDelta.y={pageRt.sizeDelta.y:0.#} → 0) — À FAIRE");
            }
            else
            {
                Undo.RecordObject(pageRt, UndoLabel);
                pageRt.anchorMin = Vector2.zero;
                pageRt.anchorMax = Vector2.one;
                pageRt.pivot = new Vector2(0.5f, 0.5f);
                pageRt.anchoredPosition = Vector2.zero;
                pageRt.sizeDelta = Vector2.zero;
                pageRt.offsetMin = Vector2.zero;
                pageRt.offsetMax = Vector2.zero;
                EditorUtility.SetDirty(pageRt);
                conforme++;
                log.AppendLine("- PageEquipe full stretch (fin du double-inset -280) ✓");
            }

            log.AppendLine();

            // —— 2. PageHeaderClearance topGap ——
            log.AppendLine("## Clearance");
            PageHeaderClearance clearance = root.GetComponent<PageHeaderClearance>();
            if (clearance == null)
            {
                failed++;
                log.AppendLine("- ✗ PageHeaderClearance absent");
            }
            else
            {
                SerializedObject so = new SerializedObject(clearance);
                SerializedProperty topGap = so.FindProperty("topGap");
                SerializedProperty bottomGap = so.FindProperty("bottomGap");
                float wantTop = UiTheme.Space2;
                float wantBottom = UiTheme.Space2;
                bool gapOk = Mathf.Approximately(topGap.floatValue, wantTop)
                             && Mathf.Approximately(bottomGap.floatValue, wantBottom);
                if (gapOk)
                {
                    conforme++;
                    log.AppendLine($"- topGap/bottomGap = Space2 ✓");
                }
                else if (!apply)
                {
                    todo++;
                    log.AppendLine(
                        $"- [DRY] topGap {topGap.floatValue}→{wantTop}, bottomGap {bottomGap.floatValue}→{wantBottom} — À FAIRE");
                }
                else
                {
                    topGap.floatValue = wantTop;
                    bottomGap.floatValue = wantBottom;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    clearance.Refresh();
                    EditorUtility.SetDirty(clearance);
                    conforme++;
                    log.AppendLine("- topGap/bottomGap → Space2 + Refresh ✓");
                }
            }

            log.AppendLine();

            // —— 3. Titre section + layout children ——
            log.AppendLine("## EquipeRoot enfants (titre / dock / sort / scroll)");
            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Titre « Équipe » + reset RT VLG dock/sort/scroll — À FAIRE");
            }
            else
            {
                EnsureSectionTitle(root, log, ref conforme);
                FixDock(root, log, ref conforme, ref failed);
                FixSortRow(root, log, ref conforme);
                FixCollectionScroll(root, log, ref conforme, ref failed);

                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root);
                if (clearance != null)
                    clearance.Refresh();
            }

            log.AppendLine();
            log.AppendLine("───────────────────────────────────────────");
            log.AppendLine($" À FAIRE={todo} | CONFORMES={conforme} | ÉCHECS={failed}");
            log.AppendLine("───────────────────────────────────────────");

            if (apply)
                EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log(log.ToString());
        }

        private static void EnsureSectionTitle(Transform root, StringBuilder log, ref int conforme)
        {
            Transform titleTx = root.Find("SectionTitle");
            TextMeshProUGUI tmp;
            if (titleTx == null)
            {
                GameObject go = new GameObject(
                    "SectionTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                Undo.SetTransformParent(go.transform, root, false, UndoLabel);
                titleTx = go.transform;
                titleTx.SetSiblingIndex(0);
                tmp = go.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                tmp = titleTx.GetComponent<TextMeshProUGUI>();
                titleTx.SetSiblingIndex(0);
            }

            PrepareVlgChild((RectTransform)titleTx);

            LayoutElement le = titleTx.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(titleTx.gameObject);
            le.minHeight = SectionTitleHeight;
            le.preferredHeight = SectionTitleHeight;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            tmp.text = "Équipe";
            tmp.fontSize = UiTypography.Label;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = UiTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(UiTheme.Space4, 0f, UiTheme.Space4, 0f);
            tmp.raycastTarget = false;

            conforme++;
            log.AppendLine("- SectionTitle « Équipe » ✓");
        }

        private static void FixDock(Transform root, StringBuilder log, ref int conforme, ref int failed)
        {
            Transform dock = root.Find("TeamDock");
            if (dock == null)
            {
                failed++;
                log.AppendLine("- ✗ TeamDock absent");
                return;
            }

            dock.SetSiblingIndex(1);
            PrepareVlgChild((RectTransform)dock);

            LayoutElement le = dock.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(dock.gameObject);
            le.minHeight = TeamDockHeight;
            le.preferredHeight = TeamDockHeight;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            conforme++;
            log.AppendLine($"- TeamDock VLG h={TeamDockHeight} ✓");
        }

        private static void FixSortRow(Transform root, StringBuilder log, ref int conforme)
        {
            Transform sortRow = root.Find("SortRow");
            if (sortRow == null)
                return;

            sortRow.SetSiblingIndex(2);
            PrepareVlgChild((RectTransform)sortRow);

            LayoutElement le = sortRow.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(sortRow.gameObject);
            le.minHeight = SortRowHeight;
            le.preferredHeight = SortRowHeight;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            conforme++;
            log.AppendLine("- SortRow VLG ✓");
        }

        private static void FixCollectionScroll(
            Transform root,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            Transform scrollTx = root.Find("CollectionScroll");
            if (scrollTx == null)
            {
                ScrollRect any = root.GetComponentInChildren<ScrollRect>(true);
                scrollTx = any != null ? any.transform : null;
            }

            if (scrollTx == null)
            {
                failed++;
                log.AppendLine("- ✗ CollectionScroll absent");
                return;
            }

            scrollTx.SetAsLastSibling();
            RectTransform scrollRt = (RectTransform)scrollTx;
            Undo.RecordObject(scrollRt, UndoLabel);

            // Sous VLG : largeur stretch, hauteur pilotée par LayoutElement (plus d'ancres phone).
            scrollRt.anchorMin = new Vector2(0f, 1f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.pivot = new Vector2(0.5f, 1f);
            scrollRt.anchoredPosition = Vector2.zero;
            scrollRt.sizeDelta = new Vector2(0f, 400f);
            scrollRt.localScale = Vector3.one;

            LayoutElement le = scrollTx.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(scrollTx.gameObject);
            Undo.RecordObject(le, UndoLabel);
            le.minHeight = 240f;
            le.preferredHeight = -1f;
            le.flexibleHeight = 1f;
            le.flexibleWidth = 1f;
            le.ignoreLayout = false;

            // Viewport plein cadre
            ScrollRect sr = scrollTx.GetComponent<ScrollRect>();
            if (sr != null && sr.viewport != null)
            {
                RectTransform vp = sr.viewport;
                Undo.RecordObject(vp, UndoLabel);
                vp.anchorMin = Vector2.zero;
                vp.anchorMax = Vector2.one;
                vp.offsetMin = Vector2.zero;
                vp.offsetMax = Vector2.zero;
                vp.anchoredPosition = Vector2.zero;
                vp.sizeDelta = Vector2.zero;
            }

            EditorUtility.SetDirty(scrollRt);
            EditorUtility.SetDirty(le);
            conforme++;
            log.AppendLine("- CollectionScroll reset VLG + Viewport stretch (fin gap bas / coupe) ✓");
        }

        /// <summary> Ancres haut-stretch adaptées au VerticalLayoutGroup. </summary>
        private static void PrepareVlgChild(RectTransform rt)
        {
            Undo.RecordObject(rt, UndoLabel);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y > 1f ? rt.sizeDelta.y : 100f);
            rt.localScale = Vector3.one;
            EditorUtility.SetDirty(rt);
        }

        private static Transform FindDeep(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform f = FindDeep(roots[i].transform, name);
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
    }
}
#endif
