#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Ajoute une section (titre + conteneur vertical aligné) sous l'objet sélectionné.
    /// Pensé pour structurer le scroll du menu pause : Personnages, Valises, Items, Stats.
    /// Menu : Take Five Games > UI > Ajouter une section.
    /// </summary>
    public static class PauseSectionGenerator
    {
        [MenuItem("Take Five Games/UI/Ajouter une section (titre + conteneur)")]
        public static void AddSection()
        {
            var parent = Selection.activeTransform as RectTransform;
            if (parent == null)
            {
                EditorUtility.DisplayDialog("Sélection requise",
                    "Sélectionne d'abord le Content du scroll (le parent à layout vertical).", "OK");
                return;
            }

            // ── Titre de section ──
            var header = NewText("SectionHeader", parent, "Section", 26,
                new Color(0.75f, 0.78f, 0.85f, 1f), FontStyles.Bold);
            var headerLe = header.gameObject.AddComponent<LayoutElement>();
            headerLe.minHeight = 40; headerLe.preferredHeight = 40;

            // ── Conteneur vertical (recevra les entrées) ──
            var content = new GameObject("SectionContent", typeof(RectTransform));
            ((RectTransform)content.transform).SetParent(parent, false);
            var v = content.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true;  v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            content.AddComponent<LayoutElement>().minHeight = 10;

            Undo.RegisterCreatedObjectUndo(header.gameObject, "Add Section Header");
            Undo.RegisterCreatedObjectUndo(content, "Add Section Content");

            // Sélectionne le conteneur pour le retrouver vite.
            Selection.activeGameObject = content;
            Debug.Log($"[Section] Ajoutée sous '{parent.name}'. Renomme le titre et le conteneur, " +
                      "et règle le texte du titre.");
        }

        private static TextMeshProUGUI NewText(string name, RectTransform parent, string text,
            float size, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            ((RectTransform)go.transform).SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.enableWordWrapping = false;
            go.AddComponent<LayoutElement>();
            return t;
        }
    }
}
#endif
