#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Stylise un titre de section sélectionné (couleur/poids/espacement/police) et insère un
    /// filet de séparation juste en dessous. Couleurs via UiTheme.
    /// Menu : Take Five Games > UI > Styliser titre de section (+ filet).
    /// </summary>
    public static class SectionHeaderStyler
    {
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

            Undo.RecordObject(header, "Style header");
            var font = UiGen.LoadFont(); if (font != null) header.font = font;
            header.fontSize = UiTheme.FontHeader;
            header.fontStyle = FontStyles.Bold;
            header.color = UiTheme.AccentSection;
            header.characterSpacing = 4f;
            EditorUtility.SetDirty(header);

            var parent = header.transform.parent as RectTransform;
            int idx = header.transform.GetSiblingIndex();

            if (idx + 1 < parent.childCount && parent.GetChild(idx + 1).name == "HeaderLine")
            {
                Debug.Log($"[Header] '{header.name}' stylisé (filet déjà présent).");
                return;
            }

            var line = new GameObject("HeaderLine", typeof(RectTransform));
            var lineRt = (RectTransform)line.transform;
            lineRt.SetParent(parent, false);
            var img = line.AddComponent<Image>();
            img.color = UiTheme.Filet;
            var le = line.AddComponent<LayoutElement>();
            le.minHeight = 2; le.preferredHeight = 2; le.flexibleWidth = 1;
            lineRt.SetSiblingIndex(idx + 1);

            Undo.RegisterCreatedObjectUndo(line, "Add header line");
            Debug.Log($"[Header] '{header.name}' stylisé + filet ajouté.");
        }
    }
}
#endif
