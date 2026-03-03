using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;

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
        private int _currentIndex;
        private bool _ignoreTurnChange;
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
        /// Initialise la liste des participants avec les alliés, trie par Speed et s'abonne aux events.
        /// Les ennemis sont ajoutés via AddEnemies().
        /// </summary>
        public void Initialize()
        {
            _participants.Clear();
            _onStoppedHandlers.Clear();
            _onDeathHandlers.Clear();

            if (initialAllies != null)
            {
                for (int i = 0; i < initialAllies.Count; i++)
                {
                    if (initialAllies[i] != null)
                        _participants.Add(initialAllies[i]);
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

            int start = _currentIndex >= 0 ? _currentIndex : 0;
            _currentIndex = (_currentIndex + 1) % _participants.Count;

            while (_participants[_currentIndex].IsDead)
            {
                _currentIndex = (_currentIndex + 1) % _participants.Count;
                if (_currentIndex == start) { _currentIndex = -1; break; }
            }

            UpdateMovableStates();
            OnTurnChanged?.Invoke(CurrentParticipant);
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
            if (initialAllies == null) return;

            for (int i = 0; i < initialAllies.Count; i++)
            {
                if (initialAllies[i] != null)
                    initialAllies[i].Revive();
            }
        }

        /// <summary>
        /// Repositionne les alliés vivants aux positions données, selon l'ordre original (initialAllies).
        /// </summary>
        public void ResetAlliesPositions(List<Vector2> positions)
        {
            if (initialAllies == null || positions == null) return;

            for (int i = 0; i < initialAllies.Count && i < positions.Count; i++)
            {
                CharacterBall ball = initialAllies[i];
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

        /// <summary>
        /// Met à jour qui peut bouger : seul le participant actif est Dynamic, les autres sont Kinematic.
        /// </summary>
        private void UpdateMovableStates()
        {
            ITurnParticipant current = CurrentParticipant;
            for (int i = 0; i < _participants.Count; i++)
                _participants[i].SetMovable(_participants[i] == current);

            // Si c'est le tour d'un ennemi, déclenche son IA
            if (current != null && !current.IsAlly)
            {
                Enemy enemy = current as Enemy;
                if (enemy != null)
                {
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
