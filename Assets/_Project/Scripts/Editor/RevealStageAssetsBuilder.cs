#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using ChezArthur.UI.RevealStage;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Crée / met à jour EN PLACE RevealLight.mat + RevealStageConfig.asset (GUID intacts).
    /// </summary>
    public static class RevealStageAssetsBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string ArtFxFolder = "Assets/_Project/Art/FX";
        private const string DataUiFolder = "Assets/_Project/Data/UI";
        private const string MatPath = ArtFxFolder + "/RevealLight.mat";
        private const string ConfigPath = DataUiFolder + "/RevealStageConfig.asset";
        private const string ShaderName = "ChezArthur/UI/RevealLight";
        private const string ReportRelPath = "Audits/reveal_stage_build.txt";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Reveal/Créer ou Mettre à Jour les Assets INVR")]
        public static void BuildMenu() => Build();

        /// <summary>Point d'entrée idempotent (MenuItem + batchmode).</summary>
        public static void Build()
        {
            var report = new StringBuilder(4096);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" BUILD Reveal Stage INVR1");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();
            report.AppendLine("NOTE : socle AW intact — aucune modification ArtworkTransition.");
            report.AppendLine("NOTE : gacha vivant inchangé (dormance INVR1).");
            report.AppendLine();

            EnsureFolder(ArtFxFolder);
            EnsureFolder(DataUiFolder);

            Material mat = EnsureMaterial(report);
            RevealStageConfig cfg = EnsureConfig(report);

            if (mat != null)
            {
                mat.SetFloat("_DitherCellPx", 3f);
                mat.SetFloat("_FrontSoft", 0.10f);
                mat.SetFloat("_ShadowLevel", 0.62f);
                mat.SetFloat("_Dim", 1f);
                mat.SetFloat("_Snap", 0f);
                mat.SetFloat("_Flash", 0f);
                mat.SetFloat("_Vignette", 0f);
                mat.SetFloat("_LightR", 0f);
                mat.SetFloat("_LightB", 0f);
                mat.SetFloat("_AspectY", 1f);
                mat.SetVector("_RectMin", new Vector4(0f, 0f, 0f, 0f));
                mat.SetVector("_RectSize", new Vector4(1f, 1f, 0f, 0f));
                mat.SetVector("_FocalRect", new Vector4(0.5f, 0.5f, 0f, 0f));
                mat.SetColor("_Tint", Color.white);
                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.AppendLine();
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" Build terminé (idempotent). Relance = zéro changement attendu.");
            if (cfg != null)
                report.AppendLine($" Config : {ConfigPath}");
            if (mat != null)
                report.AppendLine($" Matériau : {MatPath}");
            report.AppendLine("═══════════════════════════════════════════");

            WriteReport(report);
            Debug.Log($"[RevealStageAssetsBuilder] OK — rapport : {ReportRelPath}");
            if (mat != null)
                EditorGUIUtility.PingObject(mat);
        }

        // ═══════════════════════════════════════════
        // CONFIG
        // ═══════════════════════════════════════════

        private static RevealStageConfig EnsureConfig(StringBuilder report)
        {
            RevealStageConfig existing =
                AssetDatabase.LoadAssetAtPath<RevealStageConfig>(ConfigPath);
            if (existing != null)
            {
                // Remet les défauts Bloc 2 (idempotent Go INVR0)
                ApplyBloc2Defaults(existing);
                EditorUtility.SetDirty(existing);
                report.AppendLine($"CONFIG : mise à jour défauts Bloc 2 → {ConfigPath}");
                return existing;
            }

            RevealStageConfig created = ScriptableObject.CreateInstance<RevealStageConfig>();
            ApplyBloc2Defaults(created);
            AssetDatabase.CreateAsset(created, ConfigPath);
            report.AppendLine($"CONFIG : créée avec défauts Bloc 2 → {ConfigPath}");
            return created;
        }

        private static void ApplyBloc2Defaults(RevealStageConfig c)
        {
            c.ditherCell = 4f;
            c.shadowLevel = 0.42f;
            c.cutDuration = 0.08f;
            c.frontSoft = 0.10f;
            c.vignette = 0.34f;
            c.exitDim = 0.28f;
            c.entryOverlap = 0.15f;

            c.entrySR = 1.40f; c.pulsesSR = 2; c.holdSR = 0f;
            c.snapSR = 0.24f; c.lightMaxSR = 0.24f; c.punchSR = 0.045f; c.partsSR = 30;

            c.entrySSR = 2.15f; c.pulsesSSR = 3; c.holdSSR = 0.28f;
            c.snapSSR = 0.30f; c.lightMaxSSR = 0.32f; c.punchSSR = 0.065f; c.partsSSR = 95;

            c.entryLR = 2.07f; c.pulsesLR = 4; c.holdLR = 0.38f;
            c.snapLR = 0.34f; c.lightMaxLR = 0.36f; c.punchLR = 0.075f; c.partsLR = 140;

            c.fakeHold = 0.18f;
            c.fakeCutBonus = 0.14f;

            c.nameDelay = 0.10f;
            c.nameDur = 0.25f;
            c.statusDelay = 1.15f;
            c.chipFill = 0.45f;
            c.tickStagger = 0.12f;
        }

        // ═══════════════════════════════════════════
        // MATÉRIAU
        // ═══════════════════════════════════════════

        private static Material EnsureMaterial(StringBuilder report)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                report.AppendLine($"MATÉRIAU : ÉCHEC shader introuvable « {ShaderName} »");
                Debug.LogError($"[RevealStageAssetsBuilder] Shader introuvable : {ShaderName}");
                return null;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MatPath);
                report.AppendLine($"MATÉRIAU : créé → {MatPath}");
            }
            else
            {
                mat.shader = shader;
                report.AppendLine($"MATÉRIAU : mis à jour → {MatPath}");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void WriteReport(StringBuilder report)
        {
            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, "reveal_stage_build.txt");
            File.WriteAllText(fullPath, report.ToString(), Encoding.UTF8);
            Debug.Log($"[RevealStageAssetsBuilder] Rapport écrit : {fullPath}");
        }
    }
}
#endif
