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
    /// Génère textures, matériaux, config (si absente) et prefab ArtworkTransitionStage (idempotent).
    /// </summary>
    public static class ArtworkTransitionAssetsBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string ArtFxFolder = "Assets/_Project/Art/FX";
        private const string DataUiFolder = "Assets/_Project/Data/UI";
        private const string PrefabUiFolder = "Assets/_Project/Prefabs/UI";

        private const string NoisePath = ArtFxFolder + "/ArtworkNoise.png";
        private const string GlowPath = ArtFxFolder + "/AwGlowSoft.png";
        private const string RaysPath = ArtFxFolder + "/AwRays.png";
        private const string VignettePath = ArtFxFolder + "/AwVignette.png";
        private const string TransitionMatPath = ArtFxFolder + "/ArtworkTransition.mat";
        private const string AdditiveMatPath = ArtFxFolder + "/AwAdditive.mat";
        private const string ConfigPath = DataUiFolder + "/ArtworkTransitionConfig.asset";
        private const string PrefabPath = PrefabUiFolder + "/ArtworkTransitionStage.prefab";
        private const string ReportRelPath = "Audits/artwork_transition_build.txt";

        private const string TransitionShaderName = "ChezArthur/UI/ArtworkTransition";
        private const string AdditiveShaderName = "ChezArthur/UI/AdditiveTint";

        private const int GlowSize = 64;
        private const int RaysSize = 512;
        private const int VignetteSize = 256;
        private const int SoftRayCount = 24;
        private const int ThinRayCount = 7;
        private const int RaysSeed = 42;

        private static readonly Color WarmWhite = new Color(1f, 0.97f, 0.90f, 1f);
        private static readonly Color FlashWarm = new Color(1f, 0.973f, 0.918f, 0f);

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI/Construire assets Transitions Artwork (AW1)")]
        public static void BuildMenu()
        {
            Build();
        }

        /// <summary>
        /// Point d'entrée idempotent (MenuItem + appel scripté).
        /// </summary>
        public static void Build()
        {
            var report = new StringBuilder(4096);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" BUILD Artwork Transition AW1");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();
            report.AppendLine(
                "NOTE : pas de dossier Materials dédié sous _Project — " +
                "matériaux écrits dans Art/FX/ (amendement A).");
            report.AppendLine();

            EnsureFolder(ArtFxFolder);
            EnsureFolder(DataUiFolder);
            EnsureFolder(PrefabUiFolder);

            // ── Config (créer seulement si absent) ──
            ArtworkTransitionConfig config = EnsureConfig(report);
            int seed = config != null ? config.noiseSeed : ArtworkNoise.DEFAULT_SEED;
            if (seed == 0)
                seed = ArtworkNoise.DEFAULT_SEED;

            // ── Textures ──
            WriteArtworkNoise(seed, report);
            WriteAwGlowSoft(report);
            WriteAwRays(report);
            WriteAwVignette(report);

            AssetDatabase.Refresh();

            Texture2D noiseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath);
            Texture2D glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GlowPath);
            Texture2D raysTex = AssetDatabase.LoadAssetAtPath<Texture2D>(RaysPath);
            Sprite vignetteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);

            // ── Matériaux ──
            Material transitionMat = EnsureMaterial(
                TransitionMatPath, TransitionShaderName, report, "ArtworkTransition.mat");
            if (transitionMat != null && noiseTex != null)
            {
                transitionMat.SetTexture("_NoiseTex", noiseTex);
                EditorUtility.SetDirty(transitionMat);
            }

            Material additiveMat = EnsureMaterial(
                AdditiveMatPath, AdditiveShaderName, report, "AwAdditive.mat");

            // ── Prefab ──
            BuildPrefab(
                transitionMat, additiveMat, noiseTex, glowTex, raysTex,
                vignetteSprite, config, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.AppendLine();
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" Build terminé (idempotent).");
            report.AppendLine("═══════════════════════════════════════════");

            WriteReport(report);
            Debug.Log($"[ArtworkTransitionAssetsBuilder] OK — rapport : {ReportRelPath}");
            if (transitionMat != null)
                EditorGUIUtility.PingObject(transitionMat);
        }

        // ═══════════════════════════════════════════
        // CONFIG
        // ═══════════════════════════════════════════

        private static ArtworkTransitionConfig EnsureConfig(StringBuilder report)
        {
            ArtworkTransitionConfig existing =
                AssetDatabase.LoadAssetAtPath<ArtworkTransitionConfig>(ConfigPath);
            if (existing != null)
            {
                report.AppendLine($"CONFIG : conservée (non écrasée) → {ConfigPath}");
                report.AppendLine($"  noiseSeed = {existing.noiseSeed}");
                return existing;
            }

            ArtworkTransitionConfig created = ScriptableObject.CreateInstance<ArtworkTransitionConfig>();
            AssetDatabase.CreateAsset(created, ConfigPath);
            report.AppendLine($"CONFIG : créée avec défauts CreateInstance → {ConfigPath}");
            report.AppendLine($"  noiseSeed = {created.noiseSeed}");
            return created;
        }

        // ═══════════════════════════════════════════
        // TEXTURES
        // ═══════════════════════════════════════════

        private static void WriteArtworkNoise(int seed, StringBuilder report)
        {
            float[] field = ArtworkNoise.Generate(seed);
            int size = ArtworkNoise.SIZE;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            // y=0 en bas — index y * SIZE + x, PAS de flip
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float g = field[y * size + x];
                    byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(g * 255f), 0, 255);
                    pixels[y * size + x] = new Color32(b, b, b, 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            WritePng(tex, NoisePath);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(NoisePath, ImportAssetOptions.ForceUpdate);
            ConfigureNoiseImporter(NoisePath, size);
            report.AppendLine($"TEXTURE : {NoisePath} ({size}², seed={seed}, Point/Repeat, sRGB off)");
        }

        private static void WriteAwGlowSoft(StringBuilder report)
        {
            var tex = new Texture2D(GlowSize, GlowSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[GlowSize * GlowSize];
            float half = GlowSize * 0.5f;
            float invHalf = 1f / half;

            for (int y = 0; y < GlowSize; y++)
            {
                for (int x = 0; x < GlowSize; x++)
                {
                    float dx = (x + 0.5f - half) * invHalf;
                    float dy = (y + 0.5f - half) * invHalf;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha;
                    if (d >= 1f)
                        alpha = 0f;
                    else if (d <= 0.35f)
                        alpha = Mathf.Lerp(1f, 0.45f, d / 0.35f);
                    else
                        alpha = Mathf.Lerp(0.45f, 0f, (d - 0.35f) / 0.65f);

                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
                    pixels[y * GlowSize + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            WritePng(tex, GlowPath);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(GlowPath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImporter(GlowPath, GlowSize);
            report.AppendLine($"TEXTURE : {GlowPath} ({GlowSize}² radial, Sprite/Bilinear/Clamp)");
        }

        private static void WriteAwRays(StringBuilder report)
        {
            var tex = new Texture2D(RaysSize, RaysSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[RaysSize * RaysSize];
            float half = RaysSize * 0.5f;
            float invHalf = 1f / half;
            var rng = new System.Random(RaysSeed);

            // Pré-calcule angles / longueurs
            var softAngles = new float[SoftRayCount];
            var softLengths = new float[SoftRayCount];
            for (int i = 0; i < SoftRayCount; i++)
            {
                softAngles[i] = i * (Mathf.PI * 2f / SoftRayCount)
                    + ((float)rng.NextDouble() - 0.5f) * 0.12f;
                softLengths[i] = 0.55f + (float)rng.NextDouble() * 0.40f;
            }

            var thinAngles = new float[ThinRayCount];
            var thinLengths = new float[ThinRayCount];
            for (int i = 0; i < ThinRayCount; i++)
            {
                thinAngles[i] = i * (Mathf.PI * 2f / ThinRayCount)
                    + ((float)rng.NextDouble() - 0.5f) * 0.08f;
                thinLengths[i] = 0.82f + (float)rng.NextDouble() * 0.16f;
            }

            byte wr = (byte)Mathf.RoundToInt(WarmWhite.r * 255f);
            byte wg = (byte)Mathf.RoundToInt(WarmWhite.g * 255f);
            byte wb = (byte)Mathf.RoundToInt(WarmWhite.b * 255f);

            for (int y = 0; y < RaysSize; y++)
            {
                for (int x = 0; x < RaysSize; x++)
                {
                    float dx = (x + 0.5f - half) * invHalf;
                    float dy = (y + 0.5f - half) * invHalf;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float alpha = 0f;

                    for (int i = 0; i < SoftRayCount; i++)
                    {
                        float contrib = RayContribution(
                            angle, dist, softAngles[i], softLengths[i], 0.055f, 0.20f);
                        if (contrib > alpha)
                            alpha = contrib;
                    }

                    for (int i = 0; i < ThinRayCount; i++)
                    {
                        float contrib = RayContribution(
                            angle, dist, thinAngles[i], thinLengths[i], 0.018f, 0.34f);
                        if (contrib > alpha)
                            alpha = contrib;
                    }

                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
                    pixels[y * RaysSize + x] = new Color32(wr, wg, wb, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            WritePng(tex, RaysPath);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(RaysPath, ImportAssetOptions.ForceUpdate);
            ConfigureDefaultGlowImporter(RaysPath, RaysSize);
            report.AppendLine(
                $"TEXTURE : {RaysPath} ({RaysSize}², {SoftRayCount} soft + {ThinRayCount} thin, Default/Bilinear/Clamp)");
        }

        private static float RayContribution(
            float angle, float dist, float rayAngle, float length, float halfWidth, float peakAlpha)
        {
            if (dist <= 0.001f || dist >= length)
                return 0f;

            float dAng = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, rayAngle * Mathf.Rad2Deg))
                * Mathf.Deg2Rad;
            float along = dist / length;
            float width = halfWidth * (1f - along * 0.35f);
            if (width < 0.004f)
                width = 0.004f;

            float beam = 1f - Mathf.Clamp01(dAng / width);
            if (beam <= 0f)
                return 0f;

            beam *= beam;
            float tip = 1f - along;
            tip *= tip;
            return beam * tip * peakAlpha;
        }

        private static void WriteAwVignette(StringBuilder report)
        {
            var tex = new Texture2D(VignetteSize, VignetteSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[VignetteSize * VignetteSize];
            float inv = 1f / VignetteSize;
            // Ellipse centrée à 45 % de hauteur
            const float cx = 0.5f;
            const float cy = 0.45f;
            const float rx = 0.52f;
            const float ry = 0.62f;

            for (int y = 0; y < VignetteSize; y++)
            {
                for (int x = 0; x < VignetteSize; x++)
                {
                    float u = (x + 0.5f) * inv;
                    float v = (y + 0.5f) * inv;
                    float nx = (u - cx) / rx;
                    float ny = (v - cy) / ry;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    // Transparent au centre, opaque (noir) aux bords
                    float alpha = 0f;
                    if (d > 0.55f)
                    {
                        float t = Mathf.Clamp01((d - 0.55f) / 0.55f);
                        alpha = t * t;
                    }

                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
                    pixels[y * VignetteSize + x] = new Color32(0, 0, 0, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            WritePng(tex, VignettePath);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(VignettePath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImporter(VignettePath, VignetteSize);
            report.AppendLine($"TEXTURE : {VignettePath} ({VignetteSize}² ellipse cy=45%, Sprite/Bilinear/Clamp)");
        }

        // ═══════════════════════════════════════════
        // MATÉRIAUX
        // ═══════════════════════════════════════════

        private static Material EnsureMaterial(
            string path, string shaderName, StringBuilder report, string label)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                report.AppendLine($"MATÉRIAU : ÉCHEC shader introuvable « {shaderName} » ({label})");
                Debug.LogError($"[ArtworkTransitionAssetsBuilder] Shader introuvable : {shaderName}");
                return null;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
                report.AppendLine($"MATÉRIAU : créé → {path}");
            }
            else
            {
                mat.shader = shader;
                report.AppendLine($"MATÉRIAU : mis à jour → {path}");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ═══════════════════════════════════════════
        // PREFAB
        // ═══════════════════════════════════════════

        private static void BuildPrefab(
            Material transitionMat,
            Material additiveMat,
            Texture2D noiseTex,
            Texture2D glowTex,
            Texture2D raysTex,
            Sprite vignetteSprite,
            ArtworkTransitionConfig config,
            StringBuilder report)
        {
            GameObject root = new GameObject("ArtworkTransitionStage", typeof(RectTransform));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            ArtworkTransitionView view = root.AddComponent<ArtworkTransitionView>();
            ArtworkTransitionDriver driver = root.AddComponent<ArtworkTransitionDriver>();

            // Shaker
            RectTransform shaker = CreateChild(rootRt, "Shaker");
            StretchFull(shaker);

            RawImage raysA = CreateRawChild(shaker, "RaysA", raysTex, additiveMat, Color.white);
            RawImage raysB = CreateRawChild(
                shaker, "RaysB", raysTex, additiveMat, new Color(1f, 1f, 1f, 0.6f));
            RawImage halo = CreateRawChild(shaker, "Halo", glowTex, additiveMat, Color.white);

            // Card — centre, hauteur ~62 % ref 640 → 397, aspect 3:4
            RectTransform cardRt = CreateChild(shaker, "Card");
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(297.6f, 396.8f);
            cardRt.anchoredPosition = Vector2.zero;
            ArtworkTransitionGraphic card = cardRt.gameObject.AddComponent<ArtworkTransitionGraphic>();
            card.raycastTarget = false;

            RectTransform ashRt = CreateChild(shaker, "ParticlesAsh");
            StretchFull(ashRt);
            PixelParticleGraphic particlesAsh = ashRt.gameObject.AddComponent<PixelParticleGraphic>();
            particlesAsh.raycastTarget = false;
            // Matériau UI par défaut (null)

            RectTransform energyRt = CreateChild(shaker, "ParticlesEnergy");
            StretchFull(energyRt);
            PixelParticleGraphic particlesEnergy =
                energyRt.gameObject.AddComponent<PixelParticleGraphic>();
            particlesEnergy.raycastTarget = false;
            if (additiveMat != null)
                particlesEnergy.material = additiveMat;

            // Vignette + Flash (frères du Shaker — hors shake)
            Image vignette = CreateImageChild(rootRt, "Vignette", vignetteSprite, Color.black);
            StretchFull(vignette.rectTransform);
            vignette.color = new Color(0f, 0f, 0f, 0f);

            Image flash = CreateImageChild(rootRt, "Flash", null, FlashWarm);
            StretchFull(flash.rectTransform);
            flash.color = FlashWarm;

            // Câblage SerializedObject — View
            SerializedObject viewSo = new SerializedObject(view);
            viewSo.FindProperty("shaker").objectReferenceValue = shaker;
            viewSo.FindProperty("raysA").objectReferenceValue = raysA;
            viewSo.FindProperty("raysB").objectReferenceValue = raysB;
            viewSo.FindProperty("halo").objectReferenceValue = halo;
            viewSo.FindProperty("card").objectReferenceValue = card;
            viewSo.FindProperty("particlesAsh").objectReferenceValue = particlesAsh;
            viewSo.FindProperty("particlesEnergy").objectReferenceValue = particlesEnergy;
            viewSo.FindProperty("vignette").objectReferenceValue = vignette;
            viewSo.FindProperty("flash").objectReferenceValue = flash;
            viewSo.FindProperty("stageRoot").objectReferenceValue = rootRt;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            // Card material + noise
            SerializedObject cardSo = new SerializedObject(card);
            cardSo.FindProperty("sharedMaterial").objectReferenceValue = transitionMat;
            cardSo.FindProperty("noiseTexture").objectReferenceValue = noiseTex;
            cardSo.FindProperty("m_RaycastTarget").boolValue = false;
            cardSo.ApplyModifiedPropertiesWithoutUndo();

            // Particules glow
            SerializedObject ashSo = new SerializedObject(particlesAsh);
            ashSo.FindProperty("glowTexture").objectReferenceValue = glowTex;
            ashSo.FindProperty("m_RaycastTarget").boolValue = false;
            ashSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject energySo = new SerializedObject(particlesEnergy);
            energySo.FindProperty("glowTexture").objectReferenceValue = glowTex;
            energySo.FindProperty("m_RaycastTarget").boolValue = false;
            if (additiveMat != null)
                energySo.FindProperty("m_Material").objectReferenceValue = additiveMat;
            energySo.ApplyModifiedPropertiesWithoutUndo();

            // Driver
            SerializedObject driverSo = new SerializedObject(driver);
            driverSo.FindProperty("view").objectReferenceValue = view;
            driverSo.FindProperty("config").objectReferenceValue = config;
            driverSo.ApplyModifiedPropertiesWithoutUndo();

            // Harness volontairement ABSENT du prefab

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            if (saved != null)
                report.AppendLine($"PREFAB : écrit → {PrefabPath} (View+Driver, sans harness)");
            else
                report.AppendLine($"PREFAB : ÉCHEC écriture → {PrefabPath}");
        }

        // ═══════════════════════════════════════════
        // HELPERS UI
        // ═══════════════════════════════════════════

        private static RectTransform CreateChild(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static RawImage CreateRawChild(
            RectTransform parent, string name, Texture tex, Material mat, Color color)
        {
            RectTransform rt = CreateChild(parent, name);
            StretchFull(rt);
            RawImage raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = color;
            raw.raycastTarget = false;
            if (mat != null)
                raw.material = mat;
            return raw;
        }

        private static Image CreateImageChild(
            RectTransform parent, string name, Sprite sprite, Color color)
        {
            RectTransform rt = CreateChild(parent, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        // ═══════════════════════════════════════════
        // IMPORT / IO
        // ═══════════════════════════════════════════

        private static void WritePng(Texture2D tex, string path)
        {
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
        }

        private static void ConfigureNoiseImporter(string path, int maxSize)
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
            importer.maxTextureSize = maxSize;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }

        private static void ConfigureSpriteImporter(string path, int maxSize)
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

        private static void ConfigureDefaultGlowImporter(string path, int maxSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Default;
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

        private static void WriteReport(StringBuilder report)
        {
            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, "artwork_transition_build.txt");
            File.WriteAllText(fullPath, report.ToString(), Encoding.UTF8);
            Debug.Log($"[ArtworkTransitionAssetsBuilder] Rapport écrit : {fullPath}");
        }
    }
}
#endif
