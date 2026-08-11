#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Réimport idles U1 + bake noir→alpha (fonds sheets Dharu) + rapport Markdown.
    /// </summary>
    public static class IdleSheetImportTool
    {
        private const string IdleFolder = "Assets/_Project/Art/Combat/Enemies/U1/Idle";
        private const string AuditsFolder = "Audits";
        /// <summary> Seuil RGB : sous ce niveau → transparent. </summary>
        private const byte BlackRgbThreshold = 14;

        [MenuItem("Chez Arthur/Art/Nettoyer fonds idles U1 (noir→alpha)")]
        public static void BakeBlackToAlphaAndReimport()
        {
            if (!AssetDatabase.IsValidFolder(IdleFolder))
            {
                Debug.LogError("[IdleSheetImportTool] Dossier introuvable : " + IdleFolder);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IdleFolder });
            int baked = 0;
            int skipped = 0;
            long totalCleared = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (TryBakeBlackToAlpha(path, out int cleared))
                {
                    baked++;
                    totalCleared += cleared;
                    Debug.Log(
                        "[IdleSheetImportTool] noir→alpha : " + Path.GetFileName(path) +
                        " — " + cleared + " px → " +
                        CombatSpriteImportPostprocessor.GetProjectAbsolutePath(path));
                }
                else
                {
                    skipped++;
                }
            }

            AssetDatabase.Refresh();
            ReimportIdleSheetsWithReport();

            Debug.Log(
                "[IdleSheetImportTool] Bake terminé — sheets touchées=" + baked +
                ", inchangées=" + skipped +
                ", px nettoyés=" + totalCleared);
        }

        [MenuItem("Chez Arthur/Art/Réimporter les idles U1 (rapport)")]
        public static void ReimportIdleSheetsWithReport()
        {
            if (!AssetDatabase.IsValidFolder(IdleFolder))
            {
                Debug.LogError("[IdleSheetImportTool] Dossier introuvable : " + IdleFolder);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IdleFolder });
            var report = new StringBuilder(2048);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            report.AppendLine("# Rapport idles U1 — " + stamp);
            report.AppendLine();
            report.AppendLine("| Sheet | Dimensions | Frame | N_utile | Mode | Sous-sprites |");
            report.AppendLine("|---|---|---:|---:|---|---|");

            int count = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                        continue;

                    CombatSpriteImportPostprocessor.ApplyIdleSheetPreset(importer);
                    CombatSpriteImportPostprocessor.SliceIdleSheet(importer, path);
                    importer.SaveAndReimport();
                    count++;

                    AppendRow(report, path, importer);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (!Directory.Exists(AuditsFolder))
                Directory.CreateDirectory(AuditsFolder);

            string reportPath = Path.Combine(AuditsFolder, "IdleSheets_" + stamp + ".md");
            File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);

            Debug.Log(
                "[IdleSheetImportTool] Réimport de " + count + " sheet(s). Rapport : " + reportPath +
                "\n" + report);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Réécrit le PNG : pixels RGB sous seuil → alpha 0. Chemin projet absolu (pas cwd).
        /// </summary>
        private static bool TryBakeBlackToAlpha(string assetPath, out int clearedPixels)
        {
            clearedPixels = 0;
            string fullPath = CombatSpriteImportPostprocessor.GetProjectAbsolutePath(assetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError("[IdleSheetImportTool] Fichier introuvable : " + fullPath);
                return false;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes, false))
            {
                UnityEngine.Object.DestroyImmediate(tex);
                return false;
            }

            Color32[] pixels = tex.GetPixels32();
            int cleared = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                if (p.a == 0)
                    continue;
                if (p.r < BlackRgbThreshold && p.g < BlackRgbThreshold && p.b < BlackRgbThreshold)
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                    cleared++;
                }
            }

            if (cleared == 0)
            {
                UnityEngine.Object.DestroyImmediate(tex);
                return false;
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            if (png == null || png.Length == 0)
                return false;

            File.WriteAllBytes(fullPath, png);
            clearedPixels = cleared;
            return true;
        }

        private static void AppendRow(StringBuilder report, string path, TextureImporter importer)
        {
            string name = Path.GetFileName(path);
            SpriteRect[] sheet = GetSpriteRects(importer);
            int nUtile = sheet != null ? sheet.Length : 0;
            int fw = nUtile > 0 ? Mathf.RoundToInt(sheet[0].rect.width) : 0;
            int fh = nUtile > 0 ? Mathf.RoundToInt(sheet[0].rect.height) : 0;

            string fullPath = CombatSpriteImportPostprocessor.GetProjectAbsolutePath(path);
            int w = 0;
            int h = 0;
            if (File.Exists(fullPath))
            {
                byte[] fileBytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(fileBytes, false))
                {
                    w = tex.width;
                    h = tex.height;
                }

                UnityEngine.Object.DestroyImmediate(tex);
            }

            var names = new StringBuilder();
            for (int i = 0; i < nUtile; i++)
            {
                if (i > 0)
                    names.Append(", ");
                names.Append(sheet[i].name);
            }

            report.Append("| `");
            report.Append(name);
            report.Append("` | ");
            report.Append(w);
            report.Append('×');
            report.Append(h);
            report.Append(" | ");
            report.Append(fw);
            report.Append('×');
            report.Append(fh);
            report.Append(" | ");
            report.Append(nUtile);
            report.Append(" | equal-split | ");
            report.Append(names);
            report.AppendLine(" |");
        }

        private static SpriteRect[] GetSpriteRects(TextureImporter importer)
        {
            if (importer == null)
                return Array.Empty<SpriteRect>();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider =
                factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
                return Array.Empty<SpriteRect>();

            dataProvider.InitSpriteEditorDataProvider();
            SpriteRect[] rects = dataProvider.GetSpriteRects();
            return rects ?? Array.Empty<SpriteRect>();
        }
    }
}
#endif
