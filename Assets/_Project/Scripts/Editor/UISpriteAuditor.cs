#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Inventaire en lecture seule des sprites UI : réglages d'import, GUID, références.
    /// Ne modifie, ne réimporte et ne supprime aucun asset.
    /// </summary>
    public static class UISpriteAuditor
    {
        private const string UIFolder = "Assets/_Project/Sprites/UI/";
        private const string ReportPath = "Assets/_Project/audit_ui_sprites.txt";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════
        [MenuItem("Take Five Games/Art/Auditer sprites UI")]
        public static void AuditUISprites()
        {
            string folder = UIFolder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog("Dossier introuvable",
                    $"Le dossier '{folder}' n'existe pas. Ajuste la constante UIFolder ou crée le dossier.", "OK");
                return;
            }

            string[] spriteGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            var spritePaths = new List<string>(spriteGuids.Length);
            for (int i = 0; i < spriteGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(spriteGuids[i]);
                if (!string.IsNullOrEmpty(path))
                    spritePaths.Add(path);
            }

            spritePaths.Sort();

            Dictionary<string, List<string>> referencersBySprite = BuildReferencerMap(spritePaths);
            var report = new StringBuilder(spritePaths.Count * 512);
            int orphanCount = 0;
            int referencedCount = 0;

            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine("AUDIT SPRITES UI — lecture seule");
            report.AppendLine($"Dossier : {UIFolder}");
            report.AppendLine($"Généré  : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine();

            var consoleTable = new StringBuilder();
            consoleTable.AppendLine("[UI Audit] ── Sprites ─────────────────────────────────────────");
            consoleTable.AppendLine($"{"Fichier",-40} {"Taille",10} {"Type",-8} {"Filter",-10} {"Mesh",-10} {"Refs",5}");
            consoleTable.AppendLine(new string('─', 90));

            for (int i = 0; i < spritePaths.Count; i++)
            {
                string path = spritePaths[i];
                string fileName = Path.GetFileName(path);
                string guid = AssetDatabase.AssetPathToGUID(path);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var settings = new TextureImporterSettings();
                if (importer != null)
                    importer.ReadTextureSettings(settings);

                int width = 0;
                int height = 0;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    width = texture.width;
                    height = texture.height;
                }

                string textureType = importer != null ? importer.textureType.ToString() : "N/A";
                string importMode = importer != null ? importer.spriteImportMode.ToString() : "N/A";
                string filterMode = importer != null ? importer.filterMode.ToString() : "N/A";
                bool mipmaps = importer != null && importer.mipmapEnabled;
                string meshType = importer != null ? settings.spriteMeshType.ToString() : "N/A";
                Vector4 border = importer != null ? importer.spriteBorder : Vector4.zero;

                referencersBySprite.TryGetValue(path, out List<string> referencers);
                referencers ??= new List<string>();
                int refCount = referencers.Count;

                if (refCount == 0)
                    orphanCount++;
                else
                    referencedCount++;

                // ── Section détaillée (fichier rapport) ──
                report.AppendLine($"── Sprite {i + 1}/{spritePaths.Count} ──────────────────────────────────────");
                report.AppendLine($"  Fichier           : {fileName}");
                report.AppendLine($"  Chemin            : {path}");
                report.AppendLine($"  GUID              : {guid}");
                report.AppendLine($"  Dimensions source : {width} x {height}");
                report.AppendLine($"  textureType       : {textureType}");
                report.AppendLine($"  spriteImportMode  : {importMode}");
                report.AppendLine($"  filterMode        : {filterMode}");
                report.AppendLine($"  mipmapEnabled     : {mipmaps}");
                report.AppendLine($"  spriteMeshType    : {meshType}");
                report.AppendLine($"  spriteBorder      : L={border.x:F1} B={border.y:F1} R={border.z:F1} T={border.w:F1}");
                report.AppendLine($"  Références        : {refCount}");

                if (refCount == 0)
                {
                    report.AppendLine("  (orphelin — aucun .prefab / .asset / .unity ne référence ce sprite)");
                }
                else
                {
                    report.AppendLine("  Assets référençant :");
                    referencers.Sort();
                    for (int r = 0; r < referencers.Count; r++)
                        report.AppendLine($"    - {referencers[r]}");
                }

                report.AppendLine();

                // ── Ligne tableau (console) ──
                string shortName = fileName.Length > 38 ? fileName.Substring(0, 35) + "..." : fileName;
                consoleTable.AppendLine(
                    $"{shortName,-40} {width}x{height,-6} {textureType,-8} {filterMode,-10} {meshType,-10} {refCount,5}");
            }

            // ── Résumé ──
            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine("RÉSUMÉ");
            report.AppendLine($"  Total sprites   : {spritePaths.Count}");
            report.AppendLine($"  Orphelins (0)   : {orphanCount}");
            report.AppendLine($"  Référencés (≥1) : {referencedCount}");
            report.AppendLine("═══════════════════════════════════════════════════════════════");

            string reportText = report.ToString();
            WriteReportFile(reportText);

            Debug.Log(consoleTable.ToString());
            Debug.Log($"[UI Audit] Total={spritePaths.Count} | Orphelins={orphanCount} | Référencés={referencedCount}");
            Debug.Log($"[UI Audit] Rapport détaillé écrit dans : {ReportPath}");
        }

        // ═══════════════════════════════════════════
        // RÉFÉRENCES (lecture seule)
        // ═══════════════════════════════════════════

        /// <summary>
        /// Pour chaque sprite UI, liste les .prefab / .asset / .unity qui en dépendent (direct).
        /// </summary>
        private static Dictionary<string, List<string>> BuildReferencerMap(List<string> spritePaths)
        {
            var map = new Dictionary<string, List<string>>(spritePaths.Count);
            for (int i = 0; i < spritePaths.Count; i++)
                map[spritePaths[i]] = new List<string>();

            string[] candidateGuids = AssetDatabase.FindAssets("", new[] { "Assets" });
            var spritePathSet = new HashSet<string>(spritePaths);

            for (int i = 0; i < candidateGuids.Length; i++)
            {
                string candidatePath = AssetDatabase.GUIDToAssetPath(candidateGuids[i]);
                if (string.IsNullOrEmpty(candidatePath))
                    continue;

                if (!IsReferencerCandidate(candidatePath))
                    continue;

                // Ne pas compter une auto-référence si le candidat est lui-même un sprite audité.
                if (spritePathSet.Contains(candidatePath))
                    continue;

                string[] dependencies = AssetDatabase.GetDependencies(candidatePath, false);
                for (int d = 0; d < dependencies.Length; d++)
                {
                    if (map.TryGetValue(dependencies[d], out List<string> referencers))
                        referencers.Add(candidatePath);
                }
            }

            return map;
        }

        private static bool IsReferencerCandidate(string path)
        {
            return path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary> Écrit uniquement le fichier rapport texte (hors pipeline sprites). </summary>
        private static void WriteReportFile(string content)
        {
            string relative = ReportPath.StartsWith("Assets/")
                ? ReportPath.Substring("Assets/".Length)
                : ReportPath;
            string fullPath = Path.Combine(Application.dataPath, relative.Replace('/', Path.DirectorySeparatorChar));
            File.WriteAllText(fullPath, content, Encoding.UTF8);
        }
    }
}
#endif
