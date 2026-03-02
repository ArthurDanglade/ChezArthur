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
        [Header("Données de l'ennemi")]
        [SerializeField] private EnemyData enemyData;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private BoxCollider2D _boxCollider;
        private Rigidbody2D _rb;
        private int _currentHp;
        private int _maxHp;
        private int _atk;
        private int _def;
        private int _speed;
        private int _talsReward;
        private bool _isDead;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Points de vie actuels (lecture seule). </summary>
        public int CurrentHp => _currentHp;
        /// <summary> PV max (lecture seule). </summary>
        public int MaxHp => _maxHp;
        /// <summary> ATK de base (lecture seule). </summary>
        public int Atk => _atk;
        /// <summary> DEF de base (lecture seule). </summary>
        public int Def => _def;
        /// <summary> Vitesse de base (lecture seule). </summary>
        public int Speed => _speed;
        /// <summary> Tals donnés à la mort (lecture seule). </summary>
        public int TalsReward => _talsReward;
        /// <summary> Données de l'ennemi assignées (lecture seule). </summary>
        public EnemyData Data => enemyData;

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
            SetupBoxCollider();
            SetupRigidbody();
            InitializeStats();
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

            _boxCollider.size = new Vector2(1f, 1f);
        }

        /// <summary>
        /// Initialise les stats depuis EnemyData (ou valeurs par défaut si null).
        /// </summary>
        private void InitializeStats()
        {
            _isDead = false;

            if (enemyData != null)
            {
                _maxHp = enemyData.BaseHp;
                _currentHp = _maxHp;
                _atk = enemyData.BaseAtk;
                _def = enemyData.BaseDef;
                _speed = enemyData.BaseSpeed;
                _talsReward = enemyData.TalsReward;
                if (_boxCollider != null)
                    _boxCollider.size = new Vector2(enemyData.ColliderWidth, enemyData.ColliderHeight);
            }
            else
            {
                Debug.LogWarning("[Enemy] Aucun EnemyData assigné, utilisation des valeurs par défaut.", this);
                _maxHp = 150;
                _currentHp = _maxHp;
                _atk = 10;
                _def = 5;
                _speed = 50;
                _talsReward = 1;
                if (_boxCollider != null)
                    _boxCollider.size = new Vector2(1f, 1f);
            }
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
