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
        private const string PRESSION_PATH = "Assets/_Project/ScriptableObjects/Valises/Valise_PressionJeLaBois.asset";
        private const string BOUCLIER_PATH = "Assets/_Project/ScriptableObjects/Valises/Valise_Bouclier.asset";
        private const string SYNERGY_PATH = "Assets/_Project/ScriptableObjects/Synergies/Synergy_CrescendoModeFurie.asset";
        private const string SYNERGY_SHIELD_PATH = "Assets/_Project/ScriptableObjects/Synergies/Synergy_ShieldRenvoi.asset";

        [MenuItem("Chez Arthur/Roguelike/Register Super Lancer Valises")]
        public static void Register()
        {
            ValiseData crescendo = AssetDatabase.LoadAssetAtPath<ValiseData>(CRESCENDO_PATH);
            ValiseData modeFurie = AssetDatabase.LoadAssetAtPath<ValiseData>(MODE_FURIE_PATH);
            ValiseData pression = AssetDatabase.LoadAssetAtPath<ValiseData>(PRESSION_PATH);
            ValiseData bouclier = AssetDatabase.LoadAssetAtPath<ValiseData>(BOUCLIER_PATH);
            SynergyData synergy = AssetDatabase.LoadAssetAtPath<SynergyData>(SYNERGY_PATH);
            SynergyData synergyShield = AssetDatabase.LoadAssetAtPath<SynergyData>(SYNERGY_SHIELD_PATH);

            if (crescendo == null || modeFurie == null || pression == null ||
                bouclier == null || synergy == null || synergyShield == null)
            {
                Debug.LogError(
                    "[SuperLancerValises] Assets introuvables.\n" +
                    $"  Crescendo: {(crescendo != null ? "OK" : "MANQUANT")}\n" +
                    $"  Mode Furie: {(modeFurie != null ? "OK" : "MANQUANT")}\n" +
                    $"  Pression: {(pression != null ? "OK" : "MANQUANT")}\n" +
                    $"  Bouclier: {(bouclier != null ? "OK" : "MANQUANT")}\n" +
                    $"  Synergie Bullet: {(synergy != null ? "OK" : "MANQUANT")}\n" +
                    $"  Synergie Shield: {(synergyShield != null ? "OK" : "MANQUANT")}");
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
                CountAppend(pools[i], "allValises", ref valiseAdds, ref valiseAlready,
                    crescendo, modeFurie, pression, bouclier);
            }

            for (int i = 0; i < gares.Length; i++)
            {
                CountAppend(gares[i], "allValises", ref valiseAdds, ref valiseAlready,
                    crescendo, modeFurie, pression, bouclier);
            }

            for (int i = 0; i < synergies.Length; i++)
            {
                int before = synergyAdds;
                synergyAdds += AppendIfMissingAndApply(synergies[i], "allSynergies", synergy);
                synergyAdds += AppendIfMissingAndApply(synergies[i], "allSynergies", synergyShield);
                if (synergyAdds == before)
                    synergyAlready += 2;
                else if (synergyAdds == before + 1)
                    synergyAlready += 1;
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
            ref int added,
            ref int already,
            params ValiseData[] valises)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty list = so.FindProperty(propertyName);
            if (list == null || !list.isArray) return;

            int localAdded = 0;
            for (int i = 0; i < valises.Length; i++)
            {
                if (valises[i] == null) continue;
                localAdded += AppendIfMissing(list, valises[i], ref already);
            }

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
