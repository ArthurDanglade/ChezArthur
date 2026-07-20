#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChezArthur.Gameplay;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Charge les variants Sanctum Pixel aura_effect_6 dans RuptureEffectsSystem.
    /// </summary>
    public static class RuptureAuraVariantsLoader
    {
        private const string AuraRoot = "Assets/sanctum_pixel/aura_2_package/Sprite";
        private const string EffectFolderName = "aura_effect_6";

        private static readonly string[] PreferredColorOrder =
        {
            "red", "orange", "yellow", "magic", "purple", "royal",
            "blue", "pink", "lime", "limegreen", "springgreen"
        };

        [MenuItem("Chez Arthur/UI/Charger auras rupture (6)")]
        public static void LoadAuraEffect6()
        {
            RuptureEffectsSystem system = UnityEngine.Object.FindObjectOfType<RuptureEffectsSystem>(true);
            if (system == null)
            {
                Debug.LogError(
                    "[RuptureAura] RuptureEffectsSystem introuvable. " +
                    "Lance d'abord Chez Arthur/UI/Monter systèmes Pression (logique).");
                return;
            }

            string effectFolder = Path.Combine(AuraRoot, EffectFolderName).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(effectFolder))
            {
                Debug.LogError($"[RuptureAura] Dossier introuvable : {effectFolder}");
                return;
            }

            string[] dirs = Directory.GetDirectories(effectFolder);
            var colorFolders = new List<string>();
            for (int d = 0; d < dirs.Length; d++)
            {
                string dir = dirs[d].Replace('\\', '/');
                if (dir.StartsWith(Application.dataPath))
                    dir = "Assets" + dir.Substring(Application.dataPath.Length);
                colorFolders.Add(dir);
            }

            colorFolders.Sort(CompareColorFolders);

            var variants = new List<RuptureEffectsSystem.RuptureAuraVariant>(colorFolders.Count);
            int preferredIndex = 0;

            for (int c = 0; c < colorFolders.Count; c++)
            {
                Sprite[] frames = LoadSortedFrames(colorFolders[c]);
                if (frames == null || frames.Length == 0)
                    continue;

                string folderName = Path.GetFileName(colorFolders[c]);
                variants.Add(new RuptureEffectsSystem.RuptureAuraVariant
                {
                    id = folderName,
                    frames = frames
                });

                if (folderName.EndsWith("_red", StringComparison.OrdinalIgnoreCase))
                    preferredIndex = variants.Count - 1;
            }

            if (variants.Count == 0)
            {
                Debug.LogError("[RuptureAura] Aucune frame trouvée sous aura_effect_6.");
                return;
            }

            Undo.RecordObject(system, "Charger auras rupture (6)");
            system.EditorReplaceAuraVariants(variants, preferredIndex);
            EditorUtility.SetDirty(system);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log(
                $"[RuptureAura] {variants.Count} variants aura_effect_6 chargés. " +
                $"Active : {variants[preferredIndex].id}. Sauvegarde la scène Game.");
            Selection.activeGameObject = system.gameObject;
        }

        private static int CompareColorFolders(string a, string b)
        {
            int ia = PreferredColorRank(Path.GetFileName(a));
            int ib = PreferredColorRank(Path.GetFileName(b));
            if (ia != ib)
                return ia.CompareTo(ib);
            return string.CompareOrdinal(a, b);
        }

        private static int PreferredColorRank(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
                return 999;

            for (int i = 0; i < PreferredColorOrder.Length; i++)
            {
                if (folderName.EndsWith("_" + PreferredColorOrder[i], StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 500;
        }

        private static Sprite[] LoadSortedFrames(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            if (guids == null || guids.Length == 0)
                return Array.Empty<Sprite>();

            var frames = new List<Sprite>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    frames.Add(sprite);
            }

            frames.Sort((a, b) => TrailingNumber(a.name).CompareTo(TrailingNumber(b.name)));
            return frames.ToArray();
        }

        private static int TrailingNumber(string name)
        {
            if (string.IsNullOrEmpty(name))
                return 0;

            int i = name.Length - 1;
            while (i >= 0 && char.IsDigit(name[i]))
                i--;

            return int.TryParse(name.Substring(i + 1), out int number) ? number : 0;
        }
    }
}
#endif
