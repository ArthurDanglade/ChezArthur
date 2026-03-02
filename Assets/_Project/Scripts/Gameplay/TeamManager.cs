using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Gère l'équipe de personnages (tour par tour). S'abonne à OnStopped / OnDeath pour avancer le tour et détecter les morts.
    /// </summary>
    public class TeamManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Équipe (prototype)")]
        [SerializeField] private List<CharacterBall> initialTeam = new List<CharacterBall>();

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<CharacterBall> _team = new List<CharacterBall>();
        private int _currentIndex;
        private List<Action> _onDeathHandlers = new List<Action>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Liste des personnages de l'équipe (lecture seule). </summary>
        public IReadOnlyList<CharacterBall> Team => _team;

        /// <summary> Nombre de personnages encore en vie. </summary>
        public int AliveCount => GetAliveCount();

        /// <summary> Personnage dont c'est le tour (null si aucun ou tous morts). </summary>
        public CharacterBall CurrentCharacter => GetCurrentCharacter();

        /// <summary> True si un personnage actif existe et n'est pas mort. </summary>
        public bool HasCurrentCharacter => CurrentCharacter != null && !CurrentCharacter.IsDead;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand un personnage de l'équipe meurt. </summary>
        public event Action<CharacterBall> OnCharacterDeath;

        /// <summary> Déclenché quand toute l'équipe est morte. </summary>
        public event Action OnTeamWiped;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _team.Count; i++)
            {
                _team[i].OnStopped -= HandleAnyCharacterStopped;
                if (i < _onDeathHandlers.Count)
                    _team[i].OnDeath -= _onDeathHandlers[i];
            }
            _onDeathHandlers.Clear();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise l'équipe à partir de initialTeam et s'abonne aux events des personnages.
        /// </summary>
        public void Initialize()
        {
            _team.Clear();
            _onDeathHandlers.Clear();

            if (initialTeam != null)
                _team.AddRange(initialTeam);

            SortTeamBySpeed();

            _currentIndex = _team.Count > 0 ? 0 : -1;
            int start = _currentIndex;
            while (_currentIndex >= 0 && _currentIndex < _team.Count && _team[_currentIndex].IsDead)
            {
                _currentIndex = (_currentIndex + 1) % _team.Count;
                if (_currentIndex == start) { _currentIndex = -1; break; }
            }

            for (int i = 0; i < _team.Count; i++)
            {
                CharacterBall ball = _team[i];
                ball.OnStopped += HandleAnyCharacterStopped;
                Action deathHandler = () => HandleCharacterDeath(ball);
                ball.OnDeath += deathHandler;
                _onDeathHandlers.Add(deathHandler);
            }

            UpdateMovableStates();
        }

        /// <summary>
        /// Passe au personnage suivant (skip les morts, boucle au début). Met _currentIndex à -1 si tous morts.
        /// </summary>
        public void NextTurn()
        {
            if (_team.Count == 0) { _currentIndex = -1; return; }

            int start = _currentIndex >= 0 ? _currentIndex : 0;
            _currentIndex = (_currentIndex + 1) % _team.Count;

            while (_team[_currentIndex].IsDead)
            {
                _currentIndex = (_currentIndex + 1) % _team.Count;
                if (_currentIndex == start) { _currentIndex = -1; break; }
            }

            UpdateMovableStates();
        }

        /// <summary>
        /// Retourne la liste des personnages encore en vie.
        /// </summary>
        public List<CharacterBall> GetAliveCharacters()
        {
            var alive = new List<CharacterBall>(_team.Count);
            for (int i = 0; i < _team.Count; i++)
            {
                if (!_team[i].IsDead)
                    alive.Add(_team[i]);
            }
            return alive;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private int GetAliveCount()
        {
            int count = 0;
            for (int i = 0; i < _team.Count; i++)
            {
                if (!_team[i].IsDead) count++;
            }
            return count;
        }

        private CharacterBall GetCurrentCharacter()
        {
            if (_currentIndex < 0 || _currentIndex >= _team.Count) return null;
            CharacterBall c = _team[_currentIndex];
            return c.IsDead ? null : c;
        }

        private void HandleAnyCharacterStopped()
        {
            NextTurn();
        }

        private void HandleCharacterDeath(CharacterBall character)
        {
            OnCharacterDeath?.Invoke(character);
            if (GetAliveCount() == 0)
                OnTeamWiped?.Invoke();
        }

        /// <summary>
        /// Met à jour qui peut bouger : seul le personnage actif est Dynamic, les autres sont Kinematic (figés).
        /// </summary>
        private void UpdateMovableStates()
        {
            CharacterBall current = CurrentCharacter;
            for (int i = 0; i < _team.Count; i++)
                _team[i].SetMovable(_team[i] == current);
        }

        /// <summary>
        /// Trie l'équipe par Speed décroissant (plus rapide en premier). Tri stable : égalité de Speed garde l'ordre original.
        /// </summary>
        private void SortTeamBySpeed()
        {
            if (_team.Count == 0) return;

            var withIndex = new List<(CharacterBall ball, int index)>(_team.Count);
            for (int i = 0; i < _team.Count; i++)
                withIndex.Add((_team[i], i));

            withIndex.Sort((a, b) =>
            {
                int cmp = b.ball.Speed.CompareTo(a.ball.Speed);
                if (cmp != 0) return cmp;
                return a.index.CompareTo(b.index);
            });

            _team.Clear();
            for (int i = 0; i < withIndex.Count; i++)
                _team.Add(withIndex[i].ball);
        }
    }
}
