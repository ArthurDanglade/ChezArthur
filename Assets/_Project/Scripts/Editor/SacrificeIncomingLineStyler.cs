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
    /// La ligne elle-même est composée au RUNTIME dans incomingNameText (voir prompt Cursor).
    ///
    /// Cet outil s'occupe de la STRUCTURE et de la GÉOMÉTRIE :
    ///   1. Masque les 5 éléments redondants câblés (icône, badge, anneau, niveau, rareté).
    ///      Ils sont lus PAR NOM DE CHAMP (SerializedObject) → zéro risque de desync de nom.
    ///   2. Masque le label statique ReceiveLabel (« Tu reçois ») — doublon du préfixe de la
    ///      ligne. Trouvé PAR NOM dans le conteneur (ce n'est pas un champ sérialisé), avec
    ///      log si absent (jamais de no-op silencieux).
    ///   3. Ligne en PLEINE LARGEUR : TextBlock VLG force-expand + word-wrap ON + aligné à
    ///      gauche → ligne unique quand ça rentre, wrap propre sinon, jamais de débordement.
    ///   4. Bandeau en CONTENT-HUG (avec plancher) : coupe la hauteur fixe de 124 px pour
    ///      supprimer le vide sous la ligne.
    ///
    /// Non-destructif, idempotent, Undo-safe. Auto-auditant (dumpe la structure reconstruite).
    /// Réglages en LEVIERS ci-dessous (passer en hauteur fixe si le content-hug se comporte mal).
    /// </summary>
    public static class SacrificeIncomingLineStyler
    {
        private const string UI_TYPE = "SacrificeUI";

        // ── Leviers ──
        private const float BAND_MIN_HEIGHT = 48f;   // plancher du bandeau (1 ligne + marge)
        // preferredHeight passé à -1 (auto/content-hug). Pour un rendu déterministe, mettre
        // BAND_MIN_HEIGHT ici aussi et forcer preferredHeight = BAND_MIN_HEIGHT plus bas.

        // Champs sérialisés à MASQUER (noms EXACTS des champs de SacrificeUI).
        private static readonly string[] SerializedToHide =
        {
            "incomingIconImage",
            "incomingBadgeBackground",
            "incomingFrameRing",
            "incomingValueText",
            "incomingRarityText",
        };

        private const string F_CONTAINER   = "incomingContainer";
        private const string F_NAME        = "incomingNameText";
        private const string N_RECEIVELBL  = "ReceiveLabel";  // label statique « Tu reçois » (par nom)
        private const string N_TEXTBLOCK   = "TextBlock";     // colonne VLB qui porte la ligne (par nom)

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

            // 1) Masquer les 5 éléments redondants (par champ sérialisé)
            foreach (string field in SerializedToHide)
            {
                SerializedProperty p = so.FindProperty(field);
                if (p == null) { report.AppendLine($"    ⚠ champ '{field}' INTROUVABLE dans {UI_TYPE} (typo ?)."); continue; }
                Component c = p.objectReferenceValue as Component;
                if (c == null) { report.AppendLine($"    ⚠ '{field}' non câblé dans l'Inspector — ignoré."); continue; }
                HideGo(c.gameObject, field, report);
            }

            // Conteneur (référence sérialisée)
            SerializedProperty cp = so.FindProperty(F_CONTAINER);
            GameObject container = cp != null ? cp.objectReferenceValue as GameObject : null;
            if (container == null) { report.AppendLine($"    ⚠ '{F_CONTAINER}' introuvable — réglages géométriques ignorés."); }

            // 2) Masquer le label statique ReceiveLabel (doublon « Tu reçois »), par NOM
            if (container != null)
            {
                Transform rl = FindDescendant(container.transform, N_RECEIVELBL);
                if (rl == null) report.AppendLine($"    ⚠ '{N_RECEIVELBL}' introuvable dans le conteneur — non masqué.");
                else HideGo(rl.gameObject, N_RECEIVELBL + " (doublon « Tu reçois »)", report);
            }

            // 3a) Nom : word-wrap ON + aligné à gauche
            SerializedProperty np = so.FindProperty(F_NAME);
            GameObject nameGo = null;
            if (np != null && np.objectReferenceValue is Component nc)
            {
                nameGo = nc.gameObject;
                TextMeshProUGUI tmp = nc as TextMeshProUGUI;
                if (tmp != null)
                {
                    Undo.RecordObject(tmp, "Name display");
                    tmp.enableWordWrapping = true;
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                    EditorUtility.SetDirty(tmp);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(tmp);
                    report.AppendLine("    incomingNameText → word-wrap ON, aligné à gauche.");
                }
            }
            else report.AppendLine($"    ⚠ '{F_NAME}' non trouvé.");

            // 3b) TextBlock : ligne en pleine largeur
            if (container != null)
            {
                Transform tb = FindDescendant(container.transform, N_TEXTBLOCK);
                if (tb == null) report.AppendLine($"    ⚠ '{N_TEXTBLOCK}' introuvable — largeur de ligne non ajustée.");
                else
                {
                    VerticalLayoutGroup vlg = tb.GetComponent<VerticalLayoutGroup>();
                    if (vlg == null) report.AppendLine($"    ⚠ '{N_TEXTBLOCK}' sans VLG — largeur non ajustée.");
                    else
                    {
                        Undo.RecordObject(vlg, "TextBlock width");
                        vlg.childControlWidth = true;
                        vlg.childForceExpandWidth = true;
                        EditorUtility.SetDirty(vlg);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(vlg);
                        report.AppendLine("    TextBlock VLG → childControlWidth + forceExpandWidth (ligne pleine largeur).");
                    }
                }
            }

            // 4) Bandeau : content-hug avec plancher (coupe la hauteur fixe)
            if (container != null)
            {
                LayoutElement le = container.GetComponent<LayoutElement>();
                if (le == null) report.AppendLine("    ⚠ IncomingContainer sans LayoutElement — hauteur non réduite.");
                else
                {
                    float oldMin = le.minHeight, oldPref = le.preferredHeight;
                    Undo.RecordObject(le, "Band height");
                    le.minHeight = BAND_MIN_HEIGHT;
                    le.preferredHeight = -1f;   // auto / content-hug
                    EditorUtility.SetDirty(le);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(le);
                    report.AppendLine($"    IncomingContainer hauteur → minH {oldMin:0}→{BAND_MIN_HEIGHT:0}, prefH {oldPref:0}→auto (content-hug).");
                }
            }

            // Rebuild pour que le dump reflète la nouvelle géométrie
            if (container != null && container.transform is RectTransform crt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(crt);

            // 5) AUTO-AUDIT : structure reconstruite
            if (container != null)
            {
                report.AppendLine($"── Structure de '{container.name}' (après réglages) ──");
                DumpTree(container.transform, nameGo, report, 0);
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            report.AppendLine("═══ Terminé. Vérifie la ligne en vue Game. ═══");
            Debug.Log(report.ToString());
        }

        private static void HideGo(GameObject go, string label, StringBuilder report)
        {
            if (go.activeSelf)
            {
                Undo.RecordObject(go, "Hide");
                go.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
                report.AppendLine($"    {label} → masqué ({go.name}).");
            }
            else report.AppendLine($"    {label} → déjà masqué ({go.name}).");
        }

        private static void DumpTree(Transform t, GameObject nameGo, StringBuilder sb, int depth)
        {
            string indent = new string(' ', depth * 2);
            string tag = (nameGo != null && t.gameObject == nameGo) ? "  ← incomingNameText" : "";

            LayoutElement le = t.GetComponent<LayoutElement>();
            string leStr = le != null ? $" LE(prefH={le.preferredHeight:0.#},minH={le.minHeight:0.#},flexW={le.flexibleWidth:0.#})" : "";

            string groups = "";
            if (t.GetComponent<VerticalLayoutGroup>() != null) groups += " VLG";
            if (t.GetComponent<HorizontalLayoutGroup>() != null) groups += " HLG";
            if (t.GetComponent<ContentSizeFitter>() != null) groups += " CSF";

            RectTransform rt = t as RectTransform;
            string geo = rt != null ? $" size[{rt.rect.width:0.#}x{rt.rect.height:0.#}]" : "";

            sb.AppendLine($"    {indent}{t.name} (active={t.gameObject.activeSelf}){leStr}{groups}{geo}{tag}");
            foreach (Transform child in t) DumpTree(child, nameGo, sb, depth + 1);
        }

        private static Transform FindDescendant(Transform parent, string name)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
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
