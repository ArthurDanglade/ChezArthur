using System.Collections.Generic;
using ChezArthur.Gameplay;
using UnityEngine;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Spawner utilitaire pour invocations en plein combat.
    /// Fournit une API simple utilisée par certains handlers ennemis.
    /// </summary>
    public class MidCombatSpawner : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform enemyContainer;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private CombatManager combatManager;

        [Header("Pool d'ennemis invocables")]
        [SerializeField] private List<EnemyData> summonableEnemies = new List<EnemyData>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static MidCombatSpawner Instance { get; private set; }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Retourne un EnemyData depuis le pool d'ennemis
        /// invocables par son nom. Retourne null si introuvable.
        /// </summary>
        public EnemyData GetEnemyData(string enemyName)
        {
            if (string.IsNullOrEmpty(enemyName)) return null;
            for (int i = 0; i < summonableEnemies.Count; i++)
            {
                if (summonableEnemies[i] != null
                    && summonableEnemies[i].EnemyName == enemyName)
                    return summonableEnemies[i];
            }
            Debug.LogWarning(
                $"[MidCombatSpawner] EnemyData '{enemyName}' " +
                $"introuvable dans summonableEnemies.");
            return null;
        }

        /// <summary>
        /// Invoque un ennemi avec multiplicateurs HP/ATK et l'enregistre dans les managers de combat.
        /// </summary>
        public Enemy SpawnEnemy(EnemyData data, Vector3 spawnPos, float hpMult, float atkMult)
        {
            if (data == null || enemyPrefab == null)
                return null;

            Transform parent = enemyContainer != null ? enemyContainer : null;
            GameObject go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, parent);
            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy == null)
            {
                Destroy(go);
                return null;
            }

            enemy.SetData(data);
            enemy.ApplyStageScaling(hpMult, atkMult);

            if (turnManager != null)
                turnManager.AddEnemyMidCombat(enemy);
            if (combatManager != null)
                combatManager.AddEnemyToCombat(enemy);

            return enemy;
        }
    }
}
