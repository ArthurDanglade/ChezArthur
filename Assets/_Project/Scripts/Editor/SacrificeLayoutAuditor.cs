using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Audit LECTURE-SEULE du layout de l'écran de sacrifice (préparation Gate 6c).
    /// Ne modifie RIEN. Dumpe le Container, la chaîne des slots
    /// (SlotsScroll → ScrollRect → Content/SlotsGrid → slots) ET la structure
    /// interne d'un slot (icône / nom / niveau). Copie tout dans le presse-papier
    /// → colle direct (Ctrl+V) dans le chat.
    /// </summary>
    public static class SacrificeLayoutAuditor
    {
        private const string PANEL_NAME        = "SacrificePanel";
        private const string CONTAINER_NAME    = "Container";
        private const string SLOTS_SCROLL_NAME = "SlotsScroll";

        [MenuItem("Take Five Games/UI/Audit sacrifice layout (presse-papier)")]
        public static void Audit()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject panel = FindByName(scene, PANEL_NAME);
            if (panel == null) { Debug.LogError($"[Audit] '{PANEL_NAME}' introuvable dans la scène."); return; }

            Transform container = FindDescendant(panel.transform, CONTAINER_NAME);
            if (container == null) { Debug.LogError($"[Audit] '{CONTAINER_NAME}' introuvable sous '{PANEL_NAME}'."); return; }

            var sb = new StringBuilder();
            sb.AppendLine("=== AUDIT SACRIFICE LAYOUT (pour Gate 6c) ===");
            sb.AppendLine();

            // ── Container ──
            sb.AppendLine("[Container]");
            sb.AppendLine("  " + DescribeRect(container));
            VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
                sb.AppendLine($"  VLG: ctrlH={vlg.childControlHeight} expandH={vlg.childForceExpandHeight} ctrlW={vlg.childControlWidth} expandW={vlg.childForceExpandWidth} align={vlg.childAlignment} spacing={vlg.spacing} pad=(L{vlg.padding.left},T{vlg.padding.top},R{vlg.padding.right},B{vlg.padding.bottom})");
            ContentSizeFitter ccsf = container.GetComponent<ContentSizeFitter>();
            if (ccsf != null) sb.AppendLine($"  CSF: horiz={ccsf.horizontalFit} vert={ccsf.verticalFit}");
            sb.AppendLine("  Enfants directs :");
            foreach (Transform ch in container)
                sb.AppendLine("    " + DescribeChild(ch));
            sb.AppendLine();

            // ── Chaîne des slots ──
            Transform slotsScroll = FindDescendant(container, SLOTS_SCROLL_NAME);
            Transform firstSlot = null;
            if (slotsScroll == null)
            {
                sb.AppendLine($"[SlotsScroll] INTROUVABLE (nom = '{SLOTS_SCROLL_NAME}' ?).");
            }
            else
            {
                sb.AppendLine("[SlotsScroll — chaîne de layout des slots]");
                sb.AppendLine("  " + DescribeChild(slotsScroll));

                ScrollRect sr = slotsScroll.GetComponent<ScrollRect>();
                Transform content = null;
                if (sr != null)
                {
                    content = sr.content;
                    sb.AppendLine($"  ScrollRect: horizontal={sr.horizontal} vertical={sr.vertical} " +
                                  $"content={(content != null ? content.name : "null")} viewport={(sr.viewport != null ? sr.viewport.name : "null")}");
                }
                else sb.AppendLine("  (pas de ScrollRect sur SlotsScroll)");

                foreach (Transform t in slotsScroll)
                {
                    bool isViewport = t.GetComponent<RectMask2D>() != null || t.name.ToLower().Contains("viewport");
                    if (isViewport)
                        sb.AppendLine("  Viewport: " + DescribeRect(t) + (t.GetComponent<RectMask2D>() != null ? " [RectMask2D]" : ""));
                }

                if (content == null) content = FindDescendant(slotsScroll, "SlotsGrid");
                if (content != null)
                {
                    sb.AppendLine($"  Content '{content.name}': " + DescribeRect(content));
                    HorizontalLayoutGroup hlg = content.GetComponent<HorizontalLayoutGroup>();
                    if (hlg != null)
                        sb.AppendLine($"    HLG: ctrlW={hlg.childControlWidth} expandW={hlg.childForceExpandWidth} ctrlH={hlg.childControlHeight} expandH={hlg.childForceExpandHeight} align={hlg.childAlignment} spacing={hlg.spacing} pad=(L{hlg.padding.left},T{hlg.padding.top},R{hlg.padding.right},B{hlg.padding.bottom})");
                    else sb.AppendLine("    (pas de HorizontalLayoutGroup sur le Content)");
                    ContentSizeFitter gcsf = content.GetComponent<ContentSizeFitter>();
                    if (gcsf != null) sb.AppendLine($"    CSF: horiz={gcsf.horizontalFit} vert={gcsf.verticalFit}");

                    int n = 0;
                    foreach (Transform slot in content)
                    {
                        if (firstSlot == null) firstSlot = slot;
                        sb.AppendLine("    " + DescribeChild(slot));
                        n++;
                    }
                    sb.AppendLine($"    → {n} slot(s) sous le Content.");
                }
                else sb.AppendLine("  Content/SlotsGrid introuvable.");
            }

            // ── Structure interne d'UN slot (pour la refonte en tuile) ──
            if (firstSlot != null)
            {
                sb.AppendLine();
                sb.AppendLine($"[Structure interne d'un slot : '{firstSlot.name}']");
                DumpTree(firstSlot, sb, "  ", 0, 3);
            }

            sb.AppendLine();
            sb.AppendLine("=== FIN AUDIT ===");

            string result = sb.ToString();
            Debug.Log(result);
            EditorGUIUtility.systemCopyBuffer = result;
            Debug.Log("[Audit] Rapport COPIÉ dans le presse-papier → colle-le (Ctrl+V) directement dans le chat.");
        }

        // ═══════════════ HELPERS ═══════════════
        private static void DumpTree(Transform t, StringBuilder sb, string indent, int depth, int maxDepth)
        {
            sb.AppendLine(indent + DescribeNode(t));
            if (depth >= maxDepth) return;
            foreach (Transform c in t)
                DumpTree(c, sb, indent + "  ", depth + 1, maxDepth);
        }

        private static string DescribeNode(Transform t)
        {
            var sb = new StringBuilder();
            sb.Append($"{t.name} [{(t.gameObject.activeSelf ? "on" : "off")}]");
            RectTransform rt = t as RectTransform;
            if (rt != null) sb.Append($" size=({rt.rect.width:0.#}x{rt.rect.height:0.#}) anchor={V(rt.anchorMin)}-{V(rt.anchorMax)} pos={V(rt.anchoredPosition)}");
            Image img = t.GetComponent<Image>();
            if (img != null) sb.Append($" Image(sprite={(img.sprite != null ? img.sprite.name : "none")} type={img.type})");
            TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) sb.Append($" TMP(\"{Trunc(tmp.text)}\" size={tmp.fontSize} align={tmp.alignment})");
            if (t.GetComponent<HorizontalLayoutGroup>() != null) sb.Append(" [HLG]");
            if (t.GetComponent<VerticalLayoutGroup>() != null) sb.Append(" [VLG]");
            LayoutElement le = t.GetComponent<LayoutElement>();
            if (le != null) sb.Append($" LE(prefW={le.preferredWidth} prefH={le.preferredHeight} flexW={le.flexibleWidth})");
            if (t.GetComponent<ContentSizeFitter>() != null) sb.Append(" [CSF]");
            return sb.ToString();
        }

        private static string DescribeRect(Transform t)
        {
            RectTransform rt = t as RectTransform;
            if (rt == null) return "(pas de RectTransform)";
            return $"anchorMin={V(rt.anchorMin)} anchorMax={V(rt.anchorMax)} pivot={V(rt.pivot)} size=({rt.rect.width:0.#}x{rt.rect.height:0.#}) sizeDelta={V(rt.sizeDelta)}";
        }

        private static string DescribeChild(Transform t)
        {
            var sb = new StringBuilder();
            sb.Append($"{t.name} (actif={t.gameObject.activeSelf}) : ");
            RectTransform rt = t as RectTransform;
            if (rt != null) sb.Append($"size=({rt.rect.width:0.#}x{rt.rect.height:0.#}) ");
            LayoutElement le = t.GetComponent<LayoutElement>();
            if (le != null) sb.Append($"LE(minW={le.minWidth} prefW={le.preferredWidth} flexW={le.flexibleWidth} | minH={le.minHeight} prefH={le.preferredHeight} flexH={le.flexibleHeight} ignore={le.ignoreLayout}) ");
            else sb.Append("LE(aucun) ");
            if (t.GetComponent<Image>() != null) sb.Append("[Image] ");
            if (t.GetComponent<HorizontalLayoutGroup>() != null) sb.Append("[HLG] ");
            if (t.GetComponent<VerticalLayoutGroup>() != null) sb.Append("[VLG] ");
            if (t.GetComponent<ContentSizeFitter>() != null) sb.Append("[CSF] ");
            if (t.GetComponent<ScrollRect>() != null) sb.Append("[ScrollRect] ");
            return sb.ToString();
        }

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

        private static string Trunc(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length > 22 ? s.Substring(0, 22) + "…" : s);
        private static string V(Vector2 v) => $"({v.x:0.###},{v.y:0.###})";
    }
}
