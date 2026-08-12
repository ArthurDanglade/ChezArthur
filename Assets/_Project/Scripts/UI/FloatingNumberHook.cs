using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.UI
{
    /// <summary>
    /// Câble les événements de dégâts/soins d'une cible vers le FloatingNumberSpawner.
    /// F5-L1 : dedup-frame au puits + log diagnostic si doublon.
    /// Note : au 12/08 aucun prefab/runtime n'attache ce hook (chemin vivant =
    /// FloatingNumberSpawner.BindAllParticipants) — dedup miroir aussi dans ShowDamage*.
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
        private int _lastDamageFrame = -1;
        private int _lastDamageAmount = int.MinValue;

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
            if (IsDuplicateDamageThisFrame(amount)) return;
            bool isCrit = _enemy != null && _enemy.LastDamageWasCrit;
            FloatingNumberSpawner.Instance.ShowDamageEnemy(amount, transform.position, isCrit);
        }

        private void OnAllyDamaged(int amount)
        {
            if (FloatingNumberSpawner.Instance == null) return;
            if (IsDuplicateDamageThisFrame(amount)) return;
            FloatingNumberSpawner.Instance.ShowDamageAlly(amount, transform.position);
        }

        private void OnAllyHealed(int amount)
        {
            if (FloatingNumberSpawner.Instance == null) return;
            FloatingNumberSpawner.Instance.ShowHeal(amount, transform.position);
        }

        /// <summary>
        /// Même unité + même montant + même frame → skip (diagnostic DEV).
        /// </summary>
        private bool IsDuplicateDamageThisFrame(int amount)
        {
            int frame = Time.frameCount;
            if (_lastDamageFrame == frame && _lastDamageAmount == amount)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Popup] doublon {name} {amount} f{frame}");
#endif
                return true;
            }

            _lastDamageFrame = frame;
            _lastDamageAmount = amount;
            return false;
        }
    }
}
