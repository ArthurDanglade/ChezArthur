#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Enemies;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Câblage et audit des sprites combat (personnages + ennemis) par identifiant.
    /// Convention : combat_&lt;id&gt;.png sous Art/Combat/Characters, Enemies ou Bosses.
    /// </summary>
    public static class CombatSpriteTools
    {
        private const string CharactersDataFolder = "Assets/_Project/Data/Characters";
        private const string EnemiesDataFolder = "Assets/_Project/ScriptableObjects/Enemies";
        private const string CharactersCombatFolder = "Assets/_Project/Art/Combat/Characters";
        private const string EnemiesCombatFolder = "Assets/_Project/Art/Combat/Enemies";
        private const string BossesCombatFolder = "Assets/_Project/Art/Combat/Bosses";
        private const string CombatPrefix = "combat_";
        // Seuil 1.4 = milieu géométrique entre l'idéal du 256 (collider 1.0) et celui du 512 (collider 2.0).
        private const float DensityThreshold = 1.4f;

        // ═══════════════════════════════════════════
        // MENU — CÂBLAGE
        // ═══════════════════════════════════════════
        [MenuItem("Chez Arthur/Art/Câbler Combat Sprites par Id")]
        public static void WireCombatSpritesById()
        {
            WireReport characterReport = WireCharacterCombatSprites();
            WireReport enemyReport = WireEnemyCombatSprites();

            AssetDatabase.SaveAssets();

            Debug.Log(
                "[CombatSpriteTools] Câblage terminé.\n" +
                "Personnages : câblé=" + characterReport.Wired +
                ", déjà câblé=" + characterReport.AlreadyWired +
                ", manquant=" + characterReport.Missing +
                ", réassigné=" + characterReport.Reassigned + "\n" +
                "Ennemis : câblé=" + enemyReport.Wired +
                ", déjà câblé=" + enemyReport.AlreadyWired +
                ", manquant=" + enemyReport.Missing +
                ", réassigné=" + enemyReport.Reassigned);
        }

        // ═══════════════════════════════════════════
        // MENU — AUDIT
        // ═══════════════════════════════════════════
        [MenuItem("Chez Arthur/Art/Audit Combat Sprites")]
        public static void AuditCombatSprites()
        {
            var report = new StringBuilder(4096);
            int errorCount = 0;
            int warningCount = 0;

            report.AppendLine("═══════════════════════════════════════════════════════════════");
            report.AppendLine("AUDIT COMBAT SPRITES");
            report.AppendLine("═══════════════════════════════════════════════════════════════");

            AuditCharacters(report, ref errorCount, ref warningCount);
            AuditEnemies(report, ref errorCount, ref warningCount);

            report.AppendLine();
            report.AppendLine("── RÉSUMÉ ───────────────────────────────────────────────────");
            report.AppendLine("Erreurs   : " + errorCount);
            report.AppendLine("Avertissements : " + warningCount);

            Debug.Log(report.ToString());
        }

        // ═══════════════════════════════════════════
        // CÂBLAGE — PERSONNAGES
        // ═══════════════════════════════════════════
        private static WireReport WireCharacterCombatSprites()
        {
            var report = new WireReport();
            string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharactersDataFolder });

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var data = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
                    if (data == null || string.IsNullOrEmpty(data.Id))
                        continue;

                    string expectedFileName = CombatPrefix + data.Id;
                    string spritePath = FindExactCombatPng(CharactersCombatFolder, expectedFileName);
                    if (spritePath == null)
                    {
                        report.Missing++;
                        continue;
                    }

                    Sprite sprite = LoadCombatSprite(spritePath, expectedFileName);
                    if (sprite == null)
                    {
                        report.Missing++;
                        Debug.LogWarning(
                            "[CombatSpriteTools] Sprite introuvable dans " + spritePath +
                            " (id=" + data.Id + ").");
                        continue;
                    }

                    TryAssignCombatSprite(data, sprite, spritePath, data.Id, ref report);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return report;
        }

        // ═══════════════════════════════════════════
        // CÂBLAGE — ENNEMIS
        // ═══════════════════════════════════════════
        private static WireReport WireEnemyCombatSprites()
        {
            var report = new WireReport();
            string[] guids = AssetDatabase.FindAssets("t:EnemyData", new[] { EnemiesDataFolder });
            var wiredIdleIds = new HashSet<string>();

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var data = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
                    if (data == null || string.IsNullOrEmpty(data.Id))
                        continue;

                    string idleFileName = CombatPrefix + data.Id + "_idle";
                    string idlePath = FindExactCombatPng(EnemiesCombatFolder, idleFileName);
                    if (idlePath != null)
                    {
                        if (TryWireEnemyIdle(data, idlePath, idleFileName, ref report))
                        {
                            wiredIdleIds.Add(data.Id);
                            continue;
                        }

                        // Idle présent mais 0 frame (non découpé) : ne pas abandonner le static.
                        Debug.LogWarning(
                            "[CombatSpriteTools] Idle sans frames pour id=" + data.Id +
                            " — repli sur sprite static si disponible.");
                    }

                    string expectedFileName = CombatPrefix + data.Id;
                    string enemiesPath = FindExactCombatPng(EnemiesCombatFolder, expectedFileName);
                    string bossesPath = FindExactCombatPng(BossesCombatFolder, expectedFileName);

                    if (enemiesPath != null && bossesPath != null)
                    {
                        Debug.LogError(
                            "[CombatSpriteTools] Conflit combat sprite pour id=" + data.Id +
                            " — Enemies: " + enemiesPath + " | Bosses: " + bossesPath);
                        report.Missing++;
                        continue;
                    }

                    string spritePath = enemiesPath ?? bossesPath;
                    if (spritePath == null)
                    {
                        report.Missing++;
                        continue;
                    }

                    Sprite sprite = LoadCombatSprite(spritePath, expectedFileName);
                    if (sprite == null)
                    {
                        report.Missing++;
                        Debug.LogWarning(
                            "[CombatSpriteTools] Sprite introuvable dans " + spritePath +
                            " (id=" + data.Id + ").");
                        continue;
                    }

                    TryAssignCombatSprite(data, sprite, spritePath, data.Id, ref report);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            ReportOrphanIdleSheets(wiredIdleIds);
            return report;
        }

        private static bool TryWireEnemyIdle(
            EnemyData data,
            string idlePath,
            string idleFileName,
            ref WireReport report)
        {
            List<Sprite> frames = LoadSortedIdleFrames(idlePath);
            if (frames.Count == 0)
            {
                report.Missing++;
                Debug.LogWarning(
                    "[CombatSpriteTools] Aucune frame idle dans " + idlePath + " (id=" + data.Id + ").");
                return false;
            }

            var so = new SerializedObject(data);
            SerializedProperty framesProp = so.FindProperty("idleFrames");
            SerializedProperty combatProp = so.FindProperty("combatSprite");
            if (framesProp == null || combatProp == null)
            {
                Debug.LogWarning("[CombatSpriteTools] Champs idleFrames/combatSprite introuvables sur " + data.name);
                return false;
            }

            bool changed = false;

            // combatSprite = frame 0
            Sprite frame0 = frames[0];
            if (combatProp.objectReferenceValue as Sprite != frame0)
            {
                if (combatProp.objectReferenceValue != null)
                    report.Reassigned++;
                else
                    report.Wired++;
                combatProp.objectReferenceValue = frame0;
                changed = true;
            }
            else
            {
                report.AlreadyWired++;
            }

            if (!AreSpriteListsEqual(framesProp, frames))
            {
                framesProp.ClearArray();
                framesProp.arraySize = frames.Count;
                for (int i = 0; i < frames.Count; i++)
                    framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                changed = true;
                Debug.Log(
                    "[CombatSpriteTools] idleFrames ← " + frames.Count + " frames (" +
                    Path.GetFileName(idlePath) + ", id=" + data.Id + ")");
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
            }

            return true;
        }

        private static bool AreSpriteListsEqual(SerializedProperty listProp, List<Sprite> frames)
        {
            if (listProp.arraySize != frames.Count)
                return false;

            for (int i = 0; i < frames.Count; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue as Sprite != frames[i])
                    return false;
            }

            return true;
        }

        private static List<Sprite> LoadSortedIdleFrames(string assetPath)
        {
            var frames = new List<Sprite>(16);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null)
                return frames;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    frames.Add(sprite);
            }

            frames.Sort((a, b) => TrailingNumber(a).CompareTo(TrailingNumber(b)));
            return frames;
        }

        private static int TrailingNumber(Sprite s)
        {
            if (s == null)
                return 0;
            string name = s.name;
            int i = name.Length - 1;
            while (i >= 0 && char.IsDigit(name[i]))
                i--;
            return int.TryParse(name.Substring(i + 1), out int n) ? n : 0;
        }

        /// <summary>
        /// Sheets idle sans EnemyData (alucadra, epee_volante…) — attendu avant G6a-P3/G6c.
        /// </summary>
        private static void ReportOrphanIdleSheets(HashSet<string> wiredIdleIds)
        {
            string folder = (EnemiesCombatFolder + "/U1/Idle").Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                // Recherche large sous Enemies/
                folder = EnemiesCombatFolder;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                if (!path.Contains("/Idle/"))
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith(CombatPrefix) || !fileName.EndsWith("_idle"))
                    continue;

                string id = fileName.Substring(
                    CombatPrefix.Length,
                    fileName.Length - CombatPrefix.Length - "_idle".Length);

                if (wiredIdleIds.Contains(id))
                    continue;

                Debug.Log(
                    "[CombatSpriteTools] Idle orphelin — attendu, câblage au gate suivant : " +
                    path + " (id=" + id + ")");
            }
        }

        private static void TryAssignCombatSprite(
            Object dataAsset,
            Sprite sprite,
            string spritePath,
            string entityId,
            ref WireReport report)
        {
            var so = new SerializedObject(dataAsset);
            SerializedProperty property = so.FindProperty("combatSprite");
            if (property == null)
            {
                Debug.LogWarning("[CombatSpriteTools] Champ combatSprite introuvable sur " + dataAsset.name);
                return;
            }

            Sprite current = property.objectReferenceValue as Sprite;
            if (current == sprite)
            {
                report.AlreadyWired++;
                return;
            }

            if (current != null)
            {
                string currentPath = AssetDatabase.GetAssetPath(current);
                Debug.Log(
                    "[CombatSpriteTools] Réassignation combatSprite pour id=" + entityId +
                    " : " + Path.GetFileName(currentPath) + " → " + Path.GetFileName(spritePath));
                report.Reassigned++;
            }
            else
            {
                report.Wired++;
            }

            property.objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dataAsset);
            Debug.Log(
                "[CombatSpriteTools] combatSprite ← " + Path.GetFileName(spritePath) + " (id=" + entityId + ")");
        }

        // ═══════════════════════════════════════════
        // AUDIT — PERSONNAGES
        // ═══════════════════════════════════════════
        private static void AuditCharacters(StringBuilder report, ref int errorCount, ref int warningCount)
        {
            report.AppendLine();
            report.AppendLine("── PERSONNAGES ────────────────────────────────────────────────");

            HashSet<string> knownIds = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { CharactersDataFolder });

            for (int i = 0; i < guids.Length; i++)
            {
                var data = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (data == null)
                    continue;

                if (string.IsNullOrEmpty(data.Id))
                {
                    report.AppendLine("ERREUR — CharacterData id vide : " + data.name);
                    errorCount++;
                    continue;
                }

                knownIds.Add(data.Id);

                if (data.CombatSprite == null)
                {
                    report.AppendLine("ERREUR — CharacterData combatSprite null : id=" + data.Id);
                    errorCount++;
                    continue;
                }

                string spritePath = AssetDatabase.GetAssetPath(data.CombatSprite);
                if (!IsUnderCombatRoot(spritePath, CharactersCombatFolder))
                {
                    report.AppendLine(
                        "AVERTISSEMENT — combatSprite hors Characters/ : id=" + data.Id +
                        " → " + spritePath);
                    warningCount++;
                }
            }

            AppendOrphanCombatPngs(report, CharactersCombatFolder, knownIds, "INFO — PNG orphelins Characters/");
        }

        // ═══════════════════════════════════════════
        // AUDIT — ENNEMIS
        // ═══════════════════════════════════════════
        private static void AuditEnemies(StringBuilder report, ref int errorCount, ref int warningCount)
        {
            report.AppendLine();
            report.AppendLine("── ENNEMIS ────────────────────────────────────────────────────");

            HashSet<string> knownIds = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("t:EnemyData", new[] { EnemiesDataFolder });

            for (int i = 0; i < guids.Length; i++)
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (data == null)
                    continue;

                if (string.IsNullOrEmpty(data.Id))
                {
                    report.AppendLine("ERREUR — EnemyData id vide : " + data.name);
                    errorCount++;
                    continue;
                }

                knownIds.Add(data.Id);

                if (data.CombatSprite == null)
                {
                    report.AppendLine("ERREUR — EnemyData combatSprite null : id=" + data.Id);
                    errorCount++;
                    continue;
                }

                string spritePath = AssetDatabase.GetAssetPath(data.CombatSprite);
                bool underEnemies = IsUnderCombatRoot(spritePath, EnemiesCombatFolder);
                bool underBosses = IsUnderCombatRoot(spritePath, BossesCombatFolder);

                if (!underEnemies && !underBosses)
                {
                    report.AppendLine(
                        "AVERTISSEMENT — combatSprite hors Enemies/ et Bosses/ (batching cassé) : id=" +
                        data.Id + " → " + spritePath);
                    warningCount++;
                }

                float maxDim = Mathf.Max(data.ColliderWidth, data.ColliderHeight);
                if (underEnemies && maxDim > DensityThreshold)
                {
                    report.AppendLine(
                        "AVERTISSEMENT — densité : id=" + data.Id +
                        " — canvas 256 pour un collider de " + maxDim.ToString("F1") +
                        " — envisager Bosses/ (512)");
                    warningCount++;
                }
                else if (underBosses && maxDim <= DensityThreshold)
                {
                    report.AppendLine(
                        "AVERTISSEMENT — densité : id=" + data.Id +
                        " — canvas 512 sur-échantillonné pour un collider de " + maxDim.ToString("F1") +
                        " — envisager Enemies/ (256)");
                    warningCount++;
                }

                string idlePath = FindExactCombatPng(EnemiesCombatFolder, CombatPrefix + data.Id + "_idle");
                if (idlePath == null && data.UniverseIndex == 1)
                {
                    report.AppendLine(
                        "AVERTISSEMENT — EnemyData U1 sans sheet idle : id=" + data.Id);
                    warningCount++;
                }
                else if (idlePath != null && (data.IdleFrames == null || data.IdleFrames.Count == 0))
                {
                    report.AppendLine(
                        "AVERTISSEMENT — sheet idle présente mais idleFrames vide : id=" + data.Id);
                    warningCount++;
                }
            }

            var enemyIds = new HashSet<string>(knownIds);
            AppendOrphanCombatPngs(report, EnemiesCombatFolder, enemyIds, "INFO — PNG orphelins Enemies/");
            AppendOrphanCombatPngs(report, BossesCombatFolder, enemyIds, "INFO — PNG orphelins Bosses/");
            AppendOrphanIdleSheets(report, enemyIds);
        }

        private static void AppendOrphanIdleSheets(StringBuilder report, HashSet<string> knownIds)
        {
            string folder = EnemiesCombatFolder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
                return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            bool sectionOpened = false;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                if (!path.Contains("/Idle/"))
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith(CombatPrefix) || !fileName.EndsWith("_idle"))
                    continue;

                string id = fileName.Substring(
                    CombatPrefix.Length,
                    fileName.Length - CombatPrefix.Length - "_idle".Length);

                if (knownIds.Contains(id))
                    continue;

                if (!sectionOpened)
                {
                    report.AppendLine("INFO — Idle orphelins (attendu avant G6a-P3/G6c) :");
                    sectionOpened = true;
                }

                report.AppendLine("  " + path + " (id=" + id + ")");
            }
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════
        private static string FindExactCombatPng(string rootFolder, string expectedFileName)
        {
            string folder = rootFolder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
                return null;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName == expectedFileName)
                    return path;
            }

            return null;
        }

        private static Sprite LoadCombatSprite(string assetPath, string expectedSpriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null || assets.Length == 0)
                return null;

            Sprite namedMatch = null;
            Sprite fallback = null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    if (sprite.name == expectedSpriteName)
                        namedMatch = sprite;
                    if (fallback == null)
                        fallback = sprite;
                }
            }

            return namedMatch != null ? namedMatch : fallback;
        }

        private static bool IsUnderCombatRoot(string assetPath, string rootFolder)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            string normalizedPath = assetPath.Replace('\\', '/');
            string normalizedRoot = rootFolder.TrimEnd('/') + "/";
            return normalizedPath.StartsWith(normalizedRoot);
        }

        private static void AppendOrphanCombatPngs(
            StringBuilder report,
            string rootFolder,
            HashSet<string> knownIds,
            string sectionTitle)
        {
            string folder = rootFolder.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
                return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            bool sectionOpened = false;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                // Les sheets idle sont auditées séparément (AppendOrphanIdleSheets).
                if (path.Contains("/Idle/"))
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith(CombatPrefix))
                    continue;
                if (fileName.EndsWith("_idle"))
                    continue;

                string orphanId = fileName.Substring(CombatPrefix.Length);
                if (string.IsNullOrEmpty(orphanId) || knownIds.Contains(orphanId))
                    continue;

                if (!sectionOpened)
                {
                    report.AppendLine(sectionTitle);
                    sectionOpened = true;
                }

                report.AppendLine("  " + path);
            }
        }

        private struct WireReport
        {
            public int Wired;
            public int AlreadyWired;
            public int Missing;
            public int Reassigned;
        }
    }
}
#endif
