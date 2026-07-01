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
    /// Gate 6c.1 — Bascule chaque slot de sacrifice en TUILE VERTICALE :
    /// icône en haut, nom centré dessous. Niveau masqué dans la tuile (il reste
    /// affiché dans la comparaison). Overlays (SelectionRing, RarityAccent) passés
    /// en ignoreLayout pour rester des surcouches, pas des éléments de flux.
    ///
    /// SLOT-INTERNE UNIQUEMENT : ne touche PAS SlotsScroll / Viewport / RectMask2D /
    /// ScrollRect (chaîne fragile) — la tuile tient dans la hauteur actuelle.
    /// Non-destructif, idempotent, Undo-safe. Adresse tout par NOM. Ne reparente rien
    /// (bascule HLG→VLG + réordonne des siblings existants). Opère sur les instances
    /// en scène (overrides — cohérent avec le styler pastille ; bake prefab plus tard).
    /// La distribution « remplir la largeur » = Gate 6c.2, séparé.
    /// </summary>
    public static class SacrificeSlotTileStyler
    {
        // ── Leviers d'ajustement ──
        private const float ICON        = 68f; // taille de l'IconFrame (grosse icône, tient dans la hauteur actuelle)
        private const float NAME_FONT   = 22f; // taille de PastilleName dans la tuile
        private const float SLOT_SPACING = 4f; // espace icône ↔ nom
        private const int   SLOT_PAD     = 4;  // padding intérieur de la tuile
        private const bool  HIDE_LEVEL   = true; // niveau masqué dans la tuile (affiché dans la comparaison)

        // ── Noms d'objets (stables) ──
        private const string PANEL_NAME     = "SacrificePanel";
        private const string CONTAINER_NAME = "Container";
        private const string SLOTS_GRID     = "SlotsGrid";
        private const string ICON_FRAME     = "IconFrame";
        private const string TEXT_COL       = "TextCol";
        private const string NAME_TXT       = "PastilleName";
        private const string LEVEL_TXT      = "PastilleLevel";
        private const string SELECTION_RING = "SelectionRing";
        private const string RARITY_ACCENT  = "RarityAccent";

        [MenuItem("Take Five Games/UI/Slots vers tuiles verticales (6c.1)")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject panel = FindByName(scene, PANEL_NAME);
            if (panel == null) { Debug.LogError($"[6c.1] '{PANEL_NAME}' introuvable."); return; }

            Transform container = FindDescendant(panel.transform, CONTAINER_NAME);
            Transform grid = container != null ? FindDescendant(container, SLOTS_GRID) : null;
            if (grid == null) { Debug.LogError($"[6c.1] '{SLOTS_GRID}' introuvable sous '{CONTAINER_NAME}'."); return; }

            var report = new StringBuilder();
            report.AppendLine("═══════ [6c.1] Slots → tuiles verticales — rapport ═══════");

            Undo.SetCurrentGroupName("Slots tuiles verticales (6c.1)");
            int group = Undo.GetCurrentGroup();

            int done = 0;
            foreach (Transform slot in grid)
            {
                if (StyleSlot(slot, report)) done++;
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);

            report.AppendLine($"═══ Terminé — {done} slot(s) traités. Active SacrificePanel + vue Game pour valider. ═══");
            Debug.Log(report.ToString());
        }

        private static bool StyleSlot(Transform slot, StringBuilder report)
        {
            report.AppendLine($"[{slot.name}]");

            // 1) Slot root : HLG → VLG (icône au-dessus du texte)
            VerticalLayoutGroup vlg = EnsureVLG(slot.gameObject, report);
            Undo.RecordObject(vlg, "Slot VLG");
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = SLOT_SPACING;
            vlg.padding = new RectOffset(SLOT_PAD, SLOT_PAD, SLOT_PAD, SLOT_PAD);
            Dirty(vlg);

            // 2) IconFrame : taille + placé en premier (en haut)
            Transform iconFrame = FindChild(slot, ICON_FRAME);
            if (iconFrame != null)
            {
                LayoutElement le = iconFrame.GetComponent<LayoutElement>();
                if (le == null) le = Undo.AddComponent<LayoutElement>(iconFrame.gameObject);
                Undo.RecordObject(le, "IconFrame size");
                le.preferredWidth = ICON;
                le.preferredHeight = ICON;
                Dirty(le);
                iconFrame.SetSiblingIndex(0);
                report.AppendLine($"    IconFrame → {ICON}x{ICON}, en haut.");
            }
            else report.AppendLine("    ⚠ IconFrame introuvable.");

            // 3) TextCol : sous l'icône, centré
            Transform textCol = FindChild(slot, TEXT_COL);
            if (textCol != null)
            {
                textCol.SetSiblingIndex(1);
                VerticalLayoutGroup tvlg = textCol.GetComponent<VerticalLayoutGroup>();
                if (tvlg != null)
                {
                    Undo.RecordObject(tvlg, "TextCol center");
                    tvlg.childAlignment = TextAnchor.MiddleCenter;
                    Dirty(tvlg);
                }
                // Nom centré + taille tuile
                Transform nameT = FindChild(textCol, NAME_TXT);
                TextMeshProUGUI nameTmp = nameT != null ? nameT.GetComponent<TextMeshProUGUI>() : null;
                if (nameTmp != null)
                {
                    Undo.RecordObject(nameTmp, "Name center");
                    nameTmp.alignment = TextAlignmentOptions.Center;
                    nameTmp.fontSize = NAME_FONT;
                    Dirty(nameTmp);
                    report.AppendLine($"    PastilleName → centré, font {NAME_FONT}.");
                }
                // Niveau : masqué dans la tuile (affiché dans la comparaison)
                Transform lvlT = FindChild(textCol, LEVEL_TXT);
                if (lvlT != null)
                {
                    Undo.RecordObject(lvlT.gameObject, "Hide level in tile");
                    lvlT.gameObject.SetActive(!HIDE_LEVEL);
                    DirtyGo(lvlT.gameObject);
                    report.AppendLine($"    PastilleLevel → {(HIDE_LEVEL ? "masqué (dans la comparaison)" : "affiché")}.");
                }
            }
            else report.AppendLine("    ⚠ TextCol introuvable.");

            // 4) Overlays : ignoreLayout (ne pas les faire flotter dans le VLG)
            SetIgnoreLayout(slot, SELECTION_RING, report);
            SetIgnoreLayout(slot, RARITY_ACCENT, report);

            return true;
        }

        private static VerticalLayoutGroup EnsureVLG(GameObject go, StringBuilder report)
        {
            HorizontalLayoutGroup hlg = go.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                Undo.DestroyObjectImmediate(hlg);
                report.AppendLine("    HLG → VLG (bascule verticale).");
            }
            VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = Undo.AddComponent<VerticalLayoutGroup>(go);
            return vlg;
        }

        private static void SetIgnoreLayout(Transform slot, string childName, StringBuilder report)
        {
            Transform t = FindChild(slot, childName);
            if (t == null) return;
            LayoutElement le = t.GetComponent<LayoutElement>();
            if (le == null) le = Undo.AddComponent<LayoutElement>(t.gameObject);
            if (!le.ignoreLayout)
            {
                Undo.RecordObject(le, "ignoreLayout");
                le.ignoreLayout = true;
                Dirty(le);
                report.AppendLine($"    {childName} → ignoreLayout (surcouche).");
            }
        }

        // ═══════════════ HELPERS ═══════════════
        private static GameObject FindByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }

        private static Transform FindDescendant(Transform parent, string name)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
                if (t != parent && t.name == name) return t;
            return null;
        }

        // Cherche un descendant par nom SOUS un slot (nom exact).
        private static Transform FindChild(Transform parent, string name) => FindDescendant(parent, name);

        private static void Dirty(Object o)
        {
            EditorUtility.SetDirty(o);
            if (o is Component comp) PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
        }

        private static void DirtyGo(GameObject go)
        {
            EditorUtility.SetDirty(go);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }
    }
}
