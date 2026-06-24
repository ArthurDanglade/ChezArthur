#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Stylise un titre de section sélectionné (couleur/poids/espacement) et insère un
    /// filet de séparation juste en dessous (entre le titre et son contenu).
    /// Menu : Take Five Games > UI > Styliser titre de section.
    /// </summary>
    public static class SectionHeaderStyler
    {
        private static readonly Color HeaderColor = new Color(0.66f, 0.69f, 0.83f, 1f); // cool clair
        private static readonly Color LineColor   = new Color(0.23f, 0.24f, 0.32f, 1f); // filet sombre

        [MenuItem("Take Five Games/UI/Styliser titre de section (+ filet)")]
        public static void Style()
        {
            var go = Selection.activeGameObject;
            var header = go != null ? go.GetComponent<TextMeshProUGUI>() : null;
            if (header == null)
            {
                EditorUtility.DisplayDialog("Sélection requise",
                    "Sélectionne un titre de section (un objet avec un TextMeshPro).", "OK");
                return;
            }

            // Style du titre
            Undo.RecordObject(header, "Style header");
            header.fontSize = 24;
            header.fontStyle = FontStyles.Bold;
            header.color = HeaderColor;
            header.characterSpacing = 4f;
            EditorUtility.SetDirty(header);

            var parent = header.transform.parent as RectTransform;
            int idx = header.transform.GetSiblingIndex();

            // Évite le doublon si un filet est déjà juste après.
            if (idx + 1 < parent.childCount &&
                parent.GetChild(idx + 1).name == "HeaderLine")
            {
                Debug.Log($"[Header] '{header.name}' stylisé (filet déjà présent).");
                return;
            }

            // Filet juste après le titre
            var line = new GameObject("HeaderLine", typeof(RectTransform));
            var lineRt = (RectTransform)line.transform;
            lineRt.SetParent(parent, false);
            var img = line.AddComponent<Image>();
            img.color = LineColor;
            var le = line.AddComponent<LayoutElement>();
            le.minHeight = 2; le.preferredHeight = 2; le.flexibleWidth = 1;
            lineRt.SetSiblingIndex(idx + 1);

            Undo.RegisterCreatedObjectUndo(line, "Add header line");
            Debug.Log($"[Header] '{header.name}' stylisé + filet ajouté.");
        }
    }
}
#endif
