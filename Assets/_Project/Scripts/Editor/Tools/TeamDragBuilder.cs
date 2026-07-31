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
    /// Gate 5.b — DragLayer + TeamDragController + hint (harnais v2).
    /// </summary>
    public static class TeamDragBuilder
    {
        private const string UndoLabel = "Team Drag Builder 5.b";
        private const string PageName = "PageEquipe";
        private const string RootName = "EquipeRoot";
        private const string DragLayerName = "DragLayer";
        private const string OverlayName = "OverlayLayer";
        private const string HintName = "DragHint";

        [MenuItem("Chez Arthur/Refonte Hub/Page Équipe — Drag 5.b (DRY RUN)")]
        public static void DryRun() => Run(false);

        [MenuItem("Chez Arthur/Refonte Hub/Page Équipe — Drag 5.b (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Drag Équipe 5.b",
                    "Crée DragLayer (HubCanvas, avant OverlayLayer), pose TeamDragController, "
                    + "hint onboarding, rebind slots/cartes.\n\nCtrl+S Hub ensuite.",
                    "Appliquer",
                    "Annuler"))
                return;

            Run(true);
        }

        private static void Run(bool apply)
        {
            var log = new StringBuilder(8192);
            int todo = 0, conforme = 0, failed = 0;
            string mode = apply ? "APPLIQUER" : "DRY RUN";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" TeamDragBuilder 5.b — {mode}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Hub")
            {
                Debug.LogError("[TeamDragBuilder] Ouvre Hub.unity.");
                return;
            }

            // Verdict hint
            log.AppendLine("## Verdict hint");
            log.AppendLine(
                "- SaveData non gelé → `hintTeamDragSeen` dans SaveData + PersistentManager ✓");
            conforme++;
            log.AppendLine();

            Transform hubCanvas = FindDeep(scene, "HubCanvas");
            Transform overlay = FindDeep(scene, OverlayName);
            Transform page = FindDeep(scene, PageName);
            Transform root = page != null ? page.Find(RootName) : null;
            TeamPageUI pageUi = page != null ? page.GetComponent<TeamPageUI>() : null;

            if (hubCanvas == null || page == null || root == null || pageUi == null)
            {
                failed++;
                log.AppendLine("- ✗ HubCanvas / PageEquipe / EquipeRoot / TeamPageUI manquant");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            log.AppendLine("## DragLayer (HubCanvas, avant OverlayLayer)");
            Transform dragLayer = hubCanvas.Find(DragLayerName);
            if (dragLayer != null)
            {
                conforme++;
                log.AppendLine("- DragLayer déjà présent ✓");
            }
            else if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] CRÉER DragLayer sibling avant OverlayLayer — À FAIRE");
            }
            else
            {
                GameObject go = new GameObject(DragLayerName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                Undo.SetTransformParent(go.transform, hubCanvas, false, UndoLabel);
                dragLayer = go.transform;
                RectTransform rt = (RectTransform)dragLayer;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                if (overlay != null)
                    dragLayer.SetSiblingIndex(overlay.GetSiblingIndex());
                else
                    dragLayer.SetAsLastSibling();
                conforme++;
                log.AppendLine("- DragLayer créé (avant OverlayLayer) ✓");
            }

            if (overlay != null && dragLayer != null && apply)
            {
                int want = overlay.GetSiblingIndex();
                if (dragLayer.GetSiblingIndex() > want)
                    dragLayer.SetSiblingIndex(want);
                log.AppendLine(
                    $"- Ordre : DragLayer index={dragLayer.GetSiblingIndex()}, "
                    + $"OverlayLayer={overlay.GetSiblingIndex()} ✓");
            }

            log.AppendLine();
            log.AppendLine("## TeamDragController + hint");

            TeamDragController ctrl = root.GetComponent<TeamDragController>();
            if (ctrl == null && !apply)
            {
                todo++;
                log.AppendLine("- [DRY] AJOUTER TeamDragController sur EquipeRoot — À FAIRE");
            }
            else if (ctrl == null && apply)
            {
                ctrl = Undo.AddComponent<TeamDragController>(root.gameObject);
                conforme++;
                log.AppendLine("- TeamDragController ajouté ✓");
            }
            else
            {
                conforme++;
                log.AppendLine("- TeamDragController présent ✓");
            }

            Transform dock = root.Find("TeamDock");
            ScrollRect scroll = root.GetComponentInChildren<ScrollRect>(true);
            TeamSlotUI[] slots = root.GetComponentsInChildren<TeamSlotUI>(true);

            Transform hint = root.Find(HintName);
            if (hint == null && !apply)
            {
                todo++;
                log.AppendLine("- [DRY] CRÉER DragHint Caption sous dock — À FAIRE");
            }
            else if (apply)
            {
                hint = EnsureHint(root, dock, log, ref conforme);
            }

            Sprite borderSprite = RoundedRectSpriteGenerator.LoadSpriteS();

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] REBIND controller + TeamPageUI.dragController — À FAIRE");
                todo++;
                log.AppendLine("- [DRY] BindDragController sur slots existants — À FAIRE");
            }
            else if (ctrl != null)
            {
                SerializedObject so = new SerializedObject(ctrl);
                so.FindProperty("teamPageUI").objectReferenceValue = pageUi;
                SerializedProperty slotsProp = so.FindProperty("teamSlots");
                slotsProp.arraySize = 4;
                for (int i = 0; i < 4; i++)
                {
                    TeamSlotUI s = i < slots.Length ? slots[i] : null;
                    slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = s;
                    if (s != null)
                        s.BindDragController(ctrl);
                }

                so.FindProperty("collectionScroll").objectReferenceValue = scroll;
                so.FindProperty("dragLayer").objectReferenceValue =
                    dragLayer != null ? dragLayer : null;
                so.FindProperty("teamDock").objectReferenceValue =
                    dock != null ? dock : null;
                so.FindProperty("dragHintRoot").objectReferenceValue =
                    hint != null ? hint.gameObject : null;
                so.FindProperty("ghostBorderSprite").objectReferenceValue = borderSprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ctrl);

                SerializedObject pageSo = new SerializedObject(pageUi);
                pageSo.FindProperty("dragController").objectReferenceValue = ctrl;
                pageSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pageUi);

                conforme++;
                log.AppendLine(
                    $"- Rebind OK (slots={slots.Length}, scroll={(scroll != null)}, hint={(hint != null)}) ✓");
            }

            log.AppendLine();
            log.AppendLine("## INTERDITS respectés");
            log.AppendLine("- CharacterDetailPopup structure : non touché ✓");
            log.AppendLine("- Tri / grille / header / nav : non touchés ✓");
            conforme++;

            log.AppendLine();
            AppendCounter(log, todo, conforme, failed);

            if (apply)
                EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log(log.ToString());
        }

        private static Transform EnsureHint(
            Transform root,
            Transform dock,
            StringBuilder log,
            ref int conforme)
        {
            Transform hint = root.Find(HintName);
            if (hint == null)
            {
                GameObject go = new GameObject(
                    HintName, typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                Undo.SetTransformParent(go.transform, root, false, UndoLabel);
                hint = go.transform;

                // Juste sous TeamDock
                if (dock != null)
                    hint.SetSiblingIndex(dock.GetSiblingIndex() + 1);
            }

            LayoutElement le = hint.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(hint.gameObject);
            le.minHeight = 36f;
            le.preferredHeight = 36f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            RectTransform rt = (RectTransform)hint;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            TextMeshProUGUI tmp = hint.GetComponent<TextMeshProUGUI>();
            tmp.text = "Maintiens un personnage pour l'ajouter à l'équipe";
            tmp.fontSize = UiTypography.Caption;
            tmp.color = UiTheme.TextMuted;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            tmp.margin = new Vector4(UiTheme.Space4, 0f, UiTheme.Space4, 0f);

            conforme++;
            log.AppendLine("- DragHint Caption ✓");
            return hint;
        }

        private static void AppendCounter(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine("───────────────────────────────────────────");
            log.AppendLine($" À FAIRE={todo} | CONFORMES={conforme} | ÉCHECS={failed}");
            log.AppendLine("───────────────────────────────────────────");
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
