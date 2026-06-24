using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChezArthur.Core
{
    /// <summary>
    /// Gestionnaire statique pour charger les scènes. Garantit Time.timeScale = 1 avant chaque chargement.
    /// </summary>
    public static class SceneLoader
    {
        private const string HUB_SCENE_NAME = "Hub";
        private const string GAME_SCENE_NAME = "Game";

        /// <summary>
        /// Charge la scène de jeu (lance une run).
        /// </summary>
        public static void LoadGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GAME_SCENE_NAME);
        }

        /// <summary>
        /// Charge la scène Hub (retour après défaite ou depuis le menu).
        /// </summary>
        public static void LoadHub()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(HUB_SCENE_NAME);
        }
    }
}
