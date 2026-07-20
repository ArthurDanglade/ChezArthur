#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère la texture de bruit dissolve et le matériau AwakeningDissolve (idempotent).
    /// </summary>
    public static class NoiseTextureGenerator
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int SEED = 12345;
        private const int TextureSize = 64;
        private const string ArtFxFolder = "Assets/_Project/Art/FX";
        private const string NoisePath = ArtFxFolder + "/noise_dissolve.png";
        private const string MaterialPath = ArtFxFolder + "/AwakeningDissolve.mat";
        private const string ShaderName = "ChezArthur/UI/AwakeningDissolve";

        [MenuItem("Chez Arthur/Art/Générer texture bruit dissolve")]
        public static void Generate()
        {
            EnsureFolder(ArtFxFolder);

            Texture2D noise = BuildNoiseTexture();
            byte[] png = noise.EncodeToPNG();
            Object.DestroyImmediate(noise);
            File.WriteAllBytes(NoisePath, png);
            AssetDatabase.ImportAsset(NoisePath, ImportAssetOptions.ForceUpdate);
            ConfigureNoiseImporter(NoisePath);

            Texture2D noiseAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath);
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[NoiseTextureGenerator] Shader introuvable : {ShaderName}");
                return;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetTexture("_NoiseTex", noiseAsset);
            mat.SetFloat("_DissolveAmount", 0f);
            mat.SetFloat("_EdgeWidth", 0.08f);
            mat.SetColor("_EdgeColor", UiTheme.Gold);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[NoiseTextureGenerator] Bruit + matériau OK : {NoisePath}, {MaterialPath}");
            EditorGUIUtility.PingObject(mat);
        }

        private static Texture2D BuildNoiseTexture()
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;

            var rng = new System.Random(SEED);
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float g = (float)rng.NextDouble();
                    tex.SetPixel(x, y, new Color(g, g, g, 1f));
                }
            }

            tex.Apply(false, false);
            return tex;
        }

        private static void ConfigureNoiseImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 64;
            importer.SaveAndReimport();
        }

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
    }
}
#endif
