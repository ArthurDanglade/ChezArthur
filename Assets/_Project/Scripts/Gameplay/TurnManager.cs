using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Passives.Handlers;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Gère les tours de tous les participants au combat (alliés + ennemis) dans une liste unifiée triée par Speed.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Participants (prototype)")]
        [SerializeField] private List<CharacterBall> initialAllies = new List<CharacterBall>();

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<ITurnParticipant> _participants = new List<ITurnParticipant>();
        private List<CharacterBall> _runtimeAllies = new List<CharacterBall>();
        private int _currentIndex;
        private bool _ignoreTurnChange;
        /// <summary> Profondeur d'appel NextTurn (évite double TickTurn si un listener enchaîne un autre NextTurn, ex. skip gel). </summary>
        private int _turnProcessingDepth;
        private List<Action> _onStoppedHandlers = new List<Action>();
        private List<Action> _onDeathHandlers = new List<Action>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Liste unifiée des participants (lecture seule). </summary>
        public IReadOnlyList<ITurnParticipant> Participants => _participants;

        /// <summary> Participant dont c'est le tour (null si aucun ou tous morts). </summary>
        public ITurnParticipant CurrentParticipant => GetCurrentParticipant();

        /// <summary> True si un participant actif existe et n'est pas mort. </summary>
        public bool HasCurrentParticipant => CurrentParticipant != null && !CurrentParticipant.IsDead;

        /// <summary> True si c'est au tour d'un allié. </summary>
        public bool IsPlayerTurn => CurrentParticipant != null && CurrentParticipant.IsAlly;

        /// <summary> Nombre d'alliés encore en vie. </summary>
        public int AliveAlliesCount => GetAliveCount(true);

        /// <summary> Nombre d'ennemis encore en vie. </summary>
        public int AliveEnemiesCount => GetAliveCount(false);

        /// <summary> Liste des alliés (pour initialisation passifs, reset étage). </summary>
        public IReadOnlyList<CharacterBall> GetAllies() => _runtimeAllies;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand un participant meurt. </summary>
        public event Action<ITurnParticipant> OnParticipantDeath;

        /// <summary> Déclenché quand tous les alliés sont morts (défaite). </summary>
        public event Action OnAllAlliesDead;

        /// <summary> Déclenché quand tous les ennemis sont morts (victoire d'étage). </summary>
        public event Action OnAllEnemiesDead;

        /// <summary> Déclenché quand le tour change. Paramètre : nouveau participant actif. </summary>
        public event Action<ITurnParticipant> OnTurnChanged;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _participants.Count; i++)
            {
                if (i < _onStoppedHandlers.Count)
                    _participants[i].OnStopped -= _onStoppedHandlers[i];
                if (i < _onDeathHandlers.Count)
                    _participants[i].OnDeath -= _onDeathHandlers[i];
            }
            _onStoppedHandlers.Clear();
            _onDeathHandlers.Clear();
            _participants.Clear();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise la liste des participants avec les alliés (initialAllies de l'Inspector), trie par Speed et s'abonne aux events.
        /// Les ennemis sont ajoutés via AddEnemies().
        /// </summary>
        public void Initialize()
        {
            SetupAllies(initialAllies != null ? initialAllies : new List<CharacterBall>());
        }

        /// <summary>
        /// Initialise avec une liste externe d'alliés (ex. balles spawnées par CharacterBallFactory).
        /// Remplace l'usage de initialAllies pour cette run.
        /// </summary>
        public void Initialize(List<CharacterBall> spawnedBalls)
        {
            SetupAllies(spawnedBalls != null ? spawnedBalls : new List<CharacterBall>());
        }

        private void SetupAllies(List<CharacterBall> allies)
        {
            _participants.Clear();
            _onStoppedHandlers.Clear();
            _onDeathHandlers.Clear();
            _runtimeAllies.Clear();

            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null)
                {
                    _runtimeAllies.Add(allies[i]);
                    _participants.Add(allies[i]);
                }
            }

            SortParticipantsBySpeed();

            _currentIndex = _participants.Count > 0 ? 0 : -1;
            int start = _currentIndex;
            while (_currentIndex >= 0 && _currentIndex < _participants.Count && _participants[_currentIndex].IsDead)
            {
                _currentIndex = (_currentIndex + 1) % _participants.Count;
                if (_currentIndex == start) { _currentIndex = -1; break; }
            }

            for (int i = 0; i < _participants.Count; i++)
            {
                ITurnParticipant p = _participants[i];
                Action stoppedHandler = () => HandleParticipantStopped(p);
                Action deathHandler = () => HandleParticipantDeath(p);
                p.OnStopped += stoppedHandler;
                p.OnDeath += deathHandler;
                _onStoppedHandlers.Add(stoppedHandler);
                _onDeathHandlers.Add(deathHandler);
            }

            UpdateMovableStates();
        }

        /// <summary>
        /// Ajoute les ennemis à la liste, s'abonne à leurs events, re-trie par Speed et remet le tour au premier.
        /// </summary>
        public void AddEnemies(List<Enemy> enemies)
        {
            if (enemies == null) return;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    ITurnParticipant p = enemies[i];
                    _participants.Add(p);
                    Action stoppedHandler = () => HandleParticipantStopped(p);
                    Action deathHandler = () => HandleParticipantDeath(p);
                    p.OnStopped += stoppedHandler;
                    p.OnDeath += deathHandler;
                    _onStoppedHandlers.Add(stoppedHandler);
                    _onDeathHandlers.Add(deathHandler);
                }
            }

            SortParticipantsBySpeed();

            _currentIndex = 0;
            int start = _currentIndex;
            while (_currentIndex >= 0 && _currentIndex < _participants.Count && _participants[_currentIndex].IsDead)
            {
                _currentIndex = (_currentIndex + 1) % _participants.Count;
                if (_currentIndex == start) { _currentIndex = -1; break; }
            }

            UpdateMovableStates();
        }

        /// <summary>
        /// Désabonne et retire tous les ennemis de la liste. Les alliés sont conservés.
        /// </summary>
        public void ClearEnemies()
        {
            for (int i = _participants.Count - 1; i >= 0; i--)
            {
                if (!_participants[i].IsAlly)
                {
                    _participants[i].OnStopped -= _onStoppedHandlers[i];
                    _participants[i].OnDeath -= _onDeathHandlers[i];
                    _participants.RemoveAt(i);
                    _onStoppedHandlers.RemoveAt(i);
                    _onDeathHandlers.RemoveAt(i);
                }
            }

            if (_currentIndex >= _participants.Count)
                _currentIndex = _participants.Count > 0 ? 0 : -1;
            if (_participants.Count > 0 && _currentIndex >= 0)
            {
                int start = _currentIndex;
                while (_participants[_currentIndex].IsDead)
                {
                    _currentIndex = (_currentIndex + 1) % _participants.Count;
                    if (_currentIndex == start) { _currentIndex = -1; break; }
                }
            }
            UpdateMovableStates();
        }

        /// <summary>
        /// Passe au participant suivant (skip les morts, boucle au début). Met _currentIndex à -1 si tous morts.
        /// </summary>
        public void NextTurn()
        {
            if (_ignoreTurnChange) return;
            if (_participants.Count == 0) { _currentIndex = -1; return; }

            _turnProcessingDepth++;
            bool isRootTurn = _turnProcessingDepth == 1;

            try
            {
                int start = _currentIndex >= 0 ? _currentIndex : 0;
                _currentIndex = (_currentIndex + 1) % _participants.Count;

                while (_participants[_currentIndex].IsDead)
                {
                    _currentIndex = (_currentIndex + 1) % _participants.Count;
                    if (_currentIndex == start) { _currentIndex = -1; break; }
                }

                UpdateMovableStates();
                OnTurnChanged?.Invoke(CurrentParticipant);

                // Sous-appel (ex. SkipCurrentTurn depuis FreezeSystem) : pas de double OnTurnStart / TickTurn.
                if (!isRootTurn)
                    return;
                ProcessTurnStartForCurrentParticipant();
            }
            finally
            {
                _turnProcessingDepth--;
            }
        }

        /// <summary>
        /// Skippe le tour du participant actuel et passe au suivant. Utilisé par le gel / autres CC.
        /// </summary>
        public void SkipCurrentTurn()
        {
            NextTurn();
        }

        /// <summary>
        /// Propage un trigger d'allié à tous les AUTRES alliés vivants (ex: OnAllyKill, OnAllyTakeDamage).
        /// </summary>
        public void PropagateAllyTrigger(CharacterBall source, PassiveTrigger trigger)
        {
            for (int i = 0; i < _runtimeAllies.Count; i++)
            {
                CharacterBall ally = _runtimeAllies[i];
                if (ally == null || ally == source || ally.IsDead) continue;
                ally.NotifyAllyTrigger(trigger);
            }
        }

        /// <summary>
        /// Remet l'ordre des tours au début (premier participant vivant).
        /// </summary>
        public void ResetTurnOrder()
        {
            _currentIndex = 0;

            int start = _currentIndex;
            while (_currentIndex >= 0 && _currentIndex < _participants.Count && _participants[_currentIndex].IsDead)
            {
                _currentIndex = (_currentIndex + 1) % _participants.Count;
                if (_currentIndex == start) { _currentIndex = -1; break; }
            }

            UpdateMovableStates();
        }

        /// <summary>
        /// Active ou désactive le changement de tour automatique (utilisé pendant les transitions d'étage).
        /// </summary>
        public void SetTurnChangeEnabled(bool enabled)
        {
            _ignoreTurnChange = !enabled;
        }

        /// <summary>
        /// Ressuscite tous les alliés avec tous leurs HP.
        /// </summary>
        public void ReviveAllAllies()
        {
            for (int i = 0; i < _runtimeAllies.Count; i++)
            {
                if (_runtimeAllies[i] != null)
                    _runtimeAllies[i].Revive();
            }
        }

        /// <summary>
        /// Repositionne les alliés vivants aux positions données, selon l'ordre de l'équipe courante.
        /// </summary>
        public void ResetAlliesPositions(List<Vector2> positions)
        {
            if (positions == null) return;

            for (int i = 0; i < _runtimeAllies.Count && i < positions.Count; i++)
            {
                CharacterBall ball = _runtimeAllies[i];
                if (ball != null && !ball.IsDead)
                {
                    ball.Transform.position = new Vector3(positions[i].x, positions[i].y, 0f);
                    ball.SetMovable(false);
                }
            }
            UpdateMovableStates();
        }

        /// <summary>
        /// Soigne tous les alliés vivants d'un pourcentage de leurs HP max.
        /// </summary>
        public void HealAllAllies(float percentOfMax)
        {
            for (int i = 0; i < _participants.Count; i++)
            {
                if (!_participants[i].IsAlly) continue;
                if (_participants[i].IsDead) continue;

                CharacterBall ally = _participants[i] as CharacterBall;
                if (ally == null) continue;

                int healAmount = Mathf.CeilToInt(ally.MaxHp * percentOfMax);
                ally.Heal(healAmount);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private int GetAliveCount(bool allies)
        {
            int count = 0;
            for (int i = 0; i < _participants.Count; i++)
            {
                if (!_participants[i].IsDead && _participants[i].IsAlly == allies)
                    count++;
            }
            return count;
        }

        private ITurnParticipant GetCurrentParticipant()
        {
            if (_currentIndex < 0 || _currentIndex >= _participants.Count) return null;
            ITurnParticipant p = _participants[_currentIndex];
            return p.IsDead ? null : p;
        }

        private void HandleParticipantStopped(ITurnParticipant p)
        {
            if (_ignoreTurnChange) return;
            if (p != CurrentParticipant) return;

            CharacterBall ally = p as CharacterBall;
            if (ally != null && ally.ConsumeQueuedExtraTurn())
            {
                // Même participant rejoue immédiatement.
                UpdateMovableStates();
                OnTurnChanged?.Invoke(CurrentParticipant);
                ProcessTurnStartForCurrentParticipant();
                return;
            }

            NextTurn();
        }

        private void HandleParticipantDeath(ITurnParticipant p)
        {
            OnParticipantDeath?.Invoke(p);

            // Si le participant qui meurt est celui dont c'est le tour, passer au suivant
            bool wasCurrentParticipant = (p == CurrentParticipant || (_currentIndex >= 0 && _currentIndex < _participants.Count && _participants[_currentIndex] == p));

            if (AliveEnemiesCount == 0)
                OnAllEnemiesDead?.Invoke();
            if (AliveAlliesCount == 0)
                OnAllAlliesDead?.Invoke();

            // Passer au tour suivant si c'était le participant actif qui est mort
            // (et qu'il reste des participants vivants des deux côtés)
            if (wasCurrentParticipant && AliveAlliesCount > 0 && AliveEnemiesCount > 0)
                NextTurn();
        }

        private void ProcessTurnStartForCurrentParticipant()
        {
            if (CurrentParticipant != null && CurrentParticipant.IsAlly)
            {
                CharacterBall allyBall = CurrentParticipant as CharacterBall;
                if (allyBall != null)
                {
                    // Enregistre la spé au début du tour (pour détecter un switch avant le lancer).
                    allyBall.RecordSpecAtTurnStart();

                    CharacterPassiveRuntime runtime = allyBall.GetComponent<CharacterPassiveRuntime>();
                    if (runtime != null)
                        runtime.NotifyTrigger(PassiveTrigger.OnTurnStart);

                    // Durée des buffs ciblés (tours du porteur). Note : TickCycle sera branché quand la fin de cycle sera détectée.
                    if (allyBall.BuffReceiver != null)
                        allyBall.BuffReceiver.TickTurn();
                }
            }

            // Tick des buffs/debuffs pour les ennemis aussi.
            if (CurrentParticipant != null && !CurrentParticipant.IsAlly)
            {
                Enemy enemy = CurrentParticipant as Enemy;
                if (enemy != null && enemy.BuffReceiver != null)
                    enemy.BuffReceiver.TickTurn();
            }
        }

        /// <summary>
        /// Re-synchronise les états kinematic/dynamic après un dégel hors changement de tour (gel Frigor).
        /// </summary>
        public void RefreshMovableStates()
        {
            UpdateMovableStates();
        }

        /// <summary>
        /// Met à jour qui peut bouger : seul le participant actif est Dynamic, les autres sont Kinematic.
        /// L'ennemi gelé par Frigor ne compte pas comme « actif » pour mouvement / IA même si c'est son index de tour.
        /// </summary>
        private void UpdateMovableStates()
        {
            ITurnParticipant current = CurrentParticipant;
            for (int i = 0; i < _participants.Count; i++)
            {
                bool allowMove = _participants[i] == current;
                if (allowMove && _participants[i] is Enemy en)
                {
                    if (FreezeSystem.Instance != null && FreezeSystem.Instance.IsFrozenEnemy(en))
                        allowMove = false;
                    else if (StunSystem.Instance != null && StunSystem.Instance.IsStunned(en))
                        allowMove = false;
                }

                _participants[i].SetMovable(allowMove);
            }

            // Si c'est le tour d'un ennemi, déclenche son IA (sauf s'il est gelé — le tour sera skippé).
            if (current != null && !current.IsAlly)
            {
                Enemy enemy = current as Enemy;
                if (enemy != null)
                {
                    if (FreezeSystem.Instance != null && FreezeSystem.Instance.IsFrozenEnemy(enemy))
                        return;
                    if (StunSystem.Instance != null && StunSystem.Instance.IsStunned(enemy))
                        return;

                    EnemyAI ai = enemy.GetComponent<EnemyAI>();
                    if (ai != null)
                        ai.StartTurn();
                }
            }
        }

        /// <summary>
        /// Trie les participants par Speed décroissant (plus rapide en premier). Tri stable.
        /// Réordonne aussi les listes de handlers pour garder les indices synchronisés.
        /// </summary>
        private void SortParticipantsBySpeed()
        {
            if (_participants.Count == 0) return;

            var combined = new List<(ITurnParticipant p, Action stopped, Action death, int index)>(_participants.Count);
            for (int i = 0; i < _participants.Count; i++)
            {
                Action stopped = i < _onStoppedHandlers.Count ? _onStoppedHandlers[i] : null;
                Action death = i < _onDeathHandlers.Count ? _onDeathHandlers[i] : null;
                combined.Add((_participants[i], stopped, death, i));
            }

            combined.Sort((a, b) =>
            {
                int cmp = b.p.Speed.CompareTo(a.p.Speed);
                if (cmp != 0) return cmp;
                return a.index.CompareTo(b.index);
            });

            _participants.Clear();
            _onStoppedHandlers.Clear();
            _onDeathHandlers.Clear();
            for (int i = 0; i < combined.Count; i++)
            {
                var c = combined[i];
                _participants.Add(c.p);
                if (c.stopped != null) _onStoppedHandlers.Add(c.stopped);
                if (c.death != null) _onDeathHandlers.Add(c.death);
            }
        }
    }
}
