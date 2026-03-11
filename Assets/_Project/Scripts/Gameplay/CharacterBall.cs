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
        /// <summary> Seuil pour considérer le personnage "visuellement arrêté" et changer de tour. </summary>
        private const float FINAL_STOP_THRESHOLD = 1.5f;
        private static readonly float FINAL_STOP_THRESHOLD_SQR = FINAL_STOP_THRESHOLD * FINAL_STOP_THRESHOLD;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Données du personnage")]
        [SerializeField] private CharacterData characterData;

        [Header("Ralentissement")]
        [Tooltip("% de vitesse conservé chaque frame (0.995 = perd 0.5%/frame). Plus haut = va plus loin.")]
        [SerializeField] private float velocityRetentionPerFrame = 0.995f;

        [Header("Decay aux collisions")]
        [Tooltip("Decay quand collision avec un MUR (peu de perte, conserve momentum).")]
        [SerializeField] private float wallDecay = 0.92f;
        [Tooltip("Decay quand collision avec un ENNEMI (plus de perte).")]
        [SerializeField] private float enemyDecay = 0.7f;

        [Header("Dégâts (collision ennemis)")]
        [Tooltip("Dégâts = (ATK × velocityFactor) × multiplicateur. velocityFactor = vélocité / 10. Min 1.")]
        [SerializeField] private float damageMultiplier = 1f;

        [Header("Physique (optionnel)")]
        [Tooltip("Si non assigné, un matériau bounciness=1 / friction=0 est créé en Awake.")]
        [SerializeField] private PhysicsMaterial2D bouncyMaterial;

        [Header("Références (optionnel)")]
        [SerializeField] private TurnManager turnManager;

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
        private CharacterPassiveRuntime _passiveRuntime;

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

        /// <summary> ATK effective (base + bonus roguelike + bonus passifs). </summary>
        public int EffectiveAtk
        {
            get
            {
                float bonusPercent = 0f;
                float bonusFlat = 0f;
                if (BonusManager.Instance != null)
                {
                    var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.ATK);
                    bonusPercent += percent;
                    bonusFlat += flat;
                }
                if (_passiveRuntime != null)
                    bonusPercent += _passiveRuntime.GetStatBonus(PassiveEffect.BuffATK);
                return Mathf.RoundToInt((_atk + bonusFlat) * (1f + bonusPercent));
            }
        }

        /// <summary> HP Max effectif (base + bonus roguelike + bonus passifs). </summary>
        public int EffectiveMaxHp
        {
            get
            {
                float bonusPercent = 0f;
                float bonusFlat = 0f;
                if (BonusManager.Instance != null)
                {
                    var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.HP);
                    bonusPercent += percent;
                    bonusFlat += flat;
                }
                if (_passiveRuntime != null)
                    bonusPercent += _passiveRuntime.GetStatBonus(PassiveEffect.BuffHP);
                return Mathf.RoundToInt((_maxHp + bonusFlat) * (1f + bonusPercent));
            }
        }

        /// <summary> Speed effective (base + bonus roguelike + bonus passifs). </summary>
        public int EffectiveSpeed
        {
            get
            {
                float bonusPercent = 0f;
                float bonusFlat = 0f;
                if (BonusManager.Instance != null)
                {
                    var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.Speed);
                    bonusPercent += percent;
                    bonusFlat += flat;
                }
                if (_passiveRuntime != null)
                    bonusPercent += _passiveRuntime.GetStatBonus(PassiveEffect.BuffSpeed);
                return Mathf.RoundToInt((_speed + bonusFlat) * (1f + bonusPercent));
            }
        }

        /// <summary> DEF effective (base + bonus roguelike + bonus passifs). </summary>
        public int EffectiveDef
        {
            get
            {
                float bonusPercent = 0f;
                float bonusFlat = 0f;
                if (BonusManager.Instance != null)
                {
                    var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.DamageReduction);
                    bonusPercent += percent;
                    bonusFlat += flat;
                }
                if (_passiveRuntime != null)
                    bonusPercent += _passiveRuntime.GetStatBonus(PassiveEffect.BuffDEF);
                return Mathf.RoundToInt((_def + bonusFlat) * (1f + bonusPercent));
            }
        }

        /// <summary> Multiplicateur de force de lancement (bonus roguelike + bonus passifs). </summary>
        public float EffectiveLaunchForceMultiplier
        {
            get
            {
                float bonusPercent = 0f;
                float bonusFlat = 0f;
                if (BonusManager.Instance != null)
                {
                    var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.LaunchForce);
                    bonusPercent += percent;
                    bonusFlat += flat;
                }
                if (_passiveRuntime != null)
                    bonusPercent += _passiveRuntime.GetStatBonus(PassiveEffect.BuffLaunchForce);
                return 1f + bonusPercent + bonusFlat;
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
        /// <summary> Déclenché quand les stats changent (bonus, etc.). L'UI doit se rafraîchir. </summary>
        public event Action OnStatsChanged;
        /// <summary> Déclenché quand ce personnage touche un ennemi (collision). </summary>
        public event Action OnHitEnemy;
        /// <summary> Déclenché quand ce personnage tue un ennemi. </summary>
        public event Action OnKillEnemy;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            SetupRigidbody();
            SetupCircleCollider();
            InitializeStats();
            ApplyBouncyMaterial();
            _passiveRuntime = GetComponent<CharacterPassiveRuntime>();
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;
            if (_hasStoppedForThisLaunch) return;

            float speedSqr = _rb.velocity.sqrMagnitude;

            // Arrêt visuel : vitesse assez basse → stoppe net et change de tour
            if (speedSqr <= FINAL_STOP_THRESHOLD_SQR)
            {
                _rb.velocity = Vector2.zero; // Snap à l'arrêt (pas de glissade)
                if (_hasBeenLaunched)
                    TriggerStopped();
                return;
            }

            // Decay constant par frame
            if (_hasBeenLaunched)
                _rb.velocity *= velocityRetentionPerFrame;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                int damage = CalculateDamage();
                enemy.TakeDamage(damage);

                OnHitEnemy?.Invoke();
                if (_passiveRuntime != null)
                    _passiveRuntime.NotifyTrigger(PassiveTrigger.OnHitEnemy);

                if (enemy.IsDead)
                {
                    OnKillEnemy?.Invoke();
                    if (_passiveRuntime != null)
                        _passiveRuntime.NotifyTrigger(PassiveTrigger.OnKillEnemy);
                    if (turnManager != null)
                        turnManager.PropagateAllyTrigger(this, PassiveTrigger.OnAllyKill);
                }

                if (_passiveRuntime != null)
                    _passiveRuntime.NotifyTrigger(PassiveTrigger.OnBounceEnemy);

                _rb.velocity *= enemyDecay;
            }
            else
            {
                if (_passiveRuntime != null)
                    _passiveRuntime.NotifyTrigger(PassiveTrigger.OnBounceWall);

                _rb.velocity *= wallDecay;
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

            if (_passiveRuntime != null)
                _passiveRuntime.NotifyTrigger(PassiveTrigger.OnLaunch);
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

            if (_passiveRuntime != null)
                _passiveRuntime.NotifyTrigger(PassiveTrigger.OnTakeDamage);
            if (turnManager != null)
                turnManager.PropagateAllyTrigger(this, PassiveTrigger.OnAllyTakeDamage);

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
        /// Recalcule les HP actuels après un changement de bonus.
        /// Déclenche OnStatsChanged pour rafraîchir l'UI.
        /// </summary>
        public void RecalculateHpAfterBonus()
        {
            // Si les HP actuels dépassent le nouveau max, les réduire
            if (_currentHp > EffectiveMaxHp)
                _currentHp = EffectiveMaxHp;

            // Notifie l'UI que les stats ont changé
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Injecte les données du personnage après l'instanciation (utilisé par CharacterBallFactory).
        /// </summary>
        public void SetCharacterData(CharacterData data)
        {
            characterData = data;
            InitializeStats();
        }

        /// <summary>
        /// Notifie un trigger d'allié (appelé par d'autres CharacterBall ou le TurnManager).
        /// </summary>
        public void NotifyAllyTrigger(PassiveTrigger trigger)
        {
            if (_passiveRuntime != null)
                _passiveRuntime.NotifyTrigger(trigger);
        }

        /// <summary>
        /// Active ou désactive le mouvement (Dynamic = peut bouger, Kinematic = figé).
        /// </summary>
        public void SetMovable(bool canMove)
        {
            if (_rb == null) return;

            if (canMove)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
            }
            else
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f; // Reset aussi la rotation
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
