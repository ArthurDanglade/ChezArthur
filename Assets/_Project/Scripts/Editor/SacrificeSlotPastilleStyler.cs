#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Habille les SacrificeSlotUI en PASTILLES (Gate 5a), conformément à la maquette :
    /// [cadre icône : boîte Frame + liseré rareté] + [nom / niveau], fond Surface, coins arrondis ;
    /// liseré OR de sélection ; SlotsGrid en rangée horizontale.
    ///
    /// IMPORTANT — compatible PREFAB : on NE déplace PAS les enfants existants (Unity interdit de
    /// reparenter un enfant de prefab sous un GameObject ajouté). On CRÉE des éléments neufs
    /// (icône + nom + niveau) dans les nouveaux conteneurs, on RECÂBLE iconImage/nameText/levelText
    /// vers eux, et on MASQUE l'ancien HeaderRow. In place, idempotent. Constantes = contrat maquette.
    /// Remplace l'ancien SacrificeSlotStyler. Menu : Take Five Games > UI > Habiller slots (pastilles).
    /// </summary>
    public static class SacrificeSlotPastilleStyler
    {
        // ── Contrat maquette ──
        private const float PastilleMinHeight = 110f;
        private const float IconBox           = 72f;
        private const float IconInset         = 8f;
        private const int   RingPPUM          = 8;     // épaisseur des liserés (plus haut = plus fin)
        private const int   Padding           = 14;
        private const int   Spacing           = 14;
        private const float SelectedTint      = 0.12f; // or à 12 % sur Surface (slot sélectionné)

        [MenuItem("Take Five Games/UI/Habiller slots (pastilles)")]
        public static void Style()
        {
            var slots = Object.FindObjectsOfType<SacrificeSlotUI>(true);
            if (slots == null || slots.Length == 0)
            { EditorUtility.DisplayDialog("Aucun slot", "Aucun SacrificeSlotUI trouvé (ouvre la scène Game).", "OK"); return; }

            Sprite rounded = UiGen.Card;
            Transform grid = null;
            foreach (var slot in slots)
            {
                StyleOne(slot, rounded);
                if (grid == null) grid = slot.transform.parent;
            }
            if (grid != null) StyleGrid(grid.gameObject);

            Debug.Log($"[Sacrifice] {slots.Length} slot(s) en pastilles (éléments neufs recâblés) + SlotsGrid en rangée.");
        }

        private static void StyleOne(SacrificeSlotUI slot, Sprite rounded)
        {
            var so = new SerializedObject(slot);
            var bg = Img(so, "backgroundImage");
            var root = slot.gameObject;

            // ── Root : carte Surface arrondie + layout horizontal + hauteur de pastille ──
            if (bg != null) { Undo.RecordObject(bg, "bg"); bg.sprite = rounded; bg.type = Image.Type.Sliced; bg.color = UiTheme.Surface; EditorUtility.SetDirty(bg); }
            var h = EnsureLayout<HorizontalLayoutGroup>(root);
            Undo.RecordObject(h, "h");
            h.padding = new RectOffset(Padding, Padding, Padding, Padding); h.spacing = Spacing;
            h.childAlignment = TextAnchor.MiddleLeft; h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false; EditorUtility.SetDirty(h);
            RemoveComponent<ContentSizeFitter>(root);
            var rootLe = root.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(root);
            Undo.RecordObject(rootLe, "le"); rootLe.minHeight = PastilleMinHeight; rootLe.preferredHeight = PastilleMinHeight; EditorUtility.SetDirty(rootLe);

            // ── Liseré de sélection (overlay plein, ignore layout, désactivé) → selectionOutline ──
            var ring = GetOrCreate(root.transform, "SelectionRing");
            var ringImg = ring.GetComponent<Image>() ?? Undo.AddComponent<Image>(ring);
            Undo.RecordObject(ringImg, "ring");
            ringImg.sprite = rounded; ringImg.type = Image.Type.Sliced; ringImg.fillCenter = false;
            ringImg.pixelsPerUnitMultiplier = RingPPUM; ringImg.color = UiTheme.Gold; ringImg.raycastTarget = false;
            EditorUtility.SetDirty(ringImg);
            Stretch((RectTransform)ring.transform); IgnoreLayout(ring);
            ring.transform.SetAsLastSibling(); ring.SetActive(false);

            // ── Cadre icône : boîte Frame + icône NEUVE + liseré rareté ──
            var iconFrame = GetOrCreate(root.transform, "IconFrame");
            var frameImg = iconFrame.GetComponent<Image>() ?? Undo.AddComponent<Image>(iconFrame);
            Undo.RecordObject(frameImg, "frame");
            frameImg.sprite = rounded; frameImg.type = Image.Type.Sliced; frameImg.color = UiTheme.Frame; frameImg.raycastTarget = false;
            EditorUtility.SetDirty(frameImg);
            var frameLe = iconFrame.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(iconFrame);
            frameLe.minWidth = IconBox; frameLe.preferredWidth = IconBox; frameLe.minHeight = IconBox; frameLe.preferredHeight = IconBox;
            iconFrame.transform.SetSiblingIndex(0);

            // Icône neuve (au runtime, iconImage sera recâblé dessus)
            var iconGo = GetOrCreate(iconFrame.transform, "PastilleIcon");
            var pIcon = iconGo.GetComponent<Image>() ?? Undo.AddComponent<Image>(iconGo);
            Undo.RecordObject(pIcon, "icon");
            pIcon.raycastTarget = false; pIcon.preserveAspect = true; pIcon.type = Image.Type.Simple;
            EditorUtility.SetDirty(pIcon);
            var irt = (RectTransform)iconGo.transform;
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(IconInset, IconInset); irt.offsetMax = new Vector2(-IconInset, -IconInset);
            iconGo.transform.SetSiblingIndex(0);

            // Liseré rareté (au-dessus de l'icône) → rarityAccent (couleur pilotée au runtime)
            var rarityRing = GetOrCreate(iconFrame.transform, "RarityRing");
            var rarityImg = rarityRing.GetComponent<Image>() ?? Undo.AddComponent<Image>(rarityRing);
            Undo.RecordObject(rarityImg, "rarity");
            rarityImg.sprite = rounded; rarityImg.type = Image.Type.Sliced; rarityImg.fillCenter = false;
            rarityImg.pixelsPerUnitMultiplier = RingPPUM; rarityImg.color = new Color(1f, 1f, 1f, 0f); rarityImg.raycastTarget = false;
            EditorUtility.SetDirty(rarityImg);
            Stretch((RectTransform)rarityRing.transform); rarityRing.transform.SetAsLastSibling();

            // ── Colonne texte : nom / niveau NEUFS ──
            var textCol = GetOrCreate(root.transform, "TextCol");
            var colV = EnsureLayout<VerticalLayoutGroup>(textCol);
            colV.padding = new RectOffset(0, 0, 0, 0); colV.spacing = 2; colV.childAlignment = TextAnchor.MiddleLeft;
            colV.childControlWidth = true; colV.childControlHeight = true; colV.childForceExpandWidth = false; colV.childForceExpandHeight = false;
            textCol.transform.SetSiblingIndex(1);

            var pName  = GetOrCreateText(textCol.transform, "PastilleName");
            StyleText(pName, UiTheme.FontName, FontStyles.Bold, UiTheme.TextPrimary);
            pName.transform.SetSiblingIndex(0);
            if (string.IsNullOrEmpty(pName.text)) pName.text = "Nom"; // aperçu éditeur ; le runtime écrase

            var pLevel = GetOrCreateText(textCol.transform, "PastilleLevel");
            StyleText(pLevel, UiTheme.FontBody, FontStyles.Normal, UiTheme.TextMuted);
            pLevel.transform.SetSiblingIndex(1);
            if (string.IsNullOrEmpty(pLevel.text)) pLevel.text = "Niv. 1";

            // ── Masquer l'ancienne structure (par nom : stable, idempotent) ──
            HideByName(root.transform, "HeaderRow");   // contient l'ancien SlotIcon / LeftBlock(Name,Level) / ValueText
            HideByName(root.transform, "DescLine");
            HideByName(root.transform, "DetailSlot");
            HideByName(root.transform, "RarityAccent");

            // ── Recâblage des références vers les éléments neufs + couleurs sélection ──
            Wire(so, "iconImage", pIcon);
            Wire(so, "nameText", pName);
            Wire(so, "levelText", pLevel);
            Wire(so, "rarityAccent", rarityImg);
            Wire(so, "selectionOutline", ringImg);
            SetCol(so, "neutralColor",  UiTheme.Surface);
            SetCol(so, "selectedColor", Color.Lerp(UiTheme.Surface, UiTheme.Gold, SelectedTint));
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(slot);
        }

        private static void StyleGrid(GameObject grid)
        {
            var h = EnsureLayout<HorizontalLayoutGroup>(grid);
            Undo.RecordObject(h, "grid h");
            h.padding = new RectOffset(0, 0, 0, 0); h.spacing = Spacing; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            EditorUtility.SetDirty(h);
            var csf = grid.GetComponent<ContentSizeFitter>() ?? Undo.AddComponent<ContentSizeFitter>(grid);
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EditorUtility.SetDirty(csf);
        }

        // ── helpers ──
        private static void HideByName(Transform root, string name)
        { var t = root.Find(name); if (t != null && t.gameObject.activeSelf) { Undo.RecordObject(t.gameObject, "hide"); t.gameObject.SetActive(false); } }

        private static void Stretch(RectTransform rt)
        { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }

        private static void IgnoreLayout(GameObject go)
        { var le = go.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(go); le.ignoreLayout = true; }

        private static void RemoveComponent<T>(GameObject go) where T : Component
        { var c = go.GetComponent<T>(); if (c != null) Undo.DestroyObjectImmediate(c); }

        private static T EnsureLayout<T>(GameObject go) where T : LayoutGroup
        {
            var existing = go.GetComponent<LayoutGroup>();
            if (existing != null && !(existing is T)) Undo.DestroyObjectImmediate(existing);
            return go.GetComponent<T>() ?? Undo.AddComponent<T>(go);
        }

        private static GameObject GetOrCreate(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            ((RectTransform)go.transform).SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "create");
            return go;
        }

        private static TextMeshProUGUI GetOrCreateText(Transform parent, string name)
        {
            var t = parent.Find(name);
            GameObject go;
            if (t != null) go = t.gameObject;
            else { go = new GameObject(name, typeof(RectTransform)); ((RectTransform)go.transform).SetParent(parent, false); Undo.RegisterCreatedObjectUndo(go, "create text"); }
            return go.GetComponent<TextMeshProUGUI>() ?? Undo.AddComponent<TextMeshProUGUI>(go);
        }

        private static void StyleText(TextMeshProUGUI t, float size, FontStyles style, Color col)
        {
            Undo.RecordObject(t, "text");
            var f = UiGen.LoadFont(); if (f != null) t.font = f;
            t.fontSize = size; t.fontStyle = style; t.color = col; t.alignment = TextAlignmentOptions.MidlineLeft;
            t.enableWordWrapping = false; t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            EditorUtility.SetDirty(t);
        }

        private static Image Img(SerializedObject so, string field) { var p = so.FindProperty(field); return p != null ? p.objectReferenceValue as Image : null; }
        private static void Wire(SerializedObject so, string field, Object value) { var p = so.FindProperty(field); if (p != null) p.objectReferenceValue = value; }
        private static void SetCol(SerializedObject so, string field, Color c) { var p = so.FindProperty(field); if (p != null) p.colorValue = c; }
    }
}
#endif
