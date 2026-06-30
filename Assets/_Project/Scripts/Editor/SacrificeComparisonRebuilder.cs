#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Reconstruit la carte de comparaison du SacrificeUI (Gate 4 — structure héros + pastilles) :
    /// ComparisonContainer (VerticalLayoutGroup, fond Surface, CSF vertical conservé)
    ///   └── ColumnsRow (HorizontalLayoutGroup, ForceExpandHeight → colonnes de MÊME hauteur)
    ///         ├── SacrificeSection (rouge sombre) : header, icône HÉROS encadrée rareté, nom, niveau, pastilles
    ///         └── GainSection      (vert sombre)  : idem
    ///   ├── ConfirmHintText
    ///   └── ConfirmButton (CTA pleine largeur)
    /// Chaque LoseRow_*/GainRow_* devient une PASTILLE arrondie horizontale : [icône?] label | valeur.
    /// Idempotent, Undo-safe, lit UiTheme. Câblage préservé. Menu : Take Five Games > UI > Reconstruire comparaison sacrifice.
    /// </summary>
    public static class SacrificeComparisonRebuilder
    {
        // ── Constantes de style (source unique des dimensions) ──
        private const float IconFrameSize     = 120f; // icône héros encadrée (était 84)
        private const float IconInset         = 10f;
        private const float ChipMinHeight     = 44f;   // hauteur mini d'une pastille
        private const float ChipIconSize      = 32f;   // emplacement d'icône de stat (optionnel)
        private const float ConfirmButtonHeight = 80f;

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

            var hintText = Ref(so, "confirmHintText") as TextMeshProUGUI;
            var confirmButton = Ref(so, "confirmButton") as Button;

            Sprite rounded = UiGen.Card;

            // ── 1) Conteneur : carte verticale sombre ──
            var compImg = comp.GetComponent<Image>() ?? Undo.AddComponent<Image>(comp);
            Undo.RecordObject(compImg, "bg");
            compImg.sprite = rounded; compImg.type = Image.Type.Sliced; compImg.color = UiTheme.Surface; compImg.raycastTarget = false;
            EditorUtility.SetDirty(compImg);

            var compV = EnsureLayout<VerticalLayoutGroup>(comp);
            Undo.RecordObject(compV, "v");
            compV.padding = new RectOffset(14, 14, 14, 14); compV.spacing = 12; compV.childAlignment = TextAnchor.UpperCenter;
            compV.childControlWidth = true; compV.childControlHeight = true; compV.childForceExpandWidth = true; compV.childForceExpandHeight = false;
            EditorUtility.SetDirty(compV);

            // Le VLG du Container parent ne contrôle PAS la hauteur → CSF vertical conservé (cf. Gate 3).
            var compCsf = comp.GetComponent<ContentSizeFitter>() ?? Undo.AddComponent<ContentSizeFitter>(comp);
            Undo.RecordObject(compCsf, "csf");
            compCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            compCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EditorUtility.SetDirty(compCsf);

            // ── 2) ColumnsRow : colonnes de même hauteur ──
            var columnsRow = GetOrCreate(comp.transform, "ColumnsRow");
            var rowH = EnsureLayout<HorizontalLayoutGroup>(columnsRow);
            Undo.RecordObject(rowH, "row h");
            rowH.padding = new RectOffset(0, 0, 0, 0); rowH.spacing = 12; rowH.childAlignment = TextAnchor.UpperCenter;
            rowH.childControlWidth = true; rowH.childControlHeight = true; rowH.childForceExpandWidth = true; rowH.childForceExpandHeight = true;
            EditorUtility.SetDirty(rowH);
            columnsRow.transform.SetSiblingIndex(0);

            // ── 3) Reparenter les sections sous ColumnsRow ──
            var loseSection = (RectTransform)loseHeader.transform.parent;
            var gainSection = (RectTransform)gainHeader.transform.parent;
            Reparent(loseSection, columnsRow.transform, 0);
            Reparent(gainSection, columnsRow.transform, 1);

            // ── 4) Construire chaque colonne (héros + pastilles) ──
            var lose = BuildColumn(loseSection, loseHeader, Dark(UiTheme.Negative), UiTheme.Negative, "Tu perds", rounded);
            var gain = BuildColumn(gainSection, gainHeader, Dark(UiTheme.Positive), UiTheme.Positive, "Tu gagnes", rounded);

            // ── 5) Hint + bouton CTA pleine largeur ──
            int below = 1;
            if (hintText != null) { Reparent((RectTransform)hintText.transform, comp.transform, below); below++; }
            if (confirmButton != null)
            {
                Reparent((RectTransform)confirmButton.transform, comp.transform, below);
                var btnLe = confirmButton.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(confirmButton.gameObject);
                Undo.RecordObject(btnLe, "btn le");
                btnLe.minHeight = ConfirmButtonHeight; btnLe.preferredHeight = ConfirmButtonHeight; btnLe.flexibleWidth = 1;
                EditorUtility.SetDirty(btnLe);
            }

            // ── 6) Branchement des 8 champs de colonne ──
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
            Debug.Log("[Sacrifice] Gate 4 (structure) : héros agrandi + pastilles. Overlap fossile purgé.");
        }

        private struct Col { public Image icon, frame; public TextMeshProUGUI name, level; }

        private static Col BuildColumn(RectTransform section, TextMeshProUGUI header, Color tint, Color accent, string label, Sprite rounded)
        {
            RemoveComponent<ContentSizeFitter>(section.gameObject);

            // Fond teinté + colonne verticale
            var img = section.GetComponent<Image>() ?? Undo.AddComponent<Image>(section.gameObject);
            Undo.RecordObject(img, "col bg"); img.sprite = rounded; img.type = Image.Type.Sliced; img.color = tint; EditorUtility.SetDirty(img);
            var v = EnsureLayout<VerticalLayoutGroup>(section.gameObject);
            Undo.RecordObject(v, "col v");
            v.padding = new RectOffset(12, 12, 12, 12); v.spacing = 8; v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            EditorUtility.SetDirty(v);
            var le = section.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(section.gameObject);
            Undo.RecordObject(le, "col le"); le.flexibleWidth = 1; EditorUtility.SetDirty(le);

            // Header
            Undo.RecordObject(header, "hdr");
            var f = UiGen.LoadFont(); if (f != null) header.font = f;
            header.text = label; header.color = accent; header.fontStyle = FontStyles.Bold;
            header.fontSize = UiTheme.FontLabel; header.alignment = TextAlignmentOptions.Center; EditorUtility.SetDirty(header);

            int idx = header.transform.GetSiblingIndex();

            // Rangée icône HÉROS (centrée), cadre rareté, icône
            var iconRow = GetOrCreate(section, "ColIconRow");
            var iconRowH = EnsureLayout<HorizontalLayoutGroup>(iconRow);
            iconRowH.childAlignment = TextAnchor.MiddleCenter; iconRowH.childControlWidth = true; iconRowH.childControlHeight = true;
            iconRowH.childForceExpandWidth = false; iconRowH.childForceExpandHeight = false;
            iconRow.transform.SetSiblingIndex(idx + 1);

            var frame = GetOrCreate(iconRow.transform, "ColIconFrame");
            var frameImg = frame.GetComponent<Image>() ?? frame.AddComponent<Image>();
            frameImg.sprite = rounded; frameImg.type = Image.Type.Sliced; frameImg.color = UiTheme.Frame;
            var frameLe = frame.GetComponent<LayoutElement>() ?? frame.AddComponent<LayoutElement>();
            frameLe.minWidth = IconFrameSize; frameLe.preferredWidth = IconFrameSize; frameLe.minHeight = IconFrameSize; frameLe.preferredHeight = IconFrameSize;

            var icon = GetOrCreate(frame.transform, "ColIcon");
            var iconImg = icon.GetComponent<Image>() ?? icon.AddComponent<Image>();
            iconImg.preserveAspect = true; iconImg.color = Color.white;
            var iconRt = (RectTransform)icon.transform;
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(IconInset, IconInset); iconRt.offsetMax = new Vector2(-IconInset, -IconInset);

            // Nom (HÉROS) + niveau
            var nameGo = GetOrCreate(section, "ColName");
            var nameT = Text(nameGo, UiTheme.FontName, FontStyles.Bold, UiTheme.TextPrimary);
            nameGo.transform.SetSiblingIndex(idx + 2);

            var levelGo = GetOrCreate(section, "ColLevel");
            var levelT = Text(levelGo, UiTheme.FontBody, FontStyles.Normal, UiTheme.TextMuted);
            levelGo.transform.SetSiblingIndex(idx + 3);

            // Pastilles (LoseRow_*/GainRow_*)
            StyleStatChips(section, accent, rounded);

            return new Col { icon = iconImg, frame = frameImg, name = nameT, level = levelT };
        }

        /// <summary> Transforme chaque StatLineUI de la colonne en pastille arrondie horizontale. </summary>
        private static void StyleStatChips(Transform section, Color accent, Sprite rounded)
        {
            Color chipBg = Color.Lerp(UiTheme.Surface, accent, 0.16f); chipBg.a = 1f;
            var rows = section.GetComponentsInChildren<StatLineUI>(true);

            foreach (var row in rows)
            {
                var go = row.gameObject;

                // Fond pastille
                var img = go.GetComponent<Image>() ?? Undo.AddComponent<Image>(go);
                Undo.RecordObject(img, "chip bg");
                img.sprite = rounded; img.type = Image.Type.Sliced; img.color = chipBg; img.raycastTarget = false;
                EditorUtility.SetDirty(img);

                // Une pastille est pilotée par le layout parent → pas de CSF
                RemoveComponent<ContentSizeFitter>(go);

                // Layout horizontal : [icône?] label | valeur
                var h = EnsureLayout<HorizontalLayoutGroup>(go);
                Undo.RecordObject(h, "chip h");
                h.padding = new RectOffset(14, 14, 8, 8); h.spacing = 8; h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
                EditorUtility.SetDirty(h);

                // Purge du preferredHeight fossile, hauteur mini de pastille
                var le = go.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(go);
                Undo.RecordObject(le, "chip le");
                le.preferredHeight = -1f; le.minHeight = ChipMinHeight; le.flexibleWidth = 1f;
                EditorUtility.SetDirty(le);

                // Icône optionnelle (1er enfant), désactivée tant qu'aucune icône n'est fournie
                var iconGo = GetOrCreate(go.transform, "ChipIcon");
                var iconImg = iconGo.GetComponent<Image>() ?? iconGo.AddComponent<Image>();
                iconImg.preserveAspect = true;
                var iconLe = iconGo.GetComponent<LayoutElement>() ?? iconGo.AddComponent<LayoutElement>();
                iconLe.minWidth = ChipIconSize; iconLe.preferredWidth = ChipIconSize; iconLe.minHeight = ChipIconSize; iconLe.preferredHeight = ChipIconSize;
                iconGo.transform.SetSiblingIndex(0);
                iconGo.SetActive(false);

                // Label + valeur : lire les refs sérialisées, styliser, réordonner
                var rso = new SerializedObject(row);
                var labelT = rso.FindProperty("labelText")?.objectReferenceValue as TextMeshProUGUI;
                var valueT = rso.FindProperty("valueText")?.objectReferenceValue as TextMeshProUGUI;

                if (labelT != null)
                {
                    Undo.RecordObject(labelT, "chip label");
                    var lf = UiGen.LoadFont(); if (lf != null) labelT.font = lf;
                    labelT.fontSize = UiTheme.FontBody; labelT.fontStyle = FontStyles.Normal;
                    labelT.alignment = TextAlignmentOptions.MidlineLeft; labelT.enableWordWrapping = true;
                    labelT.overflowMode = TextOverflowModes.Overflow;
                    var labelLe = labelT.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(labelT.gameObject);
                    labelLe.flexibleWidth = 1f;
                    labelT.transform.SetSiblingIndex(1);
                    EditorUtility.SetDirty(labelT);
                }
                if (valueT != null)
                {
                    Undo.RecordObject(valueT, "chip value");
                    var vf = UiGen.LoadFont(); if (vf != null) valueT.font = vf;
                    valueT.fontSize = UiTheme.FontBody; valueT.fontStyle = FontStyles.Bold;
                    valueT.alignment = TextAlignmentOptions.MidlineRight; valueT.enableWordWrapping = false;
                    var valueLe = valueT.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(valueT.gameObject);
                    valueLe.flexibleWidth = 0f;
                    valueT.transform.SetSiblingIndex(2);
                    EditorUtility.SetDirty(valueT);
                }

                // Câbler l'icône optionnelle dans le StatLineUI
                UiGen.Wire(rso, "iconImage", iconImg);
                rso.ApplyModifiedProperties();
            }
        }

        // ── helpers ──

        private static void Reparent(RectTransform child, Transform newParent, int siblingIndex)
        {
            if (child == null || newParent == null) return;
            if (child.parent != newParent)
                Undo.SetTransformParent(child, newParent, "reparent comparaison");
            child.SetSiblingIndex(siblingIndex);
            EditorUtility.SetDirty(child);
        }

        private static void RemoveComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c != null) Undo.DestroyObjectImmediate(c);
        }

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
