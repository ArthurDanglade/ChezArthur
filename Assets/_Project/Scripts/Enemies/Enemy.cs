using System;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives.Handlers;
using ChezArthur.Roguelike;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Ennemi placeholder : hitbox carrée, PV, dégâts et mort. Implémente ITurnParticipant pour le TurnManager.
    /// </summary>
    public class Enemy : MonoBehaviour, ITurnParticipant
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        /// <summary> Seuil pour considérer "visuellement arrêté" et changer de tour. </summary>
        private const float FINAL_STOP_THRESHOLD = 1.5f;
        private static readonly float FINAL_STOP_THRESHOLD_SQR = FINAL_STOP_THRESHOLD * FINAL_STOP_THRESHOLD;
        private const string BOUNCY_MATERIAL_NAME = "BouncyMaterial";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Données de l'ennemi")]
        [SerializeField] private EnemyData enemyData;

        [Header("Ralentissement")]
        [Tooltip("% de vitesse conservé chaque frame (0.99 = perd 1%/frame). Plus haut = va plus loin.")]
        [SerializeField] private float velocityRetentionPerFrame = 0.99f;

        [Header("Decay aux collisions")]
        [Tooltip("Decay quand collision avec un MUR (peu de perte).")]
        [SerializeField] private float wallDecay = 0.75f;
        [Tooltip("Decay quand collision avec un ALLIÉ (plus de perte).")]
        [SerializeField] private float allyDecay = 0.6f;

        [Header("Physique")]
        [SerializeField] private PhysicsMaterial2D bouncyMaterial;

        [Header("Dégâts (collision alliés)")]
        [Tooltip("Dégâts = (ATK × velocityFactor) × multiplicateur. velocityFactor = vélocité / 10. Min 1.")]
        [SerializeField] private float damageMultiplier = 1f;

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
        private float _launchSpeed;
        private bool _hasStoppedForThisLaunch;
        private bool _hasBeenLaunched;
        private BuffReceiver _buffReceiver;

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
        /// <summary> Vitesse effective pour l'ordre des tours (base + buffs/debuffs). </summary>
        public int Speed => EffectiveSpeed;
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
        /// <summary> Buffs/debuffs ciblés sur cet ennemi (saignement, vulnérabilité, etc.). </summary>
        public BuffReceiver BuffReceiver => _buffReceiver;

        /// <summary> ATK effective (base + debuffs). </summary>
        public int EffectiveAtk
        {
            get
            {
                if (_buffReceiver == null) return _atk;
                var (percent, flat) = _buffReceiver.GetStatModifier(BuffStatType.ATK);
                return Mathf.Max(0, Mathf.RoundToInt((_atk + flat) * (1f + percent)));
            }
        }

        /// <summary> Speed effective (base + debuffs). </summary>
        public int EffectiveSpeed
        {
            get
            {
                if (_buffReceiver == null) return _speed;
                var (percent, flat) = _buffReceiver.GetStatModifier(BuffStatType.Speed);
                return Mathf.Max(1, Mathf.RoundToInt((_speed + flat) * (1f + percent)));
            }
        }

        /// <summary> DEF effective (base + debuffs). </summary>
        public int EffectiveDef
        {
            get
            {
                if (_buffReceiver == null) return _def;
                var (percent, flat) = _buffReceiver.GetStatModifier(BuffStatType.DEF);
                return Mathf.Max(0, Mathf.RoundToInt((_def + flat) * (1f + percent)));
            }
        }

        /// <summary> True si le Rigidbody a encore une vélocité significative (ITurnParticipant). </summary>
        public bool IsMoving => _rb != null && _rb.velocity.sqrMagnitude > FINAL_STOP_THRESHOLD_SQR;

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
            ApplyBouncyMaterial();
            _buffReceiver = GetComponent<BuffReceiver>();
            if (_buffReceiver == null)
                _buffReceiver = gameObject.AddComponent<BuffReceiver>();
            // InitializeStats sera appelé par SetData() si spawné procéduralement
            // Sinon, on l'appelle ici si un EnemyData est déjà assigné dans l'éditeur
            if (enemyData != null)
                InitializeStats();
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
            if (_rb == null) return;

            // Dégâts à l'allié
            CharacterBall ally = collision.gameObject.GetComponent<CharacterBall>();
            if (ally != null)
            {
                int damage = CalculateDamage();

                // Spenda : échange de position instantané avant application des dégâts.
                CharacterBall actualTarget = ally;
                SpendaTeleportSystem spendaSystem = SpendaTeleportSystem.Instance;
                if (spendaSystem != null)
                {
                    CharacterBall swapped = spendaSystem.TryTeleportSwap(ally);
                    if (swapped != null)
                        actualTarget = swapped;
                }

                actualTarget.TakeDamage(damage);

                // Dégâts en retour des ronces de Ronss (sur l'ennemi qui frappe l'allié protégé).
                BuffReceiver allyBr = actualTarget.BuffReceiver;
                if (allyBr != null && allyBr.HasBuff("ronss_thorns"))
                {
                    var allyBuffs = allyBr.ActiveBuffs;
                    for (int j = 0; j < allyBuffs.Count; j++)
                    {
                        BuffData b = allyBuffs[j];
                        if (b == null) continue;
                        if (b.BuffId != "ronss_thorns" || b.Source == null) continue;

                        int thornsDamage = Mathf.Max(1, Mathf.RoundToInt(b.Source.EffectiveDef * 0.20f));
                        TakeDamage(thornsDamage);
                        break;
                    }
                }

                // Decay collision allié (plus de perte)
                _rb.velocity *= allyDecay;
            }
            else
            {
                // Decay collision mur (peu de perte, conserve momentum)
                _rb.velocity *= wallDecay;
            }
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

            // Absorption bouclier (ex. effets rares / corruption).
            if (_buffReceiver != null)
                damage = _buffReceiver.AbsorbDamageWithShield(damage);
            if (damage <= 0) return;

            // Réduction DEF de base (avec debuffs sur la DEF).
            int finalDamage = Mathf.Max(1, damage - EffectiveDef);

            if (_buffReceiver != null)
            {
                var (reductionPercent, reductionFlat) = _buffReceiver.GetStatModifier(BuffStatType.DamageReduction);
                if (reductionPercent != 0f || reductionFlat != 0f)
                    finalDamage = Mathf.Max(1, Mathf.RoundToInt((finalDamage - reductionFlat) * (1f - reductionPercent)));

                var (ampPercent, ampFlat) = _buffReceiver.GetStatModifier(BuffStatType.DamageAmplification);
                if (ampPercent != 0f || ampFlat != 0f)
                    finalDamage = Mathf.Max(1, Mathf.RoundToInt((finalDamage + ampFlat) * (1f + ampPercent)));
            }

            _currentHp = Mathf.Max(0, _currentHp - finalDamage);
            OnDamaged?.Invoke(finalDamage);

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

            // Récompense en Tals (avec multiplicateur si salle spéciale)
            if (_talsReward > 0 && RunManager.Instance != null)
            {
                int tals = _talsReward;
                if (SpecialRoomManager.Instance != null)
                    tals = Mathf.RoundToInt(tals * SpecialRoomManager.Instance.TalsMultiplier);
                RunManager.Instance.AddTals(tals);
            }

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

            _hasBeenLaunched = true;

            Vector2 dir = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
            _rb.AddForce(dir * force, ForceMode2D.Impulse);
            _launchSpeed = force / _rb.mass;
            _hasStoppedForThisLaunch = false;
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
                _rb.angularVelocity = 0f; // Reset aussi la rotation
                _hasBeenLaunched = false;
                _hasStoppedForThisLaunch = true;
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

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.drag = 0f;
            _rb.angularDrag = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        /// <summary>
        /// Applique le matériau rebondissant au BoxCollider2D (bounciness 1, friction 0 si non assigné).
        /// </summary>
        private void ApplyBouncyMaterial()
        {
            if (_boxCollider == null) return;

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

            _boxCollider.sharedMaterial = material;
        }

        private void TriggerStopped()
        {
            if (_hasStoppedForThisLaunch) return;
            _hasStoppedForThisLaunch = true;
            OnStopped?.Invoke();
        }
    }
}
