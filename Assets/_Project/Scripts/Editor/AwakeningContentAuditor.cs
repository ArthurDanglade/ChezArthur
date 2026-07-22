#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Rapport de préparation du contenu éveil SSR — lecture seule, ne modifie rien.
    /// </summary>
    public static class AwakeningContentAuditor
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string CeremonySystemPrefabPath =
            "Assets/_Project/Prefabs/Systems/AwakeningCeremonySystem.prefab";
        private const string CeremonySystemPrefabAltPath =
            "Assets/_Project/Prefabs/AwakeningCeremonySystem.prefab";

        private static readonly string[] CeremonyClipFields =
        {
            "ambienceLoop",
            "riserClip",
            "flashClip",
            "fanfareClip"
        };

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Audit/Rapport de préparation éveil SSR")]
        public static void Run()
        {
            var report = new StringBuilder(4096);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" AUDIT ÉVEIL SSR — préparation contenu");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();

            string[] guids = AssetDatabase.FindAssets("t:CharacterData");
            var ssrList = new List<CharacterData>(32);
            var lrList = new List<CharacterData>(16);
            var readyIds = new List<string>(16);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (data == null)
                    continue;

                if (data.Rarity == CharacterRarity.SSR)
                    ssrList.Add(data);
                else if (data.Rarity == CharacterRarity.LR)
                    lrList.Add(data);
            }

            // Tri stable par id pour un rapport reproductible
            ssrList.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            lrList.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));

            for (int i = 0; i < ssrList.Count; i++)
            {
                CharacterData data = ssrList[i];
                AppendSsrBlock(report, data, out bool ready);
                if (ready)
                    readyIds.Add(data.Id);
                report.AppendLine();
            }

            AppendUniverseCollisions(report, ssrList);
            report.AppendLine();

            AppendLrUniverseWarnings(report, lrList);
            report.AppendLine();

            AppendCeremonyPrefabAudit(report);
            report.AppendLine();

            // Synthèse
            report.AppendLine("───────────────────────────────────────────");
            report.AppendLine(" SYNTHÈSE");
            report.AppendLine("───────────────────────────────────────────");
            if (readyIds.Count == 0)
            {
                report.AppendLine(
                    $"0 SSR prêts / {ssrList.Count} total — aucun personnage prêt pour l'éveil.");
            }
            else
            {
                report.AppendLine(
                    $"{readyIds.Count} SSR prêts / {ssrList.Count} total — le système est actif pour :");
                report.AppendLine("  " + string.Join(", ", readyIds));
            }

            report.AppendLine();
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" Fin du rapport (aucune modification effectuée)");
            report.AppendLine("═══════════════════════════════════════════");

            Debug.Log(report.ToString());
        }

        // ═══════════════════════════════════════════
        // BLOCS DE RAPPORT
        // ═══════════════════════════════════════════

        private static void AppendSsrBlock(StringBuilder report, CharacterData data, out bool ready)
        {
            ready = false;
            string id = string.IsNullOrEmpty(data.Id) ? data.name : data.Id;
            string displayName = string.IsNullOrEmpty(data.CharacterName) ? id : data.CharacterName;

            report.AppendLine($"── SSR : {displayName} ({id}) ──");

            var missing = new List<string>(4);

            // Univers
            int universe = data.UniverseIndex;
            if (universe == 0)
            {
                report.AppendLine("  universeIndex : ❌ univers non relié");
                missing.Add("univers");
            }
            else if (universe >= 1 && universe <= 5)
            {
                report.AppendLine($"  universeIndex : ✅ univers {universe}");
            }
            else
            {
                report.AppendLine($"  universeIndex : ⚠️ valeur hors plage ({universe})");
                missing.Add("univers hors plage");
            }

            // Déchu
            AppendPortraitCheck(
                report,
                "AnimatedPortraitDechu",
                data.AnimatedPortraitDechu,
                missing,
                "déchu");

            // Prime
            AppendPortraitCheck(
                report,
                "AnimatedPortraitPrime",
                data.AnimatedPortraitPrime,
                missing,
                "prime");

            bool universeOk = universe >= 1 && universe <= 5;
            bool dechuOk = !missing.Contains("déchu");
            bool primeOk = !missing.Contains("prime");
            ready = universeOk && dechuOk && primeOk;

            if (ready)
                report.AppendLine("  Verdict : PRÊT POUR L'ÉVEIL");
            else
                report.AppendLine($"  Verdict : PARTIEL — manque : {string.Join(", ", missing)}");
        }

        private static void AppendPortraitCheck(
            StringBuilder report,
            string label,
            AnimatedPortraitData portrait,
            List<string> missing,
            string missingKey)
        {
            if (portrait == null)
            {
                report.AppendLine($"  {label} : ❌");
                missing.Add(missingKey);
                return;
            }

            string path = portrait.ResourcesPath;
            if (string.IsNullOrEmpty(path))
            {
                report.AppendLine($"  {label} : ⚠️ chemin cassé : (vide)");
                missing.Add(missingKey);
                return;
            }

            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                report.AppendLine($"  {label} : ⚠️ chemin cassé : {path}");
                missing.Add(missingKey);
                return;
            }

            Resources.UnloadAsset(tex);
            report.AppendLine($"  {label} : ✅ ({path})");
        }

        private static void AppendUniverseCollisions(StringBuilder report, List<CharacterData> ssrList)
        {
            report.AppendLine("── Collisions d'univers ──");

            var byUniverse = new Dictionary<int, List<string>>(8);
            for (int i = 0; i < ssrList.Count; i++)
            {
                CharacterData data = ssrList[i];
                int u = data.UniverseIndex;
                if (u < 1 || u > 5)
                    continue;

                if (!byUniverse.TryGetValue(u, out List<string> ids))
                {
                    ids = new List<string>(4);
                    byUniverse[u] = ids;
                }

                ids.Add(string.IsNullOrEmpty(data.Id) ? data.name : data.Id);
            }

            bool any = false;
            foreach (KeyValuePair<int, List<string>> pair in byUniverse)
            {
                if (pair.Value.Count < 2)
                    continue;

                any = true;
                report.AppendLine(
                    $"  ⚠️ conflit univers {pair.Key} : {string.Join(", ", pair.Value)}");
            }

            if (!any)
                report.AppendLine("  ✅ aucune collision (1 SSR max par univers 1–5)");
        }

        private static void AppendLrUniverseWarnings(StringBuilder report, List<CharacterData> lrList)
        {
            report.AppendLine("── LR / univers ──");

            bool any = false;
            for (int i = 0; i < lrList.Count; i++)
            {
                CharacterData data = lrList[i];
                if (data.UniverseIndex == 0)
                    continue;

                any = true;
                string id = string.IsNullOrEmpty(data.Id) ? data.name : data.Id;
                report.AppendLine(
                    $"  ⚠️ {id} : LR relié à un univers (l'éveil est SSR uniquement — " +
                    "champ probablement câblé par erreur)");
            }

            if (!any)
                report.AppendLine("  ✅ aucun LR avec universeIndex ≠ 0");
        }

        private static void AppendCeremonyPrefabAudit(StringBuilder report)
        {
            report.AppendLine("── Prefab AwakeningCeremonySystem ──");

            string path = CeremonySystemPrefabPath;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                path = CeremonySystemPrefabAltPath;
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            if (prefab == null)
            {
                // Cherche par nom n'importe où sous Prefabs
                string[] found = AssetDatabase.FindAssets("AwakeningCeremonySystem t:Prefab");
                if (found != null && found.Length > 0)
                {
                    path = AssetDatabase.GUIDToAssetPath(found[0]);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            if (prefab == null)
            {
                report.AppendLine(
                    "  ⚠️ prefab AwakeningCeremonySystem.prefab introuvable " +
                    "(le controller est probablement monté en scène Game uniquement).");
                report.AppendLine(
                    "  → Vérifier manuellement AwakeningCeremonyController dans Game.unity " +
                    "(clips + overlayPrefab).");
                return;
            }

            report.AppendLine($"  Prefab : {path}");

            AwakeningCeremonyController controller =
                prefab.GetComponent<AwakeningCeremonyController>();
            if (controller == null)
                controller = prefab.GetComponentInChildren<AwakeningCeremonyController>(true);

            if (controller == null)
            {
                report.AppendLine("  ⚠️ AwakeningCeremonyController absent sur le prefab");
                return;
            }

            SerializedObject so = new SerializedObject(controller);

            for (int i = 0; i < CeremonyClipFields.Length; i++)
            {
                string field = CeremonyClipFields[i];
                SerializedProperty prop = so.FindProperty(field);
                if (prop == null)
                {
                    report.AppendLine($"  ⚠️ champ sérialisé introuvable : {field}");
                    continue;
                }

                if (prop.objectReferenceValue == null)
                    report.AppendLine($"  ⚠️ clip manquant : {field}");
                else
                    report.AppendLine($"  ✅ {field} : {prop.objectReferenceValue.name}");
            }

            SerializedProperty overlayProp = so.FindProperty("overlayPrefab");
            if (overlayProp == null)
            {
                report.AppendLine("  ⚠️ champ sérialisé introuvable : overlayPrefab");
            }
            else if (overlayProp.objectReferenceValue == null)
            {
                report.AppendLine("  ⚠️ clip manquant : overlayPrefab");
            }
            else
            {
                report.AppendLine($"  ✅ overlayPrefab : {overlayProp.objectReferenceValue.name}");
            }
        }
    }
}
#endif
