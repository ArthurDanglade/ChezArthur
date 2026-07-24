using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChezArthur.Gameplay;
using ChezArthur.Meta;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Phase 1 Meta — crée UniverseContentConfig et le câble sur StageGenerator (scène Game).
    /// Idempotent. À lancer depuis le menu Unity (pas de manip manuelle).
    /// </summary>
    public static class Phase1MetaSetup
    {
        private const string ConfigFolder = "Assets/_Project/ScriptableObjects/Config";
        private const string ConfigAssetPath = ConfigFolder + "/UniverseContentConfig.asset";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        /// <summary>
        /// Étape unique Phase 1 côté Unity : asset + câblage scène Game.
        /// </summary>
        [MenuItem("Chez Arthur/Missions/Phase 1 — Appliquer Meta (Clock/Saison/ContentGate)")]
        public static void ApplyPhase1()
        {
            UniverseContentConfig config = EnsureConfigAsset();
            bool wired = WireStageGenerator(config);

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Phase 1 Meta",
                "Terminé.\n\n" +
                "• Asset : " + ConfigAssetPath + "\n" +
                "• forceArdaculaOnly = true (spawn U1 pour les tests)\n" +
                "• StageGenerator (Game) : " + (wired ? "câblé" : "déjà à jour ou introuvable") + "\n\n" +
                "Vérif Play Mode : DebugMenu → section META / SAISON.",
                "OK");

            Debug.Log(
                "[Phase1MetaSetup] OK — config=" + ConfigAssetPath +
                ", wired=" + wired +
                ". DebugMenu → META / SAISON pour vérifier.");
        }

        [MenuItem("Chez Arthur/Meta/Créer Universe Content Config")]
        public static void CreateConfigOnly()
        {
            UniverseContentConfig config = EnsureConfigAsset();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            Debug.Log("[Phase1MetaSetup] Config prête : " + ConfigAssetPath);
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private static UniverseContentConfig EnsureConfigAsset()
        {
            EnsureFolder(ConfigFolder);

            UniverseContentConfig existing =
                AssetDatabase.LoadAssetAtPath<UniverseContentConfig>(ConfigAssetPath);
            if (existing != null)
                return existing;

            UniverseContentConfig config = ScriptableObject.CreateInstance<UniverseContentConfig>();
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Phase1MetaSetup] Asset créé : " + ConfigAssetPath);
            return config;
        }

        private static bool WireStageGenerator(UniverseContentConfig config)
        {
            if (!System.IO.File.Exists(GameScenePath))
            {
                Debug.LogError("[Phase1MetaSetup] Scène introuvable : " + GameScenePath);
                return false;
            }

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            StageGenerator generator = Object.FindObjectOfType<StageGenerator>(true);
            if (generator == null)
            {
                Debug.LogError("[Phase1MetaSetup] Aucun StageGenerator dans " + GameScenePath);
                return false;
            }

            SerializedObject so = new SerializedObject(generator);
            SerializedProperty prop = so.FindProperty("universeContentConfig");
            if (prop == null)
            {
                Debug.LogError(
                    "[Phase1MetaSetup] Champ universeContentConfig introuvable sur StageGenerator. " +
                    "Recompile / vérifie le script.");
                return false;
            }

            if (prop.objectReferenceValue == config)
            {
                Debug.Log("[Phase1MetaSetup] StageGenerator déjà câblé.");
                return false;
            }

            prop.objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(generator);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase1MetaSetup] StageGenerator câblé + scène Game sauvegardée.");
            return true;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
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
