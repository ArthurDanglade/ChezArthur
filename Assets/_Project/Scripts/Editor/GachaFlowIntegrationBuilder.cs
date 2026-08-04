#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gacha;
using ChezArthur.UI.InvocationFlow;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Câble le polish invocation INV2 sous revealScene (Hub).
    /// Idempotent — second run sans changement.
    /// </summary>
    public static class GachaFlowIntegrationBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string HubScenePath = "Assets/_Project/Scenes/Hub.unity";
        private const string ConfigPath = "Assets/_Project/Data/UI/InvocationFlowConfig.asset";
        private const string VeilPrefabPath = "Assets/_Project/Prefabs/UI/PixelVeilOverlay.prefab";
        private const string RarityPrefabPath = "Assets/_Project/Prefabs/UI/RevealRarityLayer.prefab";
        private const string BannerPrefabPath = "Assets/_Project/Prefabs/UI/RevealBanner.prefab";
        private const string ReportRelPath = "Audits/gacha_flow_integration.txt";

        private const string VeilName = "PixelVeilOverlay";
        private const string RarityName = "RevealRarityLayer";
        private const string BannerName = "RevealBanner";
        private const string SkipAllName = "SkipAllButton";
        private const string SmokeName = "SmokeTransition";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Gacha/Câbler le polish invocation (INV2)")]
        public static void BuildMenu()
        {
            Build();
        }

        /// <summary>Point d'entrée idempotent (MenuItem + appel scripté).</summary>
        public static void Build()
        {
            var report = new StringBuilder(4096);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" Gacha — câblage polish invocation INV2");
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
                    "[GachaFlowIntegrationBuilder] GachaAnimationController introuvable.");
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
                    "[GachaFlowIntegrationBuilder] revealScene manquant.");
                return;
            }

            report.AppendLine($"revealScene : '{revealScene.name}'.");

            InvocationFlowConfig config =
                AssetDatabase.LoadAssetAtPath<InvocationFlowConfig>(ConfigPath);
            if (config == null)
            {
                report.AppendLine($"ÉCHEC : config introuvable → {ConfigPath}");
                WriteReport(report);
                Debug.LogError(
                    $"[GachaFlowIntegrationBuilder] Config manquante : {ConfigPath}");
                return;
            }

            report.AppendLine($"flowConfig : {ConfigPath}");

            // ── Prefabs sous revealScene ──
            GameObject veilGo = EnsurePrefabChild(
                revealScene.transform, VeilName, VeilPrefabPath, report, ref anyChange);
            GameObject rarityGo = EnsurePrefabChild(
                revealScene.transform, RarityName, RarityPrefabPath, report, ref anyChange);
            GameObject bannerGo = EnsurePrefabChild(
                revealScene.transform, BannerName, BannerPrefabPath, report, ref anyChange);

            // Hiérarchie : rarity au-dessus artwork, veil au-dessus du reveal, skip-all au-dessus du veil.
            if (PlaceRarityAboveArtwork(so, rarityGo, report))
                anyChange = true;
            if (PlaceVeilNearTop(revealScene.transform, veilGo, report))
                anyChange = true;

            // ── Skip-all (haut-droite, discret, inactif) ──
            GameObject skipGo = EnsureSkipAllButton(revealScene.transform, report, ref anyChange);
            if (skipGo != null && veilGo != null)
            {
                // Skip-all au-dessus du voile.
                int veilIdx = veilGo.transform.GetSiblingIndex();
                if (skipGo.transform.GetSiblingIndex() < veilIdx)
                {
                    skipGo.transform.SetSiblingIndex(veilIdx + 1);
                    anyChange = true;
                    report.AppendLine("Sibling : SkipAllButton placé au-dessus du voile.");
                }
            }

            // ── Purge SmokeTransition sous revealScene ──
            if (DestroySmokeUnderReveal(revealScene.transform, report))
                anyChange = true;

            // ── Wire champs INV2 ──
            PixelVeilController veil =
                veilGo != null ? veilGo.GetComponent<PixelVeilController>() : null;
            RevealRarityLayer rarity =
                rarityGo != null ? rarityGo.GetComponent<RevealRarityLayer>() : null;
            RevealBannerUI banner =
                bannerGo != null ? bannerGo.GetComponent<RevealBannerUI>() : null;
            Button skipBtn =
                skipGo != null ? skipGo.GetComponent<Button>() : null;

            bool wireChanged = false;
            wireChanged |= SetRef(so, "flowConfig", config, report, "flowConfig");
            wireChanged |= SetRef(so, "pixelVeil", veil, report, "pixelVeil");
            wireChanged |= SetRef(so, "rarityLayer", rarity, report, "rarityLayer");
            wireChanged |= SetRef(so, "revealBanner", banner, report, "revealBanner");
            wireChanged |= SetRef(so, "skipAllButton", skipBtn, report, "skipAllButton");

            // Nettoyage champs legacy si encore présents (YAML scène).
            wireChanged |= ClearRefIfPresent(so, "smokeTransition", report);
            wireChanged |= ClearRefIfPresent(so, "revealStatusUi", report);
            wireChanged |= ClearRefIfPresent(so, "revealXpProgressClip", report);
            wireChanged |= ClearRefIfPresent(so, "revealLevelUpClip", report);
            wireChanged |= ClearRefIfPresent(so, "revealStatTickClip", report);
            wireChanged |= ClearRefIfPresent(so, "revealMaxConfirmClip", report);
            if (ClearFloatIfPresent(so, "revealResolveDuration", report))
                wireChanged = true;

            if (wireChanged)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                anyChange = true;
            }

            Scene scene = controller.gameObject.scene;
            if (anyChange)
            {
                EditorUtility.SetDirty(controller);
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
            report.AppendLine("═══════════════════════════════════════════");

            WriteReport(report);
            Debug.Log(
                $"[GachaFlowIntegrationBuilder] OK — rapport : {ReportRelPath}" +
                (anyChange ? " (changements)" : " (idempotent)"));
        }

        // ═══════════════════════════════════════════
        // INSTANCIATION
        // ═══════════════════════════════════════════

        private static GameObject EnsurePrefabChild(
            Transform parent,
            string childName,
            string prefabPath,
            StringBuilder report,
            ref bool anyChange)
        {
            GameObject existing = FindChildByName(parent, childName);
            if (existing != null)
            {
                report.AppendLine($"{childName} : déjà présent (idempotent).");
                return existing;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                report.AppendLine($"ÉCHEC : prefab introuvable → {prefabPath}");
                Debug.LogError(
                    $"[GachaFlowIntegrationBuilder] Prefab manquant : {prefabPath}");
                return null;
            }

            GameObject go = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (go == null)
            {
                report.AppendLine($"ÉCHEC : InstantiatePrefab null → {prefabPath}");
                return null;
            }

            go.name = childName;
            StretchFull(go.GetComponent<RectTransform>());
            anyChange = true;
            report.AppendLine($"{childName} : instancié sous revealScene → {prefabPath}");
            return go;
        }

        private static GameObject EnsureSkipAllButton(
            Transform parent, StringBuilder report, ref bool anyChange)
        {
            GameObject existing = FindChildByName(parent, SkipAllName);
            if (existing != null)
            {
                report.AppendLine("SkipAllButton : déjà présent (idempotent).");
                return existing;
            }

            GameObject go = new GameObject(
                SkipAllName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(160f, 48f);
            rt.anchoredPosition = new Vector2(-24f, -24f);

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.14f, 0.55f);
            img.raycastTarget = true;

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = true;

            GameObject labelGo = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform lrt = labelGo.GetComponent<RectTransform>();
            StretchFull(lrt);
            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "Tout passer";
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 1f, 1f, 0.85f);
            tmp.raycastTarget = false;

            go.SetActive(false);
            anyChange = true;
            report.AppendLine(
                "SkipAllButton : créé (haut-droite, discret, inactif par défaut).");
            return go;
        }

        private static bool DestroySmokeUnderReveal(Transform revealRoot, StringBuilder report)
        {
            GameObject smoke = FindChildByName(revealRoot, SmokeName);
            if (smoke == null)
            {
                // Recherche récursive légère (1 niveau enfants)
                for (int i = 0; i < revealRoot.childCount; i++)
                {
                    Transform c = revealRoot.GetChild(i);
                    if (c != null && c.name == SmokeName)
                    {
                        smoke = c.gameObject;
                        break;
                    }
                }
            }

            if (smoke == null)
            {
                report.AppendLine(
                    "Purge : SmokeTransition absent sous revealScene (OK).");
                return false;
            }

            string path = GetHierarchyPath(smoke.transform);
            Object.DestroyImmediate(smoke);
            report.AppendLine($"Purge : objet retiré → {path}");
            return true;
        }

        // ═══════════════════════════════════════════
        // HIÉRARCHIE
        // ═══════════════════════════════════════════

        private static bool PlaceRarityAboveArtwork(
            SerializedObject controllerSo, GameObject rarityGo, StringBuilder report)
        {
            if (rarityGo == null)
                return false;

            Transform artworkTf = ResolveArtworkTransform(controllerSo);
            if (artworkTf == null || artworkTf.parent != rarityGo.transform.parent)
            {
                report.AppendLine(
                    "Sibling rarity : artwork introuvable / parent différent — index non forcé.");
                return false;
            }

            int target = artworkTf.GetSiblingIndex() + 1;
            int before = rarityGo.transform.GetSiblingIndex();
            if (before < artworkTf.GetSiblingIndex())
                target = artworkTf.GetSiblingIndex();
            else
                target = artworkTf.GetSiblingIndex() + 1;

            if (before == target)
            {
                report.AppendLine("Sibling rarity : déjà au-dessus de l'artwork.");
                return false;
            }

            rarityGo.transform.SetSiblingIndex(target);
            report.AppendLine(
                $"Sibling rarity : index {before} → {rarityGo.transform.GetSiblingIndex()}.");
            return true;
        }

        private static bool PlaceVeilNearTop(
            Transform revealRoot, GameObject veilGo, StringBuilder report)
        {
            if (veilGo == null || revealRoot == null)
                return false;

            // Voile presque au sommet (sous SkipAll s'il existe).
            int target = revealRoot.childCount - 1;
            GameObject skip = FindChildByName(revealRoot, SkipAllName);
            if (skip != null)
                target = Mathf.Max(0, skip.transform.GetSiblingIndex() - 1);

            int before = veilGo.transform.GetSiblingIndex();
            if (before == target)
            {
                report.AppendLine("Sibling veil : déjà en haut du reveal.");
                return false;
            }

            veilGo.transform.SetSiblingIndex(target);
            report.AppendLine(
                $"Sibling veil : index {before} → {veilGo.transform.GetSiblingIndex()}.");
            return true;
        }

        private static Transform ResolveArtworkTransform(SerializedObject controllerSo)
        {
            SerializedProperty rawProp = controllerSo.FindProperty("artworkRawImage");
            if (rawProp != null && rawProp.objectReferenceValue is RawImage raw)
                return raw.transform;

            SerializedProperty viewProp = controllerSo.FindProperty("artworkView");
            if (viewProp != null && viewProp.objectReferenceValue is Component view)
                return view.transform;

            return null;
        }

        // ═══════════════════════════════════════════
        // SERIALIZED WIRE
        // ═══════════════════════════════════════════

        private static bool SetRef(
            SerializedObject so,
            string propName,
            Object value,
            StringBuilder report,
            string label)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null)
            {
                report.AppendLine(
                    $"Wire : propriété '{propName}' absente (commit code INV2 pas encore appliqué ?).");
                return false;
            }

            if (prop.objectReferenceValue == value)
            {
                report.AppendLine($"Wire : {label} déjà OK.");
                return false;
            }

            prop.objectReferenceValue = value;
            report.AppendLine($"Wire : {label} ← {(value != null ? value.name : "null")}.");
            return true;
        }

        private static bool ClearRefIfPresent(
            SerializedObject so, string propName, StringBuilder report)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null)
                return false;
            if (prop.objectReferenceValue == null)
                return false;
            prop.objectReferenceValue = null;
            report.AppendLine($"Purge champ : {propName} → null.");
            return true;
        }

        private static bool ClearFloatIfPresent(
            SerializedObject so, string propName, StringBuilder report)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop == null)
                return false;
            // Champ code retiré : si encore en YAML Unity ignore, mais on log.
            report.AppendLine(
                $"Note : champ legacy '{propName}' — Unity purgera au reserialize si absent du script.");
            return false;
        }

        // ═══════════════════════════════════════════
        // RÉSOLUTION CONTRÔLEUR
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

        private static string GetHierarchyPath(Transform t)
        {
            if (t == null)
                return "(null)";
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }

            return path;
        }

        private static void WriteReport(StringBuilder report)
        {
            string abs = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", ReportRelPath));
            string dir = Path.GetDirectoryName(abs);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(abs, report.ToString(), Encoding.UTF8);
        }
    }
}
#endif
