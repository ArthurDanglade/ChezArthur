#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Reconstruit la mise en page interne d'une BonusCard sélectionnée (garde le GameObject,
    /// le bouton et sa place dans la liste) : padding, badge centré, cadre d'icône, gros titre
    /// centré, description aérée, lignes avant→après / malus. Configure aussi le conteneur parent
    /// (marges + espacement). Le code pilote le contenu ; cet outil pilote la structure.
    /// Menu : Take Five Games > UI > Reconstruire BonusCard (layout).
    /// </summary>
    public static class BonusCardRebuilder
    {
        private static readonly Color Common   = new Color(0.227f, 0.243f, 0.282f);
        private static readonly Color Uncommon = new Color(0.180f, 0.369f, 0.227f);
        private static readonly Color Rare     = new Color(0.165f, 0.290f, 0.478f);
        private static readonly Color Epic      = new Color(0.290f, 0.165f, 0.431f);
        private static readonly Color Special  = new Color(0.478f, 0.369f, 0.118f);

        private static readonly Color FrameCol  = new Color(0.165f, 0.180f, 0.220f);
        private static readonly Color NameCol   = new Color(0.949f, 0.957f, 0.973f);
        private static readonly Color RarityCol = new Color(0.682f, 0.706f, 0.776f);
        private static readonly Color DescCol   = new Color(0.812f, 0.827f, 0.871f);
        private static readonly Color BeforeCol = new Color(0.565f, 0.596f, 0.659f);
        private static readonly Color AfterCol  = new Color(0.486f, 0.780f, 0.486f);
        private static readonly Color ArrowCol  = new Color(0.565f, 0.596f, 0.659f);
        private static readonly Color DownCol   = new Color(0.878f, 0.478f, 0.478f);

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

            Sprite rounded = LoadByName("card_rounded")
                ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var root = card.gameObject;
            var rootRt = (RectTransform)root.transform;

            // Supprime les anciens enfants (contenu posé en absolu)
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(root.transform.GetChild(i).gameObject);

            // Fond = Image du root (teinté par le code), arrondi
            var bg = root.GetComponent<Image>() ?? Undo.AddComponent<Image>(root);
            Undo.RecordObject(bg, "bg"); bg.sprite = rounded; bg.type = Image.Type.Sliced; EditorUtility.SetDirty(bg);

            // Bouton = root
            var btn = root.GetComponent<Button>() ?? Undo.AddComponent<Button>(root);
            Undo.RecordObject(btn, "btn"); btn.targetGraphic = bg; EditorUtility.SetDirty(btn);

            // Nettoie un éventuel ContentSizeFitter sur le root (conflit layout)
            var oldCsf = root.GetComponent<ContentSizeFitter>();
            if (oldCsf != null) Undo.DestroyObjectImmediate(oldCsf);

            // Layout vertical du root
            var v = root.GetComponent<VerticalLayoutGroup>() ?? Undo.AddComponent<VerticalLayoutGroup>(root);
            Undo.RecordObject(v, "vlg");
            v.padding = new RectOffset(26, 26, 22, 22);
            v.spacing = 12;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;  v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            EditorUtility.SetDirty(v);

            // ── Badge (centré) ──
            var badgeRow = Row("BadgeRow", rootRt, TextAnchor.MiddleCenter, false);
            var badge = NewUI("Badge", badgeRow, out RectTransform badgeRt);
            var badgeBg = badge.AddComponent<Image>(); badgeBg.sprite = rounded; badgeBg.type = Image.Type.Sliced; badgeBg.color = new Color(0.4f,0.4f,0.4f);
            var badgeH = badge.AddComponent<HorizontalLayoutGroup>();
            badgeH.padding = new RectOffset(18,18,6,6); badgeH.childAlignment = TextAnchor.MiddleCenter;
            badgeH.childControlWidth = true; badgeH.childControlHeight = true;
            var badgeCsf = badge.AddComponent<ContentSizeFitter>();
            badgeCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            badgeCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var badgeText = NewText("BadgeText", badgeRt, "BADGE", 18, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, false);

            // ── Cadre d'icône (toujours visible) ──
            var iconRow = Row("IconRow", rootRt, TextAnchor.MiddleCenter, false);
            var frame = NewUI("IconFrame", iconRow, out RectTransform frameRt);
            var frameImg = frame.AddComponent<Image>(); frameImg.sprite = rounded; frameImg.type = Image.Type.Sliced; frameImg.color = FrameCol;
            var frameLe = frame.AddComponent<LayoutElement>();
            frameLe.minWidth = 132; frameLe.preferredWidth = 132; frameLe.minHeight = 132; frameLe.preferredHeight = 132;
            var icon = NewUI("Icon", frameRt, out RectTransform iconRt);
            var iconImg = icon.AddComponent<Image>(); iconImg.sprite = knob; iconImg.color = Color.white; iconImg.preserveAspect = true;
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(10,10); iconRt.offsetMax = new Vector2(-10,-10);

            // ── Titre (gros, centré) ──
            var nameText = NewText("NameText", rootRt, "Nom", 38, NameCol, FontStyles.Bold, TextAlignmentOptions.Center, true);
            var rarityText = NewText("RarityText", rootRt, "", 18, RarityCol, FontStyles.Normal, TextAlignmentOptions.Center, false);
            var descText = NewText("DescriptionText", rootRt, "Description…", 22, DescCol, FontStyles.Normal, TextAlignmentOptions.Center, true);

            // ── Avant → Après (upgrades) ──
            var beforeAfter = Row("BeforeAfterContainer", rootRt, TextAnchor.MiddleCenter, false);
            beforeAfter.gameObject.GetComponent<HorizontalLayoutGroup>().spacing = 12;
            var beforeText = NewText("BeforeText", beforeAfter, "—", 24, BeforeCol, FontStyles.Normal, TextAlignmentOptions.Center, false);
            NewText("Arrow", beforeAfter, "→", 24, ArrowCol, FontStyles.Bold, TextAlignmentOptions.Center, false);
            var afterText = NewText("AfterText", beforeAfter, "—", 24, AfterCol, FontStyles.Bold, TextAlignmentOptions.Center, false);

            // ── Malus (items risqués) ──
            var downside = Row("DownsideContainer", rootRt, TextAnchor.MiddleCenter, false);
            var downText = NewText("DownsideText", downside, "▼", 20, DownCol, FontStyles.Bold, TextAlignmentOptions.Center, false);

            // ── Branchement ──
            var so = new SerializedObject(card);
            Wire(so, "backgroundImage", bg);
            Wire(so, "selectButton", btn);
            Wire(so, "iconImage", iconImg);
            Wire(so, "badgeBackground", badgeBg);
            Wire(so, "badgeText", badgeText);
            Wire(so, "nameText", nameText);
            Wire(so, "rarityText", rarityText);
            Wire(so, "descriptionText", descText);
            Wire(so, "beforeAfterContainer", beforeAfter.gameObject);
            Wire(so, "beforeText", beforeText);
            Wire(so, "afterText", afterText);
            Wire(so, "downsideContainer", downside.gameObject);
            Wire(so, "downsideText", downText);
            SetCol(so, "commonColor", Common); SetCol(so, "uncommonColor", Uncommon);
            SetCol(so, "rareColor", Rare); SetCol(so, "epicColor", Epic); SetCol(so, "specialColor", Special);
            so.ApplyModifiedProperties();

            // Conteneurs optionnels masqués par défaut (le code les rallume)
            beforeAfter.gameObject.SetActive(false);
            downside.gameObject.SetActive(false);

            // ── Conteneur parent : marges + espacement ──
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

        // ── helpers ──
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
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = align;
            t.enableWordWrapping = wrap; t.overflowMode = TextOverflowModes.Overflow;
            g.AddComponent<LayoutElement>();
            return t;
        }

        private static void Wire(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning($"[BonusCard] Champ '{field}' introuvable.");
        }

        private static void SetCol(SerializedObject so, string field, Color col)
        {
            var p = so.FindProperty(field);
            if (p != null) p.colorValue = col;
        }

        private static Sprite LoadByName(string spriteName)
        {
            foreach (var g in AssetDatabase.FindAssets($"{spriteName} t:Sprite"))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g));
                if (s != null && s.name == spriteName) return s;
            }
            return null;
        }
    }
}
#endif
