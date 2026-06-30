#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Gate 6a — Reconstruit l'incoming en BANDE "Tu reçois" compacte, conformément à la maquette :
    /// une ligne, fond Surface, liseré or, [boîte icône (icône réelle du reçu)] + ["Tu reçois" (or) /
    /// Nom (gras)] + ["Niv. X" (muted) à droite]. Fini le gros bloc vert et le chevauchement nom/desc.
    ///
    /// In place (IncomingContainer est un objet de scène, reparentage OK), idempotent.
    /// Câble incomingIconImage sur l'icône neuve. Prérequis : l'edit Cursor « couleurs statiques ».
    /// Menu : Take Five Games > UI > Habiller la bande incoming.
    /// </summary>
    public static class SacrificeIncomingBandStyler
    {
        // ── Contrat maquette ──
        private const float BandHeight = 124f;
        private const float IconBox    = 88f;
        private const float IconInset  = 12f;
        private const int   RingPPUM   = 8;
        private const int   Padding    = 14;
        private const int   Spacing    = 14;

        [MenuItem("Take Five Games/UI/Habiller la bande incoming")]
        public static void Style()
        {
            var ui = Object.FindObjectOfType<SacrificeUI>(true);
            if (ui == null) { Dialog("Aucun SacrificeUI trouvé (ouvre la scène Game)."); return; }

            var so = new SerializedObject(ui);
            var card   = Img(so, "incomingCardBackground");   // Image de IncomingContainer
            var badge  = Img(so, "incomingBadgeBackground");  // Image de IncomingBadge
            var nameT  = Txt(so, "incomingNameText");
            var valueT = Txt(so, "incomingValueText");

            if (card == null || badge == null || nameT == null || valueT == null)
            { Dialog("Références incoming incomplètes sur SacrificeUI (card/badge/name/value)."); return; }

            Sprite rounded = UiGen.Card;
            var container = card.gameObject;   // IncomingContainer
            var badgeGo   = badge.gameObject;  // IncomingBadge → boîte d'icône
            var textBlock = nameT.transform.parent.gameObject; // TextBlock

            // ── Bande : fond Surface arrondi + liseré or + layout horizontal + hauteur réduite ──
            Undo.RecordObject(card, "card"); card.sprite = rounded; card.type = Image.Type.Sliced; card.color = UiTheme.Surface; EditorUtility.SetDirty(card);
            var h = EnsureLayout<HorizontalLayoutGroup>(container);
            Undo.RecordObject(h, "h");
            h.padding = new RectOffset(Padding, Padding, Padding, Padding); h.spacing = Spacing;
            h.childAlignment = TextAnchor.MiddleLeft; h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false; EditorUtility.SetDirty(h);
            var cRt = (RectTransform)container.transform; cRt.sizeDelta = new Vector2(cRt.sizeDelta.x, BandHeight);
            var cLe = container.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(container);
            cLe.minHeight = BandHeight; cLe.preferredHeight = BandHeight; // prêt pour Gate 6b

            // Liseré or (overlay, ignore layout)
            var ring = GetOrCreate(container.transform, "IncomingGoldRing");
            var ringImg = ring.GetComponent<Image>() ?? Undo.AddComponent<Image>(ring);
            ringImg.sprite = rounded; ringImg.type = Image.Type.Sliced; ringImg.fillCenter = false;
            ringImg.pixelsPerUnitMultiplier = RingPPUM; ringImg.color = UiTheme.Gold; ringImg.raycastTarget = false;
            Stretch((RectTransform)ring.transform); IgnoreLayout(ring); ring.transform.SetAsLastSibling();

            // ── Boîte d'icône (= IncomingBadge) : or léger, taille fixe, icône réelle dedans ──
            Undo.RecordObject(badge, "badge"); badge.sprite = rounded; badge.type = Image.Type.Sliced;
            badge.color = UiTheme.Frame; EditorUtility.SetDirty(badge); // défaut neutre ; le runtime remplit par rareté
            var bLe = badgeGo.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(badgeGo);
            bLe.minWidth = IconBox; bLe.preferredWidth = IconBox; bLe.minHeight = IconBox; bLe.preferredHeight = IconBox;
            badgeGo.transform.SetSiblingIndex(0);

            var iconGo = GetOrCreate(badgeGo.transform, "IncomingIcon");
            var pIcon = iconGo.GetComponent<Image>() ?? Undo.AddComponent<Image>(iconGo);
            pIcon.raycastTarget = false; pIcon.preserveAspect = true; pIcon.type = Image.Type.Simple;
            var iRt = (RectTransform)iconGo.transform; iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
            iRt.offsetMin = new Vector2(IconInset, IconInset); iRt.offsetMax = new Vector2(-IconInset, -IconInset);

            var oldRarity = badgeGo.transform.Find("IncomingRarityText"); // ancien tag "VALISE" (dans la boîte)
            if (oldRarity != null) HideGO(oldRarity.gameObject);

            // ── Colonne texte : "Tu reçois" (or) au-dessus du Nom (gras) ──
            var colV = EnsureLayout<VerticalLayoutGroup>(textBlock);
            colV.padding = new RectOffset(0, 0, 0, 0); colV.spacing = 4; colV.childAlignment = TextAnchor.MiddleLeft;
            colV.childControlWidth = true; colV.childControlHeight = true; colV.childForceExpandWidth = false; colV.childForceExpandHeight = false;
            var tLe = textBlock.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(textBlock);
            tLe.flexibleWidth = 1f; tLe.minWidth = 0f; // pousse "Niv." à droite
            textBlock.transform.SetSiblingIndex(1);

            var label = GetOrCreateText(textBlock.transform, "ReceiveLabel");
            StyleText(label, UiTheme.FontBody, FontStyles.Normal, UiTheme.Gold, TextAlignmentOptions.MidlineLeft);
            label.text = "Tu reçois"; label.transform.SetSiblingIndex(0);

            StyleText(nameT, UiTheme.FontName, FontStyles.Bold, UiTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            nameT.transform.SetSiblingIndex(1);

            // Ligne rareté "Amélioration X" sous le nom (texte + couleur pilotés au runtime)
            var rarityLine = GetOrCreateText(textBlock.transform, "ReceiveRarity");
            StyleText(rarityLine, UiTheme.FontLabel, FontStyles.Normal, UiTheme.TextMuted, TextAlignmentOptions.MidlineLeft);
            rarityLine.text = "Amélioration"; rarityLine.transform.SetSiblingIndex(2);

            // ── "Niv. X" à droite (reparenté hors du TextBlock, 3e enfant de la bande) ──
            Reparent((RectTransform)valueT.transform, container.transform, 2);
            StyleText(valueT, UiTheme.FontBody, FontStyles.Normal, UiTheme.TextMuted, TextAlignmentOptions.MidlineRight);

            // ── Câblage de l'icône reçue + ligne rareté + liseré (coloré par rareté au runtime) ──
            Wire(so, "incomingIconImage", pIcon);
            Wire(so, "incomingRarityText", rarityLine);
            Wire(so, "incomingFrameRing", ringImg);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ui);

            Debug.Log("[Sacrifice] Bande incoming reconstruite (Tu reçois + icône réelle + Niv. à droite).");
        }

        // ── helpers ──
        private static void Reparent(RectTransform child, Transform parent, int idx)
        { if (child == null || parent == null) return; if (child.parent != parent) Undo.SetTransformParent(child, parent, "reparent"); child.SetSiblingIndex(idx); EditorUtility.SetDirty(child); }

        private static void Stretch(RectTransform rt)
        { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }

        private static void IgnoreLayout(GameObject go)
        { var le = go.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(go); le.ignoreLayout = true; }

        private static void HideGO(GameObject go) { if (go != null && go.activeSelf) { Undo.RecordObject(go, "hide"); go.SetActive(false); } }

        private static T EnsureLayout<T>(GameObject go) where T : LayoutGroup
        { var ex = go.GetComponent<LayoutGroup>(); if (ex != null && !(ex is T)) Undo.DestroyObjectImmediate(ex); return go.GetComponent<T>() ?? Undo.AddComponent<T>(go); }

        private static GameObject GetOrCreate(Transform parent, string name)
        { var t = parent.Find(name); if (t != null) return t.gameObject; var go = new GameObject(name, typeof(RectTransform)); ((RectTransform)go.transform).SetParent(parent, false); Undo.RegisterCreatedObjectUndo(go, "create"); return go; }

        private static TextMeshProUGUI GetOrCreateText(Transform parent, string name)
        { var t = parent.Find(name); GameObject go; if (t != null) go = t.gameObject; else { go = new GameObject(name, typeof(RectTransform)); ((RectTransform)go.transform).SetParent(parent, false); Undo.RegisterCreatedObjectUndo(go, "create"); } return go.GetComponent<TextMeshProUGUI>() ?? Undo.AddComponent<TextMeshProUGUI>(go); }

        private static void StyleText(TextMeshProUGUI t, float size, FontStyles style, Color col, TextAlignmentOptions align)
        { Undo.RecordObject(t, "text"); var f = UiGen.LoadFont(); if (f != null) t.font = f; t.fontSize = size; t.fontStyle = style; t.color = col; t.alignment = align; t.enableWordWrapping = false; t.overflowMode = TextOverflowModes.Ellipsis; t.raycastTarget = false; EditorUtility.SetDirty(t); }

        private static Image Img(SerializedObject so, string f) { var p = so.FindProperty(f); return p != null ? p.objectReferenceValue as Image : null; }
        private static TextMeshProUGUI Txt(SerializedObject so, string f) { var p = so.FindProperty(f); return p != null ? p.objectReferenceValue as TextMeshProUGUI : null; }
        private static void Wire(SerializedObject so, string f, Object v) { var p = so.FindProperty(f); if (p != null) p.objectReferenceValue = v; }
        private static void Dialog(string m) { EditorUtility.DisplayDialog("Bande incoming", m, "OK"); }
    }
}
#endif
