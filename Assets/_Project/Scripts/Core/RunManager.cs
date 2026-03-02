using System;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Core
{
    /// <summary>
    /// État d'une run roguelike.
    /// </summary>
    public enum RunState
    {
        /// <summary> Run pas encore démarrée. </summary>
        NotStarted,

        /// <summary> Run en cours (étages, Tals). </summary>
        InProgress,

        /// <summary> Run terminée en victoire. </summary>
        Victory,

        /// <summary> Run terminée en défaite. </summary>
        Defeat
    }

    /// <summary>
    /// Gère une run roguelike complète : étages, Tals, victoire/défaite. Singleton sans DontDestroyOnLoad (reset à chaque partie).
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private CombatManager combatManager;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static RunManager _instance;
        private RunState _currentState = RunState.NotStarted;
        private int _currentStage = 1;
        private int _talsEarned;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Instance unique du RunManager (scène courante, pas DontDestroyOnLoad). </summary>
        public static RunManager Instance => _instance;

        /// <summary> État actuel de la run. </summary>
        public RunState CurrentState => _currentState;

        /// <summary> Étage actuel (commence à 1). </summary>
        public int CurrentStage => _currentStage;

        /// <summary> Tals gagnés cette run. </summary>
        public int TalsEarned => _talsEarned;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché au démarrage d'une run. </summary>
        public event Action OnRunStarted;

        /// <summary> Déclenché quand le total de Tals change. Paramètre : nouveau total. </summary>
        public event Action<int> OnTalsChanged;

        /// <summary> Déclenché quand un étage est complété. Paramètre : numéro de l'étage complété. </summary>
        public event Action<int> OnStageCompleted;

        /// <summary> Déclenché à la fin de la run. Paramètre : true = victoire, false = défaite. </summary>
        public event Action<bool> OnRunEnded;

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

            if (combatManager != null)
            {
                combatManager.OnVictory += CompleteStage;
                combatManager.OnDefeat += HandleDefeat;
            }
        }

        private void OnDestroy()
        {
            if (combatManager != null)
            {
                combatManager.OnVictory -= CompleteStage;
                combatManager.OnDefeat -= HandleDefeat;
            }
            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Démarre une nouvelle run (reset stage et Tals, état InProgress).
        /// </summary>
        public void StartRun()
        {
            _currentStage = 1;
            _talsEarned = 0;
            _currentState = RunState.InProgress;
            OnRunStarted?.Invoke();
        }

        /// <summary>
        /// Ajoute des Tals au total de la run.
        /// </summary>
        public void AddTals(int amount)
        {
            _talsEarned += amount;
            OnTalsChanged?.Invoke(_talsEarned);
        }

        /// <summary>
        /// Appelé quand tous les ennemis sont morts (victoire d'étage). N'appelle pas le générateur d'étage.
        /// </summary>
        public void CompleteStage()
        {
            int completedStage = _currentStage;
            _currentStage++;
            OnStageCompleted?.Invoke(completedStage);
        }

        /// <summary>
        /// Termine la run (victoire ou défaite).
        /// </summary>
        public void EndRun(bool victory)
        {
            _currentState = victory ? RunState.Victory : RunState.Defeat;
            OnRunEnded?.Invoke(victory);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void HandleDefeat()
        {
            EndRun(false);
        }
    }
}
