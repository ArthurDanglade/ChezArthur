using UnityEngine;
using System;
using System.IO;

namespace ChezArthur.Core
{
    /// <summary>
    /// Système de sauvegarde/chargement en JSON.
    /// Frontière : PlayerPrefs = préférences device (volumes, langue…) ;
    /// save.json = progression joueur (Tals, personnages, pity, missions…).
    /// Écriture atomique (tmp → bak → save) + quarantaine anti-corruption.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_FILE_NAME = "save.json";
        private const string TMP_SUFFIX = ".tmp";
        private const string BAK_SUFFIX = ".bak";
        private const string CORRUPT_PREFIX = "save.json.corrupt-";

        /// <summary>
        /// True si une save illisible n'a pas pu être mise en quarantaine :
        /// toute écriture est alors refusée pour préserver la preuve.
        /// </summary>
        private static bool _saveBlockedUntilQuarantine;

        /// <summary> Version courante du schéma de sauvegarde (stampée à l'écriture). </summary>
        public const int CURRENT_SAVE_VERSION = 3;

        /// <summary> Chemin complet du fichier de sauvegarde. </summary>
        private static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        private static string TmpPath => SavePath + TMP_SUFFIX;
        private static string BakPath => SavePath + BAK_SUFFIX;

        /// <summary>
        /// Sauvegarde les données dans un fichier JSON (écriture atomique).
        /// </summary>
        public static void Save(SaveData data)
        {
            string step = "verrou";
            try
            {
                if (_saveBlockedUntilQuarantine)
                {
                    Debug.LogError("[SaveSystem] Écriture refusée : save corrompue non quarantinée.");
                    return;
                }

                step = "sérialisation";
                data.saveVersion = CURRENT_SAVE_VERSION;
                string json = JsonUtility.ToJson(data, true);

                step = "écriture tmp";
                File.WriteAllText(TmpPath, json);

                step = "rotation backup";
                if (File.Exists(SavePath))
                {
                    if (File.Exists(BakPath))
                        File.Delete(BakPath);
                    File.Move(SavePath, BakPath);
                }

                step = "promotion";
                File.Move(TmpPath, SavePath);

                Debug.Log($"[SaveSystem] Sauvegarde réussie : {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Erreur de sauvegarde (étape={step}) : {e.Message}");
                TryRollbackFromBackup();
            }
        }

        /// <summary>
        /// Charge les données depuis le fichier JSON (quarantaine + backup + migration).
        /// </summary>
        /// <returns>Les données chargées, ou une nouvelle instance si aucune récupération possible.</returns>
        public static SaveData Load()
        {
            if (File.Exists(SavePath))
            {
                SaveData primary = TryParseFile(SavePath, out Exception parseError);
                if (primary != null)
                {
                    _saveBlockedUntilQuarantine = false;
                    SaveMigrator.MigrateToCurrent(primary);
                    CleanupOrphanTmp();
                    Debug.Log($"[SaveSystem] Chargement réussi : {SavePath}");
                    return primary;
                }

                Debug.LogError($"[SaveSystem] Parse save.json échoué : {(parseError != null ? parseError.Message : "résultat null")}");

                if (!TryQuarantineCorruptFile(SavePath))
                {
                    _saveBlockedUntilQuarantine = true;
                    Debug.LogError("[SaveSystem] Preuve non préservée, écritures gelées.");
                    return new SaveData();
                }

                SaveData fromBak = TryLoadBackup();
                if (fromBak != null)
                    return fromBak;

                Debug.LogError("[SaveSystem] Aucune récupération possible.");
                return new SaveData();
            }

            // save.json absent — crash possible en pleine écriture, ou première install
            SaveData fromTmp = TryPromoteTmp();
            if (fromTmp != null)
                return fromTmp;

            SaveData fromBakOnly = TryLoadBackup();
            if (fromBakOnly != null)
                return fromBakOnly;

            Debug.Log("[SaveSystem] Aucune sauvegarde trouvée, création de nouvelles données.");
            return new SaveData();
        }

        /// <summary>
        /// Vérifie si une sauvegarde existe.
        /// </summary>
        public static bool SaveExists()
        {
            return File.Exists(SavePath);
        }

        /// <summary>
        /// Supprime la sauvegarde (pour reset ou debug).
        /// Supprime aussi .tmp et .bak — jamais les .corrupt-*.
        /// </summary>
        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath))
                    File.Delete(SavePath);
                if (File.Exists(TmpPath))
                    File.Delete(TmpPath);
                if (File.Exists(BakPath))
                    File.Delete(BakPath);

                _saveBlockedUntilQuarantine = false;
                Debug.Log("[SaveSystem] Sauvegarde supprimée.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Erreur de suppression : {e.Message}");
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Rollback minimal après échec d'écriture : restaure save.json depuis .bak si besoin.
        /// </summary>
        private static void TryRollbackFromBackup()
        {
            try
            {
                if (!File.Exists(SavePath) && File.Exists(BakPath))
                    File.Move(BakPath, SavePath);
            }
            catch
            {
                // Silencieux — on a déjà logué l'erreur principale.
            }
        }

        private static SaveData TryParseFile(string path, out Exception error)
        {
            error = null;
            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    error = new Exception("JsonUtility a renvoyé null");
                    return null;
                }

                return data;
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }
        }

        /// <summary>
        /// Déplace un fichier corrompu vers save.json.corrupt-&lt;timestamp&gt; (jamais écrasé).
        /// </summary>
        private static bool TryQuarantineCorruptFile(string path)
        {
            try
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                string dest = Path.Combine(Application.persistentDataPath, CORRUPT_PREFIX + stamp);
                int suffix = 2;
                while (File.Exists(dest))
                {
                    dest = Path.Combine(Application.persistentDataPath, CORRUPT_PREFIX + stamp + "-" + suffix);
                    suffix++;
                }

                File.Move(path, dest);
                Debug.LogWarning($"[SaveSystem] Save corrompue mise en quarantaine : {dest}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Échec quarantaine : {e.Message}");
                return false;
            }
        }

        private static SaveData TryLoadBackup()
        {
            if (!File.Exists(BakPath))
                return null;

            SaveData data = TryParseFile(BakPath, out Exception error);
            if (data == null)
            {
                Debug.LogError($"[SaveSystem] Backup illisible : {(error != null ? error.Message : "null")}");
                return null;
            }

            _saveBlockedUntilQuarantine = false;
            SaveMigrator.MigrateToCurrent(data);
            CleanupOrphanTmp();
            Debug.LogWarning("[SaveSystem] Restauré depuis backup.");
            return data;
        }

        private static SaveData TryPromoteTmp()
        {
            if (!File.Exists(TmpPath))
                return null;

            SaveData data = TryParseFile(TmpPath, out Exception error);
            if (data == null)
            {
                Debug.LogError($"[SaveSystem] Fichier temporaire illisible : {(error != null ? error.Message : "null")}");
                return null;
            }

            try
            {
                File.Move(TmpPath, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Promotion tmp échouée : {e.Message}");
                return null;
            }

            _saveBlockedUntilQuarantine = false;
            SaveMigrator.MigrateToCurrent(data);
            Debug.LogWarning("[SaveSystem] Promotion du fichier temporaire.");
            return data;
        }

        private static void CleanupOrphanTmp()
        {
            try
            {
                if (File.Exists(TmpPath) && File.Exists(SavePath))
                    File.Delete(TmpPath);
            }
            catch
            {
                // Silencieux — résidu non bloquant.
            }
        }
    }
}
