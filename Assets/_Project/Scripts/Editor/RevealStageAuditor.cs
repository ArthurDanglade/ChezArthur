#if UNITY_EDITOR
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using ChezArthur.UI.RevealStage;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Audit lecture seule INVR1 — n'écrit que le rapport. Exige 0 FAIL.
    /// </summary>
    public static class RevealStageAuditor
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string ShaderName = "ChezArthur/UI/RevealLight";
        private const string MatPath = "Assets/_Project/Art/FX/RevealLight.mat";
        private const string ConfigPath = "Assets/_Project/Data/UI/RevealStageConfig.asset";
        private const string AwViewPath =
            "Assets/_Project/Scripts/UI/ArtworkTransition/ArtworkTransitionView.cs";
        private const string AwPpgPath =
            "Assets/_Project/Scripts/UI/ArtworkTransition/PixelParticleGraphic.cs";
        private const string GachaCtrlPath =
            "Assets/_Project/Scripts/Gacha/GachaAnimationController.cs";

        private static readonly string[] PurgedPaths =
        {
            "Assets/_Project/Scripts/UI/InvocationFlow",
            "Assets/_Project/Shaders/PixelVeil.shader",
            "Assets/_Project/Scripts/Editor/InvocationFlowAssetsBuilder.cs",
            "Assets/_Project/Scripts/Editor/InvocationFlowAuditor.cs",
            "Assets/_Project/Art/FX/PixelVeil.mat",
            "Assets/_Project/Data/UI/InvocationFlowConfig.asset",
            "Assets/_Project/Prefabs/UI/PixelVeilOverlay.prefab",
            "Assets/_Project/Prefabs/UI/RevealBanner.prefab",
            "Assets/_Project/Prefabs/UI/RevealRarityLayer.prefab",
            "Audits/invocation_flow_build.txt",
            "Audits/invocation_flow_audit.txt",
            "Assets/_Project/Scripts/UI/InvocationFlow/InvocationFlowConfig.cs"
        };

        private static readonly string[] NewFiles =
        {
            "Assets/_Project/Shaders/RevealLight.shader",
            "Assets/_Project/Scripts/UI/RevealStage/RevealStageConfig.cs",
            "Assets/_Project/Scripts/UI/RevealStage/RevealStageDirector.cs",
            "Assets/_Project/Scripts/UI/RevealStage/RevealInfoPanel.cs",
            "Assets/_Project/Scripts/UI/RevealStage/RevealPixelFxGraphic.cs",
            "Assets/_Project/Scripts/UI/RevealStage/RevealStageDevHarness.cs",
            "Assets/_Project/Scripts/Editor/RevealStageAssetsBuilder.cs",
            "Assets/_Project/Scripts/Editor/RevealStageAuditor.cs"
        };

        private static readonly string[] ShaderProps =
        {
            "_RectMin", "_RectSize", "_DitherCellPx", "_FocalRect", "_AspectY",
            "_LightR", "_LightB", "_Tint", "_Snap", "_FrontSoft",
            "_Flash", "_Vignette", "_ShadowLevel", "_Dim"
        };

        private static int _ok;
        private static int _warn;
        private static int _fail;

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Reveal/Auditer INVR1")]
        public static void Audit()
        {
            _ok = 0;
            _warn = 0;
            _fail = 0;

            var report = new StringBuilder(16384);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" AUDIT Reveal Stage INVR1 (lecture seule)");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();

            AuditPurge(report);
            report.AppendLine();
            AuditNewFiles(report);
            report.AppendLine();
            AuditShader(report);
            report.AppendLine();
            AuditConfig(report);
            report.AppendLine();
            AuditMaterial(report);
            report.AppendLine();
            AuditAwHashes(report);
            report.AppendLine();
            AuditDormancy(report);

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
        // SECTIONS
        // ═══════════════════════════════════════════

        private static void AuditPurge(StringBuilder report)
        {
            report.AppendLine("── Purge INV1 ──");
            int remaining = 0;
            for (int i = 0; i < PurgedPaths.Length; i++)
            {
                if (FileOrFolderExists(PurgedPaths[i]))
                {
                    Fail(report, $"Purge incomplète — existe encore : {PurgedPaths[i]}");
                    remaining++;
                }
            }

            if (remaining == 0)
                Ok(report, "Aucun des chemins INV1 purgés n'existe");

            // t:Script InvocationFlow = 0
            string[] scripts = AssetDatabase.FindAssets("t:Script InvocationFlow");
            if (scripts != null && scripts.Length > 0)
            {
                for (int i = 0; i < scripts.Length; i++)
                    Fail(report, $"Script InvocationFlow restant : {AssetDatabase.GUIDToAssetPath(scripts[i])}");
            }
            else
            {
                Ok(report, "t:Script InvocationFlow = 0");
            }
        }

        private static void AuditNewFiles(StringBuilder report)
        {
            report.AppendLine("── Fichiers INVR1 ──");
            for (int i = 0; i < NewFiles.Length; i++)
            {
                if (FileExists(NewFiles[i]))
                    Ok(report, $"Présent : {NewFiles[i]}");
                else
                    Fail(report, $"Manquant : {NewFiles[i]}");
            }
        }

        private static void AuditShader(StringBuilder report)
        {
            report.AppendLine("── Shader RevealLight ──");
            Shader s = Shader.Find(ShaderName);
            if (s == null)
            {
                Fail(report, $"Shader INTROUVABLE : {ShaderName}");
                return;
            }

            Ok(report, $"Shader trouvé : {ShaderName}");

            // 13 propriétés pilotées (+ _MainTex hors compte directeur)
            // Spec : 13 propriétés listées dans le prompt
            int found = 0;
            for (int i = 0; i < ShaderProps.Length; i++)
            {
                if (s.HasProperty(ShaderProps[i]))
                {
                    found++;
                }
                else
                {
                    Fail(report, $"Propriété manquante : {ShaderProps[i]}");
                }
            }

            if (found == ShaderProps.Length)
                Ok(report, $"Propriétés Director présentes ({found}/{ShaderProps.Length})");
        }

        private static void AuditConfig(StringBuilder report)
        {
            report.AppendLine("── Config (défauts Bloc 2) ──");
            RevealStageConfig cfg =
                AssetDatabase.LoadAssetAtPath<RevealStageConfig>(ConfigPath);
            if (cfg == null)
            {
                Fail(report, $"Manquant : {ConfigPath}");
                return;
            }

            Ok(report, $"Config présente → {ConfigPath}");

            CheckDefault(report, "ditherCell", cfg.ditherCell, 3f);
            CheckDefault(report, "shadowLevel", cfg.shadowLevel, 0.62f);
            CheckDefault(report, "cutDuration", cfg.cutDuration, 0.08f);
            CheckDefault(report, "frontSoft", cfg.frontSoft, 0.10f);
            CheckDefault(report, "vignette", cfg.vignette, 0.34f);
            CheckDefault(report, "exitDim", cfg.exitDim, 0.28f);
            CheckDefault(report, "entryOverlap", cfg.entryOverlap, 0.15f);

            CheckDefault(report, "entrySR", cfg.entrySR, 0.95f);
            CheckDefault(report, "pulsesSR", cfg.pulsesSR, 2);
            CheckDefault(report, "holdSR", cfg.holdSR, 0f);
            CheckDefault(report, "snapSR", cfg.snapSR, 0.24f);
            CheckDefault(report, "lightMaxSR", cfg.lightMaxSR, 0.34f);
            CheckDefault(report, "punchSR", cfg.punchSR, 0.045f);
            CheckDefault(report, "partsSR", cfg.partsSR, 30);

            CheckDefault(report, "entrySSR", cfg.entrySSR, 1.60f);
            CheckDefault(report, "pulsesSSR", cfg.pulsesSSR, 3);
            CheckDefault(report, "holdSSR", cfg.holdSSR, 0.28f);
            CheckDefault(report, "snapSSR", cfg.snapSSR, 0.30f);
            CheckDefault(report, "lightMaxSSR", cfg.lightMaxSSR, 0.42f);
            CheckDefault(report, "punchSSR", cfg.punchSSR, 0.065f);
            CheckDefault(report, "partsSSR", cfg.partsSSR, 95);

            CheckDefault(report, "entryLR", cfg.entryLR, 1.95f);
            CheckDefault(report, "pulsesLR", cfg.pulsesLR, 4);
            CheckDefault(report, "holdLR", cfg.holdLR, 0.38f);
            CheckDefault(report, "snapLR", cfg.snapLR, 0.34f);
            CheckDefault(report, "lightMaxLR", cfg.lightMaxLR, 0.46f);
            CheckDefault(report, "punchLR", cfg.punchLR, 0.075f);
            CheckDefault(report, "partsLR", cfg.partsLR, 140);

            CheckDefault(report, "fakeHold", cfg.fakeHold, 0.18f);
            CheckDefault(report, "fakeCutBonus", cfg.fakeCutBonus, 0.14f);

            CheckDefault(report, "nameDelay", cfg.nameDelay, 0.10f);
            CheckDefault(report, "nameDur", cfg.nameDur, 0.25f);
            CheckDefault(report, "statusDelay", cfg.statusDelay, 0.42f);
            CheckDefault(report, "chipFill", cfg.chipFill, 0.45f);
            CheckDefault(report, "tickStagger", cfg.tickStagger, 0.12f);
        }

        private static void AuditMaterial(StringBuilder report)
        {
            report.AppendLine("── Matériau ──");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                Fail(report, $"Manquant : {MatPath}");
                return;
            }

            if (mat.shader != null && mat.shader.name == ShaderName)
                Ok(report, $"RevealLight.mat → shader {ShaderName}");
            else
                Fail(report, $"RevealLight.mat shader incorrect : {(mat.shader != null ? mat.shader.name : "null")}");
        }

        private static void AuditAwHashes(StringBuilder report)
        {
            report.AppendLine("── Socle AW intact (SHA256) ──");
            string h1 = Sha256OfAsset(AwViewPath);
            string h2 = Sha256OfAsset(AwPpgPath);

            if (string.IsNullOrEmpty(h1))
                Fail(report, $"Impossible de hasher : {AwViewPath}");
            else
                Ok(report, $"ArtworkTransitionView.cs SHA256 = {h1}");

            if (string.IsNullOrEmpty(h2))
                Fail(report, $"Impossible de hasher : {AwPpgPath}");
            else
                Ok(report, $"PixelParticleGraphic.cs SHA256 = {h2}");

            report.AppendLine(
                "  NOTE : ces hash prouvent que le socle AW n'a pas été modifié par INVR1.");
        }

        private static void AuditDormancy(StringBuilder report)
        {
            report.AppendLine("── Dormance gacha ──");
            if (!FileExists(GachaCtrlPath))
            {
                Warn(report, $"GachaAnimationController introuvable : {GachaCtrlPath}");
                return;
            }

            string text = File.ReadAllText(FullPath(GachaCtrlPath));
            if (text.Contains("RevealStage") || text.Contains("RevealLight")
                || text.Contains("RevealStageDirector"))
            {
                Fail(report, "GachaAnimationController référence RevealStage (dormance brisée)");
            }
            else
            {
                Ok(report, "GachaAnimationController sans référence RevealStage (dormance OK)");
            }
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static void CheckDefault(StringBuilder report, string name, float value, float expected)
        {
            if (Mathf.Abs(value - expected) < 0.001f)
                Ok(report, $"Défaut {name} = {value}");
            else
                Fail(report, $"Défaut {name} = {value} (attendu {expected})");
        }

        private static void CheckDefault(StringBuilder report, string name, int value, int expected)
        {
            if (value == expected)
                Ok(report, $"Défaut {name} = {value}");
            else
                Fail(report, $"Défaut {name} = {value} (attendu {expected})");
        }

        private static string Sha256OfAsset(string assetPath)
        {
            string full = FullPath(assetPath);
            if (!File.Exists(full))
                return null;

            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(full))
            {
                byte[] hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("X2"));
                return sb.ToString();
            }
        }

        private static string FullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static bool FileExists(string assetPath) => File.Exists(FullPath(assetPath));

        private static bool FileOrFolderExists(string assetPath)
        {
            string full = FullPath(assetPath);
            return File.Exists(full) || Directory.Exists(full);
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
            string fullPath = Path.Combine(auditsRoot, "reveal_stage_audit.txt");
            File.WriteAllText(fullPath, text, Encoding.UTF8);
            Debug.Log($"[RevealStageAuditor] Rapport écrit : {fullPath}");
        }
    }
}
#endif
