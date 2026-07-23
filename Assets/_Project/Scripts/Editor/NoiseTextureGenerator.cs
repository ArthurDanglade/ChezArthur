#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère bruit dissolve, glows additifs et matériaux associés (idempotent).
    /// </summary>
    public static class NoiseTextureGenerator
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int SEED = 12345;
        private const int TextureSize = 64;
        private const int RadialSize = 256;
        private const int MoteSize = 32;
        private const int CardBloomW = 192;
        private const int CardBloomH = 448;
        private const int RaysSize = 256;
        private const string ArtFxFolder = "Assets/_Project/Art/FX";
        private const string NoisePath = ArtFxFolder + "/noise_dissolve.png";
        private const string MaterialPath = ArtFxFolder + "/AwakeningDissolve.mat";
        private const string RadialGlowPath = ArtFxFolder + "/fx_radial_glow.png";
        private const string MotePath = ArtFxFolder + "/fx_mote.png";
        private const string CardBloomPath = ArtFxFolder + "/fx_card_bloom.png";
        private const string RaysPath = ArtFxFolder + "/fx_rays.png";
        private const string GlowMaterialPath = ArtFxFolder + "/AwakeningGlow.mat";
        private const string DissolveShaderName = "ChezArthur/UI/AwakeningDissolve";
        private const string GlowShaderName = "ChezArthur/UI/AwakeningGlowAdditive";
        private const string PixelateShaderName = "ChezArthur/UI/GachaRevealPixelate";
        private const string PixelateMaterialPath = ArtFxFolder + "/GachaRevealPixelate.mat";
        private const string DoorContreJourShaderName = "ChezArthur/UI/GachaDoorContreJour";
        private const string DoorContreJourMaterialPath = ArtFxFolder + "/GachaDoorContreJour.mat";

        [MenuItem("Chez Arthur/Art/Générer texture bruit dissolve")]
        public static void Generate()
        {
            EnsureFolder(ArtFxFolder);

            // ── Bruit dissolve ──
            Texture2D noise = BuildNoiseTexture();
            WritePng(noise, NoisePath);
            Object.DestroyImmediate(noise);
            AssetDatabase.ImportAsset(NoisePath, ImportAssetOptions.ForceUpdate);
            ConfigureNoiseImporter(NoisePath);

            Texture2D noiseAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath);
            Shader dissolveShader = Shader.Find(DissolveShaderName);
            if (dissolveShader == null)
            {
                Debug.LogError($"[NoiseTextureGenerator] Shader introuvable : {DissolveShaderName}");
                return;
            }

            Material dissolveMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (dissolveMat == null)
            {
                dissolveMat = new Material(dissolveShader);
                AssetDatabase.CreateAsset(dissolveMat, MaterialPath);
            }
            else
            {
                dissolveMat.shader = dissolveShader;
            }

            dissolveMat.SetTexture("_NoiseTex", noiseAsset);
            dissolveMat.SetFloat("_DissolveAmount", 0f);
            dissolveMat.SetFloat("_EdgeWidth", 0.08f);
            dissolveMat.SetColor("_EdgeColor", UiTheme.Gold);
            dissolveMat.SetFloat("_Whiteout", 0f);
            dissolveMat.SetColor("_WhiteoutColor", UiTheme.CeremonyLight);
            EditorUtility.SetDirty(dissolveMat);

            // ── Glows additifs ──
            Texture2D radial = BuildRadialGlow(RadialSize);
            WritePng(radial, RadialGlowPath);
            Object.DestroyImmediate(radial);
            AssetDatabase.ImportAsset(RadialGlowPath, ImportAssetOptions.ForceUpdate);
            ConfigureGlowSpriteImporter(RadialGlowPath, RadialSize);

            Texture2D mote = BuildRadialGlow(MoteSize);
            WritePng(mote, MotePath);
            Object.DestroyImmediate(mote);
            AssetDatabase.ImportAsset(MotePath, ImportAssetOptions.ForceUpdate);
            ConfigureGlowSpriteImporter(MotePath, MoteSize);

            Texture2D bloom = BuildCardBloom(CardBloomW, CardBloomH);
            WritePng(bloom, CardBloomPath);
            Object.DestroyImmediate(bloom);
            AssetDatabase.ImportAsset(CardBloomPath, ImportAssetOptions.ForceUpdate);
            ConfigureGlowSpriteImporter(CardBloomPath, Mathf.Max(CardBloomW, CardBloomH));

            Texture2D rays = BuildRays(RaysSize);
            WritePng(rays, RaysPath);
            Object.DestroyImmediate(rays);
            AssetDatabase.ImportAsset(RaysPath, ImportAssetOptions.ForceUpdate);
            ConfigureGlowSpriteImporter(RaysPath, RaysSize);

            Shader glowShader = Shader.Find(GlowShaderName);
            if (glowShader == null)
            {
                Debug.LogError($"[NoiseTextureGenerator] Shader introuvable : {GlowShaderName}");
                return;
            }

            Material glowMat = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
            if (glowMat == null)
            {
                glowMat = new Material(glowShader);
                AssetDatabase.CreateAsset(glowMat, GlowMaterialPath);
            }
            else
            {
                glowMat.shader = glowShader;
            }

            EditorUtility.SetDirty(glowMat);

            // ── Pixelate reveal gacha (valeurs nettes par défaut) ──
            EnsureGachaRevealPixelateMaterial();
            EnsureGachaDoorContreJourMaterial();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[NoiseTextureGenerator] OK : {NoisePath}, {MaterialPath}, " +
                $"{RadialGlowPath}, {MotePath}, {CardBloomPath}, {RaysPath}, " +
                $"{GlowMaterialPath}, {PixelateMaterialPath}, {DoorContreJourMaterialPath}");
            EditorGUIUtility.PingObject(dissolveMat);
        }

        /// <summary>
        /// Crée / met à jour GachaRevealPixelate.mat (idempotent, défauts nets).
        /// </summary>
        private static void EnsureGachaRevealPixelateMaterial()
        {
            Shader pixelateShader = Shader.Find(PixelateShaderName);
            if (pixelateShader == null)
            {
                Debug.LogError(
                    $"[NoiseTextureGenerator] Shader introuvable : {PixelateShaderName}");
                return;
            }

            Material pixelateMat =
                AssetDatabase.LoadAssetAtPath<Material>(PixelateMaterialPath);
            if (pixelateMat == null)
            {
                pixelateMat = new Material(pixelateShader);
                AssetDatabase.CreateAsset(pixelateMat, PixelateMaterialPath);
            }
            else
            {
                pixelateMat.shader = pixelateShader;
            }

            pixelateMat.SetVector("_UvRect", new Vector4(0f, 0f, 1f, 1f));
            pixelateMat.SetFloat("_PixelSteps", 4096f);
            pixelateMat.SetFloat("_Saturation", 1f);
            EditorUtility.SetDirty(pixelateMat);
        }

        /// <summary>
        /// Matériau contre-jour porte gacha (noir flipbook → lumière progressive).
        /// </summary>
        private static void EnsureGachaDoorContreJourMaterial()
        {
            Shader shader = Shader.Find(DoorContreJourShaderName);
            if (shader == null)
            {
                Debug.LogError(
                    $"[NoiseTextureGenerator] Shader introuvable : {DoorContreJourShaderName}");
                return;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(DoorContreJourMaterialPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, DoorContreJourMaterialPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetFloat("_ContreJour", 0f);
            mat.SetFloat("_BlackThreshold", 0.12f);
            mat.SetColor("_LightColor", new Color(1f, 0.95f, 0.82f, 1f));
            mat.SetFloat("_LightBoost", 2.2f);
            EditorUtility.SetDirty(mat);
        }

        private static void WritePng(Texture2D tex, string path)
        {
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
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

        private static Texture2D BuildRadialGlow(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float half = size * 0.5f;
            float invHalf = 1f / half;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) * invHalf;
                    float dy = (y + 0.5f - half) * invHalf;
                    float d01 = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - d01), 2.2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply(false, false);
            return tex;
        }

        /// <summary>
        /// Cadre de lumière : vide au centre (rect arrondi), falloff vers l'extérieur.
        /// </summary>
        private static Texture2D BuildCardBloom(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float marginX = width * 0.22f;
            float marginY = height * 0.22f;
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;
            float boxHalfX = halfW - marginX;
            float boxHalfY = halfH - marginY;
            const float cornerR = 24f;

            float dMax = Mathf.Sqrt(marginX * marginX + marginY * marginY);
            if (dMax < 1f)
                dMax = 1f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f - halfW;
                    float py = y + 0.5f - halfH;
                    float sd = SdRoundedBox(px, py, boxHalfX, boxHalfY, cornerR);
                    float alpha = 0f;
                    if (sd > 0f)
                    {
                        float t = Mathf.Clamp01(1f - sd / dMax);
                        alpha = Mathf.Pow(t, 2f);
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply(false, false);
            return tex;
        }

        private static float SdRoundedBox(float px, float py, float halfX, float halfY, float r)
        {
            float qx = Mathf.Abs(px) - halfX + r;
            float qy = Mathf.Abs(py) - halfY + r;
            float outside = Mathf.Sqrt(
                Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) +
                Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outside + inside - r;
        }

        private static Texture2D BuildRays(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float half = size * 0.5f;
            float invHalf = 1f / half;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) * invHalf;
                    float dy = (y + 0.5f - half) * invHalf;
                    float d01 = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float beam = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(12f * angle)), 6f);
                    float alpha = beam * Mathf.Clamp01(1f - d01);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
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

        private static void ConfigureGlowSpriteImporter(string path, int maxSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = maxSize;
            importer.alphaIsTransparency = true;
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
