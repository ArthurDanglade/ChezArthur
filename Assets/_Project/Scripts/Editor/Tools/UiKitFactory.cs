#if UNITY_EDITOR
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

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

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
