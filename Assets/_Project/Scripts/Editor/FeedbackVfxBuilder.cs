#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChezArthur.Gameplay.Feedback;
using ChezArthur.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// F3-P2a/P2b — VFX one-shot groupe B + boucles d'état.
    /// Idempotent : SaveAsPrefabAsset SANS DeleteAsset (GUID conservé).
    /// </summary>
    public static class FeedbackVfxBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string ArtFolder = "Assets/_Project/Art/FX/Feedback";
        private const string PrefabFolder = "Assets/_Project/Prefabs/VFX/Feedback";
        private const string LoopFolder = "Assets/_Project/Resources/VFX/Feedback/Loops";
        private const string CatalogPath = "Assets/_Project/Data/Feedback/FeedbackCatalog.asset";
        private const string PlaceholderPath = PrefabFolder + "/FxPlaceholder.prefab";
        private const string GlowShaderName = "ChezArthur/UI/AwakeningGlowAdditive";
        private const int MatterSortingOrder = 12;
        private const int GlowSortingOrder = 13;
        private const int UnitSortingOrder = 10;

        private struct WireSpec
        {
            public FeedbackEventId EventId;
            public string PrefabName;
            public FeedbackCause Cause;
            public float Scale;
        }

        private static readonly WireSpec[] Wiring =
        {
            new WireSpec { EventId = FeedbackEventId.HealReceived, PrefabName = "FxHealMotes", Cause = FeedbackCause.Heal, Scale = 1f },
            new WireSpec { EventId = FeedbackEventId.BuffApplied, PrefabName = "FxChevronsUp", Cause = FeedbackCause.BuffUp, Scale = 1f },
            new WireSpec { EventId = FeedbackEventId.BuffExpired, PrefabName = "FxDissipate", Cause = FeedbackCause.BuffUp, Scale = 0.8f },
            new WireSpec { EventId = FeedbackEventId.DebuffApplied, PrefabName = "FxChevronsDown", Cause = FeedbackCause.DebuffDown, Scale = 1f },
            new WireSpec { EventId = FeedbackEventId.DebuffExpired, PrefabName = "FxDissipate", Cause = FeedbackCause.DebuffDown, Scale = 0.8f },
            new WireSpec { EventId = FeedbackEventId.ShieldGained, PrefabName = "FxShieldGain", Cause = FeedbackCause.Shield, Scale = 1f },
            new WireSpec { EventId = FeedbackEventId.ShieldAbsorbed, PrefabName = "FxShieldPulse", Cause = FeedbackCause.Shield, Scale = 0.8f },
            new WireSpec { EventId = FeedbackEventId.ShieldBroken, PrefabName = "FxShieldShatter", Cause = FeedbackCause.Shield, Scale = 1f },
            new WireSpec { EventId = FeedbackEventId.BurnApplied, PrefabName = "FxBurnFlare", Cause = FeedbackCause.Burn, Scale = 1f },
            new WireSpec { EventId = FeedbackEventId.BurnTick, PrefabName = "FxBurnFlare", Cause = FeedbackCause.Burn, Scale = 0.6f },
            new WireSpec { EventId = FeedbackEventId.BurnEnded, PrefabName = "FxDissipate", Cause = FeedbackCause.Burn, Scale = 0.7f },
            new WireSpec { EventId = FeedbackEventId.PoisonApplied, PrefabName = "FxPoisonSplash", Cause = FeedbackCause.Poison, Scale = 1f },
            new WireSpec { EventId = FeedbackEventId.PoisonTick, PrefabName = "FxPoisonSplash", Cause = FeedbackCause.Poison, Scale = 0.6f },
            new WireSpec { EventId = FeedbackEventId.PoisonEnded, PrefabName = "FxDissipate", Cause = FeedbackCause.Poison, Scale = 0.7f },
            new WireSpec { EventId = FeedbackEventId.StunApplied, PrefabName = "FxStunRing", Cause = FeedbackCause.Stun, Scale = 1f },
            new WireSpec { EventId = FeedbackEventId.StunEnded, PrefabName = "FxDissipate", Cause = FeedbackCause.Stun, Scale = 0.7f },
            new WireSpec { EventId = FeedbackEventId.FreezeApplied, PrefabName = "FxFreezeCrystals", Cause = FeedbackCause.Freeze, Scale = 1f },
            new WireSpec { EventId = FeedbackEventId.FreezeEnded, PrefabName = "FxFreezeShatter", Cause = FeedbackCause.Freeze, Scale = 1f },
        };

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Feedback/Générer VFX Groupe B (textures + prefabs + câblage)")]
        public static void GenerateAll()
        {
            var report = new StringBuilder();
            report.AppendLine("# Feedback VFX Groupe B");
            report.AppendLine();
            report.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine();

            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/FX");
            EnsureFolder(ArtFolder);
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/VFX");
            EnsureFolder(PrefabFolder);

            report.AppendLine("## Textures");
            WriteTexture("chevron", 8, DrawChevron, report);
            WriteTexture("croix", 8, DrawCross, report);
            WriteTexture("arc", 24, DrawArc, report);
            WriteTexture("eclat", 8, DrawShard, report);
            WriteTexture("goutte", 8, DrawDrop, report);
            WriteTexture("etoile", 8, DrawStar, report);
            WriteTexture("cristal", 8, DrawCrystal, report);
            WriteTexture("glow", 32, DrawGlow, report);

            report.AppendLine();
            report.AppendLine("## Matériaux");
            bool glowFallback = false;
            EnsureSpriteMat("chevron", report);
            EnsureSpriteMat("croix", report);
            EnsureSpriteMat("arc", report);
            EnsureSpriteMat("eclat", report);
            EnsureSpriteMat("goutte", report);
            EnsureSpriteMat("etoile", report);
            EnsureSpriteMat("cristal", report);
            glowFallback = EnsureGlowMat(report);

            report.AppendLine();
            report.AppendLine("## Prefabs");
            Dictionary<string, ParticleSystem> prefabs = new Dictionary<string, ParticleSystem>();
            prefabs["FxChevronsUp"] = SavePrefab("FxChevronsUp", BuildChevronsUp, report);
            prefabs["FxChevronsDown"] = SavePrefab("FxChevronsDown", BuildChevronsDown, report);
            prefabs["FxHealMotes"] = SavePrefab("FxHealMotes", BuildHealMotes, report);
            prefabs["FxShieldGain"] = SavePrefab("FxShieldGain", BuildShieldGain, report);
            prefabs["FxShieldPulse"] = SavePrefab("FxShieldPulse", BuildShieldPulse, report);
            prefabs["FxShieldShatter"] = SavePrefab("FxShieldShatter", BuildShieldShatter, report);
            prefabs["FxBurnFlare"] = SavePrefab("FxBurnFlare", BuildBurnFlare, report);
            prefabs["FxPoisonSplash"] = SavePrefab("FxPoisonSplash", BuildPoisonSplash, report);
            prefabs["FxStunRing"] = SavePrefab("FxStunRing", BuildStunRing, report);
            prefabs["FxFreezeCrystals"] = SavePrefab("FxFreezeCrystals", BuildFreezeCrystals, report);
            prefabs["FxFreezeShatter"] = SavePrefab("FxFreezeShatter", BuildFreezeShatter, report);
            prefabs["FxDissipate"] = SavePrefab("FxDissipate", BuildDissipate, report);

            report.AppendLine();
            report.AppendLine("## Câblage catalogue");
            WireCatalog(prefabs, report);

            report.AppendLine();
            report.AppendLine("## Sorting");
            report.AppendLine($"- Unités : {UnitSortingOrder}");
            report.AppendLine($"- Matière FX : {MatterSortingOrder}");
            report.AppendLine($"- Glow FX : {GlowSortingOrder}");
            if (glowFallback)
                report.AppendLine("- **WARNING** : shader glow projet introuvable → repli Legacy Particles/Additive");

            string auditsDir = Path.Combine(Application.dataPath, "..", "Audits");
            Directory.CreateDirectory(auditsDir);
            string reportPath = Path.Combine(auditsDir, $"FeedbackVfx_{DateTime.Now:yyyyMMdd_HHmm}.md");
            File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FeedbackVfxBuilder] Terminé. Rapport : {reportPath}");
        }

        [MenuItem("Chez Arthur/Feedback/Générer Boucles d'État (P2b)")]
        public static void GenerateStatusLoops()
        {
            var report = new StringBuilder();
            report.AppendLine("# Feedback Loops P2b");
            report.AppendLine();
            report.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine();

            EnsureFolder("Assets/_Project/Resources");
            EnsureFolder("Assets/_Project/Resources/VFX");
            EnsureFolder("Assets/_Project/Resources/VFX/Feedback");
            EnsureFolder(LoopFolder);

            // Réutilise matériaux P2a (aucune nouvelle texture).
            EnsureSpriteMat("eclat", report);
            EnsureSpriteMat("goutte", report);
            EnsureSpriteMat("arc", report);
            EnsureSpriteMat("etoile", report);
            EnsureSpriteMat("cristal", report);
            EnsureGlowMat(report);

            report.AppendLine();
            report.AppendLine("## Prefabs boucle");
            SaveLoopPrefab("LoopBurn", BuildLoopBurn, report);
            SaveLoopPrefab("LoopPoison", BuildLoopPoison, report);
            SaveLoopPrefab("LoopShield", BuildLoopShield, report);
            SaveLoopPrefab("LoopStun", BuildLoopStun, report);
            SaveLoopPrefab("LoopFreeze", BuildLoopFreeze, report);

            string auditsDir = Path.Combine(Application.dataPath, "..", "Audits");
            Directory.CreateDirectory(auditsDir);
            string reportPath = Path.Combine(auditsDir, $"FeedbackLoops_{DateTime.Now:yyyyMMdd_HHmm}.md");
            File.WriteAllText(reportPath, report.ToString(), Encoding.UTF8);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FeedbackVfxBuilder] Boucles P2b terminées. Rapport : {reportPath}");
        }

        // ═══════════════════════════════════════════
        // TEXTURES
        // ═══════════════════════════════════════════

        private delegate void PixelDrawer(Color32[] px, int size);

        private static void WriteTexture(string name, int size, PixelDrawer drawer, StringBuilder report)
        {
            string path = $"{ArtFolder}/tex_fx_{name}.png";
            bool existed = File.Exists(Path.Combine(Application.dataPath, "..", path.Replace('/', Path.DirectorySeparatorChar)));

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color32(0, 0, 0, 0);
            drawer(px, size);
            tex.SetPixels32(px);
            tex.Apply(false, false);

            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigurePointImporter(path);

            report.AppendLine($"- tex_fx_{name}.png : {(existed ? "MIS À JOUR" : "CRÉÉ")}");
        }

        private static void ConfigurePointImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.None;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void SetPx(Color32[] px, int size, int x, int y)
        {
            if (x < 0 || y < 0 || x >= size || y >= size) return;
            px[y * size + x] = new Color32(255, 255, 255, 255);
        }

        private static void DrawChevron(Color32[] px, int size)
        {
            // Chevron ^ 2 px — lignes (1,2)-(3,4)-(5,2) et parallèle
            for (int t = 0; t < 2; t++)
            {
                for (int i = 0; i <= 3; i++)
                {
                    SetPx(px, size, 1 + i, 2 + i - t);
                    SetPx(px, size, 7 - i, 2 + i - t);
                }
            }
        }

        private static void DrawCross(Color32[] px, int size)
        {
            for (int i = 1; i < size - 1; i++)
            {
                SetPx(px, size, 3, i);
                SetPx(px, size, 4, i);
                SetPx(px, size, i, 3);
                SetPx(px, size, i, 4);
            }
        }

        private static void DrawArc(Color32[] px, int size)
        {
            // Arc ~120° en haut, épaisseur 2
            float cx = (size - 1) * 0.5f;
            float cy = size * 0.55f;
            float rInner = size * 0.28f;
            float rOuter = size * 0.38f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                // Haut : angles ~30° à 150° en espace image (y up en texture Unity)
                if (ang < 30f || ang > 150f) continue;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d >= rInner && d <= rOuter)
                    SetPx(px, size, x, y);
            }
        }

        private static void DrawShard(Color32[] px, int size)
        {
            // Triangle effilé pointe haute
            for (int y = 1; y < size - 1; y++)
            {
                int half = Mathf.Max(0, (y - 1) / 2);
                for (int x = 3 - half; x <= 4 + half; x++)
                    SetPx(px, size, x, size - 1 - y);
            }
        }

        private static void DrawDrop(Color32[] px, int size)
        {
            // Rond bas + pointe haute
            for (int y = 0; y < 5; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - 3.5f;
                float dy = y - 2.5f;
                if (dx * dx + dy * dy <= 4.5f)
                    SetPx(px, size, x, y);
            }
            SetPx(px, size, 3, 5);
            SetPx(px, size, 4, 5);
            SetPx(px, size, 3, 6);
            SetPx(px, size, 4, 6);
            SetPx(px, size, 3, 7);
        }

        private static void DrawStar(Color32[] px, int size)
        {
            for (int i = 0; i < size; i++)
            {
                SetPx(px, size, 3, i);
                SetPx(px, size, 4, i);
                SetPx(px, size, i, 3);
                SetPx(px, size, i, 4);
            }
            SetPx(px, size, 1, 1);
            SetPx(px, size, 6, 1);
            SetPx(px, size, 1, 6);
            SetPx(px, size, 6, 6);
            SetPx(px, size, 2, 2);
            SetPx(px, size, 5, 2);
            SetPx(px, size, 2, 5);
            SetPx(px, size, 5, 5);
        }

        private static void DrawCrystal(Color32[] px, int size)
        {
            // Losange vertical
            int[] widths = { 0, 1, 2, 3, 3, 2, 1, 0 };
            for (int y = 0; y < size; y++)
            {
                int w = widths[y];
                for (int x = 4 - w; x <= 3 + w; x++)
                    SetPx(px, size, x, y);
            }
        }

        private static void DrawGlow(Color32[] px, int size)
        {
            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float maxR = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = Mathf.Clamp01(1f - d / maxR);
                a = a * a;
                byte alpha = (byte)Mathf.RoundToInt(a * 255f);
                px[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        // ═══════════════════════════════════════════
        // MATÉRIAUX
        // ═══════════════════════════════════════════

        private static Material EnsureSpriteMat(string form, StringBuilder report)
        {
            string path = $"{ArtFolder}/mat_fx_{form}.mat";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtFolder}/tex_fx_{form}.png");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader sh = Shader.Find("Sprites/Default");
            bool created = false;
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, path);
                created = true;
            }
            else
            {
                mat.shader = sh;
            }

            mat.mainTexture = tex;
            EditorUtility.SetDirty(mat);
            report.AppendLine($"- mat_fx_{form}.mat : {(created ? "CRÉÉ" : "MIS À JOUR")}");
            return mat;
        }

        private static bool EnsureGlowMat(StringBuilder report)
        {
            string path = $"{ArtFolder}/mat_fx_glow.mat";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtFolder}/tex_fx_glow.png");
            Shader sh = Shader.Find(GlowShaderName);
            bool fallback = false;
            if (sh == null)
            {
                sh = Shader.Find("Legacy Shaders/Particles/Additive");
                fallback = true;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = false;
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, path);
                created = true;
            }
            else
            {
                mat.shader = sh;
            }

            mat.mainTexture = tex;
            EditorUtility.SetDirty(mat);
            report.AppendLine($"- mat_fx_glow.mat : {(created ? "CRÉÉ" : "MIS À JOUR")}" + (fallback ? " (repli Additive)" : ""));
            return fallback;
        }

        // ═══════════════════════════════════════════
        // PREFABS
        // ═══════════════════════════════════════════

        private delegate GameObject PrefabBuilder();

        private static ParticleSystem SavePrefab(string name, PrefabBuilder builder, StringBuilder report)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;

            GameObject root = builder();
            // Pas de DeleteAsset — GUID conservé
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);

            GameObject prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            ParticleSystem ps = prefabGo != null ? prefabGo.GetComponent<ParticleSystem>() : null;
            report.AppendLine($"- {name}.prefab : {(existed ? "MIS À JOUR" : "CRÉÉ")}");
            return ps;
        }

        private static GameObject CreateRoot(string name, Material matterMat, float duration, bool withGlow)
        {
            GameObject go = new GameObject(name, typeof(ParticleSystem));
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            ConfigureCommonRoot(ps, duration);
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            ConfigureMatterRenderer(renderer, matterMat);

            if (withGlow)
                AddGlowChild(go, duration);

            return go;
        }

        private static void ConfigureCommonRoot(ParticleSystem ps, float duration)
        {
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.duration = duration;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = 64;
            main.startColor = Color.white;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        private static void ConfigureMatterRenderer(ParticleSystemRenderer renderer, Material mat)
        {
            renderer.enabled = true;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.sortingOrder = MatterSortingOrder;
            renderer.sharedMaterial = mat;
        }

        private static void AddGlowChild(GameObject root, float rootDuration)
        {
            Material glowMat = AssetDatabase.LoadAssetAtPath<Material>($"{ArtFolder}/mat_fx_glow.mat");
            GameObject child = new GameObject("Glow", typeof(ParticleSystem));
            child.transform.SetParent(root.transform, false);

            ParticleSystem ps = child.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.duration = Mathf.Min(0.35f, rootDuration);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.maxParticles = 4;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startColor = Color.white;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1, 3) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.sortingOrder = GlowSortingOrder;
            renderer.sharedMaterial = glowMat;
        }

        private static Material Mat(string form) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{ArtFolder}/mat_fx_{form}.mat");

        private static GameObject BuildChevronsUp()
        {
            GameObject go = CreateRoot("FxChevronsUp", Mat("chevron"), 0.5f, true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.12f);
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5, 7) });
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            return go;
        }

        private static GameObject BuildChevronsDown()
        {
            GameObject go = CreateRoot("FxChevronsDown", Mat("chevron"), 0.5f, true);
            go.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.12f);
            main.startRotation = Mathf.PI;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5, 7) });
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            return go;
        }

        private static GameObject BuildHealMotes()
        {
            GameObject go = CreateRoot("FxHealMotes", Mat("croix"), 0.6f, true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.11f);
            main.gravityModifier = 0f;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 8) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.08f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.2f;
            noise.frequency = 0.4f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            return go;
        }

        private static GameObject BuildShieldGain()
        {
            GameObject go = CreateRoot("FxShieldGain", Mat("arc"), 0.4f, true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 0f;
            main.startSize = 0.35f;
            main.maxParticles = 4;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1, 1) });
            var shape = ps.shape;
            shape.enabled = false;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.6f), new Keyframe(1f, 1f)));
            return go;
        }

        private static GameObject BuildShieldPulse()
        {
            GameObject go = CreateRoot("FxShieldPulse", Mat("arc"), 0.3f, false);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.25f;
            main.startSpeed = 0f;
            main.startSize = 0.32f;
            main.maxParticles = 2;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1, 1) });
            var shape = ps.shape;
            shape.enabled = false;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.4f, 1.15f), new Keyframe(1f, 0.9f)));
            return go;
        }

        private static GameObject BuildShieldShatter()
        {
            GameObject go = CreateRoot("FxShieldShatter", Mat("eclat"), 0.55f, true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 14) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.3f;
            limit.limit = 6f;
            return go;
        }

        private static GameObject BuildBurnFlare()
        {
            GameObject go = CreateRoot("FxBurnFlare", Mat("eclat"), 0.45f, true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.12f);
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 9) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.06f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            return go;
        }

        private static GameObject BuildPoisonSplash()
        {
            GameObject go = CreateRoot("FxPoisonSplash", Mat("goutte"), 0.55f, true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
            main.gravityModifier = 1.2f;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5, 7) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.08f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            return go;
        }

        private static GameObject BuildStunRing()
        {
            GameObject go = CreateRoot("FxStunRing", Mat("etoile"), 0.55f, true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.12f);
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3, 4) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;
            shape.position = new Vector3(0f, 0.25f, 0f);
            // Même workaround que LoopStun — orbital Unity instable selon les modes.
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = false;
            rot.z = 360f;
            return go;
        }

        private static GameObject BuildFreezeCrystals()
        {
            GameObject go = CreateRoot("FxFreezeCrystals", Mat("cristal"), 0.55f, true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.11f);
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5, 6) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.85f)));
            return go;
        }

        private static GameObject BuildFreezeShatter()
        {
            GameObject go = CreateRoot("FxFreezeShatter", Mat("cristal"), 0.55f, true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
            main.gravityModifier = 0.8f;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 12) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            return go;
        }

        private static GameObject BuildDissipate()
        {
            GameObject go = CreateRoot("FxDissipate", Mat("croix"), 0.4f, false);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.08f);
            main.gravityModifier = 0.4f;
            ps.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 4, 6) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.06f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            return go;
        }

        // ═══════════════════════════════════════════
        // CÂBLAGE
        // ═══════════════════════════════════════════

        private static void WireCatalog(Dictionary<string, ParticleSystem> prefabs, StringBuilder report)
        {
            FeedbackCatalog catalog = AssetDatabase.LoadAssetAtPath<FeedbackCatalog>(CatalogPath);
            if (catalog == null)
            {
                report.AppendLine("**ERREUR** : catalogue introuvable.");
                Debug.LogError($"[FeedbackVfxBuilder] Catalogue introuvable : {CatalogPath}");
                return;
            }

            ParticleSystem placeholder = null;
            GameObject phGo = AssetDatabase.LoadAssetAtPath<GameObject>(PlaceholderPath);
            if (phGo != null)
                placeholder = phGo.GetComponent<ParticleSystem>();

            Undo.RecordObject(catalog, "Câbler VFX Groupe B");

            int cabled = 0, replaced = 0, intact = 0;
            List<FeedbackCatalog.Entry> entries = catalog.EntriesMutable;

            for (int w = 0; w < Wiring.Length; w++)
            {
                WireSpec spec = Wiring[w];
                FeedbackCatalog.Entry entry = FindEntry(entries, spec.EventId);
                if (entry == null || entry.bundle == null)
                {
                    report.AppendLine($"| {spec.EventId} | ERREUR | entrée absente |");
                    continue;
                }

                if (!prefabs.TryGetValue(spec.PrefabName, out ParticleSystem prefab) || prefab == null)
                {
                    report.AppendLine($"| {spec.EventId} | ERREUR | prefab {spec.PrefabName} manquant |");
                    continue;
                }

                FeedbackBundle b = entry.bundle;
                ParticleSystem current = b.vfxPrefab;
                bool isEmpty = current == null;
                bool isPlaceholder = placeholder != null && current == placeholder;

                if (!isEmpty && !isPlaceholder)
                {
                    intact++;
                    report.AppendLine($"| {spec.EventId} | INTACTE | vfx déjà custom |");
                    continue;
                }

                string status = isPlaceholder ? "REMPLACÉE" : "CÂBLÉE";
                if (isPlaceholder) replaced++;
                else cabled++;

                b.vfxPrefab = prefab;
                b.tintMode = FeedbackBundle.TintMode.Cause;
                b.tintCause = spec.Cause;
                b.vfxScale = spec.Scale;
                report.AppendLine($"| {spec.EventId} | {status} | {spec.PrefabName} · {spec.Cause} · scale {spec.Scale} |");
            }

            EditorUtility.SetDirty(catalog);
            report.AppendLine();
            report.AppendLine($"**Récap** : {cabled} CÂBLÉES · {replaced} REMPLACÉES · {intact} INTACTES");
        }

        private static FeedbackCatalog.Entry FindEntry(List<FeedbackCatalog.Entry> entries, FeedbackEventId id)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].eventId == id)
                    return entries[i];
            }
            return null;
        }

        // ═══════════════════════════════════════════
        // BOUCLES P2b
        // ═══════════════════════════════════════════

        private static void SaveLoopPrefab(string name, PrefabBuilder builder, StringBuilder report)
        {
            string path = $"{LoopFolder}/{name}.prefab";
            bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject root = builder();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            report.AppendLine($"- {name}.prefab : {(existed ? "MIS À JOUR" : "CRÉÉ")}");
        }

        private static GameObject CreateLoopRoot(string name, Material matterMat, bool withGlow)
        {
            GameObject go = new GameObject(name, typeof(ParticleSystem));
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = 12;
            main.startColor = Color.white;

            var emission = ps.emission;
            emission.enabled = true;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            ConfigureMatterRenderer(renderer, matterMat);

            if (withGlow)
                AddGlowChild(go, 1f);

            return go;
        }

        private static GameObject BuildLoopBurn()
        {
            GameObject go = CreateLoopRoot("LoopBurn", Mat("eclat"), true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.08f);
            var emission = ps.emission;
            emission.rateOverTime = 4f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.35f, 0.45f, 0.1f);
            shape.rotation = new Vector3(-90f, 0f, 0f);
            return go;
        }

        private static GameObject BuildLoopPoison()
        {
            GameObject go = CreateLoopRoot("LoopPoison", Mat("goutte"), false);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.08f);
            main.gravityModifier = 0.8f;
            var emission = ps.emission;
            emission.rateOverTime = 3f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.1f;
            shape.position = new Vector3(0f, 0.2f, 0f);
            shape.rotation = new Vector3(90f, 0f, 0f);
            return go;
        }

        private static GameObject BuildLoopShield()
        {
            GameObject go = CreateLoopRoot("LoopShield", Mat("arc"), false);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 0.4f;
            main.startColor = new Color(1f, 1f, 1f, 0.35f);
            var emission = ps.emission;
            emission.rateOverTime = 1f;
            var shape = ps.shape;
            shape.enabled = false;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.5f, 1.06f), new Keyframe(1f, 1f)));
            return go;
        }

        private static GameObject BuildLoopStun()
        {
            GameObject go = CreateLoopRoot("LoopStun", Mat("etoile"), true);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.09f);
            var emission = ps.emission;
            emission.rateOverTime = 3f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.18f;
            shape.position = new Vector3(0f, 0.35f, 0f);
            // Pas d'orbital : Unity spam « Orbital Velocity curves must all be in the same mode ».
            // Rotation Z = lecture « étoiles qui tournent » sans le bug.
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = false;
            rot.z = 180f;
            return go;
        }

        private static GameObject BuildLoopFreeze()
        {
            GameObject go = CreateLoopRoot("LoopFreeze", Mat("cristal"), false);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.09f);
            var emission = ps.emission;
            emission.rateOverTime = 2f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.4f, 0.4f, 0.1f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0.6f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);
            return go;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int lastSlash = path.LastIndexOf('/');
            if (lastSlash <= 0)
                return;

            string parent = path.Substring(0, lastSlash);
            string name = path.Substring(lastSlash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
