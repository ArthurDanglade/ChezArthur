#if UNITY_EDITOR
using System.Text;
using ChezArthur.Hub;
using ChezArthur.Hub.Pages;
using ChezArthur.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Gate 3.1 — HomeIllustrationRig + BottomZone + framing cover.
    /// Harnais v2. Identification des couches par inspection (pas Find sur noms à espaces).
    /// </summary>
    public static class HomeRigBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string UndoLabel = "Home Rig 3.1";
        private const string RigName = "HomeIllustrationRig";
        private const string BottomZoneName = "BottomZone";
        private const string WindowSpriteGuid = "8cf9e715a0f7d074e9ae3b417614fd3b";
        private const string BaseSpriteGuid = "68da5987414e110478bee5c8170f5b8f";
        private const string CharSpriteGuid = "02845a251b44de64f91a057d7a7d2c0c";
        private const string VfxTextureGuid = "aeac67c8edc4b3a4190b43a15614c751";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Refonte Hub/Construire le Rig Accueil (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Construire le Rig Accueil (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rig Accueil Gate 3.1",
                    "Va créer HomeIllustrationRig + BottomZone sous PageAccueil.\n" +
                    "LOCK 2.1 / nav / UI Jouer intacts.\nContinuer ?",
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
            log.AppendLine($" HomeRigBuilder — {mode}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine(" Convergence = À FAIRE : 0");
            log.AppendLine(" LOCK 2.1 : header / nav non modifiés");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int todo = 0;
            int conforme = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[HomeRigBuilder] Aucune scène active.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine();

            RectTransform pageAccueil = FindPageAccueil(scene);
            if (pageAccueil == null)
            {
                failed++;
                log.AppendLine("- ✗ PageAccueil introuvable — abort");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            log.AppendLine($"PageAccueil : `{GetPath(pageAccueil)}`");
            log.AppendLine();

            // Classification des enfants (inspection, pas Find(nom)).
            LayerRefs layers = ResolveLayers(pageAccueil, log);
            if (!layers.AllIllustrationFound)
            {
                failed++;
                log.AppendLine("- ✗ Couches illustration incomplètes — abort");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            log.AppendLine("## Identification couches (inspection)");
            log.AppendLine($"- Landscape : `{GetPath(layers.Landscape)}` ✓");
            log.AppendLine($"- Window : `{GetPath(layers.Window)}` ✓");
            log.AppendLine($"- Wagon : `{GetPath(layers.Wagon)}` ✓");
            log.AppendLine($"- Character : `{GetPath(layers.Character)}` ✓");
            log.AppendLine($"- LightOverlay : `{GetPath(layers.Light)}` ✓");
            log.AppendLine($"- UILayer : {(layers.UiLayer != null ? GetPath(layers.UiLayer) : "—")} ");
            log.AppendLine($"- ModeSelectOverlay : {(layers.ModeSelect != null ? GetPath(layers.ModeSelect) : "—")} ");
            log.AppendLine();

            // 1. Rig + reparent
            log.AppendLine("## HomeIllustrationRig + reparent");
            RectTransform rig = EnsureRigAndReparent(
                pageAccueil, layers, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 2. BottomZone
            log.AppendLine("## BottomZone");
            RectTransform bottomZone = EnsureBottomZone(
                pageAccueil, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 3. Framing
            log.AppendLine("## HomeIllustrationFraming");
            EnsureFraming(rig, bottomZone, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 4. Remove PageAccueil Image veil
            log.AppendLine("## PageAccueil Image (voile alpha)");
            EnsurePageImageRemoved(pageAccueil, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // 5. Sibling order
            log.AppendLine("## Ordre siblings (Rig → BottomZone → UILayer → ModeSelect)");
            EnsureSiblingOrder(
                pageAccueil, rig, bottomZone, layers, apply, log, ref todo, ref conforme, ref failed);

            // PageAccueilUI intact
            log.AppendLine();
            log.AppendLine("## PageAccueilUI");
            if (pageAccueil.GetComponent<PageAccueilUI>() != null)
            {
                conforme++;
                log.AppendLine("- PageAccueilUI présent — intact ✓");
            }
            else
            {
                failed++;
                log.AppendLine("- PageAccueilUI absent ✗");
            }

            if (apply)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                log.AppendLine();
                log.AppendLine("Scène marquée dirty — Ctrl+S.");
            }

            AppendCounter(log, todo, conforme, failed);
            Debug.Log(log.ToString());

            if (apply && failed == 0 && todo == 0)
                Debug.Log($"[HomeRigBuilder] APPLIQUER OK — CONFORMES={conforme}.");
            else if (apply && failed > 0)
                Debug.LogError($"[HomeRigBuilder] APPLIQUER INCOMPLET — échecs={failed}.");
            else if (!apply && todo == 0 && failed == 0)
                Debug.Log($"[HomeRigBuilder] DRY RUN — convergence OK (CONFORMES={conforme}).");
        }

        private static void AppendCounter(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine();
            log.AppendLine("## COMPTEUR D'ACTIONS (harnais v2)");
            log.AppendLine($"- À FAIRE : {todo}");
            log.AppendLine($"- CONFORMES : {conforme}");
            log.AppendLine($"- ÉCHECS : {failed}");
            log.AppendLine(todo == 0 && failed == 0
                ? "- Convergence : OUI (À FAIRE = 0)"
                : "- Convergence : NON");
        }

        // ═══════════════════════════════════════════
        // ÉTAPES
        // ═══════════════════════════════════════════

        private static RectTransform EnsureRigAndReparent(
            RectTransform page,
            LayerRefs layers,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            RectTransform rig = FindDirectChildNamed(page, RigName);
            bool orderOk = IsRigParentingOk(rig, layers);

            if (rig != null && orderOk)
            {
                if (apply)
                    ApplyCoverStretch(page, layers);

                // Vérifie si stretch racines + paysage / mask / preserveAspect restent à faire.
                bool visualsOk = AreIllustrationLayersStretched(layers)
                                 && IsLandscapeChildrenStretched(layers.Landscape)
                                 && page.GetComponent<RectMask2D>() != null
                                 && !HasPreserveAspect(layers.Window)
                                 && !HasPreserveAspect(layers.Wagon)
                                 && !HasPreserveAspect(layers.Character);

                if (visualsOk)
                {
                    conforme++;
                    log.AppendLine("- Rig présent + couches plein cadre + mask ✓");
                    return rig;
                }

                if (!apply)
                {
                    todo++;
                    log.AppendLine(
                        "- [DRY] Stretch 5 couches (sizeDelta 0) + paysage + RectMask2D + preserveAspect off — À FAIRE");
                    return rig;
                }

                conforme++;
                log.AppendLine("- Rig présent, correctifs visuels cover ✓ → conforme");
                return rig;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    rig == null
                        ? "- [DRY] CRÉER HomeIllustrationRig + reparent 5 couches — À FAIRE"
                        : "- [DRY] Réordonner / reparenter couches sous le rig — À FAIRE");
                return rig;
            }

            if (rig == null)
            {
                GameObject go = new GameObject(RigName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                rig = go.GetComponent<RectTransform>();
                rig.SetParent(page, false);
                go.layer = page.gameObject.layer;
                rig.anchorMin = new Vector2(0.5f, 0.5f);
                rig.anchorMax = new Vector2(0.5f, 0.5f);
                rig.pivot = new Vector2(0.5f, 0.5f);
                rig.anchoredPosition = Vector2.zero;
                rig.sizeDelta = new Vector2(
                    HomeIllustrationFraming.NativeWidth,
                    HomeIllustrationFraming.NativeHeight);
                rig.localScale = Vector3.one;
            }

            // Reparent dans l'ordre exact, puis stretch plein cadre (plus de sizeDelta négatif).
            Transform[] ordered =
            {
                layers.Landscape, layers.Window, layers.Wagon, layers.Character, layers.Light
            };
            for (int i = 0; i < ordered.Length; i++)
            {
                Transform t = ordered[i];
                if (t == null)
                    continue;
                Undo.SetTransformParent(t, rig, UndoLabel);
                t.SetSiblingIndex(i);
            }

            ApplyCoverStretch(page, layers);

            if (IsRigParentingOk(rig, layers))
            {
                conforme++;
                log.AppendLine("- HomeIllustrationRig + reparent ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- Reparent rig — ÉCHEC ✗");
            }

            return rig;
        }

        private static bool HasPreserveAspect(Transform layer)
        {
            if (layer == null)
                return false;
            Image img = layer.GetComponent<Image>();
            return img != null && img.preserveAspect;
        }

        /// <summary>
        /// Stretch racines + enfants paysage + mask page (cover sans bandes).
        /// </summary>
        private static void ApplyCoverStretch(RectTransform page, LayerRefs layers)
        {
            Transform[] roots =
            {
                layers.Landscape, layers.Window, layers.Wagon, layers.Character, layers.Light
            };
            for (int i = 0; i < roots.Length; i++)
            {
                StretchLayerRoot(roots[i]);
                DisablePreserveAspect(roots[i]);
            }

            if (layers.Landscape != null)
                StretchLandscapeChildren(layers.Landscape);

            EnsurePageRectMask(page);
        }

        /// <summary>
        /// true si les 5 couches illustration remplissent le rig (pas de sizeDelta négatif).
        /// </summary>
        private static bool AreIllustrationLayersStretched(LayerRefs layers)
        {
            return IsLayerRootStretched(layers.Landscape)
                   && IsLayerRootStretched(layers.Window)
                   && IsLayerRootStretched(layers.Wagon)
                   && IsLayerRootStretched(layers.Character)
                   && IsLayerRootStretched(layers.Light);
        }

        private static bool IsLayerRootStretched(Transform layer)
        {
            if (layer == null)
                return false;
            RectTransform rt = layer as RectTransform;
            if (rt == null)
                return false;
            return Mathf.Approximately(rt.anchorMin.x, 0f)
                   && Mathf.Approximately(rt.anchorMin.y, 0f)
                   && Mathf.Approximately(rt.anchorMax.x, 1f)
                   && Mathf.Approximately(rt.anchorMax.y, 1f)
                   && Mathf.Approximately(rt.sizeDelta.x, 0f)
                   && Mathf.Approximately(rt.sizeDelta.y, 0f)
                   && Mathf.Approximately(rt.anchoredPosition.x, 0f)
                   && Mathf.Approximately(rt.anchoredPosition.y, 0f);
        }

        private static bool IsLandscapeChildrenStretched(Transform landscape)
        {
            if (landscape == null || landscape.childCount == 0)
                return true;
            for (int i = 0; i < landscape.childCount; i++)
            {
                RectTransform rt = landscape.GetChild(i) as RectTransform;
                if (rt == null)
                    continue;
                if (!Mathf.Approximately(rt.anchorMin.x, 0f)
                    || !Mathf.Approximately(rt.anchorMin.y, 0f)
                    || !Mathf.Approximately(rt.anchorMax.x, 1f)
                    || !Mathf.Approximately(rt.anchorMax.y, 1f)
                    || !Mathf.Approximately(rt.sizeDelta.x, 0f)
                    || !Mathf.Approximately(rt.sizeDelta.y, 0f))
                    return false;
            }

            return true;
        }

        private static void DisablePreserveAspect(Transform layer)
        {
            if (layer == null)
                return;
            Image img = layer.GetComponent<Image>();
            if (img != null && img.preserveAspect)
            {
                Undo.RecordObject(img, UndoLabel);
                img.preserveAspect = false;
                EditorUtility.SetDirty(img);
            }
        }

        /// <summary>
        /// Force une couche illustration à remplir 100 % du HomeIllustrationRig.
        /// (sizeDelta négatif = bandes latérales — dette legacy phone-frame.)
        /// </summary>
        private static void StretchLayerRoot(Transform layer)
        {
            if (layer == null)
                return;
            RectTransform rt = layer as RectTransform;
            if (rt == null)
                return;

            Undo.RecordObject(rt, UndoLabel);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            EditorUtility.SetDirty(rt);
        }

        /// <summary>
        /// Les RawImage paysage en size fixe 1080 centrée laissent des bords vides
        /// (fond noir) dès que le parent est plus large — on les passe en stretch.
        /// </summary>
        private static void StretchLandscapeChildren(Transform landscape)
        {
            for (int i = 0; i < landscape.childCount; i++)
            {
                RectTransform rt = landscape.GetChild(i) as RectTransform;
                if (rt == null)
                    continue;

                Undo.RecordObject(rt, UndoLabel);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.localScale = Vector3.one;
                EditorUtility.SetDirty(rt);
            }
        }

        private static void EnsurePageRectMask(RectTransform page)
        {
            if (page.GetComponent<RectMask2D>() != null)
                return;
            Undo.AddComponent<RectMask2D>(page.gameObject);
        }

        private static bool IsRigParentingOk(RectTransform rig, LayerRefs layers)
        {
            if (rig == null)
                return false;
            if (layers.Landscape == null || layers.Landscape.parent != rig)
                return false;
            if (layers.Window == null || layers.Window.parent != rig)
                return false;
            if (layers.Wagon == null || layers.Wagon.parent != rig)
                return false;
            if (layers.Character == null || layers.Character.parent != rig)
                return false;
            if (layers.Light == null || layers.Light.parent != rig)
                return false;

            return layers.Landscape.GetSiblingIndex() < layers.Window.GetSiblingIndex()
                   && layers.Window.GetSiblingIndex() < layers.Wagon.GetSiblingIndex()
                   && layers.Wagon.GetSiblingIndex() < layers.Character.GetSiblingIndex()
                   && layers.Character.GetSiblingIndex() < layers.Light.GetSiblingIndex();
        }

        private static RectTransform EnsureBottomZone(
            RectTransform page,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            RectTransform zone = FindDirectChildNamed(page, BottomZoneName);
            bool ok = zone != null && IsBottomZoneConforme(zone);

            if (ok)
            {
                conforme++;
                log.AppendLine($"- BottomZone conforme (anchors bas, posY≥NavHeight) ✓");
                return zone;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] CRÉER/aligner BottomZone — À FAIRE");
                return zone;
            }

            if (zone == null)
            {
                GameObject go = new GameObject(BottomZoneName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                zone = go.GetComponent<RectTransform>();
                zone.SetParent(page, false);
                go.layer = page.gameObject.layer;
            }

            Undo.RecordObject(zone, UndoLabel);
            zone.anchorMin = new Vector2(0f, 0f);
            zone.anchorMax = new Vector2(1f, 0f);
            zone.pivot = new Vector2(0.5f, 0f);
            zone.anchoredPosition = new Vector2(0f, UiTheme.NavHeight);
            zone.sizeDelta = new Vector2(0f, 0f);
            zone.localScale = Vector3.one;
            EditorUtility.SetDirty(zone);

            if (IsBottomZoneConforme(zone))
            {
                conforme++;
                log.AppendLine("- BottomZone créé/aligné ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- BottomZone — ÉCHEC ✗");
            }

            return zone;
        }

        private static bool IsBottomZoneConforme(RectTransform zone)
        {
            // Hauteur = contenu (CSF). posY = hauteur nav réelle (BottomZoneNavClearance).
            return Mathf.Approximately(zone.anchorMin.x, 0f)
                   && Mathf.Approximately(zone.anchorMin.y, 0f)
                   && Mathf.Approximately(zone.anchorMax.x, 1f)
                   && Mathf.Approximately(zone.anchorMax.y, 0f)
                   && Mathf.Approximately(zone.pivot.y, 0f)
                   && zone.anchoredPosition.y >= UiTheme.NavHeight - 0.5f;
        }

        private static void EnsureFraming(
            RectTransform rig,
            RectTransform bottomZone,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (rig == null)
            {
                if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] HomeIllustrationFraming (après création du rig) — À FAIRE");
                    return;
                }

                log.AppendLine("- Rig absent — framing skip");
                return;
            }

            HomeIllustrationFraming framing = rig.GetComponent<HomeIllustrationFraming>();
            bool wired = framing != null && IsFramingWired(framing, bottomZone);

            if (wired)
            {
                conforme++;
                log.AppendLine("- HomeIllustrationFraming câblé (focusY=0.38, BottomZone) ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] AJOUTER/câbler HomeIllustrationFraming — À FAIRE");
                return;
            }

            if (framing == null)
                framing = Undo.AddComponent<HomeIllustrationFraming>(rig.gameObject);

            SerializedObject so = new SerializedObject(framing);
            so.FindProperty("focusX").floatValue = 0.5f;
            so.FindProperty("focusY").floatValue = 0.38f;
            so.FindProperty("bottomZone").objectReferenceValue = bottomZone;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(framing);
            framing.Refresh();

            if (IsFramingWired(framing, bottomZone))
            {
                conforme++;
                log.AppendLine("- HomeIllustrationFraming câblé ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- HomeIllustrationFraming — ÉCHEC ✗");
            }
        }

        private static bool IsFramingWired(HomeIllustrationFraming framing, RectTransform bottomZone)
        {
            SerializedObject so = new SerializedObject(framing);
            if (!Mathf.Approximately(so.FindProperty("focusY").floatValue, 0.38f))
                return false;
            return so.FindProperty("bottomZone").objectReferenceValue == bottomZone;
        }

        private static void EnsurePageImageRemoved(
            RectTransform page,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            Image img = page.GetComponent<Image>();
            if (img == null)
            {
                conforme++;
                log.AppendLine("- Image racine PageAccueil absente ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] SUPPRIMER Image voile PageAccueil — À FAIRE");
                return;
            }

            Undo.DestroyObjectImmediate(img);
            if (page.GetComponent<Image>() == null)
            {
                conforme++;
                log.AppendLine("- Image voile PageAccueil supprimée ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- Suppression Image — ÉCHEC ✗");
            }
        }

        private static void EnsureSiblingOrder(
            RectTransform page,
            RectTransform rig,
            RectTransform bottomZone,
            LayerRefs layers,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (rig == null || bottomZone == null)
            {
                if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] Réordonner siblings (après création rig/zone) — À FAIRE");
                    return;
                }

                failed++;
                log.AppendLine("- Ordre impossible (rig/zone manquants) ✗");
                return;
            }

            bool ok =
                rig.GetSiblingIndex() < bottomZone.GetSiblingIndex()
                && (layers.UiLayer == null
                    || (bottomZone.GetSiblingIndex() < layers.UiLayer.GetSiblingIndex()
                        && (layers.ModeSelect == null
                            || layers.UiLayer.GetSiblingIndex() < layers.ModeSelect.GetSiblingIndex())));

            if (ok && layers.ModeSelect != null
                && layers.ModeSelect.GetSiblingIndex() == page.childCount - 1)
            {
                // ModeSelect dernier parmi les connus — OK si rien après critique
            }

            if (ok)
            {
                conforme++;
                log.AppendLine("- Ordre Rig → BottomZone → UILayer → ModeSelect ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Réordonner siblings — À FAIRE");
                return;
            }

            int index = 0;
            rig.SetSiblingIndex(index++);
            bottomZone.SetSiblingIndex(index++);
            if (layers.UiLayer != null)
                layers.UiLayer.SetSiblingIndex(index++);
            if (layers.ModeSelect != null)
                layers.ModeSelect.SetSiblingIndex(index);

            conforme++;
            log.AppendLine("- Ordre siblings corrigé ✓ → conforme");
        }

        // ═══════════════════════════════════════════
        // RÉSOLUTION COUCHES (inspection)
        // ═══════════════════════════════════════════

        private struct LayerRefs
        {
            public Transform Landscape;
            public Transform Window;
            public Transform Wagon;
            public Transform Character;
            public Transform Light;
            public Transform UiLayer;
            public Transform ModeSelect;

            public bool AllIllustrationFound =>
                Landscape != null && Window != null && Wagon != null
                && Character != null && Light != null;
        }

        private static LayerRefs ResolveLayers(RectTransform page, StringBuilder log)
        {
            var refs = new LayerRefs();

            // Inclut enfants déjà sous un rig existant.
            RectTransform existingRig = FindDirectChildNamed(page, RigName);
            Transform[] pool = CollectCandidates(page, existingRig);

            Sprite windowSp = LoadSprite(WindowSpriteGuid);
            Sprite baseSp = LoadSprite(BaseSpriteGuid);
            Sprite charSp = LoadSprite(CharSpriteGuid);
            Texture2D vfxTex = LoadTexture(VfxTextureGuid);

            for (int i = 0; i < pool.Length; i++)
            {
                Transform t = pool[i];
                if (t == null)
                    continue;

                ParallaxManager pm = t.GetComponent<ParallaxManager>();
                if (pm != null && refs.Landscape == null)
                {
                    refs.Landscape = t;
                    continue;
                }

                Image img = t.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    if (windowSp != null && img.sprite == windowSp && refs.Window == null)
                    {
                        refs.Window = t;
                        continue;
                    }
                    if (baseSp != null && img.sprite == baseSp && refs.Wagon == null)
                    {
                        refs.Wagon = t;
                        continue;
                    }
                    if (charSp != null && img.sprite == charSp && refs.Character == null)
                    {
                        refs.Character = t;
                        continue;
                    }
                }

                RawImage raw = t.GetComponent<RawImage>();
                if (raw != null && vfxTex != null && raw.texture == vfxTex && refs.Light == null)
                {
                    refs.Light = t;
                    continue;
                }
            }

            // UI legacy / actions : identification par nom (gate 3.2 a retiré modeSelectRoot).
            refs.UiLayer = FindDirectChildNamed(page, "UILayer");
            Transform modeSelect = page.Find("ModeSelectOverlay");
            if (modeSelect == null)
            {
                for (int i = 0; i < page.childCount; i++)
                {
                    Transform c = page.GetChild(i);
                    if (c != null && c.name == "ModeSelectOverlay")
                    {
                        modeSelect = c;
                        break;
                    }
                }
            }

            refs.ModeSelect = modeSelect;

            if (!refs.AllIllustrationFound)
            {
                if (refs.Landscape == null) log.AppendLine("- Landscape (ParallaxManager) introuvable ✗");
                if (refs.Window == null) log.AppendLine("- Window (sprite reflection) introuvable ✗");
                if (refs.Wagon == null) log.AppendLine("- Wagon (base.png) introuvable ✗");
                if (refs.Character == null) log.AppendLine("- Character (char.png) introuvable ✗");
                if (refs.Light == null) log.AppendLine("- LightOverlay (vfx.png RawImage) introuvable ✗");
            }

            return refs;
        }

        private static Transform[] CollectCandidates(RectTransform page, RectTransform existingRig)
        {
            // Enfants directs de la page + enfants du rig (si présent).
            int count = page.childCount;
            if (existingRig != null)
                count += existingRig.childCount;

            var list = new Transform[count];
            int n = 0;
            for (int i = 0; i < page.childCount; i++)
            {
                Transform c = page.GetChild(i);
                if (c == existingRig)
                    continue;
                list[n++] = c;
            }

            if (existingRig != null)
            {
                for (int i = 0; i < existingRig.childCount; i++)
                    list[n++] = existingRig.GetChild(i);
            }

            if (n == list.Length)
                return list;

            var trimmed = new Transform[n];
            for (int i = 0; i < n; i++)
                trimmed[i] = list[i];
            return trimmed;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static RectTransform FindPageAccueil(Scene scene)
        {
            HubManager hub = Object.FindObjectOfType<HubManager>();
            if (hub != null && hub.AccueilPage != null)
                return hub.AccueilPage.transform as RectTransform;

            PageAccueilUI ui = Object.FindObjectOfType<PageAccueilUI>(true);
            return ui != null ? ui.transform as RectTransform : null;
        }

        private static RectTransform FindDirectChildNamed(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c != null && c.name == name)
                    return c as RectTransform;
            }

            return null;
        }

        private static Sprite LoadSprite(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D LoadTexture(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static string GetPath(Transform t)
        {
            if (t == null)
                return "null";
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }

            return path;
        }
    }
}
#endif
