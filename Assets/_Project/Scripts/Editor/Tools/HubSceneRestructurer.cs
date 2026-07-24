#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using ChezArthur.Gacha;
using ChezArthur.Hub;
using ChezArthur.Hub.Pages;
using ChezArthur.Hub.Pages.Invocation;
using ChezArthur.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Restructuration Gate 1.2 de la scène Hub : BackgroundLayer / SafeRoot / OverlayLayer.
    /// Idempotent, Undo-safe. DRY RUN = log seul (zéro modification) ; APPLIQUER = exécution.
    /// </summary>
    public static class HubSceneRestructurer
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BackgroundLayerName = "BackgroundLayer";
        private const string SafeRootName = "SafeRoot";
        private const string LegacySafeAreaName = "SafeArea";
        private const string PageContainerName = "PageContainer";
        private const string OverlayLayerName = "OverlayLayer";
        private const string NavigationBarName = "NavigationBar";
        private const string SceneBackgroundName = "Background";
        private const string UndoLabel = "Restructurer Hub";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Refonte Hub/Restructurer la scène (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Restructurer la scène (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Restructurer la scène Hub",
                    "Va modifier la hiérarchie de la scène ouverte (Undo disponible).\nContinuer ?",
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
            var log = new StringBuilder(4096);
            string mode = apply ? "APPLIQUER" : "DRY RUN";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" HubSceneRestructurer — {mode}");
            log.AppendLine(" (compteur planifié/exécuté/échec en fin de log)");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int planned = 0;
            int executed = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[HubSceneRestructurer] Aucune scène active chargée.");
                return;
            }

            HubManager hubManager = Object.FindObjectOfType<HubManager>(true);
            if (hubManager == null)
            {
                Debug.LogError("[HubSceneRestructurer] HubManager introuvable — ouvrir Hub.unity.");
                return;
            }

            List<GameObject> pages = ReadHubPages(hubManager);
            if (pages.Count == 0)
            {
                Debug.LogError("[HubSceneRestructurer] HubManager.pages est vide.");
                return;
            }

            log.AppendLine($"HubManager.pages : {pages.Count} entrée(s)");
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] == null)
                    log.AppendLine($"  [{i}] null ⚠");
                else
                    log.AppendLine($"  [{i}] `{GetPath(pages[i].transform)}`");
            }

            Canvas pagesCanvas = null;
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] == null) continue;
                pagesCanvas = pages[i].GetComponentInParent<Canvas>();
                if (pagesCanvas != null) break;
            }

            if (pagesCanvas == null)
            {
                Debug.LogError("[HubSceneRestructurer] Canvas parent des pages introuvable.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine($"Canvas pages : `{GetPath(pagesCanvas.transform)}`");
            WarnOtherRootCanvases(pagesCanvas, log);
            log.AppendLine();

            Transform canvasTx = pagesCanvas.transform;

            // ── Conteneurs ──
            log.AppendLine("## Conteneurs");
            log.AppendLine();

            RectTransform backgroundLayer = EnsureStretchContainer(
                canvasTx, BackgroundLayerName, apply, log, out bool bgCreated);

            RectTransform safeRoot = EnsureSafeRoot(canvasTx, apply, log, out bool safeCreated);

            RectTransform overlayLayer = EnsureStretchContainer(
                canvasTx, OverlayLayerName, apply, log, out bool overlayCreated);

            RectTransform pageContainer = null;
            bool pageContainerCreated = false;
            if (safeRoot != null)
            {
                string pageContainerParentLog = (!apply && safeRoot.name == LegacySafeAreaName)
                    ? $"{GetPath(canvasTx)}/{SafeRootName}"
                    : null;
                pageContainer = EnsureStretchContainer(
                    safeRoot, PageContainerName, apply, log, out pageContainerCreated,
                    dryParentPathOverride: pageContainerParentLog);
                EnsureSafeAreaFitter(safeRoot, apply, log);
            }
            else if (!apply)
            {
                log.AppendLine($"- [DRY] CRÉER `{PageContainerName}` sous `{GetPath(canvasTx)}/{SafeRootName}` (full-stretch)");
            }
            else
            {
                Debug.LogError("[HubSceneRestructurer] SafeRoot null après Ensure — abort reparentages.");
                log.AppendLine("- ✗ SafeRoot null — reparentages annulés");
            }

            // ── Collecte cibles ──
            GameObject sceneBackground = FindDirectChild(canvasTx, SceneBackgroundName);
            if (sceneBackground == null && backgroundLayer != null)
                sceneBackground = FindDirectChild(backgroundLayer, SceneBackgroundName);

            InfoBarUI infoBar = Object.FindObjectOfType<InfoBarUI>(true);
            GameObject navigationBar = FindInHierarchyByName(canvasTx, NavigationBarName);

            CharacterDetailPopup detailPopup = Object.FindObjectOfType<CharacterDetailPopup>(true);
            RatesPopupUI ratesPopup = Object.FindObjectOfType<RatesPopupUI>(true);
            RateUpPopupUI rateUpPopup = Object.FindObjectOfType<RateUpPopupUI>(true);
            PullResultPopupUI pullResultPopup = Object.FindObjectOfType<PullResultPopupUI>(true);
            SettingsPanelUI settingsPanel = Object.FindObjectOfType<SettingsPanelUI>(true);
            GachaAnimationController gacha = Object.FindObjectOfType<GachaAnimationController>(true);

            var overlayTargets = new List<Transform>(8);
            TryCollectOverlayRoot(detailPopup != null ? detailPopup.transform : null, canvasTx, overlayTargets);
            TryCollectOverlayRoot(ratesPopup != null ? ratesPopup.transform : null, canvasTx, overlayTargets);
            TryCollectOverlayRoot(rateUpPopup != null ? rateUpPopup.transform : null, canvasTx, overlayTargets);
            TryCollectOverlayRoot(pullResultPopup != null ? pullResultPopup.transform : null, canvasTx, overlayTargets);
            TryCollectOverlayRoot(settingsPanel != null ? settingsPanel.transform : null, canvasTx, overlayTargets);
            TryCollectOverlayRoot(gacha != null ? gacha.transform : null, canvasTx, overlayTargets);
            SortBySiblingIndex(overlayTargets);

            string canvasPath = GetPath(canvasTx);
            string safeParentPath = $"{canvasPath}/{SafeRootName}";
            string pageParentPath = $"{safeParentPath}/{PageContainerName}";
            string bgParentPath = $"{canvasPath}/{BackgroundLayerName}";
            string overlayParentPath = $"{canvasPath}/{OverlayLayerName}";

            // ── Reparentages ──
            log.AppendLine();
            log.AppendLine("## Reparentages");
            log.AppendLine();

            if (sceneBackground != null)
            {
                Reparent(sceneBackground.transform, backgroundLayer, bgParentPath, apply, log,
                    "Fond de scène", ref planned, ref executed, ref failed);
            }
            else
            {
                log.AppendLine("- Fond de scène `Background` : introuvable (rien à déplacer)");
            }

            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] == null)
                {
                    log.AppendLine($"- Page[{i}] : null — ignorée");
                    failed++;
                    Debug.LogError($"[HubSceneRestructurer] HubManager.pages[{i}] est null.");
                    continue;
                }

                Reparent(pages[i].transform, pageContainer, pageParentPath, apply, log,
                    $"Page[{i}] `{pages[i].name}`", ref planned, ref executed, ref failed);
            }

            if (infoBar != null)
            {
                Reparent(infoBar.transform, safeRoot, safeParentPath, apply, log,
                    "Header (InfoBar)", ref planned, ref executed, ref failed);
            }
            else
            {
                log.AppendLine("- InfoBar : introuvable ⚠");
                failed++;
                Debug.LogError("[HubSceneRestructurer] InfoBar introuvable.");
            }

            if (navigationBar != null)
            {
                Reparent(navigationBar.transform, safeRoot, safeParentPath, apply, log,
                    "NavigationBar", ref planned, ref executed, ref failed);
            }
            else
            {
                log.AppendLine("- NavigationBar : introuvable ⚠");
                failed++;
                Debug.LogError("[HubSceneRestructurer] NavigationBar introuvable.");
            }

            for (int i = 0; i < overlayTargets.Count; i++)
            {
                Reparent(overlayTargets[i], overlayLayer, overlayParentPath, apply, log,
                    $"Overlay `{overlayTargets[i].name}`", ref planned, ref executed, ref failed);
            }

            if (settingsPanel == null)
                log.AppendLine("- SettingsPanel : absent de la scène (OK)");

            // Purge SafeArea legacy vide après migration
            TryPurgeEmptyLegacySafeArea(canvasTx, apply, log, ref planned, ref executed, ref failed);

            // ── Ordre ──
            log.AppendLine();
            log.AppendLine("## Ordre sous SafeRoot (PageContainer → InfoBar → NavigationBar)");
            ForceSafeRootOrder(safeRoot, pageContainer, infoBar, navigationBar, apply, log);

            log.AppendLine();
            log.AppendLine("## Ordre sous Canvas (BackgroundLayer → SafeRoot → OverlayLayer)");
            ForceCanvasLayerOrder(canvasTx, backgroundLayer, safeRoot, overlayLayer, apply, log);

            log.AppendLine();
            log.AppendLine("## CanvasScaler");
            ApplyCanvasScalerMatch(pagesCanvas, apply, log);

            // ── Non traités ──
            log.AppendLine();
            log.AppendLine("## NON TRAITÉ — décision manuelle");
            HashSet<Transform> claimed = BuildClaimedSet(
                backgroundLayer, safeRoot, overlayLayer, pageContainer,
                sceneBackground, pages, infoBar, navigationBar, overlayTargets);

            Transform legacySafe = FindDirectChildTransform(canvasTx, LegacySafeAreaName);
            if (legacySafe != null) claimed.Add(legacySafe);

            int untreated = 0;
            for (int i = 0; i < canvasTx.childCount; i++)
            {
                Transform child = canvasTx.GetChild(i);
                if (claimed.Contains(child))
                    continue;

                untreated++;
                log.AppendLine($"- `{GetPath(child)}` (activeSelf={child.gameObject.activeSelf})");
            }

            if (untreated == 0)
                log.AppendLine("_Aucun objet racine Canvas non classé._");

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
            log.AppendLine(
                $"Fin {mode}. Créations {(apply ? "effectuées" : "prévues")} : " +
                $"BG={(bgCreated ? "oui" : "non")}, " +
                $"Safe={(safeCreated ? "oui" : "non")}, Overlay={(overlayCreated ? "oui" : "non")}, " +
                $"PageContainer={(pageContainerCreated ? "oui" : "non")}.");

            Debug.Log(log.ToString());

            if (apply && failed > 0)
            {
                Debug.LogError(
                    $"[HubSceneRestructurer] APPLIQUER INCOMPLET — {failed} échec(s), " +
                    $"{executed}/{planned} actions OK. Voir log ci-dessus.");
            }
            else if (apply && executed < planned)
            {
                Debug.LogError(
                    $"[HubSceneRestructurer] APPLIQUER ÉCART — exécuté {executed} < planifié {planned}. " +
                    "Reparentages manquants possibles.");
            }
            else if (apply)
            {
                Debug.Log(
                    $"[HubSceneRestructurer] APPLIQUER OK — {executed}/{planned} actions, 0 échec.");
            }
        }

        // ═══════════════════════════════════════════
        // CONTENEURS
        // ═══════════════════════════════════════════

        private static RectTransform EnsureSafeRoot(
            Transform canvasTx,
            bool apply,
            StringBuilder log,
            out bool created)
        {
            created = false;

            Transform safeRootTx = FindDirectChildTransform(canvasTx, SafeRootName);
            Transform legacyTx = FindDirectChildTransform(canvasTx, LegacySafeAreaName);

            // État dual (bug 1.2) : SafeRoot vide + SafeArea peuplé → fusionner
            if (safeRootTx != null && legacyTx != null && safeRootTx != legacyTx)
            {
                log.AppendLine(
                    $"- ⚠ État dual détecté : `{SafeRootName}` + `{LegacySafeAreaName}`");

                if (!apply)
                {
                    log.AppendLine(
                        $"- [DRY] Migrer enfants de `{LegacySafeAreaName}` → `{SafeRootName}`, " +
                        $"puis supprimer `{LegacySafeAreaName}` si vide");
                    return safeRootTx as RectTransform;
                }

                // Déplacer tous les enfants de SafeArea vers SafeRoot
                while (legacyTx.childCount > 0)
                {
                    Transform child = legacyTx.GetChild(0);
                    Undo.SetTransformParent(child, safeRootTx, UndoLabel);
                    child.SetParent(safeRootTx, false);
                    log.AppendLine($"- FUSION : `{child.name}` → `{SafeRootName}`");
                }

                if (legacyTx.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(legacyTx.gameObject);
                    log.AppendLine($"- SUPPRIMER `{LegacySafeAreaName}` (vidé après fusion)");
                }

                RectTransform merged = safeRootTx as RectTransform;
                if (merged != null)
                {
                    Undo.RecordObject(merged, UndoLabel);
                    StretchFull(merged);
                }

                return merged;
            }

            if (safeRootTx != null)
            {
                RectTransform rt = safeRootTx as RectTransform;
                if (rt == null)
                {
                    log.AppendLine($"- `{SafeRootName}` : existe sans RectTransform");
                    return null;
                }

                if (apply)
                {
                    Undo.RecordObject(rt, UndoLabel);
                    StretchFull(rt);
                }

                log.AppendLine($"- `{SafeRootName}` : déjà présent (`{GetPath(safeRootTx)}`)");
                return rt;
            }

            if (legacyTx != null)
            {
                RectTransform legacyRt = legacyTx as RectTransform;
                if (apply)
                {
                    Undo.RecordObject(legacyTx.gameObject, UndoLabel);
                    legacyTx.gameObject.name = SafeRootName;
                    if (legacyRt != null)
                    {
                        Undo.RecordObject(legacyRt, UndoLabel);
                        StretchFull(legacyRt);
                    }

                    log.AppendLine($"- RENOMMER `{LegacySafeAreaName}` → `{SafeRootName}` (`{GetPath(legacyTx)}`)");
                }
                else
                {
                    log.AppendLine($"- [DRY] RENOMMER `{LegacySafeAreaName}` → `{SafeRootName}`");
                }

                return legacyRt;
            }

            return EnsureStretchContainer(canvasTx, SafeRootName, apply, log, out created);
        }

        private static RectTransform EnsureStretchContainer(
            Transform parent,
            string name,
            bool apply,
            StringBuilder log,
            out bool created,
            string dryParentPathOverride = null)
        {
            created = false;
            if (parent == null)
            {
                log.AppendLine($"- `{name}` : parent null");
                return null;
            }

            Transform existing = FindDirectChildTransform(parent, name);
            if (existing != null)
            {
                RectTransform existingRt = existing as RectTransform;
                if (existingRt == null)
                {
                    log.AppendLine($"- `{name}` : existe sans RectTransform");
                    return null;
                }

                if (apply)
                {
                    Undo.RecordObject(existingRt, UndoLabel);
                    StretchFull(existingRt);
                }

                log.AppendLine($"- `{name}` : déjà présent (`{GetPath(existing)}`)");
                return existingRt;
            }

            string parentPathForLog = !string.IsNullOrEmpty(dryParentPathOverride)
                ? dryParentPathOverride
                : GetPath(parent);

            if (!apply)
            {
                log.AppendLine($"- [DRY] CRÉER `{name}` sous `{parentPathForLog}` (full-stretch)");
                created = true; // prévu
                return null;
            }

            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            RectTransform rt = (RectTransform)go.transform;
            Undo.SetTransformParent(rt, parent, UndoLabel);
            rt.SetParent(parent, false);
            StretchFull(rt);
            created = true;
            log.AppendLine($"- CRÉER `{name}` sous `{GetPath(parent)}`");
            return rt;
        }

        private static void EnsureSafeAreaFitter(RectTransform safeRoot, bool apply, StringBuilder log)
        {
            if (safeRoot == null)
                return;

            string displayName = safeRoot.name == LegacySafeAreaName ? SafeRootName : safeRoot.name;

            if (safeRoot.GetComponent<SafeAreaFitter>() != null)
            {
                log.AppendLine($"- SafeAreaFitter : déjà présent sur `{displayName}`");
                return;
            }

            if (apply)
            {
                Undo.AddComponent<SafeAreaFitter>(safeRoot.gameObject);
                log.AppendLine($"- SafeAreaFitter : ajouté sur `{displayName}`");
            }
            else
            {
                log.AppendLine($"- [DRY] SafeAreaFitter : à ajouter sur `{displayName}`");
            }
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        // ═══════════════════════════════════════════
        // REPARENT / ORDRE / SCALER
        // ═══════════════════════════════════════════

        private static void Reparent(
            Transform target,
            Transform newParentOrNull,
            string newParentPathForDry,
            bool apply,
            StringBuilder log,
            string label,
            ref int planned,
            ref int executed,
            ref int failed)
        {
            if (target == null)
            {
                failed++;
                log.AppendLine($"- {label} : ÉCHEC — target null");
                Debug.LogError($"[HubSceneRestructurer] {label} : target null");
                return;
            }

            planned++;
            string before = GetPath(target);

            if (newParentOrNull != null && target.parent == newParentOrNull)
            {
                string parentDisplay = newParentOrNull.name == LegacySafeAreaName
                    ? SafeRootName
                    : newParentOrNull.name;
                log.AppendLine($"- {label} : déjà sous `{parentDisplay}` (`{before}`)");
                executed++;
                return;
            }

            string after = $"{newParentPathForDry}/{target.name}";

            if (!apply)
            {
                log.AppendLine($"- [DRY] {label} : `{before}` → `{after}`");
                executed++; // prévu compté comme « ok plan »
                return;
            }

            if (newParentOrNull == null)
            {
                failed++;
                log.AppendLine($"- {label} : ÉCHEC — parent cible null (`{after}`)");
                Debug.LogError(
                    $"[HubSceneRestructurer] {label} : parent null — `{before}` non déplacé.");
                return;
            }

            // Undo + SetParent worldPositionStays=false (UI)
            Undo.SetTransformParent(target, newParentOrNull, UndoLabel);
            target.SetParent(newParentOrNull, false);

            if (target.parent != newParentOrNull)
            {
                failed++;
                log.AppendLine($"- {label} : ÉCHEC — parent après move = `{GetPath(target.parent)}` (attendu `{newParentPathForDry}`)");
                Debug.LogError(
                    $"[HubSceneRestructurer] {label} : SetParent a échoué. " +
                    $"Avant `{before}`, après `{GetPath(target)}`.");
                return;
            }

            executed++;
            log.AppendLine($"- {label} : `{before}` → `{GetPath(target)}` ✓");
        }

        private static void TryPurgeEmptyLegacySafeArea(
            Transform canvasTx,
            bool apply,
            StringBuilder log,
            ref int planned,
            ref int executed,
            ref int failed)
        {
            Transform legacy = FindDirectChildTransform(canvasTx, LegacySafeAreaName);
            if (legacy == null)
                return;

            planned++;
            if (legacy.childCount > 0)
            {
                log.AppendLine(
                    $"- `{LegacySafeAreaName}` : encore {legacy.childCount} enfant(s) — " +
                    "NON TRAITÉ (ne pas supprimer)");
                // Pas un échec bloquant si les enfants sont volontairement restés
                executed++;
                return;
            }

            if (!apply)
            {
                log.AppendLine($"- [DRY] SUPPRIMER `{LegacySafeAreaName}` (vide)");
                executed++;
                return;
            }

            Undo.DestroyObjectImmediate(legacy.gameObject);
            executed++;
            log.AppendLine($"- SUPPRIMER `{LegacySafeAreaName}` (vide) ✓");
        }

        private static void ForceSafeRootOrder(
            RectTransform safeRoot,
            RectTransform pageContainer,
            InfoBarUI infoBar,
            GameObject navigationBar,
            bool apply,
            StringBuilder log)
        {
            // Ordre cible toujours loggé en entier (y compris PageContainer pas encore créé en DRY).
            string infoBarName = infoBar != null ? infoBar.name : "InfoBar";
            string navName = navigationBar != null ? navigationBar.name : NavigationBarName;

            if (!apply)
            {
                log.AppendLine($"- [DRY] sibling[0] = `{PageContainerName}`");
                log.AppendLine(infoBar != null
                    ? $"- [DRY] sibling[1] = `{infoBarName}`"
                    : $"- [DRY] sibling[1] = `{infoBarName}` (introuvable)");
                log.AppendLine(navigationBar != null
                    ? $"- [DRY] sibling[2] = `{navName}`"
                    : $"- [DRY] sibling[2] = `{navName}` (introuvable)");
                return;
            }

            var order = new List<Transform>(3);
            if (pageContainer != null) order.Add(pageContainer);
            if (infoBar != null) order.Add(infoBar.transform);
            if (navigationBar != null) order.Add(navigationBar.transform);

            if (order.Count == 0)
            {
                log.AppendLine("- (rien à ordonner)");
                return;
            }

            for (int i = 0; i < order.Count; i++)
            {
                Transform t = order[i];
                if (safeRoot == null || t.parent != safeRoot)
                {
                    log.AppendLine($"- sibling[{i}] = `{t.name}` — parent inattendu, skip");
                    continue;
                }

                Undo.RecordObject(t, UndoLabel);
                t.SetSiblingIndex(i);
                log.AppendLine($"- sibling[{i}] = `{t.name}`");
            }
        }

        private static void ForceCanvasLayerOrder(
            Transform canvasTx,
            RectTransform backgroundLayer,
            RectTransform safeRoot,
            RectTransform overlayLayer,
            bool apply,
            StringBuilder log)
        {
            RectTransform[] layers = { backgroundLayer, safeRoot, overlayLayer };
            string[] labels = { BackgroundLayerName, SafeRootName, OverlayLayerName };

            for (int i = 0; i < layers.Length; i++)
            {
                if (!apply)
                {
                    log.AppendLine($"- [DRY] sibling[{i}] = `{labels[i]}`");
                    continue;
                }

                if (layers[i] == null || layers[i].parent != canvasTx)
                {
                    log.AppendLine($"- sibling[{i}] = `{labels[i]}` — indisponible, skip");
                    continue;
                }

                Undo.RecordObject(layers[i], UndoLabel);
                layers[i].SetSiblingIndex(i);
                log.AppendLine($"- sibling[{i}] = `{labels[i]}`");
            }
        }

        private static void ApplyCanvasScalerMatch(Canvas pagesCanvas, bool apply, StringBuilder log)
        {
            CanvasScaler scaler = pagesCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                log.AppendLine("- Aucun CanvasScaler sur le canvas pages");
                return;
            }

            float currentMatch = scaler.matchWidthOrHeight;
            log.AppendLine(
                $"- État actuel : mode=`{scaler.uiScaleMode}`, " +
                $"ref={scaler.referenceResolution}, match={currentMatch:0.###}");

            if (Mathf.Approximately(currentMatch, 1f))
            {
                log.AppendLine("- matchWidthOrHeight déjà = 1.0 — rien à faire");
                return;
            }

            if (!apply)
            {
                log.AppendLine($"- [DRY] matchWidthOrHeight : {currentMatch:0.###} → 1.0");
                return;
            }

            Undo.RecordObject(scaler, UndoLabel);
            SerializedObject so = new SerializedObject(scaler);
            SerializedProperty matchProp = so.FindProperty("m_MatchWidthOrHeight");
            if (matchProp != null)
            {
                matchProp.floatValue = 1f;
                so.ApplyModifiedProperties();
            }
            else
            {
                scaler.matchWidthOrHeight = 1f;
            }

            log.AppendLine($"- matchWidthOrHeight : {currentMatch:0.###} → 1.0");
        }

        // ═══════════════════════════════════════════
        // COLLECTE / HELPERS
        // ═══════════════════════════════════════════

        private static List<GameObject> ReadHubPages(HubManager hubManager)
        {
            var result = new List<GameObject>(4);
            SerializedObject so = new SerializedObject(hubManager);
            SerializedProperty pagesProp = so.FindProperty("pages");
            if (pagesProp == null || !pagesProp.isArray)
                return result;

            for (int i = 0; i < pagesProp.arraySize; i++)
            {
                Object obj = pagesProp.GetArrayElementAtIndex(i).objectReferenceValue;
                result.Add(obj as GameObject);
            }

            return result;
        }

        private static void WarnOtherRootCanvases(Canvas pagesCanvas, StringBuilder log)
        {
            Canvas[] all = Object.FindObjectsOfType<Canvas>(true);
            int extra = 0;
            for (int i = 0; i < all.Length; i++)
            {
                Canvas c = all[i];
                if (c == null || c == pagesCanvas) continue;
                if (c.transform.parent != null) continue;
                extra++;
                log.AppendLine($"⚠ Canvas racine supplémentaire ignoré : `{GetPath(c.transform)}`");
            }

            if (extra == 0)
                log.AppendLine("Canvas racine pages unique — OK");
        }

        /// <summary>
        /// Remonte jusqu'à l'enfant « racine overlay » sous le Canvas (ou déjà sous OverlayLayer).
        /// </summary>
        private static void TryCollectOverlayRoot(
            Transform componentTx,
            Transform canvasTx,
            List<Transform> into)
        {
            if (componentTx == null || canvasTx == null)
                return;

            Transform cursor = componentTx;
            while (cursor.parent != null
                   && cursor.parent != canvasTx
                   && cursor.parent.name != OverlayLayerName)
            {
                cursor = cursor.parent;
            }

            if (cursor.parent != canvasTx && cursor.parent != null
                && cursor.parent.name != OverlayLayerName)
                return;

            if (!into.Contains(cursor))
                into.Add(cursor);
        }

        private static void SortBySiblingIndex(List<Transform> list)
        {
            list.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                if (a.parent == b.parent)
                    return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
                return 0;
            });
        }

        private static HashSet<Transform> BuildClaimedSet(
            RectTransform backgroundLayer,
            RectTransform safeRoot,
            RectTransform overlayLayer,
            RectTransform pageContainer,
            GameObject sceneBackground,
            List<GameObject> pages,
            InfoBarUI infoBar,
            GameObject navigationBar,
            List<Transform> overlayTargets)
        {
            var set = new HashSet<Transform>();
            AddClaim(set, backgroundLayer);
            AddClaim(set, safeRoot);
            AddClaim(set, overlayLayer);
            AddClaim(set, pageContainer);
            if (sceneBackground != null) AddClaim(set, sceneBackground.transform);
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] != null) AddClaim(set, pages[i].transform);
            }

            if (infoBar != null) AddClaim(set, infoBar.transform);
            if (navigationBar != null) AddClaim(set, navigationBar.transform);
            for (int i = 0; i < overlayTargets.Count; i++)
                AddClaim(set, overlayTargets[i]);

            return set;
        }

        private static void AddClaim(HashSet<Transform> set, Component c)
        {
            if (c != null) set.Add(c.transform);
        }

        private static void AddClaim(HashSet<Transform> set, Transform t)
        {
            if (t != null) set.Add(t);
        }

        private static GameObject FindDirectChild(Transform parent, string name)
        {
            Transform t = FindDirectChildTransform(parent, name);
            return t != null ? t.gameObject : null;
        }

        private static Transform FindDirectChildTransform(Transform parent, string name)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static GameObject FindInHierarchyByName(Transform root, string name)
        {
            if (root == null) return null;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == name)
                    return all[i].gameObject;
            }

            return null;
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
