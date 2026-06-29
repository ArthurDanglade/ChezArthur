#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Habille un SacrificeSlotUI sélectionné EN PLACE : lit ses références et les stylise
    /// (carte arrondie, contour arrondi, icône preserveAspect, typo + couleurs défaut/sélection
    /// via UiTheme). Ne touche ni au layout ni à la logique. À lancer sur chaque slot (ou le prefab).
    /// Menu : Take Five Games > UI > Habiller slot de sacrifice.
    /// </summary>
    public static class SacrificeSlotStyler
    {
        [MenuItem("Take Five Games/UI/Habiller slot de sacrifice")]
        public static void Style()
        {
            var go = Selection.activeGameObject;
            var slot = go != null ? go.GetComponent<SacrificeSlotUI>() : null;
            if (slot == null)
            {
                EditorUtility.DisplayDialog("Sélection requise",
                    "Sélectionne un objet avec le composant SacrificeSlotUI.", "OK");
                return;
            }

            Sprite rounded = UiGen.Card;
            var so = new SerializedObject(slot);

            // Fond → carte arrondie (la couleur reste pilotée par le code via defaultColor/selectedColor)
            var bg = Img(so, "backgroundImage");
            if (bg != null) { Undo.RecordObject(bg, "style"); bg.sprite = rounded; bg.type = Image.Type.Sliced; EditorUtility.SetDirty(bg); }

            // Contour de sélection → arrondi (couleur pilotée par le code)
            var outline = Img(so, "selectionOutline");
            if (outline != null) { Undo.RecordObject(outline, "style"); outline.sprite = rounded; outline.type = Image.Type.Sliced; EditorUtility.SetDirty(outline); }

            // Icône : ne pas déformer
            var icon = Img(so, "iconImage");
            if (icon != null) { Undo.RecordObject(icon, "style"); icon.preserveAspect = true; EditorUtility.SetDirty(icon); }

            // Typographie (la couleur du niveau est repeinte au runtime par le code → on ne fixe que taille/police)
            StyleText(so, "nameText",        UiTheme.FontName, FontStyles.Bold,   UiTheme.TextPrimary,   true);
            StyleText(so, "levelText",       UiTheme.FontLabel, FontStyles.Bold,  UiTheme.TextMuted,     false);
            StyleText(so, "descriptionText", UiTheme.FontBody, FontStyles.Normal, UiTheme.TextSecondary, true);

            // Couleurs défaut/sélection → tokens (carte sombre, sélection or)
            SetCol(so, "defaultColor",  UiTheme.Surface);
            SetCol(so, "selectedColor", UiTheme.Gold);
            so.ApplyModifiedProperties();

            Debug.Log($"[Sacrifice] Slot '{slot.name}' habillé.");
        }

        private static Image Img(SerializedObject so, string field)
        {
            var p = so.FindProperty(field);
            return p != null ? p.objectReferenceValue as Image : null;
        }

        private static void StyleText(SerializedObject so, string field, float size, FontStyles style, Color col, bool setColor)
        {
            var p = so.FindProperty(field);
            var t = p != null ? p.objectReferenceValue as TextMeshProUGUI : null;
            if (t == null) return;
            Undo.RecordObject(t, "style text");
            var font = UiGen.LoadFont(); if (font != null) t.font = font;
            t.fontSize = size; t.fontStyle = style;
            if (setColor) t.color = col;
            EditorUtility.SetDirty(t);
        }

        private static void SetCol(SerializedObject so, string field, Color col)
        {
            var p = so.FindProperty(field);
            if (p != null) p.colorValue = col;
        }
    }
}
#endif
