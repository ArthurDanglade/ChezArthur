#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
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
    /// Gate 5.a — refonte PageEquipe (collection-first, dock, purge PhoneFrame).
    /// Harnais v2 DRY / APPLY.
    /// </summary>
    public static class TeamPageRebuilder
    {
        private const string UndoLabel = "Team Page Rebuilder 5.a";
        private const string PageName = "PageEquipe";
        private const string RootName = "EquipeRoot";
        private const string CardPrefabPath = "Assets/_Project/Prefabs/UI/CharacterCard.prefab";
        private const float TeamDockHeight = 200f;
        private const float SortRowHeight = 64f;
        private const float CardAspect = 1.35f;
        private const float BadgeSize = 44f;
        private const float BadgeOverhang = 6f;
        private const float AwakenDotSize = 16f;
        private const float InTeamStripH = 4f;
        private const float InTeamCheckSize = 20f;
        private const float RoleBorderPx = 4f;

        private static readonly string[] SharedHubAssetsForbidden =
        {
            "Assets/_Project/Sprites/Hub/base.png",
            "Assets/_Project/Sprites/Hub/window reflection.png"
        };

        private static readonly string[] ExclusiveTeampageAssets =
        {
            "Assets/_Project/Sprites/Teampage/Team page- new phone.png",
            "Assets/_Project/Sprites/Teampage/Team page- base phone.png",
            "Assets/_Project/Sprites/Teampage/Tabs/Team page- collection active.png",
            "Assets/_Project/Sprites/Teampage/Tabs/Team page- collection inactive.png",
            "Assets/_Project/Sprites/Teampage/Tabs/Team page- team active.png",
            "Assets/_Project/Sprites/Teampage/Tabs/Team page- team inactive.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- 1.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- 2.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- 3.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- 4.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- 5.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- 1w.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- 2w.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- 3w.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- 4w.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- 5w.png",
            "Assets/_Project/Sprites/Teampage/Presets/Team page- active selection.png",
            "Assets/_Project/Sprites/Teampage/Team page- phone stats.png"
        };

        [MenuItem("Chez Arthur/Refonte Hub/Page Équipe — Rebuilder 5.a (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Page Équipe — Rebuilder 5.a (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Page Équipe 5.a",
                    "Purge PhoneFrame / presets-UI / duplicatas, dock collection-first, "
                    + "restyle slots + prefab CharacterCard, purge assets Teampage exclusifs.\n\n"
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
            log.AppendLine($" TeamPageRebuilder 5.a — {mode}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int todo = 0;
            int conforme = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "Hub")
            {
                Debug.LogError("[TeamPageRebuilder] Ouvre Hub.unity.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine();

            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteL = RoundedRectSpriteGenerator.LoadSpriteL();
            if (spriteS == null || spriteM == null || spriteL == null)
            {
                failed++;
                log.AppendLine("- ✗ RoundedRect_S/M/L manquants — Générer les sprites arrondis");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            Transform page = FindDeep(scene, PageName);
            if (page == null)
            {
                failed++;
                log.AppendLine("- ✗ PageEquipe introuvable");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            TeamPageUI pageUi = page.GetComponent<TeamPageUI>();
            if (pageUi == null)
            {
                failed++;
                log.AppendLine("- ✗ TeamPageUI absent sur PageEquipe");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            // —— Fond ——
            log.AppendLine("## Fond page");
            Transform bgLayer = FindDeep(scene, "BackgroundLayer");
            if (bgLayer != null)
            {
                conforme++;
                log.AppendLine(
                    "- BackgroundLayer présent → pas d'Image BgDeep sur PageEquipe (fond scène) ✓");
            }
            else
            {
                todo++;
                log.AppendLine(
                    "- [DRY] BackgroundLayer absent — créer Image BgDeep pleine page si APPLY");
                if (apply)
                {
                    EnsurePageBgDeep(page, spriteL, log, ref conforme, ref failed);
                }
            }

            log.AppendLine();

            // —— Snapshot refs avant purge ——
            TeamSlotUI[] existingSlots = page.GetComponentsInChildren<TeamSlotUI>(true);
            ScrollRect existingScroll = FindScrollRect(page);
            Transform existingContent = existingScroll != null && existingScroll.content != null
                ? existingScroll.content
                : null;

            SerializedObject pageSoPeek = new SerializedObject(pageUi);
            Object detailPopupRef = pageSoPeek.FindProperty("detailPopup").objectReferenceValue;
            Object cardPrefabRef = pageSoPeek.FindProperty("cardPrefab").objectReferenceValue;
            Object databaseRef = pageSoPeek.FindProperty("characterDatabase").objectReferenceValue;

            log.AppendLine("## Snapshot pré-purge");
            log.AppendLine($"- TeamSlotUI trouvés : {existingSlots.Length}");
            log.AppendLine(
                $"- ScrollRect : {(existingScroll != null ? existingScroll.name : "null")}");
            log.AppendLine(
                $"- Content : {(existingContent != null ? existingContent.name : "null")}");
            log.AppendLine($"- detailPopup : {(detailPopupRef != null ? "OK" : "NULL")}");
            log.AppendLine($"- cardPrefab : {(cardPrefabRef != null ? "OK" : "NULL")}");
            log.AppendLine();

            // —— PURGE GO ——
            log.AppendLine("## PURGE GameObjects (nominative)");
            PurgeNamedChildren(page, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— STRUCTURE ——
            log.AppendLine("## Structure EquipeRoot");
            Transform root;
            TeamSlotUI[] dockSlots;
            CollectionSortBar sortBar;
            Transform collectionContent;
            ScrollRect scroll;

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    "- [DRY] CRÉER EquipeRoot + TeamDock + SortRow + CollectionScroll — À FAIRE");
                todo++;
                log.AppendLine("- [DRY] RESTYLE 4 TeamSlotUI + CharacterCard.prefab — À FAIRE");
                todo++;
                log.AppendLine("- [DRY] RE-BIND TeamPageUI — À FAIRE");
                root = null;
                dockSlots = existingSlots;
                sortBar = null;
                collectionContent = existingContent;
                scroll = existingScroll;
            }
            else
            {
                BuildStructure(
                    page,
                    existingSlots,
                    existingScroll,
                    existingContent,
                    spriteS,
                    spriteM,
                    spriteL,
                    log,
                    out root,
                    out dockSlots,
                    out sortBar,
                    out collectionContent,
                    out scroll,
                    ref conforme,
                    ref failed);
            }

            log.AppendLine();

            // —— Prefab carte ——
            log.AppendLine("## CharacterCard.prefab");
            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Restructurer CharacterCard.prefab — À FAIRE");
            }
            else
            {
                RebuildCharacterCardPrefab(spriteS, spriteM, log, ref conforme, ref failed);
                cardPrefabRef = AssetDatabase.LoadAssetAtPath<CharacterCardUI>(CardPrefabPath);
            }

            log.AppendLine();

            // —— Rebind ——
            log.AppendLine("## Re-bind TeamPageUI");
            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] SerializedObject TeamPageUI — À FAIRE");
            }
            else if (pageUi != null)
            {
                RebindTeamPageUi(
                    pageUi, dockSlots, collectionContent, cardPrefabRef, databaseRef,
                    detailPopupRef, sortBar, log, ref conforme, ref failed);
            }

            log.AppendLine();

            // —— Assets exclusifs ——
            log.AppendLine("## PURGE assets Teampage exclusifs");
            log.AppendLine("INTERDICTION assets Hub partagés :");
            for (int i = 0; i < SharedHubAssetsForbidden.Length; i++)
                log.AppendLine($"  · NE PAS TOUCHER `{SharedHubAssetsForbidden[i]}`");

            PurgeExclusiveAssets(apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— Gacha string ——
            log.AppendLine("## Gacha AutoFindHubPagesIfNeeded");
            VerifyGachaPageResolution(log, ref conforme, ref failed);

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
        // PURGE GO
        // ═══════════════════════════════════════════

        private static void PurgeNamedChildren(
            Transform page,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            // Noms cibles (Trim) — trailing spaces / (1) gérés via NameMatches.
            string[] purgeExact =
            {
                "LandscapeLayer (1)",
                "Window (1)",
                "WagonLayer (1)",
                "LightOverlay  (1)",
                "LightOverlay (1)",
                "PhoneFrame",
                "PresetBar"
            };

            var toDestroy = new List<Transform>();
            for (int i = 0; i < page.childCount; i++)
            {
                Transform child = page.GetChild(i);
                string n = child.name;
                for (int p = 0; p < purgeExact.Length; p++)
                {
                    if (NameMatches(n, purgeExact[p]))
                    {
                        toDestroy.Add(child);
                        break;
                    }
                }
            }

            // PresetBar peut être plus profond (sous TeamSetupPanel) si PhoneFrame déjà partiel.
            Transform deepPreset = FindDeepTrimmed(page, "PresetBar");
            if (deepPreset != null && !toDestroy.Contains(deepPreset)
                && !IsUnder(deepPreset, toDestroy))
                toDestroy.Add(deepPreset);

            Transform phone = FindDeepTrimmed(page, "PhoneFrame");
            if (phone != null && !toDestroy.Contains(phone) && !IsUnder(phone, toDestroy))
                toDestroy.Add(phone);

            // Ne garder que les racines (évite PresetBar sous PhoneFrame déjà listé).
            FilterToDestroyRoots(toDestroy);

            if (toDestroy.Count == 0)
            {
                conforme++;
                log.AppendLine("- Aucun GO de purge restant (déjà propre) ✓");
                return;
            }

            for (int i = 0; i < toDestroy.Count; i++)
            {
                Transform t = toDestroy[i];
                if (t == null)
                    continue;

                string goName = t.name;
                string path = GetPath(t);
                if (!apply)
                {
                    todo++;
                    log.AppendLine($"- [DRY] PURGE GO `{goName}` @ {path} — À FAIRE");
                }
                else
                {
                    // Détache TeamSlotUI / Scroll avant destruction PhoneFrame.
                    DetachRecyclablesBeforeDestroy(t, page);
                    Undo.DestroyObjectImmediate(t.gameObject);
                    conforme++;
                    log.AppendLine($"- PURGE GO `{goName}` @ {path} ✓");
                }
            }

            // PhoneTabController / TeamPresetUI orphelins éventuels
            PhoneTabController[] tabs = page.GetComponentsInChildren<PhoneTabController>(true);
            TeamPresetUI[] presets = page.GetComponentsInChildren<TeamPresetUI>(true);
            if (!apply)
            {
                if (tabs.Length > 0)
                {
                    todo++;
                    log.AppendLine(
                        $"- [DRY] PURGE PhoneTabController ×{tabs.Length} — À FAIRE");
                }

                if (presets.Length > 0)
                {
                    todo++;
                    log.AppendLine($"- [DRY] PURGE TeamPresetUI ×{presets.Length} — À FAIRE");
                }
            }
            else
            {
                for (int i = 0; i < tabs.Length; i++)
                {
                    if (tabs[i] == null)
                        continue;
                    string host = tabs[i].gameObject != null ? tabs[i].gameObject.name : "?";
                    Undo.DestroyObjectImmediate(tabs[i]);
                    conforme++;
                    log.AppendLine($"- PURGE composant PhoneTabController sur `{host}` ✓");
                }

                for (int i = 0; i < presets.Length; i++)
                {
                    if (presets[i] == null)
                        continue;
                    string host = presets[i].gameObject != null ? presets[i].gameObject.name : "?";
                    Undo.DestroyObjectImmediate(presets[i]);
                    conforme++;
                    log.AppendLine($"- PURGE composant TeamPresetUI sur `{host}` ✓");
                }
            }
        }

        /// <summary>
        /// Retire de la liste tout transform descendant d'un autre élément listé.
        /// </summary>
        private static void FilterToDestroyRoots(List<Transform> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Transform t = list[i];
                if (t == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                bool underOther = false;
                for (int j = 0; j < list.Count; j++)
                {
                    if (i == j || list[j] == null)
                        continue;
                    if (IsDescendantOf(t, list[j]))
                    {
                        underOther = true;
                        break;
                    }
                }

                if (underOther)
                    list.RemoveAt(i);
            }
        }

        private static bool IsDescendantOf(Transform t, Transform ancestor)
        {
            if (t == null || ancestor == null)
                return false;
            Transform cur = t.parent;
            while (cur != null)
            {
                if (cur == ancestor)
                    return true;
                cur = cur.parent;
            }

            return false;
        }

        private static void DetachRecyclablesBeforeDestroy(Transform doomed, Transform page)
        {
            if (doomed == null || page == null)
                return;

            // Remonte temporairement slots + scroll hors de l'arbre à détruire.
            TeamSlotUI[] slots = doomed.GetComponentsInChildren<TeamSlotUI>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    Undo.SetTransformParent(slots[i].transform, page, false, UndoLabel);
            }

            ScrollRect[] scrolls = doomed.GetComponentsInChildren<ScrollRect>(true);
            for (int i = 0; i < scrolls.Length; i++)
            {
                if (scrolls[i] != null)
                    Undo.SetTransformParent(scrolls[i].transform, page, false, UndoLabel);
            }
        }

        // ═══════════════════════════════════════════
        // STRUCTURE
        // ═══════════════════════════════════════════

        private static void BuildStructure(
            Transform page,
            TeamSlotUI[] existingSlots,
            ScrollRect existingScroll,
            Transform existingContent,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL,
            StringBuilder log,
            out Transform root,
            out TeamSlotUI[] dockSlots,
            out CollectionSortBar sortBar,
            out Transform collectionContent,
            out ScrollRect scroll,
            ref int conforme,
            ref int failed)
        {
            root = page.Find(RootName);
            if (root == null)
            {
                GameObject rootGo = new GameObject(
                    RootName,
                    typeof(RectTransform),
                    typeof(VerticalLayoutGroup));
                Undo.RegisterCreatedObjectUndo(rootGo, UndoLabel);
                Undo.SetTransformParent(rootGo.transform, page, false, UndoLabel);
                root = rootGo.transform;
            }

            RectTransform rootRt = (RectTransform)root;
            StretchFull(rootRt);

            // PageEquipe doit être full-bleed : un sizeDelta.y négatif (legacy phone/nav)
            // + PageHeaderClearance = double inset (gros trou sous header / au-dessus nav).
            RectTransform pageRt = page as RectTransform;
            if (pageRt != null)
            {
                Undo.RecordObject(pageRt, UndoLabel);
                StretchFull(pageRt);
                pageRt.anchoredPosition = Vector2.zero;
                pageRt.sizeDelta = Vector2.zero;
            }

            VerticalLayoutGroup vlg = root.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = Undo.AddComponent<VerticalLayoutGroup>(root.gameObject);
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = UiTheme.Space3;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            PageHeaderClearance clearance = root.GetComponent<PageHeaderClearance>();
            if (clearance == null)
                clearance = Undo.AddComponent<PageHeaderClearance>(root.gameObject);
            Transform header = FindDeep(page.root, "Header");
            Transform nav = FindDeep(page.root, "NavigationBar");
            clearance.Bind(header as RectTransform, nav as RectTransform);
            SerializedObject clearanceSo = new SerializedObject(clearance);
            clearanceSo.FindProperty("topGap").floatValue = UiTheme.Space2;
            clearanceSo.FindProperty("bottomGap").floatValue = UiTheme.Space2;
            clearanceSo.ApplyModifiedPropertiesWithoutUndo();
            clearance.Refresh();
            log.AppendLine("- EquipeRoot + PageHeaderClearance ✓");
            conforme++;

            // Nettoie vieux panels Collection/TeamSetup orphelins (hors slots/scroll détachés).
            CleanupLegacyPanels(page, root, log, ref conforme);

            // TeamDock
            Transform dock = root.Find("TeamDock");
            if (dock == null)
            {
                GameObject dockGo = new GameObject(
                    "TeamDock",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                Undo.RegisterCreatedObjectUndo(dockGo, UndoLabel);
                Undo.SetTransformParent(dockGo.transform, root, false, UndoLabel);
                dock = dockGo.transform;
            }

            LayoutElement dockLe = dock.GetComponent<LayoutElement>();
            if (dockLe == null)
                dockLe = Undo.AddComponent<LayoutElement>(dock.gameObject);
            dockLe.minHeight = TeamDockHeight;
            dockLe.preferredHeight = TeamDockHeight;
            dockLe.flexibleHeight = 0f;
            dockLe.flexibleWidth = 1f;

            PanelSurface dockSurface = dock.GetComponent<PanelSurface>();
            if (dockSurface == null)
                dockSurface = Undo.AddComponent<PanelSurface>(dock.gameObject);
            BindPanelSurface(
                dockSurface, PanelSurface.SurfaceVariant.Panel,
                PanelSurface.SurfaceBorder.Subtle, spriteS, spriteM, spriteL);

            HorizontalLayoutGroup dockHlg = dock.GetComponent<HorizontalLayoutGroup>();
            if (dockHlg == null)
                dockHlg = Undo.AddComponent<HorizontalLayoutGroup>(dock.gameObject);
            int pad = Mathf.RoundToInt(UiTheme.Space4);
            dockHlg.padding = new RectOffset(pad, pad, pad, pad);
            dockHlg.spacing = UiTheme.Space3;
            dockHlg.childAlignment = TextAnchor.MiddleCenter;
            dockHlg.childControlWidth = true;
            dockHlg.childControlHeight = true;
            dockHlg.childForceExpandWidth = true;
            dockHlg.childForceExpandHeight = true;
            IgnoreLayoutOnFill(dock);

            dockSlots = RestyleAndDockSlots(
                dock, existingSlots, spriteS, spriteM, spriteL, log, ref conforme, ref failed);
            log.AppendLine($"- TeamDock + {dockSlots.Length} slots ✓");
            conforme++;

            // SortRow
            Transform sortRow = root.Find("SortRow");
            if (sortRow == null)
            {
                GameObject sortGo = new GameObject("SortRow", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(sortGo, UndoLabel);
                Undo.SetTransformParent(sortGo.transform, root, false, UndoLabel);
                sortRow = sortGo.transform;
            }

            LayoutElement sortLe = sortRow.GetComponent<LayoutElement>();
            if (sortLe == null)
                sortLe = Undo.AddComponent<LayoutElement>(sortRow.gameObject);
            sortLe.minHeight = SortRowHeight;
            sortLe.preferredHeight = SortRowHeight;
            sortLe.flexibleHeight = 0f;
            sortLe.flexibleWidth = 1f;

            HorizontalLayoutGroup sortHlg = sortRow.GetComponent<HorizontalLayoutGroup>();
            if (sortHlg == null)
                sortHlg = Undo.AddComponent<HorizontalLayoutGroup>(sortRow.gameObject);
            sortHlg.padding = new RectOffset(
                Mathf.RoundToInt(UiTheme.Space4),
                Mathf.RoundToInt(UiTheme.Space4),
                0,
                0);
            sortHlg.childAlignment = TextAnchor.MiddleRight;
            sortHlg.childControlWidth = false;
            sortHlg.childControlHeight = true;
            sortHlg.childForceExpandWidth = false;
            sortHlg.childForceExpandHeight = true;

            sortBar = BuildSortBar(sortRow, spriteS, spriteM, spriteL, log, ref conforme);
            log.AppendLine("- SortRow + CollectionSortBar ✓");

            // CollectionScroll
            scroll = existingScroll;
            if (scroll == null)
                scroll = FindScrollRect(page);

            if (scroll == null)
            {
                scroll = CreateCollectionScroll(root, spriteS, log, ref failed);
            }
            else
            {
                Undo.SetTransformParent(scroll.transform, root, false, UndoLabel);
                scroll.gameObject.name = "CollectionScroll";
            }

            // Ancres phone legacy (posY=115, sizeDelta.y=230) cassent le VLG → reset.
            RectTransform scrollRtFix = (RectTransform)scroll.transform;
            Undo.RecordObject(scrollRtFix, UndoLabel);
            scrollRtFix.anchorMin = new Vector2(0f, 1f);
            scrollRtFix.anchorMax = new Vector2(1f, 1f);
            scrollRtFix.pivot = new Vector2(0.5f, 1f);
            scrollRtFix.anchoredPosition = Vector2.zero;
            scrollRtFix.sizeDelta = new Vector2(0f, 400f);
            scrollRtFix.localScale = Vector3.one;

            LayoutElement scrollLe = scroll.GetComponent<LayoutElement>();
            if (scrollLe == null)
                scrollLe = Undo.AddComponent<LayoutElement>(scroll.gameObject);
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 240f;
            scrollLe.flexibleWidth = 1f;
            scrollLe.preferredHeight = -1f;
            scrollLe.ignoreLayout = false;

            if (scroll.viewport != null)
            {
                RectTransform vp = scroll.viewport;
                Undo.RecordObject(vp, UndoLabel);
                vp.anchorMin = Vector2.zero;
                vp.anchorMax = Vector2.one;
                vp.offsetMin = Vector2.zero;
                vp.offsetMax = Vector2.zero;
                vp.sizeDelta = Vector2.zero;
            }

            collectionContent = scroll.content;
            if (collectionContent == null && existingContent != null)
            {
                scroll.content = existingContent as RectTransform;
                collectionContent = existingContent;
            }

            if (collectionContent == null)
            {
                failed++;
                log.AppendLine("- ✗ Content collection introuvable");
                return;
            }

            ConfigureCollectionGrid(page, collectionContent, log, ref conforme);
            log.AppendLine("- CollectionScroll recyclé + GridLayout 4 cols ✓");
            conforme++;

            // Ordre enfants root
            dock.SetSiblingIndex(0);
            sortRow.SetSiblingIndex(1);
            scroll.transform.SetSiblingIndex(2);
        }

        private static void CleanupLegacyPanels(
            Transform page,
            Transform root,
            StringBuilder log,
            ref int conforme)
        {
            for (int i = page.childCount - 1; i >= 0; i--)
            {
                Transform child = page.GetChild(i);
                if (child == root)
                    continue;
                string n = child.name != null ? child.name.Trim() : string.Empty;
                if (n == "CollectionPanel" || n == "TeamSetupPanel" || n == "PhoneContent"
                    || n == "TeamSlotsContainer" || n == "CollectionTitle")
                {
                    // Si contient encore des slots, détache d'abord
                    TeamSlotUI[] slots = child.GetComponentsInChildren<TeamSlotUI>(true);
                    for (int s = 0; s < slots.Length; s++)
                        Undo.SetTransformParent(slots[s].transform, page, false, UndoLabel);

                    ScrollRect[] scrolls = child.GetComponentsInChildren<ScrollRect>(true);
                    for (int s = 0; s < scrolls.Length; s++)
                        Undo.SetTransformParent(scrolls[s].transform, page, false, UndoLabel);

                    Undo.DestroyObjectImmediate(child.gameObject);
                    log.AppendLine($"- Cleanup legacy `{n}` ✓");
                    conforme++;
                }
            }
        }

        private static TeamSlotUI[] RestyleAndDockSlots(
            Transform dock,
            TeamSlotUI[] existing,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            var list = new List<TeamSlotUI>(4);
            if (existing != null)
            {
                for (int i = 0; i < existing.Length; i++)
                {
                    if (existing[i] != null)
                        list.Add(existing[i]);
                }
            }

            while (list.Count < 4)
            {
                TeamSlotUI created = CreateTeamSlot(dock, list.Count + 1, spriteS, spriteM, spriteL);
                list.Add(created);
                log.AppendLine($"- CRÉER TeamSlot{list.Count} ✓");
            }

            var result = new TeamSlotUI[4];
            for (int i = 0; i < 4; i++)
            {
                TeamSlotUI slot = list[i];
                Undo.SetTransformParent(slot.transform, dock, false, UndoLabel);
                slot.gameObject.name = "TeamSlot" + (i + 1);
                RestyleTeamSlot(slot, spriteS, spriteM, spriteL, log, ref conforme, ref failed);
                result[i] = slot;

                LayoutElement le = slot.GetComponent<LayoutElement>();
                if (le == null)
                    le = Undo.AddComponent<LayoutElement>(slot.gameObject);
                le.flexibleWidth = 1f;
                le.flexibleHeight = 1f;
                le.minWidth = 80f;
                le.minHeight = 80f;
            }

            // Détruit slots surnuméraires
            for (int i = 4; i < list.Count; i++)
            {
                if (list[i] != null)
                    Undo.DestroyObjectImmediate(list[i].gameObject);
            }

            return result;
        }

        private static TeamSlotUI CreateTeamSlot(
            Transform parent,
            int index,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL)
        {
            GameObject go = new GameObject(
                "TeamSlot" + index,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);
            TeamSlotUI slot = Undo.AddComponent<TeamSlotUI>(go);
            int c = 0;
            int f = 0;
            RestyleTeamSlot(slot, spriteS, spriteM, spriteL, new StringBuilder(), ref c, ref f);
            return slot;
        }

        private static void RestyleTeamSlot(
            TeamSlotUI slot,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            if (slot == null)
            {
                failed++;
                return;
            }

            Transform t = slot.transform;

            // Purge enfants legacy (EmptyState / FilledState…)
            for (int i = t.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(t.GetChild(i).gameObject);

            Image roleFrame = t.GetComponent<Image>();
            if (roleFrame == null)
                roleFrame = Undo.AddComponent<Image>(t.gameObject);
            roleFrame.sprite = spriteM;
            roleFrame.type = Image.Type.Sliced;
            roleFrame.color = UiTheme.BorderSubtle;
            roleFrame.raycastTarget = true;

            Button btn = t.GetComponent<Button>();
            if (btn == null)
                btn = Undo.AddComponent<Button>(t.gameObject);
            btn.targetGraphic = roleFrame;
            btn.transition = Selectable.Transition.None;

            GameObject innerGo = new GameObject(
                "Inner",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(innerGo, UndoLabel);
            Undo.SetTransformParent(innerGo.transform, t, false, UndoLabel);
            RectTransform innerRt = (RectTransform)innerGo.transform;
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(RoleBorderPx, RoleBorderPx);
            innerRt.offsetMax = new Vector2(-RoleBorderPx, -RoleBorderPx);

            PanelSurface surface = Undo.AddComponent<PanelSurface>(innerGo);
            BindPanelSurface(
                surface, PanelSurface.SurfaceVariant.Card,
                PanelSurface.SurfaceBorder.Subtle, spriteS, spriteM, spriteL);
            IgnoreLayoutOnFill(innerGo.transform);

            GameObject iconGo = new GameObject(
                "IconImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconGo, UndoLabel);
            Undo.SetTransformParent(iconGo.transform, innerGo.transform, false, UndoLabel);
            RectTransform iconRt = (RectTransform)iconGo.transform;
            StretchWithPadding(iconRt, UiTheme.Space2);
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.enabled = false;

            TextMeshProUGUI level = CreateTmp(
                innerGo.transform, "LevelText", "Nv.1",
                UiTypography.Caption, UiTheme.TextPrimary);
            RectTransform levelRt = level.rectTransform;
            levelRt.anchorMin = new Vector2(0f, 0f);
            levelRt.anchorMax = new Vector2(1f, 0f);
            levelRt.pivot = new Vector2(0.5f, 0f);
            levelRt.anchoredPosition = new Vector2(0f, UiTheme.Space1);
            levelRt.sizeDelta = new Vector2(-UiTheme.Space2, 28f);
            level.alignment = TextAlignmentOptions.Center;
            level.gameObject.SetActive(false);

            TextMeshProUGUI plus = CreateTmp(
                innerGo.transform, "EmptyPlus", "+",
                UiTypography.Display, UiTheme.TextMuted);
            Color muted = UiTheme.TextMuted;
            muted.a = 0.5f;
            plus.color = muted;
            RectTransform plusRt = plus.rectTransform;
            StretchFull(plusRt);
            plus.alignment = TextAlignmentOptions.Center;

            SerializedObject so = new SerializedObject(slot);
            so.FindProperty("roleFrame").objectReferenceValue = roleFrame;
            so.FindProperty("innerContent").objectReferenceValue = innerRt;
            so.FindProperty("panelSurface").objectReferenceValue = surface;
            so.FindProperty("iconImage").objectReferenceValue = iconImg;
            so.FindProperty("levelText").objectReferenceValue = level;
            so.FindProperty("emptyPlusText").objectReferenceValue = plus;
            so.FindProperty("slotButton").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(slot);

            slot.SetEmpty();
            conforme++;
            log.AppendLine($"- Restyle `{slot.name}` ✓");
        }

        private static CollectionSortBar BuildSortBar(
            Transform sortRow,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL,
            StringBuilder log,
            ref int conforme)
        {
            CollectionSortBar existing = sortRow.GetComponentInChildren<CollectionSortBar>(true);
            if (existing != null)
            {
                conforme++;
                return existing;
            }

            // Spacer flexible
            GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(spacer, UndoLabel);
            Undo.SetTransformParent(spacer.transform, sortRow, false, UndoLabel);
            LayoutElement spacerLe = Undo.AddComponent<LayoutElement>(spacer);
            spacerLe.flexibleWidth = 1f;

            UiKitFactory.PillHandle pill = UiKitFactory.CreatePill(
                sortRow,
                "SortPill",
                "Tri : Rareté v",
                SortRowHeight - UiTheme.Space2,
                icon: null,
                border: PanelSurface.SurfaceBorder.Subtle,
                blocksRaycasts: true);

            Button btn = pill.Root.GetComponent<Button>();
            if (btn == null)
                btn = Undo.AddComponent<Button>(pill.Root);
            btn.transition = Selectable.Transition.None;
            Image target = pill.Root.GetComponent<Image>();
            if (target != null)
                btn.targetGraphic = target;

            CollectionSortBar bar = Undo.AddComponent<CollectionSortBar>(pill.Root);
            SerializedObject so = new SerializedObject(bar);
            so.FindProperty("cycleButton").objectReferenceValue = btn;
            so.FindProperty("label").objectReferenceValue = pill.Label;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (pill.Label != null)
            {
                pill.Label.fontSize = UiTypography.Label;
                pill.Label.color = UiTheme.TextPrimary;
            }

            conforme++;
            return bar;
        }

        private static ScrollRect CreateCollectionScroll(
            Transform root,
            Sprite spriteS,
            StringBuilder log,
            ref int failed)
        {
            GameObject scrollGo = new GameObject(
                "CollectionScroll",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect));
            Undo.RegisterCreatedObjectUndo(scrollGo, UndoLabel);
            Undo.SetTransformParent(scrollGo.transform, root, false, UndoLabel);

            Image bg = scrollGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;

            GameObject viewport = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            Undo.RegisterCreatedObjectUndo(viewport, UndoLabel);
            Undo.SetTransformParent(viewport.transform, scrollGo.transform, false, UndoLabel);
            StretchFull((RectTransform)viewport.transform);
            Image vpImg = viewport.GetComponent<Image>();
            vpImg.color = Color.white;
            vpImg.raycastTarget = true;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(content, UndoLabel);
            Undo.SetTransformParent(content.transform, viewport.transform, false, UndoLabel);

            RectTransform contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;

            ScrollRect sr = scrollGo.GetComponent<ScrollRect>();
            sr.viewport = (RectTransform)viewport.transform;
            sr.content = contentRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            log.AppendLine("- CollectionScroll créé (fallback) ✓");
            return sr;
        }

        private static void ConfigureCollectionGrid(
            Transform page,
            Transform content,
            StringBuilder log,
            ref int conforme)
        {
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = Undo.AddComponent<GridLayoutGroup>(content.gameObject);

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            if (csf == null)
                csf = Undo.AddComponent<ContentSizeFitter>(content.gameObject);
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            float pageW = 1080f;
            RectTransform pageRt = page as RectTransform;
            if (pageRt != null && pageRt.rect.width > 1f)
                pageW = pageRt.rect.width;

            float usable = pageW - (3f * UiTheme.Space3) - (2f * UiTheme.Space4);
            float cellW = usable / 4f;
            float cellH = cellW * CardAspect;

            grid.cellSize = new Vector2(cellW, cellH);
            grid.spacing = new Vector2(UiTheme.Space3, UiTheme.Space3);
            grid.padding = new RectOffset(
                Mathf.RoundToInt(UiTheme.Space4),
                Mathf.RoundToInt(UiTheme.Space4),
                Mathf.RoundToInt(UiTheme.Space3),
                Mathf.RoundToInt(UiTheme.Space4));
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            log.AppendLine($"- Grid cell {cellW:0.#}×{cellH:0.#} (pageW={pageW:0.#}) ✓");
            conforme++;
        }

        private static void RebindTeamPageUi(
            TeamPageUI pageUi,
            TeamSlotUI[] dockSlots,
            Transform collectionContent,
            Object cardPrefabRef,
            Object databaseRef,
            Object detailPopupRef,
            CollectionSortBar sortBar,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            SerializedObject so = new SerializedObject(pageUi);
            SerializedProperty slotsProp = so.FindProperty("teamSlots");
            slotsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    dockSlots != null && i < dockSlots.Length ? dockSlots[i] : null;
            }

            so.FindProperty("collectionContainer").objectReferenceValue = collectionContent;
            if (cardPrefabRef != null)
                so.FindProperty("cardPrefab").objectReferenceValue = cardPrefabRef;
            if (databaseRef != null)
                so.FindProperty("characterDatabase").objectReferenceValue = databaseRef;
            if (detailPopupRef != null)
                so.FindProperty("detailPopup").objectReferenceValue = detailPopupRef;
            so.FindProperty("sortBar").objectReferenceValue = sortBar;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pageUi);

            if (dockSlots == null || dockSlots.Length < 4 || dockSlots[0] == null)
            {
                failed++;
                log.AppendLine("- ✗ teamSlots incomplets après rebind");
            }
            else if (collectionContent == null)
            {
                failed++;
                log.AppendLine("- ✗ collectionContainer null");
            }
            else
            {
                conforme++;
                log.AppendLine("- TeamPageUI rebind (slots, content, sortBar, popup) ✓");
            }
        }

        // ═══════════════════════════════════════════
        // CHARACTER CARD PREFAB
        // ═══════════════════════════════════════════

        private static void RebuildCharacterCardPrefab(
            Sprite spriteS,
            Sprite spriteM,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            if (root == null)
            {
                failed++;
                log.AppendLine($"- ✗ Prefab introuvable `{CardPrefabPath}`");
                return;
            }

            try
            {
                // Purge enfants
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

                RectTransform rootRt = (RectTransform)root.transform;
                rootRt.sizeDelta = new Vector2(150f, 150f * CardAspect);

                // Racine = liseré rareté ; fond BgElevated en inset (RadiusS).
                Image rarityBorder = root.GetComponent<Image>();
                if (rarityBorder == null)
                    rarityBorder = root.AddComponent<Image>();
                rarityBorder.sprite = spriteS;
                rarityBorder.type = Image.Type.Sliced;
                rarityBorder.color = CharacterRarityPalette.SR;
                rarityBorder.raycastTarget = true;

                Button btn = root.GetComponent<Button>();
                if (btn == null)
                    btn = root.AddComponent<Button>();
                btn.targetGraphic = rarityBorder;
                btn.transition = Selectable.Transition.None;

                CharacterCardUI cardUi = root.GetComponent<CharacterCardUI>();
                if (cardUi == null)
                    cardUi = root.AddComponent<CharacterCardUI>();

                Image cardBg = CreateChildImage(root.transform, "CardBackground", spriteS);
                StretchWithPadding(cardBg.rectTransform, UiTheme.BorderFocus);
                cardBg.color = UiTheme.BgElevated;
                cardBg.raycastTarget = false;

                // Icon plein cadre (haut → bandeau) — plus de letterbox gris.
                Image icon = CreateChildImage(root.transform, "IconImage", null);
                RectTransform iconRt = icon.rectTransform;
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = new Vector2(UiTheme.BorderFocus, 44f);
                iconRt.offsetMax = new Vector2(-UiTheme.BorderFocus, -UiTheme.BorderFocus);
                icon.preserveAspect = false;
                icon.raycastTarget = false;
                icon.color = Color.white;

                // Badge rareté (haut-droit, déborde 6 px)
                GameObject badgeGo = new GameObject(
                    "BadgeRarity", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeGo.transform.SetParent(root.transform, false);
                RectTransform badgeRt = (RectTransform)badgeGo.transform;
                badgeRt.anchorMin = new Vector2(1f, 1f);
                badgeRt.anchorMax = new Vector2(1f, 1f);
                badgeRt.pivot = new Vector2(1f, 1f);
                badgeRt.sizeDelta = new Vector2(BadgeSize, BadgeSize);
                badgeRt.anchoredPosition = new Vector2(BadgeOverhang, BadgeOverhang);
                Image badgeImg = badgeGo.GetComponent<Image>();
                badgeImg.sprite = spriteS;
                badgeImg.type = Image.Type.Sliced;
                badgeImg.color = CharacterRarityPalette.SR;
                badgeImg.raycastTarget = false;

                TextMeshProUGUI badgeTxt = CreateTmp(
                    badgeGo.transform, "BadgeText", "SR",
                    UiTypography.Caption, UiTheme.TextPrimary);
                badgeTxt.fontStyle = FontStyles.Bold;
                StretchFull(badgeTxt.rectTransform);
                badgeTxt.alignment = TextAlignmentOptions.Center;

                // AwakenDot sous badge
                Image awaken = CreateChildImage(root.transform, "AwakenDot", spriteS);
                RectTransform awakenRt = awaken.rectTransform;
                awakenRt.anchorMin = new Vector2(1f, 1f);
                awakenRt.anchorMax = new Vector2(1f, 1f);
                awakenRt.pivot = new Vector2(1f, 1f);
                awakenRt.sizeDelta = new Vector2(AwakenDotSize, AwakenDotSize);
                awakenRt.anchoredPosition = new Vector2(
                    BadgeOverhang - (BadgeSize - AwakenDotSize) * 0.5f,
                    BadgeOverhang - BadgeSize - UiTheme.Space1);
                awaken.color = UiTheme.AccentGold;
                awaken.raycastTarget = false;
                awaken.gameObject.SetActive(false);

                // BottomBanner : Nv. gauche + rôle ATK/DEF/SUP droite
                Image banner = CreateChildImage(root.transform, "BottomBanner", spriteS);
                RectTransform bannerRt = banner.rectTransform;
                bannerRt.anchorMin = new Vector2(0f, 0f);
                bannerRt.anchorMax = new Vector2(1f, 0f);
                bannerRt.pivot = new Vector2(0.5f, 0f);
                bannerRt.sizeDelta = new Vector2(0f, 44f);
                bannerRt.anchoredPosition = Vector2.zero;
                Color bc = UiTheme.BgElevated;
                bc.a = 0.85f;
                banner.color = bc;
                banner.raycastTarget = false;

                TextMeshProUGUI name = CreateTmp(
                    banner.transform, "NameText", "Nom",
                    UiTypography.Caption, UiTheme.TextPrimary);
                name.gameObject.SetActive(false);

                TextMeshProUGUI level = CreateTmp(
                    banner.transform, "LevelText", "Nv.1",
                    UiTypography.Caption, UiTheme.TextMuted);
                RectTransform levelRt = level.rectTransform;
                levelRt.anchorMin = new Vector2(0f, 0f);
                levelRt.anchorMax = new Vector2(0.55f, 1f);
                levelRt.offsetMin = new Vector2(UiTheme.Space2, 0f);
                levelRt.offsetMax = new Vector2(-UiTheme.Space1, 0f);
                level.alignment = TextAlignmentOptions.MidlineLeft;
                level.verticalAlignment = VerticalAlignmentOptions.Middle;

                TextMeshProUGUI roleLabel = CreateTmp(
                    banner.transform, "RoleLabel", "ATK",
                    UiTypography.Caption, UiTheme.RoleAttacker);
                RectTransform roleLabelRt = roleLabel.rectTransform;
                roleLabelRt.anchorMin = new Vector2(0.45f, 0f);
                roleLabelRt.anchorMax = new Vector2(1f, 1f);
                roleLabelRt.offsetMin = new Vector2(UiTheme.Space1, 0f);
                roleLabelRt.offsetMax = new Vector2(-UiTheme.Space2, 0f);
                roleLabel.fontStyle = FontStyles.Bold;
                roleLabel.alignment = TextAlignmentOptions.MidlineRight;
                roleLabel.verticalAlignment = VerticalAlignmentOptions.Middle;

                // InTeamIndicator
                GameObject inTeam = new GameObject("InTeamIndicator", typeof(RectTransform));
                inTeam.transform.SetParent(root.transform, false);
                StretchFull((RectTransform)inTeam.transform);
                inTeam.SetActive(false);

                Image strip = CreateChildImage(inTeam.transform, "InTeamStrip", null);
                RectTransform stripRt = strip.rectTransform;
                stripRt.anchorMin = new Vector2(0f, 0f);
                stripRt.anchorMax = new Vector2(1f, 0f);
                stripRt.pivot = new Vector2(0.5f, 0f);
                stripRt.sizeDelta = new Vector2(0f, InTeamStripH);
                stripRt.anchoredPosition = Vector2.zero;
                strip.color = UiTheme.AccentAmber;
                strip.sprite = null;
                strip.raycastTarget = false;

                TextMeshProUGUI check = CreateTmp(
                    inTeam.transform, "InTeamCheck", "OK",
                    UiTypography.Caption, UiTheme.AccentAmber);
                check.fontSize = InTeamCheckSize;
                RectTransform checkRt = check.rectTransform;
                checkRt.anchorMin = new Vector2(1f, 0f);
                checkRt.anchorMax = new Vector2(1f, 0f);
                checkRt.pivot = new Vector2(1f, 0f);
                checkRt.sizeDelta = new Vector2(InTeamCheckSize, InTeamCheckSize);
                checkRt.anchoredPosition = new Vector2(-UiTheme.Space1, InTeamStripH + UiTheme.Space1);
                check.alignment = TextAlignmentOptions.Center;

                SerializedObject so = new SerializedObject(cardUi);
                so.FindProperty("cardBackground").objectReferenceValue = cardBg;
                so.FindProperty("iconImage").objectReferenceValue = icon;
                so.FindProperty("rarityBorder").objectReferenceValue = rarityBorder;
                so.FindProperty("badgeRarityImage").objectReferenceValue = badgeImg;
                so.FindProperty("badgeRarityText").objectReferenceValue = badgeTxt;
                so.FindProperty("badgeSprites").arraySize = 3;
                so.FindProperty("badgeSprites").GetArrayElementAtIndex(0).objectReferenceValue = null;
                so.FindProperty("badgeSprites").GetArrayElementAtIndex(1).objectReferenceValue = null;
                so.FindProperty("badgeSprites").GetArrayElementAtIndex(2).objectReferenceValue = null;
                so.FindProperty("awakenDot").objectReferenceValue = awaken;
                so.FindProperty("bottomBanner").objectReferenceValue = banner;
                so.FindProperty("nameText").objectReferenceValue = name;
                so.FindProperty("levelText").objectReferenceValue = level;
                so.FindProperty("roleLabel").objectReferenceValue = roleLabel;
                so.FindProperty("inTeamIndicator").objectReferenceValue = inTeam;
                so.FindProperty("inTeamStrip").objectReferenceValue = strip;
                so.FindProperty("inTeamCheck").objectReferenceValue = check;
                so.FindProperty("cardButton").objectReferenceValue = btn;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
                conforme++;
                log.AppendLine("- CharacterCard.prefab restructuré + CharacterCardUI rebind ✓");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ═══════════════════════════════════════════
        // ASSETS
        // ═══════════════════════════════════════════

        private static void PurgeExclusiveAssets(
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            // Hub.unity : refs PhoneFrame / PresetBar / tabs — retirées par la purge GO.
            // Le scan fichier lit le YAML disque (souvent pas encore sauvé) → on l'ignore
            // volontairement ; on échoue seulement si une REF externe (prefab/autre scène) existe.
            log.AppendLine(
                "- Note : refs `Hub.unity` ignorées (appartenant aux GO purgés PhoneFrame/PresetBar)");

            if (apply)
            {
                Scene hubScene = SceneManager.GetActiveScene();
                if (hubScene.IsValid() && hubScene.name == "Hub" && hubScene.isDirty)
                {
                    EditorSceneManager.SaveScene(hubScene);
                    log.AppendLine("- Hub.unity sauvé avant DELETE assets (YAML disque à jour) ✓");
                }
            }

            for (int i = 0; i < ExclusiveTeampageAssets.Length; i++)
            {
                string path = ExclusiveTeampageAssets[i];
                if (!File.Exists(path) && !File.Exists(path + ".meta"))
                {
                    conforme++;
                    log.AppendLine($"- déjà absent `{Path.GetFileName(path)}` ✓");
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                {
                    conforme++;
                    log.AppendLine($"- GUID vide `{path}` (skip) ✓");
                    continue;
                }

                List<string> externalRefs = FindGuidReferences(guid, path, ignoreHubScene: true);
                if (externalRefs.Count > 0)
                {
                    failed++;
                    log.AppendLine(
                        $"- ✗ REF externe pour `{Path.GetFileName(path)}` → ne purge pas");
                    for (int r = 0; r < externalRefs.Count && r < 5; r++)
                        log.AppendLine($"    · {externalRefs[r]}");
                    continue;
                }

                if (!apply)
                {
                    todo++;
                    log.AppendLine(
                        $"- [DRY] DELETE asset `{Path.GetFileName(path)}` (après purge GO) — À FAIRE");
                }
                else
                {
                    if (AssetDatabase.DeleteAsset(path))
                    {
                        conforme++;
                        log.AppendLine($"- DELETE `{path}` ✓");
                    }
                    else
                    {
                        failed++;
                        log.AppendLine($"- ✗ Échec delete `{path}`");
                    }
                }
            }
        }

        /// <summary>
        /// Cherche des refs GUID hors self. Si ignoreHubScene : saute Hub.unity
        /// (refs attendues sur PhoneFrame / presets, purgées avec les GO).
        /// </summary>
        private static List<string> FindGuidReferences(
            string guid,
            string selfPath,
            bool ignoreHubScene)
        {
            var hits = new List<string>();
            string[] all = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < all.Length; i++)
            {
                string p = all[i];
                if (p == selfPath || p == selfPath + ".meta")
                    continue;
                if (ignoreHubScene && IsHubScenePath(p))
                    continue;
                if (!(p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".asset")
                      || p.EndsWith(".mat") || p.EndsWith(".controller")))
                    continue;

                try
                {
                    string text = File.ReadAllText(p);
                    if (text.Contains(guid))
                        hits.Add(p);
                }
                catch
                {
                    // ignore
                }
            }

            return hits;
        }

        private static bool IsHubScenePath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
            string n = assetPath.Replace('\\', '/');
            return n.EndsWith("/Hub.unity", System.StringComparison.OrdinalIgnoreCase)
                   || n.Equals("Assets/_Project/Scenes/Hub.unity",
                       System.StringComparison.OrdinalIgnoreCase);
        }

        private static void VerifyGachaPageResolution(StringBuilder log, ref int conforme, ref int failed)
        {
            HubManager hub = Object.FindObjectOfType<HubManager>();
            if (hub == null || hub.AllPages == null || hub.AllPages.Length == 0)
            {
                failed++;
                log.AppendLine("- ✗ HubManager.AllPages vide — fallback gacha non fiable");
                return;
            }

            bool foundEquipe = false;
            for (int i = 0; i < hub.AllPages.Length; i++)
            {
                GameObject p = hub.AllPages[i];
                if (p != null && p.name == PageName)
                    foundEquipe = true;
            }

            if (foundEquipe)
            {
                conforme++;
                log.AppendLine(
                    "- HubManager.pages contient PageEquipe ✓ "
                    + "(GachaAnimationController résout via HubManager — bug Find post-1.2 soldé)");
            }
            else
            {
                failed++;
                log.AppendLine("- ✗ PageEquipe absente de HubManager.pages");
            }

            // Confirme que canvas.Find direct est bien mort
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform direct = canvas.transform.Find(PageName);
                if (direct == null)
                {
                    conforme++;
                    log.AppendLine(
                        "- Confirmé : canvas.Find(\"PageEquipe\") = null (sous PageContainer) ✓");
                }
                else
                {
                    log.AppendLine("- canvas.Find(\"PageEquipe\") fonctionne encore (page racine)");
                }
            }
        }

        private static void EnsurePageBgDeep(
            Transform page,
            Sprite spriteL,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            Transform existing = page.Find("PageBgDeep");
            if (existing != null)
            {
                conforme++;
                return;
            }

            GameObject go = new GameObject(
                "PageBgDeep", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, page, false, UndoLabel);
            go.transform.SetAsFirstSibling();
            StretchFull((RectTransform)go.transform);
            Image img = go.GetComponent<Image>();
            img.sprite = spriteL;
            img.type = Image.Type.Sliced;
            img.color = UiTheme.BgDeep;
            img.raycastTarget = false;
            log.AppendLine("- Image PageBgDeep créée ✓");
            conforme++;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static void BindPanelSurface(
            PanelSurface surface,
            PanelSurface.SurfaceVariant variant,
            PanelSurface.SurfaceBorder border,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL)
        {
            SerializedObject surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("variant").enumValueIndex = (int)variant;
            surfaceSo.FindProperty("borderStyle").enumValueIndex = (int)border;
            surfaceSo.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            surfaceSo.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            surfaceSo.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            surfaceSo.FindProperty("blocksRaycasts").boolValue = false;
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();
            surface.ApplyStyle();
        }

        private static void IgnoreLayoutOnFill(Transform root)
        {
            Transform fill = root.Find("Fill");
            if (fill == null)
                return;
            LayoutElement le = fill.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(fill.gameObject);
            le.ignoreLayout = true;
        }

        private static Image CreateChildImage(Transform parent, string name, Sprite sprite)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }

            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            string text,
            float size,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
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

        private static void StretchWithPadding(RectTransform rt, float pad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        private static ScrollRect FindScrollRect(Transform root)
        {
            return root != null ? root.GetComponentInChildren<ScrollRect>(true) : null;
        }

        private static bool NameMatches(string actual, string expected)
        {
            if (actual == null || expected == null)
                return false;
            if (actual == expected)
                return true;
            return string.Equals(actual.Trim(), expected.Trim(), System.StringComparison.Ordinal);
        }

        private static bool IsUnder(Transform t, List<Transform> ancestors)
        {
            for (int i = 0; i < ancestors.Count; i++)
            {
                Transform a = ancestors[i];
                if (a == null)
                    continue;
                Transform cur = t;
                while (cur != null)
                {
                    if (cur == a)
                        return true;
                    cur = cur.parent;
                }
            }

            return false;
        }

        private static string GetPath(Transform t)
        {
            if (t == null)
                return "?";
            var sb = new StringBuilder(t.name);
            Transform p = t.parent;
            while (p != null)
            {
                sb.Insert(0, p.name + "/");
                p = p.parent;
            }

            return sb.ToString();
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
            if (NameMatches(root.name, name))
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform f = FindDeep(root.GetChild(i), name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private static Transform FindDeepTrimmed(Transform root, string name)
        {
            return FindDeep(root, name);
        }

        private static void AppendCounter(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine("───────────────────────────────────────────");
            log.AppendLine($" À FAIRE={todo} | CONFORMES={conforme} | ÉCHECS={failed}");
            log.AppendLine("───────────────────────────────────────────");
        }
    }
}
#endif
