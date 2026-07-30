using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private float minLaunchForce = 28f;
        [SerializeField] private float maxLaunchForce = 38f;
        [Tooltip("Délai avant lancement (feedback visuel futur).")]
        [SerializeField] private float launchDelay = 0.5f;

        [Header("Précision")]
        [Tooltip("1 = parfait, 0 = aléatoire total.")]
        [SerializeField] [Range(0f, 1f)] private float accuracy = 0.82f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Enemy _enemy;
        private bool _isExecutingTurn;
        private TurnManager _turnManager;

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

        /// <summary>
        /// Résolution de cible courante SANS agir (ligne d'aggro G3, réévaluée au switch).
        /// </summary>
        public CharacterBall ResolveCurrentTarget()
        {
            return GetTarget();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private IEnumerator ExecuteTurn()
        {
            _isExecutingTurn = true;

            // R2 — un Fixe n'est jamais lancé : patterns au G6, placeholder = tour passé proprement.
            if (_enemy.Archetype == EnemyArchetype.Fixed)
            {
                yield return new WaitForSeconds(launchDelay);
                _enemy.CompleteTurnWithoutLaunch();
                _isExecutingTurn = false;
                yield break;
            }

            yield return new WaitForSeconds(launchDelay);

            CharacterBall target = GetTarget();
            if (target == null)
            {
                _enemy.CompleteTurnWithoutLaunch();
                _isExecutingTurn = false;
                yield break;
            }

            Vector2 direction = GetLaunchDirection(target.transform);
            float force = Random.Range(minLaunchForce, maxLaunchForce);
            _enemy.Launch(direction, force);

            _isExecutingTurn = false;
        }

        /// <summary>
        /// Résout la cible via TargetSelector (R3) ; liste vide = plus proche (historique).
        /// </summary>
        private CharacterBall GetTarget()
        {
            IReadOnlyList<CharacterBall> candidates = ResolveAllyCandidates();
            TargetSelectorData selector = _enemy != null && _enemy.Data != null
                ? _enemy.Data.TargetSelector
                : null;
            return TargetSelectorResolver.Resolve(selector, transform.position, candidates);
        }

        private IReadOnlyList<CharacterBall> ResolveAllyCandidates()
        {
            if (_turnManager == null)
                _turnManager = FindObjectOfType<TurnManager>();

            if (_turnManager != null)
                return _turnManager.GetAllies();

            // Repli scènes dev sans TurnManager — seule allocation tolérée de ce chemin.
            return FindObjectsOfType<CharacterBall>();
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
