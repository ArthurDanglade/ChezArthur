using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;

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

        [Header("Base de données")]
        [SerializeField] private CharacterDatabase characterDatabase;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private CharacterManager _characterManager;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string PlayerName => playerName;
        public int Tals => tals;
        public int BestStage => bestStage;

        /// <summary>
        /// Accès au gestionnaire de personnages.
        /// </summary>
        public CharacterManager Characters => _characterManager;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand les données joueur sont modifiées. </summary>
        public event Action OnDataChanged;

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

            // Initialise le CharacterManager
            if (characterDatabase != null)
            {
                _characterManager = new CharacterManager(characterDatabase);
            }
            else
            {
                Debug.LogError("[PersistentManager] CharacterDatabase non assignée !");
            }

            LoadGame();
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
            {
                playerName = name;
                SaveGame();
                OnDataChanged?.Invoke();
            }
        }

        /// <summary>
        /// Ajoute des Tals au joueur.
        /// </summary>
        public void AddTals(int amount)
        {
            if (amount > 0)
            {
                tals += amount;
                SaveGame();
                OnDataChanged?.Invoke();
            }
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
            SaveGame();
            OnDataChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Met à jour le meilleur étage si le nouveau est supérieur.
        /// </summary>
        public void UpdateBestStage(int stage)
        {
            if (stage > bestStage)
            {
                bestStage = stage;
                SaveGame();
                OnDataChanged?.Invoke();
            }
        }

        /// <summary>
        /// Réinitialise toutes les données (pour debug ou reset).
        /// </summary>
        public void ResetAllData()
        {
            playerName = "Voyageur";
            tals = 0;
            bestStage = 0;
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Sauvegarde les données du joueur sur le disque.
        /// </summary>
        public void SaveGame()
        {
            SaveData data = new SaveData
            {
                playerName = this.playerName,
                tals = this.tals,
                bestStage = this.bestStage
            };

            // Sauvegarder les personnages
            if (_characterManager != null)
            {
                var (owned, team) = _characterManager.GetSaveData();
                data.ownedCharacters = new List<OwnedCharacter>(owned);
                data.selectedTeamIds = new List<string>(team);
            }

            SaveSystem.Save(data);
        }

        /// <summary>
        /// Charge les données du joueur depuis le disque.
        /// </summary>
        public void LoadGame()
        {
            SaveData data = SaveSystem.Load();
            playerName = data.playerName;
            tals = data.tals;
            bestStage = data.bestStage;

            // Charger les personnages
            if (_characterManager != null)
            {
                _characterManager.LoadFromSaveData(data.ownedCharacters, data.selectedTeamIds);
            }
        }
    }
}
