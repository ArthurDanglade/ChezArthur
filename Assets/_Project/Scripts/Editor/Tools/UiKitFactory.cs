#if UNITY_EDITOR
using System.Collections.Generic;
using ChezArthur.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Factory unique de construction UI Kit (boutons, pills).
    /// Sandbox, HomeActions, futurs builders — une seule vérité.
    /// </summary>
    public static class UiKitFactory
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string UndoLabel = "UiKitFactory";
        private const string FillChildName = "Fill";
        private const string LabelChildName = "Label";
        private const string IconChildName = "Icon";

        // ═══════════════════════════════════════════
        // RÉSULTATS
        // ═══════════════════════════════════════════

        /// <summary> Pill créée : racine + surface + label (+ icône optionnelle). </summary>
        public readonly struct PillHandle
        {
            public readonly GameObject Root;
            public readonly RectTransform Rect;
            public readonly PanelSurface Surface;
            public readonly TextMeshProUGUI Label;
            public readonly Image Icon;

            public PillHandle(
                GameObject root,
                RectTransform rect,
                PanelSurface surface,
                TextMeshProUGUI label,
                Image icon)
            {
                Root = root;
                Rect = rect;
                Surface = surface;
                Label = label;
                Icon = icon;
            }
        }

        // ═══════════════════════════════════════════
        // BOUTONS
        // ═══════════════════════════════════════════

        /// <summary>
        /// Crée un HubButtonUI (Primary / Secondary) sous parent.
        /// </summary>
        public static HubButtonUI CreateButton(
            Transform parent,
            HubButtonUI.ButtonVariant variant,
            string label,
            string subLabel,
            float height)
        {
            return CreateButton(parent, variant, label, subLabel, height, locked: false, objectName: null);
        }

        /// <summary>
        /// Variante complète : locked + nom d'objet explicite.
        /// </summary>
        public static HubButtonUI CreateButton(
            Transform parent,
            HubButtonUI.ButtonVariant variant,
            string label,
            string subLabel,
            float height,
            bool locked,
            string objectName)
        {
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteL = RoundedRectSpriteGenerator.LoadSpriteL();
            if (spriteS == null || spriteM == null || spriteL == null)
            {
                Debug.LogError("[UiKitFactory] Sprites RoundedRect_S/M/L introuvables — lancer Générer les sprites arrondis.");
                return null;
            }

            string name = string.IsNullOrEmpty(objectName)
                ? "Btn_" + SanitizeName(label) + (locked ? "_Locked" : "")
                : objectName;

            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            HubButtonUI hubBtn = Undo.AddComponent<HubButtonUI>(go);
            SerializedObject so = new SerializedObject(hubBtn);
            so.FindProperty("variant").enumValueIndex = (int)variant;
            so.FindProperty("locked").boolValue = locked;
            so.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            so.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            so.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            so.ApplyModifiedPropertiesWithoutUndo();

            hubBtn.ApplyStyle();
            hubBtn.SetLabel(label ?? string.Empty);
            hubBtn.SetSubLabel(subLabel);
            hubBtn.ApplyStyle();

            // Hauteur demandée par l'appelant (tokens PrimaryH / SecondaryH en pratique).
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(go);
            Undo.RecordObject(le, UndoLabel);
            float h = Mathf.Max(1f, height);
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleWidth = 1f;
            EditorUtility.SetDirty(le);

            return hubBtn;
        }

        // ═══════════════════════════════════════════
        // PILLS
        // ═══════════════════════════════════════════

        /// <summary>
        /// Crée une pill PanelSurface (CSF largeur preferred, hauteur fixe).
        /// Alignée sur le pattern header — réutilisable hors header.
        /// </summary>
        public static PillHandle CreatePill(
            Transform parent,
            string objectName,
            string label,
            float height)
        {
            return CreatePill(parent, objectName, label, height, icon: null,
                border: PanelSurface.SurfaceBorder.Subtle, blocksRaycasts: false);
        }

        /// <summary>
        /// Pill complète : icône optionnelle, bordure, raycasts.
        /// </summary>
        public static PillHandle CreatePill(
            Transform parent,
            string objectName,
            string label,
            float height,
            Sprite icon,
            PanelSurface.SurfaceBorder border,
            bool blocksRaycasts)
        {
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteL = RoundedRectSpriteGenerator.LoadSpriteL();
            if (spriteS == null || spriteM == null || spriteL == null)
            {
                Debug.LogError("[UiKitFactory] Sprites RoundedRect_S/M/L introuvables — lancer Générer les sprites arrondis.");
                return default;
            }

            string name = string.IsNullOrEmpty(objectName) ? "Pill" : objectName;
            float h = Mathf.Max(1f, height);

            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            RectTransform rt = (RectTransform)go.transform;
            Undo.RecordObject(rt, UndoLabel);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);

            LayoutElement rootLe = Undo.AddComponent<LayoutElement>(go);
            rootLe.minHeight = h;
            rootLe.preferredHeight = h;
            rootLe.flexibleWidth = 0f;

            ContentSizeFitter csf = Undo.AddComponent<ContentSizeFitter>(go);
            // Règle CSF projet : largeur suit contenu, hauteur contrainte.
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            HorizontalLayoutGroup hlg = Undo.AddComponent<HorizontalLayoutGroup>(go);
            int pad = Mathf.RoundToInt(UiTheme.Space3);
            hlg.padding = new RectOffset(pad, pad, pad, pad);
            hlg.spacing = UiTheme.Space2;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            PanelSurface surface = Undo.AddComponent<PanelSurface>(go);
            SerializedObject surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("variant").enumValueIndex = (int)PanelSurface.SurfaceVariant.Pill;
            surfaceSo.FindProperty("borderStyle").enumValueIndex = (int)border;
            surfaceSo.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            surfaceSo.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            surfaceSo.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            surfaceSo.FindProperty("blocksRaycasts").boolValue = blocksRaycasts;
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();
            surface.ApplyStyle();
            IgnoreLayoutOnFill(go.transform);

            Image iconImg = null;
            if (icon != null)
                iconImg = CreatePillIcon(go.transform, icon);

            TextMeshProUGUI tmp = CreatePillLabel(go.transform, label ?? string.Empty);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            return new PillHandle(go, rt, surface, tmp, iconImg);
        }

        /// <summary>
        /// Bouton icône carré (Shop / News) — slot Image Icon vide pour sprite user.
        /// </summary>
        public static HubButtonUI CreateIconButton(
            Transform parent,
            string objectName,
            float size,
            bool locked)
        {
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteL = RoundedRectSpriteGenerator.LoadSpriteL();
            if (spriteS == null || spriteM == null || spriteL == null)
            {
                Debug.LogError("[UiKitFactory] Sprites RoundedRect_S/M/L introuvables.");
                return null;
            }

            string name = string.IsNullOrEmpty(objectName) ? "BtnIcon" : objectName;
            float s = Mathf.Max(UiTheme.TouchTargetMin * 0.5f, size);

            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            RectTransform rt = (RectTransform)go.transform;
            Undo.RecordObject(rt, UndoLabel);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(s, s);

            HubButtonUI hubBtn = Undo.AddComponent<HubButtonUI>(go);
            SerializedObject so = new SerializedObject(hubBtn);
            so.FindProperty("variant").enumValueIndex = (int)HubButtonUI.ButtonVariant.Secondary;
            so.FindProperty("locked").boolValue = locked;
            so.FindProperty("overrideHeight").floatValue = s;
            so.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            so.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            so.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            so.ApplyModifiedPropertiesWithoutUndo();

            hubBtn.ApplyStyle();
            hubBtn.SetLabel(string.Empty);
            hubBtn.SetSubLabel(null);
            hubBtn.ApplyStyle();

            // Masque les labels texte — place à l'icône.
            Transform labelTx = go.transform.Find("Label");
            if (labelTx != null)
                labelTx.gameObject.SetActive(false);
            Transform subTx = go.transform.Find("SubLabel");
            if (subTx != null)
                subTx.gameObject.SetActive(false);

            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(go);
            Undo.RecordObject(le, UndoLabel);
            le.minWidth = s;
            le.preferredWidth = s;
            le.minHeight = s;
            le.preferredHeight = s;
            le.flexibleWidth = 0f;
            EditorUtility.SetDirty(le);

            // Slot icône (sprite à assigner plus tard).
            Transform existingIcon = go.transform.Find(IconChildName);
            GameObject iconGo;
            if (existingIcon != null)
            {
                iconGo = existingIcon.gameObject;
            }
            else
            {
                iconGo = new GameObject(
                    IconChildName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                Undo.RegisterCreatedObjectUndo(iconGo, UndoLabel);
                Undo.SetTransformParent(iconGo.transform, go.transform, false, UndoLabel);
            }

            RectTransform iconRt = (RectTransform)iconGo.transform;
            Undo.RecordObject(iconRt, UndoLabel);
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            float iconSize = s * 0.55f;
            iconRt.sizeDelta = new Vector2(iconSize, iconSize);
            iconRt.anchoredPosition = Vector2.zero;

            Image iconImg = iconGo.GetComponent<Image>();
            Undo.RecordObject(iconImg, UndoLabel);
            iconImg.sprite = null;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = new Color(1f, 1f, 1f, 0.35f); // placeholder visible jusqu'au sprite
            EditorUtility.SetDirty(iconImg);

            return hubBtn;
        }

        /// <summary>
        /// Applique un style de texte nommé (RUI1 — pas de TMP StyleSheet).
        /// </summary>
        public static void ApplyTextStyle(TextMeshProUGUI tmp, UiTextStyle style)
        {
            UiTextStyleUtil.Apply(tmp, style);
        }

        /// <summary>
        /// Panneau niveau 1..3 (Deep / Panel / Elevated). panelLevel=0 ailleurs = Hub inchangé.
        /// </summary>
        public static PanelSurface CreatePanel(Transform parent, int level, string objectName = null)
        {
            int clamped = Mathf.Clamp(level, 1, 3);
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteL = RoundedRectSpriteGenerator.LoadSpriteL();
            if (spriteS == null || spriteM == null || spriteL == null)
            {
                Debug.LogError("[UiKitFactory] Sprites RoundedRect manquants.");
                return null;
            }

            string name = string.IsNullOrEmpty(objectName) ? "Panel_L" + clamped : objectName;
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            PanelSurface surface = Undo.AddComponent<PanelSurface>(go);
            SerializedObject so = new SerializedObject(surface);
            so.FindProperty("variant").enumValueIndex = (int)PanelSurface.SurfaceVariant.Panel;
            so.FindProperty("borderStyle").enumValueIndex = (int)PanelSurface.SurfaceBorder.Subtle;
            so.FindProperty("panelLevel").intValue = clamped;
            so.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            so.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            so.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            so.ApplyModifiedPropertiesWithoutUndo();
            surface.ApplyStyle();
            return surface;
        }

        /// <summary>
        /// En-tête de section (barre ambre + titre).
        /// </summary>
        public static SectionHeaderUI CreateSectionHeader(
            Transform parent, string title, string count = null, string objectName = null)
        {
            string name = string.IsNullOrEmpty(objectName) ? "SectionHeader" : objectName;
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            HorizontalLayoutGroup hlg = Undo.AddComponent<HorizontalLayoutGroup>(go);
            hlg.spacing = UiTheme.Space2;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(0, 0, 4, 4);

            LayoutElement rootLe = Undo.AddComponent<LayoutElement>(go);
            rootLe.minHeight = UiTheme.Space5;
            rootLe.preferredHeight = UiTheme.Space5;

            GameObject barGo = new GameObject(
                "AccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(barGo, UndoLabel);
            Undo.SetTransformParent(barGo.transform, go.transform, false, UndoLabel);
            Image bar = barGo.GetComponent<Image>();
            bar.color = UiTheme.AccentAmber;
            bar.raycastTarget = false;
            LayoutElement barLe = Undo.AddComponent<LayoutElement>(barGo);
            barLe.minWidth = 4f;
            barLe.preferredWidth = 4f;
            barLe.minHeight = 16f;
            barLe.preferredHeight = 16f;

            TextMeshProUGUI titleTmp = CreateTmpChild(go.transform, "Title", title ?? string.Empty);
            ApplyTextStyle(titleTmp, UiTextStyle.Chip);
            titleTmp.color = UiTheme.TextSecondary;
            LayoutElement titleLe = Undo.AddComponent<LayoutElement>(titleTmp.gameObject);
            titleLe.flexibleWidth = 1f;

            TextMeshProUGUI countTmp = CreateTmpChild(go.transform, "Count", count ?? string.Empty);
            ApplyTextStyle(countTmp, UiTextStyle.Caption);
            countTmp.gameObject.SetActive(!string.IsNullOrEmpty(count));

            SectionHeaderUI header = Undo.AddComponent<SectionHeaderUI>(go);
            header.Bind(bar, titleTmp, countTmp);
            header.Set(title, count);
            return header;
        }

        /// <summary>
        /// Wrapper TabBarUI existant (G1) — ne crée pas un 2ᵉ système d'onglets.
        /// </summary>
        public static TabBarUI CreateTabBar(
            Transform parent,
            IReadOnlyList<string> labels,
            string objectName = null)
        {
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            if (spriteS == null)
            {
                Debug.LogError("[UiKitFactory] RoundedRect_S manquant.");
                return null;
            }

            string name = string.IsNullOrEmpty(objectName) ? "TabBar" : objectName;
            GameObject barGo = new GameObject(
                name, typeof(RectTransform), typeof(TabBarUI));
            Undo.RegisterCreatedObjectUndo(barGo, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(barGo.transform, parent, false, UndoLabel);

            LayoutElement le = Undo.AddComponent<LayoutElement>(barGo);
            le.minHeight = UiTheme.TouchTargetMin;
            le.preferredHeight = UiTheme.TouchTargetMin;
            le.flexibleWidth = 1f;

            // Template (même pattern sandbox)
            GameObject template = new GameObject(
                "TabItemTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(template, UndoLabel);
            Undo.SetTransformParent(template.transform, barGo.transform, false, UndoLabel);
            template.SetActive(false);

            Image border = template.GetComponent<Image>();
            border.sprite = spriteS;
            border.type = Image.Type.Sliced;
            border.color = UiTheme.BorderSubtle;

            GameObject fillGo = new GameObject(
                "Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.SetTransformParent(fillGo.transform, template.transform, false, UndoLabel);
            Image fill = fillGo.GetComponent<Image>();
            fill.sprite = spriteS;
            fill.type = Image.Type.Sliced;
            fill.color = UiTheme.TabInactive;
            RectTransform fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            float inset = UiTheme.BorderThin;
            fillRt.offsetMin = new Vector2(inset, inset);
            fillRt.offsetMax = new Vector2(-inset, -inset);

            GameObject labelGo = new GameObject(
                "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.SetTransformParent(labelGo.transform, template.transform, false, UndoLabel);
            TextMeshProUGUI labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            ApplyTextStyle(labelTmp, UiTextStyle.Chip);
            labelTmp.alignment = TextAlignmentOptions.Center;
            RectTransform labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            TabBarUI tabBar = barGo.GetComponent<TabBarUI>();
            SerializedObject so = new SerializedObject(tabBar);
            so.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            so.FindProperty("tabItemTemplate").objectReferenceValue = template;
            so.FindProperty("fixedItemHeight").floatValue = UiTheme.TouchTargetMin;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (labels != null && labels.Count > 0)
                tabBar.Init(labels, null, 0);

            return tabBar;
        }

        /// <summary>
        /// Ligne de liste (naissance RUI1).
        /// </summary>
        public static ListRowUI CreateListRow(Transform parent, string objectName = null)
        {
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            string name = string.IsNullOrEmpty(objectName) ? "ListRow" : objectName;
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            Image bg = go.GetComponent<Image>();
            bg.sprite = spriteM;
            bg.type = Image.Type.Sliced;
            bg.color = UiTheme.BgElevated;

            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = 64f;
            le.preferredHeight = 64f;
            le.flexibleWidth = 1f;

            HorizontalLayoutGroup hlg = Undo.AddComponent<HorizontalLayoutGroup>(go);
            hlg.padding = new RectOffset(12, 12, 10, 10);
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;

            GameObject avatarGo = new GameObject(
                "Avatar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.SetTransformParent(avatarGo.transform, go.transform, false, UndoLabel);
            Image frame = avatarGo.GetComponent<Image>();
            frame.color = UiTheme.BorderStrong;
            frame.raycastTarget = false;
            LayoutElement avLe = Undo.AddComponent<LayoutElement>(avatarGo);
            avLe.minWidth = 44f;
            avLe.preferredWidth = 44f;
            avLe.minHeight = 44f;
            avLe.preferredHeight = 44f;

            GameObject mid = new GameObject("Mid", typeof(RectTransform));
            Undo.SetTransformParent(mid.transform, go.transform, false, UndoLabel);
            LayoutElement midLe = Undo.AddComponent<LayoutElement>(mid);
            midLe.flexibleWidth = 1f;
            VerticalLayoutGroup vlg = Undo.AddComponent<VerticalLayoutGroup>(mid);
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            TextMeshProUGUI nameTmp = CreateTmpChild(mid.transform, "Name", "Nom");
            ApplyTextStyle(nameTmp, UiTextStyle.H2);
            TextMeshProUGUI metaTmp = CreateTmpChild(mid.transform, "Meta", "Nv.1");
            ApplyTextStyle(metaTmp, UiTextStyle.Caption);

            GameObject hpGo = new GameObject(
                "HpBar", typeof(RectTransform), typeof(Slider));
            Undo.SetTransformParent(hpGo.transform, mid.transform, false, UndoLabel);
            Slider hp = hpGo.GetComponent<Slider>();
            hp.minValue = 0f;
            hp.maxValue = 1f;
            hp.value = 0.78f;
            LayoutElement hpLe = Undo.AddComponent<LayoutElement>(hpGo);
            hpLe.minHeight = 6f;
            hpLe.preferredHeight = 6f;

            TextMeshProUGUI hpLabel = CreateTmpChild(mid.transform, "HpText", "780/1000");
            ApplyTextStyle(hpLabel, UiTextStyle.Caption);

            ListRowUI row = Undo.AddComponent<ListRowUI>(go);
            row.Bind(frame, frame, nameTmp, metaTmp, hp, hpLabel);
            return row;
        }

        /// <summary>
        /// Cellule de stat (naissance RUI1).
        /// </summary>
        public static StatCellUI CreateStatCell(
            Transform parent, string label, string value, Color accent, string objectName = null)
        {
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            string name = string.IsNullOrEmpty(objectName) ? "Stat_" + (label ?? "X") : objectName;
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            Image bg = go.GetComponent<Image>();
            bg.sprite = spriteS;
            bg.type = Image.Type.Sliced;

            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = 72f;
            le.preferredHeight = 72f;
            le.flexibleWidth = 1f;

            VerticalLayoutGroup vlg = Undo.AddComponent<VerticalLayoutGroup>(go);
            vlg.padding = new RectOffset(6, 6, 8, 8);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;

            TextMeshProUGUI k = CreateTmpChild(go.transform, "Label", label ?? string.Empty);
            ApplyTextStyle(k, UiTextStyle.Chip);
            k.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI v = CreateTmpChild(go.transform, "Value", value ?? string.Empty);
            ApplyTextStyle(v, UiTextStyle.H2);
            v.alignment = TextAlignmentOptions.Center;

            StatCellUI cell = Undo.AddComponent<StatCellUI>(go);
            cell.Bind(k, v, bg);
            cell.Set(label, value, accent);
            return cell;
        }

        /// <summary>
        /// Chip générique (naissance RUI1).
        /// </summary>
        public static UiChipUI CreateChip(
            Transform parent, string text, Color bgColor, Color fg, string objectName = null)
        {
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            string name = string.IsNullOrEmpty(objectName) ? "Chip" : objectName;
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            Image bg = go.GetComponent<Image>();
            bg.sprite = spriteS;
            bg.type = Image.Type.Sliced;
            bg.color = bgColor;

            ContentSizeFitter csf = Undo.AddComponent<ContentSizeFitter>(go);
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            HorizontalLayoutGroup hlg = Undo.AddComponent<HorizontalLayoutGroup>(go);
            hlg.padding = new RectOffset(10, 10, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleCenter;

            TextMeshProUGUI tmp = CreateTmpChild(go.transform, "Label", text ?? string.Empty);
            ApplyTextStyle(tmp, UiTextStyle.Chip);
            tmp.color = fg;

            UiChipUI chip = Undo.AddComponent<UiChipUI>(go);
            chip.Bind(bg, tmp);
            chip.Set(text, bgColor, fg);
            return chip;
        }

        /// <summary>
        /// Chip récompense Tals.
        /// </summary>
        public static RewardChipUI CreateRewardChip(Transform parent, int amount, string objectName = null)
        {
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            string name = string.IsNullOrEmpty(objectName) ? "RewardChip" : objectName;
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            Image bg = go.GetComponent<Image>();
            bg.sprite = spriteS;
            bg.type = Image.Type.Sliced;
            bg.color = UiTheme.BgElevated;

            HorizontalLayoutGroup hlg = Undo.AddComponent<HorizontalLayoutGroup>(go);
            hlg.padding = new RectOffset(12, 12, 8, 8);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = 48f;
            le.preferredHeight = 48f;

            GameObject iconGo = new GameObject(
                "Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.SetTransformParent(iconGo.transform, go.transform, false, UndoLabel);
            Image icon = iconGo.GetComponent<Image>();
            icon.color = UiTheme.Gold;
            icon.raycastTarget = false;
            LayoutElement iconLe = Undo.AddComponent<LayoutElement>(iconGo);
            iconLe.minWidth = 28f;
            iconLe.preferredWidth = 28f;
            iconLe.minHeight = 28f;
            iconLe.preferredHeight = 28f;

            TextMeshProUGUI amountTmp = CreateTmpChild(go.transform, "Amount", amount.ToString());
            ApplyTextStyle(amountTmp, UiTextStyle.H2);
            amountTmp.color = UiTheme.Gold;

            RewardChipUI chip = Undo.AddComponent<RewardChipUI>(go);
            chip.Bind(icon, amountTmp, bg);
            chip.SetAmount(amount);
            return chip;
        }

        /// <summary>
        /// Page scaffold zones réservées (Header 112 / Titre / Scroll / Footer 152).
        /// </summary>
        public static PageScaffold CreatePageScaffold(Transform parent, string objectName = null)
        {
            string name = string.IsNullOrEmpty(objectName) ? "PageScaffold" : objectName;
            GameObject root = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(root.transform, parent, false, UndoLabel);

            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            float headerH = UiTheme.HeaderHeight;
            float footerH = UiTheme.NavHeight;
            float titleH = 72f;

            RectTransform header = CreateZone(root.transform, "HeaderZone", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -headerH), new Vector2(0f, 0f));
            RectTransform title = CreateZone(root.transform, "TitleZone", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -(headerH + titleH)), new Vector2(0f, -headerH));
            RectTransform footer = CreateZone(root.transform, "FooterZone", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, footerH));
            RectTransform scrollZone = CreateZone(root.transform, "ScrollZone", new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, footerH), new Vector2(0f, -(headerH + titleH)));

            Image hBg = header.gameObject.AddComponent<Image>();
            hBg.color = UiTheme.BgPanel;
            hBg.raycastTarget = false;
            Image tBg = title.gameObject.AddComponent<Image>();
            tBg.color = UiTheme.BgDeep;
            tBg.raycastTarget = false;
            Image fBg = footer.gameObject.AddComponent<Image>();
            fBg.color = UiTheme.BgPanel;
            fBg.raycastTarget = false;

            GameObject viewport = new GameObject(
                "Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            Undo.SetTransformParent(viewport.transform, scrollZone, false, UndoLabel);
            RectTransform vpRt = (RectTransform)viewport.transform;
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            Undo.SetTransformParent(content.transform, viewport.transform, false, UndoLabel);
            RectTransform contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 400f);

            ScrollRect scroll = scrollZone.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = vpRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;

            PageScaffold scaffold = Undo.AddComponent<PageScaffold>(root);
            scaffold.Bind(header, title, scrollZone, scroll, footer);
            return scaffold;
        }

        /// <summary>
        /// Popup micro-décision (scrim + carte).
        /// </summary>
        public static PopupScaffold CreatePopupScaffold(Transform parent, string objectName = null)
        {
            string name = string.IsNullOrEmpty(objectName) ? "PopupScaffold" : objectName;
            GameObject root = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, UndoLabel);
            if (parent != null)
                Undo.SetTransformParent(root.transform, parent, false, UndoLabel);

            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            GameObject scrimGo = new GameObject(
                "Scrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.SetTransformParent(scrimGo.transform, root.transform, false, UndoLabel);
            RectTransform scrimRt = (RectTransform)scrimGo.transform;
            scrimRt.anchorMin = Vector2.zero;
            scrimRt.anchorMax = Vector2.one;
            scrimRt.offsetMin = Vector2.zero;
            scrimRt.offsetMax = Vector2.zero;
            Image scrim = scrimGo.GetComponent<Image>();
            scrim.color = UiTheme.ScrimOverlay;

            PanelSurface card = CreatePanel(root.transform, 3, "Card");
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(720f, 420f);

            PopupScaffold popup = Undo.AddComponent<PopupScaffold>(root);
            popup.Bind(scrim, cardRt, scrimGo.GetComponent<Button>());
            return popup;
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private static RectTransform CreateZone(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        private static TextMeshProUGUI CreateTmpChild(Transform parent, string name, string text)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text ?? string.Empty;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static void IgnoreLayoutOnFill(Transform pillTx)
        {
            Transform fill = pillTx.Find(FillChildName);
            if (fill == null)
                return;

            LayoutElement le = fill.GetComponent<LayoutElement>();
            if (le == null)
                le = Undo.AddComponent<LayoutElement>(fill.gameObject);
            Undo.RecordObject(le, UndoLabel);
            le.ignoreLayout = true;

            RectTransform fillRt = fill as RectTransform;
            if (fillRt == null)
                return;
            Undo.RecordObject(fillRt, UndoLabel);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            float inset = UiTheme.BorderThin;
            fillRt.offsetMin = new Vector2(inset, inset);
            fillRt.offsetMax = new Vector2(-inset, -inset);
            fillRt.SetAsFirstSibling();
        }

        private static Image CreatePillIcon(Transform pillTx, Sprite sprite)
        {
            GameObject iconGo = new GameObject(
                IconChildName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconGo, UndoLabel);
            Undo.SetTransformParent(iconGo.transform, pillTx, false, UndoLabel);

            Image img = iconGo.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;

            LayoutElement le = Undo.AddComponent<LayoutElement>(iconGo);
            le.minWidth = UiTheme.Space5;
            le.preferredWidth = UiTheme.Space5;
            le.minHeight = UiTheme.Space5;
            le.preferredHeight = UiTheme.Space5;

            // Après Fill, avant Label.
            Transform fill = pillTx.Find(FillChildName);
            int idx = fill != null ? fill.GetSiblingIndex() + 1 : 0;
            iconGo.transform.SetSiblingIndex(idx);
            return img;
        }

        private static TextMeshProUGUI CreatePillLabel(Transform pillTx, string text)
        {
            GameObject labelGo = new GameObject(
                LabelChildName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelGo, UndoLabel);
            Undo.SetTransformParent(labelGo.transform, pillTx, false, UndoLabel);

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = UiTypography.Caption;
            tmp.color = UiTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;

            LayoutElement le = Undo.AddComponent<LayoutElement>(labelGo);
            le.flexibleWidth = 0f;
            le.minHeight = UiTheme.Space4;

            return tmp;
        }

        private static string SanitizeName(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "Button";
            char[] chars = label.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                    chars[i] = '_';
            }

            return new string(chars);
        }
    }
}
#endif
