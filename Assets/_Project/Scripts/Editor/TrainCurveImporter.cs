#if UNITY_EDITOR
using System;
using System.IO;
using ChezArthur.Gacha;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Importe la courbe train gacha (JSON Dharu) vers TrainCurveData,
    /// et force le preset du sprite train_side si présent.
    /// </summary>
    public static class TrainCurveImporter
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string SourcesJsonRel = "ArtSources/Gacha/train_side_curve.json";
        private const string SourcesJsonGasha =
            "Assets/_Project/Sprites/Gasha/train_side_curve.json";
        private const string CurveAssetPath = "Assets/_Project/Data/Gacha/TrainCurve_Depart.asset";
        private const string TrainSpritePath = "Assets/_Project/Sprites/Gasha/train_side_sprite.png";
        private const string DataFolder = "Assets/_Project/Data/Gacha";

        // ═══════════════════════════════════════════
        // DTOs JSON
        // ═══════════════════════════════════════════
        [Serializable]
        private class TrainCurveDoc
        {
            public float frameDurationSec;
            public int canvasWidth;
            public int spriteWidth;
            public float spriteStartX;
            public TrainOffsetEntry[] offsets;
        }

        [Serializable]
        private class TrainOffsetEntry
        {
            public float t;
            public float dx;
        }

        // ═══════════════════════════════════════════
        // MENU / API
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Art/Importer courbe train gacha")]
        private static void ImportMenu()
        {
            ImportOrUpdate();
        }

        /// <summary>
        /// Import idempotent (menu + builder Gate 3). Retourne l'asset ou null.
        /// </summary>
        public static TrainCurveData ImportOrUpdate()
        {
            string jsonAbs = ResolveJsonAbsolutePath();
            if (jsonAbs == null)
            {
                Debug.LogError(
                    "[TrainCurveImporter] JSON introuvable. Cherché :\n" +
                    $"  - {SourcesJsonGasha}\n" +
                    $"  - {SourcesJsonRel}");
                return null;
            }

            string jsonText = File.ReadAllText(jsonAbs);
            TrainCurveDoc doc;
            try
            {
                doc = JsonUtility.FromJson<TrainCurveDoc>(jsonText);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[TrainCurveImporter] Parse JSON échoué : {ex.Message}");
                return null;
            }

            if (doc == null || doc.offsets == null || doc.offsets.Length == 0)
            {
                Debug.LogError(
                    "[TrainCurveImporter] offsets vide ou absents — import annulé.");
                return null;
            }

            AnimationCurve curve = BuildLinearCurve(doc.offsets);
            EnsureAssetFolder(DataFolder);

            TrainCurveData asset =
                AssetDatabase.LoadAssetAtPath<TrainCurveData>(CurveAssetPath);

            if (asset != null)
            {
                asset.EditorInitialize(curve, doc.spriteWidth, doc.canvasWidth);
                EditorUtility.SetDirty(asset);
            }
            else
            {
                asset = ScriptableObject.CreateInstance<TrainCurveData>();
                asset.EditorInitialize(curve, doc.spriteWidth, doc.canvasWidth);
                AssetDatabase.CreateAsset(asset, CurveAssetPath);
            }

            ApplyTrainSpritePresetIfPresent();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            float duration = asset.Duration;
            Debug.Log(
                $"[TrainCurveImporter] Terminé — {curve.length} clés | " +
                $"durée {duration:F3}s | spriteWidth {doc.spriteWidth}px → {CurveAssetPath}");
            return asset;
        }

        private static string ResolveJsonAbsolutePath()
        {
            string gashaAbs = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), SourcesJsonGasha));
            if (File.Exists(gashaAbs))
                return gashaAbs;

            string legacyAbs = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), SourcesJsonRel));
            if (File.Exists(legacyAbs))
                return legacyAbs;

            return null;
        }

        // ═══════════════════════════════════════════
        // COURBE
        // ═══════════════════════════════════════════

        private static AnimationCurve BuildLinearCurve(TrainOffsetEntry[] offsets)
        {
            AnimationCurve curve = new AnimationCurve();

            for (int i = 0; i < offsets.Length; i++)
            {
                TrainOffsetEntry entry = offsets[i];
                if (entry == null)
                    continue;

                curve.AddKey(new Keyframe(entry.t, entry.dx));
            }

            // Tangentes linéaires : préserve l'espacement exact de Dharu.
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, i, AnimationUtility.TangentMode.Linear);
            }

            return curve;
        }

        // ═══════════════════════════════════════════
        // SPRITE TRAIN
        // ═══════════════════════════════════════════

        private static void ApplyTrainSpritePresetIfPresent()
        {
            if (!File.Exists(
                    Path.GetFullPath(
                        Path.Combine(Directory.GetCurrentDirectory(), TrainSpritePath))))
            {
                // Vérifie aussi via AssetDatabase (fichier peut exister côté Unity).
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(TrainSpritePath) == null)
                {
                    Debug.LogWarning(
                        $"[TrainCurveImporter] Sprite train absent — attendu : {TrainSpritePath}");
                    return;
                }
            }

            TextureImporter importer =
                AssetImporter.GetAtPath(TrainSpritePath) as TextureImporter;

            if (importer == null)
            {
                Debug.LogWarning(
                    $"[TrainCurveImporter] Sprite train absent — attendu : {TrainSpritePath}");
                return;
            }

            // Preset Sprite UI pixel art — PPU existant non touché.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;

            TextureImporterPlatformSettings settings =
                importer.GetDefaultPlatformTextureSettings();
            settings.format = TextureImporterFormat.RGBA32;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(settings);

            importer.SaveAndReimport();
        }

        // ═══════════════════════════════════════════
        // DOSSIERS
        // ═══════════════════════════════════════════

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;

            string[] parts = assetFolder.Split('/');
            if (parts.Length < 2)
                return;

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
