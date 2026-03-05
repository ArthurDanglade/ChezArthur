using UnityEngine;

namespace ChezArthur.Core
{
    /// <summary>
    /// Manager persistant entre les scènes. Stocke les données du joueur.
    /// </summary>
    public class PersistentManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static PersistentManager Instance { get; private set; }

        // ═══════════════════════════════════════════
        // DONNÉES JOUEUR
        // ═══════════════════════════════════════════
        [Header("Données joueur")]
        [SerializeField] private string playerName = "Voyageur";
        [SerializeField] private int tals = 0;
        [SerializeField] private int bestStage = 0;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string PlayerName => playerName;
        public int Tals => tals;
        public int BestStage => bestStage;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            // Singleton pattern avec DontDestroyOnLoad
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Définit le pseudo du joueur.
        /// </summary>
        public void SetPlayerName(string name)
        {
            if (!string.IsNullOrEmpty(name))
                playerName = name;
        }

        /// <summary>
        /// Ajoute des Tals au joueur.
        /// </summary>
        public void AddTals(int amount)
        {
            if (amount > 0)
                tals += amount;
        }

        /// <summary>
        /// Dépense des Tals si le joueur en a assez.
        /// </summary>
        /// <returns>True si la dépense a réussi, false sinon.</returns>
        public bool SpendTals(int amount)
        {
            if (amount <= 0 || tals < amount)
                return false;

            tals -= amount;
            return true;
        }

        /// <summary>
        /// Met à jour le meilleur étage si le nouveau est supérieur.
        /// </summary>
        public void UpdateBestStage(int stage)
        {
            if (stage > bestStage)
                bestStage = stage;
        }

        /// <summary>
        /// Réinitialise toutes les données (pour debug ou reset).
        /// </summary>
        public void ResetAllData()
        {
            playerName = "Voyageur";
            tals = 0;
            bestStage = 0;
        }
    }
}
