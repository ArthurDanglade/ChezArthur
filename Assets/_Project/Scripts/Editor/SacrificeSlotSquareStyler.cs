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
    /// Gate 6c.2 (partie ÉDITEUR, révisé) — Slots en CARRÉS + nom calé + icône recentrée
    /// + aperçu d'espacement PLAFONNÉ et CENTRÉ.
    /// - Chaque slot : carré fixe TILE x TILE.
    /// - Nom : contraint + auto-size + wrap pour tenir dans la tuile.
    /// - PastilleIcon : recentrée dans son cadre (marges symétriques) — corrige l'icône décalée.
    /// - Aperçu d'espacement : plafonné à MAX_GAP et centré (le RUNTIME fait foi — prompt Cursor 6c.2).
    ///
    /// Ne touche PAS SelectionRing/RarityRing (visuel de sélection traité séparément).
    /// Non-destructif, idempotent, Undo-safe.
    /// </summary>
    public static class SacrificeSlotSquareStyler
    {
        // ── Leviers ──
        private const float TILE       = 110f; // côté de la tuile carrée
        private const float TEXT_WIDTH = 96f;  // largeur allouée au nom
        private const float NAME_MIN   = 12f;  // auto-size mini du nom
        private const float NAME_MAX   = 22f;  // auto-size maxi du nom
        private const float ICON_INSET = 8f;   // marge symétrique de l'icône dans son cadre (recentrage)
        private const float MAX_GAP    = 48f;  // écart max entre tuiles (évite le « trop éloigné » à peu de slots)

        // ── Noms ──
        private const string PANEL_NAME     = "SacrificePanel";
        private const string CONTAINER_NAME = "Container";
        private const string SLOTS_GRID     = "SlotsGrid";
        private const string TEXT_COL       = "TextCol";
        private const string NAME_TXT       = "PastilleName";
        private const string ICON_FRAME     = "IconFrame";
        private const string PASTILLE_ICON  = "PastilleIcon";

        [MenuItem("Take Five Games/UI/Slots en carrés + preview espacement (6c.2)")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject panel = FindByName(scene, PANEL_NAME);
            if (panel == null) { Debug.LogError($"[6c.2] '{PANEL_NAME}' introuvable."); return; }

            Transform container = FindDescendant(panel.transform, CONTAINER_NAME);
            Transform grid = container != null ? FindDescendant(container, SLOTS_GRID) : null;
            if (grid == null) { Debug.LogError($"[6c.2] '{SLOTS_GRID}' introuvable."); return; }

            var report = new StringBuilder();
            report.AppendLine("═══════ [6c.2 révisé] Carrés + icône recentrée + preview plafonné ═══════");

            Undo.SetCurrentGroupName("Slots carrés révisé (6c.2)");
            int group = Undo.GetCurrentGroup();

            int count = 0, activeCount = 0;
            foreach (Transform slot in grid)
            {
                SquareSlot(slot, report);
                count++;
                if (slot.gameObject.activeSelf) activeCount++;
            }

            // Aperçu d'espacement : plafonné + centré (le runtime recalcule pour le vrai N).
            HorizontalLayoutGroup hlg = grid.GetComponent<HorizontalLayoutGroup>();
            RectTransform viewport = grid.parent as RectTransform;
            if (hlg != null && viewport != null)
            {
                Undo.RecordObject(hlg, "Preview spacing");
                hlg.childAlignment = TextAnchor.MiddleCenter;
                float railW = viewport.rect.width;
                if (railW > 1f && activeCount > 1)
                {
                    float spaceBetween = (railW - activeCount * TILE) / (activeCount - 1);
                    hlg.spacing = Mathf.Min(Mathf.Max(0f, spaceBetween), MAX_GAP);
                    report.AppendLine($"[Preview] rail={railW:0.#}px, {activeCount} slots → spacing={hlg.spacing:0.#} (plafond {MAX_GAP}, centré ; runtime recalcule).");
                }
                else
                {
                    report.AppendLine($"[Preview] rail non mesurable / <2 slots ({railW:0.#}px, {activeCount}) — laissé au runtime. Active SacrificePanel et relance pour l'aperçu.");
                }
                Dirty(hlg);
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            report.AppendLine($"═══ Terminé — {count} slot(s). ═══");
            Debug.Log(report.ToString());
        }

        private static void SquareSlot(Transform slot, StringBuilder report)
        {
            report.AppendLine($"[{slot.name}]");

            // Carré fixe
            LayoutElement le = slot.GetComponent<LayoutElement>();
            if (le == null) le = Undo.AddComponent<LayoutElement>(slot.gameObject);
            Undo.RecordObject(le, "Slot square");
            le.preferredWidth = TILE;
            le.preferredHeight = TILE;
            Dirty(le);

            // Nom : contraint + auto-size + wrap
            Transform textCol = FindDescendant(slot, TEXT_COL);
            if (textCol != null)
            {
                LayoutElement tle = textCol.GetComponent<LayoutElement>();
                if (tle == null) tle = Undo.AddComponent<LayoutElement>(textCol.gameObject);
                Undo.RecordObject(tle, "TextCol width");
                tle.preferredWidth = TEXT_WIDTH;
                tle.flexibleWidth = 0f;
                Dirty(tle);

                VerticalLayoutGroup tvlg = textCol.GetComponent<VerticalLayoutGroup>();
                if (tvlg != null)
                {
                    Undo.RecordObject(tvlg, "TextCol control width");
                    tvlg.childControlWidth = true;
                    tvlg.childForceExpandWidth = true;
                    Dirty(tvlg);
                }

                Transform nameT = FindDescendant(textCol, NAME_TXT);
                TextMeshProUGUI tmp = nameT != null ? nameT.GetComponent<TextMeshProUGUI>() : null;
                if (tmp != null)
                {
                    Undo.RecordObject(tmp, "Name autosize");
                    tmp.enableWordWrapping = true;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = NAME_MIN;
                    tmp.fontSizeMax = NAME_MAX;
                    tmp.overflowMode = TextOverflowModes.Ellipsis;
                    Dirty(tmp);
                }
            }

            // Icône recentrée dans son cadre (marges symétriques) — corrige le décalage
            Transform iconFrame = FindDescendant(slot, ICON_FRAME);
            if (iconFrame != null)
            {
                Transform icon = FindDescendant(iconFrame, PASTILLE_ICON);
                RectTransform irt = icon as RectTransform;
                if (irt != null)
                {
                    Undo.RecordObject(irt, "Center icon");
                    irt.anchorMin = Vector2.zero;
                    irt.anchorMax = Vector2.one;
                    irt.offsetMin = new Vector2(ICON_INSET, ICON_INSET);
                    irt.offsetMax = new Vector2(-ICON_INSET, -ICON_INSET);
                    Dirty(irt);
                    report.AppendLine($"    PastilleIcon → recentrée (marge {ICON_INSET} symétrique).");
                }
                else report.AppendLine("    ⚠ PastilleIcon introuvable.");
            }
        }

        // ═══════ HELPERS ═══════
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

        private static void Dirty(Object o)
        {
            EditorUtility.SetDirty(o);
            if (o is Component comp) PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
        }
    }
}
