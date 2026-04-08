using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Gère les conditions de victoire (tous les ennemis morts) et défaite (équipe anéantie).
    /// Victoire = victoire d'étage (noms à clarifier avec RunManager plus tard).
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static CombatManager Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private List<Enemy> enemies = new List<Enemy>();

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<Enemy> _subscribedEnemies = new List<Enemy>();
        private List<Action> _enemyDeathHandlers = new List<Action>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Nombre d'ennemis encore en vie. </summary>
        public int EnemiesAliveCount => GetEnemiesAliveCount();

        /// <summary> True si tous les ennemis sont morts. </summary>
        public bool AllEnemiesDead => EnemiesAliveCount == 0;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Remplace la liste des ennemis (ex. après génération procédurale par StageGenerator). Désabonne des anciens, s'abonne aux nouveaux.
        /// </summary>
        public void SetEnemies(List<Enemy> newEnemies)
        {
            for (int i = 0; i < _subscribedEnemies.Count && i < _enemyDeathHandlers.Count; i++)
            {
                if (_subscribedEnemies[i] != null)
                    _subscribedEnemies[i].OnDeath -= _enemyDeathHandlers[i];
            }
            _subscribedEnemies.Clear();
            _enemyDeathHandlers.Clear();

            enemies.Clear();
            if (newEnemies != null)
            {
                enemies.AddRange(newEnemies);
                for (int i = 0; i < enemies.Count; i++)
                {
                    Enemy enemy = enemies[i];
                    if (enemy == null) continue;
                    Action handler = () => HandleEnemyDeath(enemy);
                    enemy.OnDeath += handler;
                    _subscribedEnemies.Add(enemy);
                    _enemyDeathHandlers.Add(handler);
                }
            }
        }

        /// <summary>
        /// Copie la liste d'ennemis actuelle (références non nulles) pour y ajouter un invoqué puis appeler SetEnemies.
        /// </summary>
        public List<Enemy> CopyEnemyListForSetEnemies()
        {
            var copy = new List<Enemy>(enemies.Count + 1);
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                    copy.Add(enemies[i]);
            }

            return copy;
        }

        /// <summary>
        /// Ajoute un seul ennemi en cours de combat. S'abonne uniquement à cet ennemi sans toucher aux abonnements existants.
        /// </summary>
        public void AddEnemyToCombat(Enemy enemy)
        {
            if (enemy == null)
                return;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (ReferenceEquals(enemies[i], enemy))
                    return;
            }

            for (int i = 0; i < _subscribedEnemies.Count; i++)
            {
                if (ReferenceEquals(_subscribedEnemies[i], enemy))
                    return;
            }

            enemies.Add(enemy);
            Action handler = () => HandleEnemyDeath(enemy);
            enemy.OnDeath += handler;
            _subscribedEnemies.Add(enemy);
            _enemyDeathHandlers.Add(handler);
        }

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand un ennemi meurt. </summary>
        public event Action<Enemy> OnEnemyDeath;

        /// <summary> Déclenché quand tous les ennemis sont morts (victoire d'étage). </summary>
        public event Action OnVictory;

        /// <summary> Déclenché quand toute l'équipe est morte (défaite). </summary>
        public event Action OnDefeat;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            for (int i = 0; i < _subscribedEnemies.Count && i < _enemyDeathHandlers.Count; i++)
            {
                if (_subscribedEnemies[i] != null)
                    _subscribedEnemies[i].OnDeath -= _enemyDeathHandlers[i];
            }
            if (turnManager != null)
                turnManager.OnAllAlliesDead -= HandleTeamWiped;
            _subscribedEnemies.Clear();
            _enemyDeathHandlers.Clear();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Initialize()
        {
            _subscribedEnemies.Clear();
            _enemyDeathHandlers.Clear();

            if (enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    Enemy enemy = enemies[i];
                    if (enemy == null) continue;
                    Action handler = () => HandleEnemyDeath(enemy);
                    enemy.OnDeath += handler;
                    _subscribedEnemies.Add(enemy);
                    _enemyDeathHandlers.Add(handler);
                }
            }

            if (turnManager != null)
                turnManager.OnAllAlliesDead += HandleTeamWiped;
        }

        private int GetEnemiesAliveCount()
        {
            if (enemies == null) return 0;
            int count = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && !enemies[i].IsDead)
                    count++;
            }
            return count;
        }

        private void HandleEnemyDeath(Enemy enemy)
        {
            OnEnemyDeath?.Invoke(enemy);

            // Les Tals sont ajoutés dans Enemy.Die() (avec multiplicateur Client VIP)
            // Notifier tous les ennemis vivants qu'un coéquipier
            // est mort
            NotifyAllEnemyRuntimes(
                EnemyPassiveTrigger.OnMateKilled,
                mate: enemy);
            CheckVictory();
        }

        /// <summary>
        /// Notifie tous les EnemyPassiveRuntime des ennemis
        /// encore en vie d'un trigger donné.
        /// </summary>
        private void NotifyAllEnemyRuntimes(
            EnemyPassiveTrigger trigger,
            CharacterBall ally = null,
            Enemy mate = null,
            int damageOrHeal = 0)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e == null || e.IsDead) continue;
                EnemyPassiveRuntime runtime =
                    e.GetComponent<EnemyPassiveRuntime>();
                if (runtime == null) continue;
                runtime.NotifyTrigger(
                    trigger, ally, mate, damageOrHeal);
            }
        }

        public void NotifyAllyDamaged(CharacterBall ally, int damage)
        {
            NotifyAllEnemyRuntimes(
                EnemyPassiveTrigger.OnAllyDamaged,
                ally: ally,
                damageOrHeal: damage);
        }

        public void NotifyAllyKilled(CharacterBall ally)
        {
            NotifyAllEnemyRuntimes(
                EnemyPassiveTrigger.OnAllyKilled,
                ally: ally);
        }

        private void HandleTeamWiped()
        {
            Debug.Log("[CombatManager] HandleTeamWiped (OnAllAlliesDead) → CheckDefeat");
            CheckDefeat();
        }

        /// <summary>
        /// Vérifie si tous les ennemis sont morts → victoire d'étage.
        /// </summary>
        private void CheckVictory()
        {
            if (!AllEnemiesDead) return;
            if (GameManager.Instance != null)
                GameManager.Instance.Victory();
            OnVictory?.Invoke();
        }

        /// <summary>
        /// Défaite : équipe anéantie.
        /// </summary>
        private void CheckDefeat()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Defeat();
            Debug.Log("[CombatManager] CheckDefeat, invocation OnDefeat");
            OnDefeat?.Invoke();
        }
    }
}
