using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay;
using ChezArthur.Roguelike;
using ChezArthur.UI;

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
        [SerializeField] private StageGenerator stageGenerator;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private CharacterBallFactory characterBallFactory;

        [Header("Positions de spawn des alliés")]
        [SerializeField] private List<Vector2> allySpawnPositions = new List<Vector2>
        {
            new Vector2(-3f, -6f),
            new Vector2(-1f, -6f),
            new Vector2(1f, -6f),
            new Vector2(3f, -6f)
        };

        [Header("Régénération entre étages")]
        [SerializeField] [Range(0f, 1f)] private float healPercentBetweenStages = 0.1f; // 10%

        [Header("UI Bonus")]
        [SerializeField] private BonusSelectionUI bonusSelectionUI;

        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int BONUS_SELECTION_INTERVAL = 3;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static RunManager _instance;
        private RunState _currentState = RunState.NotStarted;
        private int _currentStage = 1;
        private int _talsEarned;
        private List<CharacterBall> _spawnedAllies = new List<CharacterBall>();

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

        private void Start()
        {
            // Temporaire : démarre automatiquement la run pour le prototype
            StartRun();
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
        /// Démarre une nouvelle run (reset complet : stage, Tals, alliés, état).
        /// </summary>
        public void StartRun()
        {
            _currentStage = 1;
            _talsEarned = 0;
            _currentState = RunState.InProgress;

            // Remet le jeu en état Playing
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Playing);

            // Reset les bonus en début de run
            if (BonusManager.Instance != null)
                BonusManager.Instance.Initialize();

            // Détruit les anciennes balles spawnées (si re-run)
            for (int i = _spawnedAllies.Count - 1; i >= 0; i--)
            {
                if (_spawnedAllies[i] != null)
                    Destroy(_spawnedAllies[i].gameObject);
            }
            _spawnedAllies.Clear();

            // Récupère l'équipe depuis PersistentManager et spawn, ou fallback sur initialTeam
            List<CharacterBall> alliesToUse = null;
            if (characterBallFactory != null && PersistentManager.Instance != null && PersistentManager.Instance.Characters != null)
            {
                var team = PersistentManager.Instance.Characters.GetSelectedTeam();
                if (team != null && team.Count > 0 && allySpawnPositions != null && allySpawnPositions.Count > 0)
                {
                    List<CharacterBall> spawned = characterBallFactory.SpawnTeam(team, allySpawnPositions, turnManager);
                    if (spawned != null && spawned.Count > 0)
                    {
                        _spawnedAllies.AddRange(spawned);
                        alliesToUse = spawned;
                    }
                }
            }

            if (alliesToUse == null)
                Debug.LogWarning("[RunManager] PersistentManager, équipe vide ou factory manquante : utilisation de l'équipe par défaut (initialAllies).");

            if (turnManager != null)
            {
                if (alliesToUse != null)
                    turnManager.Initialize(alliesToUse);
                else
                    turnManager.Initialize();

                turnManager.ReviveAllAllies();
                turnManager.ResetAlliesPositions(allySpawnPositions);
            }

            // Initialise les passifs de chaque allié selon sa spé et son niveau
            InitializeAlliesPassives();

            // Génère le premier étage
            if (stageGenerator != null)
                stageGenerator.GenerateStage(_currentStage);

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
        /// Appelé quand tous les ennemis sont morts (victoire d'étage).
        /// Affiche la sélection de bonus tous les 3 étages, sinon continue vers l'étage suivant.
        /// </summary>
        public void CompleteStage()
        {
            int completedStage = _currentStage;
            _currentStage++;
            OnStageCompleted?.Invoke(completedStage);

            // Bonus au premier étage (hook) puis tous les 3 étages à partir de l'étage 4
            bool isFirstStageHook = (completedStage == 1);
            bool isRegularBonusStage = (completedStage > 1 && (completedStage - 1) % BONUS_SELECTION_INTERVAL == 0);

            if (isFirstStageHook || isRegularBonusStage)
            {
                if (bonusSelectionUI != null)
                {
                    bonusSelectionUI.OnSelectionComplete += OnBonusSelectionComplete;
                    bonusSelectionUI.Show();
                    return;
                }
            }

            ContinueToNextStage();
        }

        /// <summary>
        /// Appelé quand le joueur a choisi son bonus.
        /// </summary>
        private void OnBonusSelectionComplete()
        {
            if (bonusSelectionUI != null)
                bonusSelectionUI.OnSelectionComplete -= OnBonusSelectionComplete;

            ContinueToNextStage();
        }

        /// <summary>
        /// Continue vers l'étage suivant (repositionnement, heal, génération).
        /// </summary>
        private void ContinueToNextStage()
        {
            // Bloque les changements de tour pendant la transition
            if (turnManager != null)
                turnManager.SetTurnChangeEnabled(false);

            // Remet le jeu en état Playing pour l'étage suivant
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Playing);

            // Repositionne les alliés vivants
            if (turnManager != null)
                turnManager.ResetAlliesPositions(allySpawnPositions);

            // Régénère les HP des alliés vivants
            if (turnManager != null)
                turnManager.HealAllAllies(healPercentBetweenStages);

            // Reset les stacks des passifs qui se reset par étage + Notify OnStageStart
            ResetAlliesPassivesForNewStage();

            // Reset l'ordre des tours (le plus rapide recommence)
            if (turnManager != null)
                turnManager.ResetTurnOrder();

            // Génère l'étage suivant
            if (stageGenerator != null)
                stageGenerator.GenerateStage(_currentStage);

            // Réactive les changements de tour après un court délai (laisse le temps au personnage de s'arrêter)
            Invoke(nameof(ReenableTurnChange), 0.5f);
        }

        /// <summary>
        /// Termine la run (victoire ou défaite).
        /// </summary>
        public void EndRun(bool victory)
        {
            Debug.Log($"[RunManager] EndRun appelé, victory = {victory}");
            _currentState = victory ? RunState.Victory : RunState.Defeat;
            OnRunEnded?.Invoke(victory);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void HandleDefeat()
        {
            Debug.Log("[RunManager] HandleDefeat appelé");
            EndRun(false);
        }

        private void ReenableTurnChange()
        {
            if (turnManager != null)
                turnManager.SetTurnChangeEnabled(true);
        }

        /// <summary>
        /// Initialise le CharacterPassiveRuntime de chaque allié avec la bonne spécialisation et le bon niveau.
        /// </summary>
        private void InitializeAlliesPassives()
        {
            if (turnManager == null) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null) continue;

                CharacterPassiveRuntime runtime = ally.GetComponent<CharacterPassiveRuntime>();
                if (runtime == null) continue;

                CharacterData charData = ally.Data;
                if (charData == null) continue;

                int level = 1;
                int specIndex = -1;
                if (PersistentManager.Instance != null && PersistentManager.Instance.Characters != null)
                {
                    OwnedCharacter owned = PersistentManager.Instance.Characters.GetOwnedCharacter(charData.Id);
                    if (owned != null)
                    {
                        level = owned.level;
                        specIndex = owned.GetSpecialization();
                    }
                }

                SpecializationData spec = charData.GetSpecialization(specIndex);
                runtime.InitializeForRun(spec, level, specIndex);
            }
        }

        /// <summary>
        /// Reset les stacks des passifs ResetPerStage pour tous les alliés vivants. Notifie également OnStageStart.
        /// </summary>
        private void ResetAlliesPassivesForNewStage()
        {
            if (turnManager == null) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;

                CharacterPassiveRuntime runtime = ally.GetComponent<CharacterPassiveRuntime>();
                if (runtime == null) continue;

                runtime.ResetForNewStage();
                runtime.NotifyTrigger(PassiveTrigger.OnStageStart);
            }
        }
    }
}
