using System.Collections.Generic;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using UnityEngine;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Invocation de coéquipiers en combat : spawn, scaling d'étage, TurnManager / CombatManager, passifs optionnels.
    /// </summary>
    public class EnemySummonSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const float SpawnMargin = 1f;
        private const float MinDistanceFromOwner = 2f;
        private const int MaxSpawnAttempts = 10;
        private const float StageScalingPerLevel = 0.1f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private Enemy _owner;
        private TurnManager _turnManager;
        private CombatManager _combatManager;
        private Arena _arena;
        private GameObject _enemyPrefab;
        private Transform _summonContainer;
        private int _currentStage;
        private int _maxSummons;

        private readonly List<Enemy> _activeSummons = new List<Enemy>(4);

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Nombre d'invocations actuellement en vie sur le terrain. </summary>
        public int AliveSummonCount { get; private set; }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise le système.
        /// </summary>
        public void Initialize(Enemy owner, TurnManager turnManager, CombatManager combatManager, Arena arena,
            GameObject enemyPrefab, int currentStage, Transform summonContainer, int maxSummons = 4)
        {
            _owner = owner;
            _turnManager = turnManager;
            _combatManager = combatManager;
            _arena = arena;
            _enemyPrefab = enemyPrefab;
            _currentStage = currentStage;
            _summonContainer = summonContainer;
            _maxSummons = maxSummons;

            _activeSummons.Clear();
            AliveSummonCount = 0;
        }

        /// <summary>
        /// Invoque un ennemi depuis les données fournies. Retourne null si échec ou limite atteinte.
        /// </summary>
        public Enemy Summon(EnemyData data)
        {
            CleanDeadSummons();

            if (_activeSummons.Count >= _maxSummons)
                return null;

            if (data == null || _enemyPrefab == null || _arena == null)
                return null;

            bool spawnOk = TryGetSpawnPosition(out Vector2 spawn);
            if (!spawnOk)
                return null;

            GameObject go = Instantiate(_enemyPrefab, new Vector3(spawn.x, spawn.y, 0f), Quaternion.identity, _summonContainer);
            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy == null)
            {
                Destroy(go);
                return null;
            }

            enemy.SetData(data);

            float mult = 1f + StageScalingPerLevel * (_currentStage - 1);
            enemy.ApplyStageScaling(mult, mult);

            if (data.Passives != null && data.Passives.Count > 0)
            {
                EnemyPassiveRuntime runtime = enemy.GetComponent<EnemyPassiveRuntime>();
                if (runtime == null)
                    runtime = enemy.gameObject.AddComponent<EnemyPassiveRuntime>();

                var passivesCopy = new List<EnemyPassiveData>(data.Passives.Count);
                for (int i = 0; i < data.Passives.Count; i++)
                {
                    if (data.Passives[i] != null)
                        passivesCopy.Add(data.Passives[i]);
                }

                if (passivesCopy.Count > 0 && _turnManager != null)
                    runtime.Initialize(enemy, passivesCopy, _turnManager);
            }

            if (_turnManager != null)
                _turnManager.AddEnemyMidCombat(enemy);

            if (_combatManager != null)
                _combatManager.AddEnemyToCombat(enemy);

            _activeSummons.Add(enemy);
            AliveSummonCount = _activeSummons.Count;

            return enemy;
        }

        /// <summary>
        /// Remet à zéro pour un nouvel étage (compteur d'étage + suivi des invocations).
        /// </summary>
        public void ResetForNewStage(int newStage)
        {
            _currentStage = newStage;
            _activeSummons.Clear();
            AliveSummonCount = 0;
        }

        /// <summary>
        /// Nettoie les références.
        /// </summary>
        public void Cleanup()
        {
            _owner = null;
            _turnManager = null;
            _combatManager = null;
            _arena = null;
            _enemyPrefab = null;
            _summonContainer = null;
            _activeSummons.Clear();
            AliveSummonCount = 0;
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void OnDestroy()
        {
            Cleanup();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Retire les invocations mortes ou détruites sans allocation (swap avec le dernier élément).
        /// </summary>
        private void CleanDeadSummons()
        {
            int i = 0;
            while (i < _activeSummons.Count)
            {
                Enemy e = _activeSummons[i];
                if (e == null || e.IsDead)
                {
                    int last = _activeSummons.Count - 1;
                    _activeSummons[i] = _activeSummons[last];
                    _activeSummons.RemoveAt(last);
                }
                else
                    i++;
            }

            AliveSummonCount = _activeSummons.Count;
        }

        /// <summary>
        /// Moitié haute de l'arène (comme StageGenerator), marge 1 u, distance min 2 u au propriétaire.
        /// </summary>
        private bool TryGetSpawnPosition(out Vector2 position)
        {
            position = Vector2.zero;

            if (_owner == null || _arena == null)
                return false;

            Bounds b = _arena.Bounds;
            float xMin = b.min.x + SpawnMargin;
            float xMax = b.max.x - SpawnMargin;
            float yMin = b.center.y;
            float yMax = b.max.y - SpawnMargin;

            Vector2 ownerPos = _owner.Transform.position;

            for (int attempt = 0; attempt < MaxSpawnAttempts; attempt++)
            {
                float x = Random.Range(xMin, xMax);
                float y = Random.Range(yMin, yMax);
                Vector2 p = new Vector2(x, y);

                if (Vector2.Distance(p, ownerPos) >= MinDistanceFromOwner)
                {
                    position = p;
                    return true;
                }
            }

            return false;
        }
    }
}
