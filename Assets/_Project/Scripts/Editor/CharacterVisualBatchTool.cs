#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Pipeline one-shot : presets d'import (icônes, portraits, combat) + câblage CharacterData.
    /// Menu : Chez Arthur > Art > Préparer visuels personnages (batch).
    /// </summary>
    public static class CharacterVisualBatchTool
    {
        private const string CharactersDataFolder = "Assets/_Project/Data/Characters";
        private const string IconsFolder = CharacterIconImportPostprocessor.IconsFolder;
        private const string PortraitsFolder = CharacterPortraitImportPostprocessor.PortraitsFolder;
        private const string CombatFolder = "Assets/_Project/Art/Combat/Characters";

        [MenuItem("Chez Arthur/Art/Préparer visuels personnages (batch)")]
        public static void RunBatch()
        {
            int iconCount = CharacterIconImportPostprocessor.ReimportFolder(
                IconsFolder,
                CharacterIconImportPostprocessor.ApplyCharacterIconPreset,
                "icônes personnages");
            int portraitCount = CharacterIconImportPostprocessor.ReimportFolder(
                PortraitsFolder,
                CharacterPortraitImportPostprocessor.ApplyCharacterPortraitPreset,
                "portraits personnages");
            int combatCount = CharacterIconImportPostprocessor.ReimportFolder(
                CombatFolder,
                CombatSpriteImportPostprocessor.ApplyCombatPreset,
                "sprites combat");

            AssetDatabase.Refresh();
            CombatSpriteAtlasTool.CreateOrUpdateAtlas();

            WireReport report = WireAllCharacterData();

            Debug.Log(
                $"[CharacterVisual] Batch terminé — presets : {iconCount} icône(s), {portraitCount} portrait(s), " +
                $"{combatCount} combat(s). Câblage : {report.Wired} CharacterData mis à jour, " +
                $"{report.Skipped} ignoré(s), {report.Missing} sans visuel trouvé.");
        }

        private static WireReport WireAllCharacterData()
        {
            var iconIndex = BuildAssetIndex(IconsFolder, "icon");
            var portraitIndex = BuildAssetIndex(PortraitsFolder, "portrait");
            var combatIndex = BuildAssetIndex(CombatFolder, "combat");

            string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharactersDataFolder });
            var report = new WireReport();

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                    if (data == null || string.IsNullOrEmpty(data.Id))
                    {
                        report.Skipped++;
                        continue;
                    }

                    bool changed = false;
                    var so = new SerializedObject(data);

                    changed |= TryWireSprite(so, "icon", FindBestAssetPath(data.Id, iconIndex), data.Id, "icon");
                    changed |= TryWireSprite(so, "combatSprite", FindBestAssetPath(data.Id, combatIndex), data.Id, "combat");

                    // Portrait Resources = texture Default : on câble seulement si un sous-asset Sprite existe.
                    changed |= TryWireSprite(so, "portrait", FindBestAssetPath(data.Id, portraitIndex), data.Id, "portrait");

                    if (changed)
                    {
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(data);
                        report.Wired++;
                    }
                    else if (!HasAnyVisual(data.Id, iconIndex, portraitIndex, combatIndex))
                    {
                        report.Missing++;
                        Debug.LogWarning($"[CharacterVisual] Aucun visuel trouvé pour '{data.CharacterName}' (id={data.Id}).");
                    }
                    else
                    {
                        report.Skipped++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            return report;
        }

        private static bool HasAnyVisual(
            string characterId,
            Dictionary<string, string> iconIndex,
            Dictionary<string, string> portraitIndex,
            Dictionary<string, string> combatIndex)
        {
            return FindBestAssetPath(characterId, iconIndex) != null
                || FindBestAssetPath(characterId, portraitIndex) != null
                || FindBestAssetPath(characterId, combatIndex) != null;
        }

        private static bool TryWireSprite(
            SerializedObject so,
            string fieldName,
            string assetPath,
            string characterId,
            string category)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            Sprite sprite = LoadSprite(assetPath);
            if (sprite == null)
            {
                if (category == "portrait")
                {
                    Debug.Log(
                        $"[CharacterVisual] Portrait Resources pour id={characterId} : preset Default (OK pour PortraitLoader), " +
                        "pas de Sprite → champ portrait SO non modifié.");
                }
                else
                {
                    Debug.LogWarning($"[CharacterVisual] Sprite introuvable ({category}) : {assetPath} (id={characterId}).");
                }

                return false;
            }

            SerializedProperty property = so.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogWarning($"[CharacterVisual] Champ '{fieldName}' introuvable sur CharacterData.");
                return false;
            }

            if (property.objectReferenceValue == sprite)
                return false;

            property.objectReferenceValue = sprite;
            Debug.Log($"[CharacterVisual] {fieldName} ← {Path.GetFileName(assetPath)} (id={characterId})");
            return true;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (direct != null)
                return direct;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    return sprite;
            }

            return null;
        }

        private static Dictionary<string, string> BuildAssetIndex(string folder, string prefix)
        {
            var index = new Dictionary<string, string>();
            string trimmed = folder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(trimmed))
                return index;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { trimmed });
            string normalizedPrefix = Normalize(prefix);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileNameWithoutExtension(path);
                string key = Normalize(fileName);

                if (!string.IsNullOrEmpty(normalizedPrefix) && key.StartsWith(normalizedPrefix))
                    key = key.Substring(normalizedPrefix.Length);

                if (string.IsNullOrEmpty(key) || index.ContainsKey(key))
                    continue;

                index.Add(key, path);
            }

            return index;
        }

        private static string FindBestAssetPath(string characterId, Dictionary<string, string> index)
        {
            if (string.IsNullOrEmpty(characterId) || index == null || index.Count == 0)
                return null;

            string id = Normalize(characterId);
            if (index.TryGetValue(id, out string exact))
                return exact;

            string bestPath = null;
            int bestScore = 0;

            foreach (KeyValuePair<string, string> entry in index)
            {
                int score = ScoreMatch(id, entry.Key);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPath = entry.Value;
                }
            }

            return bestScore > 0 ? bestPath : null;
        }

        private static int ScoreMatch(string id, string key)
        {
            if (id == key)
                return 1000;

            if (key.StartsWith(id))
                return 500 + id.Length;

            if (id.StartsWith(key))
                return 400 + key.Length;

            return 0;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c))
                    builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private struct WireReport
        {
            public int Wired;
            public int Skipped;
            public int Missing;
        }
    }
}
#endif
