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
        private readonly Dictionary<CharacterBall, Action> _allyLaunchHandlers = new Dictionary<CharacterBall, Action>();
        private readonly Dictionary<CharacterBall, Action<Enemy>> _allyHitEnemyRefHandlers = new Dictionary<CharacterBall, Action<Enemy>>();
        private readonly Dictionary<CharacterBall, Action<Enemy, int>> _allyCritValiseHandlers = new Dictionary<CharacterBall, Action<Enemy, int>>();
        private readonly Dictionary<CharacterBall, SpecializationData> _lastSpecByAlly = new Dictionary<CharacterBall, SpecializationData>();
        private int _consecutiveKillsWithoutDamage;
        private bool _tookDamageThisTurn;
        private float _cumulativeDamageTaken;
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
            _consecutiveKillsWithoutDamage = 0;
            _tookDamageThisTurn = false;
            _cumulativeDamageTaken = 0f;

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
            _tookDamageThisTurn = false;
            _consecutiveKillsWithoutDamage = 0;
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
                ValiseManager.Instance.OnValiseUpgradedWithRarity += OnValiseUpgradedWithRarity;
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
                ValiseManager.Instance.OnValiseUpgradedWithRarity -= OnValiseUpgradedWithRarity;
        }

        private void SubscribeAlly(CharacterBall ally)
        {
            if (ally == null) return;
            if (_allyDeathHandlers.ContainsKey(ally)) return;

            Action deathHandler = () => OnAllyDeath(ally);
            Action killHandler = () => OnAllyKill(ally);
            Action<int> damagedHandler = (damage) => OnAllyTakeDamage(ally, damage);
            Action launchHandler = () => OnAllyLaunched(ally);
            Action<Enemy> hitEnemyHandler = (enemy) => OnAllyHitEnemy(ally, enemy);
            Action<Enemy, int> critValiseHandler = (enemy, dmg) => OnAllyCrit(ally, enemy, dmg);

            _allyDeathHandlers[ally] = deathHandler;
            _allyKillHandlers[ally] = killHandler;
            _allyDamagedHandlers[ally] = damagedHandler;
            _allyLaunchHandlers[ally] = launchHandler;
            _allyHitEnemyRefHandlers[ally] = hitEnemyHandler;
            _allyCritValiseHandlers[ally] = critValiseHandler;
            _lastSpecByAlly[ally] = ally.ActiveSpec;
            _subscribedAllies.Add(ally);

            ally.OnDeath += deathHandler;
            ally.OnKillEnemy += killHandler;
            ally.OnDamaged += damagedHandler;
            ally.OnLaunched += launchHandler;
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
                if (_allyLaunchHandlers.TryGetValue(ally, out Action launchHandler))
                    ally.OnLaunched -= launchHandler;
                if (_allyHitEnemyRefHandlers.TryGetValue(ally, out Action<Enemy> hitEnemyHandler))
                    ally.OnHitEnemyWithRef -= hitEnemyHandler;
                if (_allyCritValiseHandlers.TryGetValue(ally, out Action<Enemy, int> critValiseHandler))
                    ally.OnCriticalHit -= critValiseHandler;
            }

            _subscribedAllies.Clear();
            _allyDeathHandlers.Clear();
            _allyKillHandlers.Clear();
            _allyDamagedHandlers.Clear();
            _allyLaunchHandlers.Clear();
            _allyHitEnemyRefHandlers.Clear();
            _allyCritValiseHandlers.Clear();
            _lastSpecByAlly.Clear();
        }

        private void OnAllyKill(CharacterBall ally)
        {
            if (!_initialized || ally == null || ValiseManager.Instance == null) return;

            if (ValiseManager.Instance.IsValiseActive("valise_frenesie"))
                ValiseManager.Instance.AddStackToValise("valise_frenesie");

            if (ValiseManager.Instance.IsValiseActive("valise_momentum"))
            {
                if (_tookDamageThisTurn) return;
                _consecutiveKillsWithoutDamage++;
                ValiseManager.Instance.AddStackToValise("valise_momentum");
            }
        }

        private void OnAllyDeath(CharacterBall ally)
        {
            if (!_initialized || ValiseManager.Instance == null) return;

            if (ValiseManager.Instance.IsValiseActive("valise_frenesie"))
                ValiseManager.Instance.ResetStacksOnValise("valise_frenesie");

            if (ValiseManager.Instance.IsValiseActive("valise_dernier_debout"))
                ValiseManager.Instance.AddStackToValise("valise_dernier_debout");
        }

        private void OnAllyTakeDamage(CharacterBall ally, int damage)
        {
            if (!_initialized || ValiseManager.Instance == null) return;

            _tookDamageThisTurn = true;
            if (ValiseManager.Instance.IsValiseActive("valise_momentum"))
            {
                ValiseManager.Instance.ResetStacksOnValise("valise_momentum");
                _consecutiveKillsWithoutDamage = 0;
            }

            if (ValiseManager.Instance.IsValiseActive("valise_cicatrice"))
            {
                _cumulativeDamageTaken += damage;
                float totalMaxHp = ComputeTotalTeamMaxHp();
                if (totalMaxHp > 0f)
                {
                    int targetStacks = Mathf.FloorToInt(_cumulativeDamageTaken / (totalMaxHp * 0.10f));
                    SyncStacksToTarget("valise_cicatrice", targetStacks);
                }
            }

            // Effet Renvoi (base + niv20).
            HandleRenvoi(ally, damage);
            // Effet niv20 Valise Défense : transfert partiel des dégâts.
            HandleDefenseLv20(ally, damage);
        }

        private void OnAllyLaunched(CharacterBall ally)
        {
            if (!_initialized || ally == null || ValiseManager.Instance == null) return;

            _tookDamageThisTurn = false;

            SpecializationData previousSpec = null;
            _lastSpecByAlly.TryGetValue(ally, out previousSpec);
            bool hasSwitchedSpec = !ReferenceEquals(ally.ActiveSpec, previousSpec);

            if (ValiseManager.Instance.IsValiseActive("valise_cameleon") && hasSwitchedSpec)
            {
                ValiseManager.Instance.AddStackToValise("valise_cameleon");
                ValiseInstance cameleon = ValiseManager.Instance.GetActiveValise("valise_cameleon");
                if (cameleon != null && cameleon.IsLevel20Unlocked)
                    ValiseManager.Instance.AddStackToValise("valise_cameleon");
            }

            if (ValiseManager.Instance.IsValiseActive("valise_discipline"))
            {
                bool isSrAlly = ally.Data != null && ally.Data.Rarity == CharacterRarity.SR;
                bool sameSpec = ReferenceEquals(ally.ActiveSpec, previousSpec);
                if (sameSpec || isSrAlly)
                    ValiseManager.Instance.AddStackToValise("valise_discipline");
            }

            _lastSpecByAlly[ally] = ally.ActiveSpec;
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

        private void OnAllyHitEnemy(CharacterBall ally, Enemy enemy)
        {
            if (!_initialized || ally == null || enemy == null) return;
            if (ValiseManager.Instance == null || turnManager == null) return;
            if (!ValiseManager.Instance.IsValiseActive("valise_vol_de_vie")) return;

            ValiseInstance volDeVie = ValiseManager.Instance.GetActiveValise("valise_vol_de_vie");
            if (volDeVie == null) return;

            // TODO : remplacer l'approximation par les dégâts réels quand l'event les exposera.
            int healAmount = Mathf.RoundToInt(ally.EffectiveAtk * volDeVie.GetTotalStatValue());
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

        private void OnValiseUpgradedWithRarity(ValiseInstance instance, ValiseImprovementRarity rarity)
        {
            if (!_initialized || ValiseManager.Instance == null) return;
            if (ValiseManager.Instance.IsValiseActive("valise_interet_compose") &&
                (rarity == ValiseImprovementRarity.Epique || rarity == ValiseImprovementRarity.Legendaire))
            {
                ValiseManager.Instance.AddStackToValise("valise_interet_compose");
            }
            RefreshLv20Overrides();
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

        private void HandleRenvoi(CharacterBall ally, int damage)
        {
            if (ValiseManager.Instance == null || turnManager == null) return;
            if (!ValiseManager.Instance.IsValiseActive("valise_renvoi")) return;

            ValiseInstance renvoi = ValiseManager.Instance.GetActiveValise("valise_renvoi");
            if (renvoi == null) return;

            int renvoiDamage = Mathf.RoundToInt(damage * renvoi.GetTotalStatValue());
            if (renvoiDamage <= 0) return;

            ITurnParticipant currentParticipant = turnManager.CurrentParticipant;
            if (currentParticipant == null || currentParticipant.IsAlly) return;

            Enemy attacker = currentParticipant as Enemy;
            if (attacker == null || attacker.IsDead) return;

            if (renvoi.IsLevel20Unlocked)
            {
                IReadOnlyList<ITurnParticipant> participants = turnManager.Participants;
                if (participants == null) return;

                for (int i = 0; i < participants.Count; i++)
                {
                    ITurnParticipant participant = participants[i];
                    if (participant == null || participant.IsAlly || participant.IsDead) continue;
                    Enemy enemy = participant as Enemy;
                    if (enemy == null || enemy.IsDead) continue;
                    enemy.TakeDamage(renvoiDamage);
                }
                return;
            }

            attacker.TakeDamage(renvoiDamage);
            // Synergie Vol de Vie + Renvoi : le renvoi soigne l'allié frappé.
            if (ValiseManager.Instance.IsValiseActive("valise_vol_de_vie"))
                ally.Heal(renvoiDamage);
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
            ApplyInteretComposeLv20Override();
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

        private void ApplyInteretComposeLv20Override()
        {
            if (ValiseManager.Instance == null) return;
            ValiseInstance instance = ValiseManager.Instance.GetActiveValise("valise_interet_compose");
            if (instance == null) return;

            if (instance.IsLevel20Unlocked)
                instance.SetValuePerLevelOverride(0.0035f);
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
