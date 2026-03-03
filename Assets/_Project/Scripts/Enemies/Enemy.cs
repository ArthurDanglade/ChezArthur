using System;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Ennemi placeholder : hitbox carrée, PV, dégâts et mort. Implémente ITurnParticipant pour le TurnManager.
    /// </summary>
    public class Enemy : MonoBehaviour, ITurnParticipant
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
        private int _baseMaxHp;
        private int _baseAtk;
        private EnemyData _runtimeEnemyData;

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
        /// <summary> Données de l'ennemi assignées (lecture seule). Runtime si SetData appelé, sinon SerializeField. </summary>
        public EnemyData Data => _runtimeEnemyData != null ? _runtimeEnemyData : enemyData;

        /// <summary> True si l'ennemi est mort (GameObject désactivé ou _isDead). </summary>
        public bool IsDead => _isDead;

        /// <summary> Nom de l'ennemi (ITurnParticipant). </summary>
        public string Name => Data != null ? Data.EnemyName : gameObject.name;
        /// <summary> Toujours false pour les ennemis (ITurnParticipant). </summary>
        public bool IsAlly => false;
        /// <summary> Transform du GameObject (ITurnParticipant). </summary>
        public Transform Transform => transform;
        /// <summary> True si le Rigidbody a encore une vélocité significative (ITurnParticipant). </summary>
        public bool IsMoving => _rb != null && _rb.velocity.sqrMagnitude > 0.01f;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand l'ennemi prend des dégâts. Paramètre : dégâts reçus. </summary>
        public event Action<int> OnDamaged;

        /// <summary> Déclenché quand l'ennemi meurt. </summary>
        public event Action OnDeath;

        /// <summary> Déclenché quand l'ennemi s'arrête (ITurnParticipant). À brancher sur la physique de mouvement. </summary>
        public event Action OnStopped;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            SetupBoxCollider();
            SetupRigidbody();
            // InitializeStats sera appelé par SetData() si spawné procéduralement
            // Sinon, on l'appelle ici si un EnemyData est déjà assigné dans l'éditeur
            if (enemyData != null)
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

        /// <summary>
        /// Assigne des données ennemi à la volée et réinitialise les stats (pour le spawn procédural).
        /// </summary>
        public void SetData(EnemyData data)
        {
            _runtimeEnemyData = data;
            InitializeStats();
        }

        /// <summary>
        /// Applique le scaling d'étage aux HP et ATK (utilisé par StageGenerator).
        /// </summary>
        public void ApplyStageScaling(float hpMultiplier, float atkMultiplier)
        {
            _maxHp = Mathf.RoundToInt(_baseMaxHp * hpMultiplier);
            _currentHp = _maxHp;
            _atk = Mathf.RoundToInt(_baseAtk * atkMultiplier);
        }

        /// <summary>
        /// Lance l'ennemi dans la direction avec la force donnée (ITurnParticipant). Cohérent avec CharacterBall (Impulse).
        /// </summary>
        public void Launch(Vector2 direction, float force)
        {
            if (_rb == null) return;
            if (force <= 0f) return;

            Vector2 dir = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
            _rb.AddForce(dir * force, ForceMode2D.Impulse);
        }

        /// <summary>
        /// Active ou désactive le mouvement (Dynamic / Kinematic) (ITurnParticipant).
        /// </summary>
        public void SetMovable(bool canMove)
        {
            if (_rb == null) return;
            if (canMove)
                _rb.bodyType = RigidbodyType2D.Dynamic;
            else
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.velocity = Vector2.zero;
            }
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
            EnemyData dataToUse = _runtimeEnemyData != null ? _runtimeEnemyData : enemyData;

            if (dataToUse != null)
            {
                _baseMaxHp = dataToUse.BaseHp;
                _baseAtk = dataToUse.BaseAtk;
                _maxHp = _baseMaxHp;
                _currentHp = _maxHp;
                _atk = _baseAtk;
                _def = dataToUse.BaseDef;
                _speed = dataToUse.BaseSpeed;
                _talsReward = dataToUse.TalsReward;
                if (_boxCollider != null)
                    _boxCollider.size = new Vector2(dataToUse.ColliderWidth, dataToUse.ColliderHeight);
            }
            else
            {
                Debug.LogWarning("[Enemy] Aucun EnemyData assigné, utilisation des valeurs par défaut.", this);
                _baseMaxHp = 150;
                _baseAtk = 10;
                _maxHp = _baseMaxHp;
                _currentHp = _maxHp;
                _atk = _baseAtk;
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
