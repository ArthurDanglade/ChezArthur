using System;
using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives.Handlers;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Feedback;
using ChezArthur.UI;
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
        // CONSTANTES — flottants switch de spé (R4 / D12)
        // ═══════════════════════════════════════════
        private const float LABEL_Y_OFFSET = 0.9f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES — timbres multi-hit (R5)
        // ═══════════════════════════════════════════
        private int _turnStamp;
        private int _cycleStamp;
        private int[] _lastDamageFireTurnStamp;
        private int[] _lastDamageFireCycleStamp;

        /// <summary>
        /// D28 — plafond de renvoi par attaquant et par tour, en fraction des PV max de l'attaquant.
        /// Surchargé par specialValue1 du passif si &gt; 0.
        /// </summary>
        private const float REFLECT_CAP_DEFAULT = 0.15f;
        private Dictionary<CharacterBall, int> _reflectedThisTurn;
        private int _reflectResetStamp;

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
            AllocateDamageFireStamps(n);

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
            AllocateDamageFireStamps(n);
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
            _lastDamageFireTurnStamp = null;
            _lastDamageFireCycleStamp = null;
            _turnStamp = 0;
            _cycleStamp = 0;
            _reflectedThisTurn = null;
            _reflectResetStamp = 0;
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
            // Event STATIQUE : désabonnement symétrique obligatoire (fuite sinon).
            CharacterBall.OnAnySpecSwitchedInCombat += OnAllySpecSwitchedInCombat;
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

            CharacterBall.OnAnySpecSwitchedInCombat -= OnAllySpecSwitchedInCombat;
            _subscribed = false;
        }

        private void OnTurnManagerTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null || _owner.IsDead)
                return;

            // Chaque tour de n'importe qui compte (extra-turns et interludes inclus).
            _turnStamp++;

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

            _cycleStamp++;

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

        /// <summary>
        /// R4 — réévaluation immédiate au switch de spé alliée.
        /// La ré-résolution de la cible affichée (ligne d'aggro) relève du système d'intention (G3),
        /// qui s'abonnera au même event CharacterBall.OnAnySpecSwitchedInCombat.
        /// </summary>
        private void OnAllySpecSwitchedInCombat(CharacterBall ally)
        {
            if (_owner == null || _owner.IsDead)
                return;

            BuffReceiver br = _owner.BuffReceiver;
            float atkBefore = 0f, defBefore = 0f, spdBefore = 0f, lfBefore = 0f;
            if (br != null)
            {
                atkBefore = br.GetStatModifier(BuffStatType.ATK).percent;
                defBefore = br.GetStatModifier(BuffStatType.DEF).percent;
                spdBefore = br.GetStatModifier(BuffStatType.Speed).percent;
                lfBefore = br.GetStatModifier(BuffStatType.LaunchForce).percent;
            }

            if (_activePassives != null)
            {
                for (int i = 0; i < _activePassives.Count; i++)
                {
                    if (_activePassives[i] != null
                        && _activePassives[i].Trigger == EnemyPassiveTrigger.Permanent)
                    {
                        EvaluatePassive(i, EnemyPassiveTrigger.Permanent, null, null, 0);
                    }
                }
            }

            if (_handlerPerPassive != null)
            {
                for (int i = 0; i < _handlerPerPassive.Length; i++)
                {
                    if (_handlerPerPassive[i] != null)
                        _handlerPerPassive[i].OnAllySpecSwitched(ally);
                }
            }

            float atkAfter = 0f, defAfter = 0f, spdAfter = 0f, lfAfter = 0f;
            if (br != null)
            {
                atkAfter = br.GetStatModifier(BuffStatType.ATK).percent;
                defAfter = br.GetStatModifier(BuffStatType.DEF).percent;
                spdAfter = br.GetStatModifier(BuffStatType.Speed).percent;
                lfAfter = br.GetStatModifier(BuffStatType.LaunchForce).percent;
            }

            // Limite documentée : flottants sur la composante percent uniquement
            // (les passifs concernés sont en % ; le flat n'est pas affiché).
            Vector3 labelPos = _owner.transform.position + Vector3.up * LABEL_Y_OFFSET;
            TryShowSpecSwitchLabel("ATK", atkBefore, atkAfter, labelPos);
            TryShowSpecSwitchLabel("DEF", defBefore, defAfter, labelPos);
            TryShowSpecSwitchLabel("VIT", spdBefore, spdAfter, labelPos);
            TryShowSpecSwitchLabel("FORCE", lfBefore, lfAfter, labelPos);
        }

        /// <summary>
        /// Affiche un flottant D12 uniquement si le percent a changé (anti-bruit).
        /// </summary>
        private static void TryShowSpecSwitchLabel(string statLabel, float before, float after, Vector3 worldPos)
        {
            if (Mathf.Approximately(before, after))
                return;

            string label = statLabel + " " + FormatSignedPercent(before) + " → " + FormatSignedPercent(after);
            FloatingNumberSpawner.Instance?.ShowLabel(label, CombatFeedbackPalette.SpecSwitchReeval, worldPos, 1f);
        }

        private static string FormatSignedPercent(float percent)
        {
            int rounded = Mathf.RoundToInt(percent * 100f);
            if (rounded > 0)
                return "+" + rounded + " %";
            if (rounded < 0)
                return rounded + " %";
            return "+0 %";
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

            // R5 — politique multi-hit : budget par tour / par cycle pour les triggers de dégâts reçus.
            if ((incomingTrigger == EnemyPassiveTrigger.OnTakeDamage || incomingTrigger == EnemyPassiveTrigger.OnHitByAlly)
                && data.MultiHitPolicy != EnemyPassiveMultiHitPolicy.PerHit)
            {
                if (data.MultiHitPolicy == EnemyPassiveMultiHitPolicy.PerTurn
                    && _lastDamageFireTurnStamp != null
                    && _lastDamageFireTurnStamp[index] == _turnStamp)
                    return;
                if (data.MultiHitPolicy == EnemyPassiveMultiHitPolicy.PerCycle
                    && _lastDamageFireCycleStamp != null
                    && _lastDamageFireCycleStamp[index] == _cycleStamp)
                    return;
            }

            if (data.OneTimeOnly && _triggeredOnce.Contains(index))
                return;

            if (!CheckCondition(index, data, ally))
            {
                // Correctif stale-buff : Permanent redevenu faux → retire le buff (id instance seulement).
                if (data.Trigger == EnemyPassiveTrigger.Permanent
                    && string.IsNullOrEmpty(data.SharedBuffId))
                {
                    RemoveStaleBuffs(index, data);
                }

                return;
            }

            // Consommation du budget multi-hit uniquement sur déclenchement réel (post-condition).
            if ((incomingTrigger == EnemyPassiveTrigger.OnTakeDamage || incomingTrigger == EnemyPassiveTrigger.OnHitByAlly)
                && data.MultiHitPolicy != EnemyPassiveMultiHitPolicy.PerHit)
            {
                if (_lastDamageFireTurnStamp != null)
                    _lastDamageFireTurnStamp[index] = _turnStamp;
                if (_lastDamageFireCycleStamp != null)
                    _lastDamageFireCycleStamp[index] = _cycleStamp;
            }

            if (data.Effect == EnemyPassiveEffect.SpecialHandler)
            {
                PushEnemyPassiveScope(data);
                try
                {
                    DispatchHandler(index, incomingTrigger, ally, mate, damageOrHeal);
                }
                finally
                {
                    BuffOriginScope.Pop();
                }

                if (data.OneTimeOnly)
                    _triggeredOnce.Add(index);
                return;
            }

            PushEnemyPassiveScope(data);
            try
            {
                ApplyEffect(index, data, ally, mate, damageOrHeal);
                TryAutoIncrementStacks(index, data);
            }
            finally
            {
                BuffOriginScope.Pop();
            }

            if (data.OneTimeOnly)
                _triggeredOnce.Add(index);
        }

        private void PushEnemyPassiveScope(EnemyPassiveData data)
        {
            Transform sourceVisual = null;
            if (_owner != null)
                sourceVisual = _owner.Visual != null ? _owner.Visual : _owner.transform;

            string display = data != null ? data.PassiveName : null;
            string id = data != null ? data.PassiveId : null;
            bool silent = data != null && data.SilentProc;
            BuffOriginScope.Push(BuffOrigin.Passif, sourceVisual, display, id, silent);
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

                case EnemyPassiveCondition.NoAllyOfRole:
                {
                    FillScratchAllies();
                    for (int i = 0; i < _scratchAllies.Count; i++)
                    {
                        if (GetAllyRole(_scratchAllies[i]) == data.ConditionRole)
                            return false;
                    }

                    return true;
                }

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

        /// <summary>
        /// Auto-stack (Colère) : incrément APRÈS ApplyEffect — le tir courant utilise le compte AVANT.
        /// Plafond MaxStacks−1 : bonus max = Value + (MaxStacks−1)×StackValue (ex. +10 % → +50 %).
        /// </summary>
        private void TryAutoIncrementStacks(int index, EnemyPassiveData data)
        {
            if (data == null || _stacks == null)
                return;
            if (data.MaxStacks <= 0 || Mathf.Approximately(data.StackValue, 0f))
                return;
            if (data.Condition == EnemyPassiveCondition.StacksReachedMax)
                return;
            if (!IsAutoStackableBuffEffect(data.Effect))
                return;

            int cur = GetStackCount(index);
            int cap = Mathf.Max(0, data.MaxStacks - 1);
            _stacks[index] = Mathf.Min(cur + 1, cap);
        }

        private static bool IsAutoStackableBuffEffect(EnemyPassiveEffect effect)
        {
            switch (effect)
            {
                case EnemyPassiveEffect.BuffSelfATK:
                case EnemyPassiveEffect.BuffSelfDEF:
                case EnemyPassiveEffect.BuffSelfSPD:
                case EnemyPassiveEffect.BuffSelfLaunchForce:
                case EnemyPassiveEffect.BuffMateATK:
                case EnemyPassiveEffect.BuffMateDEF:
                case EnemyPassiveEffect.BuffEnemyTeamDEF:
                case EnemyPassiveEffect.BuffOtherMatesATK:
                case EnemyPassiveEffect.DebuffAllyATK:
                case EnemyPassiveEffect.DebuffAllySPD:
                    return true;
                default:
                    // AddStack / ResetStack / SpecialHandler / heals / etc. : machinerie explicite.
                    return false;
            }
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
                    // Correctif : buff idempotent via BuffReceiver (fini le cumul AddLaunchForceBonus).
                    ApplyBuff(
                        ownerBr,
                        data,
                        index,
                        BuffStatType.LaunchForce,
                        data.IsPercentage ? stackedValue : stackedValue / 100f,
                        true);
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

                case EnemyPassiveEffect.BuffEnemyTeamDEF:
                {
                    // Aura du Mur : toute l'équipe ennemie, porteur inclus.
                    if (ownerBr != null)
                        ApplyBuff(ownerBr, data, index, BuffStatType.DEF, stackedValue, data.IsPercentage);
                    FillScratchEnemies();
                    for (int i = 0; i < _scratchEnemies.Count; i++)
                    {
                        Enemy e = _scratchEnemies[i];
                        if (e == null || e.IsDead || e == _owner || e.BuffReceiver == null)
                            continue;
                        ApplyBuff(e.BuffReceiver, data, index, BuffStatType.DEF, stackedValue, data.IsPercentage);
                    }

                    break;
                }

                case EnemyPassiveEffect.BuffOtherMatesATK:
                {
                    // Colère du Rempart : tous les autres, porteur exclu.
                    FillScratchEnemies();
                    for (int i = 0; i < _scratchEnemies.Count; i++)
                    {
                        Enemy e = _scratchEnemies[i];
                        if (e == null || e.IsDead || e == _owner || e.BuffReceiver == null)
                            continue;
                        ApplyBuff(e.BuffReceiver, data, index, BuffStatType.ATK, stackedValue, data.IsPercentage);
                    }

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
                        if (_reflectedThisTurn == null)
                            _reflectedThisTurn = new Dictionary<CharacterBall, int>(4);
                        if (_reflectResetStamp != _turnStamp)
                        {
                            _reflectedThisTurn.Clear();
                            _reflectResetStamp = _turnStamp;
                        }

                        float capFraction = data.SpecialValue1 > 0f ? data.SpecialValue1 : REFLECT_CAP_DEFAULT;
                        int capAbsolute = Mathf.RoundToInt(ally.MaxHp * capFraction);
                        _reflectedThisTurn.TryGetValue(ally, out int alreadyReflected);

                        int reflected = Mathf.RoundToInt(damageOrHeal * data.Value);
                        reflected = Mathf.Min(reflected, capAbsolute - alreadyReflected);
                        if (reflected > 0)
                        {
                            _reflectedThisTurn[ally] = alreadyReflected + reflected;
                            // R9 : un renvoi PEUT tuer (choix lisible).
                            ally.TakeDamage(reflected);
                        }
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

            bool bindEnemySource = data.ExpiresWithSource || data.DurationCycles > 0;
            var buff = new BuffData
            {
                BuffId = BuildBuffId(data, passiveIndex, stat),
                Source = null,
                StatType = stat,
                Value = value,
                IsPercent = isPercent,
                RemainingTurns = data.DurationTurns > 0 ? data.DurationTurns : -1,
                RemainingCycles = data.DurationCycles > 0 ? data.DurationCycles : -1,
                EnemySource = bindEnemySource ? _owner : null,
                ExpiresWithSource = data.ExpiresWithSource,
                UniqueGlobal = true,
                UniquePerSource = false
            };

            target.AddBuff(buff);
        }

        /// <summary>
        /// Id de buff : sharedBuffId déclaratif (D25) ou id par instance historique.
        /// </summary>
        private string BuildBuffId(EnemyPassiveData data, int passiveIndex, BuffStatType stat)
        {
            if (!string.IsNullOrEmpty(data.SharedBuffId))
                return data.SharedBuffId + "_" + (int)stat;
            return $"enemy_passive_{_owner.GetInstanceID()}_{passiveIndex}_{(int)stat}";
        }

        private void AllocateDamageFireStamps(int count)
        {
            _lastDamageFireTurnStamp = new int[count];
            _lastDamageFireCycleStamp = new int[count];
            for (int i = 0; i < count; i++)
            {
                _lastDamageFireTurnStamp[i] = -1;
                _lastDamageFireCycleStamp[i] = -1;
            }
        }

        /// <summary>
        /// Retire un buff Permanent devenu illégitime (condition redevenue fausse).
        /// </summary>
        private void RemoveStaleBuffs(int index, EnemyPassiveData data)
        {
            if (!TryMapEffectToBuffStat(data.Effect, out BuffStatType stat))
                return;

            string buffId = BuildBuffId(data, index, stat);
            bool removed = false;

            switch (data.Effect)
            {
                case EnemyPassiveEffect.BuffSelfATK:
                case EnemyPassiveEffect.BuffSelfDEF:
                case EnemyPassiveEffect.BuffSelfSPD:
                case EnemyPassiveEffect.BuffSelfLaunchForce:
                {
                    BuffReceiver br = _owner != null ? _owner.BuffReceiver : null;
                    if (br != null)
                    {
                        br.RemoveBuffsById(buffId);
                        removed = true;
                    }

                    break;
                }

                case EnemyPassiveEffect.BuffMateATK:
                case EnemyPassiveEffect.BuffMateDEF:
                {
                    FillScratchEnemies();
                    for (int i = 0; i < _scratchEnemies.Count; i++)
                    {
                        Enemy e = _scratchEnemies[i];
                        if (e == null || e.IsDead || e == _owner || e.BuffReceiver == null)
                            continue;
                        e.BuffReceiver.RemoveBuffsById(buffId);
                        removed = true;
                    }

                    break;
                }

                case EnemyPassiveEffect.BuffEnemyTeamDEF:
                {
                    BuffReceiver ownerBr = _owner != null ? _owner.BuffReceiver : null;
                    if (ownerBr != null)
                    {
                        ownerBr.RemoveBuffsById(buffId);
                        removed = true;
                    }

                    FillScratchEnemies();
                    for (int i = 0; i < _scratchEnemies.Count; i++)
                    {
                        Enemy e = _scratchEnemies[i];
                        if (e == null || e.IsDead || e == _owner || e.BuffReceiver == null)
                            continue;
                        e.BuffReceiver.RemoveBuffsById(buffId);
                        removed = true;
                    }

                    break;
                }

                case EnemyPassiveEffect.BuffOtherMatesATK:
                {
                    FillScratchEnemies();
                    for (int i = 0; i < _scratchEnemies.Count; i++)
                    {
                        Enemy e = _scratchEnemies[i];
                        if (e == null || e.IsDead || e == _owner || e.BuffReceiver == null)
                            continue;
                        e.BuffReceiver.RemoveBuffsById(buffId);
                        removed = true;
                    }

                    break;
                }

                case EnemyPassiveEffect.DebuffAllyATK:
                case EnemyPassiveEffect.DebuffAllySPD:
                {
                    FillScratchAllies();
                    for (int i = 0; i < _scratchAllies.Count; i++)
                    {
                        CharacterBall a = _scratchAllies[i];
                        if (a == null || a.BuffReceiver == null)
                            continue;
                        a.BuffReceiver.RemoveBuffsById(buffId);
                        removed = true;
                    }

                    break;
                }
            }

            if (!removed)
                return;

            string label = !string.IsNullOrEmpty(data.PassiveName) ? data.PassiveName : data.name;
            Debug.Log($"[EnemyPassiveRuntime] Stale-buff retiré : {label} sur {_owner?.name}", _owner);
        }

        /// <summary>
        /// Mapping effet → stat pour apply/remove (évite de dupliquer le switch).
        /// </summary>
        private static bool TryMapEffectToBuffStat(EnemyPassiveEffect effect, out BuffStatType stat)
        {
            switch (effect)
            {
                case EnemyPassiveEffect.BuffSelfATK:
                case EnemyPassiveEffect.BuffMateATK:
                case EnemyPassiveEffect.BuffOtherMatesATK:
                case EnemyPassiveEffect.DebuffAllyATK:
                    stat = BuffStatType.ATK;
                    return true;
                case EnemyPassiveEffect.BuffSelfDEF:
                case EnemyPassiveEffect.BuffMateDEF:
                case EnemyPassiveEffect.BuffEnemyTeamDEF:
                    stat = BuffStatType.DEF;
                    return true;
                case EnemyPassiveEffect.BuffSelfSPD:
                case EnemyPassiveEffect.DebuffAllySPD:
                    stat = BuffStatType.Speed;
                    return true;
                case EnemyPassiveEffect.BuffSelfLaunchForce:
                    stat = BuffStatType.LaunchForce;
                    return true;
                default:
                    stat = default;
                    return false;
            }
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
                    if (h is EnemyPassiveHandlerBase hb)
                        hb.OnHitByAllyWithDamage(ally, damageOrHeal);
                    else
                        h.OnHitByAlly(ally); // implémenteur direct hypothétique : comportement historique
                    break;
                default:
                    break;
            }
        }
    }
}
