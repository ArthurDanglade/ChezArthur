#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Gate 1.4a/c — purge dette scène Hub (raycasts, fonts null, objets test).
    /// Idempotent, Undo-safe. DRY RUN = log seul ; APPLIQUER = écritures + compteur.
    /// </summary>
    public static class HubScenePurger
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string UndoLabel = "Purger Hub";
        private const string TestSliceName = "_TEST_Slice";
        private const string LegacySafeAreaName = "SafeArea";

        private static readonly string[] BlockingPopupNames =
        {
            "CharacterDetailPopup",
            "PullResultPopup",
            "RatesPopup",
            "RateUpPopup",
            "SettingsPanel"
        };

        private static readonly string[] NamedTapZones =
        {
            "TapArea",
            "TapCatcherTrain",
            "TapCatcherDoor",
            "CrankHandle"
        };

        private enum ExcludeRule
        {
            None = 0,
            E1_TargetGraphic,
            E2_SelectableOrEventTrigger,
            E3_ScrollRect,
            E4_BlockingPopupOrScrim,
            E5_NamedTapZone,
            E6_EventSystemHandler
        }

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Refonte Hub/Purger la scène (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Purger la scène (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Purger la scène Hub",
                    "Va modifier raycasts / fonts / supprimer _TEST_Slice (Undo disponible).\nContinuer ?",
                    "Appliquer",
                    "Annuler"))
                return;

            Run(apply: true);
        }

        // ═══════════════════════════════════════════
        // PIPELINE
        // ═══════════════════════════════════════════

        private static void Run(bool apply)
        {
            var log = new StringBuilder(8192);
            string mode = apply ? "APPLIQUER" : "DRY RUN";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" HubScenePurger — {mode}");
            log.AppendLine(" (compteur planifié/exécuté/échec en fin de log)");
            log.AppendLine(" E3 : Viewport/Content sous ScrollRect (incl. inactifs)");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int planned = 0;
            int executed = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[HubScenePurger] Aucune scène active chargée.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine();

            // Pré-collecte Selectables → targetGraphics (E1)
            HashSet<Graphic> protectedTargets = CollectSelectableTargetGraphics(scene);

            // ── Action 1 : Raycasts ──
            log.AppendLine("## ACTION 1 — Raycasts");
            log.AppendLine();
            int excludeCount = 0;
            int alreadyOff = 0;

            Graphic[] graphics = FindInScene<Graphic>(scene);
            var toDisable = new List<Graphic>(256);
            var exclusions = new List<(Graphic g, ExcludeRule rule)>(256);

            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic g = graphics[i];
                if (g == null) continue;

                if (!g.raycastTarget)
                {
                    alreadyOff++;
                    continue;
                }

                ExcludeRule rule = EvaluateExclusion(g, protectedTargets);
                if (rule != ExcludeRule.None)
                {
                    exclusions.Add((g, rule));
                    excludeCount++;
                }
                else
                {
                    toDisable.Add(g);
                }
            }

            log.AppendLine("### À désactiver (raycastTarget → false)");
            if (toDisable.Count == 0)
            {
                log.AppendLine("_Aucun._");
            }
            else
            {
                for (int i = 0; i < toDisable.Count; i++)
                {
                    Graphic g = toDisable[i];
                    string path = GetPath(g.transform);
                    planned++;

                    if (!apply)
                    {
                        log.AppendLine($"- [DRY] `{path}`");
                        executed++;
                        continue;
                    }

                    Undo.RecordObject(g, UndoLabel);
                    g.raycastTarget = false;
                    EditorUtility.SetDirty(g);

                    if (!g.raycastTarget)
                    {
                        executed++;
                        log.AppendLine($"- `{path}` → raycastTarget=false ✓");
                    }
                    else
                    {
                        failed++;
                        log.AppendLine($"- `{path}` → ÉCHEC (raycastTarget encore true) ✗");
                        Debug.LogError(
                            $"[HubScenePurger] ACTION 1 ÉCHEC — `{path}` raycastTarget non coupé.");
                    }
                }
            }

            log.AppendLine();
            log.AppendLine("### Exclusions (protégés)");
            if (exclusions.Count == 0)
            {
                log.AppendLine("_Aucune._");
            }
            else
            {
                for (int i = 0; i < exclusions.Count; i++)
                {
                    (Graphic g, ExcludeRule rule) = exclusions[i];
                    log.AppendLine($"- `{GetPath(g.transform)}` — {FormatRule(rule)}");
                }
            }

            log.AppendLine();
            log.AppendLine(
                $"Résumé raycasts : {toDisable.Count} à couper, {excludeCount} exclus, " +
                $"{alreadyOff} déjà false.");
            log.AppendLine();

            // ── Action 2 : Fonts null (roots + inactifs) ──
            log.AppendLine("## ACTION 2 — Fonts null (TMP)");
            log.AppendLine();

            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            List<TextMeshProUGUI> nullFontTmps = CollectNullFontTmpFromSceneRoots(scene);

            if (defaultFont == null)
            {
                log.AppendLine("⚠ TMP_Settings.defaultFontAsset est null — impossible d'assigner.");
                if (nullFontTmps.Count > 0)
                {
                    for (int i = 0; i < nullFontTmps.Count; i++)
                    {
                        planned++;
                        failed++;
                        log.AppendLine(
                            $"- `{GetPath(nullFontTmps[i].transform)}` → ÉCHEC (pas de font défaut) ✗");
                    }
                }
            }
            else
            {
                log.AppendLine($"Font par défaut : `{defaultFont.name}`");
                log.AppendLine(
                    "Scan : roots scène + GetComponentsInChildren<TextMeshProUGUI>(true) " +
                    $"→ {nullFontTmps.Count} font null.");
                log.AppendLine();

                if (nullFontTmps.Count == 0)
                {
                    log.AppendLine("_Aucun TextMeshProUGUI avec font null._");
                }
                else
                {
                    for (int i = 0; i < nullFontTmps.Count; i++)
                    {
                        TextMeshProUGUI tmp = nullFontTmps[i];
                        string path = GetPath(tmp.transform);
                        planned++;

                        if (!apply)
                        {
                            log.AppendLine($"- [DRY] `{path}` → assigner `{defaultFont.name}`");
                            executed++;
                            continue;
                        }

                        Undo.RecordObject(tmp, UndoLabel);
                        tmp.font = defaultFont;
                        EditorUtility.SetDirty(tmp);

                        if (tmp.font != null)
                        {
                            executed++;
                            log.AppendLine($"- `{path}` → font=`{tmp.font.name}` ✓");
                        }
                        else
                        {
                            failed++;
                            log.AppendLine($"- `{path}` → ÉCHEC (font encore null) ✗");
                            Debug.LogError(
                                $"[HubScenePurger] ACTION 2 ÉCHEC — `{path}` font null après write.");
                        }
                    }
                }
            }

            log.AppendLine();
            log.AppendLine($"Résumé fonts : {nullFontTmps.Count} cas.");
            log.AppendLine();

            // ── Action 3 : Éléments test ──
            log.AppendLine("## ACTION 3 — Éléments de test sérialisés");
            log.AppendLine();
            PurgeTestSlice(scene, apply, log, ref planned, ref executed, ref failed);
            PurgeLegacySafeArea(scene, apply, log, ref planned, ref executed, ref failed);
            log.AppendLine();
            log.AppendLine(
                "Non touchés (volontaire) : TopGoldAccent, GoldLine, PanelGoldLine, " +
                "LightOverlay / fonds pages.");

            if (apply)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                log.AppendLine();
                log.AppendLine("Scène marquée dirty — pense à sauvegarder.");
            }

            // ── Compteur bruyant ──
            log.AppendLine();
            log.AppendLine("## COMPTEUR D'ACTIONS");
            log.AppendLine($"- Planifiées : {planned}");
            log.AppendLine($"- Exécutées (ou déjà conformes) : {executed}");
            log.AppendLine($"- Échecs : {failed}");
            log.AppendLine($"Fin {mode}.");

            Debug.Log(log.ToString());

            if (apply && failed > 0)
            {
                Debug.LogError(
                    $"[HubScenePurger] APPLIQUER INCOMPLET — {failed} échec(s), " +
                    $"{executed}/{planned} actions OK. Voir log ci-dessus.");
            }
            else if (apply && executed < planned)
            {
                Debug.LogError(
                    $"[HubScenePurger] APPLIQUER ÉCART — exécuté {executed} < planifié {planned}. " +
                    "Writes manquants possibles.");
            }
            else if (apply)
            {
                Debug.Log(
                    $"[HubScenePurger] APPLIQUER OK — {executed}/{planned} actions, 0 échec.");
            }
        }

        // ═══════════════════════════════════════════
        // ACTION 1 — EXCLUSIONS
        // ═══════════════════════════════════════════

        private static HashSet<Graphic> CollectSelectableTargetGraphics(Scene scene)
        {
            var set = new HashSet<Graphic>();
            Selectable[] selectables = FindInScene<Selectable>(scene);
            for (int i = 0; i < selectables.Length; i++)
            {
                Selectable s = selectables[i];
                if (s == null || s.targetGraphic == null)
                    continue;
                set.Add(s.targetGraphic);
            }

            return set;
        }

        private static ExcludeRule EvaluateExclusion(Graphic g, HashSet<Graphic> protectedTargets)
        {
            if (g == null)
                return ExcludeRule.None;

            // E5 — zones de tap nommées (sécurité ; souvent déjà E1/E2)
            if (NameMatches(g.gameObject, NamedTapZones) || HasAncestorNamed(g.transform, NamedTapZones))
                return ExcludeRule.E5_NamedTapZone;

            // E1 — targetGraphic d'un Selectable
            if (protectedTargets.Contains(g))
                return ExcludeRule.E1_TargetGraphic;

            // E2 — Selectable ou EventTrigger (soi ou ancêtre)
            if (HasSelectableOrEventTrigger(g.transform))
                return ExcludeRule.E2_SelectableOrEventTrigger;

            // E3 — ScrollRect / viewport / content
            if (IsScrollRectRelated(g))
                return ExcludeRule.E3_ScrollRect;

            // E4 — popup/scrim bloquant
            if (IsBlockingPopupOrScrim(g.transform))
                return ExcludeRule.E4_BlockingPopupOrScrim;

            // E6 — MonoBehaviour custom IEventSystemHandler (IDragHandler, etc.)
            // GetComponent<IEventSystemHandler>() rate parfois les handlers custom.
            if (HasEventSystemHandlerMonoBehaviour(g.transform))
                return ExcludeRule.E6_EventSystemHandler;

            return ExcludeRule.None;
        }

        private static bool HasSelectableOrEventTrigger(Transform t)
        {
            Transform cursor = t;
            while (cursor != null)
            {
                if (cursor.GetComponent<Selectable>() != null)
                    return true;
                if (cursor.GetComponent<EventTrigger>() != null)
                    return true;
                cursor = cursor.parent;
            }

            return false;
        }

        /// <summary>
        /// MonoBehaviour sur soi ou un parent assignable à IEventSystemHandler
        /// (IPointerDownHandler, IDragHandler, IBeginDragHandler, …).
        /// </summary>
        private static bool HasEventSystemHandlerMonoBehaviour(Transform t)
        {
            Transform cursor = t;
            while (cursor != null)
            {
                MonoBehaviour[] behaviours = cursor.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour mb = behaviours[i];
                    if (mb == null)
                        continue;
                    if (typeof(IEventSystemHandler).IsAssignableFrom(mb.GetType()))
                        return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private static bool IsScrollRectRelated(Graphic g)
        {
            if (g == null)
                return false;

            // includeInactive : pages / SummaryScene souvent désactivés dans le Hub
            if (g.GetComponent<ScrollRect>() != null)
                return true;

            ScrollRect parentScroll = g.GetComponentInParent<ScrollRect>(true);
            if (parentScroll == null)
            {
                // Fallback manuel si API/includeInactive défaille
                Transform climb = g.transform.parent;
                while (climb != null)
                {
                    parentScroll = climb.GetComponent<ScrollRect>();
                    if (parentScroll != null)
                        break;
                    climb = climb.parent;
                }
            }

            if (parentScroll == null)
                return false;

            Transform gTx = g.transform;

            if (parentScroll.viewport != null && gTx == parentScroll.viewport)
                return true;
            if (parentScroll.content != null && gTx == parentScroll.content)
                return true;

            // Enfant (in)direct nommé Viewport/Content sous le ScrollRect
            string name = g.gameObject.name.Trim();
            if (name == "Viewport" || name == "Content")
            {
                if (gTx == parentScroll.transform)
                    return true;

                Transform p = gTx.parent;
                while (p != null)
                {
                    if (p == parentScroll.transform)
                        return true;
                    // Stop si on croise un autre ScrollRect en remontant
                    if (p.GetComponent<ScrollRect>() != null)
                        break;
                    p = p.parent;
                }
            }

            return false;
        }

        private static bool IsBlockingPopupOrScrim(Transform t)
        {
            Transform cursor = t;
            while (cursor != null)
            {
                if (NameMatches(cursor.gameObject, BlockingPopupNames))
                    return true;

                CanvasGroup cg = cursor.GetComponent<CanvasGroup>();
                if (cg != null && cg.blocksRaycasts)
                    return true;

                cursor = cursor.parent;
            }

            return false;
        }

        private static string FormatRule(ExcludeRule rule)
        {
            switch (rule)
            {
                case ExcludeRule.E1_TargetGraphic: return "E1 (targetGraphic Selectable)";
                case ExcludeRule.E2_SelectableOrEventTrigger:
                    return "E2 (Selectable / EventTrigger)";
                case ExcludeRule.E3_ScrollRect: return "E3 (ScrollRect viewport/content)";
                case ExcludeRule.E4_BlockingPopupOrScrim: return "E4 (popup / CanvasGroup.blocksRaycasts)";
                case ExcludeRule.E5_NamedTapZone: return "E5 (TapArea / TapCatcher*)";
                case ExcludeRule.E6_EventSystemHandler:
                    return "E6 (IEventSystemHandler MonoBehaviour)";
                default: return "E?";
            }
        }

        // ═══════════════════════════════════════════
        // ACTION 2 — FONTS (roots + inactifs)
        // ═══════════════════════════════════════════

        /// <summary>
        /// Scan explicite : racines de scène + GetComponentsInChildren(true) — inactifs inclus.
        /// </summary>
        private static List<TextMeshProUGUI> CollectNullFontTmpFromSceneRoots(Scene scene)
        {
            var list = new List<TextMeshProUGUI>(64);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                if (roots[r] == null)
                    continue;

                TextMeshProUGUI[] tmps =
                    roots[r].GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < tmps.Length; i++)
                {
                    TextMeshProUGUI tmp = tmps[i];
                    if (tmp == null || tmp.font != null)
                        continue;
                    list.Add(tmp);
                }
            }

            return list;
        }

        // ═══════════════════════════════════════════
        // ACTION 3 — SUPPRESSIONS
        // ═══════════════════════════════════════════

        private static void PurgeTestSlice(
            Scene scene,
            bool apply,
            StringBuilder log,
            ref int planned,
            ref int executed,
            ref int failed)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            var found = new List<GameObject>(4);

            for (int r = 0; r < roots.Length; r++)
                CollectByName(roots[r].transform, TestSliceName, found);

            if (found.Count == 0)
            {
                log.AppendLine($"- `{TestSliceName}` : introuvable (déjà purgé ou absent)");
                return;
            }

            for (int i = 0; i < found.Count; i++)
            {
                GameObject go = found[i];
                string path = GetPath(go.transform);
                planned++;

                if (!apply)
                {
                    log.AppendLine($"- [DRY] SUPPRIMER `{path}`");
                    executed++;
                    continue;
                }

                Undo.DestroyObjectImmediate(go);
                executed++;
                log.AppendLine($"- SUPPRIMER `{path}` ✓");
            }
        }

        private static void PurgeLegacySafeArea(
            Scene scene,
            bool apply,
            StringBuilder log,
            ref int planned,
            ref int executed,
            ref int failed)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            var found = new List<GameObject>(2);

            for (int r = 0; r < roots.Length; r++)
                CollectByName(roots[r].transform, LegacySafeAreaName, found);

            if (found.Count == 0)
            {
                log.AppendLine($"- `{LegacySafeAreaName}` : introuvable (renommé SafeRoot ou absent) — OK");
                return;
            }

            for (int i = 0; i < found.Count; i++)
            {
                GameObject go = found[i];
                string path = GetPath(go.transform);

                if (!IsEmptyOrOnlyEmptyChildren(go.transform))
                {
                    log.AppendLine(
                        $"- NON TRAITÉ — décision manuelle : `{path}` " +
                        $"({go.transform.childCount} enfant(s) non vides)");
                    continue;
                }

                planned++;

                if (!apply)
                {
                    log.AppendLine($"- [DRY] SUPPRIMER `{path}` (vide / enfants vides)");
                    executed++;
                    continue;
                }

                Undo.DestroyObjectImmediate(go);
                executed++;
                log.AppendLine($"- SUPPRIMER `{path}` (vide / enfants vides) ✓");
            }
        }

        /// <summary>
        /// Vrai si aucun enfant, ou uniquement des GO sans enfants et sans composants
        /// « utiles » au-delà de Transform (objets coquille).
        /// </summary>
        private static bool IsEmptyOrOnlyEmptyChildren(Transform root)
        {
            if (root.childCount == 0)
                return true;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.childCount > 0)
                    return false;

                // Enfant avec plus que Transform (+ RectTransform éventuel) = contenu
                Component[] comps = child.GetComponents<Component>();
                for (int c = 0; c < comps.Length; c++)
                {
                    if (comps[c] == null) continue;
                    if (comps[c] is Transform) continue;
                    return false;
                }
            }

            return true;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static T[] FindInScene<T>(Scene scene) where T : Object
        {
            T[] all = Object.FindObjectsOfType<T>(true);
            var list = new List<T>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                T obj = all[i];
                if (obj == null) continue;

                GameObject go = null;
                if (obj is Component comp)
                    go = comp.gameObject;
                else if (obj is GameObject g)
                    go = g;

                if (go == null) continue;
                if (!go.scene.IsValid() || go.scene != scene) continue;
                // Hors assets / prefab stage
                if (EditorUtility.IsPersistent(go)) continue;

                list.Add(obj);
            }

            return list.ToArray();
        }

        private static void CollectByName(Transform root, string name, List<GameObject> into)
        {
            if (root.name == name)
                into.Add(root.gameObject);

            for (int i = 0; i < root.childCount; i++)
                CollectByName(root.GetChild(i), name, into);
        }

        private static bool NameMatches(GameObject go, string[] names)
        {
            if (go == null) return false;
            for (int i = 0; i < names.Length; i++)
            {
                if (go.name == names[i])
                    return true;
            }

            return false;
        }

        private static bool HasAncestorNamed(Transform t, string[] names)
        {
            Transform c = t;
            while (c != null)
            {
                if (NameMatches(c.gameObject, names))
                    return true;
                c = c.parent;
            }

            return false;
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "(null)";

            var stack = new List<string>(8);
            Transform c = t;
            while (c != null)
            {
                stack.Add(c.name);
                c = c.parent;
            }

            var sb = new StringBuilder(64);
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                sb.Append(stack[i]);
                if (i > 0) sb.Append('/');
            }

            return sb.ToString();
        }
    }
}
#endif
