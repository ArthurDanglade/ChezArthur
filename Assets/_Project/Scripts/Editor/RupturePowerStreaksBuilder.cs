#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using ChezArthur.Gameplay;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Crée le prefab de traits montants « aura de puissance » pour la Rupture,
    /// et l'assigne à RuptureEffectsSystem.
    /// </summary>
    public static class RupturePowerStreaksBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/VFX/RupturePowerStreaks.prefab";
        private const string MenuCreateOnly = "Chez Arthur/VFX/Créer RupturePowerStreaks (prefab)";
        private const string MenuCreateAndAssign = "Chez Arthur/VFX/Créer + Assigner RupturePowerStreaks";

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

            GameObject root = BuildPowerStreaksGO();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                AssetDatabase.DeleteAsset(PrefabPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (prefab != null)
            {
                Debug.Log($"[RupturePowerStreaks] Prefab créé : {PrefabPath}");
                EditorGUIUtility.PingObject(prefab);
            }

            if (!assignToScene)
                return;

            RuptureEffectsSystem system = Object.FindObjectOfType<RuptureEffectsSystem>(true);
            if (system == null)
            {
                Debug.LogWarning(
                    "[RupturePowerStreaks] RuptureEffectsSystem introuvable — assignation ignorée.");
                return;
            }

            SerializedObject so = new SerializedObject(system);
            SerializedProperty p = so.FindProperty("powerStreaksPrefab");
            if (p == null)
            {
                Debug.LogWarning(
                    "[RupturePowerStreaks] Champ powerStreaksPrefab introuvable — assignation ignorée.");
                return;
            }

            Undo.RecordObject(system, "Assigner RupturePowerStreaks");
            p.objectReferenceValue = prefab != null ? prefab.GetComponent<ParticleSystem>() : null;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(system);

            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log("[RupturePowerStreaks] Prefab assigné sur RuptureEffectsSystem.");
        }

        /// <summary> Construit le GO ParticleSystem (réutilisable hors menu). </summary>
        public static GameObject BuildPowerStreaksGO()
        {
            GameObject go = new GameObject("RupturePowerStreaks", typeof(ParticleSystem));
            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startRotation = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = 64;
            main.gravityModifier = 0f;
            main.startColor = new Color(1f, 0.4f, 0.18f, 0.85f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 22f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.22f;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            // Circle Unity = plan XZ → -90° pour le plan XY (2D).
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            // Même mode (TwoConstants) sur X/Y/Z — sinon Unity : "curves must all be in the same mode".
            velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.55f), 0.45f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.2f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.12f),
                    new GradientAlphaKey(0.55f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(g);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f, 0.15f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var noise = ps.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            noise.frequency = 0.45f;
            noise.scrollSpeed = 0.15f;
            noise.damping = true;
            noise.separateAxes = false;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.enabled = true;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3.2f;
            renderer.velocityScale = 0.12f;
            renderer.cameraVelocityScale = 0f;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
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
