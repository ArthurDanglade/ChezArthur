using System;
using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives.Handlers;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives
{
    /// <summary>
    /// Composant central des passifs ennemis : résolution data-driven, handlers spécialisés, stacks et pools A/B.
    /// </summary>
    public class EnemyPassiveRuntime : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES — références
        // ═══════════════════════════════════════════

        private Enemy _owner;
        private TurnManager _turnManager;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES — état passifs
        // ═══════════════════════════════════════════

        private List<EnemyPassiveData> _sourcePassives;
        private List<EnemyPassiveData> _activePassives;
        private Dictionary<int, int> _stacks;
        private HashSet<int> _triggeredOnce;
        private Dictionary<int, int> _durationCounters;
        private IEnemyPassiveHandler[] _handlerPerPassive;
        private bool _subscribed;
        private int _alliesKilledThisStage;
        private bool _survivedFatalBlowFlag;
        private bool _resurrectionArmed;
        private float _resurrectionHpFraction;
        private readonly List<Enemy> _scratchEnemies = new List<Enemy>(8);
        private readonly List<CharacterBall> _scratchAllies = new List<CharacterBall>(8);

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES — registre handlers
        // ═══════════════════════════════════════════

        private static readonly Dictionary<string, Func<IEnemyPassiveHandler>> HandlerFactories =
            new Dictionary<string, Func<IEnemyPassiveHandler>>();

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES STATIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Enregistre une factory de handler. Appelé au démarrage du jeu (avant toute Initialize).
        /// </summary>
        public static void RegisterHandler(string handlerId, Func<IEnemyPassiveHandler> factory)
        {
            if (string.IsNullOrEmpty(handlerId) || factory == null) return;
            HandlerFactories[handlerId] = factory;
        }

        /// <summary>
        /// Efface toutes les factories enregistrées. Utile pour les tests.
        /// </summary>
        public static void ClearHandlers()
        {
            HandlerFactories.Clear();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES D'INSTANCE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise le runtime avec les passifs de cet ennemi. Résout les pools, instancie les handlers, s'abonne aux events.
        /// </summary>
        public void Initialize(Enemy owner, List<EnemyPassiveData> passives, TurnManager turnManager)
        {
            Cleanup();

            _owner = owner;
            _turnManager = turnManager;
            _sourcePassives = passives != null ? new List<EnemyPassiveData>(passives) : new List<EnemyPassiveData>();
            _activePassives = new List<EnemyPassiveData>(_sourcePassives.Count);
            _stacks = new Dictionary<int, int>(_sourcePassives.Count);
            _triggeredOnce = new HashSet<int>();
            _durationCounters = new Dictionary<int, int>(_sourcePassives.Count);
            _alliesKilledThisStage = 0;
            _survivedFatalBlowFlag = false;
            _resurrectionArmed = false;
            _resurrectionHpFraction = 0f;

            for (int i = 0; i < _sourcePassives.Count; i++)
            {
                EnemyPassiveData resolved = _sourcePassives[i] != null ? _sourcePassives[i].ResolvePool() : null;
                if (resolved == null) continue;

                int idx = _activePassives.Count;
                _activePassives.Add(resolved);
                _stacks[idx] = 0;
            }

            int n = _activePassives.Count;
            _handlerPerPassive = new IEnemyPassiveHandler[n];

            for (int i = 0; i < n; i++)
            {
                EnemyPassiveData d = _activePassives[i];
                if (d == null || string.IsNullOrEmpty(d.SpecialHandlerId))
                    continue;

                if (!HandlerFactories.TryGetValue(d.SpecialHandlerId, out Func<IEnemyPassiveHandler> factory))
                {
                    Debug.LogWarning($"[EnemyPassiveRuntime] Aucune factory pour SpecialHandlerId \"{d.SpecialHandlerId}\" sur {_owner?.name}.", _owner);
                    continue;
                }

                IEnemyPassiveHandler handler = factory();
                handler.Initialize(_owner, d, _turnManager);
                _handlerPerPassive[i] = handler;
            }

            SubscribeEvents();
            NotifyTrigger(EnemyPassiveTrigger.OnStageStart);
        }

        /// <summary>
        /// Réinitialise pour un nouvel étage : re-pools, stacks / oneTime sauf persistance, handlers.
        /// </summary>
        public void ResetForNewStage()
        {
            if (_owner == null || _turnManager == null)
                return;

            for (int i = 0; i < _handlerPerPassive.Length; i++)
            {
                if (_handlerPerPassive[i] != null)
                    _handlerPerPassive[i].ResetForNewStage();
            }

            var oldPassives = _activePassives;
            var oldStacks = new Dictionary<int, int>(_stacks);

            _activePassives = new List<EnemyPassiveData>(_sourcePassives.Count);
            _stacks.Clear();
            _triggeredOnce.Clear();
            _durationCounters.Clear();
            _alliesKilledThisStage = 0;
            _survivedFatalBlowFlag = false;

            for (int i = 0; i < _sourcePassives.Count; i++)
            {
                EnemyPassiveData resolved = _sourcePassives[i] != null ? _sourcePassives[i].ResolvePool() : null;
                if (resolved == null) continue;

                int idx = _activePassives.Count;
                _activePassives.Add(resolved);
                if (idx < oldPassives.Count
                    && oldPassives[idx] == resolved
                    && resolved.PersistBetweenStages
                    && oldStacks.TryGetValue(idx, out int kept))
                {
                    _stacks[idx] = kept;
                }
                else
                    _stacks[idx] = 0;
            }

            int n = _activePassives.Count;
            for (int i = 0; i < _handlerPerPassive.Length; i++)
            {
                if (_handlerPerPassive[i] != null)
                    _handlerPerPassive[i].Cleanup();
            }

            _handlerPerPassive = new IEnemyPassiveHandler[n];
            for (int i = 0; i < n; i++)
            {
                EnemyPassiveData d = _activePassives[i];
                if (d == null || string.IsNullOrEmpty(d.SpecialHandlerId))
                    continue;

                if (!HandlerFactories.TryGetValue(d.SpecialHandlerId, out Func<IEnemyPassiveHandler> factory))
                {
                    Debug.LogWarning($"[EnemyPassiveRuntime] Aucune factory pour SpecialHandlerId \"{d.SpecialHandlerId}\" sur {_owner?.name}.", _owner);
                    continue;
                }

                IEnemyPassiveHandler handler = factory();
                handler.Initialize(_owner, d, _turnManager);
                _handlerPerPassive[i] = handler;
            }

            NotifyTrigger(EnemyPassiveTrigger.OnStageStart);
        }

        /// <summary>
        /// Notifie un trigger externe (CombatManager, collisions, etc.).
        /// </summary>
        public void NotifyTrigger(EnemyPassiveTrigger trigger, CharacterBall ally = null, Enemy mate = null, int damageOrHeal = 0)
        {
            if (_owner == null || _owner.IsDead)
                return;

            if (trigger == EnemyPassiveTrigger.OnAllyKilled)
                _alliesKilledThisStage++;

            for (int i = 0; i < _activePassives.Count; i++)
                EvaluatePassive(i, trigger, ally, mate, damageOrHeal);
        }

        /// <summary>
        /// Notifie un changement de PV (seuils, handlers).
        /// </summary>
        public void NotifyHpChanged(int currentHp, int maxHp)
        {
            if (_owner == null || _owner.IsDead)
                return;

            for (int i = 0; i < _handlerPerPassive.Length; i++)
            {
                if (_handlerPerPassive[i] != null)
                    _handlerPerPassive[i].OnHpChanged(currentHp, maxHp);
            }

            for (int i = 0; i < _activePassives.Count; i++)
            {
                EvaluatePassive(i, EnemyPassiveTrigger.OnHpThreshold, null, null, 0);
                if (_activePassives[i] != null && _activePassives[i].Trigger == EnemyPassiveTrigger.Permanent)
                    EvaluatePassive(i, EnemyPassiveTrigger.Permanent, null, null, 0);
            }
        }

        /// <summary>
        /// Interception des soins alliés : somme des fractions (data.Value) pour chaque passif InterceptAllyHeal dont la condition est OK.
        /// </summary>
        public int InterceptHeal(int healAmount, CharacterBall healedAlly = null)
        {
            if (healAmount <= 0 || _owner == null || _owner.IsDead)
                return 0;

            int intercepted = 0;
            for (int i = 0; i < _activePassives.Count; i++)
            {
                EnemyPassiveData d = _activePassives[i];
                if (d == null || d.Effect != EnemyPassiveEffect.InterceptAllyHeal)
                    continue;
                if (d.Trigger != EnemyPassiveTrigger.OnAllyHealed)
                    continue;
                if (!CheckCondition(i, d, healedAlly))
                    continue;

                intercepted += Mathf.RoundToInt(healAmount * d.Value);

                if (d.SpecialValue1 != 0f)
                    ApplyBuffSelfFromValues(i, d, BuffStatType.ATK, d.SpecialValue1, true);
                if (d.SpecialValue2 != 0f)
                    ApplyBuffSelfFromValues(i, d, BuffStatType.DEF, d.SpecialValue2, true);
            }

            return intercepted;
        }

        /// <summary>
        /// Consommation unique de résurrection (armée par l'effet ResurrectSelf).
        /// </summary>
        public bool TryConsumeResurrection(out int reviveHp)
        {
            reviveHp = 0;
            if (!_resurrectionArmed || _owner == null)
                return false;

            _resurrectionArmed = false;
            reviveHp = Mathf.Max(1, Mathf.RoundToInt(_owner.MaxHp * _resurrectionHpFraction));
            return true;
        }

        /// <summary>
        /// À appeler depuis Enemy quand l'ennemi survit à un coup fatal (ex. passive / handler).
        /// </summary>
        public void NotifySurvivedFatalBlow()
        {
            _survivedFatalBlowFlag = true;
        }

        /// <summary>
        /// Nettoie abonnements et handlers.
        /// </summary>
        public void Cleanup()
        {
            UnsubscribeEvents();

            if (_handlerPerPassive != null)
            {
                for (int i = 0; i < _handlerPerPassive.Length; i++)
                {
                    if (_handlerPerPassive[i] != null)
                        _handlerPerPassive[i].Cleanup();
                }
            }

            _handlerPerPassive = Array.Empty<IEnemyPassiveHandler>();
            _owner = null;
            _turnManager = null;
            _sourcePassives = null;
            _activePassives = null;
            _stacks = null;
            _triggeredOnce = null;
            _durationCounters = null;
            _resurrectionArmed = false;
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void OnDestroy()
        {
            Cleanup();
        }

        // ═══════════════════════════════════════════
        // ABONNEMENTS
        // ═══════════════════════════════════════════

        private void SubscribeEvents()
        {
            if (_subscribed || _turnManager == null || _owner == null)
                return;

            _turnManager.OnTurnChanged += OnTurnManagerTurnChanged;
            _turnManager.OnCycleStarted += OnCycleStarted;
            _owner.OnDamaged += OnOwnerDamaged;
            _subscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_subscribed)
                return;

            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged -= OnTurnManagerTurnChanged;
                _turnManager.OnCycleStarted -= OnCycleStarted;
            }

            if (_owner != null)
                _owner.OnDamaged -= OnOwnerDamaged;

            _subscribed = false;
        }

        private void OnTurnManagerTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null || _owner.IsDead)
                return;

            if (ReferenceEquals(participant, _owner))
                _owner.ClearDamageImmunityAtTurnStart();

            if (!ReferenceEquals(participant, _owner))
                return;

            NotifyTrigger(EnemyPassiveTrigger.OnTurnStart);

            // Réévalue les passifs permanents au début du tour.
            for (int i = 0; i < _activePassives.Count; i++)
            {
                if (_activePassives[i] != null && _activePassives[i].Trigger == EnemyPassiveTrigger.Permanent)
                    EvaluatePassive(i, EnemyPassiveTrigger.Permanent, null, null, 0);
            }
        }

        private void OnCycleStarted()
        {
            if (_owner == null || _owner.IsDead)
                return;

            NotifyTrigger(EnemyPassiveTrigger.OnCycleStart);

            // Réévalue les passifs permanents au début du cycle.
            for (int i = 0; i < _activePassives.Count; i++)
            {
                if (_activePassives[i] != null && _activePassives[i].Trigger == EnemyPassiveTrigger.Permanent)
                    EvaluatePassive(i, EnemyPassiveTrigger.Permanent, null, null, 0);
            }
        }

        private void OnOwnerDamaged(int damage)
        {
            if (_owner == null || _owner.IsDead)
                return;

            NotifyTrigger(EnemyPassiveTrigger.OnTakeDamage, damageOrHeal: damage);
        }

        // ═══════════════════════════════════════════
        // ÉVALUATION
        // ═══════════════════════════════════════════

        private void EvaluatePassive(int index, EnemyPassiveTrigger incomingTrigger, CharacterBall ally, Enemy mate, int damageOrHeal)
        {
            if (index < 0 || index >= _activePassives.Count)
                return;

            EnemyPassiveData data = _activePassives[index];
            if (data == null)
                return;

            if (data.Trigger != incomingTrigger)
                return;

            if (data.OneTimeOnly && _triggeredOnce.Contains(index))
                return;

            if (!CheckCondition(index, data, ally))
                return;

            if (data.Effect == EnemyPassiveEffect.SpecialHandler)
            {
                DispatchHandler(index, incomingTrigger, ally, mate, damageOrHeal);
                if (data.OneTimeOnly)
                    _triggeredOnce.Add(index);
                return;
            }

            ApplyEffect(index, data, ally, mate, damageOrHeal);

            if (data.OneTimeOnly)
                _triggeredOnce.Add(index);
        }

        private bool CheckCondition(int index, EnemyPassiveData data, CharacterBall ally)
        {
            switch (data.Condition)
            {
                case EnemyPassiveCondition.None:
                    return true;

                case EnemyPassiveCondition.SelfHpBelow:
                {
                    float ratio = _owner.MaxHp > 0 ? (float)_owner.CurrentHp / _owner.MaxHp : 0f;
                    return ratio < data.ConditionThreshold;
                }

                case EnemyPassiveCondition.SelfHpAbove:
                {
                    float ratio = _owner.MaxHp > 0 ? (float)_owner.CurrentHp / _owner.MaxHp : 0f;
                    return ratio > data.ConditionThreshold;
                }

                case EnemyPassiveCondition.SelfHpFull:
                    return _owner.CurrentHp >= _owner.MaxHp && _owner.MaxHp > 0;

                case EnemyPassiveCondition.MinMatesAlive:
                    return CountLivingMates() >= data.ConditionCount;

                case EnemyPassiveCondition.NoMatesAlive:
                    return CountLivingMates() == 0;

                case EnemyPassiveCondition.StacksReachedMax:
                {
                    int s = GetStackCount(index);
                    int cap = data.MaxStacks > 0 ? data.MaxStacks : int.MaxValue;
                    return s >= cap;
                }

                case EnemyPassiveCondition.TargetAllyRole:
                    return ally != null && GetAllyRole(ally) == data.ConditionRole;

                case EnemyPassiveCondition.MinAlliesKilled:
                    return _alliesKilledThisStage >= data.ConditionCount;

                case EnemyPassiveCondition.AllAlliesSameRole:
                    return AllAliveAlliesSameRole();

                case EnemyPassiveCondition.TeamHasAllThreeRoles:
                    return TeamHasAttackerDefenderSupport();

                case EnemyPassiveCondition.SurvivedFatalBlow:
                    if (!_survivedFatalBlowFlag)
                        return false;
                    _survivedFatalBlowFlag = false;
                    return true;

                case EnemyPassiveCondition.SpecialGaugeFull:
                    return false;

                default:
                    return true;
            }
        }

        private CharacterRole GetAllyRole(CharacterBall ally)
        {
            if (ally.ActiveSpec != null)
                return ally.ActiveSpec.Role;
            return ally.Data != null ? ally.Data.Role : default;
        }

        private int CountLivingMates()
        {
            FillScratchEnemies();
            int count = 0;
            for (int i = 0; i < _scratchEnemies.Count; i++)
            {
                Enemy e = _scratchEnemies[i];
                if (e != null && !e.IsDead && e != _owner)
                    count++;
            }

            return count;
        }

        private void FillScratchEnemies()
        {
            _scratchEnemies.Clear();
            if (_turnManager == null) return;

            IReadOnlyList<ITurnParticipant> parts = _turnManager.Participants;
            for (int i = 0; i < parts.Count; i++)
            {
                ITurnParticipant p = parts[i];
                if (p == null || p.IsDead || p.IsAlly) continue;
                if (p is Enemy en)
                    _scratchEnemies.Add(en);
            }
        }

        private void FillScratchAllies()
        {
            _scratchAllies.Clear();
            if (_turnManager == null) return;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall a = allies[i];
                if (a != null && !a.IsDead)
                    _scratchAllies.Add(a);
            }
        }

        private bool AllAliveAlliesSameRole()
        {
            FillScratchAllies();
            if (_scratchAllies.Count <= 1)
                return true;

            CharacterRole first = GetAllyRole(_scratchAllies[0]);
            for (int i = 1; i < _scratchAllies.Count; i++)
            {
                if (GetAllyRole(_scratchAllies[i]) != first)
                    return false;
            }

            return true;
        }

        private bool TeamHasAttackerDefenderSupport()
        {
            FillScratchAllies();
            bool atk = false, def = false, sup = false;
            for (int i = 0; i < _scratchAllies.Count; i++)
            {
                switch (GetAllyRole(_scratchAllies[i]))
                {
                    case CharacterRole.Attacker: atk = true; break;
                    case CharacterRole.Defender: def = true; break;
                    case CharacterRole.Support: sup = true; break;
                }
            }

            return atk && def && sup;
        }

        private int GetStackCount(int index)
        {
            return _stacks != null && _stacks.TryGetValue(index, out int s) ? s : 0;
        }

        // ═══════════════════════════════════════════
        // EFFETS
        // ═══════════════════════════════════════════

        private void ApplyEffect(int index, EnemyPassiveData data, CharacterBall ally, Enemy mate, int damageOrHeal)
        {
            BuffReceiver ownerBr = _owner.BuffReceiver;
            float stackedValue = data.Value + GetStackCount(index) * data.StackValue;

            switch (data.Effect)
            {
                case EnemyPassiveEffect.None:
                    break;

                case EnemyPassiveEffect.BuffSelfATK:
                    ApplyBuff(ownerBr, data, index, BuffStatType.ATK, stackedValue, data.IsPercentage);
                    break;

                case EnemyPassiveEffect.BuffSelfDEF:
                    ApplyBuff(ownerBr, data, index, BuffStatType.DEF, stackedValue, data.IsPercentage);
                    break;

                case EnemyPassiveEffect.BuffSelfSPD:
                    ApplyBuff(ownerBr, data, index, BuffStatType.Speed, stackedValue, data.IsPercentage);
                    break;

                case EnemyPassiveEffect.BuffSelfLaunchForce:
                    _owner.AddLaunchForceBonus(data.IsPercentage ? stackedValue : stackedValue / 100f);
                    break;

                case EnemyPassiveEffect.HealSelf:
                {
                    int heal = Mathf.RoundToInt(_owner.MaxHp * data.Value);
                    _owner.Heal(heal);
                    break;
                }

                case EnemyPassiveEffect.ShieldSelf:
                {
                    EnemyShieldSystem shieldSys = _owner.GetComponent<EnemyShieldSystem>();
                    if (shieldSys == null)
                        shieldSys = _owner.gameObject.AddComponent<EnemyShieldSystem>();
                    shieldSys.Initialize(_owner, _turnManager);
                    shieldSys.ActivateShield(data.Value);
                    if (data.DurationCycles > 0)
                        shieldSys.EnableShieldRegen(data.SpecialValue1);
                    break;
                }

                case EnemyPassiveEffect.ResurrectSelf:
                    _resurrectionArmed = true;
                    _resurrectionHpFraction = data.Value;
                    break;

                case EnemyPassiveEffect.ImmunityOneTurn:
                    _owner.GrantDamageImmunityForOneEnemyTurn();
                    break;

                case EnemyPassiveEffect.HealMate:
                {
                    Enemy targetMate = ResolveMateTarget(mate);
                    if (targetMate != null)
                    {
                        int heal = Mathf.RoundToInt(targetMate.MaxHp * data.Value);
                        targetMate.Heal(heal);
                    }

                    break;
                }

                case EnemyPassiveEffect.BuffMateATK:
                {
                    Enemy targetMate = ResolveMateTarget(mate);
                    if (targetMate != null && targetMate.BuffReceiver != null)
                        ApplyBuff(targetMate.BuffReceiver, data, index, BuffStatType.ATK, stackedValue, data.IsPercentage);
                    break;
                }

                case EnemyPassiveEffect.BuffMateDEF:
                {
                    Enemy targetMate = ResolveMateTarget(mate);
                    if (targetMate != null && targetMate.BuffReceiver != null)
                        ApplyBuff(targetMate.BuffReceiver, data, index, BuffStatType.DEF, stackedValue, data.IsPercentage);
                    break;
                }

                case EnemyPassiveEffect.DebuffAllyATK:
                {
                    CharacterBall target = ResolveAllyTarget(ally);
                    if (target != null && target.BuffReceiver != null)
                        ApplyBuff(target.BuffReceiver, data, index, BuffStatType.ATK, -Mathf.Abs(stackedValue), data.IsPercentage);
                    break;
                }

                case EnemyPassiveEffect.DebuffAllySPD:
                {
                    CharacterBall target = ResolveAllyTarget(ally);
                    if (target != null && target.BuffReceiver != null)
                        ApplyBuff(target.BuffReceiver, data, index, BuffStatType.Speed, -Mathf.Abs(stackedValue), data.IsPercentage);
                    break;
                }

                case EnemyPassiveEffect.ReflectDamageToAttacker:
                    if (ally != null && damageOrHeal > 0)
                    {
                        int reflected = Mathf.RoundToInt(damageOrHeal * data.Value);
                        if (reflected > 0)
                            ally.TakeDamage(reflected);
                    }

                    break;

                case EnemyPassiveEffect.DamageAllAllies:
                    FillScratchAllies();
                    for (int i = 0; i < _scratchAllies.Count; i++)
                    {
                        CharacterBall a = _scratchAllies[i];
                        if (a == null) continue;
                        int dmg = Mathf.RoundToInt(a.MaxHp * data.Value);
                        if (dmg > 0)
                            a.TakeDamage(dmg);
                    }

                    break;

                case EnemyPassiveEffect.InterceptAllyHeal:
                    break;

                case EnemyPassiveEffect.CancelAllyBuffs:
                    FillScratchAllies();
                    for (int i = 0; i < _scratchAllies.Count; i++)
                    {
                        CharacterBall a = _scratchAllies[i];
                        if (a != null && a.BuffReceiver != null)
                            a.BuffReceiver.ClearAll();
                    }

                    break;

                case EnemyPassiveEffect.ChanceToMissAlly:
                {
                    CharacterBall target = ResolveAllyTarget(ally);
                    if (target != null && target.BuffReceiver != null)
                    {
                        // Applique une chance de rater via MissChance.
                        // La résolution du raté est gérée au moment du tour dans le système de combat.
                        ApplyBuff(target.BuffReceiver, data, index, BuffStatType.MissChance, data.Value, false);
                    }

                    break;
                }

                case EnemyPassiveEffect.AddStack:
                {
                    int cap = data.MaxStacks > 0 ? data.MaxStacks : int.MaxValue;
                    int cur = GetStackCount(index);
                    _stacks[index] = Mathf.Min(cur + 1, cap);
                    break;
                }

                case EnemyPassiveEffect.ResetStack:
                    _stacks[index] = 0;
                    break;

                case EnemyPassiveEffect.SpecialHandler:
                    break;
            }
        }

        private void ApplyBuffSelfFromValues(int index, EnemyPassiveData data, BuffStatType stat, float value, bool isPercent)
        {
            BuffReceiver br = _owner.BuffReceiver;
            if (br == null) return;
            ApplyBuff(br, data, index, stat, value, isPercent);
        }

        private void ApplyBuff(BuffReceiver target, EnemyPassiveData data, int passiveIndex, BuffStatType stat, float value, bool isPercent)
        {
            if (target == null) return;

            var buff = new BuffData
            {
                BuffId = $"enemy_passive_{_owner.GetInstanceID()}_{passiveIndex}_{(int)stat}",
                Source = null,
                StatType = stat,
                Value = value,
                IsPercent = isPercent,
                RemainingTurns = data.DurationTurns > 0 ? data.DurationTurns : -1,
                RemainingCycles = data.DurationCycles > 0 ? data.DurationCycles : -1,
                UniqueGlobal = true,
                UniquePerSource = false
            };

            target.AddBuff(buff);
        }

        private Enemy ResolveMateTarget(Enemy mate)
        {
            if (mate != null && !mate.IsDead && mate != _owner)
                return mate;

            FillScratchEnemies();
            Enemy best = null;
            int bestHp = int.MaxValue;
            for (int i = 0; i < _scratchEnemies.Count; i++)
            {
                Enemy e = _scratchEnemies[i];
                if (e == null || e.IsDead || e == _owner) continue;
                if (e.CurrentHp < bestHp)
                {
                    bestHp = e.CurrentHp;
                    best = e;
                }
            }

            return best;
        }

        private CharacterBall ResolveAllyTarget(CharacterBall ally)
        {
            if (ally != null && !ally.IsDead)
                return ally;

            FillScratchAllies();
            if (_scratchAllies.Count == 0)
                return null;
            int pick = UnityEngine.Random.Range(0, _scratchAllies.Count);
            return _scratchAllies[pick];
        }

        private void DispatchHandler(int index, EnemyPassiveTrigger trigger, CharacterBall ally, Enemy mate, int damageOrHeal)
        {
            IEnemyPassiveHandler h = index >= 0 && index < _handlerPerPassive.Length ? _handlerPerPassive[index] : null;
            if (h == null)
                return;

            switch (trigger)
            {
                case EnemyPassiveTrigger.OnTurnStart:
                    h.OnTurnStart();
                    break;
                case EnemyPassiveTrigger.OnCycleStart:
                    h.OnCycleStart();
                    break;
                case EnemyPassiveTrigger.OnTakeDamage:
                    h.OnTakeDamage(damageOrHeal);
                    break;
                case EnemyPassiveTrigger.OnAllyDamaged:
                    h.OnAllyDamaged(ally, damageOrHeal);
                    break;
                case EnemyPassiveTrigger.OnAllyHealed:
                    h.OnAllyHealed(ally, damageOrHeal);
                    break;
                case EnemyPassiveTrigger.OnAllyKilled:
                    h.OnAllyKilled(ally);
                    break;
                case EnemyPassiveTrigger.OnMateKilled:
                    h.OnMateKilled(mate);
                    break;
                case EnemyPassiveTrigger.OnAnyEntityKilled:
                    if (mate != null)
                        h.OnMateKilled(mate);
                    else
                        h.OnAllyKilled(ally);
                    break;
                case EnemyPassiveTrigger.OnKillAlly:
                    h.OnAllyKilled(ally);
                    break;
                case EnemyPassiveTrigger.OnHitAlly:
                    h.OnHitAlly(ally);
                    break;
                case EnemyPassiveTrigger.OnHitByAlly:
                    h.OnHitByAlly(ally);
                    break;
                default:
                    break;
            }
        }
    }
}
