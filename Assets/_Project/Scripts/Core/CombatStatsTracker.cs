using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Core
{
    /// <summary> Stats agrégées d'un personnage sur la run courante. </summary>
    public struct CharacterRunStats
    {
        public long DamageDealt;   // Dégâts infligés aux ennemis
        public long DamageTaken;   // Dégâts encaissés
        public long HealingDone;   // Soins prodigués en combat (source allié tracké uniquement)
    }

    /// <summary>
    /// Agrège les stats de combat par personnage pour la run courante (dégâts, soins).
    /// Singleton de scène, sans DontDestroyOnLoad.
    /// </summary>
    public class CombatStatsTracker : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Debug")]
        [SerializeField] private bool logAccumulation = false;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static CombatStatsTracker _instance;

        private readonly Dictionary<string, CharacterRunStats> _stats =
            new Dictionary<string, CharacterRunStats>();
        private readonly List<string> _trackedCharacterIds = new List<string>();
        private readonly List<CharacterBall> _subscribedAllies = new List<CharacterBall>();
        private readonly Dictionary<CharacterBall, Action<int>> _damagedHandlers =
            new Dictionary<CharacterBall, Action<int>>();
        private readonly Dictionary<CharacterBall, Action<Enemy, int>> _hitEnemyHandlers =
            new Dictionary<CharacterBall, Action<Enemy, int>>();
        private readonly Dictionary<CharacterBall, Action<CharacterBall, int>> _healedHandlers =
            new Dictionary<CharacterBall, Action<CharacterBall, int>>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static CombatStatsTracker Instance => _instance;

        /// <summary> IDs des personnages suivis, dans l'ordre de spawn. </summary>
        public IReadOnlyList<string> TrackedCharacterIds => _trackedCharacterIds;

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
        }

        private void OnDestroy()
        {
            StopTracking();

            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Démarre le suivi d'une nouvelle équipe : désabonne l'équipe précédente, vide les stats, s'abonne à chaque allié.
        /// </summary>
        public void BeginTracking(IReadOnlyList<CharacterBall> allies)
        {
            StopTracking();
            _stats.Clear();
            _trackedCharacterIds.Clear();

            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                    RegisterAllyForTracking(allies[i]);
            }

            LogTrackingStarted();
        }

        /// <summary>
        /// Stats agrégées d'un personnage par ID. Struct à zéro si inconnu (jamais d'exception).
        /// </summary>
        public CharacterRunStats GetStatsFor(string characterId)
        {
            if (characterId != null && _stats.TryGetValue(characterId, out CharacterRunStats stats))
                return stats;

            return default;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — ABONNEMENTS
        // ═══════════════════════════════════════════

        private void RegisterAllyForTracking(CharacterBall ally)
        {
            if (ally == null)
            {
                Debug.LogWarning("[CombatStats] Allié null ignoré.", this);
                return;
            }

            CharacterData data = ally.Data;
            if (data == null || string.IsNullOrEmpty(data.Id))
            {
                Debug.LogWarning(
                    $"[CombatStats] Allié sans ID valide ignoré : {ally.name}",
                    this);
                return;
            }

            string id = data.Id;
            if (!_stats.ContainsKey(id))
            {
                _trackedCharacterIds.Add(id);
                _stats[id] = default;
            }

            SubscribeAlly(ally);
        }

        private void SubscribeAlly(CharacterBall ally)
        {
            if (ally == null || _damagedHandlers.ContainsKey(ally))
                return;

            Action<int> damagedHandler = (dmg) => OnAllyDamaged(ally, dmg);
            Action<Enemy, int> hitEnemyHandler = (enemy, dmg) => OnAllyHitEnemy(ally, enemy, dmg);
            Action<CharacterBall, int> healedHandler = (source, amount) => OnAllyHealed(ally, source, amount);

            _damagedHandlers[ally] = damagedHandler;
            _hitEnemyHandlers[ally] = hitEnemyHandler;
            _healedHandlers[ally] = healedHandler;
            _subscribedAllies.Add(ally);

            ally.OnDamaged += damagedHandler;
            ally.OnHitEnemyWithRef += hitEnemyHandler;
            ally.OnHealedWithSource += healedHandler;
        }

        private void StopTracking()
        {
            for (int i = 0; i < _subscribedAllies.Count; i++)
            {
                CharacterBall ally = _subscribedAllies[i];
                if (ally == null)
                    continue;

                if (_damagedHandlers.TryGetValue(ally, out Action<int> damagedHandler))
                    ally.OnDamaged -= damagedHandler;
                if (_hitEnemyHandlers.TryGetValue(ally, out Action<Enemy, int> hitEnemyHandler))
                    ally.OnHitEnemyWithRef -= hitEnemyHandler;
                if (_healedHandlers.TryGetValue(ally, out Action<CharacterBall, int> healedHandler))
                    ally.OnHealedWithSource -= healedHandler;
            }

            _subscribedAllies.Clear();
            _damagedHandlers.Clear();
            _hitEnemyHandlers.Clear();
            _healedHandlers.Clear();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — ACCUMULATION
        // ═══════════════════════════════════════════

        private void OnAllyDamaged(CharacterBall ally, int damage)
        {
            if (!TryGetCharacterId(ally, out string characterId) || !IsTrackedId(characterId))
                return;

            AddDamageTaken(characterId, damage);
        }

        private void OnAllyHitEnemy(CharacterBall ally, Enemy enemy, int damage)
        {
            if (!TryGetCharacterId(ally, out string characterId) || !IsTrackedId(characterId))
                return;

            AddDamageDealt(characterId, damage);
        }

        private void OnAllyHealed(CharacterBall receiver, CharacterBall source, int amount)
        {
            if (source == null)
                return;

            if (!TryGetCharacterId(source, out string sourceId) || !IsTrackedId(sourceId))
                return;

            AddHealingDone(sourceId, amount);
        }

        private void AddDamageDealt(string characterId, int amount)
        {
            CharacterRunStats s = _stats[characterId];
            s.DamageDealt += amount;
            _stats[characterId] = s;

            if (logAccumulation)
                Debug.Log($"[CombatStats] {characterId} +{amount} DamageDealt (total {s.DamageDealt})", this);
        }

        private void AddDamageTaken(string characterId, int amount)
        {
            CharacterRunStats s = _stats[characterId];
            s.DamageTaken += amount;
            _stats[characterId] = s;

            if (logAccumulation)
                Debug.Log($"[CombatStats] {characterId} +{amount} DamageTaken (total {s.DamageTaken})", this);
        }

        private void AddHealingDone(string characterId, int amount)
        {
            CharacterRunStats s = _stats[characterId];
            s.HealingDone += amount;
            _stats[characterId] = s;

            if (logAccumulation)
                Debug.Log($"[CombatStats] {characterId} +{amount} HealingDone (total {s.HealingDone})", this);
        }

        private static bool TryGetCharacterId(CharacterBall ball, out string characterId)
        {
            characterId = null;
            if (ball == null || ball.Data == null)
                return false;

            characterId = ball.Data.Id;
            return !string.IsNullOrEmpty(characterId);
        }

        private bool IsTrackedId(string characterId)
        {
            return characterId != null && _stats.ContainsKey(characterId);
        }

        private void LogTrackingStarted()
        {
            string idsList = _trackedCharacterIds.Count > 0
                ? string.Join(", ", _trackedCharacterIds)
                : string.Empty;
            Debug.Log(
                $"[CombatStats] Tracking démarré : {_subscribedAllies.Count} alliés ({idsList})",
                this);
        }

        // ═══════════════════════════════════════════
        // OUTILLAGE DEBUG
        // ═══════════════════════════════════════════

        [ContextMenu("Log Stats Snapshot")]
        private void DebugLogSnapshot()
        {
            if (_trackedCharacterIds.Count == 0)
            {
                Debug.Log("[CombatStats] Snapshot : aucun personnage suivi.", this);
                return;
            }

            for (int i = 0; i < _trackedCharacterIds.Count; i++)
            {
                string id = _trackedCharacterIds[i];
                CharacterRunStats s = _stats[id];
                Debug.Log(
                    $"[CombatStats] {id} : dealt={s.DamageDealt}, taken={s.DamageTaken}, heal={s.HealingDone}",
                    this);
            }
        }
    }
}
