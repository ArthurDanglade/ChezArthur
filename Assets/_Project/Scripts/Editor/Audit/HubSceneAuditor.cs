#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools.Audit
{
    /// <summary>
    /// Audit lecture seule de la scène ouverte (Gate 0.a — Refonte Hub).
    /// Ne modifie ni la scène, ni les assets — écrit uniquement un rapport Markdown hors Assets/.
    /// </summary>
    public static class HubSceneAuditor
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private static readonly string[] DebugNameTokens =
        {
            "test", "debug", "preview", "temp", "old", "copy"
        };

        private const int MAX_COLOR_EXAMPLE_PATHS = 3;

        // ═══════════════════════════════════════════
        // STRUCTURES DE COLLECTE
        // ═══════════════════════════════════════════
        private sealed class ColorEntry
        {
            public int Count;
            public readonly List<string> ExamplePaths = new List<string>(MAX_COLOR_EXAMPLE_PATHS);
        }

        private sealed class FontEntry
        {
            public string FontName;
            public int Count;
        }

        private sealed class InactiveSubtreeEntry
        {
            public string Path;
            public int ChildCount;
        }

        private sealed class AuditData
        {
            public string SceneName;
            public DateTime GeneratedAt;
            public int TotalCount;
            public int ActiveCount;
            public int InactiveCount;

            public readonly List<string> CanvasScalerLines = new List<string>(8);
            public readonly Dictionary<string, ColorEntry> ColorsByHex = new Dictionary<string, ColorEntry>(64);
            public readonly Dictionary<string, FontEntry> FontsByKey = new Dictionary<string, FontEntry>(16);
            public readonly List<string> SuspectRaycastPaths = new List<string>(128);
            public readonly HashSet<string> SuspectRaycastPathSet = new HashSet<string>();
            public readonly List<string> DebugElementLines = new List<string>(32);
            public readonly List<string> ButtonLines = new List<string>(64);
            public readonly List<InactiveSubtreeEntry> InactiveSubtrees = new List<InactiveSubtreeEntry>(32);
        }

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Audit/Auditer la scène ouverte")]
        public static void AuditOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[HubSceneAuditor] Aucune scène active valide.");
                return;
            }

            AuditData data = Collect(scene);
            string reportPath = WriteMarkdownReport(data);
            LogConsoleSummary(data, reportPath);
        }

        // ═══════════════════════════════════════════
        // COLLECTE (lecture seule)
        // ═══════════════════════════════════════════

        private static AuditData Collect(Scene scene)
        {
            var data = new AuditData
            {
                SceneName = string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name,
                GeneratedAt = DateTime.Now
            };

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                Walk(roots[i].transform, roots[i].name, data);

            return data;
        }

        private static void Walk(Transform t, string path, AuditData data)
        {
            GameObject go = t.gameObject;
            data.TotalCount++;

            bool isActive = go.activeInHierarchy;
            if (isActive)
                data.ActiveCount++;
            else
                data.InactiveCount++;

            // Sous-arbres inactifs : objet inactif dont le parent est actif (ou racine)
            if (!go.activeSelf)
            {
                Transform parent = t.parent;
                bool parentActive = parent == null || parent.gameObject.activeInHierarchy;
                if (parentActive)
                {
                    data.InactiveSubtrees.Add(new InactiveSubtreeEntry
                    {
                        Path = path,
                        ChildCount = t.childCount
                    });
                }
            }

            // Éléments debug (nom)
            if (NameLooksLikeDebug(go.name))
            {
                string state = go.activeSelf ? "actif" : "inactif";
                data.DebugElementLines.Add($"- `{path}` — {state} (activeSelf={go.activeSelf}, activeInHierarchy={go.activeInHierarchy})");
            }

            // CanvasScaler
            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas != null)
            {
                CanvasScaler scaler = go.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    data.CanvasScalerLines.Add(
                        $"- **{path}** — mode=`{scaler.uiScaleMode}`, " +
                        $"refResolution={scaler.referenceResolution}, " +
                        $"screenMatchMode=`{scaler.screenMatchMode}`, " +
                        $"matchWidthOrHeight={scaler.matchWidthOrHeight:0.###}");
                }
                else
                {
                    data.CanvasScalerLines.Add($"- **{path}** — Canvas sans CanvasScaler");
                }
            }

            // Graphics : couleurs + raycasts
            Graphic[] graphics = go.GetComponents<Graphic>();
            for (int g = 0; g < graphics.Length; g++)
            {
                Graphic graphic = graphics[g];
                if (graphic == null)
                    continue;

                if (IsTrackedGraphic(graphic))
                    RegisterColor(data, graphic.color, path);

                if (graphic.raycastTarget && !HasInteractiveAncestor(t)
                    && data.SuspectRaycastPathSet.Add(path))
                {
                    data.SuspectRaycastPaths.Add(path);
                }
            }

            // Polices TMP
            TextMeshProUGUI[] tmpTexts = go.GetComponents<TextMeshProUGUI>();
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                TextMeshProUGUI tmp = tmpTexts[i];
                if (tmp == null)
                    continue;

                TMP_FontAsset font = tmp.font;
                string fontKey = font != null ? font.name : "(null)";
                if (!data.FontsByKey.TryGetValue(fontKey, out FontEntry fontEntry))
                {
                    fontEntry = new FontEntry { FontName = fontKey, Count = 0 };
                    data.FontsByKey[fontKey] = fontEntry;
                }

                fontEntry.Count++;
            }

            // Boutons
            Button[] buttons = go.GetComponents<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                int listenerCount = button.onClick.GetPersistentEventCount();
                data.ButtonLines.Add($"- `{path}` — listeners persistants = {listenerCount}");
            }

            // Enfants (y compris inactifs)
            int childCount = t.childCount;
            for (int c = 0; c < childCount; c++)
            {
                Transform child = t.GetChild(c);
                Walk(child, path + "/" + child.name, data);
            }
        }

        // ═══════════════════════════════════════════
        // HELPERS LECTURE
        // ═══════════════════════════════════════════

        private static bool IsTrackedGraphic(Graphic graphic)
        {
            return graphic is Image || graphic is RawImage || graphic is TextMeshProUGUI;
        }

        private static void RegisterColor(AuditData data, Color color, string path)
        {
            string hex = ColorToHex(color);
            if (!data.ColorsByHex.TryGetValue(hex, out ColorEntry entry))
            {
                entry = new ColorEntry();
                data.ColorsByHex[hex] = entry;
            }

            entry.Count++;
            if (entry.ExamplePaths.Count < MAX_COLOR_EXAMPLE_PATHS)
                entry.ExamplePaths.Add(path);
        }

        private static string ColorToHex(Color color)
        {
            Color32 c = color;
            return $"#{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}";
        }

        private static bool HasInteractiveAncestor(Transform t)
        {
            Transform current = t;
            while (current != null)
            {
                if (current.GetComponent<Selectable>() != null)
                    return true;
                if (current.GetComponent<EventTrigger>() != null)
                    return true;
                current = current.parent;
            }

            return false;
        }

        private static bool NameLooksLikeDebug(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            string lower = name.ToLowerInvariant();
            for (int i = 0; i < DebugNameTokens.Length; i++)
            {
                if (lower.Contains(DebugNameTokens[i]))
                    return true;
            }

            return false;
        }

        // ═══════════════════════════════════════════
        // RAPPORT MARKDOWN
        // ═══════════════════════════════════════════

        private static string WriteMarkdownReport(AuditData data)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string auditsDir = Path.Combine(projectRoot, "Audits");
            if (!Directory.Exists(auditsDir))
                Directory.CreateDirectory(auditsDir);

            string safeSceneName = SanitizeFileName(data.SceneName);
            string stamp = data.GeneratedAt.ToString("yyyyMMdd_HHmm");
            string fileName = $"SceneAudit_{safeSceneName}_{stamp}.md";
            string fullPath = Path.Combine(auditsDir, fileName);

            var sb = new StringBuilder(8192);
            AppendHeader(sb, data);
            AppendCanvasScalers(sb, data);
            AppendColors(sb, data);
            AppendFonts(sb, data);
            AppendSuspectRaycasts(sb, data);
            AppendDebugElements(sb, data);
            AppendButtons(sb, data);
            AppendInactiveSubtrees(sb, data);

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
            return fullPath;
        }

        private static void AppendHeader(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("# Audit de scène — lecture seule");
            sb.AppendLine();
            sb.AppendLine($"- **Scène** : `{data.SceneName}`");
            sb.AppendLine($"- **Date** : {data.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **GameObjects** : {data.TotalCount} total ({data.ActiveCount} actifs / {data.InactiveCount} inactifs)");
            sb.AppendLine();
            sb.AppendLine("> Outil Gate 0.a — aucune modification de scène ni d'asset.");
            sb.AppendLine();
        }

        private static void AppendCanvasScalers(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## CanvasScaler");
            sb.AppendLine();
            if (data.CanvasScalerLines.Count == 0)
            {
                sb.AppendLine("_Aucun Canvas trouvé._");
            }
            else
            {
                for (int i = 0; i < data.CanvasScalerLines.Count; i++)
                    sb.AppendLine(data.CanvasScalerLines[i]);
            }

            sb.AppendLine();
        }

        private static void AppendColors(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## Couleurs (Graphic)");
            sb.AppendLine();
            sb.AppendLine("Inventaire groupé — Image / RawImage / TextMeshProUGUI (`graphic.color`).");
            sb.AppendLine();

            if (data.ColorsByHex.Count == 0)
            {
                sb.AppendLine("_Aucune couleur collectée._");
                sb.AppendLine();
                return;
            }

            var sorted = new List<KeyValuePair<string, ColorEntry>>(data.ColorsByHex);
            sorted.Sort((a, b) =>
            {
                int byCount = b.Value.Count.CompareTo(a.Value.Count);
                return byCount != 0 ? byCount : string.CompareOrdinal(a.Key, b.Key);
            });

            sb.AppendLine("| Hex | Occurrences | Exemples |");
            sb.AppendLine("|-----|-------------|----------|");
            for (int i = 0; i < sorted.Count; i++)
            {
                string hex = sorted[i].Key;
                ColorEntry entry = sorted[i].Value;
                string examples = string.Join(", ", entry.ExamplePaths);
                sb.AppendLine($"| `{hex}` | {entry.Count} | {examples} |");
            }

            sb.AppendLine();
            sb.AppendLine($"**Couleurs uniques** : {sorted.Count}");
            sb.AppendLine();
        }

        private static void AppendFonts(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## Polices TMP");
            sb.AppendLine();

            if (data.FontsByKey.Count == 0)
            {
                sb.AppendLine("_Aucun TextMeshProUGUI trouvé._");
                sb.AppendLine();
                return;
            }

            var sorted = new List<FontEntry>(data.FontsByKey.Values);
            sorted.Sort((a, b) =>
            {
                int byCount = b.Count.CompareTo(a.Count);
                return byCount != 0 ? byCount : string.CompareOrdinal(a.FontName, b.FontName);
            });

            sb.AppendLine("| Font asset | Occurrences |");
            sb.AppendLine("|------------|-------------|");
            for (int i = 0; i < sorted.Count; i++)
                sb.AppendLine($"| `{sorted[i].FontName}` | {sorted[i].Count} |");

            sb.AppendLine();
            sb.AppendLine($"**Fonts uniques** : {sorted.Count}");
            sb.AppendLine();
        }

        private static void AppendSuspectRaycasts(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## Raycasts suspects");
            sb.AppendLine();
            sb.AppendLine("`raycastTarget = true` sans `Selectable` ni `EventTrigger` sur soi ou un parent.");
            sb.AppendLine("Liste de revue (faux positifs possibles) — ne pas supprimer aveuglément.");
            sb.AppendLine();

            if (data.SuspectRaycastPaths.Count == 0)
            {
                sb.AppendLine("_Aucun raycast suspect._");
            }
            else
            {
                for (int i = 0; i < data.SuspectRaycastPaths.Count; i++)
                    sb.AppendLine($"- `{data.SuspectRaycastPaths[i]}`");
            }

            sb.AppendLine();
            sb.AppendLine($"**Total** : {data.SuspectRaycastPaths.Count}");
            sb.AppendLine();
        }

        private static void AppendDebugElements(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## Éléments debug");
            sb.AppendLine();
            sb.AppendLine("Noms contenant (insensible à la casse) : test, debug, preview, temp, old, copy.");
            sb.AppendLine();

            if (data.DebugElementLines.Count == 0)
            {
                sb.AppendLine("_Aucun élément debug nommé trouvé._");
            }
            else
            {
                for (int i = 0; i < data.DebugElementLines.Count; i++)
                    sb.AppendLine(data.DebugElementLines[i]);
            }

            sb.AppendLine();
            sb.AppendLine($"**Total** : {data.DebugElementLines.Count}");
            sb.AppendLine();
        }

        private static void AppendButtons(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## Boutons");
            sb.AppendLine();

            if (data.ButtonLines.Count == 0)
            {
                sb.AppendLine("_Aucun Button trouvé._");
            }
            else
            {
                for (int i = 0; i < data.ButtonLines.Count; i++)
                    sb.AppendLine(data.ButtonLines[i]);
            }

            sb.AppendLine();
            sb.AppendLine($"**Total** : {data.ButtonLines.Count}");
            sb.AppendLine();
        }

        private static void AppendInactiveSubtrees(StringBuilder sb, AuditData data)
        {
            sb.AppendLine("## Sous-arbres inactifs");
            sb.AppendLine();
            sb.AppendLine("Objets `activeSelf = false` dont le parent est actif (racines de zones désactivées).");
            sb.AppendLine();

            if (data.InactiveSubtrees.Count == 0)
            {
                sb.AppendLine("_Aucun sous-arbre inactif détecté._");
            }
            else
            {
                for (int i = 0; i < data.InactiveSubtrees.Count; i++)
                {
                    InactiveSubtreeEntry entry = data.InactiveSubtrees[i];
                    sb.AppendLine($"- `{entry.Path}` — {entry.ChildCount} enfant(s)");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"**Total** : {data.InactiveSubtrees.Count}");
            sb.AppendLine();
        }

        // ═══════════════════════════════════════════
        // CONSOLE
        // ═══════════════════════════════════════════

        private static void LogConsoleSummary(AuditData data, string reportPath)
        {
            Debug.Log(
                $"[HubSceneAuditor] Scène `{data.SceneName}` — " +
                $"{data.TotalCount} objets ({data.ActiveCount} actifs / {data.InactiveCount} inactifs), " +
                $"{data.ColorsByHex.Count} couleurs uniques, " +
                $"{data.FontsByKey.Count} fonts uniques, " +
                $"{data.SuspectRaycastPaths.Count} raycasts suspects, " +
                $"{data.DebugElementLines.Count} éléments debug. " +
                $"Rapport : {reportPath}");
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Untitled";

            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool bad = false;
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (c == invalid[j])
                    {
                        bad = true;
                        break;
                    }
                }

                sb.Append(bad ? '_' : c);
            }

            return sb.ToString();
        }
    }
}
#endif
