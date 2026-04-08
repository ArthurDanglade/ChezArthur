using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.UI
{
    /// <summary>
    /// Câble les événements de dégâts/soins d'une cible vers le FloatingNumberSpawner.
    /// </summary>
    public class FloatingNumberHook : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // TYPES PUBLICS
        // ═══════════════════════════════════════════
        public enum HookTarget
        {
            Ally,
            Enemy
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private HookTarget hookTarget;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private CharacterBall _characterBall;
        private Enemy _enemy;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (hookTarget == HookTarget.Ally)
                _characterBall = GetComponent<CharacterBall>();
            else if (hookTarget == HookTarget.Enemy)
                _enemy = GetComponent<Enemy>();
        }

        private void OnEnable()
        {
            if (_characterBall != null)
            {
                _characterBall.OnDamaged += OnAllyDamaged;
                _characterBall.OnHealed += OnAllyHealed;
            }

            if (_enemy != null)
                _enemy.OnDamaged += OnEnemyDamaged;
        }

        private void OnDisable()
        {
            if (_characterBall != null)
            {
                _characterBall.OnDamaged -= OnAllyDamaged;
                _characterBall.OnHealed -= OnAllyHealed;
            }

            if (_enemy != null)
                _enemy.OnDamaged -= OnEnemyDamaged;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void OnEnemyDamaged(int amount)
        {
            if (FloatingNumberSpawner.Instance == null) return;
            FloatingNumberSpawner.Instance.ShowDamageEnemy(amount, transform.position);
        }

        private void OnAllyDamaged(int amount)
        {
            if (FloatingNumberSpawner.Instance == null) return;
            FloatingNumberSpawner.Instance.ShowDamageAlly(amount, transform.position);
        }

        private void OnAllyHealed(int amount)
        {
            if (FloatingNumberSpawner.Instance == null) return;
            FloatingNumberSpawner.Instance.ShowHeal(amount, transform.position);
        }
    }
}
