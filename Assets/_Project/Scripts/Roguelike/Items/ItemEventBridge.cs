using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Pont entre les events gameplay (alliés/tours) et les triggers d'items.
    /// </summary>
    public class ItemEventBridge : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private TurnManager turnManager;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static ItemEventBridge _instance;
        private readonly List<CharacterBall> _subscribedAllies = new List<CharacterBall>();
        private readonly Dictionary<CharacterBall, Action> _allyDeathHandlers = new Dictionary<CharacterBall, Action>();
        private readonly Dictionary<CharacterBall, Action<int>> _allyDamagedHandlers = new Dictionary<CharacterBall, Action<int>>();
        private readonly Dictionary<CharacterBall, Action<int>> _allyHealedHandlers = new Dictionary<CharacterBall, Action<int>>();
        private readonly Dictionary<CharacterBall, Action> _allyKillHandlers = new Dictionary<CharacterBall, Action>();
        private readonly Dictionary<CharacterBall, Action> _allyLaunchHandlers = new Dictionary<CharacterBall, Action>();
        private readonly Dictionary<CharacterBall, Action<Enemy, int>> _allyHitEnemyRefHandlers = new Dictionary<CharacterBall, Action<Enemy, int>>();
        private readonly Dictionary<CharacterBall, Action<Enemy, int>> _allyKillEnemyRefHandlers = new Dictionary<CharacterBall, Action<Enemy, int>>();
        private readonly Dictionary<CharacterBall, Action<Enemy, int>> _allyCritHandlers = new Dictionary<CharacterBall, Action<Enemy, int>>();
        private bool _initialized;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static ItemEventBridge Instance => _instance;

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
            UnsubscribeAll();

            if (turnManager != null)
                turnManager.OnTurnChanged -= OnTurnChanged;
            if (ItemManager.Instance != null)
                ItemManager.Instance.OnItemAdded -= OnItemAdded;

            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise le pont avec le TurnManager courant.
        /// </summary>
        public void Initialize(TurnManager tm)
        {
            if (turnManager != null)
                turnManager.OnTurnChanged -= OnTurnChanged;
            if (ItemManager.Instance != null)
                ItemManager.Instance.OnItemAdded -= OnItemAdded;

            UnsubscribeAll();

            turnManager = tm;
            if (turnManager == null)
            {
                _initialized = false;
                return;
            }

            turnManager.OnTurnChanged += OnTurnChanged;
            if (ItemManager.Instance != null)
                ItemManager.Instance.OnItemAdded += OnItemAdded;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                    SubscribeAlly(allies[i]);
            }

            _initialized = true;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void SubscribeAlly(CharacterBall ally)
        {
            if (ally == null) return;
            if (_allyDeathHandlers.ContainsKey(ally)) return;

            Action deathHandler = () => OnAllyDeath(ally);
            Action<int> damagedHandler = (dmg) => OnAllyTakeDamage(ally, dmg);
            Action<int> healedHandler = (amount) => OnAllyHeal(ally, amount);
            Action killHandler = () => OnAllyKill(ally);
            Action launchHandler = () => OnAllyLaunched(ally);
            Action<Enemy, int> hitEnemyRef = (enemy, damage) => OnEnemyHitWithRef(ally, enemy, damage);
            Action<Enemy, int> killEnemyRef = (enemy, dmg) => OnEnemyKillWithRef(ally, enemy, dmg);
            Action<Enemy, int> critHandler = (enemy, dmg) => OnAllyCriticalHit(ally, enemy, dmg);

            _allyDeathHandlers[ally] = deathHandler;
            _allyDamagedHandlers[ally] = damagedHandler;
            _allyHealedHandlers[ally] = healedHandler;
            _allyKillHandlers[ally] = killHandler;
            _allyLaunchHandlers[ally] = launchHandler;
            _allyHitEnemyRefHandlers[ally] = hitEnemyRef;
            _allyKillEnemyRefHandlers[ally] = killEnemyRef;
            _allyCritHandlers[ally] = critHandler;
            _subscribedAllies.Add(ally);

            ally.OnDeath += deathHandler;
            ally.OnDamaged += damagedHandler;
            ally.OnHealed += healedHandler;
            ally.OnKillEnemy += killHandler;
            ally.OnLaunched += launchHandler;
            ally.OnHitEnemyWithRef += hitEnemyRef;
            ally.OnKillEnemyWithRef += killEnemyRef;
            ally.OnCriticalHit += critHandler;
        }

        private void UnsubscribeAll()
        {
            for (int i = 0; i < _subscribedAllies.Count; i++)
            {
                CharacterBall ally = _subscribedAllies[i];
                if (ally == null) continue;

                if (_allyDeathHandlers.TryGetValue(ally, out Action deathHandler))
                    ally.OnDeath -= deathHandler;
                if (_allyDamagedHandlers.TryGetValue(ally, out Action<int> damagedHandler))
                    ally.OnDamaged -= damagedHandler;
                if (_allyHealedHandlers.TryGetValue(ally, out Action<int> healedHandler))
                    ally.OnHealed -= healedHandler;
                if (_allyKillHandlers.TryGetValue(ally, out Action killHandler))
                    ally.OnKillEnemy -= killHandler;
                if (_allyLaunchHandlers.TryGetValue(ally, out Action launchHandler))
                    ally.OnLaunched -= launchHandler;
                if (_allyHitEnemyRefHandlers.TryGetValue(ally, out Action<Enemy, int> hitEnemyRefHandler))
                    ally.OnHitEnemyWithRef -= hitEnemyRefHandler;
                if (_allyKillEnemyRefHandlers.TryGetValue(ally, out Action<Enemy, int> killEnemyRefHandler))
                    ally.OnKillEnemyWithRef -= killEnemyRefHandler;
                if (_allyCritHandlers.TryGetValue(ally, out Action<Enemy, int> critHandler))
                    ally.OnCriticalHit -= critHandler;
            }

            _subscribedAllies.Clear();
            _allyDeathHandlers.Clear();
            _allyDamagedHandlers.Clear();
            _allyHealedHandlers.Clear();
            _allyKillHandlers.Clear();
            _allyLaunchHandlers.Clear();
            _allyHitEnemyRefHandlers.Clear();
            _allyKillEnemyRefHandlers.Clear();
            _allyCritHandlers.Clear();
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (!_initialized) return;

            CharacterBall ally = participant as CharacterBall;
            if (ally == null || ally.IsDead) return;

            // Assure l'abonnement sur les alliés qui apparaissent plus tard.
            SubscribeAlly(ally);
        }

        private void OnAllyDeath(CharacterBall ally)
        {
            ItemEffectContext context = BuildContext();
            context.SourceAlly = ally;
            context.TurnManager = turnManager;
            NotifyTrigger(ItemTrigger.OnAllyDeath, context);
        }

        private void OnAllyTakeDamage(CharacterBall ally, int damage)
        {
            ItemEffectContext context = BuildContext();
            context.SourceAlly = ally;
            context.DamageAmount = damage;
            context.TurnManager = turnManager;
            NotifyTrigger(ItemTrigger.OnAllyTakeDamage, context);
        }

        private void OnAllyHeal(CharacterBall ally, int amount)
        {
            ItemEffectContext context = BuildContext();
            context.SourceAlly = ally;
            context.DamageAmount = amount;
            context.TurnManager = turnManager;
            NotifyTrigger(ItemTrigger.OnAllyHeal, context);
        }

        private void OnAllyKill(CharacterBall ally)
        {
            ItemEffectContext context = BuildContext();
            context.SourceAlly = ally;
            context.TurnManager = turnManager;
            NotifyTrigger(ItemTrigger.OnAllyKill, context);
        }

        private void OnAllyLaunched(CharacterBall ally)
        {
            ItemEffectContext context = BuildContext();
            context.SourceAlly = ally;
            context.TurnManager = turnManager;
            NotifyTrigger(ItemTrigger.OnAllyLaunch, context);
        }

        private void OnEnemyHitWithRef(CharacterBall ally, Enemy enemy, int damage)
        {
            if (enemy == null) return;

            ItemEffectContext context = BuildContext();
            context.DamageAmount = damage;
            context.SourceAlly = ally;
            context.TargetEnemy = enemy;
            context.TurnManager = turnManager;
            if (context.SourceAlly != null)
            {
                context.WallBounceCount = context.SourceAlly.WallBounceCountThisLaunch;
                context.EnemyHitCount = context.SourceAlly.EnemyHitCountThisLaunch;
                context.VelocityRatio = GetVelocityRatio(context.SourceAlly);
            }
            NotifyTrigger(ItemTrigger.OnEnemyHit, context);
        }

        private void OnEnemyKillWithRef(CharacterBall ally, Enemy enemy, int damage)
        {
            if (enemy == null) return;

            ItemEffectContext context = BuildContext();
            context.SourceAlly = ally;
            context.TargetEnemy = enemy;
            context.DamageAmount = damage;
            context.TurnManager = turnManager;
            NotifyTrigger(ItemTrigger.OnEnemyDeath, context);
        }

        private void OnAllyCriticalHit(CharacterBall ally, Enemy enemy, int damage)
        {
            if (enemy == null) return;

            ItemEffectContext context = BuildContext();
            context.SourceAlly = ally;
            context.TargetEnemy = enemy;
            context.DamageAmount = damage;
            context.TurnManager = turnManager;
            NotifyTrigger(ItemTrigger.OnCriticalHit, context);
        }

        private void NotifyTrigger(ItemTrigger trigger, ItemEffectContext context)
        {
            if (ItemManager.Instance == null) return;
            ItemManager.Instance.NotifyTrigger(trigger, context);
        }

        private ItemEffectContext BuildContext()
        {
            if (ItemEffectRegistry.Instance == null)
                return new ItemEffectContext();

            return ItemEffectRegistry.Instance.GetSharedContext();
        }

        private float GetVelocityRatio(CharacterBall ally)
        {
            if (ally == null) return 0f;
            float max = ally.LaunchSpeedThisLaunch;
            if (max <= 0f) return 0f;
            return Mathf.Clamp01(ally.CurrentVelocity / max);
        }

        private void OnItemAdded(ItemInstance instance)
        {
            ItemEffectContext context = BuildContext();
            context.TurnManager = turnManager;
            if (ItemManager.Instance != null)
                ItemManager.Instance.NotifyTrigger(ItemTrigger.OnItemAcquired, context);
        }
    }
}
