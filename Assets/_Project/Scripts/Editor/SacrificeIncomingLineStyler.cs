using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Polish pré-comparaison — réduit la carte « Bonus entrant » à une LIGNE UNIQUE.
    /// Masque les 5 éléments redondants (icône, fond de badge, anneau, niveau, rareté) ;
    /// la ligne elle-même est composée au RUNTIME dans incomingNameText (voir prompt Cursor).
    ///
    /// Lit les refs sérialisées de SacrificeUI PAR NOM DE CHAMP (SerializedObject) — donc
    /// zéro risque de desync de nom d'objet. S'AUTO-AUDITE : dumpe la structure interne du
    /// conteneur (pour caler un éventuel réglage fin de largeur/hauteur ensuite).
    ///
    /// Ne touche PAS la géométrie (hauteur du bandeau / ancrage du nom) : la carte
    /// content-hug se resserre d'elle-même quand les éléments sont masqués — on lit le dump
    /// avant tout réglage. Seule prop du nom touchée : word-wrap ON (évite un débordement
    /// hors du bandeau au premier rendu). Non-destructif, idempotent, Undo-safe.
    /// </summary>
    public static class SacrificeIncomingLineStyler
    {
        private const string UI_TYPE = "SacrificeUI";

        // Champs sérialisés à MASQUER (noms EXACTS des champs de SacrificeUI).
        private static readonly string[] ToHide =
        {
            "incomingIconImage",
            "incomingBadgeBackground",
            "incomingFrameRing",
            "incomingValueText",
            "incomingRarityText",
        };

        private const string F_CONTAINER = "incomingContainer";
        private const string F_NAME      = "incomingNameText";

        [MenuItem("Take Five Games/UI/Bonus entrant → ligne unique")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            MonoBehaviour ui = FindByTypeName(scene, UI_TYPE);
            if (ui == null) { Debug.LogError($"[Bonus entrant] Composant '{UI_TYPE}' introuvable dans la scène active."); return; }

            SerializedObject so = new SerializedObject(ui);
            var report = new StringBuilder();
            report.AppendLine("═══════ [Bonus entrant → ligne unique] rapport ═══════");

            Undo.SetCurrentGroupName("Bonus entrant → ligne unique");
            int group = Undo.GetCurrentGroup();

            // 1) Masquer les 5 éléments redondants
            foreach (string field in ToHide)
            {
                SerializedProperty p = so.FindProperty(field);
                if (p == null) { report.AppendLine($"    ⚠ champ '{field}' INTROUVABLE dans {UI_TYPE} (typo ?)."); continue; }
                Component c = p.objectReferenceValue as Component;
                if (c == null) { report.AppendLine($"    ⚠ '{field}' non câblé dans l'Inspector — ignoré."); continue; }
                GameObject go = c.gameObject;
                if (go.activeSelf)
                {
                    Undo.RecordObject(go, "Hide incoming element");
                    go.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
                    report.AppendLine($"    {field} → masqué ({go.name}).");
                }
                else report.AppendLine($"    {field} → déjà masqué ({go.name}).");
            }

            // 2) Nom : word-wrap ON (sécurité, évite un débordement de la longue ligne)
            SerializedProperty np = so.FindProperty(F_NAME);
            GameObject nameGo = null;
            if (np != null && np.objectReferenceValue is Component nc)
            {
                nameGo = nc.gameObject;
                TextMeshProUGUI tmp = nc as TextMeshProUGUI;
                if (tmp != null && !tmp.enableWordWrapping)
                {
                    Undo.RecordObject(tmp, "Name wrap");
                    tmp.enableWordWrapping = true;
                    EditorUtility.SetDirty(tmp);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(tmp);
                    report.AppendLine("    incomingNameText → word-wrap ON.");
                }
            }
            else report.AppendLine("    ⚠ incomingNameText non trouvé.");

            // 3) AUTO-AUDIT : structure interne du conteneur (pour le réglage fin)
            SerializedProperty cp = so.FindProperty(F_CONTAINER);
            GameObject container = cp != null ? cp.objectReferenceValue as GameObject : null;
            if (container != null)
            {
                report.AppendLine($"── Structure de '{container.name}' (pour réglage fin largeur/hauteur) ──");
                DumpTree(container.transform, nameGo, report, 0);
            }
            else report.AppendLine("    ⚠ incomingContainer non trouvé — pas de dump.");

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            report.AppendLine("═══ Terminé. Applique le prompt Cursor pour composer la ligne, puis valide en vue Game. ═══");
            Debug.Log(report.ToString());
        }

        private static void DumpTree(Transform t, GameObject nameGo, StringBuilder sb, int depth)
        {
            string indent = new string(' ', depth * 2);
            string tag = (nameGo != null && t.gameObject == nameGo) ? "  ← incomingNameText" : "";

            LayoutElement le = t.GetComponent<LayoutElement>();
            string leStr = le != null ? $" LE(prefH={le.preferredHeight:0.#},prefW={le.preferredWidth:0.#},flexW={le.flexibleWidth:0.#})" : "";

            string groups = "";
            if (t.GetComponent<VerticalLayoutGroup>() != null) groups += " VLG";
            if (t.GetComponent<HorizontalLayoutGroup>() != null) groups += " HLG";
            if (t.GetComponent<ContentSizeFitter>() != null) groups += " CSF";

            RectTransform rt = t as RectTransform;
            string geo = rt != null
                ? $" a[{rt.anchorMin.x:0.##},{rt.anchorMin.y:0.##}→{rt.anchorMax.x:0.##},{rt.anchorMax.y:0.##}] size[{rt.rect.width:0.#}x{rt.rect.height:0.#}]"
                : "";

            sb.AppendLine($"    {indent}{t.name} (active={t.gameObject.activeSelf}){leStr}{groups}{geo}{tag}");
            foreach (Transform child in t) DumpTree(child, nameGo, sb, depth + 1);
        }

        private static MonoBehaviour FindByTypeName(Scene scene, string typeName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb != null && mb.GetType().Name == typeName) return mb;
            return null;
        }
    }
}
