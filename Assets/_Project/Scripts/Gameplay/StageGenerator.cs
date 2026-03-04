using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Roguelike;
using ChezArthur.UI;

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
        private const int MILESTONE_INTERVAL = 10;
        private const int SPECIAL_ROOM_INTERVAL = 5;
        private const int HORDE_ENEMY_COUNT = 8;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private Arena arena;
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private TurnManager turnManager;
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

        [Header("Salles spéciales")]
        [SerializeField] private SpecialRoomManager specialRoomManager;

        [Header("Données ennemis Milestone")]
        [SerializeField] private EnemyData bossData;
        [SerializeField] private EnemyData miniBossData;
        [SerializeField] private EnemyData hordeEnemyData;

        [Header("Probabilités Milestone")]
        [SerializeField] [Range(0f, 1f)] private float bossClassicChance = 0.50f;
        [SerializeField] [Range(0f, 1f)] private float miniBossDuoChance = 0.20f;
        [SerializeField] [Range(0f, 1f)] private float hordeChance = 0.15f;

        [Header("UI")]
        [SerializeField] private StageAnnouncerUI stageAnnouncerUI;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<Enemy> _currentEnemies = new List<Enemy>();

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Génère les ennemis pour l'étage donné, les injecte dans le CombatManager et retourne la liste.
        /// Étages 10, 20, 30... = milestones ; 5, 15, 25... = salles spéciales.
        /// </summary>
        public List<Enemy> GenerateStage(int stageNumber)
        {
            ClearStage();
            _currentEnemies.Clear();

            if (arena == null || combatManager == null || enemyPrefab == null || enemyContainer == null)
            {
                Debug.LogWarning("[StageGenerator] Références manquantes, génération annulée.", this);
                return new List<Enemy>(_currentEnemies);
            }

            bool isMilestone = stageNumber > 0 && stageNumber % MILESTONE_INTERVAL == 0;
            bool isSpecialRoom = !isMilestone && stageNumber > 0 && stageNumber % SPECIAL_ROOM_INTERVAL == 0;

            // Gère le modificateur de salle
            if (specialRoomManager != null)
            {
                if (isSpecialRoom)
                {
                    SpecialRoomType roomType = GetRandomSpecialRoomType();
                    specialRoomManager.SetSpecialRoom(roomType);
                }
                else
                {
                    specialRoomManager.ClearSpecialRoom();
                }
            }

            // Génère selon le type d'étage
            if (isMilestone)
            {
                MilestoneType milestoneType = GetRandomMilestoneType();
                GenerateMilestoneStage(stageNumber, milestoneType);
            }
            else if (isSpecialRoom && specialRoomManager != null && specialRoomManager.IsClientVIP)
            {
                GenerateClientVIPStage(stageNumber);
            }
            else
            {
                GenerateNormalStage(stageNumber);
            }

            return new List<Enemy>(_currentEnemies);
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

        /// <summary>
        /// Tire aléatoirement un type de salle spéciale.
        /// </summary>
        private SpecialRoomType GetRandomSpecialRoomType()
        {
            SpecialRoomType[] availableTypes = new SpecialRoomType[]
            {
                SpecialRoomType.HappyHour,
                SpecialRoomType.Horde,
                SpecialRoomType.ClientVIP
            };

            return availableTypes[Random.Range(0, availableTypes.Length)];
        }

        /// <summary>
        /// Tire aléatoirement le type de milestone selon les probabilités.
        /// </summary>
        private MilestoneType GetRandomMilestoneType()
        {
            float roll = Random.value;

            if (roll < bossClassicChance)
                return MilestoneType.BossClassic;

            roll -= bossClassicChance;
            if (roll < miniBossDuoChance)
                return MilestoneType.MiniBossDuo;

            roll -= miniBossDuoChance;
            if (roll < hordeChance)
                return MilestoneType.Horde;

            return MilestoneType.BossWithRoom;
        }

        /// <summary>
        /// Génère un étage Milestone selon le type et enregistre dans les managers.
        /// </summary>
        private void GenerateMilestoneStage(int stageNumber, MilestoneType milestoneType)
        {
            Debug.Log($"[StageGenerator] GenerateMilestoneStage - stageAnnouncerUI est {(stageAnnouncerUI != null ? "présent" : "NULL")}");

            // Annonce le boss
            if (stageAnnouncerUI != null)
            {
                string title = milestoneType == MilestoneType.Horde ? "HORDE !" : "BOSS FIGHT";
                Debug.Log($"[StageGenerator] Appel ShowBossAnnounce avec titre : {title}");
                stageAnnouncerUI.ShowBossAnnounce(title);
            }
            else
            {
                Debug.LogWarning("[StageGenerator] stageAnnouncerUI est NULL, impossible d'afficher le bandeau boss.");
            }

            switch (milestoneType)
            {
                case MilestoneType.BossClassic:
                case MilestoneType.BossWithRoom:
                    GenerateBossStage(stageNumber);
                    break;
                case MilestoneType.MiniBossDuo:
                    GenerateMiniBossDuoStage(stageNumber);
                    break;
                case MilestoneType.Horde:
                    GenerateHordeStage(stageNumber);
                    break;
            }

            if (combatManager != null)
                combatManager.SetEnemies(_currentEnemies);

            if (turnManager != null)
            {
                turnManager.ClearEnemies();
                turnManager.AddEnemies(_currentEnemies);
            }
        }

        /// <summary>
        /// Génère un étage avec 1 boss au centre.
        /// </summary>
        private void GenerateBossStage(int stageNumber)
        {
            if (bossData == null)
            {
                Debug.LogWarning("[StageGenerator] bossData non assigné, fallback sur étage normal.", this);
                GenerateNormalStage(stageNumber);
                return;
            }

            Vector2 bossPosition = new Vector2(0f, 3f);
            Enemy boss = SpawnEnemy(bossData, bossPosition, stageNumber);
            if (boss != null)
            {
                _currentEnemies.Add(boss);
                boss.transform.localScale = Vector3.one * 1.5f;
            }
        }

        /// <summary>
        /// Génère un étage avec 2 mini-boss espacés.
        /// </summary>
        private void GenerateMiniBossDuoStage(int stageNumber)
        {
            if (miniBossData == null)
            {
                Debug.LogWarning("[StageGenerator] miniBossData non assigné, fallback sur boss classique.", this);
                GenerateBossStage(stageNumber);
                return;
            }

            Vector2[] positions = new Vector2[]
            {
                new Vector2(-2f, 3f),
                new Vector2(2f, 3f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                Enemy miniBoss = SpawnEnemy(miniBossData, positions[i], stageNumber);
                if (miniBoss != null)
                {
                    _currentEnemies.Add(miniBoss);
                    miniBoss.transform.localScale = Vector3.one * 1.2f;
                }
            }
        }

        /// <summary>
        /// Génère un étage avec beaucoup d'ennemis faibles.
        /// </summary>
        private void GenerateHordeStage(int stageNumber)
        {
            EnemyData hordeData = hordeEnemyData != null ? hordeEnemyData : GetRandomEnemyData(stageNumber);

            if (hordeData == null)
            {
                Debug.LogWarning("[StageGenerator] Aucun ennemi disponible pour la horde.", this);
                return;
            }

            Vector2[] positions = new Vector2[]
            {
                new Vector2(-3f, 5f), new Vector2(-1f, 5f), new Vector2(1f, 5f), new Vector2(3f, 5f),
                new Vector2(-2f, 3f), new Vector2(0f, 3f), new Vector2(2f, 3f),
                new Vector2(-1f, 1f), new Vector2(1f, 1f)
            };

            int count = Mathf.Min(HORDE_ENEMY_COUNT, positions.Length);

            for (int i = 0; i < count; i++)
            {
                Enemy enemy = SpawnEnemy(hordeData, positions[i], stageNumber);
                if (enemy != null)
                {
                    _currentEnemies.Add(enemy);
                    enemy.transform.localScale = Vector3.one * 0.7f;
                }
            }
        }

        /// <summary>
        /// Instancie un ennemi à la position donnée avec données et scaling, sans l'ajouter à _currentEnemies.
        /// </summary>
        private Enemy SpawnEnemy(EnemyData data, Vector2 position, int stageNumber)
        {
            if (data == null || enemyPrefab == null || enemyContainer == null) return null;

            GameObject go = Instantiate(enemyPrefab, enemyContainer);
            go.transform.position = new Vector3(position.x, position.y, 0f);

            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy == null) return null;

            enemy.SetData(data);
            ApplyScaling(enemy, stageNumber);
            return enemy;
        }

        /// <summary>
        /// Génère un étage Client VIP : 1 seul mini-boss.
        /// </summary>
        private void GenerateClientVIPStage(int stageNumber)
        {
            EnemyData vipData = miniBossData != null ? miniBossData : bossData;

            if (vipData == null)
            {
                Debug.LogWarning("[StageGenerator] Pas de données pour Client VIP, fallback normal.", this);
                GenerateNormalStage(stageNumber);
                return;
            }

            Vector2 vipPosition = new Vector2(0f, 3f);
            Enemy vip = SpawnEnemy(vipData, vipPosition, stageNumber);

            if (vip != null)
            {
                _currentEnemies.Add(vip);
                vip.transform.localScale = Vector3.one * 1.3f;
            }

            if (combatManager != null)
                combatManager.SetEnemies(_currentEnemies);

            if (turnManager != null)
            {
                turnManager.ClearEnemies();
                turnManager.AddEnemies(_currentEnemies);
            }
        }

        /// <summary>
        /// Génère un étage normal (non milestone) et enregistre dans les managers.
        /// </summary>
        private void GenerateNormalStage(int stageNumber)
        {
            int count = GetEnemyCountForStage(stageNumber);

            // Bonus d'ennemis si salle Horde
            if (specialRoomManager != null)
                count += specialRoomManager.ExtraEnemyCount;

            for (int i = 0; i < count; i++)
            {
                EnemyData data = GetRandomEnemyData(stageNumber);
                if (data == null) continue;

                Vector2 pos = GetRandomSpawnPosition();
                Enemy enemy = SpawnEnemy(data, pos, stageNumber);
                if (enemy != null)
                    _currentEnemies.Add(enemy);
            }

            if (combatManager != null)
                combatManager.SetEnemies(_currentEnemies);

            if (turnManager != null)
            {
                turnManager.ClearEnemies();
                turnManager.AddEnemies(_currentEnemies);
            }
        }

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
