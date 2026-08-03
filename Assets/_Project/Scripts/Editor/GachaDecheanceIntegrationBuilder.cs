#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.Gacha;
using ChezArthur.UI.ArtworkTransition;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Câble ArtworkTransitionStage sous le reveal gacha (AW2 déchéance).
    /// Idempotent — second run sans changement.
    /// </summary>
    public static class GachaDecheanceIntegrationBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string PrefabPath =
            "Assets/_Project/Prefabs/UI/ArtworkTransitionStage.prefab";
        private const string ConfigPath =
            "Assets/_Project/Data/UI/ArtworkTransitionConfig.asset";
        private const string HubScenePath =
            "Assets/_Project/Scenes/Hub.unity";
        private const string RevealSoundPath =
            "Assets/_Project/Audio/SFX/revealsound.mp3";
        private const string BurnSoundPath =
            "Assets/_Project/Audio/SFX/Gacha/sfx_gacha_burn.wav";
        private const string ReportRelPath =
            "Audits/gacha_decheance_integration.txt";

        private const string StageName = "ArtworkTransitionStage";
        private const string BurnUnderlayName = "GachaBurnDechuUnderlay";
        private const string BurnFxName = "GachaBurnFx";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Gacha/Câbler la déchéance (AW2)")]
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
            report.AppendLine(" Gacha — câblage déchéance AW2");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();

            bool anyChange = false;

            GachaAnimationController controller = FindControllerPreferOpenScenes(report);
            if (controller == null)
            {
                report.AppendLine(
                    "Aucun contrôleur en scènes ouvertes — ouverture de Hub.unity.");
                Scene hub = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
                report.AppendLine($"Scène ouverte : {hub.path}");
                controller = FindControllerInLoadedScenes();
            }

            if (controller == null)
            {
                report.AppendLine("ÉCHEC : GachaAnimationController introuvable.");
                WriteReport(report);
                Debug.LogError(
                    "[GachaDecheanceIntegrationBuilder] GachaAnimationController introuvable.");
                return;
            }

            report.AppendLine(
                $"Contrôleur : '{controller.name}' (scène '{controller.gameObject.scene.path}').");

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty revealProp = so.FindProperty("revealScene");
            GameObject revealScene = revealProp != null
                ? revealProp.objectReferenceValue as GameObject
                : null;

            if (revealScene == null)
            {
                report.AppendLine("ÉCHEC : revealScene null sur le contrôleur.");
                WriteReport(report);
                Debug.LogError(
                    "[GachaDecheanceIntegrationBuilder] revealScene manquant.");
                return;
            }

            report.AppendLine($"revealScene : '{revealScene.name}'.");

            // ── Stage sous revealScene ──
            GameObject stageGo = FindChildByName(revealScene.transform, StageName);
            if (stageGo == null)
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null)
                {
                    report.AppendLine($"ÉCHEC : prefab introuvable → {PrefabPath}");
                    WriteReport(report);
                    Debug.LogError(
                        $"[GachaDecheanceIntegrationBuilder] Prefab manquant : {PrefabPath}");
                    return;
                }

                stageGo = PrefabUtility.InstantiatePrefab(
                    prefab, revealScene.transform) as GameObject;
                if (stageGo == null)
                {
                    report.AppendLine("ÉCHEC : InstantiatePrefab a renvoyé null.");
                    WriteReport(report);
                    return;
                }

                stageGo.name = StageName;
                StretchFull(stageGo.GetComponent<RectTransform>());
                PlaceAfterArtwork(so, stageGo.transform, report);
                stageGo.SetActive(false);
                anyChange = true;
                report.AppendLine(
                    $"Stage : instancié sous revealScene (inactif) → {PrefabPath}");
            }
            else
            {
                report.AppendLine("Stage : déjà présent (idempotent).");
                if (PlaceAfterArtwork(so, stageGo.transform, report))
                    anyChange = true;
                if (stageGo.activeSelf)
                {
                    stageGo.SetActive(false);
                    anyChange = true;
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
                report.AppendLine("Driver : ArtworkTransitionDriver OK.");

            // ── Wire SerializedObject (artworkDriver + artworkStageRoot uniquement) ──
            SerializedProperty driverProp = so.FindProperty("artworkDriver");
            SerializedProperty stageRootProp = so.FindProperty("artworkStageRoot");
            bool wireChanged = false;

            if (driverProp != null)
            {
                if (driverProp.objectReferenceValue != driver)
                {
                    driverProp.objectReferenceValue = driver;
                    wireChanged = true;
                    anyChange = true;
                    report.AppendLine("Wire : artworkDriver ← stage Driver.");
                }
                else
                {
                    report.AppendLine("Wire : artworkDriver déjà OK.");
                }
            }
            else
            {
                report.AppendLine(
                    "Wire : propriété artworkDriver absente (commit code AW2 pas encore appliqué).");
            }

            if (stageRootProp != null)
            {
                if (stageRootProp.objectReferenceValue != stageGo)
                {
                    stageRootProp.objectReferenceValue = stageGo;
                    wireChanged = true;
                    anyChange = true;
                    report.AppendLine("Wire : artworkStageRoot ← stage GO.");
                }
                else
                {
                    report.AppendLine("Wire : artworkStageRoot déjà OK.");
                }
            }
            else
            {
                report.AppendLine(
                    "Wire : propriété artworkStageRoot absente (commit code AW2 pas encore appliqué).");
            }

            if (wireChanged)
                so.ApplyModifiedPropertiesWithoutUndo();

            // ── Purge composant placeholder (type peut déjà être absent du code) ──
            if (DestroyComponentByTypeName(controller.gameObject, "GachaPrimeBurnPlayer", report))
                anyChange = true;
            else
                report.AppendLine("Purge : GachaPrimeBurnPlayer absent (OK).");

            // Nettoie d'éventuelles refs sérialisées legacy si encore présentes.
            ClearLegacyBurnFields(so, report);

            // ── Destroy underlay / fx dédiés ──
            Scene scene = controller.gameObject.scene;
            if (DestroyNamedInScene(scene, BurnUnderlayName, report))
                anyChange = true;
            if (DestroyNamedInScene(scene, BurnFxName, report))
                anyChange = true;

            // ── Audio burn (sting + ignite) — remplace revealsound provisoire ──
            if (EnsureBurnAudioClips(report))
                anyChange = true;

            // ── Dirty + save Hub (uniquement si changement) ──
            if (anyChange)
            {
                EditorUtility.SetDirty(controller);
                if (stageGo != null)
                    EditorUtility.SetDirty(stageGo);

                EditorSceneManager.MarkSceneDirty(scene);
                if (scene.path == HubScenePath || scene.name == "Hub")
                {
                    EditorSceneManager.SaveScene(scene);
                    report.AppendLine($"Scène sauvegardée : {scene.path}");
                }
                else
                {
                    report.AppendLine(
                        $"Scène marquée dirty (non Hub) : {scene.path} — save manuelle si besoin.");
                }

                AssetDatabase.SaveAssets();
            }

            report.AppendLine();
            if (anyChange)
                report.AppendLine("Résultat : modifications appliquées.");
            else
                report.AppendLine("Résultat : aucune modification (idempotent).");
            report.AppendLine();
            report.AppendLine(
                "Note M1 : rapport AW2 gacha — re-run ce builder après sync si besoin. " +
                "Ascension cérémonie = menu « Chez Arthur/Cérémonie/Câbler l'ascension (AW3) ».");
            report.AppendLine("═══════════════════════════════════════════");

            WriteReport(report);
            Debug.Log(
                $"[GachaDecheanceIntegrationBuilder] OK — rapport : {ReportRelPath}" +
                (anyChange ? " (changements)" : " (idempotent)"));
        }

        // ═══════════════════════════════════════════
        // RÉSOLUTION CONTRÔLEUR / SCÈNE
        // ═══════════════════════════════════════════

        private static GachaAnimationController FindControllerPreferOpenScenes(
            StringBuilder report)
        {
            GachaAnimationController found = FindControllerInLoadedScenes();
            if (found != null)
            {
                report.AppendLine(
                    $"Contrôleur trouvé en scènes ouvertes : '{found.name}'.");
                return found;
            }

            report.AppendLine("Aucun GachaAnimationController en scènes ouvertes.");
            return null;
        }

        private static GachaAnimationController FindControllerInLoadedScenes()
        {
            GachaAnimationController[] all =
                Object.FindObjectsOfType<GachaAnimationController>(true);
            if (all == null || all.Length == 0)
                return null;

            // Préfère Hub si plusieurs.
            for (int i = 0; i < all.Length; i++)
            {
                GachaAnimationController c = all[i];
                if (c == null)
                    continue;
                Scene s = c.gameObject.scene;
                if (s.IsValid() && (s.path == HubScenePath || s.name == "Hub"))
                    return c;
            }

            return all[0];
        }

        // ═══════════════════════════════════════════
        // STAGE / HIÉRARCHIE
        // ═══════════════════════════════════════════

        private static GameObject FindChildByName(Transform parent, string name)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name == name)
                    return child.gameObject;
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

        /// <summary>
        /// Place le stage juste après l'artwork (couvre l'artwork).
        /// Retourne true si l'index sibling a changé.
        /// </summary>
        private static bool PlaceAfterArtwork(
            SerializedObject controllerSo,
            Transform stageTf,
            StringBuilder report)
        {
            if (stageTf == null || stageTf.parent == null)
                return false;

            Transform artworkTf = ResolveArtworkTransform(controllerSo);
            if (artworkTf == null || artworkTf.parent != stageTf.parent)
            {
                report.AppendLine(
                    "Sibling : artwork introuvable / parent différent — index non forcé.");
                return false;
            }

            int target = artworkTf.GetSiblingIndex() + 1;
            // Si le stage est avant l'artwork, GetSiblingIndex de artwork
            // ne compte pas encore le déplacement — ajuster après move.
            int before = stageTf.GetSiblingIndex();
            if (before < artworkTf.GetSiblingIndex())
                target = artworkTf.GetSiblingIndex(); // après move, artwork descend d'1
            else
                target = artworkTf.GetSiblingIndex() + 1;

            if (before == target)
            {
                report.AppendLine(
                    $"Sibling : déjà après artwork (index {before}).");
                return false;
            }

            stageTf.SetSiblingIndex(target);
            report.AppendLine(
                $"Sibling : stage index {before} → {stageTf.GetSiblingIndex()} (après artwork).");
            return true;
        }

        private static Transform ResolveArtworkTransform(SerializedObject so)
        {
            SerializedProperty rawProp = so.FindProperty("artworkRawImage");
            if (rawProp != null && rawProp.objectReferenceValue is RawImage raw && raw != null)
                return raw.transform;

            SerializedProperty viewProp = so.FindProperty("artworkView");
            if (viewProp != null && viewProp.objectReferenceValue is Component view && view != null)
                return view.transform;

            return null;
        }

        // ═══════════════════════════════════════════
        // PURGE / CONFIG
        // ═══════════════════════════════════════════

        private static bool DestroyNamedInScene(
            Scene scene, string objectName, StringBuilder report)
        {
            if (!scene.IsValid())
                return false;

            bool destroyed = false;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (DestroyNamedRecursive(roots[i].transform, objectName))
                    destroyed = true;
            }

            if (destroyed)
                report.AppendLine($"Purge : '{objectName}' détruit.");
            else
                report.AppendLine($"Purge : '{objectName}' absent (OK).");

            return destroyed;
        }

        private static bool DestroyNamedRecursive(Transform root, string objectName)
        {
            bool any = false;
            // Collecte d'abord (évite mutation pendant itération).
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (DestroyNamedRecursive(child, objectName))
                    any = true;
            }

            if (root.name == objectName)
            {
                Object.DestroyImmediate(root.gameObject);
                return true;
            }

            return any;
        }

        /// <summary>
        /// Retire un composant par nom de type (sans référence compile-time au type purgé).
        /// </summary>
        private static bool DestroyComponentByTypeName(
            GameObject go,
            string typeName,
            StringBuilder report)
        {
            if (go == null || string.IsNullOrEmpty(typeName))
                return false;

            Component[] comps = go.GetComponents<Component>();
            bool destroyed = false;
            for (int i = 0; i < comps.Length; i++)
            {
                Component c = comps[i];
                if (c == null)
                    continue;
                if (c.GetType().Name != typeName)
                    continue;

                Object.DestroyImmediate(c);
                destroyed = true;
            }

            if (destroyed)
                report.AppendLine($"Purge : {typeName} retiré du contrôleur.");

            return destroyed;
        }

        private static void ClearLegacyBurnFields(SerializedObject so, StringBuilder report)
        {
            if (so == null)
                return;

            string[] legacy =
            {
                "primeBurnPlayer",
                "gachaBurnMaterial",
                "gachaBurnEmberMaterial",
                "gachaBurnEmberSprite",
                "gachaBurnClip",
            };

            bool cleared = false;
            for (int i = 0; i < legacy.Length; i++)
            {
                SerializedProperty p = so.FindProperty(legacy[i]);
                if (p == null)
                    continue;
                if (p.propertyType == SerializedPropertyType.ObjectReference
                    && p.objectReferenceValue != null)
                {
                    p.objectReferenceValue = null;
                    cleared = true;
                }
            }

            if (cleared)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                report.AppendLine("Purge : champs sérialisés legacy burn nullifiés.");
            }
        }

        /// <summary>
        /// Branche sfx_gacha_burn sur sting + ignite.
        /// Remplace revealsound s'il avait été mis en provisoire AW2.
        /// </summary>
        private static bool EnsureBurnAudioClips(StringBuilder report)
        {
            ArtworkTransitionConfig config =
                AssetDatabase.LoadAssetAtPath<ArtworkTransitionConfig>(ConfigPath);
            if (config == null)
            {
                report.AppendLine($"Config : introuvable → {ConfigPath}");
                return false;
            }

            AudioClip burn = AssetDatabase.LoadAssetAtPath<AudioClip>(BurnSoundPath);
            if (burn == null)
            {
                report.AppendLine($"Config : burn clip introuvable → {BurnSoundPath}");
                return false;
            }

            AudioClip reveal = AssetDatabase.LoadAssetAtPath<AudioClip>(RevealSoundPath);
            bool changed = false;

            bool stingIsReveal = config.stingClip != null
                && reveal != null
                && config.stingClip == reveal;
            if (config.stingClip == null || stingIsReveal)
            {
                config.stingClip = burn;
                changed = true;
                report.AppendLine(
                    $"Config : stingClip ← {BurnSoundPath} (burn gacha, remplace revealsound).");
            }
            else
            {
                report.AppendLine($"Config : stingClip déjà assigné ({config.stingClip.name}).");
            }

            bool igniteIsReveal = config.igniteClip != null
                && reveal != null
                && config.igniteClip == reveal;
            if (config.igniteClip == null || igniteIsReveal)
            {
                config.igniteClip = burn;
                changed = true;
                report.AppendLine(
                    $"Config : igniteClip ← {BurnSoundPath} (burn gacha, remplace revealsound).");
            }
            else
            {
                report.AppendLine($"Config : igniteClip déjà assigné ({config.igniteClip.name}).");
            }

            if (changed)
                EditorUtility.SetDirty(config);

            return changed;
        }

        // ═══════════════════════════════════════════
        // IO
        // ═══════════════════════════════════════════

        private static void WriteReport(StringBuilder report)
        {
            string auditsRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, "gacha_decheance_integration.txt");
            File.WriteAllText(fullPath, report.ToString(), Encoding.UTF8);
            Debug.Log($"[GachaDecheanceIntegrationBuilder] Rapport écrit : {fullPath}");
        }
    }
}
#endif
