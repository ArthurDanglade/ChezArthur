#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Gate 6b (A) — Grossit le CONTENU de la comparaison en place, sans toucher à la structure.
    /// Lit les 10 refs sérialisées du SacrificeUI (headers, icônes, cadres, noms, niveaux) et applique
    /// des tailles à l'échelle du mockup : gros cadre d'icône, gros nom, header et niveau lisibles,
    /// padding/spacing de colonne plus aérés. Non-destructif, idempotent, Undo-safe.
    /// Menu : Take Five Games > UI > Grossir contenu comparaison.
    /// </summary>
    public static class SacrificeComparisonContentStyler
    {
        // Tailles cibles (tokens UiTheme : FontHeader 24 / FontName 30 / FontBody 22)
        private const float HeaderFont = 24f;   // UiTheme.FontHeader
        private const float NameFont   = 30f;   // UiTheme.FontName
        private const float LevelFont  = 22f;   // UiTheme.FontBody
        private const float FrameSize  = 160f;  // cadre d'icône carré
        private const float IconInset  = 16f;   // marge icône dans le cadre
        private const int   ColPadding = 18;
        private const int   ColSpacing = 12;

        [MenuItem("Take Five Games/UI/Grossir contenu comparaison")]
        public static void Apply()
        {
            var ui = Object.FindObjectOfType<SacrificeUI>(true);
            if (ui == null) { Dialog("Aucun SacrificeUI (ouvre la scène Game)."); return; }
            var so = new SerializedObject(ui);

            StyleSide(so, "sacrificeHeader", "loseRarityFrame", "loseIcon", "loseNameText", "loseLevelText");
            StyleSide(so, "gainHeader",      "gainRarityFrame", "gainIcon", "gainNameText", "gainLevelText");

            Debug.Log("[Sacrifice] Contenu comparaison grossi (cadre 160, nom 30, header 24, niveau 22, colonnes aérées).");
        }

        private static void StyleSide(SerializedObject so, string headerF, string frameF, string iconF, string nameF, string levelF)
        {
            var header = Ref<TextMeshProUGUI>(so, headerF);
            var frame  = Ref<Image>(so, frameF);
            var icon   = Ref<Image>(so, iconF);
            var nameT  = Ref<TextMeshProUGUI>(so, nameF);
            var levelT = Ref<TextMeshProUGUI>(so, levelF);

            if (header != null) { SetFont(header, HeaderFont); }
            if (nameT  != null) { SetFont(nameT,  NameFont); }
            if (levelT != null) { SetFont(levelT, LevelFont); }

            // Cadre d'icône : LayoutElement carré FrameSize
            if (frame != null)
            {
                var le = frame.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(frame.gameObject);
                Undo.RecordObject(le, "frame size");
                le.minWidth = FrameSize; le.preferredWidth = FrameSize;
                le.minHeight = FrameSize; le.preferredHeight = FrameSize;
                EditorUtility.SetDirty(le);
            }

            // Icône : étirée dans le cadre avec marge IconInset
            if (icon != null)
            {
                var rt = (RectTransform)icon.transform;
                Undo.RecordObject(rt, "icon inset");
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(IconInset, IconInset);
                rt.offsetMax = new Vector2(-IconInset, -IconInset);
                EditorUtility.SetDirty(rt);
            }

            // Colonne (parent du header) : padding/spacing aérés
            if (header != null && header.transform.parent != null)
            {
                var vlg = header.transform.parent.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    Undo.RecordObject(vlg, "col air");
                    vlg.padding = new RectOffset(ColPadding, ColPadding, ColPadding, ColPadding);
                    vlg.spacing = ColSpacing;
                    EditorUtility.SetDirty(vlg);
                }
            }
        }

        private static void SetFont(TextMeshProUGUI t, float size)
        { Undo.RecordObject(t, "font"); t.fontSize = size; EditorUtility.SetDirty(t); }

        private static T Ref<T>(SerializedObject so, string field) where T : Object
        { var p = so.FindProperty(field); return p != null ? p.objectReferenceValue as T : null; }

        private static void Dialog(string m) { EditorUtility.DisplayDialog("Grossir contenu", m, "OK"); }
    }
}
#endif
