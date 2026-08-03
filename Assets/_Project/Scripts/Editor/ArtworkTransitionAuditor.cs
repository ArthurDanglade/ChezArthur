#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.UI.ArtworkTransition;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Audit lecture seule des assets Transitions Artwork (AW1) — n'écrit que le rapport.
    /// </summary>
    public static class ArtworkTransitionAuditor
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string ArtFxFolder = "Assets/_Project/Art/FX";
        private const string NoisePath = ArtFxFolder + "/ArtworkNoise.png";
        private const string GlowPath = ArtFxFolder + "/AwGlowSoft.png";
        private const string RaysPath = ArtFxFolder + "/AwRays.png";
        private const string VignettePath = ArtFxFolder + "/AwVignette.png";
        private const string TransitionMatPath = ArtFxFolder + "/ArtworkTransition.mat";
        private const string AdditiveMatPath = ArtFxFolder + "/AwAdditive.mat";
        private const string ConfigPath = "Assets/_Project/Data/UI/ArtworkTransitionConfig.asset";
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/ArtworkTransitionStage.prefab";

        private const string TransitionShaderName = "ChezArthur/UI/ArtworkTransition";
        private const string AdditiveShaderName = "ChezArthur/UI/AdditiveTint";
        private const int ExpectedNoiseSeed = 1337;

        private static int _ok;
        private static int _warn;
        private static int _fail;

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI/Auditer Transitions Artwork")]
        public static void Audit()
        {
            _ok = 0;
            _warn = 0;
            _fail = 0;

            var report = new StringBuilder(8192);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" AUDIT Artwork Transition AW1 (lecture seule)");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();
            report.AppendLine(
                "NOTE : matériaux attendus dans Art/FX/ (pas de dossier Materials dédié).");
            report.AppendLine();

            AuditShaders(report);
            report.AppendLine();
            AuditTextures(report);
            report.AppendLine();
            AuditMaterials(report);
            report.AppendLine();
            AuditConfig(report);
            report.AppendLine();
            AuditPrefab(report);

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
            CheckShader(report, TransitionShaderName);
            CheckShader(report, AdditiveShaderName);
        }

        private static void CheckShader(StringBuilder report, string name)
        {
            Shader s = Shader.Find(name);
            if (s != null)
                Ok(report, $"Shader trouvé : {name}");
            else
                Fail(report, $"Shader INTROUVABLE : {name}");
        }

        // ═══════════════════════════════════════════
        // TEXTURES
        // ═══════════════════════════════════════════

        private static void AuditTextures(StringBuilder report)
        {
            report.AppendLine("── Textures ──");

            CheckNoiseImporter(report, NoisePath, ArtworkNoise.SIZE);
            CheckSpriteOrDefault(
                report, GlowPath, 64,
                expectSprite: true, expectFilter: FilterMode.Bilinear, expectWrap: TextureWrapMode.Clamp);
            CheckSpriteOrDefault(
                report, RaysPath, 512,
                expectSprite: false, expectFilter: FilterMode.Bilinear, expectWrap: TextureWrapMode.Clamp);
            CheckSpriteOrDefault(
                report, VignettePath, 256,
                expectSprite: true, expectFilter: FilterMode.Bilinear, expectWrap: TextureWrapMode.Clamp);
        }

        private static void CheckNoiseImporter(StringBuilder report, string path, int expectedSize)
        {
            if (!FileExists(path))
            {
                Fail(report, $"Manquant : {path}");
                return;
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (tex == null || importer == null)
            {
                Fail(report, $"Import cassé : {path}");
                return;
            }

            bool sizeOk = tex.width == expectedSize && tex.height == expectedSize;
            bool typeOk = importer.textureType == TextureImporterType.Default;
            bool filterOk = importer.filterMode == FilterMode.Point;
            bool wrapOk = importer.wrapMode == TextureWrapMode.Repeat;
            bool mipOk = !importer.mipmapEnabled;
            bool srgbOk = !importer.sRGBTexture;
            bool compOk = importer.textureCompression == TextureImporterCompression.Uncompressed;

            if (sizeOk && typeOk && filterOk && wrapOk && mipOk && srgbOk && compOk)
            {
                Ok(report, $"{path} — {tex.width}×{tex.height} Point/Repeat Default uncompressed sRGB=off");
            }
            else
            {
                Fail(report,
                    $"{path} — import incorrect " +
                    $"(size={tex.width}x{tex.height} expect {expectedSize}, " +
                    $"type={importer.textureType}, filter={importer.filterMode}, " +
                    $"wrap={importer.wrapMode}, mip={importer.mipmapEnabled}, " +
                    $"sRGB={importer.sRGBTexture}, comp={importer.textureCompression})");
            }
        }

        private static void CheckSpriteOrDefault(
            StringBuilder report,
            string path,
            int expectedSize,
            bool expectSprite,
            FilterMode expectFilter,
            TextureWrapMode expectWrap)
        {
            if (!FileExists(path))
            {
                Fail(report, $"Manquant : {path}");
                return;
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (tex == null || importer == null)
            {
                Fail(report, $"Import cassé : {path}");
                return;
            }

            bool sizeOk = tex.width == expectedSize && tex.height == expectedSize;
            bool typeOk = expectSprite
                ? importer.textureType == TextureImporterType.Sprite
                : (importer.textureType == TextureImporterType.Default
                   || importer.textureType == TextureImporterType.Sprite);
            bool filterOk = importer.filterMode == expectFilter;
            bool wrapOk = importer.wrapMode == expectWrap;
            bool mipOk = !importer.mipmapEnabled;

            if (sizeOk && typeOk && filterOk && wrapOk && mipOk)
            {
                Ok(report,
                    $"{path} — {tex.width}×{tex.height} {importer.textureType}/{expectFilter}/{expectWrap}");
            }
            else
            {
                Fail(report,
                    $"{path} — import incorrect " +
                    $"(size={tex.width}x{tex.height}, type={importer.textureType}, " +
                    $"filter={importer.filterMode}, wrap={importer.wrapMode}, mip={importer.mipmapEnabled})");
            }

            if (expectSprite)
            {
                Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp == null)
                    Warn(report, $"{path} — Sprite sub-asset absent (import Sprite Single attendu)");
            }
        }

        // ═══════════════════════════════════════════
        // MATÉRIAUX
        // ═══════════════════════════════════════════

        private static void AuditMaterials(StringBuilder report)
        {
            report.AppendLine("── Matériaux ──");
            CheckMaterial(report, TransitionMatPath, TransitionShaderName, requireNoise: true);
            CheckMaterial(report, AdditiveMatPath, AdditiveShaderName, requireNoise: false);
        }

        private static void CheckMaterial(
            StringBuilder report, string path, string shaderName, bool requireNoise)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Fail(report, $"Matériau manquant : {path}");
                return;
            }

            if (mat.shader == null || mat.shader.name != shaderName)
            {
                Fail(report,
                    $"{path} — shader incorrect " +
                    $"(got {(mat.shader != null ? mat.shader.name : "null")}, expect {shaderName})");
            }
            else
            {
                Ok(report, $"{path} — shader OK ({shaderName})");
            }

            if (requireNoise)
            {
                Texture noise = mat.GetTexture("_NoiseTex");
                if (noise == null)
                    Fail(report, $"{path} — _NoiseTex non assigné");
                else
                    Ok(report, $"{path} — _NoiseTex = {noise.name}");
            }
        }

        // ═══════════════════════════════════════════
        // CONFIG
        // ═══════════════════════════════════════════

        private static void AuditConfig(StringBuilder report)
        {
            report.AppendLine("── Config SO ──");
            ArtworkTransitionConfig cfg =
                AssetDatabase.LoadAssetAtPath<ArtworkTransitionConfig>(ConfigPath);
            if (cfg == null)
            {
                Fail(report, $"Config manquante : {ConfigPath}");
                return;
            }

            Ok(report, $"Config présente : {ConfigPath}");

            if (cfg.noiseSeed == ExpectedNoiseSeed)
                Ok(report, $"noiseSeed = {cfg.noiseSeed} (attendu {ExpectedNoiseSeed})");
            else
                Warn(report,
                    $"noiseSeed = {cfg.noiseSeed} (attendu {ExpectedNoiseSeed} — " +
                    "OK si volontairement retuné)");

            CheckRange(report, "holdDuration", cfg.holdDuration, 0.2f, 3f);
            CheckRange(report, "burnDuration", cfg.burnDuration, 0.4f, 3f);
            CheckRange(report, "frontBand", cfg.frontBand, 0.02f, 0.15f);
            CheckRange(report, "emberRatePerSec", cfg.emberRatePerSec, 0f, 400f);
            CheckRange(report, "ashRatePerSec", cfg.ashRatePerSec, 0f, 300f);
            CheckRange(report, "pulseCount", cfg.pulseCount, 2, 5);
            CheckRange(report, "pulsePhaseDuration", cfg.pulsePhaseDuration, 0.4f, 3f);
            CheckRange(report, "reforgeDuration", cfg.reforgeDuration, 0.3f, 3f);
            CheckRange(report, "rayIntensity", cfg.rayIntensity, 0f, 1.5f);
            CheckRange(report, "climaxBurstCount", cfg.climaxBurstCount, 20, 400);
            CheckRange(report, "pixelSize", cfg.pixelSize, 1f, 8f);
            CheckRange(report, "glowIntensity", cfg.glowIntensity, 0f, 2f);
            CheckRange(report, "shakeIntensity", cfg.shakeIntensity, 0f, 2f);
            CheckRange(report, "dirWeight", cfg.dirWeight, 0f, 1f);
            CheckRange(report, "noiseUvScale", cfg.noiseUvScale, 0.5f, 4f);
        }

        private static void CheckRange(
            StringBuilder report, string name, float value, float min, float max)
        {
            if (value < min || value > max)
                Fail(report, $"Config.{name} = {value} hors plage [{min}..{max}]");
            else
                Ok(report, $"Config.{name} = {value} ∈ [{min}..{max}]");
        }

        private static void CheckRange(
            StringBuilder report, string name, int value, int min, int max)
        {
            if (value < min || value > max)
                Fail(report, $"Config.{name} = {value} hors plage [{min}..{max}]");
            else
                Ok(report, $"Config.{name} = {value} ∈ [{min}..{max}]");
        }

        // ═══════════════════════════════════════════
        // PREFAB
        // ═══════════════════════════════════════════

        private static void AuditPrefab(StringBuilder report)
        {
            report.AppendLine("── Prefab ──");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Fail(report, $"Prefab manquant : {PrefabPath}");
                return;
            }

            Ok(report, $"Prefab présent : {PrefabPath}");

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<ArtworkTransitionDevHarness>() != null)
                    Warn(report, "Harness présent sur le prefab (attendu : absent)");
                else
                    Ok(report, "Harness absent du prefab (OK)");

                var view = root.GetComponent<ArtworkTransitionView>();
                var driver = root.GetComponent<ArtworkTransitionDriver>();
                var cg = root.GetComponent<CanvasGroup>();

                if (view == null) Fail(report, "ArtworkTransitionView manquant sur root");
                else Ok(report, "ArtworkTransitionView présent");

                if (driver == null) Fail(report, "ArtworkTransitionDriver manquant sur root");
                else Ok(report, "ArtworkTransitionDriver présent");

                if (cg == null) Warn(report, "CanvasGroup manquant sur root");
                else Ok(report, "CanvasGroup présent");

                CheckHierarchyChild(report, root.transform, "Shaker");
                CheckHierarchyChild(report, root.transform, "Vignette");
                CheckHierarchyChild(report, root.transform, "Flash");

                Transform shaker = root.transform.Find("Shaker");
                if (shaker != null)
                {
                    CheckHierarchyChild(report, shaker, "RaysA");
                    CheckHierarchyChild(report, shaker, "RaysB");
                    CheckHierarchyChild(report, shaker, "Halo");
                    CheckHierarchyChild(report, shaker, "Card");
                    CheckHierarchyChild(report, shaker, "ParticlesAsh");
                    CheckHierarchyChild(report, shaker, "ParticlesEnergy");
                }

                if (view != null)
                {
                    SerializedObject so = new SerializedObject(view);
                    CheckRef(report, so, "shaker", "View.shaker");
                    CheckRef(report, so, "raysA", "View.raysA");
                    CheckRef(report, so, "raysB", "View.raysB");
                    CheckRef(report, so, "halo", "View.halo");
                    CheckRef(report, so, "card", "View.card");
                    CheckRef(report, so, "particlesAsh", "View.particlesAsh");
                    CheckRef(report, so, "particlesEnergy", "View.particlesEnergy");
                    CheckRef(report, so, "vignette", "View.vignette");
                    CheckRef(report, so, "flash", "View.flash");
                    CheckRef(report, so, "stageRoot", "View.stageRoot");

                    var card = so.FindProperty("card").objectReferenceValue as ArtworkTransitionGraphic;
                    if (card != null)
                    {
                        SerializedObject cardSo = new SerializedObject(card);
                        CheckRef(report, cardSo, "sharedMaterial", "Card.sharedMaterial");
                        CheckRef(report, cardSo, "noiseTexture", "Card.noiseTexture");
                        if (card.raycastTarget)
                            Fail(report, "Card.raycastTarget = true (attendu false)");
                        else
                            Ok(report, "Card.raycastTarget = false");
                    }
                }

                if (driver != null)
                {
                    SerializedObject dso = new SerializedObject(driver);
                    CheckRef(report, dso, "view", "Driver.view");
                    CheckRef(report, dso, "config", "Driver.config");
                }

                AuditRaycastTargets(report, root);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CheckHierarchyChild(StringBuilder report, Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t == null)
                Fail(report, $"Enfant manquant : {parent.name}/{name}");
            else
                Ok(report, $"Enfant OK : {parent.name}/{name}");
        }

        private static void CheckRef(
            StringBuilder report, SerializedObject so, string prop, string label)
        {
            SerializedProperty p = so.FindProperty(prop);
            if (p == null)
            {
                Fail(report, $"{label} — propriété SerializeField introuvable (« {prop} »)");
                return;
            }

            if (p.objectReferenceValue == null)
                Fail(report, $"{label} — référence NULL");
            else
                Ok(report, $"{label} → {p.objectReferenceValue.name}");
        }

        private static void AuditRaycastTargets(StringBuilder report, GameObject root)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            int bad = 0;
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null && graphics[i].raycastTarget)
                {
                    bad++;
                    Fail(report, $"raycastTarget=true sur {GetPath(graphics[i].transform)}");
                }
            }

            if (bad == 0)
                Ok(report, $"Tous les Graphic ({graphics.Length}) ont raycastTarget=false");
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static bool FileExists(string assetPath)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            return File.Exists(full);
        }

        private static string GetPath(Transform t)
        {
            if (t.parent == null)
                return t.name;
            return GetPath(t.parent) + "/" + t.name;
        }

        private static void Ok(StringBuilder report, string msg)
        {
            _ok++;
            report.AppendLine($"  ✅ {msg}");
        }

        private static void Warn(StringBuilder report, string msg)
        {
            _warn++;
            report.AppendLine($"  ⚠️ {msg}");
        }

        private static void Fail(StringBuilder report, string msg)
        {
            _fail++;
            report.AppendLine($"  ❌ {msg}");
        }

        private static void WriteReport(string text)
        {
            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, "artwork_transition_audit.txt");
            File.WriteAllText(fullPath, text, Encoding.UTF8);
            Debug.Log($"[ArtworkTransitionAuditor] Rapport : {fullPath}");
        }
    }
}
#endif
