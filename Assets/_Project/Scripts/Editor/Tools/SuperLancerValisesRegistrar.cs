using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChezArthur.Roguelike;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Enregistre Crescendo, Mode Furie et la synergie Bullet Time dans les pools de scène.
    /// </summary>
    public static class SuperLancerValisesRegistrar
    {
        private const string CRESCENDO_PATH = "Assets/_Project/ScriptableObjects/Valises/Valise_Crescendo.asset";
        private const string MODE_FURIE_PATH = "Assets/_Project/ScriptableObjects/Valises/Valise_ModeFurie.asset";
        private const string SYNERGY_PATH = "Assets/_Project/ScriptableObjects/Synergies/Synergy_CrescendoModeFurie.asset";

        [MenuItem("Chez Arthur/Roguelike/Register Super Lancer Valises")]
        public static void Register()
        {
            ValiseData crescendo = AssetDatabase.LoadAssetAtPath<ValiseData>(CRESCENDO_PATH);
            ValiseData modeFurie = AssetDatabase.LoadAssetAtPath<ValiseData>(MODE_FURIE_PATH);
            SynergyData synergy = AssetDatabase.LoadAssetAtPath<SynergyData>(SYNERGY_PATH);

            if (crescendo == null || modeFurie == null || synergy == null)
            {
                Debug.LogError(
                    "[SuperLancerValises] Assets introuvables.\n" +
                    $"  Crescendo: {(crescendo != null ? "OK" : "MANQUANT")} ({CRESCENDO_PATH})\n" +
                    $"  Mode Furie: {(modeFurie != null ? "OK" : "MANQUANT")} ({MODE_FURIE_PATH})\n" +
                    $"  Synergie: {(synergy != null ? "OK" : "MANQUANT")} ({SYNERGY_PATH})");
                return;
            }

            Scene active = SceneManager.GetActiveScene();
            string sceneName = string.IsNullOrEmpty(active.name) ? "(aucune)" : active.name;

            RoguelikeSelectionPool[] pools = Object.FindObjectsOfType<RoguelikeSelectionPool>(true);
            GareManager[] gares = Object.FindObjectsOfType<GareManager>(true);
            SynergyManager[] synergies = Object.FindObjectsOfType<SynergyManager>(true);

            int targetCount = pools.Length + gares.Length + synergies.Length;
            if (targetCount == 0)
            {
                Debug.LogWarning(
                    $"[SuperLancerValises] Aucun pool trouvé dans la scène active « {sceneName} ».\n" +
                    "→ Ces managers vivent dans la scène Game uniquement (pas Hub).\n" +
                    "→ Ouvre Assets/_Project/Scenes/Game.unity puis relance le menu.\n" +
                    "→ Note : l'enregistrement a déjà été fait dans Game.unity (Crescendo / Mode Furie / Bullet Time).");
                return;
            }

            int valiseAdds = 0;
            int synergyAdds = 0;
            int valiseAlready = 0;
            int synergyAlready = 0;

            for (int i = 0; i < pools.Length; i++)
            {
                CountAppend(pools[i], "allValises", crescendo, modeFurie, ref valiseAdds, ref valiseAlready);
            }

            for (int i = 0; i < gares.Length; i++)
            {
                CountAppend(gares[i], "allValises", crescendo, modeFurie, ref valiseAdds, ref valiseAlready);
            }

            for (int i = 0; i < synergies.Length; i++)
            {
                int before = synergyAdds;
                synergyAdds += AppendIfMissingAndApply(synergies[i], "allSynergies", synergy);
                if (synergyAdds == before)
                    synergyAlready++;
            }

            if (valiseAdds > 0 || synergyAdds > 0)
            {
                EditorSceneManager.MarkSceneDirty(active);
                AssetDatabase.SaveAssets();
            }

            if (valiseAdds == 0 && synergyAdds == 0)
            {
                Debug.Log(
                    $"[SuperLancerValises] OK — déjà enregistré dans « {sceneName} » " +
                    $"(pools={pools.Length}, gares={gares.Length}, synergies={synergies.Length} ; " +
                    $"valises déjà présentes={valiseAlready}, synergie déjà présente={synergyAlready}).\n" +
                    "Rien à ajouter. Tu peux smoke-tester via DebugMenu.");
            }
            else
            {
                Debug.Log(
                    $"[SuperLancerValises] Enregistrement dans « {sceneName} » — " +
                    $"valises +{valiseAdds} (déjà {valiseAlready}), synergies +{synergyAdds} (déjà {synergyAlready}). " +
                    "Pense à sauver la scène (Ctrl+S).");
            }
        }

        private static void CountAppend(
            Object target,
            string propertyName,
            ValiseData a,
            ValiseData b,
            ref int added,
            ref int already)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty list = so.FindProperty(propertyName);
            if (list == null || !list.isArray) return;

            int localAdded = 0;
            localAdded += AppendIfMissing(list, a, ref already);
            localAdded += AppendIfMissing(list, b, ref already);
            if (localAdded > 0)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                added += localAdded;
            }
        }

        private static int AppendIfMissingAndApply(Object target, string propertyName, Object asset)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty list = so.FindProperty(propertyName);
            if (list == null || !list.isArray) return 0;

            int already = 0;
            int added = AppendIfMissing(list, asset, ref already);
            if (added > 0)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
            return added;
        }

        private static int AppendIfMissing(SerializedProperty list, Object asset, ref int already)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == asset)
                {
                    already++;
                    return 0;
                }
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = asset;
            return 1;
        }
    }
}
