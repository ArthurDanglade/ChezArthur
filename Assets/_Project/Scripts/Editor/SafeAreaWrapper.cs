#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using ChezArthur.UI; // SafeAreaFitter (runtime)

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Crée un conteneur "SafeArea" plein écran (avec SafeAreaFitter) sous le Canvas et y déplace
    /// les objets sélectionnés, en préservant leur ordre. Laisse hors sélection les éléments qui
    /// doivent déborder (fonds, voiles de popups). Menu : Take Five Games > UI > Envelopper dans un SafeArea.
    /// </summary>
    public static class SafeAreaWrapper
    {
        [MenuItem("Take Five Games/UI/Envelopper la sélection dans un SafeArea")]
        public static void Wrap()
        {
            var objs = Selection.gameObjects;
            if (objs == null || objs.Length == 0)
            {
                EditorUtility.DisplayDialog("Sélection requise",
                    "Sélectionne le contenu à mettre dans la zone sûre (pages, NavigationBar, InfoBar).", "OK");
                return;
            }

            Transform parent = objs[0].transform.parent;
            if (parent == null)
            {
                EditorUtility.DisplayDialog("Parent manquant",
                    "Les objets doivent être enfants d'un Canvas.", "OK");
                return;
            }
            foreach (var o in objs)
            {
                if (o.transform.parent != parent)
                {
                    EditorUtility.DisplayDialog("Même parent requis",
                        "Tous les objets sélectionnés doivent partager le même parent (le Canvas).", "OK");
                    return;
                }
            }

            // Préserve l'ordre hiérarchique.
            System.Array.Sort(objs, (a, b) =>
                a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            int insertIndex = objs[0].transform.GetSiblingIndex();

            // SafeArea plein écran.
            var safe = new GameObject("SafeArea", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(safe, "Create SafeArea");
            var rt = (RectTransform)safe.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetSiblingIndex(insertIndex);
            Undo.AddComponent<SafeAreaFitter>(safe);

            // Déplace la sélection dedans (rect local préservé), ordre conservé.
            foreach (var o in objs)
            {
                Undo.SetTransformParent(o.transform, safe.transform, false, "Move to SafeArea");
                o.transform.SetAsLastSibling();
            }

            Selection.activeGameObject = safe;
            Debug.Log($"[SafeArea] {objs.Length} objet(s) enveloppé(s) dans un SafeArea sous '{parent.name}'.");
        }
    }
}
#endif
