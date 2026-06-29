#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Reconstruit la mise en page interne d'une BonusCard (badge centré, cadre d'icône, gros titre,
    /// description, avant→après, malus) + configure le conteneur parent. Couleurs via UiTheme.
    /// Le code pilote le contenu ; cet outil la structure. Menu : Take Five Games > UI > Reconstruire BonusCard (layout).
    /// </summary>
    public static class BonusCardRebuilder
    {
        [MenuItem("Take Five Games/UI/Reconstruire BonusCard (layout)")]
        public static void Rebuild()
        {
            var go = Selection.activeGameObject;
            var card = go != null ? go.GetComponent<BonusCard>() : null;
            if (card == null)
            {
                EditorUtility.DisplayDialog("Sélection requise",
                    "Sélectionne une carte avec le composant BonusCard.", "OK");
                return;
            }

            Sprite rounded = UiGen.Card;
            Sprite knob = UiGen.Knob;

            var root = card.gameObject;
            var rootRt = (RectTransform)root.transform;

            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(root.transform.GetChild(i).gameObject);

            var bg = root.GetComponent<Image>() ?? Undo.AddComponent<Image>(root);
            Undo.RecordObject(bg, "bg"); bg.sprite = rounded; bg.type = Image.Type.Sliced; EditorUtility.SetDirty(bg);

            var btn = root.GetComponent<Button>() ?? Undo.AddComponent<Button>(root);
            Undo.RecordObject(btn, "btn"); btn.targetGraphic = bg; EditorUtility.SetDirty(btn);

            var oldCsf = root.GetComponent<ContentSizeFitter>();
            if (oldCsf != null) Undo.DestroyObjectImmediate(oldCsf);

            var v = root.GetComponent<VerticalLayoutGroup>() ?? Undo.AddComponent<VerticalLayoutGroup>(root);
            Undo.RecordObject(v, "vlg");
            v.padding = new RectOffset(26, 26, 22, 22);
            v.spacing = 12;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;  v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            EditorUtility.SetDirty(v);

            // Badge centré
            var badgeRow = Row("BadgeRow", rootRt, TextAnchor.MiddleCenter, false);
            var badge = NewUI("Badge", badgeRow, out RectTransform badgeRt);
            var badgeBg = badge.AddComponent<Image>(); badgeBg.sprite = rounded; badgeBg.type = Image.Type.Sliced; badgeBg.color = UiTheme.Frame;
            var badgeH = badge.AddComponent<HorizontalLayoutGroup>();
            badgeH.padding = new RectOffset(18,18,6,6); badgeH.childAlignment = TextAnchor.MiddleCenter;
            badgeH.childControlWidth = true; badgeH.childControlHeight = true;
            var badgeCsf = badge.AddComponent<ContentSizeFitter>();
            badgeCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            badgeCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var badgeText = NewText("BadgeText", badgeRt, "BADGE", 18, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, false);

            // Cadre d'icône
            var iconRow = Row("IconRow", rootRt, TextAnchor.MiddleCenter, false);
            var frame = NewUI("IconFrame", iconRow, out RectTransform frameRt);
            var frameImg = frame.AddComponent<Image>(); frameImg.sprite = rounded; frameImg.type = Image.Type.Sliced; frameImg.color = UiTheme.Frame;
            var frameLe = frame.AddComponent<LayoutElement>();
            frameLe.minWidth = 132; frameLe.preferredWidth = 132; frameLe.minHeight = 132; frameLe.preferredHeight = 132;
            var icon = NewUI("Icon", frameRt, out RectTransform iconRt);
            var iconImg = icon.AddComponent<Image>(); iconImg.sprite = knob; iconImg.color = Color.white; iconImg.preserveAspect = true;
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(10,10); iconRt.offsetMax = new Vector2(-10,-10);

            // Titre + textes
            var nameText = NewText("NameText", rootRt, "Nom", 38, UiTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center, true);
            var rarityText = NewText("RarityText", rootRt, "", 18, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center, false);
            var descText = NewText("DescriptionText", rootRt, "Description…", 22, UiTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center, true);

            // Avant → Après
            var beforeAfter = Row("BeforeAfterContainer", rootRt, TextAnchor.MiddleCenter, false);
            beforeAfter.gameObject.GetComponent<HorizontalLayoutGroup>().spacing = 12;
            var beforeText = NewText("BeforeText", beforeAfter, "—", 24, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center, false);
            NewText("Arrow", beforeAfter, "→", 24, UiTheme.TextMuted, FontStyles.Bold, TextAlignmentOptions.Center, false);
            var afterText = NewText("AfterText", beforeAfter, "—", 24, UiTheme.Positive, FontStyles.Bold, TextAlignmentOptions.Center, false);

            // Malus
            var downside = Row("DownsideContainer", rootRt, TextAnchor.MiddleCenter, false);
            var downText = NewText("DownsideText", downside, "▼", 20, UiTheme.Negative, FontStyles.Bold, TextAlignmentOptions.Center, false);

            // Branchement
            var so = new SerializedObject(card);
            UiGen.Wire(so, "backgroundImage", bg);
            UiGen.Wire(so, "selectButton", btn);
            UiGen.Wire(so, "iconImage", iconImg);
            UiGen.Wire(so, "badgeBackground", badgeBg);
            UiGen.Wire(so, "badgeText", badgeText);
            UiGen.Wire(so, "nameText", nameText);
            UiGen.Wire(so, "rarityText", rarityText);
            UiGen.Wire(so, "descriptionText", descText);
            UiGen.Wire(so, "beforeAfterContainer", beforeAfter.gameObject);
            UiGen.Wire(so, "beforeText", beforeText);
            UiGen.Wire(so, "afterText", afterText);
            UiGen.Wire(so, "downsideContainer", downside.gameObject);
            UiGen.Wire(so, "downsideText", downText);
            SetCol(so, "commonColor", UiTheme.BonusCommon); SetCol(so, "uncommonColor", UiTheme.BonusUncommon);
            SetCol(so, "rareColor", UiTheme.BonusRare); SetCol(so, "epicColor", UiTheme.BonusEpic); SetCol(so, "specialColor", UiTheme.BonusSpecial);
            so.ApplyModifiedProperties();

            beforeAfter.gameObject.SetActive(false);
            downside.gameObject.SetActive(false);

            // Conteneur parent : marges + espacement
            var parent = root.transform.parent;
            var pv = parent != null ? parent.GetComponent<VerticalLayoutGroup>() : null;
            if (pv != null)
            {
                Undo.RecordObject(pv, "parent vlg");
                pv.padding = new RectOffset(28, 28, 24, 24);
                pv.spacing = 20;
                pv.childControlWidth = true; pv.childControlHeight = true;
                pv.childForceExpandWidth = true; pv.childForceExpandHeight = false;
                EditorUtility.SetDirty(pv);
            }
            else
            {
                Debug.Log("[BonusCard] Parent sans VerticalLayoutGroup — règle les marges du conteneur à la main.");
            }

            EditorUtility.SetDirty(card);
            Debug.Log($"[BonusCard] '{card.name}' reconstruite (layout élégant).");
        }

        private static GameObject NewUI(string name, RectTransform parent, out RectTransform rt)
        {
            var g = new GameObject(name, typeof(RectTransform));
            rt = (RectTransform)g.transform; rt.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(g, "create");
            return g;
        }

        private static RectTransform Row(string name, RectTransform parent, TextAnchor align, bool expandW)
        {
            var g = NewUI(name, parent, out RectTransform rt);
            var h = g.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = align;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = expandW; h.childForceExpandHeight = false;
            return rt;
        }

        private static TextMeshProUGUI NewText(string name, RectTransform parent, string text,
            float size, Color color, FontStyles style, TextAlignmentOptions align, bool wrap)
        {
            var g = new GameObject(name, typeof(RectTransform));
            ((RectTransform)g.transform).SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(g, "create text");
            var t = g.AddComponent<TextMeshProUGUI>();
            var font = UiGen.LoadFont(); if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = align;
            t.enableWordWrapping = wrap; t.overflowMode = TextOverflowModes.Overflow;
            g.AddComponent<LayoutElement>();
            return t;
        }

        private static void SetCol(SerializedObject so, string field, Color col)
        {
            var p = so.FindProperty(field);
            if (p != null) p.colorValue = col;
        }
    }
}
#endif
