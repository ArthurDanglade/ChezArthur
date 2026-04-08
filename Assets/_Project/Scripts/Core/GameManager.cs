using System;
using UnityEngine;
using ChezArthur.Enemies.Passives.Handlers;

namespace ChezArthur.Core
{
    /// <summary>
    /// Singleton global : gère l'état du jeu et notifie les changements.
    /// Persiste entre les scènes (DontDestroyOnLoad).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static GameManager _instance;
        // Temporaire : démarrage en Playing pour tester le prototype. Remettre Menu quand l'UI sera en place.
        private GameState _currentState = GameState.Playing;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Instance unique du GameManager (persistante entre scènes). </summary>
        public static GameManager Instance => _instance;

        /// <summary> État actuel du jeu. </summary>
        public GameState CurrentState => _currentState;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché à chaque changement d'état. Paramètres : (état précédent, nouvel état). </summary>
        public event Action<GameState, GameState> OnStateChanged;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Enregistrement des handlers de passifs ennemis
            // Doit être appelé avant tout Initialize() d'ennemi
            EnemyPassiveHandlerRegistry.RegisterAll();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Change l'état du jeu, log en console et invoque OnStateChanged.
        /// </summary>
        public void ChangeState(GameState newState)
        {
            GameState previousState = _currentState;
            _currentState = newState;

            Debug.Log($"[GameManager] État : {previousState} → {newState}");
            OnStateChanged?.Invoke(previousState, newState);
        }

        /// <summary>
        /// Démarre une run (passe en Playing).
        /// </summary>
        public void StartRun()
        {
            ChangeState(GameState.Playing);

            // Démarre une nouvelle run roguelike
            if (RunManager.Instance != null)
                RunManager.Instance.StartRun();
        }

        /// <summary>
        /// Met le jeu en pause. Ignoré si l'état actuel n'est pas Playing.
        /// </summary>
        public void PauseGame()
        {
            if (_currentState != GameState.Playing)
                return;
            ChangeState(GameState.Paused);
        }

        /// <summary>
        /// Reprend la partie. Ignoré si l'état actuel n'est pas Paused.
        /// </summary>
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused)
                return;
            ChangeState(GameState.Playing);
        }

        /// <summary>
        /// Déclare la victoire (passe en Victory).
        /// </summary>
        public void Victory()
        {
            ChangeState(GameState.Victory);
        }

        /// <summary>
        /// Déclare la défaite (passe en Defeat).
        /// </summary>
        public void Defeat()
        {
            ChangeState(GameState.Defeat);
        }

        /// <summary>
        /// Retour au menu principal (passe en Menu).
        /// </summary>
        public void ReturnToMenu()
        {
            ChangeState(GameState.Menu);
        }
    }
}
