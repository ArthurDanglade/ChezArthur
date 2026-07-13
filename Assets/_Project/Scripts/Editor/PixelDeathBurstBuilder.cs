#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using ChezArthur.Gameplay;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Crée un prefab de particules « pixel burst » pour la mort des ennemis et l'assigne à JuiceDirector.
    /// </summary>
    public static class PixelDeathBurstBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/VFX/PixelDeathBurst.prefab";
        private const string MenuCreateOnly = "Chez Arthur/VFX/Créer PixelDeathBurst (prefab)";
        private const string MenuCreateAndAssign = "Chez Arthur/VFX/Créer + Assigner PixelDeathBurst à JuiceDirector";

        [MenuItem(MenuCreateOnly)]
        public static void CreatePrefabOnly()
        {
            CreateOrReplacePrefab(assignToScene: false);
        }

        [MenuItem(MenuCreateAndAssign)]
        public static void CreateAndAssign()
        {
            CreateOrReplacePrefab(assignToScene: true);
        }

        private static void CreateOrReplacePrefab(bool assignToScene)
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/VFX");

            GameObject root = BuildPixelBurstGO();

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(PrefabPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (prefab != null)
            {
                Debug.Log($"[PixelDeathBurstBuilder] Prefab créé : {PrefabPath}");
                EditorGUIUtility.PingObject(prefab);
            }

            if (!assignToScene)
                return;

            JuiceDirector juice = Object.FindObjectOfType<JuiceDirector>(true);
            if (juice == null)
            {
                Debug.LogWarning("[PixelDeathBurstBuilder] JuiceDirector introuvable dans la scène active — assignation ignorée.");
                return;
            }

            SerializedObject so = new SerializedObject(juice);
            SerializedProperty p = so.FindProperty("_deathBurstPrefab");
            if (p == null)
            {
                Debug.LogWarning("[PixelDeathBurstBuilder] Champ _deathBurstPrefab introuvable sur JuiceDirector — assignation ignorée.");
                return;
            }

            p.objectReferenceValue = prefab.GetComponent<ParticleSystem>();
            so.ApplyModifiedPropertiesWithoutUndo();

            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log("[PixelDeathBurstBuilder] _deathBurstPrefab assigné sur JuiceDirector.");
        }

        private static GameObject BuildPixelBurstGO()
        {
            GameObject go = new GameObject("PixelDeathBurst", typeof(ParticleSystem));
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 1.0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 6.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startRotation3D = false;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = 128;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 46, 60)
            });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);

            var limitVelocity = ps.limitVelocityOverLifetime;
            limitVelocity.enabled = true;
            limitVelocity.dampen = 0.25f;
            limitVelocity.limit = 8f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.9f, 0.9f, 0.9f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(g);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve c = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0.6f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, c);

            var noise = ps.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.strength = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.2f;
            noise.damping = true;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.enabled = true;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            // Matériau par défaut : petit carré blanc (le look « pixel » vient surtout de la taille + burst).
            renderer.material = new Material(Shader.Find("Sprites/Default"));

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

