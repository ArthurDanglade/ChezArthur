using System;
using UnityEngine;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Ennemi placeholder : hitbox carrée, PV, dégâts et mort. Statique (Kinematic) pour le prototype.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Collider")]
        [SerializeField] private float width = 1f;
        [SerializeField] private float height = 1f;

        [Header("Stats (GDD)")]
        [SerializeField] private int maxHp = 150;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private BoxCollider2D _boxCollider;
        private Rigidbody2D _rb;
        private int _currentHp;
        private bool _isDead;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Points de vie actuels (lecture seule). </summary>
        public int CurrentHp => _currentHp;

        /// <summary> True si l'ennemi est mort (GameObject désactivé ou _isDead). </summary>
        public bool IsDead => _isDead;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand l'ennemi prend des dégâts. Paramètre : dégâts reçus. </summary>
        public event Action<int> OnDamaged;

        /// <summary> Déclenché quand l'ennemi meurt. </summary>
        public event Action OnDeath;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _currentHp = maxHp;
            SetupBoxCollider();
            SetupRigidbody();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Applique des dégâts à l'ennemi. Déclenche OnDamaged ; si PV &lt;= 0, appelle Die().
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            if (_isDead) return;

            _currentHp = Mathf.Max(0, _currentHp - damage);
            OnDamaged?.Invoke(damage);

            if (_currentHp <= 0)
                Die();
        }

        /// <summary>
        /// Tue l'ennemi : déclenche OnDeath et désactive le GameObject.
        /// </summary>
        public void Die()
        {
            if (_isDead) return;
            _isDead = true;
            OnDeath?.Invoke();
            gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void SetupBoxCollider()
        {
            _boxCollider = GetComponent<BoxCollider2D>();
            if (_boxCollider == null)
                _boxCollider = gameObject.AddComponent<BoxCollider2D>();

            _boxCollider.size = new Vector2(width, height);
        }

        private void SetupRigidbody()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
                _rb = gameObject.AddComponent<Rigidbody2D>();

            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
        }
    }
}
