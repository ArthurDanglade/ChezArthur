using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Roguelike;
using ChezArthur.UI;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Génère les ennemis d'un étage procéduralement (univers, rôles, salles spéciales, post-100).
    /// </summary>
    public class StageGenerator : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float SPAWN_MARGIN = 1f;
        private const int UNIVERSE_SIZE = 20;
        private const int BOSS_INTERVAL = 10;
        private const int MINIBOSS_INTERVAL = 5;
        private const int POST_GAME_START = 101;
        private const float HORDE_SCALE_REDUCTION = 0.80f;
        private const int HORDE_EXTRA_ENEMIES = 3;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private Arena arena;
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private Transform enemyContainer;

        [Header("Arène dynamique")]
        [SerializeField] private ArenaBackground arenaBackground;
        [SerializeField] private ArenaCamera arenaCamera;

        [Header("Prefab")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("Pool global ennemis")]
        [SerializeField] private List<EnemyData> allEnemies = new List<EnemyData>();

        [Header("Configuration difficulté")]
        [SerializeField] private float[] hpScalingPerStageByUniverse =
            { 0.08f, 0.10f, 0.12f, 0.14f, 0.16f };
        [SerializeField] private float[] atkScalingPerStageByUniverse =
            { 0.06f, 0.08f, 0.10f, 0.12f, 0.14f };

        [Header("Configuration spawn")]
        [SerializeField] private Vector2Int[] enemyCountRangeByBlock =
        {
            new Vector2Int(2, 4),
            new Vector2Int(3, 4),
            new Vector2Int(3, 4),
            new Vector2Int(4, 5)
        };
        [SerializeField] [Range(0f, 1f)] private float specialRoomChance = 0.25f;

        [Header("Salles spéciales")]
        [SerializeField] private SpecialRoomManager specialRoomManager;

        [Header("UI")]
        [SerializeField] private StageAnnouncerUI stageAnnouncerUI;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<Enemy> _currentEnemies = new List<Enemy>();
        private bool _specialRoomUsedInCurrentBlock;
        private int _lastBlockIndex = -1;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Génère les ennemis pour l'étage donné, les injecte dans le CombatManager et retourne la liste.
        /// </summary>
        public List<Enemy> GenerateStage(int stageNumber)
        {
            if (arenaCamera != null)
                arenaCamera.ApplyRandomSize();

            int universNumber = GetUniversNumber(stageNumber);
            if (arenaBackground != null && arena != null)
            {
                arenaBackground.SetUnivers(universNumber);
                arenaBackground.FitToBounds(arena.Bounds);
            }

            ClearStage();
            _currentEnemies.Clear();

            if (arena == null || combatManager == null || enemyPrefab == null || enemyContainer == null)
            {
                Debug.LogWarning("[StageGenerator] Références manquantes, génération annulée.", this);
                return new List<Enemy>(_currentEnemies);
            }

            int localStage = ((stageNumber - 1) % UNIVERSE_SIZE) + 1;
            int universeIndex = Mathf.Min((stageNumber - 1) / UNIVERSE_SIZE + 1, 5);

            bool isBoss = stageNumber < POST_GAME_START &&
                          (localStage == BOSS_INTERVAL || localStage == UNIVERSE_SIZE);
            bool isMiniBoss = stageNumber < POST_GAME_START &&
                              (localStage == MINIBOSS_INTERVAL || localStage == MINIBOSS_INTERVAL * 3);
            bool isPostGame = stageNumber >= POST_GAME_START;

            int blockIndex = GetBlockIndex(localStage);
            if (blockIndex != _lastBlockIndex)
            {
                _specialRoomUsedInCurrentBlock = false;
                _lastBlockIndex = blockIndex;
            }

            if (isPostGame)
                GeneratePostGameStage(stageNumber);
            else if (isBoss)
                GenerateBossStage(stageNumber, universeIndex, isMajorBoss: localStage == UNIVERSE_SIZE);
            else if (isMiniBoss)
                GenerateMiniBossStage(stageNumber, universeIndex, isMajorMiniBoss: localStage == 15);
            else
                GenerateNormalStage(stageNumber, localStage, universeIndex);

            return new List<Enemy>(_currentEnemies);
        }

        /// <summary>
        /// Détruit tous les ennemis dans enemyContainer.
        /// </summary>
        public void ClearStage()
        {
            if (enemyContainer == null) return;
            for (int i = enemyContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = enemyContainer.GetChild(i);

                EnemyPassiveRuntime runtime =
                    child.GetComponent<EnemyPassiveRuntime>();
                if (runtime != null)
                    runtime.Cleanup();

                EnemyShieldSystem shield =
                    child.GetComponent<EnemyShieldSystem>();
                if (shield != null)
                    shield.Cleanup();

                Destroy(child.gameObject);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — GÉNÉRATION
        // ═══════════════════════════════════════════

        private void GenerateNormalStage(int stageNumber, int localStage, int universeIndex)
        {
            bool isSpecial = false;
            SpecialRoomType specialType = SpecialRoomType.HappyHour;

            if (!_specialRoomUsedInCurrentBlock && Random.value < specialRoomChance)
            {
                isSpecial = true;
                _specialRoomUsedInCurrentBlock = true;
                specialType = Random.value < 0.5f
                    ? SpecialRoomType.HappyHour
                    : SpecialRoomType.Horde;
            }

            if (specialRoomManager != null)
            {
                if (isSpecial)
                    specialRoomManager.SetSpecialRoom(specialType);
                else
                    specialRoomManager.ClearSpecialRoom();
            }

            int blockIdx = GetBlockIndex(localStage);
            Vector2Int range = (blockIdx >= 0 && blockIdx < enemyCountRangeByBlock.Length)
                ? enemyCountRangeByBlock[blockIdx]
                : new Vector2Int(3, 4);

            int count = Random.Range(range.x, range.y + 1);

            bool isHorde = isSpecial && specialType == SpecialRoomType.Horde;
            if (isHorde)
                count += HORDE_EXTRA_ENEMIES;

            if (isHorde && stageAnnouncerUI != null)
            {
                Debug.Log("[StageGenerator] Appel ShowBossAnnounce avec titre : HORDE !");
                stageAnnouncerUI.ShowBossAnnounce("HORDE !");
            }
            else if (stageAnnouncerUI != null && isSpecial && specialType == SpecialRoomType.HappyHour)
            {
                // Pas de bandeau boss pour Happy Hour (optionnel, silencieux)
            }

            List<EnemyData> pool = GetBasiquePool(universeIndex);
            if (pool.Count == 0)
            {
                Debug.LogWarning($"[StageGenerator] Aucun basique pour univers {universeIndex}", this);
                RegisterEnemiesInManagers();
                return;
            }

            for (int i = 0; i < count; i++)
            {
                EnemyData data = pool[Random.Range(0, pool.Count)];
                Vector2 pos = GetRandomSpawnPosition();
                float hpOverride = isHorde
                    ? GetHpMultiplier(stageNumber, universeIndex) * HORDE_SCALE_REDUCTION
                    : -1f;
                float atkOverride = isHorde
                    ? GetAtkMultiplier(stageNumber, universeIndex) * HORDE_SCALE_REDUCTION
                    : -1f;

                Enemy enemy = SpawnEnemy(data, pos, stageNumber, hpOverride, atkOverride);
                if (enemy == null) continue;

                if (isHorde)
                    enemy.transform.localScale = Vector3.one * 0.85f;

                _currentEnemies.Add(enemy);
            }

            RegisterEnemiesInManagers();
        }

        private void GenerateBossStage(int stageNumber, int universeIndex, bool isMajorBoss)
        {
            if (stageAnnouncerUI != null)
            {
                Debug.Log("[StageGenerator] Appel ShowBossAnnounce avec titre : BOSS FIGHT");
                stageAnnouncerUI.ShowBossAnnounce("BOSS FIGHT");
            }
            else
            {
                Debug.LogWarning("[StageGenerator] stageAnnouncerUI est NULL, impossible d'afficher le bandeau boss.");
            }

            List<EnemyData> bossPool = GetBossPool(universeIndex);
            if (bossPool.Count == 0)
            {
                Debug.LogWarning($"[StageGenerator] Aucun boss pour univers {universeIndex}", this);
                RegisterEnemiesInManagers();
                return;
            }

            EnemyData bossData = isMajorBoss && bossPool.Count >= 2
                ? bossPool[1]
                : bossPool[0];

            float hpMult = GetHpMultiplier(stageNumber, universeIndex) * GetBossHpBonus(stageNumber);
            float atkMult = GetAtkMultiplier(stageNumber, universeIndex) * GetBossAtkBonus(stageNumber);

            Vector2 pos = new Vector2(0f, 3f);
            Enemy boss = SpawnEnemy(bossData, pos, stageNumber, hpMult, atkMult);
            if (boss != null)
            {
                _currentEnemies.Add(boss);
                boss.transform.localScale = Vector3.one * 1.5f;
            }

            if (specialRoomManager != null)
                specialRoomManager.ClearSpecialRoom();

            RegisterEnemiesInManagers();
        }

        private void GenerateMiniBossStage(int stageNumber, int universeIndex, bool isMajorMiniBoss)
        {
            if (stageAnnouncerUI != null)
            {
                Debug.Log("[StageGenerator] Appel ShowBossAnnounce (mini-boss)");
                stageAnnouncerUI.ShowBossAnnounce("MINI-BOSS");
            }
            else
            {
                Debug.LogWarning("[StageGenerator] stageAnnouncerUI est NULL, impossible d'afficher le bandeau boss.");
            }

            List<EnemyData> miniPool = GetMiniBossPool(universeIndex);
            if (miniPool.Count == 0)
            {
                Debug.LogWarning($"[StageGenerator] Aucun mini-boss pour univers {universeIndex}", this);
                RegisterEnemiesInManagers();
                return;
            }

            EnemyData miniData = isMajorMiniBoss && miniPool.Count >= 2
                ? miniPool[1]
                : miniPool[0];

            float hpMult = GetHpMultiplier(stageNumber, universeIndex) * GetMiniBossHpBonus(stageNumber);
            float atkMult = GetAtkMultiplier(stageNumber, universeIndex) * GetMiniBossAtkBonus(stageNumber);

            Vector2 pos = new Vector2(0f, 3f);
            Enemy mini = SpawnEnemy(miniData, pos, stageNumber, hpMult, atkMult);
            if (mini != null)
            {
                _currentEnemies.Add(mini);
                mini.transform.localScale = Vector3.one * 1.2f;
            }

            if (specialRoomManager != null)
                specialRoomManager.ClearSpecialRoom();

            RegisterEnemiesInManagers();
        }

        private void GeneratePostGameStage(int stageNumber)
        {
            bool isDuoBoss = stageNumber % 10 == 0;
            bool isDuoMiniBoss = !isDuoBoss && stageNumber % 5 == 0;

            if (isDuoBoss)
            {
                List<EnemyData> pool = GetBossPool(0);
                SpawnDuo(pool, stageNumber, 1.5f,
                    new Vector2(-2f, 3f), new Vector2(2f, 3f));
            }
            else if (isDuoMiniBoss)
            {
                List<EnemyData> pool = GetMiniBossPool(0);
                SpawnDuo(pool, stageNumber, 1.2f,
                    new Vector2(-2f, 3f), new Vector2(2f, 3f));
            }
            else
            {
                List<EnemyData> pool = GetBasiquePool(0);
                if (pool.Count == 0)
                {
                    Debug.LogWarning("[StageGenerator] Aucun basique pour post-game (pool vide).", this);
                }
                else
                {
                    int count = Random.Range(4, 6);
                    for (int i = 0; i < count; i++)
                    {
                        EnemyData data = pool[Random.Range(0, pool.Count)];
                        Enemy e = SpawnEnemy(data, GetRandomSpawnPosition(), stageNumber);
                        if (e != null)
                            _currentEnemies.Add(e);
                    }
                }
            }

            if (specialRoomManager != null)
                specialRoomManager.ClearSpecialRoom();

            RegisterEnemiesInManagers();
        }

        private void SpawnDuo(List<EnemyData> pool, int stageNumber,
            float scale, Vector2 pos1, Vector2 pos2)
        {
            if (pool == null || pool.Count == 0) return;

            EnemyData d1 = pool[Random.Range(0, pool.Count)];
            EnemyData d2 = pool.Count > 1
                ? pool.Where(e => e != d1).OrderBy(_ => Random.value).First()
                : d1;

            Enemy e1 = SpawnEnemy(d1, pos1, stageNumber);
            Enemy e2 = SpawnEnemy(d2, pos2, stageNumber);

            if (e1 != null)
            {
                e1.transform.localScale = Vector3.one * scale;
                _currentEnemies.Add(e1);
            }
            if (e2 != null)
            {
                e2.transform.localScale = Vector3.one * scale;
                _currentEnemies.Add(e2);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — POOLS
        // ═══════════════════════════════════════════

        private static bool MatchesUniverseFilter(EnemyData e, int universeFilterIndex)
        {
            if (universeFilterIndex == 0)
                return true;
            return e.UniverseIndex == 0 || e.UniverseIndex == universeFilterIndex;
        }

        private List<EnemyData> GetBasiquePool(int universeFilterIndex)
        {
            return allEnemies
                .Where(e => e != null &&
                            e.EnemyRole == EnemyRole.Basique &&
                            MatchesUniverseFilter(e, universeFilterIndex))
                .ToList();
        }

        private List<EnemyData> GetMiniBossPool(int universeFilterIndex)
        {
            return allEnemies
                .Where(e => e != null &&
                            e.EnemyRole == EnemyRole.MiniBoss &&
                            MatchesUniverseFilter(e, universeFilterIndex))
                .ToList();
        }

        private List<EnemyData> GetBossPool(int universeFilterIndex)
        {
            return allEnemies
                .Where(e => e != null &&
                            e.EnemyRole == EnemyRole.Boss &&
                            MatchesUniverseFilter(e, universeFilterIndex))
                .ToList();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — SCALING & HELPERS
        // ═══════════════════════════════════════════

        private float GetHpMultiplier(int stageNumber, int universeIndex)
        {
            int uIndex = Mathf.Clamp(universeIndex - 1, 0, hpScalingPerStageByUniverse.Length - 1);
            float rate = hpScalingPerStageByUniverse[uIndex];
            return 1f + rate * (stageNumber - 1);
        }

        private float GetAtkMultiplier(int stageNumber, int universeIndex)
        {
            int uIndex = Mathf.Clamp(universeIndex - 1, 0, atkScalingPerStageByUniverse.Length - 1);
            float rate = atkScalingPerStageByUniverse[uIndex];
            return 1f + rate * (stageNumber - 1);
        }

        private float GetBossHpBonus(int stageNumber)
        {
            int localStage = ((stageNumber - 1) % UNIVERSE_SIZE) + 1;
            if (localStage == 10) return 1.15f;
            if (localStage == 20) return 1.6f;
            return 1f;
        }

        private float GetBossAtkBonus(int stageNumber)
        {
            int localStage = ((stageNumber - 1) % UNIVERSE_SIZE) + 1;
            if (localStage == 10) return 1.0f;
            if (localStage == 20) return 1.4f;
            return 1f;
        }

        private float GetMiniBossHpBonus(int stageNumber)
        {
            int localStage = ((stageNumber - 1) % UNIVERSE_SIZE) + 1;
            if (localStage == 15) return 1.7f;
            if (localStage == 5) return 1.0f;
            return 1f;
        }

        private float GetMiniBossAtkBonus(int stageNumber)
        {
            int localStage = ((stageNumber - 1) % UNIVERSE_SIZE) + 1;
            if (localStage == 15) return 1.4f;
            if (localStage == 5) return 1.0f;
            return 1f;
        }

        /// <summary>
        /// Index de bloc (0–3) pour les plages d'ennemis ; -1 sur les jalons 5, 10, 15, 20.
        /// </summary>
        private int GetBlockIndex(int localStage)
        {
            if (localStage >= 1 && localStage <= 4) return 0;
            if (localStage >= 6 && localStage <= 9) return 1;
            if (localStage >= 11 && localStage <= 14) return 2;
            if (localStage >= 16 && localStage <= 19) return 3;
            return -1;
        }

        /// <summary>
        /// Retourne le numéro d'univers (1-5) selon l'étage (fond d'arène).
        /// </summary>
        private int GetUniversNumber(int stageNumber)
        {
            if (stageNumber <= 20) return 1;
            if (stageNumber <= 40) return 2;
            if (stageNumber <= 60) return 3;
            if (stageNumber <= 80) return 4;
            return 5;
        }

        private void RegisterEnemiesInManagers()
        {
            if (combatManager != null)
                combatManager.SetEnemies(_currentEnemies);

            if (turnManager != null)
            {
                turnManager.ClearEnemies();
                turnManager.AddEnemies(_currentEnemies);
            }
        }

        /// <summary>
        /// Instancie un ennemi à la position donnée avec données et scaling, sans l'ajouter à _currentEnemies.
        /// </summary>
        private Enemy SpawnEnemy(EnemyData data, Vector2 position, int stageNumber,
            float hpMultOverride = -1f, float atkMultOverride = -1f)
        {
            if (data == null || enemyPrefab == null || enemyContainer == null) return null;

            GameObject go = Instantiate(enemyPrefab, enemyContainer);
            go.transform.position = new Vector3(position.x, position.y, 0f);

            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy == null) return null;

            enemy.SetData(data);

            int universeForScaling = Mathf.Min((stageNumber - 1) / UNIVERSE_SIZE + 1, 5);
            float hpMult = hpMultOverride > 0f
                ? hpMultOverride
                : GetHpMultiplier(stageNumber, universeForScaling);
            float atkMult = atkMultOverride > 0f
                ? atkMultOverride
                : GetAtkMultiplier(stageNumber, universeForScaling);

            ApplyScaling(enemy, hpMult, atkMult);

            EnemyHPBar hpBar = enemy.GetComponentInChildren<EnemyHPBar>();
            if (hpBar != null)
                hpBar.Initialize(enemy);

            if (data.Passives != null && data.Passives.Count > 0)
            {
                EnemyPassiveRuntime runtime =
                    enemy.GetComponent<EnemyPassiveRuntime>();
                if (runtime == null)
                    runtime = enemy.gameObject
                        .AddComponent<EnemyPassiveRuntime>();
                runtime.Initialize(enemy,
                    new List<EnemyPassiveData>(data.Passives),
                    turnManager);
            }

            EnemyShieldSystem shieldSys =
                enemy.GetComponent<EnemyShieldSystem>();
            if (shieldSys != null)
                shieldSys.Initialize(enemy, turnManager);

            return enemy;
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
        /// Applique le multiplicateur HP/ATK déjà calculé.
        /// </summary>
        private void ApplyScaling(Enemy enemy, float hpMult, float atkMult)
        {
            enemy.ApplyStageScaling(hpMult, atkMult);
        }
    }
}
