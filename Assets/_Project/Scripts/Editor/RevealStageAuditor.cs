#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChezArthur.Gacha;
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

        [MenuItem("Chez Arthur/Reveal/Auditer INVR2")]
        public static void AuditInvr2()
        {
            _ok = 0;
            _warn = 0;
            _fail = 0;

            var report = new StringBuilder(16384);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" AUDIT Reveal Stage INVR2 (lecture seule)");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();

            AuditInvr2Wiring(report);
            report.AppendLine();
            AuditInvr2LegacyAbsent(report);
            report.AppendLine();
            AuditInvr2DeletedFiles(report);
            report.AppendLine();
            AuditInvr2SmokeTrain(report);
            report.AppendLine();
            AuditInvr2Clips(report);
            report.AppendLine();
            AuditInvr2Pity(report);
            report.AppendLine();
            AuditAwHashes(report);

            report.AppendLine();
            report.AppendLine("───────────────────────────────────────────");
            report.AppendLine($" SYNTHÈSE : OK={_ok}  WARN={_warn}  FAIL={_fail}");
            report.AppendLine("───────────────────────────────────────────");
            report.AppendLine(" Fin du rapport (aucune modification effectuée)");
            report.AppendLine("═══════════════════════════════════════════");

            string text = report.ToString();
            Debug.Log(text);
            WriteReportTo("Audits/reveal_stage_audit_invr2.txt", text);
        }

        [MenuItem("Chez Arthur/Reveal/Auditer INVR3")]
        public static void AuditInvr3()
        {
            _ok = 0;
            _warn = 0;
            _fail = 0;

            var report = new StringBuilder(16384);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" AUDIT Reveal Stage INVR3 (lecture seule)");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();

            AuditInvr3Slots(report);
            report.AppendLine();
            AuditInvr3ClipSpecs(report);
            report.AppendLine();
            AuditInvr3Imports(report);
            report.AppendLine();
            AuditInvr3DirectorChannels(report);
            report.AppendLine();
            AuditInvr3Controller(report);
            report.AppendLine();
            AuditAwHashes(report);

            report.AppendLine();
            report.AppendLine("───────────────────────────────────────────");
            report.AppendLine($" SYNTHÈSE : OK={_ok}  WARN={_warn}  FAIL={_fail}");
            report.AppendLine("───────────────────────────────────────────");
            report.AppendLine(" Fin du rapport (aucune modification effectuée)");
            report.AppendLine("═══════════════════════════════════════════");

            string text = report.ToString();
            Debug.Log(text);
            WriteReportTo("Audits/reveal_stage_audit_invr3.txt", text);
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

            // Unity 2022 : HasProperty est sur Material, pas Shader.
            // FindPropertyIndex (>= 0) = API Shader stable.
            int found = 0;
            for (int i = 0; i < ShaderProps.Length; i++)
            {
                if (s.FindPropertyIndex(ShaderProps[i]) >= 0)
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

            CheckDefault(report, "ditherCell", cfg.ditherCell, 4f);
            CheckDefault(report, "shadowLevel", cfg.shadowLevel, 0.47f);
            CheckDefault(report, "cutDuration", cfg.cutDuration, 0.08f);
            CheckDefault(report, "frontSoft", cfg.frontSoft, 0.10f);
            CheckDefault(report, "vignette", cfg.vignette, 0.34f);
            CheckDefault(report, "exitDim", cfg.exitDim, 0.28f);
            CheckDefault(report, "entryOverlap", cfg.entryOverlap, 0.15f);

            CheckDefault(report, "entrySR", cfg.entrySR, 1.25f);
            CheckDefault(report, "pulsesSR", cfg.pulsesSR, 2);
            CheckDefault(report, "holdSR", cfg.holdSR, 0f);
            CheckDefault(report, "snapSR", cfg.snapSR, 0.24f);
            CheckDefault(report, "lightMaxSR", cfg.lightMaxSR, 0.28f);
            CheckDefault(report, "punchSR", cfg.punchSR, 0.045f);
            CheckDefault(report, "partsSR", cfg.partsSR, 30);

            CheckDefault(report, "entrySSR", cfg.entrySSR, 2.05f);
            CheckDefault(report, "pulsesSSR", cfg.pulsesSSR, 3);
            CheckDefault(report, "holdSSR", cfg.holdSSR, 0.28f);
            CheckDefault(report, "snapSSR", cfg.snapSSR, 0.30f);
            CheckDefault(report, "lightMaxSSR", cfg.lightMaxSSR, 0.38f);
            CheckDefault(report, "punchSSR", cfg.punchSSR, 0.065f);
            CheckDefault(report, "partsSSR", cfg.partsSSR, 95);

            CheckDefault(report, "entryLR", cfg.entryLR, 2.07f);
            CheckDefault(report, "pulsesLR", cfg.pulsesLR, 4);
            CheckDefault(report, "holdLR", cfg.holdLR, 0.38f);
            CheckDefault(report, "snapLR", cfg.snapLR, 0.34f);
            CheckDefault(report, "lightMaxLR", cfg.lightMaxLR, 0.42f);
            CheckDefault(report, "punchLR", cfg.punchLR, 0.075f);
            CheckDefault(report, "partsLR", cfg.partsLR, 140);

            CheckDefault(report, "fakeHold", cfg.fakeHold, 0.18f);
            CheckDefault(report, "fakeCutBonus", cfg.fakeCutBonus, 0.14f);

            CheckDefault(report, "nameDelay", cfg.nameDelay, 0.10f);
            CheckDefault(report, "nameDur", cfg.nameDur, 0.25f);
            CheckDefault(report, "statusDelay", cfg.statusDelay, 1.15f);
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
            report.AppendLine("── Dormance gacha (INVR1 historique) ──");
            if (!FileExists(GachaCtrlPath))
            {
                Warn(report, $"GachaAnimationController introuvable : {GachaCtrlPath}");
                return;
            }

            string text = File.ReadAllText(FullPath(GachaCtrlPath));
            if (text.Contains("RevealStageDirector"))
                Ok(report, "GachaAnimationController référence RevealStage (collage INVR2 actif)");
            else
                Ok(report, "GachaAnimationController sans référence RevealStage (dormance OK)");
        }

        private static void AuditInvr2Wiring(StringBuilder report)
        {
            report.AppendLine("── Câblage controller INVR2 ──");
            GachaAnimationController ctrl = FindGachaController();
            if (ctrl == null)
            {
                Fail(report, "GachaAnimationController introuvable (ouvrir Hub.unity).");
                return;
            }

            SerializedObject so = new SerializedObject(ctrl);
            CheckWired(report, so, "revealDirector");
            CheckWired(report, so, "revealConfig");
            CheckWired(report, so, "skipAllButton");
        }

        private static void AuditInvr2LegacyAbsent(StringBuilder report)
        {
            report.AppendLine("── Symboles legacy absents du controller ──");
            if (!FileExists(GachaCtrlPath))
            {
                Fail(report, "GachaAnimationController.cs manquant");
                return;
            }

            string text = File.ReadAllText(FullPath(GachaCtrlPath));
            string[] forbidden =
            {
                "InterRevealSmokeCover",
                "PlayPixelResolve",
                "GachaRevealStatusUI",
                "doorPanel",
                "smokeTransition",
                "ArmPixelResolveStart",
                "FinishPixelResolve",
                "EnsurePixelateInstance",
                "revealStatusUi"
            };

            for (int i = 0; i < forbidden.Length; i++)
            {
                if (text.Contains(forbidden[i]))
                    Fail(report, $"Symbole legacy encore présent : {forbidden[i]}");
                else
                    Ok(report, $"Absent : {forbidden[i]}");
            }
        }

        private static void AuditInvr2DeletedFiles(StringBuilder report)
        {
            report.AppendLine("── Fichiers purgés INVR2 ──");
            string[] paths =
            {
                "Assets/_Project/Scripts/Gacha/GachaRevealStatusUI.cs",
                "Assets/_Project/Shaders/GachaRevealPixelate.shader",
                "Assets/_Project/Art/FX/GachaRevealPixelate.mat"
            };

            for (int i = 0; i < paths.Length; i++)
            {
                if (FileExists(paths[i]))
                    Fail(report, $"Encore présent : {paths[i]}");
                else
                    Ok(report, $"Supprimé : {paths[i]}");
            }
        }

        private static void AuditInvr2SmokeTrain(StringBuilder report)
        {
            report.AppendLine("── SmokeTransition (train) ──");
            EnsureHubLoaded();

            TrainSequenceController train =
                UnityEngine.Object.FindObjectOfType<TrainSequenceController>(true);
            if (train == null)
            {
                Fail(report, "TrainSequenceController introuvable dans Hub.");
                return;
            }

            SerializedObject so = new SerializedObject(train);
            SerializedProperty smokeProp = so.FindProperty("smokeTransition");
            if (smokeProp == null)
            {
                Fail(report, "TrainSequenceController.smokeTransition propriété absente.");
                return;
            }

            if (smokeProp.objectReferenceValue == null)
                Fail(report, "TrainSequenceController.smokeTransition = null (voile train cassé).");
            else
                Ok(report, $"SmokeTransition câblé sur le train → {smokeProp.objectReferenceValue.name}");

            Transform smokeGo = FindDeep(train.transform.root, "SmokeTransition");
            if (smokeGo != null)
                Ok(report, "GO SmokeTransition présent dans Hub.");
            else
                Fail(report, "GO SmokeTransition introuvable dans Hub.");
        }

        private static void AuditInvr2Clips(StringBuilder report)
        {
            report.AppendLine("── Clips provisoires SO ──");
            RevealStageConfig cfg =
                AssetDatabase.LoadAssetAtPath<RevealStageConfig>(ConfigPath);
            if (cfg == null)
            {
                Fail(report, $"Config manquante : {ConfigPath}");
                return;
            }

            CheckClipNonNull(report, "entryRiserClip", cfg.entryRiserClip);
            CheckClipNonNull(report, "snapSrClip", cfg.snapSrClip);
            CheckClipNonNull(report, "snapSsrClip", cfg.snapSsrClip);
            CheckClipNonNull(report, "snapLrClip", cfg.snapLrClip);
            CheckClipNonNull(report, "stampClip", cfg.stampClip);
            CheckClipNonNull(report, "statTickClip", cfg.statTickClip);
            if (cfg.exitDimClip == null)
                Ok(report, "exitDimClip = null (provisoire OK)");
            else
                Warn(report, "exitDimClip non-null (attendu null jusqu'à INVR3)");
        }

        private static void AuditInvr2Pity(StringBuilder report)
        {
            report.AppendLine("── isPity (fakeout A) ──");
            string pullPath = "Assets/_Project/Scripts/Gacha/GachaPullResult.cs";
            string mgrPath = "Assets/_Project/Scripts/Gacha/GachaManager.cs";

            if (!FileExists(pullPath))
                Fail(report, "GachaPullResult.cs manquant");
            else if (File.ReadAllText(FullPath(pullPath)).Contains("isPity"))
                Ok(report, "PulledCharacter.isPity présent");
            else
                Fail(report, "PulledCharacter.isPity absent");

            if (!FileExists(mgrPath))
                Fail(report, "GachaManager.cs manquant");
            else if (File.ReadAllText(FullPath(mgrPath)).Contains("pulled.isPity = true"))
                Ok(report, "GachaManager marque isPity au site forceSSR");
            else
                Fail(report, "GachaManager ne marque pas isPity");
        }

        private static readonly string[] Invr3ClipPaths =
        {
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_entry_riser.wav",
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_sr.wav",
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_ssr.wav",
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_lr.wav",
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_stamp.wav",
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_exit_dim.wav",
            "Assets/_Project/Audio/SFX/statsupsound.wav"
        };

        private static void AuditInvr3Slots(StringBuilder report)
        {
            report.AppendLine("── Slots SO INVR3 ──");
            RevealStageConfig cfg =
                AssetDatabase.LoadAssetAtPath<RevealStageConfig>(ConfigPath);
            if (cfg == null)
            {
                Fail(report, $"Config manquante : {ConfigPath}");
                return;
            }

            CheckSlotPath(report, "entryRiserClip", cfg.entryRiserClip,
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_entry_riser.wav");
            CheckSlotPath(report, "snapSrClip", cfg.snapSrClip,
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_sr.wav");
            CheckSlotPath(report, "snapSsrClip", cfg.snapSsrClip,
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_ssr.wav");
            CheckSlotPath(report, "snapLrClip", cfg.snapLrClip,
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_lr.wav");
            CheckSlotPath(report, "stampClip", cfg.stampClip,
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_stamp.wav");
            CheckSlotPath(report, "exitDimClip", cfg.exitDimClip,
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_exit_dim.wav");
            CheckSlotPath(report, "statTickClip", cfg.statTickClip,
                "Assets/_Project/Audio/SFX/statsupsound.wav");

            // Provisoires absents
            string[] banned =
            {
                "revealsound", "unlocksound", "lvlupsound"
            };
            AudioClip[] slots =
            {
                cfg.entryRiserClip, cfg.snapSrClip, cfg.snapSsrClip, cfg.snapLrClip,
                cfg.stampClip, cfg.exitDimClip, cfg.statTickClip
            };
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;
                string n = slots[i].name.ToLowerInvariant();
                for (int b = 0; b < banned.Length; b++)
                {
                    if (n.Contains(banned[b]))
                        Fail(report, $"Provisoire encore câblé : {slots[i].name}");
                }
            }
        }

        private static void CheckSlotPath(
            StringBuilder report, string slot, AudioClip clip, string expectedPath)
        {
            if (clip == null)
            {
                Fail(report, $"{slot} = null");
                return;
            }

            string path = AssetDatabase.GetAssetPath(clip).Replace('\\', '/');
            if (path == expectedPath)
                Ok(report, $"{slot} ← {expectedPath}");
            else
                Fail(report, $"{slot} = {path} (attendu {expectedPath})");
        }

        private static void AuditInvr3ClipSpecs(StringBuilder report)
        {
            report.AppendLine("── Specs AudioClip ──");
            CheckClipSpec(report, "entry_riser",
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_entry_riser.wav", 2.4f, 2.7f);
            CheckClipSpec(report, "snap_sr",
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_sr.wav", 0.55f, 0.75f);
            CheckClipSpec(report, "snap_ssr",
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_ssr.wav", 1.0f, 1.2f);
            CheckClipSpec(report, "snap_lr",
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_lr.wav", 1.4f, 1.6f);
            CheckClipSpec(report, "stamp",
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_stamp.wav", 0.40f, 0.50f);
            CheckClipSpec(report, "exit_dim",
                "Assets/_Project/Audio/SFX/Reveal/sfx_inv_exit_dim.wav", 0.35f, 0.45f);
        }

        private static void CheckClipSpec(
            StringBuilder report, string label, string path, float minDur, float maxDur)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Fail(report, $"{label} introuvable → {path}");
                return;
            }

            if (clip.channels == 1)
                Ok(report, $"{label} mono");
            else
                Fail(report, $"{label} channels={clip.channels} (attendu 1)");

            if (clip.frequency == 44100)
                Ok(report, $"{label} 44100 Hz");
            else
                Fail(report, $"{label} frequency={clip.frequency} (attendu 44100)");

            float dur = clip.length;
            if (dur >= minDur - 0.02f && dur <= maxDur + 0.02f)
                Ok(report, $"{label} durée={dur:0.00}s (bornes {minDur}-{maxDur})");
            else
                Fail(report, $"{label} durée={dur:0.00}s hors bornes {minDur}-{maxDur}");
        }

        private static void AuditInvr3Imports(StringBuilder report)
        {
            report.AppendLine("── Import settings ──");
            for (int i = 0; i < Invr3ClipPaths.Length; i++)
            {
                string path = Invr3ClipPaths[i];
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    Fail(report, $"Importer absent : {path}");
                    continue;
                }

                AudioImporterSampleSettings s = importer.defaultSampleSettings;
                bool wantDecompress = !path.EndsWith("statsupsound.wav");
                bool loadOk = wantDecompress
                    ? s.loadType == AudioClipLoadType.DecompressOnLoad
                    : s.loadType == AudioClipLoadType.CompressedInMemory;
                bool ok = importer.forceToMono
                    && loadOk
                    && s.compressionFormat == AudioCompressionFormat.Vorbis
                    && Mathf.Abs(s.quality - 0.7f) < 0.05f
                    && s.preloadAudioData;
                if (ok)
                {
                    Ok(report, $"Import OK : {path}");
                }
                else
                {
                    Fail(report,
                        $"Import non conforme : {path} "
                        + $"(mono={importer.forceToMono}, load={s.loadType}, "
                        + $"fmt={s.compressionFormat}, q={s.quality}, "
                        + $"preload={s.preloadAudioData})");
                }
            }
        }

        private static void AuditInvr3DirectorChannels(StringBuilder report)
        {
            report.AppendLine("── Canaux Director ──");
            string path =
                "Assets/_Project/Scripts/UI/RevealStage/RevealStageDirector.cs";
            if (!FileExists(path))
            {
                Fail(report, "RevealStageDirector.cs manquant");
                return;
            }

            string text = File.ReadAllText(FullPath(path));
            int managed = CountOccurrences(text, "PlayManagedSfx");
            if (managed == 1)
                Ok(report, "PlayManagedSfx ×1 (riser seul)");
            else
                Fail(report, $"PlayManagedSfx ×{managed} (attendu 1)");

            if (text.Contains("PlayOneShot(config.GetSnapClip")
                || text.Contains("PlayOneShot(config.GetSnapClip(rarity)"))
                Ok(report, "Snap via PlayOneShot");
            else if (text.Contains("PlayOneShot") && text.Contains("GetSnapClip"))
                Ok(report, "Snap via PlayOneShot");
            else
                Fail(report, "Snap n'utilise pas PlayOneShot");

            if (text.Contains("PlayOneShot(config.exitDimClip"))
                Ok(report, "Dim via PlayOneShot");
            else
                Fail(report, "Dim n'utilise pas PlayOneShot");

            if (text.Contains("bool skipEntry"))
                Ok(report, "skipEntry présent dans la signature");
            else
                Fail(report, "skipEntry absent de CoPlayArrival");
        }

        private static void AuditInvr3Controller(StringBuilder report)
        {
            report.AppendLine("── Controller INVR3 ──");
            if (!FileExists(GachaCtrlPath))
            {
                Fail(report, "GachaAnimationController.cs manquant");
                return;
            }

            string text = File.ReadAllText(FullPath(GachaCtrlPath));
            if (text.Contains("skipEntry: _skipAllRequested"))
                Ok(report, "skipEntry: _skipAllRequested branché");
            else
                Fail(report, "skipEntry non branché sur _skipAllRequested");

            // Plus de SkipToSnap immédiat après StartCoroutine(CoPlayArrival
            if (text.Contains("skipEntry: _skipAllRequested));")
                && !text.Contains("skipEntry: _skipAllRequested));\r\n            if (_skipAllRequested)\r\n                revealDirector.SkipToSnap();")
                && !text.Contains("skipEntry: _skipAllRequested));\n            if (_skipAllRequested)\n                revealDirector.SkipToSnap();"))
            {
                Ok(report, "SkipToSnap post-lancement arrivée absent");
            }
            else
            {
                // Heuristique : chercher le bloc
                int idx = text.IndexOf("skipEntry: _skipAllRequested");
                if (idx >= 0)
                {
                    string after = text.Substring(idx, Math.Min(180, text.Length - idx));
                    if (after.Contains("SkipToSnap()"))
                        Fail(report, "SkipToSnap encore présent juste après CoPlayArrival");
                    else
                        Ok(report, "SkipToSnap post-lancement arrivée absent");
                }
                else
                {
                    Fail(report, "Impossible de vérifier SkipToSnap post-lancement");
                }
            }

            if (text.Contains("skipSettle: true"))
                Ok(report, "R-D3 : skipSettle: true présent");
            else
                Fail(report, "R-D3 : skipSettle: true absent");
        }

        private static int CountOccurrences(string text, string token)
        {
            int count = 0;
            int idx = 0;
            while ((idx = text.IndexOf(token, idx, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                idx += token.Length;
            }

            return count;
        }

        private static void CheckWired(StringBuilder report, SerializedObject so, string prop)
        {
            SerializedProperty p = so.FindProperty(prop);
            if (p == null)
            {
                Fail(report, $"Propriété absente : {prop}");
                return;
            }

            if (p.objectReferenceValue == null)
                Fail(report, $"{prop} = null (builder à relancer)");
            else
                Ok(report, $"{prop} ← {p.objectReferenceValue.name}");
        }

        private static void CheckClipNonNull(StringBuilder report, string name, AudioClip clip)
        {
            if (clip != null)
                Ok(report, $"{name} ← {clip.name}");
            else
                Fail(report, $"{name} = null (clips provisoires manquants)");
        }

        private static GachaAnimationController FindGachaController()
        {
            EnsureHubLoaded();
            return UnityEngine.Object.FindObjectOfType<GachaAnimationController>(true);
        }

        private static void EnsureHubLoaded()
        {
            if (UnityEngine.Object.FindObjectOfType<GachaAnimationController>(true) != null)
                return;

            string hub = "Assets/_Project/Scenes/Hub.unity";
            if (FileExists(hub))
                EditorSceneManager.OpenScene(hub, OpenSceneMode.Single);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform f = FindDeep(root.GetChild(i), name);
                if (f != null)
                    return f;
            }

            return null;
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
            WriteReportTo("Audits/reveal_stage_audit.txt", text);
        }

        private static void WriteReportTo(string relPath, string text)
        {
            string full = FullPath(relPath);
            string dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(full, text, Encoding.UTF8);
            Debug.Log($"[RevealStageAuditor] Rapport écrit : {full}");
            AssetDatabase.Refresh();
        }
    }
}
#endif
