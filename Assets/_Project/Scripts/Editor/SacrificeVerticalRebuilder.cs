#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Gate 6b (option A) — Layout vertical : la comparaison est un bloc COMPACT (dimensionné par son
    /// contenu), ancré en bas au-dessus du bouton, et l'espace libre devient une respiration sombre
    /// PROPRE entre les pastilles de slots et la comparaison (pas de colonnes colorées étirées).
    ///
    /// (1) Container sans CSF, remplit la safe area, pilote la hauteur (childControlHeight ON / forceExpandHeight OFF).
    /// (2) ComparisonContainer sans CSF, dimensionné par son contenu (flexibleHeight 0).
    /// (3) ColumnsRow dimensionné par son contenu (flexibleHeight 0) — colonnes tendues sur le contenu.
    /// (4) SacrificeSection/GainSection : contenu centré verticalement (équilibre les colonnes inégales).
    /// (5) Spacer flexible inséré entre SlotsScroll et ComparisonContainer → absorbe l'espace, pousse la
    ///     comparaison vers le bas. La respiration est du fond neutre, pas du vide coloré.
    ///
    /// Objets de scène uniquement. Idempotent, Undo-safe. Menu : Take Five Games > UI > Rebuild vertical sacrifice.
    /// </summary>
    public static class SacrificeVerticalRebuilder
    {
        private const string SpacerName = "ComparisonTopSpacer";

        [MenuItem("Take Five Games/UI/Rebuild vertical sacrifice")]
        public static void Rebuild()
        {
            var ui = Object.FindObjectOfType<SacrificeUI>(true);
            if (ui == null) { Dialog("Aucun SacrificeUI trouvé (ouvre la scène Game)."); return; }

            var so = new SerializedObject(ui);
            var comparison = so.FindProperty("comparisonContainer")?.objectReferenceValue as GameObject;
            if (comparison == null) { Dialog("Champ comparisonContainer non câblé."); return; }
            if (comparison.transform.parent == null) { Dialog("ComparisonContainer sans parent Container."); return; }
            var container = comparison.transform.parent.gameObject;

            // ── 1. Container : remplit la safe area + pilote la hauteur ──
            RemoveComponent<ContentSizeFitter>(container);
            var cvlg = container.GetComponent<VerticalLayoutGroup>();
            if (cvlg != null)
            {
                Undo.RecordObject(cvlg, "container vlg");
                cvlg.childControlHeight = true;
                cvlg.childForceExpandHeight = false;
                EditorUtility.SetDirty(cvlg);
            }

            // ── 2. ComparisonContainer : sans CSF, dimensionné par son contenu ──
            RemoveComponent<ContentSizeFitter>(comparison);
            var compLe = comparison.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(comparison);
            Undo.RecordObject(compLe, "comparison le");
            compLe.flexibleHeight = 0f; compLe.minHeight = 0f; compLe.preferredHeight = -1f;
            EditorUtility.SetDirty(compLe);

            // ── 3. ColumnsRow : dimensionné par son contenu (pas d'étirement) ──
            var columnsRow = comparison.transform.Find("ColumnsRow");
            if (columnsRow != null)
            {
                var crLe = columnsRow.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(columnsRow.gameObject);
                Undo.RecordObject(crLe, "columns le");
                crLe.flexibleHeight = 0f;
                EditorUtility.SetDirty(crLe);

                // ── 4. Sections : contenu centré (équilibre colonnes inégales) ──
                CenterSection(columnsRow, "SacrificeSection");
                CenterSection(columnsRow, "GainSection");
            }
            else Debug.LogWarning("[Sacrifice] ColumnsRow introuvable sous ComparisonContainer.");

            // ── 5. Spacer flexible juste avant la comparaison ──
            EnsureTopSpacer(container.transform, comparison.transform);

            Debug.Log("[Sacrifice] Layout A : comparaison compacte ancrée en bas, respiration sombre au-dessus.");
        }

        /// <summary> Crée (ou réutilise) un spacer vide à flexibleHeight 1, placé juste avant la comparaison. </summary>
        private static void EnsureTopSpacer(Transform container, Transform comparison)
        {
            var existing = container.Find(SpacerName);
            GameObject spacer;
            if (existing != null) spacer = existing.gameObject;
            else
            {
                spacer = new GameObject(SpacerName, typeof(RectTransform));
                ((RectTransform)spacer.transform).SetParent(container, false);
                Undo.RegisterCreatedObjectUndo(spacer, "create spacer");
            }

            var le = spacer.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(spacer);
            Undo.RecordObject(le, "spacer le");
            le.flexibleHeight = 1f; le.minHeight = 0f; le.preferredHeight = 0f;
            EditorUtility.SetDirty(le);

            // Toujours juste avant la comparaison
            spacer.transform.SetSiblingIndex(comparison.GetSiblingIndex());
        }

        private static void CenterSection(Transform columnsRow, string name)
        {
            var sec = columnsRow.Find(name);
            if (sec == null) { Debug.LogWarning($"[Sacrifice] Section {name} introuvable."); return; }
            var vlg = sec.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) return;
            Undo.RecordObject(vlg, "section center");
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandHeight = false;
            EditorUtility.SetDirty(vlg);
        }

        private static void RemoveComponent<T>(GameObject go) where T : Component
        { var c = go.GetComponent<T>(); if (c != null) Undo.DestroyObjectImmediate(c); }

        private static void Dialog(string m) { EditorUtility.DisplayDialog("Rebuild vertical", m, "OK"); }
    }
}
#endif
