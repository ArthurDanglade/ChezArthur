using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Gate 6b.1 (carte centrée) — Restructure l'écran de sacrifice en modale flottante.
    /// Overlay opaque → scrim ; Container plein-écran → CARTE CENTRÉE, marges latérales
    /// (l'arène l'encadre de tous côtés), hauteur qui ÉPOUSE LE CONTENU, et aération
    /// interne (spacing + padding) pour que ça respire.
    ///
    /// Non-destructif, idempotent, Undo-safe. N'écrit AUCUN champ sérialisé de
    /// SacrificeUI : tous les objets sont adressés par NOM (stable). Ne reparente rien
    /// → aucune contrainte d'instance de prefab. Ne touche PAS le remplissage interne
    /// des colonnes (Gate 6c/6d) : logue le layout interne pour l'étape d'après.
    /// Remplace la version précédente (même menu, même classe).
    /// </summary>
    public static class SacrificeBottomSheetRebuilder
    {
        // ── Leviers d'ajustement (les seuls chiffres à toucher) ──
        private const float OVERLAY_ALPHA    = 0.60f; // scrim : 0.75 avant, on laisse respirer l'arène
        private const float SIDE_MARGIN      = 48f;   // marge gauche/droite : voir le fond sur les côtés (« pas trop »)
        private const float VERTICAL_OFFSET  = 0f;    // 0 = pile centrée ; positif = monte, négatif = descend
        private const float SECTION_SPACING  = 28f;   // air entre les grandes sections
        private const int   INNER_PAD_H      = 28;    // padding intérieur gauche/droite
        private const int   INNER_PAD_V      = 32;    // padding intérieur haut/bas (respiration au-dessus du titre / sous le bouton)

        // ── Noms d'objets (stables) ──
        private const string PANEL_NAME     = "SacrificePanel";
        private const string OVERLAY_NAME   = "Overlay";
        private const string CONTAINER_NAME = "Container";
        private const string SPACER_NAME    = "ComparisonTopSpacer";
        private const string ROUNDED_SPRITE = "card_rounded";

        [MenuItem("Take Five Games/UI/Rebuild bottom-sheet sacrifice")]
        public static void Rebuild()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[6b.1] Aucune scène active valide.");
                return;
            }

            GameObject panel = FindByNameIncludingInactive(scene, PANEL_NAME);
            if (panel == null)
            {
                Debug.LogError($"[6b.1] Introuvable : '{PANEL_NAME}' dans la scène '{scene.name}'.");
                return;
            }

            Transform overlay   = FindDescendantByName(panel.transform, OVERLAY_NAME);
            Transform container = FindDescendantByName(panel.transform, CONTAINER_NAME);
            if (overlay == null || container == null)
            {
                Debug.LogError($"[6b.1] Sous '{PANEL_NAME}' : Overlay={overlay != null}, Container={container != null}. Les deux sont requis.");
                return;
            }

            var report = new StringBuilder();
            report.AppendLine("═══════════ [6b.1 carte centrée] Modale flottante centrée + aérée — rapport ═══════════");

            Undo.SetCurrentGroupName("Sacrifice carte centrée (6b.1)");
            int group = Undo.GetCurrentGroup();

            // ── 1) OVERLAY : opaque → scrim ──
            Image overlayImg = overlay.GetComponent<Image>();
            if (overlayImg != null)
            {
                Undo.RecordObject(overlayImg, "Overlay scrim");
                Color c = overlayImg.color;
                report.AppendLine($"[Overlay] alpha {c.a:0.###} → {OVERLAY_ALPHA:0.###}  (RGB + raycast target conservés)");
                c.a = OVERLAY_ALPHA;
                overlayImg.color = c;
                overlayImg.raycastTarget = true;
                MarkDirty(overlayImg);
            }
            else report.AppendLine("[Overlay] ⚠ pas de composant Image — alpha non modifié.");

            // ── 2) CONTAINER : carte centrée, marges latérales, hauteur = contenu ──
            RectTransform crt = container.GetComponent<RectTransform>();
            Undo.RecordObject(crt, "Container carte centrée");
            report.AppendLine($"[Container] AVANT : anchorMin={V(crt.anchorMin)} anchorMax={V(crt.anchorMax)} sizeDelta={V(crt.sizeDelta)} pivot={V(crt.pivot)} anchoredPos={V(crt.anchoredPosition)}");
            crt.anchorMin = new Vector2(0f, 0.5f);   // X pleine largeur, Y ancré au centre
            crt.anchorMax = new Vector2(1f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f); // centré, grandit symétriquement autour du centre
            crt.offsetMin = new Vector2(SIDE_MARGIN, crt.offsetMin.y);   // marge gauche
            crt.offsetMax = new Vector2(-SIDE_MARGIN, crt.offsetMax.y);  // marge droite
            crt.anchoredPosition = new Vector2(0f, VERTICAL_OFFSET);      // centrage vertical (+/- réglable)
            report.AppendLine($"[Container] APRÈS : anchorMin={V(crt.anchorMin)} anchorMax={V(crt.anchorMax)} pivot={V(crt.pivot)} anchoredPos={V(crt.anchoredPosition)} margeLat={SIDE_MARGIN} (hauteur = contenu)");
            MarkDirty(crt);

            // ── 3) Garde-fou : SafeAreaFitter écraserait l'ancrage de la carte ──
            foreach (MonoBehaviour mb in container.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                string typeName = mb.GetType().Name;
                if (typeName.Contains("SafeArea") && mb.enabled)
                {
                    Undo.RecordObject(mb, "Disable SafeAreaFitter");
                    mb.enabled = false;
                    report.AppendLine($"[Garde-fou] '{typeName}' désactivé (l'ancrage de la carte prime ; insets safe-area → gate panel-layout).");
                    MarkDirty(mb);
                }
            }

            // ── 4) CŒUR DU FIX : la carte épouse son contenu + aération interne ──
            VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                Undo.RecordObject(vlg, "VLG content-driven + aération");
                report.AppendLine($"[VLG] AVANT : childControlHeight={vlg.childControlHeight} childForceExpandHeight={vlg.childForceExpandHeight} " +
                                  $"childAlignment={vlg.childAlignment} spacing={vlg.spacing} padding=(L{vlg.padding.left},T{vlg.padding.top},R{vlg.padding.right},B{vlg.padding.bottom})");
                vlg.childControlHeight = true;                  // requis : le CSF lit la hauteur préférée du VLG
                vlg.childForceExpandHeight = false;             // pas d'étirement égal des enfants
                vlg.spacing = SECTION_SPACING;                  // air entre sections
                vlg.padding = new RectOffset(INNER_PAD_H, INNER_PAD_H, INNER_PAD_V, INNER_PAD_V); // padding intérieur
                report.AppendLine($"[VLG] APRÈS : childControlHeight={vlg.childControlHeight} childForceExpandHeight={vlg.childForceExpandHeight} spacing={vlg.spacing} padding=(L{INNER_PAD_H},T{INNER_PAD_V},R{INNER_PAD_H},B{INNER_PAD_V})");
                MarkDirty(vlg);

                ContentSizeFitter csf = container.GetComponent<ContentSizeFitter>();
                if (csf == null)
                {
                    csf = Undo.AddComponent<ContentSizeFitter>(container.gameObject);
                    report.AppendLine("[CSF] ajouté sur Container.");
                }
                Undo.RecordObject(csf, "CSF content hug");
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // largeur = anchors + marges latérales
                csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;  // hauteur = contenu
                report.AppendLine("[CSF] horizontalFit=Unconstrained, verticalFit=PreferredSize → la carte épouse la hauteur du contenu.");
                MarkDirty(csf);
            }
            else
            {
                report.AppendLine("[VLG] ⚠ absent sur Container — content-sizing impossible, CSF non appliqué. Ajoute un VerticalLayoutGroup et relance.");
            }

            // ── 5) Coins arrondis (best-effort) — la carte flotte, les 4 coins sont désormais visibles ──
            Image containerImg = container.GetComponent<Image>();
            if (containerImg != null)
            {
                Sprite rounded = LoadSpriteByName(ROUNDED_SPRITE);
                if (rounded != null)
                {
                    if (containerImg.sprite != rounded || containerImg.type != Image.Type.Sliced)
                    {
                        Undo.RecordObject(containerImg, "Card rounded sprite");
                        containerImg.sprite = rounded;
                        containerImg.type = Image.Type.Sliced;
                        report.AppendLine($"[Fond carte] sprite '{ROUNDED_SPRITE}' (9-slice) → 4 coins arrondis. Couleur inchangée.");
                        MarkDirty(containerImg);
                    }
                    else report.AppendLine($"[Fond carte] déjà en '{ROUNDED_SPRITE}' 9-slice (idempotent).");
                }
                else report.AppendLine($"[Fond carte] ⚠ sprite '{ROUNDED_SPRITE}' introuvable — coins laissés tels quels.");
            }

            // ── 6) ComparisonTopSpacer : neutralisé (relique du layout plein-écran) ──
            Transform spacer = FindDescendantByName(container, SPACER_NAME);
            if (spacer != null)
            {
                LayoutElement le = spacer.GetComponent<LayoutElement>();
                if (le != null && le.flexibleHeight != 0f)
                {
                    Undo.RecordObject(le, "Neutralize spacer");
                    report.AppendLine($"[Spacer] flexibleHeight {le.flexibleHeight} → 0 (relique « remplir la safe area »).");
                    le.flexibleHeight = 0f;
                    MarkDirty(le);
                }
                else report.AppendLine("[Spacer] déjà neutre (flex 0) ou sans LayoutElement.");
            }
            else report.AppendLine("[Spacer] absent (ok).");

            // ── 7) AUDIT LECTURE-SEULE : enfants directs (pour le remplissage interne 6c/6d) ──
            report.AppendLine("── Audit enfants directs du Container (LayoutElement) ──");
            foreach (Transform child in container)
            {
                LayoutElement le = child.GetComponent<LayoutElement>();
                if (le != null)
                    report.AppendLine($"    {child.name} : min={le.minHeight} pref={le.preferredHeight} flex={le.flexibleHeight} ignore={le.ignoreLayout} (actif={child.gameObject.activeSelf})");
                else
                    report.AppendLine($"    {child.name} : aucun LayoutElement (actif={child.gameObject.activeSelf})");
            }

            // ── 8) Rebuild immédiat si le panel est actif (sinon au prochain SetActive) ──
            if (container.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
                report.AppendLine($"[Rebuild] taille carte = {crt.rect.width:0.#} x {crt.rect.height:0.#} px.");
            }
            else
            {
                report.AppendLine("[Rebuild] SacrificePanel inactif → taille calculée à son activation (active-le pour valider).");
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);

            report.AppendLine("═══ Terminé. Active temporairement SacrificePanel + regarde la vue Game. ═══");
            Debug.Log(report.ToString());
        }

        // ═══════════════════════════════════════════ HELPERS ═══════════════════════════════════════════
        private static GameObject FindByNameIncludingInactive(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }

        private static Transform FindDescendantByName(Transform parent, string name)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
                if (t != parent && t.name == name) return t;
            return null;
        }

        private static Sprite LoadSpriteByName(string spriteName)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{spriteName} t:Sprite"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null && s.name == spriteName) return s;
                foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (obj is Sprite sub && sub.name == spriteName) return sub;
            }
            return null;
        }

        private static void MarkDirty(Object o)
        {
            EditorUtility.SetDirty(o);
            if (o is Component comp)
                PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
        }

        private static string V(Vector2 v) => $"({v.x:0.###},{v.y:0.###})";
    }
}
