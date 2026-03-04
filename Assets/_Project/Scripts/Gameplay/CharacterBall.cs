using System;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Roguelike;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Personnage placeholder en forme de balle : lancement, rebonds, arrêt.
    /// Aux impacts : decay dynamique (peu de perte rapide, forte perte lente). Sous un ratio de la vitesse de lancement, la vitesse baisse d'elle-même chaque frame → arrêt naturel sans traîner.
    /// </summary>
    public class CharacterBall : MonoBehaviour, ITurnParticipant
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BOUNCY_MATERIAL_NAME = "BouncyMaterial";
        /// <summary> En dessous de cette vitesse (magnitude), on considère arrêt total et on déclenche OnStopped. </summary>
        private const float FINAL_STOP_THRESHOLD = 0.01f;
        private static readonly float FINAL_STOP_THRESHOLD_SQR = FINAL_STOP_THRESHOLD * FINAL_STOP_THRESHOLD;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Données du personnage")]
        [SerializeField] private CharacterData characterData;

        [Header("Ralentissement dynamique (style Monster Strike)")]
        [Tooltip("Decay quand la balle est lente (perte très forte → arrêt net, évite qu'elle erre). Plus bas = arrêt plus rapide.")]
        [SerializeField] private float minDecay = 0.25f;
        [Tooltip("Decay quand la balle est rapide (perte faible).")]
        [SerializeField] private float maxDecay = 0.95f;

        [Header("Ralentissement continu (vitesse lente)")]
        [Tooltip("Quand la vitesse actuelle tombe sous ce ratio de la vitesse de lancement (ex: 0,5 = 50%), la balle perd de la vitesse toute seule chaque frame, sans attendre les impacts. Arrêt naturel rapide.")]
        [SerializeField] private float speedRatioThreshold = 0.5f;
        [Tooltip("Multiplicateur de vélocité appliqué chaque frame quand on est sous le ratio (vitesse baisse d'elle-même). Plus bas = arrêt plus rapide (ex: 0,9 = perd 10% par frame).")]
        [SerializeField] private float continuousDecayPerFrame = 0.9f;

        [Header("Dégâts (collision ennemis)")]
        [Tooltip("Dégâts = (ATK × velocityFactor) × multiplicateur. velocityFactor = vélocité / 10. Min 1.")]
        [SerializeField] private float damageMultiplier = 1f;

        [Header("Physique (optionnel)")]
        [Tooltip("Si non assigné, un matériau bounciness=1 / friction=0 est créé en Awake.")]
        [SerializeField] private PhysicsMaterial2D bouncyMaterial;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Rigidbody2D _rb;
        private CircleCollider2D _circleCollider;
        private bool _hasStoppedForThisLaunch;
        private bool _hasBeenLaunched;
        private float _launchSpeed;
        private int _currentHp;
        private int _maxHp;
        private int _atk;
        private int _def;
        private int _speed;
        private bool _isDead;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> True si la vélocité est au-dessus du seuil d'arrêt (personnage encore en mouvement). </summary>
        public bool IsMoving => _rb != null && _rb.velocity.sqrMagnitude > FINAL_STOP_THRESHOLD_SQR;

        /// <summary> PV actuels (lecture seule). </summary>
        public int CurrentHp => _currentHp;
        /// <summary> PV max (lecture seule, avec bonus). </summary>
        public int MaxHp => EffectiveMaxHp;
        /// <summary> ATK de base (lecture seule). </summary>
        public int Atk => _atk;
        /// <summary> DEF de base (lecture seule). </summary>
        public int Def => _def;
        /// <summary> Vitesse pour l'ordre des tours (avec bonus). </summary>
        public int Speed => EffectiveSpeed;
        /// <summary> Données du personnage assignées (lecture seule). </summary>
        public CharacterData Data => characterData;
        /// <summary> True si le personnage est mort (PV &lt;= 0). </summary>
        public bool IsDead => _currentHp <= 0;
        /// <summary> True si le personnage peut bouger (Rigidbody2D Dynamic). </summary>
        public bool IsMovable => _rb != null && _rb.bodyType == RigidbodyType2D.Dynamic;

        /// <summary> Nom du personnage (ITurnParticipant). </summary>
        public string Name => characterData != null ? characterData.CharacterName : gameObject.name;
        /// <summary> Toujours true pour les alliés (ITurnParticipant). </summary>
        public bool IsAlly => true;
        /// <summary> Transform du GameObject (ITurnParticipant). </summary>
        public Transform Transform => transform;

        /// <summary> ATK effective (base + bonus). </summary>
        public int EffectiveAtk
        {
            get
            {
                if (BonusManager.Instance == null) return _atk;
                var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.ATK);
                return Mathf.RoundToInt((_atk + flat) * (1f + percent));
            }
        }

        /// <summary> HP Max effectif (base + bonus). </summary>
        public int EffectiveMaxHp
        {
            get
            {
                if (BonusManager.Instance == null) return _maxHp;
                var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.HP);
                return Mathf.RoundToInt((_maxHp + flat) * (1f + percent));
            }
        }

        /// <summary> Speed effective (base + bonus). </summary>
        public int EffectiveSpeed
        {
            get
            {
                if (BonusManager.Instance == null) return _speed;
                var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.Speed);
                return Mathf.RoundToInt((_speed + flat) * (1f + percent));
            }
        }

        /// <summary>
        /// DEF effective (base + bonus).
        /// </summary>
        public int EffectiveDef
        {
            get
            {
                if (BonusManager.Instance == null) return _def;
                var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.DamageReduction);
                return Mathf.RoundToInt((_def + flat) * (1f + percent));
            }
        }

        /// <summary> Multiplicateur de force de lancement (bonus). </summary>
        public float EffectiveLaunchForceMultiplier
        {
            get
            {
                if (BonusManager.Instance == null) return 1f;
                var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.LaunchForce);
                return 1f + percent + flat;
            }
        }

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché une fois quand le personnage s'arrête (ralentissement progressif jusqu'à l'arrêt). </summary>
        public event Action OnStopped;
        /// <summary> Déclenché quand le personnage prend des dégâts. Paramètre : dégâts reçus. </summary>
        public event Action<int> OnDamaged;
        /// <summary> Déclenché quand le personnage meurt. </summary>
        public event Action OnDeath;
        /// <summary> Déclenché quand le personnage est soigné. Paramètre : montant soigné. </summary>
        public event Action<int> OnHealed;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            SetupRigidbody();
            SetupCircleCollider();
            InitializeStats();
            ApplyBouncyMaterial();
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;
            if (_hasStoppedForThisLaunch) return;

            float speedSqr = _rb.velocity.sqrMagnitude;
            if (speedSqr <= FINAL_STOP_THRESHOLD_SQR)
            {
                _rb.velocity = Vector2.zero;
                if (_hasBeenLaunched)
                    TriggerStopped();
                return;
            }

            // Vitesse baisse d'elle-même sous un certain ratio (ex: 50% de la vitesse de lancement), sans attendre les murs
            if (_launchSpeed >= 0.01f)
            {
                float launchSpeedSqr = _launchSpeed * _launchSpeed;
                float slowZoneSqr = launchSpeedSqr * speedRatioThreshold * speedRatioThreshold;
                if (speedSqr <= slowZoneSqr)
                    _rb.velocity *= continuousDecayPerFrame;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Dégâts à l'ennemi avec la vélocité d'entrée (avant decay)
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                int damage = CalculateDamage();
                enemy.TakeDamage(damage);
            }

            // Decay dynamique : peu de perte quand rapide, perte forte quand lent → arrêt naturel
            if (_launchSpeed >= 0.01f)
            {
                float speedRatio = Mathf.Clamp01(_rb.velocity.magnitude / _launchSpeed);
                float decay = Mathf.Lerp(minDecay, maxDecay, speedRatio);
                _rb.velocity *= decay;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Lance le personnage dans la direction donnée avec la force donnée.
        /// </summary>
        public void Launch(Vector2 direction, float force)
        {
            if (_rb == null) return;
            if (force <= 0f) return;

            _hasBeenLaunched = true;

            Vector2 dir = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
            float effectiveForce = force * EffectiveLaunchForceMultiplier;
            _rb.AddForce(dir * effectiveForce, ForceMode2D.Impulse);
            _launchSpeed = effectiveForce / _rb.mass;
            _hasStoppedForThisLaunch = false;
        }

        /// <summary>
        /// Applique des dégâts au personnage. Déclenche OnDamaged ; si PV &lt;= 0, appelle Die().
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            if (_currentHp <= 0) return;

            // Applique la réduction de dégâts (DEF)
            int finalDamage = Mathf.Max(1, damage - EffectiveDef);

            _currentHp = Mathf.Max(0, _currentHp - finalDamage);
            OnDamaged?.Invoke(finalDamage);

            if (_currentHp <= 0)
                Die();
        }

        /// <summary>
        /// Tue le personnage : déclenche OnDeath et désactive le GameObject.
        /// </summary>
        public void Die()
        {
            if (_isDead) return;
            _isDead = true;
            OnDeath?.Invoke();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Ressuscite le personnage avec tous ses HP.
        /// </summary>
        public void Revive()
        {
            _isDead = false;
            _currentHp = EffectiveMaxHp;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Soigne le personnage d'un montant donné (ne dépasse pas MaxHp).
        /// </summary>
        public void Heal(int amount)
        {
            if (amount <= 0) return;
            if (_isDead) return;

            // Applique le multiplicateur de salle spéciale (Happy Hour)
            if (SpecialRoomManager.Instance != null)
                amount = Mathf.RoundToInt(amount * SpecialRoomManager.Instance.HealMultiplier);

            int previousHp = _currentHp;
            _currentHp = Mathf.Min(_currentHp + amount, EffectiveMaxHp);
            int actualHeal = _currentHp - previousHp;

            if (actualHeal > 0)
                OnHealed?.Invoke(actualHeal);
        }

        /// <summary>
        /// Active ou désactive le mouvement (Dynamic = peut bouger, Kinematic = figé).
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

        private void SetupRigidbody()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
                _rb = gameObject.AddComponent<Rigidbody2D>();

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.drag = 0f;
            _rb.angularDrag = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void SetupCircleCollider()
        {
            _circleCollider = GetComponent<CircleCollider2D>();
            if (_circleCollider == null)
                _circleCollider = gameObject.AddComponent<CircleCollider2D>();

            _circleCollider.radius = 0.5f;
        }

        /// <summary>
        /// Initialise les stats depuis CharacterData (ou valeurs par défaut si null).
        /// </summary>
        private void InitializeStats()
        {
            _isDead = false;

            if (characterData != null)
            {
                _maxHp = characterData.BaseHp;
                _currentHp = _maxHp;
                _atk = characterData.BaseAtk;
                _def = characterData.BaseDef;
                _speed = characterData.BaseSpeed;
                if (_circleCollider != null)
                    _circleCollider.radius = characterData.ColliderRadius;
            }
            else
            {
                Debug.LogWarning("[CharacterBall] Aucun CharacterData assigné, utilisation des valeurs par défaut.", this);
                _maxHp = 100;
                _currentHp = _maxHp;
                _atk = 10;
                _def = 5;
                _speed = 50;
                if (_circleCollider != null)
                    _circleCollider.radius = 0.5f;
            }
        }

        private void ApplyBouncyMaterial()
        {
            if (_circleCollider == null) return;

            PhysicsMaterial2D material = bouncyMaterial;
            if (material == null)
            {
                material = new PhysicsMaterial2D
                {
                    name = BOUNCY_MATERIAL_NAME,
                    bounciness = 1f,
                    friction = 0f
                };
            }

            _circleCollider.sharedMaterial = material;
        }

        private void TriggerStopped()
        {
            if (_hasStoppedForThisLaunch) return;
            _hasStoppedForThisLaunch = true;
            OnStopped?.Invoke();
        }

        /// <summary>
        /// Calcule les dégâts à infliger : (ATK × velocityFactor) × damageMultiplier. velocityFactor = vélocité / 10. Min 1, arrondi au supérieur.
        /// </summary>
        private int CalculateDamage()
        {
            float velocityFactor = _rb.velocity.magnitude / 10f;
            float raw = (EffectiveAtk * velocityFactor) * damageMultiplier;
            return Mathf.Max(1, Mathf.CeilToInt(raw));
        }
    }
}
