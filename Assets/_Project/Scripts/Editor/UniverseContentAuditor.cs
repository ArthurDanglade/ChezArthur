#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Meta;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Audit rapide : SSR universeIndex + pools ennemis alignés sur UniverseIds.
    /// </summary>
    public static class UniverseContentAuditor
    {
        private static readonly (int id, string ssrId, string theme)[] Expected =
        {
            (UniverseIds.Ardacula, "ardacula", "Gothique"),
            (UniverseIds.Ancien, "ancien", "Arts martiaux"),
            (UniverseIds.DonCostardo, "don_costardo", "Super-héros"),
            (UniverseIds.Faille, "faille", "Failles n°"),
            (UniverseIds.Troplin, "troplin", "Dragon/gobelin"),
        };

        [MenuItem("Chez Arthur/Meta/Auditer contenu univers (SSR + ennemis)")]
        public static void Audit()
        {
            var sb = new StringBuilder(2048);
            sb.AppendLine("=== AUDIT UNIVERS ===");

            CharacterData[] characters = LoadAll<CharacterData>();
            EnemyData[] enemies = LoadAll<EnemyData>();

            for (int i = 0; i < Expected.Length; i++)
            {
                int uid = Expected[i].id;
                string ssrId = Expected[i].ssrId;
                string theme = Expected[i].theme;

                CharacterData ssr = FindCharacter(characters, ssrId);
                int enemyCount = CountEnemies(enemies, uid);

                sb.AppendLine();
                sb.AppendLine($"U{uid} {UniverseIds.GetDisplayName(uid)} — {theme}");
                if (ssr == null)
                    sb.AppendLine("  SSR : MANQUANT en assets");
                else if (ssr.UniverseIndex != uid)
                    sb.AppendLine($"  SSR : {ssr.CharacterName} universeIndex={ssr.UniverseIndex} (attendu {uid})");
                else
                    sb.AppendLine($"  SSR : {ssr.CharacterName} OK");

                sb.AppendLine($"  Ennemis index {uid} : {enemyCount}");
                if (enemyCount == 0)
                    sb.AppendLine("  ⚠ Pool ennemis vide");
            }

            // Orphelins / retraités
            int retired = CountEnemies(enemies, 99);
            if (retired > 0)
                sb.AppendLine($"\nEnnemis index 99 (retirés) : {retired}");

            CharacterDatabase db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(
                "Assets/_Project/ScriptableObjects/Characters/CharacterDatabase.asset");
            if (db != null)
            {
                bool failleInDb = db.GetById("faille") != null;
                sb.AppendLine($"\nDB Faille={failleInDb}");
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Audit univers", sb.ToString(), "OK");
        }

        private static T[] LoadAll<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var list = new List<T>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    list.Add(asset);
            }
            return list.ToArray();
        }

        private static CharacterData FindCharacter(CharacterData[] all, string id)
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].Id == id)
                    return all[i];
            }
            return null;
        }

        private static int CountEnemies(EnemyData[] all, int universeIndex)
        {
            int n = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].UniverseIndex == universeIndex)
                    n++;
            }
            return n;
        }
    }
}
#endif
