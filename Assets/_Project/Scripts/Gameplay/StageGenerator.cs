using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Génère les ennemis d'un étage procéduralement (nombre, types, positions, scaling).
    /// </summary>
    public class StageGenerator : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float SPAWN_MARGIN = 1f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private Arena arena;
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private Transform enemyContainer;

        [Header("Prefab")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("Pool d'ennemis par type")]
        [SerializeField] private List<EnemyData> weakEnemies = new List<EnemyData>();
        [SerializeField] private List<EnemyData> standardEnemies = new List<EnemyData>();
        [SerializeField] private List<EnemyData> eliteEnemies = new List<EnemyData>();
        [SerializeField] private List<EnemyData> miniBosses = new List<EnemyData>();
        [SerializeField] private List<EnemyData> bosses = new List<EnemyData>();

        [Header("Configuration par étage")]
        [SerializeField] private int baseEnemyCount = 3;
        [SerializeField] private int maxEnemyCount = 8;
        [SerializeField] private float hpScalingPerStage = 0.1f;
        [SerializeField] private float atkScalingPerStage = 0.1f;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Génère les ennemis pour l'étage donné, les injecte dans le CombatManager et retourne la liste.
        /// </summary>
        public List<Enemy> GenerateStage(int stageNumber)
        {
            ClearStage();

            if (arena == null || combatManager == null || enemyPrefab == null || enemyContainer == null)
            {
                Debug.LogWarning("[StageGenerator] Références manquantes, génération annulée.", this);
                return new List<Enemy>();
            }

            int count = GetEnemyCountForStage(stageNumber);
            var list = new List<Enemy>(count);

            for (int i = 0; i < count; i++)
            {
                EnemyData data = GetRandomEnemyData(stageNumber);
                if (data == null) continue;

                GameObject go = Instantiate(enemyPrefab, enemyContainer);
                Vector2 pos = GetRandomSpawnPosition();
                go.transform.position = new Vector3(pos.x, pos.y, 0f);

                Enemy enemy = go.GetComponent<Enemy>();
                if (enemy == null) continue;

                enemy.SetData(data);
                ApplyScaling(enemy, stageNumber);
                list.Add(enemy);
            }

            combatManager.SetEnemies(list);
            return list;
        }

        /// <summary>
        /// Détruit tous les ennemis dans enemyContainer.
        /// </summary>
        public void ClearStage()
        {
            if (enemyContainer == null) return;
            for (int i = enemyContainer.childCount - 1; i >= 0; i--)
                Destroy(enemyContainer.GetChild(i).gameObject);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private int GetEnemyCountForStage(int stage)
        {
            int count = baseEnemyCount + (stage / 3);
            return Mathf.Min(count, maxEnemyCount);
        }

        /// <summary>
        /// Choisit un EnemyData aléatoire selon l'étage (logique simplifiée prototype).
        /// </summary>
        private EnemyData GetRandomEnemyData(int stage)
        {
            if (stage >= 10 && bosses != null && bosses.Count > 0)
                return bosses[Random.Range(0, bosses.Count)];
            if (stage >= 10 && miniBosses != null && miniBosses.Count > 0)
                return miniBosses[Random.Range(0, miniBosses.Count)];

            if (stage >= 7 && stage <= 9)
            {
                if (Random.value < 0.5f && standardEnemies != null && standardEnemies.Count > 0)
                    return standardEnemies[Random.Range(0, standardEnemies.Count)];
                if (eliteEnemies != null && eliteEnemies.Count > 0)
                    return eliteEnemies[Random.Range(0, eliteEnemies.Count)];
                if (standardEnemies != null && standardEnemies.Count > 0)
                    return standardEnemies[Random.Range(0, standardEnemies.Count)];
            }

            if (stage >= 4 && stage <= 6)
            {
                if (Random.value < 0.3f && standardEnemies != null && standardEnemies.Count > 0)
                    return standardEnemies[Random.Range(0, standardEnemies.Count)];
                if (weakEnemies != null && weakEnemies.Count > 0)
                    return weakEnemies[Random.Range(0, weakEnemies.Count)];
                if (standardEnemies != null && standardEnemies.Count > 0)
                    return standardEnemies[Random.Range(0, standardEnemies.Count)];
            }

            if (weakEnemies != null && weakEnemies.Count > 0)
                return weakEnemies[Random.Range(0, weakEnemies.Count)];
            if (standardEnemies != null && standardEnemies.Count > 0)
                return standardEnemies[Random.Range(0, standardEnemies.Count)];
            if (eliteEnemies != null && eliteEnemies.Count > 0)
                return eliteEnemies[Random.Range(0, eliteEnemies.Count)];

            return null;
        }

        /// <summary>
        /// Position aléatoire dans la moitié haute de l'arène, avec marge.
        /// </summary>
        private Vector2 GetRandomSpawnPosition()
        {
            Bounds b = arena.Bounds;
            float xMin = b.min.x + SPAWN_MARGIN;
            float xMax = b.max.x - SPAWN_MARGIN;
            float yMin = b.center.y;
            float yMax = b.max.y - SPAWN_MARGIN;
            float x = Random.Range(xMin, xMax);
            float y = Random.Range(yMin, yMax);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Applique le scaling HP/ATK selon l'étage.
        /// </summary>
        private void ApplyScaling(Enemy enemy, int stage)
        {
            float hpMult = 1f + hpScalingPerStage * (stage - 1);
            float atkMult = 1f + atkScalingPerStage * (stage - 1);
            enemy.ApplyStageScaling(hpMult, atkMult);
        }
    }
}
