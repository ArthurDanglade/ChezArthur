#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.Gameplay;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Monte le bandeau d'initiative (G1-P2) sous le canvas HUD — idempotent, Undo-safe.
    /// </summary>
    public static class InitiativeBannerUIBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string PanelName = "InitiativeBannerPanel";
        private const int SlotCount = 5;
        private const float SlotSize = 56f;
        private const float IconSize = 48f;
        private const float SeparatorWidth = 2f;
        private const float SeparatorHeight = 48f;
        private const float DefaultAnchoredY = -100f;
        private const float LayoutSpacing = 6f;

        private static readonly Color DefaultAllyFrame = new Color(62f / 255f, 107f / 255f, 143f / 255f, 1f);
        private static readonly Color DefaultEnemyFrame = new Color(143f / 255f, 62f / 255f, 62f / 255f, 1f);
        private static readonly Color SeparatorColor = new Color(1f, 1f, 1f, 0.6f);

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI/Monter InitiativeBanner (G1-P2)")]
        public static void Build()
        {
            Undo.SetCurrentGroupName("Monter InitiativeBanner (G1-P2)");
            int undoGroup = Undo.GetCurrentGroup();

            Canvas canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogError("[InitiativeBannerUIBuilder] Aucun Canvas HUD trouvé dans la scène.");
                return;
            }

            InitiativeBannerUI banner = EnsureBanner(canvas);
            WireBanner(banner);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            if (banner != null)
                Selection.activeGameObject = banner.gameObject;

            Debug.Log(
                "[InitiativeBannerUIBuilder] Montage terminé — " +
                $"panneau `{PanelName}`, {SlotCount} pastilles, TurnManager câblé.");
        }

        // ═══════════════════════════════════════════
        // CANVAS / ENSURE
        // ═══════════════════════════════════════════

        private static Canvas FindMainCanvas()
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].renderMode != RenderMode.WorldSpace)
                    return canvases[i];
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static InitiativeBannerUI EnsureBanner(Canvas canvas)
        {
            InitiativeBannerUI existing = Object.FindObjectOfType<InitiativeBannerUI>(true);
            if (existing != null)
            {
                UpgradeLayout(existing);
                EnsureSlots(existing.transform as RectTransform);
                return existing;
            }

            Sprite card = UiGen.Card;

            GameObject panelGo = new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(HorizontalLayoutGroup),
                typeof(InitiativeBannerUI));
            Undo.RegisterCreatedObjectUndo(panelGo, "Créer InitiativeBannerPanel");
            panelGo.transform.SetParent(canvas.transform, false);

            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            ConfigurePanelRect(panelRt);

            CanvasGroup group = panelGo.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            HorizontalLayoutGroup layout = panelGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = LayoutSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);

            for (int i = 0; i < SlotCount; i++)
                CreateSlot(panelRt, i, card);

            panelGo.SetActive(true);
            return panelGo.GetComponent<InitiativeBannerUI>();
        }

        private static void UpgradeLayout(InitiativeBannerUI ui)
        {
            if (ui == null)
                return;

            RectTransform panelRt = ui.transform as RectTransform;
            if (panelRt == null)
                return;

            Undo.RecordObject(panelRt, "Upgrade InitiativeBanner layout");
            ConfigurePanelRect(panelRt);

            CanvasGroup group = ui.GetComponent<CanvasGroup>();
            if (group != null)
            {
                Undo.RecordObject(group, "Upgrade InitiativeBanner CanvasGroup");
                group.blocksRaycasts = false;
                group.interactable = false;
            }

            HorizontalLayoutGroup layout = ui.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = Undo.AddComponent<HorizontalLayoutGroup>(ui.gameObject);

            Undo.RecordObject(layout, "Upgrade InitiativeBanner layout group");
            layout.spacing = LayoutSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static void ConfigurePanelRect(RectTransform panelRt)
        {
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, DefaultAnchoredY);
            panelRt.sizeDelta = new Vector2(
                SlotCount * SlotSize + (SlotCount - 1) * LayoutSpacing + 8f,
                SlotSize + 12f);
            panelRt.SetAsLastSibling();
        }

        private static void EnsureSlots(RectTransform panelRt)
        {
            if (panelRt == null)
                return;

            Sprite card = UiGen.Card;
            for (int i = 0; i < SlotCount; i++)
            {
                string slotName = $"InitiativeSlot_{i}";
                Transform existing = panelRt.Find(slotName);
                if (existing != null)
                {
                    UpgradeSlot(existing as RectTransform, card);
                    continue;
                }

                CreateSlot(panelRt, i, card);
            }
        }

        // ═══════════════════════════════════════════
        // PASTILLES
        // ═══════════════════════════════════════════

        private static void CreateSlot(RectTransform parent, int index, Sprite card)
        {
            GameObject slotGo = new GameObject(
                $"InitiativeSlot_{index}",
                typeof(RectTransform),
                typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(slotGo, "Créer InitiativeSlot");
            slotGo.transform.SetParent(parent, false);

            RectTransform slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(SlotSize, SlotSize);

            LayoutElement le = slotGo.GetComponent<LayoutElement>();
            le.preferredWidth = SlotSize;
            le.preferredHeight = SlotSize;
            le.minWidth = SlotSize;
            le.minHeight = SlotSize;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            // Cadre
            GameObject frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(slotRt, false);
            RectTransform frameRt = frameGo.GetComponent<RectTransform>();
            StretchFull(frameRt);
            Image frameImg = frameGo.GetComponent<Image>();
            frameImg.sprite = card;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = DefaultAllyFrame;
            frameImg.raycastTarget = false;

            // Icône
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(slotRt, false);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(IconSize, IconSize);
            iconRt.anchoredPosition = Vector2.zero;
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = null;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.enabled = false;

            // Séparateur (côté gauche, désactivé)
            GameObject sepGo = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            sepGo.transform.SetParent(slotRt, false);
            RectTransform sepRt = sepGo.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 0.5f);
            sepRt.anchorMax = new Vector2(0f, 0.5f);
            sepRt.pivot = new Vector2(1f, 0.5f);
            sepRt.sizeDelta = new Vector2(SeparatorWidth, SeparatorHeight);
            sepRt.anchoredPosition = new Vector2(-LayoutSpacing * 0.5f, 0f);
            Image sepImg = sepGo.GetComponent<Image>();
            sepImg.sprite = card;
            sepImg.type = Image.Type.Simple;
            sepImg.color = SeparatorColor;
            sepImg.raycastTarget = false;
            sepGo.SetActive(false);

            slotGo.SetActive(false);
        }

        private static void UpgradeSlot(RectTransform slotRt, Sprite card)
        {
            if (slotRt == null)
                return;

            Undo.RecordObject(slotRt, "Upgrade InitiativeSlot");
            slotRt.sizeDelta = new Vector2(SlotSize, SlotSize);

            LayoutElement le = slotRt.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(slotRt.gameObject);
            le.preferredWidth = SlotSize;
            le.preferredHeight = SlotSize;
            le.minWidth = SlotSize;
            le.minHeight = SlotSize;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            Image frame = slotRt.Find("Frame")?.GetComponent<Image>();
            if (frame != null)
            {
                Undo.RecordObject(frame, "Upgrade slot frame");
                if (frame.sprite == null)
                    frame.sprite = card;
                frame.type = Image.Type.Sliced;
                frame.raycastTarget = false;
            }

            Image icon = slotRt.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                Undo.RecordObject(icon, "Upgrade slot icon");
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            Transform sep = slotRt.Find("Separator");
            if (sep != null)
            {
                Image sepImg = sep.GetComponent<Image>();
                if (sepImg != null)
                {
                    Undo.RecordObject(sepImg, "Upgrade slot separator");
                    sepImg.raycastTarget = false;
                    sepImg.color = SeparatorColor;
                }
            }
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        // ═══════════════════════════════════════════
        // CÂBLAGE
        // ═══════════════════════════════════════════

        private static void WireBanner(InitiativeBannerUI banner)
        {
            if (banner == null)
                return;

            TurnManager tm = Object.FindObjectOfType<TurnManager>(true);
            CanvasGroup group = banner.GetComponent<CanvasGroup>();
            RectTransform panelRt = banner.transform as RectTransform;

            SerializedObject so = new SerializedObject(banner);
            so.FindProperty("turnManager").objectReferenceValue = tm;
            so.FindProperty("canvasGroup").objectReferenceValue = group;
            so.FindProperty("panelAnchoredY").floatValue = DefaultAnchoredY;

            SerializedProperty allyColor = so.FindProperty("allyFrameColor");
            if (allyColor != null)
                allyColor.colorValue = DefaultAllyFrame;
            SerializedProperty enemyColor = so.FindProperty("enemyFrameColor");
            if (enemyColor != null)
                enemyColor.colorValue = DefaultEnemyFrame;

            SerializedProperty slotsProp = so.FindProperty("slots");
            slotsProp.arraySize = SlotCount;

            for (int i = 0; i < SlotCount; i++)
            {
                Transform slotT = panelRt != null ? panelRt.Find($"InitiativeSlot_{i}") : null;
                SerializedProperty elem = slotsProp.GetArrayElementAtIndex(i);

                RectTransform root = slotT as RectTransform;
                Image frame = slotT != null ? slotT.Find("Frame")?.GetComponent<Image>() : null;
                Image icon = slotT != null ? slotT.Find("Icon")?.GetComponent<Image>() : null;
                GameObject separator = slotT != null ? slotT.Find("Separator")?.gameObject : null;

                elem.FindPropertyRelative("root").objectReferenceValue = root;
                elem.FindPropertyRelative("frame").objectReferenceValue = frame;
                elem.FindPropertyRelative("icon").objectReferenceValue = icon;
                elem.FindPropertyRelative("separator").objectReferenceValue = separator;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(banner);
        }
    }
}
#endif
