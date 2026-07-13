#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ChezArthur.UI;
using ChezArthur.Enemies;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Retire les anciennes barres HP enfichées sur les prefabs ennemis (remplacées par HPBarManager).
    /// </summary>
    public static class EnemyHPBarCleanupTool
    {
        private const string EnemiesFolder = "Assets/_Project/Prefabs/Enemies";

        [MenuItem("Chez Arthur/UI/Nettoyer barres HP ennemis (prefabs)")]
        public static void CleanupEnemyPrefabHpBars()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EnemiesFolder });
            int cleaned = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (CleanupPrefabAtPath(path))
                    cleaned++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[EnemyHPBarCleanup] Terminé — {cleaned} prefab(s) nettoyé(s) sous {EnemiesFolder}.");
        }

        private static bool CleanupPrefabAtPath(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                return false;

            bool changed = RemoveLegacyBars(root.transform);

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[EnemyHPBarCleanup] Barres retirées : {path}");
            }

            PrefabUtility.UnloadPrefabContents(root);
            return changed;
        }

        private static bool RemoveLegacyBars(Transform root)
        {
            bool changed = false;
            var toDestroy = new List<GameObject>();

            CollectLegacyBarObjects(root, toDestroy);

            for (int i = 0; i < toDestroy.Count; i++)
            {
                if (toDestroy[i] == null)
                    continue;

                Object.DestroyImmediate(toDestroy[i]);
                changed = true;
            }

            return changed;
        }

        private static void CollectLegacyBarObjects(Transform parent, List<GameObject> results)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                    continue;

                if (IsLegacyHpBar(child))
                {
                    results.Add(child.gameObject);
                    continue;
                }

                CollectLegacyBarObjects(child, results);
            }
        }

        private static bool IsLegacyHpBar(Transform t)
        {
            if (t.GetComponent<EnemyHPBar>() != null)
                return true;

            string name = t.name.Trim();
            if (name.Equals("HPBar", System.StringComparison.OrdinalIgnoreCase)
                || name.Equals("HPBarPrefab", System.StringComparison.OrdinalIgnoreCase)
                || name.Contains("EnemyHPBar"))
            {
                return true;
            }

            return false;
        }
    }
}
#endif
