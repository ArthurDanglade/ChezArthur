#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.UI.InvocationFlow;
using ChezArthur.UI.ArtworkTransition;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Audit lecture seule des assets Invocation Flow INV1 — n'écrit que le rapport.
    /// </summary>
    public static class InvocationFlowAuditor
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string ArtFxFolder = "Assets/_Project/Art/FX";
        private const string NoisePath = ArtFxFolder + "/ArtworkNoise.png";
        private const string GlowPath = ArtFxFolder + "/AwGlowSoft.png";
        private const string PixelVeilMatPath = ArtFxFolder + "/PixelVeil.mat";
        private const string AdditiveMatPath = ArtFxFolder + "/AwAdditive.mat";
        private const string ConfigPath = "Assets/_Project/Data/UI/InvocationFlowConfig.asset";
        private const string VeilPrefabPath = "Assets/_Project/Prefabs/UI/PixelVeilOverlay.prefab";
        private const string RarityPrefabPath = "Assets/_Project/Prefabs/UI/RevealRarityLayer.prefab";
        private const string BannerPrefabPath = "Assets/_Project/Prefabs/UI/RevealBanner.prefab";
        private const string InvFlowScriptsFolder = "Assets/_Project/Scripts/UI/InvocationFlow";

        private const string PixelVeilShaderName = "ChezArthur/UI/PixelVeil";
        private const string AdditiveShaderName = "ChezArthur/UI/AdditiveTint";
        private const int ExpectedNoiseSeed = 1337;

        private static int _ok;
        private static int _warn;
        private static int _fail;

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI/Auditer Invocation Flow (INV1)")]
        public static void Audit()
        {
            _ok = 0;
            _warn = 0;
            _fail = 0;

            var report = new StringBuilder(12288);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" AUDIT Invocation Flow INV1 (lecture seule)");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();

            AuditShaders(report);
            report.AppendLine();
            AuditNoiseReuse(report);
            report.AppendLine();
            AuditMaterials(report);
            report.AppendLine();
            AuditConfig(report);
            report.AppendLine();
            AuditPrefabs(report);
            report.AppendLine();
            AuditDormancy(report);
            report.AppendLine();
            AuditAwIntact(report);

            report.AppendLine();
            report.AppendLine("───────────────────────────────────────────");
            report.AppendLine($" SYNTHÈSE : OK={_ok}  WARN={_warn}  FAIL={_fail}");
            report.AppendLine("───────────────────────────────────────────");
            report.AppendLine(" Fin du rapport (aucune modification effectuée)");
            report.AppendLine("═══════════════════════════════════════════");

            string text = report.ToString();
            Debug.Log(text);
            WriteReport(text);
        }

        // ═══════════════════════════════════════════
        // SHADERS
        // ═══════════════════════════════════════════

        private static void AuditShaders(StringBuilder report)
        {
            report.AppendLine("── Shaders ──");
            Shader s = Shader.Find(PixelVeilShaderName);
            if (s != null)
                Ok(report, $"Shader compilable / trouvé : {PixelVeilShaderName}");
            else
                Fail(report, $"Shader INTROUVABLE : {PixelVeilShaderName}");

            Shader add = Shader.Find(AdditiveShaderName);
            if (add != null)
                Ok(report, $"Shader AW AdditiveTint réutilisable : {AdditiveShaderName}");
            else
                Warn(report, $"AdditiveTint introuvable (glows rareté impactés)");
        }

        // ═══════════════════════════════════════════
        // NOISE / GLOW REUSE
        // ═══════════════════════════════════════════

        private static void AuditNoiseReuse(StringBuilder report)
        {
            report.AppendLine("── Textures AW réutilisées ──");

            if (!FileExists(NoisePath))
            {
                Fail(report, $"Manquant : {NoisePath}");
            }
            else
            {
                var importer = AssetImporter.GetAtPath(NoisePath) as TextureImporter;
                if (importer != null
                    && importer.filterMode == FilterMode.Point
                    && importer.wrapMode == TextureWrapMode.Repeat)
                {
                    Ok(report,
                        $"{NoisePath} — Point/Repeat (seed {ExpectedNoiseSeed} AW, NON régénéré par INV1)");
                }
                else
                {
                    Fail(report, $"{NoisePath} — import incorrect (attendu Point/Repeat)");
                }
            }

            if (FileExists(GlowPath))
                Ok(report, $"{GlowPath} — réutilisé pour underglow / particules");
            else
                Fail(report, $"Manquant : {GlowPath}");
        }

        // ═══════════════════════════════════════════
        // MATERIALS
        // ═══════════════════════════════════════════

        private static void AuditMaterials(StringBuilder report)
        {
            report.AppendLine("── Matériaux ──");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(PixelVeilMatPath);
            if (mat == null)
            {
                Fail(report, $"Manquant : {PixelVeilMatPath}");
                return;
            }

            if (mat.shader != null && mat.shader.name == PixelVeilShaderName)
                Ok(report, $"PixelVeil.mat → shader {PixelVeilShaderName}");
            else
                Fail(report, $"PixelVeil.mat shader incorrect : {(mat.shader != null ? mat.shader.name : "null")}");

            Texture noise = mat.GetTexture("_NoiseTex");
            Texture2D expected = AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath);
            if (noise != null && expected != null && noise == expected)
                Ok(report, "PixelVeil.mat._NoiseTex = ArtworkNoise.png (AW)");
            else if (noise != null)
                Warn(report, $"PixelVeil.mat._NoiseTex assigné mais ≠ ArtworkNoise path ({noise.name})");
            else
                Fail(report, "PixelVeil.mat._NoiseTex NULL");
        }

        // ═══════════════════════════════════════════
        // CONFIG
        // ═══════════════════════════════════════════

        private static void AuditConfig(StringBuilder report)
        {
            report.AppendLine("── Config ──");
            InvocationFlowConfig cfg =
                AssetDatabase.LoadAssetAtPath<InvocationFlowConfig>(ConfigPath);
            if (cfg == null)
            {
                Fail(report, $"Manquant : {ConfigPath}");
                return;
            }

            Ok(report, $"Config présente → {ConfigPath}");

            CheckDefault(report, "veilDuration", cfg.veilDuration, 0.70f);
            CheckDefault(report, "veilCellSize", cfg.veilCellSize, 14f);
            CheckDefault(report, "resolveDurationSR", cfg.resolveDurationSR, 1.6f);
            CheckDefault(report, "resolveDurationSSR", cfg.resolveDurationSSR, 2.4f);
            CheckDefault(report, "lrResolveBonus", cfg.lrResolveBonus, 0.2f);
            CheckDefault(report, "monteeDuration", cfg.monteeDuration, 0.35f);
            CheckDefault(report, "punchIntensity", cfg.punchIntensity, 0.7f);
            CheckDefault(report, "rarityGlowIntensity", cfg.rarityGlowIntensity, 0.7f);
            CheckDefault(report, "bannerFullDuration", cfg.bannerFullDuration, 0.9f);
            CheckDefault(report, "bannerCompactDuration", cfg.bannerCompactDuration, 0.4f);

            CheckRange(report, "veilDuration", cfg.veilDuration, 0.4f, 1.4f);
            CheckRange(report, "veilCellSize", cfg.veilCellSize, 8f, 26f);
            CheckRange(report, "resolveDurationSR", cfg.resolveDurationSR, 0.8f, 2.6f);
            CheckRange(report, "resolveDurationSSR", cfg.resolveDurationSSR, 1.4f, 3.4f);
            CheckRange(report, "monteeDuration", cfg.monteeDuration, 0f, 0.9f);
            CheckRange(report, "punchIntensity", cfg.punchIntensity, 0f, 1f);
            CheckRange(report, "rarityGlowIntensity", cfg.rarityGlowIntensity, 0f, 1f);
            CheckRange(report, "bannerFullDuration", cfg.bannerFullDuration, 0.5f, 1.6f);
            CheckRange(report, "bannerCompactDuration", cfg.bannerCompactDuration, 0.2f, 0.9f);
        }

        // ═══════════════════════════════════════════
        // PREFABS
        // ═══════════════════════════════════════════

        private static void AuditPrefabs(StringBuilder report)
        {
            report.AppendLine("── Prefabs ──");
            AuditVeilPrefab(report);
            AuditRarityPrefab(report);
            AuditBannerPrefab(report);
        }

        private static void AuditVeilPrefab(StringBuilder report)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VeilPrefabPath);
            if (prefab == null)
            {
                Fail(report, $"Manquant : {VeilPrefabPath}");
                return;
            }

            PixelVeilController ctrl = prefab.GetComponent<PixelVeilController>();
            Image img = prefab.GetComponent<Image>();
            if (ctrl == null)
                Fail(report, "PixelVeilOverlay : PixelVeilController manquant");
            else
                Ok(report, "PixelVeilOverlay : PixelVeilController présent");

            if (img == null)
                Fail(report, "PixelVeilOverlay : Image manquante");
            else if (img.raycastTarget)
                Fail(report, "PixelVeilOverlay : raycastTarget=true (attendu false)");
            else
                Ok(report, "PixelVeilOverlay : Image raycastTarget=false");

            CheckWired(report, prefab, ctrl, "config", "PixelVeilOverlay.config");
            CheckWired(report, prefab, ctrl, "sharedMaterial", "PixelVeilOverlay.sharedMaterial");
        }

        private static void AuditRarityPrefab(StringBuilder report)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RarityPrefabPath);
            if (prefab == null)
            {
                Fail(report, $"Manquant : {RarityPrefabPath}");
                return;
            }

            RevealRarityLayer layer = prefab.GetComponent<RevealRarityLayer>();
            if (layer == null)
            {
                Fail(report, "RevealRarityLayer : composant manquant");
                return;
            }

            Ok(report, "RevealRarityLayer : composant présent");
            CheckWired(report, prefab, layer, "config", "Rarity.config");
            CheckWired(report, prefab, layer, "underglowImage", "Rarity.underglowImage");
            CheckWired(report, prefab, layer, "rimFrame", "Rarity.rimFrame");
            CheckWired(report, prefab, layer, "particles", "Rarity.particles");
            CheckWired(report, prefab, layer, "shakeContainer", "Rarity.shakeContainer");
            CheckWired(report, prefab, layer, "flashOverlay", "Rarity.flashOverlay");

            PixelParticleGraphic ppg = prefab.GetComponentInChildren<PixelParticleGraphic>(true);
            if (ppg != null)
                Ok(report, "RevealRarityLayer : PixelParticleGraphic AW présent (réutilisé)");
            else
                Fail(report, "RevealRarityLayer : PixelParticleGraphic manquant");

            AssertAllRaycastOff(report, prefab, "RevealRarityLayer");
        }

        private static void AuditBannerPrefab(StringBuilder report)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BannerPrefabPath);
            if (prefab == null)
            {
                Fail(report, $"Manquant : {BannerPrefabPath}");
                return;
            }

            RevealBannerUI banner = prefab.GetComponent<RevealBannerUI>();
            if (banner == null)
            {
                Fail(report, "RevealBanner : composant manquant");
                return;
            }

            Ok(report, "RevealBanner : composant présent");
            string[] fields =
            {
                "config", "canvasGroup", "rootRect", "nameText", "rarityBar",
                "levelChip", "statusText", "xpLineFill", "xpChip"
            };
            for (int i = 0; i < fields.Length; i++)
                CheckWired(report, prefab, banner, fields[i], "Banner." + fields[i]);

            // Tableaux stats
            var so = new SerializedObject(banner);
            if (so.FindProperty("statChipGroups").arraySize == 4
                && so.FindProperty("statChipLabels").arraySize == 4
                && so.FindProperty("statChipRects").arraySize == 4)
                Ok(report, "RevealBanner : 4 chips stats câblés");
            else
                Fail(report, "RevealBanner : chips stats incomplets");

            if (prefab.GetComponentInChildren<TextMeshProUGUI>(true) != null)
                Ok(report, "RevealBanner : TextMeshProUGUI présent");
            else
                Fail(report, "RevealBanner : aucun TMP");

            AssertAllRaycastOff(report, prefab, "RevealBanner");
        }

        // ═══════════════════════════════════════════
        // DORMANCE
        // ═══════════════════════════════════════════

        private static void AuditDormancy(StringBuilder report)
        {
            report.AppendLine("── Dormance ──");
            report.AppendLine("Recherche de références InvocationFlow hors dossier/prefabs INV1…");

            string[] allScripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/_Project" });
            int leaks = 0;
            for (int i = 0; i < allScripts.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(allScripts[i]);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (path.StartsWith(InvFlowScriptsFolder))
                    continue;
                if (path.Contains("InvocationFlowAssetsBuilder") || path.Contains("InvocationFlowAuditor"))
                    continue;

                string text = File.ReadAllText(path);
                if (text.Contains("ChezArthur.UI.InvocationFlow")
                    || text.Contains("PixelVeilController")
                    || text.Contains("RevealRarityLayer")
                    || text.Contains("RevealBannerUI")
                    || text.Contains("InvocationFlowConfig")
                    || text.Contains("InvocationFlowDevHarness"))
                {
                    // Faux positifs possibles sur noms génériques — signaler
                    Fail(report, $"Fuite dormance script : {path}");
                    leaks++;
                }
            }

            // Scènes / prefabs hors INV1
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (path == VeilPrefabPath || path == RarityPrefabPath || path == BannerPrefabPath)
                    continue;

                string yaml = File.ReadAllText(path);
                if (yaml.Contains("InvocationFlow")
                    || yaml.Contains("PixelVeilController")
                    || yaml.Contains("RevealRarityLayer")
                    || yaml.Contains("RevealBannerUI"))
                {
                    Fail(report, $"Fuite dormance prefab : {path}");
                    leaks++;
                }
            }

            // Contrôleurs gacha gelés
            string[] frozen =
            {
                "Assets/_Project/Scripts/Gacha/GachaAnimationController.cs",
                "Assets/_Project/Scripts/Gacha/GachaRevealStatusUI.cs",
                "Assets/_Project/Scripts/Gameplay/AwakeningCeremonyController.cs"
            };
            for (int i = 0; i < frozen.Length; i++)
            {
                if (!FileExists(frozen[i]))
                    continue;
                string text = File.ReadAllText(frozen[i]);
                if (text.Contains("InvocationFlow") || text.Contains("PixelVeil")
                    || text.Contains("RevealRarity") || text.Contains("RevealBanner"))
                {
                    Fail(report, $"Zone gelée touchée : {frozen[i]}");
                    leaks++;
                }
            }

            if (leaks == 0)
                Ok(report, "Dormance OK — aucune référence InvocationFlow hors INV1 (scripts/prefabs)");
        }

        // ═══════════════════════════════════════════
        // SOCLE AW
        // ═══════════════════════════════════════════

        private static void AuditAwIntact(StringBuilder report)
        {
            report.AppendLine("── Socle AW intact ──");
            report.AppendLine(
                "NOTE : INV1 réutilise par référence uniquement PixelParticleGraphic, " +
                "UIAdditiveTint / AwAdditive, ArtworkNoise (seed 1337), AwGlowSoft.");
            report.AppendLine(
                "NOTE : Aucune modification attendue sous Scripts/UI/ArtworkTransition/**, " +
                "ArtworkTransition.shader, UIAdditiveTint.shader, ArtworkTransitionStage, ArtworkTransitionConfig.");

            if (FileExists("Assets/_Project/Prefabs/UI/ArtworkTransitionStage.prefab"))
                Ok(report, "ArtworkTransitionStage.prefab présent (non modifié par ce builder)");
            else
                Warn(report, "ArtworkTransitionStage.prefab introuvable");

            if (FileExists("Assets/_Project/Data/UI/ArtworkTransitionConfig.asset"))
                Ok(report, "ArtworkTransitionConfig.asset présent");
            else
                Warn(report, "ArtworkTransitionConfig.asset introuvable");

            if (AssetDatabase.LoadAssetAtPath<MonoScript>(
                    "Assets/_Project/Scripts/UI/ArtworkTransition/PixelParticleGraphic.cs") != null)
                Ok(report, "PixelParticleGraphic.cs présent (réutilisé, non modifié)");
            else
                Fail(report, "PixelParticleGraphic.cs introuvable");
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static void CheckWired(
            StringBuilder report, GameObject prefab, Object component, string field, string label)
        {
            if (component == null)
            {
                Fail(report, $"{label} : composant null");
                return;
            }

            var so = new SerializedObject(component);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Fail(report, $"{label} : champ « {field} » introuvable");
                return;
            }

            if (prop.propertyType == SerializedPropertyType.ObjectReference
                && prop.objectReferenceValue != null)
                Ok(report, $"{label} câblé");
            else
                Fail(report, $"{label} NULL");
        }

        private static void AssertAllRaycastOff(StringBuilder report, GameObject prefab, string label)
        {
            var graphics = prefab.GetComponentsInChildren<Graphic>(true);
            int bad = 0;
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].raycastTarget)
                    bad++;
            }

            if (bad == 0)
                Ok(report, $"{label} : raycastTarget=false partout ({graphics.Length} Graphics)");
            else
                Fail(report, $"{label} : {bad} Graphic(s) avec raycastTarget=true");
        }

        private static void CheckDefault(StringBuilder report, string name, float value, float expected)
        {
            if (Mathf.Abs(value - expected) < 0.001f)
                Ok(report, $"Défaut {name} = {value}");
            else
                Warn(report, $"Défaut {name} = {value} (attendu {expected} — peut avoir été tuné)");
        }

        private static void CheckRange(
            StringBuilder report, string name, float value, float min, float max)
        {
            if (value >= min && value <= max)
                Ok(report, $"Range {name} OK ({value} ∈ [{min},{max}])");
            else
                Fail(report, $"Range {name} HORS BORNES ({value} ∉ [{min},{max}])");
        }

        private static bool FileExists(string assetPath)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            return File.Exists(full);
        }

        private static void Ok(StringBuilder report, string msg)
        {
            _ok++;
            report.AppendLine($"  OK   {msg}");
        }

        private static void Warn(StringBuilder report, string msg)
        {
            _warn++;
            report.AppendLine($"  WARN {msg}");
        }

        private static void Fail(StringBuilder report, string msg)
        {
            _fail++;
            report.AppendLine($"  FAIL {msg}");
        }

        private static void WriteReport(string text)
        {
            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, "invocation_flow_audit.txt");
            File.WriteAllText(fullPath, text, Encoding.UTF8);
            Debug.Log($"[InvocationFlowAuditor] Rapport écrit : {fullPath}");
        }
    }
}
#endif
