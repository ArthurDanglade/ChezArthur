#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using ChezArthur.Characters;
using ChezArthur.Gacha;
using ChezArthur.Meta;
using UnityEditor;
using UnityEngine;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Crée SeasonRewardsConfig (Resources) + bannière portail LR cumulatif.
    /// Idempotent : ne recrée pas, complète le pool LR manquant du portail.
    /// </summary>
    public static class SeasonRewardsAssetsBuilder
    {
        private const string ConfigPath = "Assets/_Project/Resources/SeasonRewardsConfig.asset";
        private const string BannerPath = "Assets/_Project/ScriptableObjects/Banners/Banniere_Portail_LR.asset";

        [MenuItem("Chez Arthur/Meta/Build Season Rewards Assets")]
        public static void Build()
        {
            var report = new StringBuilder(4096);
            report.AppendLine("# Season Rewards Assets Builder");
            report.AppendLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            EnsureFolder("Assets/_Project/Resources");
            EnsureFolder("Assets/_Project/ScriptableObjects");
            EnsureFolder("Assets/_Project/ScriptableObjects/Banners");

            EnsureRewardsConfig(report);
            EnsurePortalBanner(report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport(report);
            EditorUtility.DisplayDialog(
                "Season Rewards",
                "Build OK. Voir Audits/season_rewards_build.txt",
                "OK");
        }

        private static void EnsureRewardsConfig(StringBuilder report)
        {
            SeasonRewardsConfig existing = AssetDatabase.LoadAssetAtPath<SeasonRewardsConfig>(ConfigPath);
            if (existing != null)
            {
                existing.EnsureTiers();
                EditorUtility.SetDirty(existing);
                report.AppendLine($"- Config déjà présente : {ConfigPath}");
                return;
            }

            SeasonRewardsConfig created = ScriptableObject.CreateInstance<SeasonRewardsConfig>();
            created.EnsureTiers();
            AssetDatabase.CreateAsset(created, ConfigPath);
            report.AppendLine($"- Créé : {ConfigPath}");
        }

        private static void EnsurePortalBanner(StringBuilder report)
        {
            BannerData banner = AssetDatabase.LoadAssetAtPath<BannerData>(BannerPath);
            bool created = false;
            if (banner == null)
            {
                banner = ScriptableObject.CreateInstance<BannerData>();
                AssetDatabase.CreateAsset(banner, BannerPath);
                created = true;
                report.AppendLine($"- Créé : {BannerPath}");
            }
            else
            {
                report.AppendLine($"- Bannière déjà présente : {BannerPath}");
            }

            SerializedObject so = new SerializedObject(banner);
            so.FindProperty("bannerId").stringValue = "banner_portail_lr";
            so.FindProperty("id").stringValue = "banner_portail_lr";
            so.FindProperty("displayTitle").stringValue = "Portail LR cumulatif";
            so.FindProperty("bannerName").stringValue = "Portail LR cumulatif";
            so.FindProperty("hasDuration").boolValue = false;
            so.FindProperty("isLrPortal").boolValue = true;
            so.FindProperty("costSingle").intValue = 100;
            so.FindProperty("costMulti").intValue = 1000;
            so.FindProperty("rateSR").floatValue = 90f;
            so.FindProperty("rateSSR").floatValue = 9f;
            so.FindProperty("rateLR").floatValue = 1f;
            so.FindProperty("pityThreshold").intValue = 100;

            // Pool LR = tous les CharacterData LR du projet.
            string[] guids = AssetDatabase.FindAssets("t:CharacterData");
            SerializedProperty lrPool = so.FindProperty("lrPool");
            if (lrPool == null)
            {
                report.AppendLine("- ✗ lrPool introuvable sur BannerData");
                so.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            var found = new System.Collections.Generic.List<CharacterData>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (data != null && data.Rarity == CharacterRarity.LR)
                    found.Add(data);
            }

            int added = 0;
            for (int i = 0; i < found.Count; i++)
            {
                CharacterData lr = found[i];
                if (ContainsObjectRef(lrPool, lr))
                    continue;
                lrPool.arraySize++;
                lrPool.GetArrayElementAtIndex(lrPool.arraySize - 1).objectReferenceValue = lr;
                added++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(banner);
            report.AppendLine($"- Portail isLrPortal=true, LR dans pool = {lrPool.arraySize} (+{added} ajoutés)");
            if (created)
                report.AppendLine("- Asset portail neuf");
            else if (added == 0)
                report.AppendLine("- Re-run : zéro LR manquant à ajouter");
        }

        private static bool ContainsObjectRef(SerializedProperty arrayProp, UnityEngine.Object obj)
        {
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                if (arrayProp.GetArrayElementAtIndex(i).objectReferenceValue == obj)
                    return true;
            }

            return false;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void WriteReport(StringBuilder report)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, "season_rewards_build.txt");
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
            Debug.Log($"[SeasonRewardsAssetsBuilder] Rapport : {path}");
        }
    }
}
#endif
