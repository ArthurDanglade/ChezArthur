using UnityEngine;
using System.IO;

namespace ChezArthur.Core
{
    /// <summary>
    /// Système de sauvegarde/chargement en JSON.
    /// </summary>
    public static class SaveSystem
    {
        private const string SAVE_FILE_NAME = "save.json";

        /// <summary>
        /// Chemin complet du fichier de sauvegarde.
        /// </summary>
        private static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        /// <summary>
        /// Sauvegarde les données dans un fichier JSON.
        /// </summary>
        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] Sauvegarde réussie : {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Erreur de sauvegarde : {e.Message}");
            }
        }

        /// <summary>
        /// Charge les données depuis le fichier JSON.
        /// </summary>
        /// <returns>Les données chargées, ou une nouvelle instance si le fichier n'existe pas.</returns>
        public static SaveData Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    SaveData data = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"[SaveSystem] Chargement réussi : {SavePath}");
                    return data;
                }
                else
                {
                    Debug.Log("[SaveSystem] Aucune sauvegarde trouvée, création de nouvelles données.");
                    return new SaveData();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Erreur de chargement : {e.Message}");
                return new SaveData();
            }
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
        /// </summary>
        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    Debug.Log("[SaveSystem] Sauvegarde supprimée.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Erreur de suppression : {e.Message}");
            }
        }
    }
}
