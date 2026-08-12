using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Feedback;
using ChezArthur.Gameplay.Passives.Handlers;
using ChezArthur.Roguelike;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Gestionnaire de tours (R1) : deux files (alliés / ennemis), séquence de cycle
    /// entrelacée proportionnellement, recalcul uniquement en fin de cycle.
    /// Architecture : inscriptions stables (_allyEntries / _enemyEntries) → projection
    /// (_participantsView) pour les consommateurs → séquence de cycle (_cycleSequence)
    /// avancée par _sequenceIndex. Wrap de fin → Rebuild silencieux + _cycleStartPending
    /// (OnCycleStarted au ProcessTurnStart du premier tour du nouveau cycle).
    /// Interlude fantôme via _ghostOverrideEntry (hors séquence, reprise sans perte de tour).
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // TYPES PRIVÉS
        // ═══════════════════════════════════════════

        /// <summary>
        /// Enregistrement unique participant + handlers (remplace les 3 listes parallèles).
        /// </summary>
        private sealed class ParticipantEntry
        {
            public ITurnParticipant Participant;
            public Action StoppedHandler;
            public Action DeathHandler;
            public int ArrivalIndex;
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Participants (prototype)")]
        [SerializeField] private List<CharacterBall> initialAllies = new List<CharacterBall>();

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES — inscription & vues
        // ═══════════════════════════════════════════
        private readonly List<ParticipantEntry> _allyEntries = new List<ParticipantEntry>(8);
        private readonly List<ParticipantEntry> _enemyEntries = new List<ParticipantEntry>(16);
        private readonly List<ITurnParticipant> _participantsView = new List<ITurnParticipant>(24);
        private readonly List<CharacterBall> _runtimeAllies = new List<CharacterBall>(8);
        private int _nextArrivalIndex;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES — séquence de cycle
        // ═══════════════════════════════════════════
        private readonly List<ParticipantEntry> _cycleSequence = new List<ParticipantEntry>(24);
        private int _sequenceIndex = -1;
        /// <summary> False au (re)build de séquence — premier tour silencieux (pas de TurnRelay). </summary>
        private bool _relayArmed;
        private bool _cycleStartPending;

        // Scratch réutilisés (zéro alloc par rebuild / peek)
        private readonly List<ParticipantEntry> _sortedAliveAllies = new List<ParticipantEntry>(8);
        private readonly List<ParticipantEntry> _sortedAliveEnemies = new List<ParticipantEntry>(16);
        private readonly List<ParticipantEntry> _interleaveSlots = new List<ParticipantEntry>(24);
        private readonly List<ParticipantEntry> _peekSimSequence = new List<ParticipantEntry>(24);

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES — contrôle de flux
        // ═══════════════════════════════════════════
        private bool _ignoreTurnChange;
        /// <summary> Profondeur d'appel NextTurn (évite double traitement si un listener enchaîne un autre NextTurn, ex. skip gel). </summary>
        private int _turnProcessingDepth;
        private CharacterBall _pendingGhostAlly;
        private CharacterBall _activeGhostAlly;
        /// <summary>
        /// Pendant l'interlude fantôme : CurrentParticipant renvoie ce participant.
        /// ÉCART VOLONTAIRE vs ancien code : l'index de séquence n'est PAS sauté vers le fantôme
        /// (anciennement ça pouvait faire perdre son tour à d'autres). Interlude puis reprise
        /// depuis _sequenceIndex inchangé — exigence R1.
        /// </summary>
        private ParticipantEntry _ghostOverrideEntry;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Liste unifiée des participants (lecture seule). </summary>
        public IReadOnlyList<ITurnParticipant> Participants => _participantsView;

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

        /// <summary>
        /// Déclenché quand un participant TERMINE effectivement son tour (a agi et s'est arrêté).
        /// Les tours sautés (gel, stun) ne le déclenchent jamais.
        /// </summary>
        public event Action<ITurnParticipant> OnParticipantTurnEnded;

        /// <summary> Déclenché quand un ennemi rejoint le combat en cours (invocation). </summary>
        public event Action<Enemy> OnEnemyAddedMidCombat;

        /// <summary>
        /// Déclenché quand tous les participants actifs ont joué au moins une fois depuis le dernier cycle.
        /// ATTENTION : si un seul participant est en vie, se déclenche à chaque tour de ce participant.
        /// Les passifs "tous les X cycles" doivent tenir compte de ce cas.
        /// </summary>
        public event Action OnCycleStarted;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            // RunManager.StartRun appelle déjà Initialize — évite d'écraser l'équipe spawnée.
            if (_runtimeAllies.Count > 0)
                return;

            Initialize();
        }

        private void OnDestroy()
        {
            UnsubscribeAllEntries(_allyEntries);
            UnsubscribeAllEntries(_enemyEntries);
            _allyEntries.Clear();
            _enemyEntries.Clear();
            _participantsView.Clear();
            _cycleSequence.Clear();
            _ghostOverrideEntry = null;
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

        /// <summary>
        /// Ajoute les ennemis à la liste, s'abonne à leurs events, reconstruit la séquence de cycle et remet le tour au premier.
        /// </summary>
        public void AddEnemies(List<Enemy> enemies)
        {
            if (enemies == null) return;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                    RegisterEnemyEntry(enemies[i]);
            }

            RebuildParticipantsView();
            RebuildCycleSequence();
            _relayArmed = false;
            _cycleStartPending = false;
            _sequenceIndex = FindFirstAliveSequenceIndex();
            UpdateMovableStates();

            if (FrigorColdFieldSystem.Instance != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                    FrigorColdFieldSystem.Instance.TryApplyColdFieldToEnemy(enemies[i]);
            }
        }

        /// <summary>
        /// Ajoute un seul ennemi au TurnManager en cours de combat.
        /// </summary>
        public void AddEnemy(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead) return;
            AddEnemies(new List<Enemy> { enemy });
        }

        /// <summary>
        /// Ajoute un ennemi en cours de combat sans perturber l'ordre des tours ni le cycle en cours.
        /// L'ennemi est inséré juste après le participant actuel dans l'ordre de la liste.
        /// </summary>
        public void AddEnemyMidCombat(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead)
                return;

            if (FindEntry(enemy) != null)
                return;

            ParticipantEntry entry = RegisterEnemyEntry(enemy);

            int viewInsert = _participantsView.Count;
            ITurnParticipant current = GetCurrentParticipantIgnoringGhostDeathCheck();
            if (current != null)
            {
                int curView = _participantsView.IndexOf(current);
                if (curView >= 0)
                    viewInsert = curView + 1;
            }
            else if (_sequenceIndex >= 0 && _sequenceIndex < _cycleSequence.Count)
            {
                int curView = _participantsView.IndexOf(_cycleSequence[_sequenceIndex].Participant);
                if (curView >= 0)
                    viewInsert = curView + 1;
            }

            _participantsView.Insert(viewInsert, enemy);

            int seqInsert = _cycleSequence.Count;
            if (_sequenceIndex >= 0 && _sequenceIndex < _cycleSequence.Count)
                seqInsert = _sequenceIndex + 1;
            _cycleSequence.Insert(seqInsert, entry);

            enemy.SetMovable(false);
            UpdateMovableStates();

            FrigorColdFieldSystem.Instance?.TryApplyColdFieldToEnemy(enemy);

            OnEnemyAddedMidCombat?.Invoke(enemy);
        }

        /// <summary>
        /// Désabonne et retire tous les ennemis de la liste. Les alliés sont conservés.
        /// </summary>
        public void ClearEnemies()
        {
            UnsubscribeAllEntries(_enemyEntries);
            _enemyEntries.Clear();

            for (int i = _cycleSequence.Count - 1; i >= 0; i--)
            {
                if (_cycleSequence[i].Participant != null && !_cycleSequence[i].Participant.IsAlly)
                    _cycleSequence.RemoveAt(i);
            }

            RebuildParticipantsView();
            ClampSequenceIndexToAlive();
            UpdateMovableStates();
        }

        /// <summary>
        /// Passe au participant suivant (skip les morts, wrap de cycle avec recalcul). Met l'index à -1 si tous morts.
        /// </summary>
        public void NextTurn()
        {
            if (_ignoreTurnChange) return;
            if (_participantsView.Count == 0)
            {
                _sequenceIndex = -1;
                return;
            }

            _turnProcessingDepth++;
            bool isRootTurn = _turnProcessingDepth == 1;

            try
            {
                AdvanceSequenceIndex();
                UpdateMovableStates();
                OnTurnChanged?.Invoke(CurrentParticipant);

                if (_relayArmed && _activeGhostAlly == null && CurrentParticipant != null)
                {
                    Vector2 relayPos = Vector2.zero;
                    if (CurrentParticipant is MonoBehaviour mb)
                        relayPos = mb.transform.position;
                    CombatFeedbackService.PlayEvent(
                        FeedbackEventId.TurnRelay,
                        FeedbackContext.At(relayPos));
                }
                _relayArmed = true;

                // Sous-appel (ex. SkipCurrentTurn depuis FreezeSystem) : pas de double OnTurnStart.
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
        /// Remet l'ordre des tours au début (premier participant vivant du cycle reconstruit).
        /// </summary>
        public void ResetTurnOrder()
        {
            RebuildCycleSequence();
            _relayArmed = false;
            _cycleStartPending = false;
            _sequenceIndex = FindFirstAliveSequenceIndex();
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
            for (int i = 0; i < _participantsView.Count; i++)
            {
                if (!_participantsView[i].IsAlly) continue;
                if (_participantsView[i].IsDead) continue;

                CharacterBall ally = _participantsView[i] as CharacterBall;
                if (ally == null) continue;

                int healAmount = Mathf.CeilToInt(ally.MaxHp * percentOfMax);
                ally.Heal(healAmount);
            }
        }

        /// <summary>
        /// Réactive un allié ressuscité dans le flux de tours.
        /// </summary>
        public void OnAllyRevived(CharacterBall ally)
        {
            if (ally == null) return;
            UpdateMovableStates();
        }

        /// <summary>
        /// Planifie un tour fantôme différé (l'ennemi en cours termine son mouvement avant bascule).
        /// </summary>
        public void RequestGhostTurn(CharacterBall ally)
        {
            if (ally == null || ally.IsDead) return;
            _pendingGhostAlly = ally;
        }

        /// <summary>
        /// Remplit buffer avec les count prochains tours (le participant courant en [0]),
        /// en suivant la séquence du cycle courant puis en simulant les cycles suivants
        /// sur l'état actuel (vivants + SPD du moment — état « vivant », R6).
        /// Retourne le nombre d'entrées écrites. nextCycleStartIndex = index dans buffer
        /// du premier tour appartenant à un cycle simulé, ou -1 si tout tient dans le cycle courant.
        /// Zéro allocation : buffer fourni par l'appelant, Clear() en entrée.
        /// </summary>
        public int PeekUpcoming(int count, List<ITurnParticipant> buffer, out int nextCycleStartIndex)
        {
            buffer.Clear();
            nextCycleStartIndex = -1;
            if (count <= 0)
                return 0;

            bool ghostPrefix = _ghostOverrideEntry != null && _ghostOverrideEntry.Participant != null;
            if (ghostPrefix)
            {
                buffer.Add(_ghostOverrideEntry.Participant);
                if (buffer.Count >= count)
                    return buffer.Count;
            }

            int i = ghostPrefix ? _sequenceIndex + 1 : _sequenceIndex;
            if (i < 0)
                i = 0;

            while (i < _cycleSequence.Count && buffer.Count < count)
            {
                ParticipantEntry entry = _cycleSequence[i];
                if (entry != null && entry.Participant != null && !entry.Participant.IsDead)
                    buffer.Add(entry.Participant);
                i++;
            }

            int safety = 0;
            while (buffer.Count < count && safety < 64)
            {
                safety++;
                BuildInterleaveInto(_peekSimSequence);
                if (_peekSimSequence.Count == 0)
                    break;

                if (nextCycleStartIndex < 0)
                    nextCycleStartIndex = buffer.Count;

                for (int s = 0; s < _peekSimSequence.Count && buffer.Count < count; s++)
                {
                    ParticipantEntry entry = _peekSimSequence[s];
                    if (entry != null && entry.Participant != null && !entry.Participant.IsDead)
                        buffer.Add(entry.Participant);
                }
            }

            return buffer.Count;
        }

        /// <summary>
        /// Re-synchronise les états kinematic/dynamic après un dégel hors changement de tour (gel Frigor).
        /// </summary>
        public void RefreshMovableStates()
        {
            UpdateMovableStates();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — setup / inscription
        // ═══════════════════════════════════════════

        private void SetupAllies(List<CharacterBall> allies)
        {
            UnsubscribeAllEntries(_allyEntries);
            UnsubscribeAllEntries(_enemyEntries);
            _allyEntries.Clear();
            _enemyEntries.Clear();
            _runtimeAllies.Clear();
            _cycleSequence.Clear();
            _participantsView.Clear();
            _ghostOverrideEntry = null;
            _pendingGhostAlly = null;
            _activeGhostAlly = null;
            _nextArrivalIndex = 0;

            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] == null)
                    continue;
                _runtimeAllies.Add(allies[i]);
                RegisterAllyEntry(allies[i]);
            }

            RebuildParticipantsView();
            RebuildCycleSequence();
            _relayArmed = false;
            _cycleStartPending = false;
            _sequenceIndex = FindFirstAliveSequenceIndex();
            UpdateMovableStates();
        }

        private ParticipantEntry RegisterAllyEntry(CharacterBall ally)
        {
            var entry = new ParticipantEntry
            {
                Participant = ally,
                ArrivalIndex = _nextArrivalIndex++
            };
            ITurnParticipant p = ally;
            entry.StoppedHandler = () => HandleParticipantStopped(p);
            entry.DeathHandler = () => HandleParticipantDeath(p);
            p.OnStopped += entry.StoppedHandler;
            p.OnDeath += entry.DeathHandler;
            _allyEntries.Add(entry);
            return entry;
        }

        private ParticipantEntry RegisterEnemyEntry(Enemy enemy)
        {
            var entry = new ParticipantEntry
            {
                Participant = enemy,
                ArrivalIndex = _nextArrivalIndex++
            };
            ITurnParticipant p = enemy;
            entry.StoppedHandler = () => HandleParticipantStopped(p);
            entry.DeathHandler = () => HandleParticipantDeath(p);
            p.OnStopped += entry.StoppedHandler;
            p.OnDeath += entry.DeathHandler;
            _enemyEntries.Add(entry);
            return entry;
        }

        private static void UnsubscribeAllEntries(List<ParticipantEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                ParticipantEntry e = entries[i];
                if (e == null || e.Participant == null)
                    continue;
                if (e.StoppedHandler != null)
                    e.Participant.OnStopped -= e.StoppedHandler;
                if (e.DeathHandler != null)
                    e.Participant.OnDeath -= e.DeathHandler;
            }
        }

        private ParticipantEntry FindEntry(ITurnParticipant participant)
        {
            if (participant == null)
                return null;

            for (int i = 0; i < _allyEntries.Count; i++)
            {
                if (ReferenceEquals(_allyEntries[i].Participant, participant))
                    return _allyEntries[i];
            }

            for (int i = 0; i < _enemyEntries.Count; i++)
            {
                if (ReferenceEquals(_enemyEntries[i].Participant, participant))
                    return _enemyEntries[i];
            }

            return null;
        }

        private void RebuildParticipantsView()
        {
            _participantsView.Clear();
            for (int i = 0; i < _allyEntries.Count; i++)
                _participantsView.Add(_allyEntries[i].Participant);
            for (int i = 0; i < _enemyEntries.Count; i++)
                _participantsView.Add(_enemyEntries[i].Participant);

            SortParticipantsViewBySpeedStable();
        }

        /// <summary>
        /// Tri SPD décroissant stable (ArrivalIndex) — même contrat que l'ancien SortParticipantsBySpeed.
        /// </summary>
        private void SortParticipantsViewBySpeedStable()
        {
            int n = _participantsView.Count;
            if (n <= 1)
                return;

            // Insertion sort stable, zéro alloc.
            for (int i = 1; i < n; i++)
            {
                ITurnParticipant key = _participantsView[i];
                ParticipantEntry keyEntry = FindEntry(key);
                int keySpeed = key.Speed;
                int keyArrival = keyEntry != null ? keyEntry.ArrivalIndex : i;
                int j = i - 1;
                while (j >= 0)
                {
                    ITurnParticipant other = _participantsView[j];
                    ParticipantEntry otherEntry = FindEntry(other);
                    int otherSpeed = other.Speed;
                    int otherArrival = otherEntry != null ? otherEntry.ArrivalIndex : j;
                    int cmp = keySpeed.CompareTo(otherSpeed);
                    if (cmp < 0)
                        break;
                    if (cmp == 0 && keyArrival >= otherArrival)
                        break;
                    _participantsView[j + 1] = other;
                    j--;
                }

                _participantsView[j + 1] = key;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — séquence / entrelacement
        // ═══════════════════════════════════════════

        /// <summary>
        /// Recalcule la séquence du cycle sur les vivants (Clear + refill, pas de new List).
        /// Exemples d'entrelacement (allié ouvre) :
        ///   4v2 → A E A A E A
        ///   4v1 → A A E A A
        ///   3v3 → A E A E A E
        ///   2v4 → A E E A E E
        /// </summary>
        private void RebuildCycleSequence()
        {
            BuildInterleaveInto(_cycleSequence);
            // _relayArmed : seulement aux chemins d'installation (AddEnemies / ResetTurnOrder / SetAllies).
            // Les rebuilds lazy (fin de cycle) ne désarment plus — tick de relais au passage de cycle.
        }

        private void BuildInterleaveInto(List<ParticipantEntry> dest)
        {
            dest.Clear();
            CollectSortedAlive(_allyEntries, _sortedAliveAllies);
            CollectSortedAlive(_enemyEntries, _sortedAliveEnemies);

            int nA = _sortedAliveAllies.Count;
            int nE = _sortedAliveEnemies.Count;

            if (nA == 0 && nE == 0)
                return;

            if (nA == 0)
            {
                for (int i = 0; i < nE; i++)
                    dest.Add(_sortedAliveEnemies[i]);
                return;
            }

            if (nE == 0)
            {
                for (int i = 0; i < nA; i++)
                    dest.Add(_sortedAliveAllies[i]);
                return;
            }

            bool openerIsAlly = _sortedAliveAllies[0].Participant.Speed >= _sortedAliveEnemies[0].Participant.Speed;
            int total = nA + nE;

            if (nA == nE)
            {
                for (int k = 0; k < total; k++)
                {
                    if (openerIsAlly)
                        dest.Add((k % 2) == 0 ? _sortedAliveAllies[k / 2] : _sortedAliveEnemies[k / 2]);
                    else
                        dest.Add((k % 2) == 0 ? _sortedAliveEnemies[k / 2] : _sortedAliveAllies[k / 2]);
                }

                return;
            }

            List<ParticipantEntry> majority;
            List<ParticipantEntry> minority;
            if (nA > nE)
            {
                majority = _sortedAliveAllies;
                minority = _sortedAliveEnemies;
            }
            else
            {
                majority = _sortedAliveEnemies;
                minority = _sortedAliveAllies;
            }

            // Ouvreur = majorité ? (sinon la minorité ouvre → formule floor(k × total / nMin))
            bool openerIsMajority = openerIsAlly ? nA > nE : nE > nA;
            int nMin = minority.Count;
            _interleaveSlots.Clear();
            for (int s = 0; s < total; s++)
                _interleaveSlots.Add(null);

            for (int k = 0; k < nMin; k++)
            {
                int slot = openerIsMajority
                    ? ((2 * k + 1) * total) / (2 * nMin)
                    : (k * total) / nMin;

                if (slot < 0)
                    slot = 0;
                if (slot >= total)
                    slot = total - 1;

                // Garde anti-collision (ratioses pathologiques)
                while (slot < total && _interleaveSlots[slot] != null)
                    slot++;
                if (slot >= total)
                {
                    slot = total - 1;
                    while (slot >= 0 && _interleaveSlots[slot] != null)
                        slot--;
                }

                if (slot >= 0)
                    _interleaveSlots[slot] = minority[k];
            }

            int majIdx = 0;
            for (int s = 0; s < total; s++)
            {
                if (_interleaveSlots[s] == null)
                    _interleaveSlots[s] = majority[majIdx++];
            }

            for (int s = 0; s < total; s++)
                dest.Add(_interleaveSlots[s]);
        }

        private static void CollectSortedAlive(
            List<ParticipantEntry> source,
            List<ParticipantEntry> dest)
        {
            dest.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                ParticipantEntry e = source[i];
                if (e == null || e.Participant == null || e.Participant.IsDead)
                    continue;
                dest.Add(e);
            }

            // Insertion sort SPD desc, ArrivalIndex asc
            for (int i = 1; i < dest.Count; i++)
            {
                ParticipantEntry key = dest[i];
                int j = i - 1;
                while (j >= 0)
                {
                    int cmp = key.Participant.Speed.CompareTo(dest[j].Participant.Speed);
                    if (cmp < 0)
                        break;
                    if (cmp == 0 && key.ArrivalIndex >= dest[j].ArrivalIndex)
                        break;
                    dest[j + 1] = dest[j];
                    j--;
                }

                dest[j + 1] = key;
            }
        }

        private void AdvanceSequenceIndex()
        {
            if (_cycleSequence.Count == 0)
            {
                RebuildCycleSequence();
                _cycleStartPending = false;
                _sequenceIndex = FindFirstAliveSequenceIndex();
                return;
            }

            _sequenceIndex++;
            int guard = 0;
            int maxGuard = (_cycleSequence.Count + 1) * 3 + 8;

            while (guard++ < maxGuard)
            {
                if (_sequenceIndex >= _cycleSequence.Count)
                {
                    RebuildCycleSequence();
                    _cycleStartPending = true;
                    _sequenceIndex = 0;
                    if (_cycleSequence.Count == 0)
                    {
                        _sequenceIndex = -1;
                        return;
                    }
                }

                ParticipantEntry entry = _cycleSequence[_sequenceIndex];
                if (entry != null && entry.Participant != null && !entry.Participant.IsDead)
                    return;

                _sequenceIndex++;
            }

            _sequenceIndex = -1;
        }

        private int FindFirstAliveSequenceIndex()
        {
            for (int i = 0; i < _cycleSequence.Count; i++)
            {
                ParticipantEntry e = _cycleSequence[i];
                if (e != null && e.Participant != null && !e.Participant.IsDead)
                    return i;
            }

            return -1;
        }

        private void ClampSequenceIndexToAlive()
        {
            if (_cycleSequence.Count == 0)
            {
                _sequenceIndex = -1;
                return;
            }

            if (_sequenceIndex < 0)
                _sequenceIndex = 0;
            if (_sequenceIndex >= _cycleSequence.Count)
                _sequenceIndex = 0;

            int start = _sequenceIndex;
            while (_cycleSequence[_sequenceIndex].Participant == null
                   || _cycleSequence[_sequenceIndex].Participant.IsDead)
            {
                _sequenceIndex = (_sequenceIndex + 1) % _cycleSequence.Count;
                if (_sequenceIndex == start)
                {
                    _sequenceIndex = -1;
                    return;
                }
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — courant / handlers
        // ═══════════════════════════════════════════

        private int GetAliveCount(bool allies)
        {
            int count = 0;
            List<ParticipantEntry> list = allies ? _allyEntries : _enemyEntries;
            for (int i = 0; i < list.Count; i++)
            {
                ITurnParticipant p = list[i].Participant;
                if (p != null && !p.IsDead)
                    count++;
            }

            return count;
        }

        private ITurnParticipant GetCurrentParticipant()
        {
            if (_ghostOverrideEntry != null && _ghostOverrideEntry.Participant != null)
                return _ghostOverrideEntry.Participant;

            if (_sequenceIndex < 0 || _sequenceIndex >= _cycleSequence.Count)
                return null;

            ITurnParticipant p = _cycleSequence[_sequenceIndex].Participant;
            if (p == null || p.IsDead)
                return null;
            return p;
        }

        /// <summary> Courant sans filtre IsDead (pour insertion mid-combat pendant override fantôme). </summary>
        private ITurnParticipant GetCurrentParticipantIgnoringGhostDeathCheck()
        {
            if (_ghostOverrideEntry != null)
                return _ghostOverrideEntry.Participant;
            if (_sequenceIndex < 0 || _sequenceIndex >= _cycleSequence.Count)
                return null;
            return _cycleSequence[_sequenceIndex].Participant;
        }

        private void HandleParticipantStopped(ITurnParticipant p)
        {
            if (_pendingGhostAlly != null)
            {
                TickParticipantBuffTurnEnd(p);
                OnParticipantTurnEnded?.Invoke(p);

                // Interlude hors séquence — _sequenceIndex inchangé (écart volontaire vs ancien saut d'index).
                _ghostOverrideEntry = FindEntry(_pendingGhostAlly);
                _activeGhostAlly = _pendingGhostAlly;
                _pendingGhostAlly = null;
                UpdateMovableStates();
                OnTurnChanged?.Invoke(CurrentParticipant);
                ProcessTurnStartForCurrentParticipant();
                return;
            }

            if (_ignoreTurnChange) return;
            if (p != CurrentParticipant) return;

            if (_activeGhostAlly != null && ReferenceEquals(p, _activeGhostAlly))
            {
                TickParticipantBuffTurnEnd(p);
                OnParticipantTurnEnded?.Invoke(p);
                _activeGhostAlly.ResolveGhost();
                _activeGhostAlly = null;
                _ghostOverrideEntry = null;
                NextTurn();
                return;
            }

            TickParticipantBuffTurnEnd(p);
            OnParticipantTurnEnded?.Invoke(p);

            CharacterBall ally = p as CharacterBall;
            if (ally != null && ally.ConsumeQueuedExtraTurn())
            {
                // Même participant rejoue immédiatement — _sequenceIndex inchangé.
                UpdateMovableStates();
                OnTurnChanged?.Invoke(CurrentParticipant);
                CombatFeedbackService.PlayEvent(
                    FeedbackEventId.ExtraTurn,
                    FeedbackContext.At(ally.transform.position));
                ProcessTurnStartForCurrentParticipant();
                return;
            }

            NextTurn();
        }

        private void HandleParticipantDeath(ITurnParticipant p)
        {
            OnParticipantDeath?.Invoke(p);
            ExpireCycleBuffsFromDeadApplicator(p);

            // Si le participant qui meurt est celui dont c'est le tour, passer au suivant
            bool wasCurrentParticipant =
                p == CurrentParticipant
                || (_sequenceIndex >= 0
                    && _sequenceIndex < _cycleSequence.Count
                    && ReferenceEquals(_cycleSequence[_sequenceIndex].Participant, p))
                || (_ghostOverrideEntry != null
                    && ReferenceEquals(_ghostOverrideEntry.Participant, p));

            if (AliveEnemiesCount == 0)
                OnAllEnemiesDead?.Invoke();
            if (AliveAlliesCount == 0)
                OnAllAlliesDead?.Invoke();

            // Passer au tour suivant si c'était le participant actif qui est mort
            // (et qu'il reste des participants vivants des deux côtés)
            if (wasCurrentParticipant && AliveAlliesCount > 0 && AliveEnemiesCount > 0)
            {
                if (_ghostOverrideEntry != null && ReferenceEquals(_ghostOverrideEntry.Participant, p))
                    _ghostOverrideEntry = null;
                NextTurn();
            }
        }

        private void ProcessTurnStartForCurrentParticipant()
        {
            ITurnParticipant current = CurrentParticipant;
            if (current != null)
                TickCycleBuffsFromApplicatorOnAllHolders(current);

            // OnCycleStarted APRÈS OnTurnChanged du premier tour du nouveau cycle (pending posé au wrap).
            if (_cycleStartPending)
            {
                _cycleStartPending = false;
                OnCycleStarted?.Invoke();
            }

            if (CurrentParticipant != null && CurrentParticipant.IsAlly)
            {
                CharacterBall allyBall = CurrentParticipant as CharacterBall;
                if (allyBall != null)
                {
                    // Enregistre la spé au début du tour (pour détecter un switch avant le lancer).
                    allyBall.RecordSpecAtTurnStart();
                    ValiseEventBridge.Instance?.NotifyAllyTurnStart(allyBall);

                    CharacterPassiveRuntime runtime = allyBall.GetComponent<CharacterPassiveRuntime>();
                    if (runtime != null)
                        runtime.NotifyTrigger(PassiveTrigger.OnTurnStart);
                }
            }
        }

        /// <summary>
        /// Décrémente RemainingTurns du porteur en fin de tour (après le lancer / arrêt).
        /// </summary>
        private void TickParticipantBuffTurnEnd(ITurnParticipant participant)
        {
            if (participant == null || participant.IsDead)
                return;

            CharacterBall ally = participant as CharacterBall;
            if (ally != null)
            {
                if (ally.BuffReceiver != null)
                    ally.BuffReceiver.TickTurn();
                return;
            }

            Enemy enemy = participant as Enemy;
            if (enemy != null && enemy.BuffReceiver != null)
                enemy.BuffReceiver.TickTurn();
        }

        /// <summary>
        /// Décrémente RemainingCycles des buffs posés par l'applicateur, sur tous les porteurs.
        /// </summary>
        private void TickCycleBuffsFromApplicatorOnAllHolders(ITurnParticipant applicator)
        {
            if (applicator == null)
                return;

            for (int i = 0; i < _participantsView.Count; i++)
            {
                ITurnParticipant holder = _participantsView[i];
                if (holder == null || holder.IsDead)
                    continue;

                BuffReceiver receiver = GetBuffReceiver(holder);
                if (receiver != null)
                    receiver.TickCycleBuffsFromApplicator(applicator);
            }
        }

        /// <summary>
        /// Retire les buffs de cycle orphelins quand l'applicateur meurt.
        /// </summary>
        private void ExpireCycleBuffsFromDeadApplicator(ITurnParticipant deadApplicator)
        {
            if (deadApplicator == null)
                return;

            for (int i = 0; i < _participantsView.Count; i++)
            {
                ITurnParticipant holder = _participantsView[i];
                if (holder == null)
                    continue;

                BuffReceiver receiver = GetBuffReceiver(holder);
                if (receiver != null)
                    receiver.ExpireCycleBuffsFromApplicator(deadApplicator);
            }
        }

        private static BuffReceiver GetBuffReceiver(ITurnParticipant participant)
        {
            if (participant is CharacterBall ally)
                return ally.BuffReceiver;

            if (participant is Enemy enemy)
                return enemy.BuffReceiver;

            return null;
        }

        /// <summary>
        /// Met à jour qui peut bouger : seul le participant actif est Dynamic, les autres sont Kinematic.
        /// L'ennemi gelé par Frigor ne compte pas comme « actif » pour mouvement / IA même si c'est son index de tour.
        /// </summary>
        private void UpdateMovableStates()
        {
            ITurnParticipant current = CurrentParticipant;
            for (int i = 0; i < _participantsView.Count; i++)
            {
                bool allowMove = _participantsView[i] == current;

                // Gel Frigor en priorité : un ennemi gelé ne doit jamais être ré-activé, même si c'est son tour.
                if (_participantsView[i] is Enemy en)
                {
                    if (FreezeSystem.Instance != null && FreezeSystem.Instance.IsFrozenEnemy(en))
                        allowMove = false;
                    else if (allowMove && StunSystem.Instance != null && StunSystem.Instance.IsStunned(en))
                        allowMove = false;
                }

                _participantsView[i].SetMovable(allowMove);
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
    }
}
