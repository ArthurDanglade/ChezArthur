using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Gère le comportement automatique d'un ennemi pendant son tour (lancement vers un allié).
    /// R6 : direction exacte, force fixe data, wind-up — zéro aléatoire.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        /// <summary> Repli pour les assets non calibrés (milieu de l'ancien 28–38). Remplacé par la data au G6a. </summary>
        private const float DEFAULT_LAUNCH_FORCE = 33f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Configuration IA")]
        [Tooltip("Délai avant lancement (feedback wind-up R6).")]
        [SerializeField] private float launchDelay = 0.5f;

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
                try
                {
                    if (EnemyFixedTurnActionRegistry.TryGet(_enemy, out Func<IEnumerator> action))
                        yield return StartCoroutine(action());
                    else
                        yield return new WaitForSecondsRealtime(launchDelay);
                }
                finally
                {
                    // Toujours clôturer le tour — une exception VFX ne doit pas geler la file.
                    if (_enemy != null && !_enemy.IsDead)
                        _enemy.CompleteTurnWithoutLaunch();
                    _isExecutingTurn = false;
                }

                yield break;
            }

            // R6 — wind-up lisible pendant l'attente pré-lancer (flash + pulse, code seul).
            _enemy.PlayWindup(launchDelay);
            yield return new WaitForSeconds(launchDelay);

            CharacterBall target = GetTarget();
            if (target == null)
            {
                _enemy.CompleteTurnWithoutLaunch();
                _isExecutingTurn = false;
                yield break;
            }

            // R6 — direction EXACTE vers la cible, force FIXE par ennemi. Zéro aléatoire.
            Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            float force = (_enemy.Data != null && _enemy.Data.LaunchForce > 0f)
                ? _enemy.Data.LaunchForce
                : DEFAULT_LAUNCH_FORCE;
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
    }
}
