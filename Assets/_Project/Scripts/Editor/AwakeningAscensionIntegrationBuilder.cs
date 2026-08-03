#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChezArthur.Gameplay;
using ChezArthur.UI.ArtworkTransition;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Câble ArtworkTransitionStage sous l'overlay cérémonie (AW3 Ascension).
    /// Idempotent — second run sans changement.
    /// </summary>
    public static class AwakeningAscensionIntegrationBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string PrefabPath =
            "Assets/_Project/Prefabs/UI/ArtworkTransitionStage.prefab";
        private const string OverlayPrefabPath =
            "Assets/_Project/Prefabs/UI/AwakeningCeremonyOverlay.prefab";
        private const string ConfigPath =
            "Assets/_Project/Data/UI/ArtworkTransitionConfig.asset";
        private const string RiserSoundPath =
            "Assets/_Project/Audio/SFX/risersound.mp3";
        private const string GameScenePath =
            "Assets/_Project/Scenes/Game.unity";
        private const string HubScenePath =
            "Assets/_Project/Scenes/Hub.unity";
        private const string ReportRelPath =
            "Audits/awakening_ascension_integration.txt";

        private const string StageName = "ArtworkTransitionStage";
        private const string PortraitContainerName = "PortraitContainer";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Cérémonie/Câbler l'ascension (AW3)")]
        public static void BuildMenu()
        {
            Build();
        }

        /// <summary>
        /// Point d'entrée idempotent (MenuItem + appel scripté).
        /// </summary>
        public static void Build()
        {
            var report = new StringBuilder(4096);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" Cérémonie — câblage ascension AW3");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();

            bool anyChange = false;

            // ── Stage sous le prefab overlay (runtime Instantiate) ──
            if (EnsureStageOnOverlayPrefab(report, out bool overlayChanged))
                anyChange |= overlayChanged;
            else
            {
                WriteReport(report);
                return;
            }

            // ── Contrôleur scène (Game prioritaire, puis Hub) ──
            AwakeningCeremonyController controller = FindControllerPreferOpenScenes(report);
            if (controller == null)
            {
                report.AppendLine(
                    "Aucun contrôleur en scènes ouvertes — ouverture de Game.unity.");
                Scene game = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
                report.AppendLine($"Scène ouverte : {game.path}");
                controller = FindControllerInLoadedScenes();
            }

            if (controller == null)
            {
                report.AppendLine(
                    "Contrôleur absent de Game — tentative Hub.unity.");
                Scene hub = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
                report.AppendLine($"Scène ouverte : {hub.path}");
                controller = FindControllerInLoadedScenes();
            }

            if (controller == null)
            {
                report.AppendLine(
                    "⚠ AwakeningCeremonyController introuvable en scène.");
                report.AppendLine(
                    "  → Le stage est sur le prefab overlay ; le controller " +
                    "reliera au runtime via BindAscensionStageFromOverlayInstance.");
                report.AppendLine(
                    "  → Preview Hub : AwakeningCeremonyDebugButton créera le controller à la volée.");
            }
            else
            {
                report.AppendLine(
                    $"Contrôleur : '{controller.name}' (scène '{controller.gameObject.scene.path}').");
                if (WireController(controller, report))
                    anyChange = true;
            }

            // ── Config riserClip provisoire ──
            if (EnsureRiserClip(report))
                anyChange = true;

            // ── Dirty + save ──
            if (anyChange)
            {
                if (controller != null)
                {
                    EditorUtility.SetDirty(controller);
                    Scene scene = controller.gameObject.scene;
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (scene.path == GameScenePath
                        || scene.path == HubScenePath
                        || scene.name == "Game"
                        || scene.name == "Hub")
                    {
                        EditorSceneManager.SaveScene(scene);
                        report.AppendLine($"Scène sauvegardée : {scene.path}");
                    }
                    else
                    {
                        report.AppendLine(
                            $"Scène marquée dirty : {scene.path} — save manuelle si besoin.");
                    }
                }

                AssetDatabase.SaveAssets();
            }

            report.AppendLine();
            report.AppendLine(
                "Note M1 (gacha) : re-run « Chez Arthur/Gacha/Câbler la déchéance (AW2) » " +
                "si le rapport gacha doit relister le câblage ; AW3 ne touche pas le gacha.");
            report.AppendLine();
            if (anyChange)
                report.AppendLine("Résultat : modifications appliquées.");
            else
                report.AppendLine("Résultat : aucune modification (idempotent).");
            report.AppendLine("═══════════════════════════════════════════");

            WriteReport(report);
            Debug.Log(
                $"[AwakeningAscensionIntegrationBuilder] OK — rapport : {ReportRelPath}" +
                (anyChange ? " (changements)" : " (idempotent)"));
        }

        // ═══════════════════════════════════════════
        // OVERLAY PREFAB
        // ═══════════════════════════════════════════

        /// <summary>
        /// Instancie le stage sous l'overlay (parent zone portrait), framing prefab
        /// par défaut (pas de stretch carte comme le gacha). Inactif.
        /// </summary>
        private static bool EnsureStageOnOverlayPrefab(StringBuilder report, out bool changed)
        {
            changed = false;

            GameObject overlayPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            if (overlayPrefab == null)
            {
                report.AppendLine($"ÉCHEC : overlay introuvable → {OverlayPrefabPath}");
                Debug.LogError(
                    $"[AwakeningAscensionIntegrationBuilder] Overlay manquant : {OverlayPrefabPath}");
                return false;
            }

            string prefabPath = AssetDatabase.GetAssetPath(overlayPrefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform portrait = FindNamedChild(root.transform, PortraitContainerName);
                Transform parent = portrait != null ? portrait.parent : root.transform;
                report.AppendLine(
                    portrait != null
                        ? $"Parent stage : sibling de '{PortraitContainerName}'."
                        : "Parent stage : racine overlay (PortraitContainer introuvable).");

                Transform existing = FindNamedChild(root.transform, StageName);
                GameObject stageGo;

                if (existing == null)
                {
                    GameObject stagePrefab =
                        AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                    if (stagePrefab == null)
                    {
                        report.AppendLine($"ÉCHEC : prefab introuvable → {PrefabPath}");
                        Debug.LogError(
                            $"[AwakeningAscensionIntegrationBuilder] Prefab manquant : {PrefabPath}");
                        return false;
                    }

                    stageGo = PrefabUtility.InstantiatePrefab(stagePrefab, parent) as GameObject;
                    if (stageGo == null)
                    {
                        report.AppendLine("ÉCHEC : InstantiatePrefab a renvoyé null.");
                        return false;
                    }

                    stageGo.name = StageName;
                    // Remplit la zone parent pour le stage, mais NE stretch PAS la Card
                    // (framing ~62 % du prefab AW1 — distinct du plein cadre gacha).
                    StretchFull(stageGo.GetComponent<RectTransform>());
                    if (portrait != null)
                        stageGo.transform.SetSiblingIndex(portrait.GetSiblingIndex() + 1);

                    stageGo.SetActive(false);
                    changed = true;
                    report.AppendLine(
                        $"Stage : instancié sous overlay (inactif, framing prefab) → {PrefabPath}");
                }
                else
                {
                    stageGo = existing.gameObject;
                    report.AppendLine("Stage : déjà présent sur overlay (idempotent).");

                    if (stageGo.transform.parent != parent && parent != null)
                    {
                        int sibling = portrait != null
                            ? portrait.GetSiblingIndex() + 1
                            : parent.childCount;
                        stageGo.transform.SetParent(parent, false);
                        stageGo.transform.SetSiblingIndex(sibling);
                        StretchFull(stageGo.GetComponent<RectTransform>());
                        changed = true;
                        report.AppendLine("Stage : reparenté sous zone portrait.");
                    }

                    if (stageGo.activeSelf)
                    {
                        stageGo.SetActive(false);
                        changed = true;
                        report.AppendLine("Stage : désactivé (était actif).");
                    }
                }

                ArtworkTransitionDriver driver =
                    stageGo.GetComponent<ArtworkTransitionDriver>();
                if (driver == null)
                    driver = stageGo.GetComponentInChildren<ArtworkTransitionDriver>(true);

                if (driver == null)
                    report.AppendLine("⚠ ArtworkTransitionDriver introuvable sur le stage.");
                else
                    report.AppendLine("Driver : ArtworkTransitionDriver OK sur overlay.");

                // Vérifie que la Card n'a pas été stretchée plein cadre (anti-régression gacha).
                ArtworkTransitionView stageView =
                    stageGo.GetComponent<ArtworkTransitionView>();
                if (stageView == null)
                    stageView = stageGo.GetComponentInChildren<ArtworkTransitionView>(true);
                RectTransform cardRt = stageView != null ? stageView.CardRect : null;
                if (cardRt != null)
                {
                    bool fullStretch =
                        Mathf.Approximately(cardRt.anchorMin.x, 0f)
                        && Mathf.Approximately(cardRt.anchorMin.y, 0f)
                        && Mathf.Approximately(cardRt.anchorMax.x, 1f)
                        && Mathf.Approximately(cardRt.anchorMax.y, 1f);
                    if (fullStretch)
                    {
                        report.AppendLine(
                            "⚠ Card en stretch plein cadre — framing cérémonie attendu " +
                            "(centré ~62 %). Ne pas appliquer LayoutDecheance gacha ici.");
                    }
                    else
                    {
                        report.AppendLine(
                            $"Card framing prefab OK (size {cardRt.sizeDelta.x:F0}×{cardRt.sizeDelta.y:F0}).");
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    report.AppendLine($"Overlay prefab sauvegardé : {prefabPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return true;
        }

        // ═══════════════════════════════════════════
        // WIRE CONTROLLER
        // ═══════════════════════════════════════════

        /// <summary>
        /// Note les champs artwork* sur le controller. Les refs réelles viennent
        /// du clone overlay au runtime (BindAscensionStageFromOverlayInstance).
        /// Si un stage scène existe déjà sous le controller, on le câble.
        /// </summary>
        private static bool WireController(
            AwakeningCeremonyController controller,
            StringBuilder report)
        {
            if (controller == null)
                return false;

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty driverProp = so.FindProperty("artworkDriver");
            SerializedProperty stageRootProp = so.FindProperty("artworkStageRoot");
            bool wireChanged = false;

            // Cherche un stage déjà en scène (debug / legacy) — sinon laisse null
            // (runtime bind depuis overlay).
            Transform sceneStage = FindNamedChild(controller.transform, StageName);
            ArtworkTransitionDriver sceneDriver = null;
            GameObject sceneStageGo = null;
            if (sceneStage != null)
            {
                sceneStageGo = sceneStage.gameObject;
                sceneDriver = sceneStage.GetComponent<ArtworkTransitionDriver>()
                    ?? sceneStage.GetComponentInChildren<ArtworkTransitionDriver>(true);
            }

            if (driverProp != null)
            {
                if (sceneDriver != null && driverProp.objectReferenceValue != sceneDriver)
                {
                    driverProp.objectReferenceValue = sceneDriver;
                    wireChanged = true;
                    report.AppendLine("Wire : artworkDriver ← stage scène (sous controller).");
                }
                else if (driverProp.objectReferenceValue != null)
                {
                    report.AppendLine(
                        "Wire : artworkDriver déjà assigné (runtime rebind overlay prioritaire).");
                }
                else
                {
                    report.AppendLine(
                        "Wire : artworkDriver null OK — bind runtime depuis overlay prefab.");
                }
            }
            else
            {
                report.AppendLine(
                    "Wire : propriété artworkDriver absente (code AW3 pas encore compilé).");
            }

            if (stageRootProp != null)
            {
                if (sceneStageGo != null && stageRootProp.objectReferenceValue != sceneStageGo)
                {
                    stageRootProp.objectReferenceValue = sceneStageGo;
                    wireChanged = true;
                    report.AppendLine("Wire : artworkStageRoot ← stage scène.");
                }
                else if (stageRootProp.objectReferenceValue != null)
                {
                    report.AppendLine(
                        "Wire : artworkStageRoot déjà assigné (runtime rebind overlay prioritaire).");
                }
                else
                {
                    report.AppendLine(
                        "Wire : artworkStageRoot null OK — bind runtime depuis overlay prefab.");
                }
            }
            else
            {
                report.AppendLine(
                    "Wire : propriété artworkStageRoot absente (code AW3 pas encore compilé).");
            }

            if (wireChanged)
                so.ApplyModifiedPropertiesWithoutUndo();

            return wireChanged;
        }

        // ═══════════════════════════════════════════
        // CONFIG AUDIO
        // ═══════════════════════════════════════════

        /// <summary>
        /// Si config.riserClip null → risersound.mp3 PROVISOIRE (AW4 remplacera).
        /// </summary>
        private static bool EnsureRiserClip(StringBuilder report)
        {
            ArtworkTransitionConfig config =
                AssetDatabase.LoadAssetAtPath<ArtworkTransitionConfig>(ConfigPath);
            if (config == null)
            {
                report.AppendLine($"Config : introuvable → {ConfigPath}");
                return false;
            }

            if (config.riserClip != null)
            {
                report.AppendLine($"Config : riserClip déjà assigné ({config.riserClip.name}).");
                return false;
            }

            AudioClip riser = AssetDatabase.LoadAssetAtPath<AudioClip>(RiserSoundPath);
            if (riser == null)
            {
                report.AppendLine($"Config : riser clip introuvable → {RiserSoundPath}");
                return false;
            }

            config.riserClip = riser;
            EditorUtility.SetDirty(config);
            report.AppendLine(
                $"Config : riserClip ← {RiserSoundPath} (PROVISOIRE AW3).");
            return true;
        }

        // ═══════════════════════════════════════════
        // RÉSOLUTION CONTRÔLEUR / SCÈNE
        // ═══════════════════════════════════════════

        private static AwakeningCeremonyController FindControllerPreferOpenScenes(
            StringBuilder report)
        {
            AwakeningCeremonyController found = FindControllerInLoadedScenes();
            if (found != null)
            {
                report.AppendLine(
                    $"Contrôleur trouvé en scènes ouvertes : '{found.name}'.");
                return found;
            }

            report.AppendLine("Aucun AwakeningCeremonyController en scènes ouvertes.");
            return null;
        }

        private static AwakeningCeremonyController FindControllerInLoadedScenes()
        {
            AwakeningCeremonyController[] all =
                Object.FindObjectsOfType<AwakeningCeremonyController>(true);
            if (all == null || all.Length == 0)
                return null;

            for (int i = 0; i < all.Length; i++)
            {
                AwakeningCeremonyController c = all[i];
                if (c == null)
                    continue;
                Scene s = c.gameObject.scene;
                if (s.IsValid() && (s.path == GameScenePath || s.name == "Game"))
                    return c;
            }

            for (int i = 0; i < all.Length; i++)
            {
                AwakeningCeremonyController c = all[i];
                if (c == null)
                    continue;
                Scene s = c.gameObject.scene;
                if (s.IsValid() && (s.path == HubScenePath || s.name == "Hub"))
                    return c;
            }

            return all[0];
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static Transform FindNamedChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            if (root.name == childName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamedChild(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void StretchFull(RectTransform rt)
        {
            if (rt == null)
                return;

            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void WriteReport(StringBuilder report)
        {
            string auditsRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, "awakening_ascension_integration.txt");
            File.WriteAllText(fullPath, report.ToString(), Encoding.UTF8);
            Debug.Log($"[AwakeningAscensionIntegrationBuilder] Rapport écrit : {fullPath}");
        }
    }
}
#endif
