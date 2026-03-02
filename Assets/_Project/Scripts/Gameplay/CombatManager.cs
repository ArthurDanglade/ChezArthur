using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Enemies;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Gère les conditions de victoire (tous les ennemis morts) et défaite (équipe anéantie).
    /// Victoire = victoire d'étage (noms à clarifier avec RunManager plus tard).
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private TeamManager teamManager;
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
            Initialize();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscribedEnemies.Count && i < _enemyDeathHandlers.Count; i++)
            {
                if (_subscribedEnemies[i] != null)
                    _subscribedEnemies[i].OnDeath -= _enemyDeathHandlers[i];
            }
            if (teamManager != null)
                teamManager.OnTeamWiped -= HandleTeamWiped;
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

            if (teamManager != null)
                teamManager.OnTeamWiped += HandleTeamWiped;
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
            CheckVictory();
        }

        private void HandleTeamWiped()
        {
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
            OnDefeat?.Invoke();
        }
    }
}
