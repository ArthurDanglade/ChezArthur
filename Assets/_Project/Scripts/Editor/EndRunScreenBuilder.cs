#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Monte d'un coup la hiérarchie scroll + wiring DefeatUI pour l'écran de fin de run.
    /// Idempotent : réutilise les textes/boutons existants, réorganise si besoin.
    /// </summary>
    public static class EndRunScreenBuilder
    {
        private const string ScrollName = "EndRunRankingScroll";
        private const string ViewportName = "Viewport";
        private const string ContentName = "Content";
        private const string ContentFrameName = "EndRunContentFrame";
        private const string ButtonsRowName = "EndRunButtonsRow";
        private const string BossesTextName = "BossesDefeatedText";
        private const string RankingContainerName = "RankingContainer";

        private const string EntryPrefabPath = "Assets/_Project/Prefabs/UI/EndRunCharacterEntryUI.prefab";
        private const string CharacterDatabasePath =
            "Assets/_Project/ScriptableObjects/Characters/CharacterDatabase.asset";

        private const int SPACING = 20;
        private const int PADDING_LATERAL = 56;
        private const float FRAME_INSET_LEFT = 44f;
        private const float FRAME_INSET_RIGHT = 44f;
        private const float FRAME_INSET_TOP = 72f;
        private const float FRAME_INSET_BOTTOM = 48f;
        private const float BUTTON_ROW_HEIGHT = 96f;
        private const float BUTTON_ROW_BOTTOM_MARGIN = 56f;
        private const float SCROLL_BOTTOM_GAP = 24f;
        private const float PANEL_OVERLAY_ALPHA = 0.97f;
        private const float SCROLL_PANEL_ALPHA = 0.94f;

        [MenuItem("Chez Arthur/UI/Monter écran fin de run (DefeatUI)")]
        public static void BuildEndRunScreen()
        {
            DefeatUI defeatUi = Object.FindObjectOfType<DefeatUI>(true);
            if (defeatUi == null)
            {
                Debug.LogError("[EndRunScreenBuilder] DefeatUI introuvable — ouvrir la scène de combat.");
                return;
            }

            SerializedObject defeatSo = new SerializedObject(defeatUi);
            GameObject panelRoot = defeatSo.FindProperty("panelRoot")?.objectReferenceValue as GameObject;
            if (panelRoot == null)
            {
                Debug.LogError("[EndRunScreenBuilder] panelRoot non assigné sur DefeatUI.");
                return;
            }

            RectTransform panelRt = panelRoot.GetComponent<RectTransform>();
            if (panelRt == null)
            {
                Debug.LogError("[EndRunScreenBuilder] panelRoot sans RectTransform.");
                return;
            }

            Undo.SetCurrentGroupName("Monter écran fin de run");
            int undoGroup = Undo.GetCurrentGroup();

            ConfigurePanelOverlay(panelRt);
            RectTransform frameRt = EnsureContentFrame(panelRt);
            CanvasGroup panelCanvasGroup = EnsureCanvasGroup(panelRt.gameObject);
            CanvasGroup frameCanvasGroup = EnsureCanvasGroup(frameRt.gameObject);
            ScrollRect scrollRect = EnsureScrollHierarchy(frameRt, out RectTransform contentRt, out RectTransform viewportRt);

            TextMeshProUGUI titleText = GetOrCreateLine(
                defeatSo, panelRt, contentRt, "titleText", "FIN DE GAME", 52f, FontStyles.Bold,
                TextAlignmentOptions.Center, 72f, UiTheme.TextPrimary);
            TextMeshProUGUI stageText = GetOrCreateLine(
                defeatSo, panelRt, contentRt, "stageReachedText", "Étage atteint : 1", 32f, FontStyles.Normal,
                TextAlignmentOptions.Left, 44f, UiTheme.TextSecondary);
            TextMeshProUGUI talsText = GetOrCreateLine(
                defeatSo, panelRt, contentRt, "talsEarnedText", "Tals remportés : 0", 32f, FontStyles.Normal,
                TextAlignmentOptions.Left, 44f, UiTheme.Gold);
            TextMeshProUGUI bossesText = GetOrCreateLine(
                defeatSo, panelRt, contentRt, "bossesDefeatedText", "Boss vaincus : 0", 32f, FontStyles.Normal,
                TextAlignmentOptions.Left, 44f, UiTheme.TextSecondary, BossesTextName);

            HideBonusLine(defeatSo);

            TMP_Text superHits = defeatSo.FindProperty("_superHitsText")?.objectReferenceValue as TMP_Text;
            if (superHits != null)
            {
                ReparentUnderContent(superHits.transform, contentRt);
                EnsureLineLayout(superHits as TextMeshProUGUI ?? superHits.GetComponent<TextMeshProUGUI>(), 44f);
                if (superHits is TextMeshProUGUI superHitsTmp)
                {
                    superHitsTmp.color = UiTheme.AccentSection;
                    superHitsTmp.alignment = TextAlignmentOptions.Left;
                }
            }

            Transform rankingContainer = EnsureRankingContainer(contentRt);

            ApplyContentOrder(contentRt, new[]
            {
                titleText.transform,
                stageText.transform,
                talsText.transform,
                bossesText.transform,
                superHits != null ? superHits.transform : null,
                rankingContainer
            });

            EnsureButtonsOnPanel(defeatSo, frameRt, contentRt);
            WireCombatHudRoots(defeatSo);

            EndRunCharacterEntryUI entryPrefab =
                AssetDatabase.LoadAssetAtPath<EndRunCharacterEntryUI>(EntryPrefabPath);
            CharacterDatabase characterDatabase =
                AssetDatabase.LoadAssetAtPath<CharacterDatabase>(CharacterDatabasePath);

            UiGen.Wire(defeatSo, "rankingContainer", rankingContainer);
            UiGen.Wire(defeatSo, "entryPrefab", entryPrefab);
            UiGen.Wire(defeatSo, "characterDatabase", characterDatabase);
            UiGen.Wire(defeatSo, "panelCanvasGroup", panelCanvasGroup);
            UiGen.Wire(defeatSo, "contentCanvasGroup", frameCanvasGroup);
            UiGen.Wire(defeatSo, "contentFrameRect", frameRt);
            defeatSo.ApplyModifiedProperties();

            ConfigureScrollRect(scrollRect);
            ConfigureViewport(viewportRt);
            ConfigureContent(contentRt);
            ConfigureRankingContainerVlg(rankingContainer);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            if (entryPrefab == null)
            {
                Debug.LogWarning(
                    $"[EndRunScreenBuilder] Prefab manquant : {EntryPrefabPath}. " +
                    "Lancez « Regénérer prefab EndRunCharacterEntry (écrase) ».");
            }

            if (characterDatabase == null)
            {
                Debug.LogWarning(
                    $"[EndRunScreenBuilder] CharacterDatabase introuvable : {CharacterDatabasePath}");
            }

            Debug.Log(
                "[EndRunScreenBuilder] Montage terminé. Lancez « Écran fin de run — Audit montage » pour valider.");
            Selection.activeGameObject = panelRoot;
        }

        private static void ConfigurePanelOverlay(RectTransform panelRt)
        {
            Image panelImage = panelRt.GetComponent<Image>();
            if (panelImage == null)
                return;

            Undo.RecordObject(panelImage, "Configurer overlay DefeatPanel");
            Color overlay = UiTheme.SurfaceGlobal;
            overlay.a = PANEL_OVERLAY_ALPHA;
            panelImage.color = overlay;
            if (panelImage.sprite == null)
                panelImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            panelImage.type = Image.Type.Sliced;
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject go)
        {
            CanvasGroup group = go.GetComponent<CanvasGroup>();
            if (group == null)
                group = Undo.AddComponent<CanvasGroup>(go);
            Undo.RecordObject(group, "Configurer CanvasGroup fin de run");
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            return group;
        }

        private static RectTransform EnsureContentFrame(RectTransform panelRt)
        {
            Transform existing = panelRt.Find(ContentFrameName);
            GameObject frameGo;
            if (existing != null)
            {
                frameGo = existing.gameObject;
            }
            else
            {
                frameGo = new GameObject(ContentFrameName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(frameGo, "Créer EndRunContentFrame");
                frameGo.transform.SetParent(panelRt, false);
            }

            RectTransform frameRt = frameGo.GetComponent<RectTransform>();
            Undo.RecordObject(frameRt, "Configurer marges EndRunContentFrame");
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.offsetMin = new Vector2(FRAME_INSET_LEFT, FRAME_INSET_BOTTOM);
            frameRt.offsetMax = new Vector2(-FRAME_INSET_RIGHT, -FRAME_INSET_TOP);
            return frameRt;
        }

        private static ScrollRect EnsureScrollHierarchy(
            RectTransform frameRt,
            out RectTransform contentRt,
            out RectTransform viewportRt)
        {
            Transform existing = frameRt.Find(ScrollName);
            if (existing == null)
            {
                RectTransform panelRt = frameRt.parent as RectTransform;
                if (panelRt != null)
                    existing = panelRt.Find(ScrollName);
            }
            GameObject scrollGo;
            float scrollBottomReserve = BUTTON_ROW_HEIGHT + BUTTON_ROW_BOTTOM_MARGIN + SCROLL_BOTTOM_GAP;
            if (existing != null)
            {
                scrollGo = existing.gameObject;
                if (scrollGo.transform.parent != frameRt)
                    Undo.SetTransformParent(scrollGo.transform, frameRt, "Reparent scroll sous frame");
            }
            else
            {
                scrollGo = new GameObject(ScrollName, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                Undo.RegisterCreatedObjectUndo(scrollGo, "Créer EndRunRankingScroll");
                scrollGo.transform.SetParent(frameRt, false);
            }

            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            Undo.RecordObject(scrollRt, "Configurer zone scroll fin de run");
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(0f, scrollBottomReserve);
            scrollRt.offsetMax = Vector2.zero;

            ConfigureScrollPanelVisual(scrollGo.GetComponent<Image>());

            ScrollRect scrollRect = scrollGo.GetComponent<ScrollRect>();

            Transform viewportTransform = scrollGo.transform.Find(ViewportName);
            if (viewportTransform == null)
            {
                GameObject viewportGo = new GameObject(ViewportName, typeof(RectTransform), typeof(RectMask2D), typeof(Image));
                Undo.RegisterCreatedObjectUndo(viewportGo, "Créer Viewport");
                viewportRt = viewportGo.GetComponent<RectTransform>();
                viewportRt.SetParent(scrollGo.transform, false);
                StretchFull(viewportRt);
                Image viewportImage = viewportGo.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
                viewportImage.raycastTarget = true;
            }
            else
            {
                viewportRt = viewportTransform as RectTransform;
                if (viewportRt.GetComponent<RectMask2D>() == null)
                    Undo.AddComponent<RectMask2D>(viewportRt.gameObject);
            }

            Transform contentTransform = viewportRt.Find(ContentName);
            if (contentTransform == null)
            {
                GameObject contentGo = new GameObject(ContentName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(contentGo, "Créer Content");
                contentRt = contentGo.GetComponent<RectTransform>();
                contentRt.SetParent(viewportRt, false);
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = new Vector2(0f, 0f);
            }
            else
            {
                contentRt = contentTransform as RectTransform;
            }

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            return scrollRect;
        }

        private static void ConfigureScrollPanelVisual(Image scrollBg)
        {
            if (scrollBg == null)
                return;

            Undo.RecordObject(scrollBg, "Configurer panneau scroll fin de run");
            scrollBg.sprite = UiGen.Card;
            scrollBg.type = Image.Type.Sliced;
            Color panel = UiTheme.SurfaceBar;
            panel.a = SCROLL_PANEL_ALPHA;
            scrollBg.color = panel;
            scrollBg.raycastTarget = true;
        }

        private static TextMeshProUGUI GetOrCreateLine(
            SerializedObject defeatSo,
            RectTransform panelRt,
            RectTransform contentRt,
            string fieldName,
            string placeholder,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            float preferredHeight,
            Color color,
            string explicitName = null)
        {
            TextMeshProUGUI existing =
                defeatSo.FindProperty(fieldName)?.objectReferenceValue as TextMeshProUGUI;
            if (existing != null)
            {
                ReparentUnderContent(existing.transform, contentRt);
                EnsureLineLayout(existing, preferredHeight);
                existing.color = color;
                existing.alignment = alignment;
                return existing;
            }

            string objectName = explicitName ?? fieldName;
            GameObject lineGo = new GameObject(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(lineGo, $"Créer {objectName}");
            RectTransform lineRt = lineGo.GetComponent<RectTransform>();
            lineRt.SetParent(contentRt, false);
            StretchHorizontal(lineRt, preferredHeight);

            TextMeshProUGUI tmp = lineGo.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = UiGen.LoadFont();
            if (font != null)
                tmp.font = font;
            tmp.text = placeholder;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;

            EnsureLineLayout(tmp, preferredHeight);
            UiGen.Wire(defeatSo, fieldName, tmp);
            return tmp;
        }

        private static Transform EnsureRankingContainer(RectTransform contentRt)
        {
            Transform existing = contentRt.Find(RankingContainerName);
            if (existing != null)
                return existing;

            GameObject containerGo = new GameObject(RankingContainerName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(containerGo, "Créer RankingContainer");
            RectTransform containerRt = containerGo.GetComponent<RectTransform>();
            containerRt.SetParent(contentRt, false);
            StretchHorizontal(containerRt, 400f);
            return containerRt;
        }

        private static void HideBonusLine(SerializedObject defeatSo)
        {
            TextMeshProUGUI bonus =
                defeatSo.FindProperty("bonusCountText")?.objectReferenceValue as TextMeshProUGUI;
            if (bonus == null)
                return;

            Undo.RecordObject(bonus.gameObject, "Masquer BonusCountText");
            bonus.gameObject.SetActive(false);
        }

        private static void EnsureButtonsOnPanel(
            SerializedObject defeatSo,
            RectTransform frameRt,
            RectTransform contentRt)
        {
            RectTransform buttonsRow = EnsureButtonsRow(frameRt);
            ConfigureButtonInRow(
                defeatSo.FindProperty("buttonReturnHub")?.objectReferenceValue as Button,
                buttonsRow, contentRt, 0, null);
            ConfigureButtonInRow(
                defeatSo.FindProperty("retryButton")?.objectReferenceValue as Button,
                buttonsRow, contentRt, 1, "Réessayer");
        }

        private static RectTransform EnsureButtonsRow(RectTransform frameRt)
        {
            Transform existing = frameRt.Find(ButtonsRowName);
            GameObject rowGo;
            if (existing != null)
            {
                rowGo = existing.gameObject;
            }
            else
            {
                rowGo = new GameObject(ButtonsRowName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(rowGo, "Créer EndRunButtonsRow");
                rowGo.transform.SetParent(frameRt, false);
            }

            RectTransform rowRt = rowGo.GetComponent<RectTransform>();
            Undo.RecordObject(rowRt, "Configurer barre boutons fin de run");
            rowRt.anchorMin = new Vector2(0f, 0f);
            rowRt.anchorMax = new Vector2(1f, 0f);
            rowRt.pivot = new Vector2(0.5f, 0f);
            rowRt.sizeDelta = new Vector2(0f, BUTTON_ROW_HEIGHT);
            rowRt.anchoredPosition = new Vector2(0f, BUTTON_ROW_BOTTOM_MARGIN);

            HorizontalLayoutGroup hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
                hlg = Undo.AddComponent<HorizontalLayoutGroup>(rowGo);
            Undo.RecordObject(hlg, "Configurer HLG boutons fin de run");
            hlg.spacing = 20f;
            hlg.padding = new RectOffset(32, 32, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;

            return rowRt;
        }

        private static void ConfigureButtonInRow(
            Button button,
            RectTransform buttonsRow,
            RectTransform contentRt,
            int siblingIndex,
            string labelIfMissing)
        {
            if (button == null)
                return;

            Transform t = button.transform;
            if (IsDescendantOf(t, contentRt))
                Undo.SetTransformParent(t, buttonsRow, "Sortir bouton du Content");

            if (t.parent != buttonsRow)
                Undo.SetTransformParent(t, buttonsRow, "Reparent bouton dans barre horizontale");

            Undo.RecordObject(t, "Réordonner bouton fin de run");
            t.SetSiblingIndex(siblingIndex);

            RectTransform rt = t as RectTransform;
            if (rt == null)
                return;

            Undo.RecordObject(rt, "Configurer bouton fin de run");
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            LayoutElement le = t.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(t.gameObject);
            Undo.RecordObject(le, "LayoutElement bouton fin de run");
            le.minHeight = BUTTON_ROW_HEIGHT;
            le.preferredHeight = BUTTON_ROW_HEIGHT;
            le.flexibleWidth = 1f;
            le.preferredWidth = 0f;

            if (!string.IsNullOrEmpty(labelIfMissing))
            {
                TextMeshProUGUI label = t.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null && string.IsNullOrWhiteSpace(label.text))
                    label.text = labelIfMissing;
            }
        }

        private static void WireCombatHudRoots(SerializedObject defeatSo)
        {
            var roots = new List<GameObject>();
            TryAddActiveRoot(roots, "TeamPanel");
            TryAddActiveRoot(roots, "SynergyHud");
            TryAddActiveRoot(roots, "TalsGroup");
            TryAddActiveRoot(roots, "MenuContainer");

            SerializedProperty prop = defeatSo.FindProperty("combatHudRootsToHide");
            if (prop == null)
            {
                Debug.LogWarning("[EndRunScreenBuilder] combatHudRootsToHide introuvable sur DefeatUI.");
                return;
            }

            prop.arraySize = roots.Count;
            for (int i = 0; i < roots.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = roots[i];
        }

        private static void TryAddActiveRoot(List<GameObject> roots, string objectName)
        {
            GameObject[] all = Object.FindObjectsOfType<GameObject>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name != objectName)
                    continue;
                if (!roots.Contains(all[i]))
                    roots.Add(all[i]);
            }
        }

        private static void ApplyContentOrder(RectTransform contentRt, Transform[] ordered)
        {
            int index = 0;
            for (int i = 0; i < ordered.Length; i++)
            {
                if (ordered[i] == null)
                    continue;
                Undo.RecordObject(ordered[i], "Réordonner Content fin de run");
                ordered[i].SetSiblingIndex(index);
                index++;
            }
        }

        private static void ReparentUnderContent(Transform child, RectTransform contentRt)
        {
            if (child == null || child.parent == contentRt)
                return;
            Undo.SetTransformParent(child, contentRt, "Reparent sous Content fin de run");
        }

        private static void EnsureLineLayout(TextMeshProUGUI tmp, float preferredHeight)
        {
            if (tmp == null)
                return;

            RectTransform rt = tmp.rectTransform;
            StretchHorizontal(rt, preferredHeight);
            LayoutElement le = tmp.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(tmp.gameObject);
            le.minHeight = preferredHeight;
            le.preferredHeight = preferredHeight;
            le.flexibleWidth = 1f;
        }

        private static void ConfigureScrollRect(ScrollRect scrollRect)
        {
            Undo.RecordObject(scrollRect, "Configurer ScrollRect fin de run");
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;
        }

        private static void ConfigureViewport(RectTransform viewportRt)
        {
            if (viewportRt.GetComponent<RectMask2D>() == null)
                Undo.AddComponent<RectMask2D>(viewportRt.gameObject);
        }

        private static void ConfigureContent(RectTransform contentRt)
        {
            VerticalLayoutGroup vlg = contentRt.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = Undo.AddComponent<VerticalLayoutGroup>(contentRt.gameObject);
            Undo.RecordObject(vlg, "Configurer VLG Content fin de run");
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = SPACING;
            vlg.padding.left = PADDING_LATERAL;
            vlg.padding.right = PADDING_LATERAL;
            vlg.padding.top = 40;
            vlg.padding.bottom = 40;
            vlg.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter csf = contentRt.GetComponent<ContentSizeFitter>();
            if (csf == null)
                csf = Undo.AddComponent<ContentSizeFitter>(contentRt.gameObject);
            Undo.RecordObject(csf, "Configurer CSF Content fin de run");
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Undo.RecordObject(contentRt, "Ancres Content fin de run");
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
        }

        private static void ConfigureRankingContainerVlg(Transform rankingContainer)
        {
            if (rankingContainer == null)
                return;

            RemoveIfPresent<ContentSizeFitter>(rankingContainer.gameObject);
            RemoveIfPresent<LayoutElement>(rankingContainer.gameObject);
            RemoveIfPresent<HorizontalLayoutGroup>(rankingContainer.gameObject);
            RemoveIfPresent<GridLayoutGroup>(rankingContainer.gameObject);

            VerticalLayoutGroup vlg = rankingContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = Undo.AddComponent<VerticalLayoutGroup>(rankingContainer.gameObject);

            Undo.RecordObject(vlg, "Configurer VLG RankingContainer");
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = SPACING;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.childAlignment = TextAnchor.UpperCenter;
        }

        private static void RemoveIfPresent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component != null)
                Undo.DestroyObjectImmediate(component);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void StretchHorizontal(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }

        private static bool IsDescendantOf(Transform child, Transform ancestor)
        {
            Transform current = child;
            while (current != null)
            {
                if (current == ancestor)
                    return true;
                current = current.parent;
            }

            return false;
        }
    }
}
#endif
