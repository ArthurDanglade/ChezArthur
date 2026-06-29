#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Reconstruit la zone de comparaison du SacrificeUI en deux colonnes Perds/Gagnes :
    /// fond sombre propre (indépendant de la sélection or du slot), colonnes teintées rouge/vert,
    /// icône encadrée rareté + nom + niveau par côté, rows existantes réutilisées en dessous.
    /// Branche les 8 champs de colonne. Idempotent (relançable). Menu : Take Five Games > UI > Reconstruire comparaison sacrifice.
    /// </summary>
    public static class SacrificeComparisonRebuilder
    {
        [MenuItem("Take Five Games/UI/Reconstruire comparaison sacrifice")]
        public static void Rebuild()
        {
            var sac = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<SacrificeUI>() : null;
            if (sac == null) sac = Object.FindObjectOfType<SacrificeUI>(true);
            if (sac == null) { EditorUtility.DisplayDialog("Introuvable", "Sélectionne l'objet SacrificeUI (ou ouvre la scène Game).", "OK"); return; }

            var so = new SerializedObject(sac);
            var comp = Ref(so, "comparisonContainer") as GameObject;
            var loseHeader = Ref(so, "sacrificeHeader") as TextMeshProUGUI;
            var gainHeader = Ref(so, "gainHeader") as TextMeshProUGUI;
            if (comp == null || loseHeader == null || gainHeader == null)
            { EditorUtility.DisplayDialog("Champs manquants", "comparisonContainer / sacrificeHeader / gainHeader non assignés.", "OK"); return; }

            Sprite rounded = UiGen.Card;

            // 1) Conteneur : fond sombre propre + 2 colonnes côte à côte
            var compImg = comp.GetComponent<Image>() ?? Undo.AddComponent<Image>(comp);
            Undo.RecordObject(compImg, "bg");
            compImg.sprite = rounded; compImg.type = Image.Type.Sliced; compImg.color = UiTheme.Surface; compImg.raycastTarget = false; EditorUtility.SetDirty(compImg);
            var compH = EnsureLayout<HorizontalLayoutGroup>(comp);
            Undo.RecordObject(compH, "h");
            compH.padding = new RectOffset(14,14,14,14); compH.spacing = 12; compH.childAlignment = TextAnchor.UpperCenter;
            compH.childControlWidth = true; compH.childControlHeight = true; compH.childForceExpandWidth = true; compH.childForceExpandHeight = false;
            EditorUtility.SetDirty(compH);

            // 2) Colonnes (lose rouge / gain vert)
            var lose = BuildColumn((RectTransform)loseHeader.transform.parent, loseHeader, Dark(UiTheme.Negative), UiTheme.Negative, "Tu perds", rounded);
            var gain = BuildColumn((RectTransform)gainHeader.transform.parent, gainHeader, Dark(UiTheme.Positive), UiTheme.Positive, "Tu gagnes", rounded);

            // 3) Branchement des 8 champs
            UiGen.Wire(so, "loseIcon", lose.icon);
            UiGen.Wire(so, "loseRarityFrame", lose.frame);
            UiGen.Wire(so, "loseNameText", lose.name);
            UiGen.Wire(so, "loseLevelText", lose.level);
            UiGen.Wire(so, "gainIcon", gain.icon);
            UiGen.Wire(so, "gainRarityFrame", gain.frame);
            UiGen.Wire(so, "gainNameText", gain.name);
            UiGen.Wire(so, "gainLevelText", gain.level);
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(sac);
            Debug.Log("[Sacrifice] Comparaison reconstruite en 2 colonnes + 8 champs branchés.");
        }

        private struct Col { public Image icon, frame; public TextMeshProUGUI name, level; }

        private static Col BuildColumn(RectTransform section, TextMeshProUGUI header, Color tint, Color headerCol, string label, Sprite rounded)
        {
            // Fond teinté + colonne verticale
            var img = section.GetComponent<Image>() ?? Undo.AddComponent<Image>(section.gameObject);
            Undo.RecordObject(img, "col bg"); img.sprite = rounded; img.type = Image.Type.Sliced; img.color = tint; EditorUtility.SetDirty(img);
            var v = EnsureLayout<VerticalLayoutGroup>(section.gameObject);
            Undo.RecordObject(v, "col v");
            v.padding = new RectOffset(12,12,12,12); v.spacing = 6; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            EditorUtility.SetDirty(v);
            var le = section.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(section.gameObject);
            Undo.RecordObject(le, "col le"); le.flexibleWidth = 1; EditorUtility.SetDirty(le);

            // Header
            Undo.RecordObject(header, "hdr");
            var f = UiGen.LoadFont(); if (f != null) header.font = f;
            header.text = label; header.color = headerCol; header.fontStyle = FontStyles.Bold;
            header.fontSize = UiTheme.FontLabel; header.alignment = TextAlignmentOptions.Center; EditorUtility.SetDirty(header);

            int idx = header.transform.GetSiblingIndex();

            // Rangée icône (centrée), cadre rareté, icône
            var iconRow = GetOrCreate(section, "ColIconRow");
            var rowH = EnsureLayout<HorizontalLayoutGroup>(iconRow);
            rowH.childAlignment = TextAnchor.MiddleCenter; rowH.childControlWidth = true; rowH.childControlHeight = true;
            rowH.childForceExpandWidth = false; rowH.childForceExpandHeight = false;
            iconRow.transform.SetSiblingIndex(idx + 1);

            var frame = GetOrCreate(iconRow.transform, "ColIconFrame");
            var frameImg = frame.GetComponent<Image>() ?? frame.AddComponent<Image>();
            frameImg.sprite = rounded; frameImg.type = Image.Type.Sliced; frameImg.color = UiTheme.Frame;
            var frameLe = frame.GetComponent<LayoutElement>() ?? frame.AddComponent<LayoutElement>();
            frameLe.minWidth = 84; frameLe.preferredWidth = 84; frameLe.minHeight = 84; frameLe.preferredHeight = 84;

            var icon = GetOrCreate(frame.transform, "ColIcon");
            var iconImg = icon.GetComponent<Image>() ?? icon.AddComponent<Image>();
            iconImg.preserveAspect = true; iconImg.color = Color.white;
            var iconRt = (RectTransform)icon.transform;
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(7,7); iconRt.offsetMax = new Vector2(-7,-7);

            // Nom + niveau
            var nameGo = GetOrCreate(section, "ColName");
            var nameT = Text(nameGo, UiTheme.FontBody, FontStyles.Bold, UiTheme.TextPrimary);
            nameGo.transform.SetSiblingIndex(idx + 2);

            var levelGo = GetOrCreate(section, "ColLevel");
            var levelT = Text(levelGo, UiTheme.FontLabel, FontStyles.Normal, UiTheme.TextMuted);
            levelGo.transform.SetSiblingIndex(idx + 3);

            return new Col { icon = iconImg, frame = frameImg, name = nameT, level = levelT };
        }

        // ── helpers ──

        /// <summary> Garantit un LayoutGroup du type voulu : retire tout LayoutGroup d'un autre type au préalable. </summary>
        private static T EnsureLayout<T>(GameObject go) where T : LayoutGroup
        {
            var existing = go.GetComponent<LayoutGroup>();
            if (existing != null && !(existing is T))
                Undo.DestroyObjectImmediate(existing);
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

        private static TextMeshProUGUI Text(GameObject go, float size, FontStyles style, Color col)
        {
            var t = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            var f = UiGen.LoadFont(); if (f != null) t.font = f;
            t.fontSize = size; t.fontStyle = style; t.color = col; t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = true; t.overflowMode = TextOverflowModes.Overflow;
            if (go.GetComponent<LayoutElement>() == null) go.AddComponent<LayoutElement>();
            return t;
        }

        private static Object Ref(SerializedObject so, string field)
        {
            var p = so.FindProperty(field);
            return p != null ? p.objectReferenceValue : null;
        }

        private static Color Dark(Color c) { var d = c * 0.27f; d.a = 1f; return d; }
    }
}
#endif
