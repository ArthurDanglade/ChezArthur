using System.Collections;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Gère le comportement automatique d'un ennemi pendant son tour (lancement vers un allié).
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Configuration IA")]
        [SerializeField] private float minLaunchForce = 20f;
        [SerializeField] private float maxLaunchForce = 30f;
        [Tooltip("Délai avant lancement (feedback visuel futur).")]
        [SerializeField] private float launchDelay = 0.5f;

        [Header("Précision")]
        [Tooltip("1 = parfait, 0 = aléatoire total.")]
        [SerializeField] [Range(0f, 1f)] private float accuracy = 0.7f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Enemy _enemy;
        private bool _isExecutingTurn;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Démarre le tour de l'ennemi (appelé par TurnManager quand c'est son tour).
        /// </summary>
        public void StartTurn()
        {
            if (_enemy == null) return;
            if (_isExecutingTurn) return;

            StartCoroutine(ExecuteTurn());
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private IEnumerator ExecuteTurn()
        {
            _isExecutingTurn = true;

            yield return new WaitForSeconds(launchDelay);

            CharacterBall target = GetTarget();
            if (target == null)
            {
                _isExecutingTurn = false;
                yield break;
            }

            Vector2 direction = GetLaunchDirection(target.transform);
            float force = Random.Range(minLaunchForce, maxLaunchForce);
            _enemy.Launch(direction, force);

            _isExecutingTurn = false;
        }

        /// <summary>
        /// Retourne l'allié vivant le plus proche, ou null si aucun.
        /// </summary>
        private CharacterBall GetTarget()
        {
            CharacterBall[] allies = FindObjectsOfType<CharacterBall>();
            CharacterBall closest = null;
            float closestSqr = float.MaxValue;
            Vector2 myPos = transform.position;

            for (int i = 0; i < allies.Length; i++)
            {
                if (allies[i] == null || allies[i].IsDead) continue;

                float sqr = (allies[i].transform.position - (Vector3)myPos).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = allies[i];
                }
            }

            return closest;
        }

        /// <summary>
        /// Calcule la direction de lancement vers la cible avec une erreur basée sur accuracy.
        /// </summary>
        private Vector2 GetLaunchDirection(Transform target)
        {
            Vector2 perfectDir = ((Vector2)target.position - (Vector2)transform.position).normalized;

            if (accuracy >= 1f)
                return perfectDir;

            Vector2 randomDir = Random.insideUnitCircle.normalized;
            if (randomDir.sqrMagnitude < 0.01f)
                randomDir = Vector2.up;

            Vector2 dir = Vector2.Lerp(perfectDir, randomDir, 1f - accuracy).normalized;
            return dir.sqrMagnitude > 0.01f ? dir : perfectDir;
        }
    }
}
