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
    /// Pont central des événements gameplay vers les valises comportementales.
    /// </summary>
    public class ValiseEventBridge : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private TurnManager turnManager;

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
        private readonly Dictionary<CharacterBall, int> _disciplineStacksByAlly = new Dictionary<CharacterBall, int>();
        private readonly Dictionary<CharacterBall, int> _cameleonStacksByAlly = new Dictionary<CharacterBall, int>();
        private int _deadSpawnedAlliesCount;
        private bool _isApplyingDefenseTransfer;
        private bool _initialized;

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
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
            UnsubscribeGlobalEvents();

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

            turnManager = tm;
            _lastSpecByAlly.Clear();
            _disciplineStacksByAlly.Clear();
            _cameleonStacksByAlly.Clear();
            _deadSpawnedAlliesCount = 0;

            if (turnManager == null)
            {
                _initialized = false;
                return;
            }

            SubscribeGlobalEvents();
            RefreshLv20Overrides();

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                    SubscribeAlly(allies[i]);
            }

            _initialized = true;
        }

        /// <summary>
        /// Notifie le début d'étage pour reset des états temporaires.
        /// </summary>
        public void NotifyStageStart()
        {
            if (ValiseManager.Instance != null &&
                ValiseManager.Instance.IsValiseActive("valise_frenesie"))
            {
                ValiseManager.Instance.ResetStacksOnValise("valise_frenesie");
                Debug.Log("[Valise] Frénésie stacks: 0 (début d'étage)");
            }
        }

        /// <summary>
        /// Notifie le début du tour d'un allié (Discipline per-personnage / Caméléon).
        /// </summary>
        public void NotifyAllyTurnStart(CharacterBall ally)
        {
            if (!_initialized || ally == null || ValiseManager.Instance == null) return;

            bool hasPreviousTurn = _lastSpecByAlly.TryGetValue(ally, out SpecializationData previousSpec);
            SpecializationData currentSpec = ally.ActiveSpec;

            if (hasPreviousTurn && ReferenceEquals(currentSpec, previousSpec) &&
                ValiseManager.Instance.IsValiseActive("valise_discipline"))
            {
                if (!_disciplineStacksByAlly.TryGetValue(ally, out int stacks))
                    stacks = 0;
                stacks++;
                _disciplineStacksByAlly[ally] = stacks;
                ApplyDisciplinePersonalModifiers(ally, stacks);
            }

            if (hasPreviousTurn && !ReferenceEquals(currentSpec, previousSpec) &&
                ValiseManager.Instance.IsValiseActive("valise_cameleon"))
            {
                if (!_cameleonStacksByAlly.TryGetValue(ally, out int stacks))
                    stacks = 0;
                stacks++;
                ValiseInstance cameleon = ValiseManager.Instance.GetActiveValise("valise_cameleon");
                if (cameleon != null && cameleon.IsLevel20Unlocked)
                    stacks++;
                _cameleonStacksByAlly[ally] = stacks;
                ApplyCameleonPersonalModifiers(ally, stacks);
            }

            _lastSpecByAlly[ally] = currentSpec;
        }

        /// <summary>
        /// Tente le renvoi de dégâts depuis l'ennemi attaquant vers la victime (appelé par Enemy).
        /// </summary>
        public void TryRenvoiFromEnemyAttack(Enemy attacker, CharacterBall victim, int damageReceived)
        {
            if (!_initialized || attacker == null || victim == null || ValiseManager.Instance == null) return;
            if (damageReceived <= 0) return;
            if (!ValiseManager.Instance.IsValiseActive("valise_renvoi")) return;

            ValiseInstance renvoi = ValiseManager.Instance.GetActiveValise("valise_renvoi");
            if (renvoi == null) return;

            int renvoiDamage = Mathf.RoundToInt(damageReceived * renvoi.GetTotalStatValue());
            if (renvoiDamage <= 0) return;

            if (renvoi.IsLevel20Unlocked)
            {
                if (turnManager == null) return;
                IReadOnlyList<ITurnParticipant> participants = turnManager.Participants;
                if (participants == null) return;

                for (int i = 0; i < participants.Count; i++)
                {
                    ITurnParticipant participant = participants[i];
                    if (participant == null || participant.IsAlly || participant.IsDead) continue;
                    Enemy enemy = participant as Enemy;
                    if (enemy == null || enemy.IsDead) continue;
                    enemy.TakePureDamage(renvoiDamage);
                }

                Debug.Log($"[Valise] Renvoi : {renvoiDamage} dégâts renvoyés");
                return;
            }

            if (attacker.IsDead) return;

            attacker.TakePureDamage(renvoiDamage);
            Debug.Log($"[Valise] Renvoi : {renvoiDamage} dégâts renvoyés");

            // Synergie Vol de Vie + Renvoi : le renvoi soigne l'allié frappé.
            if (ValiseManager.Instance.IsValiseActive("valise_vol_de_vie"))
                victim.Heal(renvoiDamage);
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
            _disciplineStacksByAlly.Clear();
            _cameleonStacksByAlly.Clear();
        }

        private void OnAllyKill(CharacterBall ally)
        {
            if (!_initialized || ally == null || ValiseManager.Instance == null) return;

            if (ValiseManager.Instance.IsValiseActive("valise_frenesie"))
            {
                ValiseManager.Instance.AddStackToValise("valise_frenesie");
                ValiseInstance frenesie = ValiseManager.Instance.GetActiveValise("valise_frenesie");
                if (frenesie != null)
                    Debug.Log($"[Valise] Frénésie stacks: {frenesie.InternalStacks}");
            }
        }

        private void OnAllyDeath(CharacterBall ally)
        {
            if (!_initialized || ally == null) return;

            // Stacks = alliés spawnés puis morts (slots d'équipe vides exclus).
            _deadSpawnedAlliesCount++;

            if (ValiseManager.Instance != null &&
                ValiseManager.Instance.IsValiseActive("valise_dernier_debout"))
            {
                SyncStacksToTarget("valise_dernier_debout", _deadSpawnedAlliesCount);
                Debug.Log($"[Valise] Dernier Debout stacks: {_deadSpawnedAlliesCount}");
            }
        }

        private void OnAllyTakeDamage(CharacterBall ally, int damage)
        {
            if (!_initialized || ValiseManager.Instance == null) return;

            if (ally.LastDamageWasContact)
                return;

            // Effet niv20 Valise Défense : transfert partiel des dégâts.
            HandleDefenseLv20(ally, damage);
        }

        private void OnTalsChanged(int newTotal)
        {
            if (!_initialized || ValiseManager.Instance == null) return;
            if (!ValiseManager.Instance.IsValiseActive("valise_fortune")) return;

            ValiseInstance fortune = ValiseManager.Instance.GetActiveValise("valise_fortune");
            if (fortune == null) return;

            int threshold = fortune.IsLevel20Unlocked ? 40 : 50;
            int targetStacks = threshold > 0 ? newTotal / threshold : 0;
            SyncStacksToTarget("valise_fortune", targetStacks);
        }

        private void OnItemChanged(ItemInstance instance)
        {
            if (!_initialized || ValiseManager.Instance == null) return;
            if (!ValiseManager.Instance.IsValiseActive("valise_equilibre")) return;
            if (ItemManager.Instance == null) return;

            IReadOnlyList<ItemInstance> items = ItemManager.Instance.GetActiveSlots();
            int targetStacks = items != null ? items.Count : 0;
            SyncStacksToTarget("valise_equilibre", targetStacks);
        }

        private void OnAllyHitEnemy(CharacterBall ally, Enemy enemy, int damageDealt)
        {
            if (!_initialized || ally == null || enemy == null) return;
            if (ValiseManager.Instance == null || turnManager == null) return;
            if (!ValiseManager.Instance.IsValiseActive("valise_vol_de_vie")) return;

            ValiseInstance volDeVie = ValiseManager.Instance.GetActiveValise("valise_vol_de_vie");
            if (volDeVie == null) return;

            int healAmount = Mathf.RoundToInt(damageDealt * volDeVie.GetTotalStatValue());
            if (healAmount <= 0) return;

            if (volDeVie.IsLevel20Unlocked)
            {
                IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
                if (allies == null) return;

                for (int i = 0; i < allies.Count; i++)
                {
                    CharacterBall target = allies[i];
                    if (target == null || target.IsDead) continue;
                    target.Heal(healAmount);
                }
                return;
            }

            ally.Heal(healAmount);
        }

        private void OnAllyCrit(CharacterBall ally, Enemy enemy, int damage)
        {
            if (!_initialized || ValiseManager.Instance == null) return;

            // Synergie Critique + Frénésie : chaque critique ajoute un stack Frénésie.
            if (ValiseManager.Instance.IsValiseActive("valise_critique") &&
                ValiseManager.Instance.IsValiseActive("valise_frenesie"))
            {
                ValiseManager.Instance.AddStackToValise("valise_frenesie");
            }
        }

        private void OnValiseStatsChanged(ValiseInstance instance)
        {
            if (!_initialized || turnManager == null) return;

            if (instance != null && instance.Data != null &&
                instance.Data.Id == "valise_dernier_debout")
            {
                SyncStacksToTarget("valise_dernier_debout", _deadSpawnedAlliesCount);
            }

            if (instance != null && instance.Data != null &&
                instance.Data.Id == "valise_discipline")
            {
                RefreshAllDisciplinePersonalModifiers();
            }

            if (instance != null && instance.Data != null &&
                instance.Data.Id == "valise_cameleon")
            {
                RefreshAllCameleonPersonalModifiers();
            }

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
            RefreshLv20Overrides();
        }

        private void ApplyDisciplinePersonalModifiers(CharacterBall ally, int stacks)
        {
            if (ally == null || ValiseManager.Instance == null) return;

            ValiseInstance discipline = ValiseManager.Instance.GetActiveValise("valise_discipline");
            if (discipline == null) return;

            float bonusPercent = stacks * discipline.AccumulatedValue;
            ally.SetPersonalDisciplineStacks(stacks);
            ally.SetPersonalValiseModifier(ValiseStatType.ATK, bonusPercent);
            ally.SetPersonalValiseModifier(ValiseStatType.DEF, bonusPercent);
            Debug.Log($"[Valise] Discipline {ally.Name} stacks: {stacks}");
        }

        private void RefreshAllDisciplinePersonalModifiers()
        {
            if (turnManager == null || ValiseManager.Instance == null) return;
            if (!ValiseManager.Instance.IsValiseActive("valise_discipline")) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                if (!_disciplineStacksByAlly.TryGetValue(ally, out int stacks) || stacks <= 0)
                    continue;
                ApplyDisciplinePersonalModifiers(ally, stacks);
            }
        }

        private void ApplyCameleonPersonalModifiers(CharacterBall ally, int stacks)
        {
            if (ally == null || ValiseManager.Instance == null) return;

            ValiseInstance cameleon = ValiseManager.Instance.GetActiveValise("valise_cameleon");
            if (cameleon == null) return;

            float bonusPercent = stacks * cameleon.AccumulatedValue;
            ally.SetPersonalValiseModifier(ValiseStatType.ATK, bonusPercent);
            ally.SetPersonalValiseModifier(ValiseStatType.DEF, bonusPercent);
            Debug.Log($"[Valise] Caméléon {ally.Name} stacks: {stacks}");
        }

        private void RefreshAllCameleonPersonalModifiers()
        {
            if (turnManager == null || ValiseManager.Instance == null) return;
            if (!ValiseManager.Instance.IsValiseActive("valise_cameleon")) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                if (!_cameleonStacksByAlly.TryGetValue(ally, out int stacks) || stacks <= 0)
                    continue;
                ApplyCameleonPersonalModifiers(ally, stacks);
            }
        }

        private void SyncStacksToTarget(string valiseId, int targetStacks)
        {
            if (ValiseManager.Instance == null) return;

            ValiseInstance instance = ValiseManager.Instance.GetActiveValise(valiseId);
            if (instance == null) return;

            if (targetStacks < 0) targetStacks = 0;
            int currentStacks = instance.InternalStacks;
            int delta = targetStacks - currentStacks;

            if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                    ValiseManager.Instance.AddStackToValise(valiseId);
                return;
            }

            if (delta < 0)
            {
                ValiseManager.Instance.ResetStacksOnValise(valiseId);
                for (int i = 0; i < targetStacks; i++)
                    ValiseManager.Instance.AddStackToValise(valiseId);
            }
        }

        private float ComputeTotalTeamMaxHp()
        {
            if (turnManager == null) return 0f;
            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return 0f;

            float total = 0f;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null) continue;
                total += ally.MaxHp;
            }
            return total;
        }

        private void HandleDefenseLv20(CharacterBall victim, int damage)
        {
            if (_isApplyingDefenseTransfer) return;
            if (ValiseManager.Instance == null || turnManager == null) return;
            if (!ValiseManager.Instance.IsValiseActive("valise_defense")) return;

            ValiseInstance defense = ValiseManager.Instance.GetActiveValise("valise_defense");
            if (defense == null || !defense.IsLevel20Unlocked) return;

            int absorbed = Mathf.RoundToInt(damage * 0.10f);
            if (absorbed <= 0) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            _isApplyingDefenseTransfer = true;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally == victim) continue;
                if (ally.Data == null || ally.Data.Role != CharacterRole.Defender) continue;
                ally.TakeDamage(absorbed);
            }
            _isApplyingDefenseTransfer = false;
        }

        /// <summary>
        /// Applique les overrides de valeur par niveau pour les effets niveau 20.
        /// </summary>
        private void RefreshLv20Overrides()
        {
            ApplyDernierDeboutLv20Override();
            ApplyEquilibreLv20Override();
        }

        private void ApplyDernierDeboutLv20Override()
        {
            if (ValiseManager.Instance == null) return;
            ValiseInstance instance = ValiseManager.Instance.GetActiveValise("valise_dernier_debout");
            if (instance == null) return;

            if (instance.IsLevel20Unlocked)
                instance.SetValuePerLevelOverride(0.04f);
            else
                instance.ClearValuePerLevelOverride();
        }

        private void ApplyEquilibreLv20Override()
        {
            if (ValiseManager.Instance == null) return;
            ValiseInstance instance = ValiseManager.Instance.GetActiveValise("valise_equilibre");
            if (instance == null) return;

            if (instance.IsLevel20Unlocked)
                instance.SetValuePerLevelOverride(0.015f);
            else
                instance.ClearValuePerLevelOverride();
        }
    }
}
