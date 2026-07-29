using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Pont central des événements gameplay vers le registry des handlers de valises.
    /// Collecte les events, remplit le contexte, dispatch — aucune logique d'effet ici.
    /// </summary>
    public class ValiseEventBridge : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private AudioClip megaCritSfx;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static ValiseEventBridge _instance;
        private readonly List<CharacterBall> _subscribedAllies = new List<CharacterBall>();
        private readonly Dictionary<CharacterBall, Action> _allyDeathHandlers = new Dictionary<CharacterBall, Action>();
        private readonly Dictionary<CharacterBall, Action> _allyKillHandlers = new Dictionary<CharacterBall, Action>();
        private readonly Dictionary<CharacterBall, Action<int>> _allyDamagedHandlers = new Dictionary<CharacterBall, Action<int>>();
        private readonly Dictionary<CharacterBall, Action<Enemy, int>> _allyHitEnemyRefHandlers = new Dictionary<CharacterBall, Action<Enemy, int>>();
        private readonly Dictionary<CharacterBall, Action<Enemy, int>> _allyCritValiseHandlers = new Dictionary<CharacterBall, Action<Enemy, int>>();
        private readonly Dictionary<CharacterBall, SpecializationData> _lastSpecByAlly = new Dictionary<CharacterBall, SpecializationData>();
        private bool _initialized;
        private bool _superLancerSubscribed;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static ValiseEventBridge Instance => _instance;

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
            ValiseEffectRegistry.EnsureExists(transform);
            BulletTimeController.EnsureExists(transform);
            ChezArthur.UI.ModeFurieGaugeUI.EnsureExists();
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
            UnsubscribeGlobalEvents();
            UnsubscribeSuperLancer();

            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise le pont valises avec le TurnManager courant.
        /// </summary>
        public void Initialize(TurnManager tm)
        {
            UnsubscribeAll();
            UnsubscribeGlobalEvents();
            UnsubscribeSuperLancer();

            turnManager = tm;
            _lastSpecByAlly.Clear();

            ValiseEffectRegistry registry = ValiseEffectRegistry.EnsureExists(transform);
            registry.SetMegaCritSfx(megaCritSfx);
            BulletTimeController.EnsureExists(transform);
            ChezArthur.UI.ModeFurieGaugeUI.EnsureExists();

            ValiseEffectContext runContext = registry.GetSharedContext();
            runContext.TurnManager = turnManager;
            registry.NotifyRunStartAll(runContext);

            if (turnManager == null)
            {
                _initialized = false;
                return;
            }

            SubscribeGlobalEvents();
            SubscribeSuperLancer();

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                    SubscribeAlly(allies[i]);
            }

            _initialized = true;
        }

        private void LateUpdate()
        {
            // SuperLancerSystem peut s'éveiller après le bridge — rattrapage unique.
            if (_initialized && !_superLancerSubscribed)
                SubscribeSuperLancer();
        }

        /// <summary>
        /// Notifie le début d'étage pour reset des états temporaires.
        /// </summary>
        public void NotifyStageStart()
        {
            if (!_initialized || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            ValiseManager.Instance.NotifyStageStart(context);
        }

        /// <summary>
        /// Notifie le début du tour d'un allié (Discipline / Caméléon).
        /// </summary>
        public void NotifyAllyTurnStart(CharacterBall ally)
        {
            if (!_initialized || ally == null || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            bool hasPreviousTurn = _lastSpecByAlly.TryGetValue(ally, out SpecializationData previousSpec);
            SpecializationData currentSpec = ally.ActiveSpec;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            context.SourceAlly = ally;
            context.HasPreviousTurn = hasPreviousTurn;
            context.PreviousSpec = previousSpec;
            context.CurrentSpec = currentSpec;
            ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnAllyTurnStart, context);

            _lastSpecByAlly[ally] = currentSpec;
        }

        /// <summary>
        /// Tente le renvoi de dégâts depuis l'ennemi attaquant (appelé par Enemy).
        /// </summary>
        public void TryRenvoiFromEnemyAttack(Enemy attacker, CharacterBall victim, int damageReceived)
        {
            if (!_initialized || attacker == null || victim == null) return;
            if (damageReceived <= 0 || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            context.SourceAlly = victim;
            context.TargetEnemy = attacker;
            context.DamageAmount = damageReceived;
            ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnAllyDamagedByEnemy, context);
        }

        /// <summary>
        /// Synergie Shield Dévastateur : renvoi des dégâts absorbés par le shield.
        /// </summary>
        public void TryShieldDevastatorFromEnemyAttack(Enemy attacker, CharacterBall victim, int shieldAbsorbed)
        {
            if (!_initialized) return;
            BouclierHandler.TryDevastatorFromEnemyAttack(attacker, victim, shieldAbsorbed);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void SubscribeGlobalEvents()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.OnTalsChanged += OnTalsChanged;
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.OnItemAdded += OnItemChanged;
                ItemManager.Instance.OnItemSacrificed += OnItemChanged;
            }
            if (ValiseManager.Instance != null)
            {
                ValiseManager.Instance.OnValiseAdded += OnValiseStatsChanged;
                ValiseManager.Instance.OnValiseUpgraded += OnValiseStatsChanged;
                ValiseManager.Instance.OnValiseUpgradedWithRarity += OnValiseUpgradedWithRarity;
            }
        }

        private void UnsubscribeGlobalEvents()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.OnTalsChanged -= OnTalsChanged;
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.OnItemAdded -= OnItemChanged;
                ItemManager.Instance.OnItemSacrificed -= OnItemChanged;
            }
            if (ValiseManager.Instance != null)
            {
                ValiseManager.Instance.OnValiseAdded -= OnValiseStatsChanged;
                ValiseManager.Instance.OnValiseUpgraded -= OnValiseStatsChanged;
                ValiseManager.Instance.OnValiseUpgradedWithRarity -= OnValiseUpgradedWithRarity;
            }
        }

        private void SubscribeSuperLancer()
        {
            if (_superLancerSubscribed || SuperLancerSystem.Instance == null) return;

            SuperLancerSystem.Instance.OnSuperLancer += OnSuperLancer;
            SuperLancerSystem.Instance.OnNormalLaunch += OnNormalLaunch;
            _superLancerSubscribed = true;
        }

        private void UnsubscribeSuperLancer()
        {
            if (!_superLancerSubscribed || SuperLancerSystem.Instance == null)
            {
                _superLancerSubscribed = false;
                return;
            }

            SuperLancerSystem.Instance.OnSuperLancer -= OnSuperLancer;
            SuperLancerSystem.Instance.OnNormalLaunch -= OnNormalLaunch;
            _superLancerSubscribed = false;
        }

        private void OnSuperLancer(CharacterBall ball)
        {
            if (!_initialized) return;

            if (ValiseManager.Instance != null && ValiseEffectRegistry.Instance != null)
            {
                ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
                context.TurnManager = turnManager;
                context.SourceAlly = ball;
                ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnSuperLancer, context);
            }

            // Synergie Crescendo+Furie : descente jauge (en plus du Bullet Time).
            PressionJeLaBoisHandler.TryDecreaseOnSuperLancer();
        }

        private void OnNormalLaunch(CharacterBall ball)
        {
            if (!_initialized || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            context.SourceAlly = ball;
            ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnNormalLaunch, context);
        }

        private void SubscribeAlly(CharacterBall ally)
        {
            if (ally == null) return;
            if (_allyDeathHandlers.ContainsKey(ally)) return;

            Action deathHandler = () => OnAllyDeath(ally);
            Action killHandler = () => OnAllyKill(ally);
            Action<int> damagedHandler = (damage) => OnAllyTakeDamage(ally, damage);
            Action<Enemy, int> hitEnemyHandler = (enemy, damage) => OnAllyHitEnemy(ally, enemy, damage);
            Action<Enemy, int> critValiseHandler = (enemy, dmg) => OnAllyCrit(ally, enemy, dmg);

            _allyDeathHandlers[ally] = deathHandler;
            _allyKillHandlers[ally] = killHandler;
            _allyDamagedHandlers[ally] = damagedHandler;
            _allyHitEnemyRefHandlers[ally] = hitEnemyHandler;
            _allyCritValiseHandlers[ally] = critValiseHandler;
            _subscribedAllies.Add(ally);
            ally.SyncTrackedEffectiveMaxHp();

            ally.OnDeath += deathHandler;
            ally.OnKillEnemy += killHandler;
            ally.OnDamaged += damagedHandler;
            ally.OnHitEnemyWithRef += hitEnemyHandler;
            ally.OnCriticalHit += critValiseHandler;
        }

        private void UnsubscribeAll()
        {
            for (int i = 0; i < _subscribedAllies.Count; i++)
            {
                CharacterBall ally = _subscribedAllies[i];
                if (ally == null) continue;

                if (_allyDeathHandlers.TryGetValue(ally, out Action deathHandler))
                    ally.OnDeath -= deathHandler;
                if (_allyKillHandlers.TryGetValue(ally, out Action killHandler))
                    ally.OnKillEnemy -= killHandler;
                if (_allyDamagedHandlers.TryGetValue(ally, out Action<int> damagedHandler))
                    ally.OnDamaged -= damagedHandler;
                if (_allyHitEnemyRefHandlers.TryGetValue(ally, out Action<Enemy, int> hitEnemyHandler))
                    ally.OnHitEnemyWithRef -= hitEnemyHandler;
                if (_allyCritValiseHandlers.TryGetValue(ally, out Action<Enemy, int> critValiseHandler))
                    ally.OnCriticalHit -= critValiseHandler;
            }

            _subscribedAllies.Clear();
            _allyDeathHandlers.Clear();
            _allyKillHandlers.Clear();
            _allyDamagedHandlers.Clear();
            _allyHitEnemyRefHandlers.Clear();
            _allyCritValiseHandlers.Clear();
            _lastSpecByAlly.Clear();
        }

        private void OnAllyKill(CharacterBall ally)
        {
            if (!_initialized || ally == null || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            context.SourceAlly = ally;
            ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnAllyKill, context);
        }

        private void OnAllyDeath(CharacterBall ally)
        {
            if (!_initialized || ally == null || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            context.SourceAlly = ally;
            ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnAllyDeath, context);
        }

        private void OnAllyTakeDamage(CharacterBall ally, int damage)
        {
            if (!_initialized || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            context.SourceAlly = ally;
            context.DamageAmount = damage;
            context.BoolFlag = ally != null && ally.LastDamageWasContact;
            ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnAllyTakeDamage, context);
        }

        private void OnTalsChanged(int newTotal)
        {
            if (!_initialized || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            context.IntValue = newTotal;
            ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnTalsChanged, context);
        }

        private void OnItemChanged(ItemInstance instance)
        {
            if (!_initialized || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnItemSlotsChanged, context);
        }

        private void OnAllyHitEnemy(CharacterBall ally, Enemy enemy, int damageDealt)
        {
            if (!_initialized || ally == null || enemy == null) return;
            if (ValiseManager.Instance == null || ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            context.SourceAlly = ally;
            context.TargetEnemy = enemy;
            context.DamageAmount = damageDealt;
            ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnEnemyHit, context);
        }

        private void OnAllyCrit(CharacterBall ally, Enemy enemy, int damage)
        {
            if (!_initialized || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            context.SourceAlly = ally;
            context.TargetEnemy = enemy;
            context.DamageAmount = damage;
            ValiseManager.Instance.NotifyTrigger(ValiseTrigger.OnCriticalHit, context);
        }

        private void OnValiseStatsChanged(ValiseInstance instance)
        {
            if (!_initialized || turnManager == null || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            ValiseManager.Instance.NotifyValiseChanged(instance, context);

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                ally.ApplyEffectiveMaxHpGain();
            }
        }

        private void OnValiseUpgradedWithRarity(ValiseInstance instance, ValiseImprovementRarity rarity)
        {
            if (!_initialized || ValiseManager.Instance == null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            ValiseEffectContext context = ValiseEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            ValiseManager.Instance.NotifyValiseChanged(instance, context);
        }
    }
}
