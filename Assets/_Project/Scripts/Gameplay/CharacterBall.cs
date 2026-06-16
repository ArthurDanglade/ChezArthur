using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Roguelike;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives.Handlers;

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
        private const float FINAL_STOP_THRESHOLD = 3.5f;
        private static readonly float FINAL_STOP_THRESHOLD_SQR = FINAL_STOP_THRESHOLD * FINAL_STOP_THRESHOLD;
        /// <summary> Alpha du sprite pendant le tour fantôme (Épée de l'Ancien Roi). </summary>
        private const float GHOST_VISUAL_ALPHA = 0.4f;
        /// <summary> Alpha de l'ombre au repos (enfant Shadow). </summary>
        private const float SHADOW_ALPHA = 0.35f;
        /// <summary> Diamètre de référence d'une bille en unités monde (visuel + collider). </summary>
        public const float ReferenceBallDiameter = 1.25f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Données du personnage")]
        [SerializeField] private CharacterData characterData;

        [Header("Ralentissement")]
        [Tooltip("% de vitesse conservé chaque frame (0.995 = perd 0.5%/frame). Plus haut = va plus loin.")]
        [SerializeField] private float velocityRetentionPerFrame = 0.97f;

        [Header("Decay aux collisions")]
        [Tooltip("Decay quand collision avec un MUR (peu de perte, conserve momentum).")]
        [SerializeField] private float wallDecay = 0.75f;
        [Tooltip("Decay quand collision avec un ENNEMI (plus de perte).")]
        [SerializeField] private float enemyDecay = 0.55f;
        [Tooltip("Decay quand collision avec un ALLIÉ (perte modérée, entre mur et ennemi).")]
        [SerializeField] private float allyDecay = 0.70f;

        [Header("Dégâts (collision ennemis)")]
        [Tooltip("Dégâts = (ATK × velocityFactor) × multiplicateur. velocityFactor = vélocité / 10. Min 1.")]
        [SerializeField] private float damageMultiplier = 1.5f;

        [Header("Physique (optionnel)")]
        [Tooltip("Si non assigné, un matériau bounciness=1 / friction=0 est créé en Awake.")]
        [SerializeField] private PhysicsMaterial2D bouncyMaterial;
        [Tooltip("Facteur du rayon collider monde (1 = plein diamètre, 0.9 = corps légèrement plus petit que le visuel).")]
        [SerializeField, Range(0.5f, 1f)] private float _colliderBodyFactor = 0.9f;

        [Header("Références (optionnel)")]
        [SerializeField] private TurnManager turnManager;

        [Header("Visuel (enfants du prefab)")]
        [SerializeField] private Transform _visual;
        [SerializeField] private Transform _shadow;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private SpriteRenderer _visualRenderer;
        private SpriteRenderer _shadowRenderer;
        private Color _defaultVisualColor = Color.white;
        private CharacterBallFloat _floatController;
        private Rigidbody2D _rb;
        private CircleCollider2D _circleCollider;
        private bool _hasStoppedForThisLaunch;
        private bool _hasBeenLaunched;
        private float _launchSpeed;
        private int _queuedExtraTurns;
        private int _currentHp;
        private int _trackedEffectiveMaxHp;
        private int _maxHp;
        private int _atk;
        private int _def;
        private int _speed;
        private bool _isDead;
        private Enemy _ghostKiller;
        private CharacterPassiveRuntime _passiveRuntime;
        // Référence vers le personnage possédé (spécialisations disponibles en combat).
        private OwnedCharacter _ownedCharacter;
        // Spécialisation active en combat (peut différer du hub après SwitchSpecInCombat).
        private SpecializationData _activeSpec;
        // Niveau utilisé pour recalculer les stats (ATK/DEF/Speed) au switch.
        private int _characterLevel = 1;
        private BuffReceiver _buffReceiver;
        private int _wallBounceCountThisLaunch;
        private int _enemyHitCountThisLaunch;
        private bool _isFrozenByHitStop;
        private bool _isArming;
        private float _armingIntensity;
        private Vector2 _hitStopCachedVelocity;
        private float _hitStopCachedAngular;
        private int _personalDisciplineStacks;
        private Dictionary<ValiseStatType, float> _personalValiseModifiers;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> True si la vélocité est au-dessus du seuil d'arrêt (personnage encore en mouvement). </summary>
        public bool IsMoving => _rb != null && _rb.velocity.sqrMagnitude > FINAL_STOP_THRESHOLD_SQR;
        /// <summary> Magnitude de la vélocité actuelle (diagnostic items). </summary>
        public float CurrentVelocity => _rb != null ? _rb.velocity.magnitude : 0f;
        /// <summary> Vélocité de référence au lancement (ratio Boule de Feu). </summary>
        public float LaunchSpeedThisLaunch => _launchSpeed;

        /// <summary> PV actuels (lecture seule). </summary>
        public int CurrentHp => _currentHp;
        /// <summary> PV max (lecture seule, avec bonus). </summary>
        public int MaxHp => EffectiveMaxHp;
        /// <summary> ATK de base (lecture seule). </summary>
        public int Atk => _atk;
        /// <summary> DEF de base (lecture seule). </summary>
        public int Def => _def;
        /// <summary> PV max de base avant bonus (lecture seule). </summary>
        public int BaseMaxHp => _maxHp;
        /// <summary> Vitesse de base avant bonus (lecture seule). </summary>
        public int BaseSpeed => _speed;
        /// <summary> Chance de critique de base (lecture seule). </summary>
        public float BaseCritChance => 0f;
        /// <summary> Multiplicateur de critique de base (lecture seule). </summary>
        public float BaseCritMultiplier => 2f;
        /// <summary> Multiplicateur de force de lancement de base (lecture seule). </summary>
        public float BaseLaunchForceMultiplier => 1f;
        /// <summary> Vitesse pour l'ordre des tours (avec bonus). </summary>
        public int Speed => EffectiveSpeed;
        /// <summary> Données du personnage assignées (lecture seule). </summary>
        public CharacterData Data => characterData;
        /// <summary> Spécialisation active en combat (lecture seule). </summary>
        public SpecializationData ActiveSpec => _activeSpec;
        /// <summary> Personnage possédé lié à cette balle (lecture seule). </summary>
        public OwnedCharacter OwnedCharacter => _ownedCharacter;
        /// <summary> Niveau du personnage utilisé pour les stats. </summary>
        public int CharacterLevel => _characterLevel;
        /// <summary> Buffs temporaires ciblés sur ce personnage (autres alliés, effets). </summary>
        public BuffReceiver BuffReceiver => _buffReceiver;
        /// <summary> True si le personnage est mort (état Die, hors fantôme). </summary>
        public bool IsDead => _isDead;
        /// <summary> True pendant le tour fantôme (Épée de l'Ancien Roi). </summary>
        public bool IsGhost { get; private set; }
        /// <summary> True pendant l'invisibilité (ex. passif Shado). </summary>
        public bool IsInvisible { get; private set; }
        /// <summary> False en état fantôme ou invisible : ignoré par l'IA ennemie. </summary>
        public bool IsTargetableByEnemies => !IsGhost && !IsInvisible;
        /// <summary> Enfant Visual (sprite personnage, scale local = 1 pour respiration future). </summary>
        public Transform Visual => _visual;
        /// <summary> SpriteRenderer du personnage (swap icône, ghost, invisibilité). </summary>
        public SpriteRenderer VisualRenderer => _visualRenderer;
        /// <summary> Enfant Shadow (ombre au sol). </summary>
        public Transform ShadowTransform => _shadow;
        /// <summary> SpriteRenderer de l'ombre. </summary>
        public SpriteRenderer ShadowRenderer => _shadowRenderer;
        /// <summary> True quand la bille est visuellement au repos (Slice 2 — respiration / ombre). </summary>
        public bool IsAtRestForVisual =>
            !IsMoving && !_isFrozenByHitStop && !IsDead && !IsGhost && !_isArming;
        /// <summary> True pendant la visée / tirée (drag actif). </summary>
        public bool IsArming => _isArming;
        /// <summary> Intensité d'armement 0–1 (distance de tirée normalisée). </summary>
        public float ArmingIntensity => _armingIntensity;
        /// <summary> True si le dernier dégât reçu était un dégât de contact (frappe ennemi). </summary>
        public bool LastDamageWasContact { get; private set; }
        /// <summary> Montant du dernier dégât effectivement reçu (lecture seule). </summary>
        public int LastDamageReceived { get; private set; }
        /// <summary> True si le personnage peut bouger (Rigidbody2D Dynamic). </summary>
        public bool IsMovable => _rb != null && _rb.bodyType == RigidbodyType2D.Dynamic;
        /// <summary> Nombre de rebonds murs du lancer courant. </summary>
        public int WallBounceCountThisLaunch => _wallBounceCountThisLaunch;
        /// <summary> Nombre d'ennemis touchés du lancer courant. </summary>
        public int EnemyHitCountThisLaunch => _enemyHitCountThisLaunch;

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
                // Bonus valises
                if (ValiseManager.Instance != null)
                    bonusPercent += ValiseManager.Instance.GetStatModifier(ValiseStatType.ATK);
                bonusPercent += GetPersonalValiseModifier(ValiseStatType.ATK);
                // Bonus items directs
                if (ItemManager.Instance != null)
                    bonusPercent += ItemManager.Instance.GetDirectStatModifier(ValiseStatType.ATK);
                // Bonus Porte-monnaie : proportionnel aux Tals en poche.
                if (ItemManager.Instance != null &&
                    ItemManager.Instance.HasItem("item_porte_monnaie"))
                {
                    if (Core.RunManager.Instance != null)
                    {
                        float talsRatio = Mathf.Clamp01(
                            Core.RunManager.Instance.TalsEarned / 1000f);
                        bonusPercent += talsRatio;
                    }
                }
                // Effet niv20 Valise Attaque : bonus dédié aux profils attaquants.
                if (ValiseManager.Instance != null)
                {
                    ValiseInstance attaque = ValiseManager.Instance.GetActiveValise("valise_attaque");
                    if (attaque != null && attaque.IsLevel20Unlocked &&
                        characterData != null &&
                        characterData.Role == CharacterRole.Attacker)
                    {
                        bonusPercent += 0.08f;
                    }
                }
                if (_passiveRuntime != null)
                    bonusPercent += _passiveRuntime.GetStatBonus(PassiveEffect.BuffATK);
                if (_buffReceiver != null)
                {
                    var (buffPercent, buffFlat) = _buffReceiver.GetStatModifier(BuffStatType.ATK);
                    bonusPercent += buffPercent;
                    bonusFlat += buffFlat;
                }
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
                // Bonus valises
                if (ValiseManager.Instance != null)
                    bonusPercent += ValiseManager.Instance.GetStatModifier(ValiseStatType.HP);
                // Bonus items directs
                if (ItemManager.Instance != null)
                    bonusPercent += ItemManager.Instance.GetDirectStatModifier(ValiseStatType.HP);
                if (_passiveRuntime != null)
                    bonusPercent += _passiveRuntime.GetStatBonus(PassiveEffect.BuffHP);
                if (_buffReceiver != null)
                {
                    var (buffPercent, buffFlat) = _buffReceiver.GetStatModifier(BuffStatType.HP);
                    bonusPercent += buffPercent;
                    bonusFlat += buffFlat;
                }
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
                // Bonus valises
                if (ValiseManager.Instance != null)
                    bonusPercent += ValiseManager.Instance.GetStatModifier(ValiseStatType.Speed);
                // Bonus items directs
                if (ItemManager.Instance != null)
                    bonusPercent += ItemManager.Instance.GetDirectStatModifier(ValiseStatType.Speed);
                if (_passiveRuntime != null)
                    bonusPercent += _passiveRuntime.GetStatBonus(PassiveEffect.BuffSpeed);
                if (_buffReceiver != null)
                {
                    var (buffPercent, buffFlat) = _buffReceiver.GetStatModifier(BuffStatType.Speed);
                    bonusPercent += buffPercent;
                    bonusFlat += buffFlat;
                }
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
                // Bonus valises
                if (ValiseManager.Instance != null)
                    bonusPercent += ValiseManager.Instance.GetStatModifier(ValiseStatType.DEF);
                bonusPercent += GetPersonalValiseModifier(ValiseStatType.DEF);
                // Bonus items directs
                if (ItemManager.Instance != null)
                    bonusPercent += ItemManager.Instance.GetDirectStatModifier(ValiseStatType.DEF);
                if (_passiveRuntime != null)
                    bonusPercent += _passiveRuntime.GetStatBonus(PassiveEffect.BuffDEF);
                if (_buffReceiver != null)
                {
                    var (buffPercent, buffFlat) = _buffReceiver.GetStatModifier(BuffStatType.DEF);
                    bonusPercent += buffPercent;
                    bonusFlat += buffFlat;
                }
                return Mathf.RoundToInt((_def + bonusFlat) * (1f + bonusPercent));
            }
        }

        /// <summary> CritChance effective (bonus + valises + items). </summary>
        public float EffectiveCritChance
        {
            get
            {
                float chance = 0f;
                if (BonusManager.Instance != null)
                {
                    var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.CritChance);
                    chance += percent + flat;
                }
                if (ValiseManager.Instance != null)
                    chance += ValiseManager.Instance.GetStatModifier(ValiseStatType.CritChance);
                if (ItemManager.Instance != null)
                    chance += ItemManager.Instance.GetDirectStatModifier(ValiseStatType.CritChance);
                return Mathf.Clamp(chance, 0f, 1f);
            }
        }

        /// <summary> CritMultiplier effectif. Base critique = x2. </summary>
        public float EffectiveCritMultiplier
        {
            get
            {
                float multiplier = 2f;
                if (BonusManager.Instance != null)
                {
                    var (percent, flat) = BonusManager.Instance.GetStatModifier(BonusStatType.CritMultiplier);
                    multiplier += percent + flat;
                }
                if (ValiseManager.Instance != null)
                    multiplier += ValiseManager.Instance.GetStatModifier(ValiseStatType.CritMultiplier);
                if (ValiseManager.Instance != null)
                {
                    ValiseInstance discipline =
                        ValiseManager.Instance.GetActiveValise("valise_discipline");
                    if (discipline != null && discipline.IsLevel20Unlocked)
                        multiplier += _personalDisciplineStacks * 0.003f;
                }
                return Mathf.Max(2f, multiplier);
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
                // Bonus valises
                if (ValiseManager.Instance != null)
                    bonusPercent += ValiseManager.Instance.GetStatModifier(ValiseStatType.LaunchForce);
                // Bonus items directs
                if (ItemManager.Instance != null)
                    bonusPercent += ItemManager.Instance.GetDirectStatModifier(ValiseStatType.LaunchForce);
                if (_passiveRuntime != null)
                    bonusPercent += _passiveRuntime.GetStatBonus(PassiveEffect.BuffLaunchForce);
                if (_buffReceiver != null)
                {
                    var (buffPercent, buffFlat) = _buffReceiver.GetStatModifier(BuffStatType.LaunchForce);
                    bonusPercent += buffPercent;
                    bonusFlat += buffFlat;
                }
                return 1f + bonusPercent + bonusFlat;
            }
        }

        /// <summary>
        /// Facteur de conservation de vélocité au rebond mur (1 = aucune perte, wallDecay = base).
        /// La valise Rebond augmente la conservation (plafonnée à 100 %).
        /// </summary>
        public float EffectiveWallDecay
        {
            get
            {
                float reboundBonus = 0f;
                if (ValiseManager.Instance != null)
                    reboundBonus += ValiseManager.Instance.GetStatModifier(ValiseStatType.ReboundDecay);
                if (ItemManager.Instance != null)
                    reboundBonus += ItemManager.Instance.GetDirectStatModifier(ValiseStatType.ReboundDecay);
                if (ItemManager.Instance != null &&
                    ItemManager.Instance.HasItem("item_ame_du_flipper"))
                    return 1f;
                return Mathf.Clamp(Mathf.Min(1f, wallDecay + reboundBonus), 0.1f, 1f);
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
        /// <summary> Déclenché quand ce personnage touche un ennemi (référence ennemi + dégâts infligés). </summary>
        public event Action<Enemy, int> OnHitEnemyWithRef;
        /// <summary> Déclenché quand ce personnage tue un ennemi. </summary>
        public event Action OnKillEnemy;
        /// <summary> Déclenché quand ce personnage tue un ennemi (avec référence et dégâts). </summary>
        public event Action<Enemy, int> OnKillEnemyWithRef;
        /// <summary> Déclenché quand ce personnage réalise un coup critique (préparation future). </summary>
        public event Action<Enemy, int> OnCriticalHit;
        /// <summary> Déclenché quand ce personnage est lancé. </summary>
        public event Action OnLaunched;
        /// <summary> Déclenché quand ce personnage touche un allié (lanceur). Paramètre : l'allié touché. </summary>
        public event Action<CharacterBall> OnHitAllyEvent;
        /// <summary> Déclenché quand ce personnage rebondit sur un mur. </summary>
        public event Action OnBounceWallEvent;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            ResolveVisualRefs();
            SetupRigidbody();
            SetupCircleCollider();
            InitializeStats();
            ApplyBouncyMaterial();
            _passiveRuntime = GetComponent<CharacterPassiveRuntime>();
            _buffReceiver = GetComponent<BuffReceiver>();
            if (_buffReceiver == null)
                _buffReceiver = gameObject.AddComponent<BuffReceiver>();
            _floatController = GetComponent<CharacterBallFloat>();
        }

        private void OnEnable()
        {
            if (_circleCollider == null)
                SetupCircleCollider();
            ChezArthur.Debugging.HitboxDebugOverlay.Register(_circleCollider);
        }

        private void OnDisable()
        {
            ChezArthur.Debugging.HitboxDebugOverlay.Unregister(_circleCollider);
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;
            if (_isFrozenByHitStop) return;
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
                Vector2 impactDir = _rb.velocity.sqrMagnitude > 0.01f
                    ? _rb.velocity.normalized
                    : Vector2.up;

                var (damage, isCrit) = CalculateDamage();
                float damageMult = _passiveRuntime != null ? _passiveRuntime.GetDamageMultiplierVsEnemy(enemy) : 1f;
                damage = Mathf.Max(1, Mathf.CeilToInt(damage * damageMult));
                enemy.TakeDamage(damage, isCrit);
                _enemyHitCountThisLaunch++;

                // Dégât de contact : l'allié perd 1 PV en frappant un ennemi.
                ApplyContactDamage();

                OnHitEnemy?.Invoke();
                OnHitEnemyWithRef?.Invoke(enemy, damage);
                if (isCrit)
                {
                    OnCriticalHit?.Invoke(enemy, damage);
                    ValiseInstance carnage = ValiseManager.Instance?.GetActiveValise("valise_carnage");
                    if (carnage != null && carnage.IsLevel20Unlocked && enemy.BuffReceiver != null)
                    {
                        enemy.BuffReceiver.AddBuff(new BuffData
                        {
                            BuffId = "carnage_lv20_def_debuff",
                            Source = this,
                            StatType = BuffStatType.DamageAmplification,
                            Value = 0.10f,
                            IsPercent = true,
                            RemainingTurns = 2,
                            RemainingCycles = -1,
                            UniqueGlobal = false,
                            UniquePerSource = false
                        });
                    }
                }
                if (_passiveRuntime != null)
                    _passiveRuntime.NotifyTriggerWithContext(PassiveTrigger.OnHitEnemy, hitEnemy: enemy, damageAmount: damage);

                if (enemy.IsDead)
                {
                    OnKillEnemy?.Invoke();
                    OnKillEnemyWithRef?.Invoke(enemy, damage);
                    if (_passiveRuntime != null)
                        _passiveRuntime.NotifyTriggerWithContext(PassiveTrigger.OnKillEnemy, hitEnemy: enemy, damageAmount: damage);
                    if (turnManager != null)
                        turnManager.PropagateAllyTrigger(this, PassiveTrigger.OnAllyKill);
                }

                if (_passiveRuntime != null)
                    _passiveRuntime.NotifyTriggerWithContext(PassiveTrigger.OnBounceEnemy, hitEnemy: enemy);

                if (PoisonTickSystem.Instance != null)
                    PoisonTickSystem.Instance.TryApplyCarrierPoison(this, enemy);

                if (FreezeSystem.Instance != null)
                    FreezeSystem.Instance.TryShatter(this, enemy);

                GoatSystem goatSystem = GetComponent<GoatSystem>();
                if (goatSystem != null)
                    goatSystem.NotifyEnemyHit();

                // Allié « éclairé » (Lumino) : débuff dégâts subis sur l'ennemi touché.
                if (_buffReceiver != null && _buffReceiver.HasBuff("lumino_eclaire_atk"))
                {
                    BuffReceiver enemyBr = enemy.BuffReceiver;
                    if (enemyBr != null && !enemyBr.HasBuff("lumino_eclaire_debuff"))
                    {
                        enemyBr.AddBuff(new BuffData
                        {
                            BuffId = "lumino_eclaire_debuff",
                            Source = this,
                            StatType = BuffStatType.DamageAmplification,
                            Value = 0.10f,
                            IsPercent = true,
                            RemainingTurns = 1,
                            RemainingCycles = -1,
                            UniquePerSource = true,
                            UniqueGlobal = false
                        });
                    }
                }

                // Allié porteur de feu (Kram) : applique la brûlure sur l'ennemi touché puis consomme le porteur.
                if (_buffReceiver != null && _buffReceiver.HasBuff("kram_fire_carrier"))
                {
                    BuffReceiver enemyBr = enemy.BuffReceiver;
                    if (enemyBr != null && !enemyBr.HasBuff("kram_burn"))
                    {
                        // Récupère la source (Kram) depuis le buff carrier.
                        CharacterBall kramSource = null;
                        IReadOnlyList<BuffData> carrierBuffs = _buffReceiver.ActiveBuffs;
                        for (int i = 0; i < carrierBuffs.Count; i++)
                        {
                            BuffData b = carrierBuffs[i];
                            if (b != null && b.BuffId == "kram_fire_carrier" && b.Source != null)
                            {
                                kramSource = b.Source;
                                break;
                            }
                        }

                        bool enhanced = false;
                        if (kramSource != null)
                        {
                            FireTrailSystem fts = kramSource.GetComponent<FireTrailSystem>();
                            if (fts != null)
                                enhanced = fts.IsEnhanced;
                        }

                        enemyBr.AddBuff(new BuffData
                        {
                            BuffId = "kram_burn",
                            Source = kramSource,
                            StatType = BuffStatType.DamageAmplification,
                            Value = enhanced ? 0.10f : 0f,
                            IsPercent = true,
                            RemainingTurns = -1,
                            RemainingCycles = -1,
                            UniquePerSource = false,
                            UniqueGlobal = true
                        });
                    }

                    // Consomme toujours le porteur (même si l'ennemi brûlait déjà).
                    _buffReceiver.RemoveBuffsById("kram_fire_carrier");
                }

                // Anty Cype : si cet allié est marqué scanner, scanne l'ennemi touché.
                if (AntyCypeScanSystem.Instance != null)
                    AntyCypeScanSystem.Instance.TryAllyScan(this, enemy);

                // Ardacula : lifesteal V1 basé sur l'ATK effective (pas les dégâts réels post-DEF ennemi).
                ArdaculaSystem ardaculaSystem = GetComponent<ArdaculaSystem>();
                if (ardaculaSystem != null)
                    ardaculaSystem.ApplyLifesteal(EffectiveAtk);

                // Ardacula : les 5 premiers rebonds (mur/ennemi) ignorent le decay.
                float impactSpeed = _rb.velocity.magnitude;
                Vector2 contactPt = collision.contactCount > 0
                    ? collision.GetContact(0).point
                    : (Vector2)transform.position;
                Vector2 contactNrm = collision.contactCount > 0
                    ? collision.GetContact(0).normal
                    : (_rb.velocity.sqrMagnitude > 0.01f ? -_rb.velocity.normalized : Vector2.up);

                if (ardaculaSystem != null && ardaculaSystem.ShouldBypassDecay())
                {
                    ardaculaSystem.RegisterBounce();
                }
                else
                {
                    // Effet niv20 Rebond : premier contact ennemi sans decay.
                    bool skipEnemyDecay = false;
                    ValiseInstance rebondValise =
                        ValiseManager.Instance?.GetActiveValise("valise_rebond");
                    if (rebondValise != null && rebondValise.IsLevel20Unlocked &&
                        _enemyHitCountThisLaunch == 1)
                    {
                        skipEnemyDecay = true;
                    }
                    if (!skipEnemyDecay)
                        _rb.velocity *= enemyDecay;
                }

                JuiceDirector.Instance?.PlayHitEnemy(this, enemy, damage, isCrit, contactPt, contactNrm, impactSpeed);
                enemy.OnHitReact(impactDir);
            }
            else
            {
                CharacterBall otherAlly = collision.gameObject.GetComponent<CharacterBall>();
                if (otherAlly != null && otherAlly != this && !otherAlly.IsDead)
                {
                    // OnCollisionEnter2D fire sur les deux billes : seul le participant lancé notifie.
                    if (turnManager != null && ReferenceEquals(turnManager.CurrentParticipant, this))
                    {
                        OnHitAllyEvent?.Invoke(otherAlly);

                        if (_passiveRuntime != null)
                            _passiveRuntime.NotifyTriggerWithContext(PassiveTrigger.OnHitAlly, hitAlly: otherAlly);

                        CharacterPassiveRuntime allyRuntime = otherAlly.GetComponent<CharacterPassiveRuntime>();
                        if (allyRuntime != null)
                            allyRuntime.NotifyTriggerWithContext(PassiveTrigger.OnHitBySelf, hitAlly: this);

                        _rb.velocity *= allyDecay;
                    }
                }
                else
                {
                    float wallImpactSpeed = _rb.velocity.magnitude;

                    if (_passiveRuntime != null)
                        _passiveRuntime.NotifyTrigger(PassiveTrigger.OnBounceWall);
                    OnBounceWallEvent?.Invoke();

                    // Voltrain : enregistre le point de contact du mur touché pour électrifier la zone.
                    ElectricWallSystem ews = GetComponent<ElectricWallSystem>();
                    if (ews != null && collision.contactCount > 0)
                    {
                        ContactPoint2D contact = collision.GetContact(0);
                        ews.RecordWallHit(contact.point, contact.normal);
                    }

                    ArdaculaSystem ardaculaSystem = GetComponent<ArdaculaSystem>();
                    if (ardaculaSystem != null && ardaculaSystem.ShouldBypassDecay())
                    {
                        ardaculaSystem.RegisterBounce();
                    }
                    else
                    {
                        float wallDecayToApply = EffectiveWallDecay;
                        // Effet niv20 LaunchForce : 3 premiers murs conservent 90% vélocité.
                        ValiseInstance launchForceValise =
                            ValiseManager.Instance?.GetActiveValise("valise_launchforce");
                        if (launchForceValise != null && launchForceValise.IsLevel20Unlocked &&
                            _wallBounceCountThisLaunch < 3)
                        {
                            wallDecayToApply = Mathf.Max(wallDecayToApply, 0.90f);
                        }
                        _rb.velocity *= wallDecayToApply;
                    }

                    _wallBounceCountThisLaunch++;

                    Vector2 wallContact = collision.contactCount > 0
                        ? collision.GetContact(0).point
                        : (Vector2)transform.position;
                    JuiceDirector.Instance?.PlayBounceWall(wallContact, wallImpactSpeed, _wallBounceCountThisLaunch);

                    GoatSystem goatSystem = GetComponent<GoatSystem>();
                    if (goatSystem != null)
                        goatSystem.TryApplyPostWallVelocityBoost();
                }
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Gèle la bille brièvement (hitstop) sans toucher Time.timeScale.
        /// </summary>
        public void ApplyHitStop(float duration)
        {
            if (_isFrozenByHitStop || _rb == null || duration <= 0f) return;
            StartCoroutine(HitStopRoutine(duration));
        }

        /// <summary>
        /// Lance le personnage dans la direction donnée avec la force donnée.
        /// </summary>
        public void Launch(Vector2 direction, float force)
        {
            if (_rb == null) return;
            if (force <= 0f) return;

            _hasBeenLaunched = true;

            // Déclenche OnSpecSwitch si la spé a changé depuis le début du tour.
            if (_passiveRuntime != null)
                _passiveRuntime.NotifySpecSwitchIfNeeded();

            Vector2 dir = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
            float effectiveForce = force * EffectiveLaunchForceMultiplier;
            _rb.AddForce(dir * effectiveForce, ForceMode2D.Impulse);
            JuiceDirector.Instance?.PlayLaunch((Vector2)transform.position, dir, _rb.velocity.magnitude);
            _launchSpeed = effectiveForce / _rb.mass;
            _hasStoppedForThisLaunch = false;
            _wallBounceCountThisLaunch = 0;
            _enemyHitCountThisLaunch = 0;
            OnLaunched?.Invoke();
            _floatController?.TriggerLaunchStretch(dir);

            if (_passiveRuntime != null)
                _passiveRuntime.NotifyTrigger(PassiveTrigger.OnLaunch);
        }

        /// <summary>
        /// Applique des dégâts au personnage. Déclenche OnDamaged ; si PV &lt;= 0, appelle Die().
        /// </summary>
        public void TakeDamage(int damage)
        {
            LastDamageWasContact = false;
            LastDamageReceived = 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ChezArthur.Debugging.DebugCheats.GodMode) return;
#endif
            if (IsGhost) return;
            if (damage <= 0) return;
            if (_isDead || _currentHp <= 0) return;

            if (_buffReceiver != null)
                damage = _buffReceiver.AbsorbDamageWithShield(damage);
            if (damage <= 0) return;

            // Applique la réduction de dégâts (DEF)
            int finalDamage = Mathf.Max(1, damage - EffectiveDef);

            if (_buffReceiver != null)
            {
                var (reductionPercent, reductionFlat) = _buffReceiver.GetStatModifier(BuffStatType.DamageReduction);
                finalDamage = Mathf.Max(1, Mathf.RoundToInt((finalDamage - reductionFlat) * (1f - reductionPercent)));

                var (ampPercent, ampFlat) = _buffReceiver.GetStatModifier(BuffStatType.DamageAmplification);
                finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * (1f + ampPercent) + ampFlat));
            }

            // Réduction additionnelle : zone de Zoneur (allié immobile dans le rayon).
            if (turnManager != null)
            {
                IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
                if (allies != null)
                {
                    for (int i = 0; i < allies.Count; i++)
                    {
                        if (allies[i] == null || allies[i].IsDead || allies[i] == this) continue;

                        ZoneSystem zone = allies[i].GetComponent<ZoneSystem>();
                        if (zone == null) continue;

                        float zoneReduction = zone.GetDamageReductionForAlly(this);
                        if (zoneReduction > 0f)
                        {
                            Debug.Log($"[Passif] Zoneur : {name} à l'arrêt dans la zone (-40%)");
                            finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * (1f - zoneReduction)));
                            break;
                        }
                    }
                }
            }

            // Leuk : pile ou face — modifie les dégâts aléatoirement avant application aux PV.
            LeukCoinFlipSystem coinFlip = GetComponent<LeukCoinFlipSystem>();
            if (coinFlip != null)
                finalDamage = coinFlip.ModifyDamage(finalDamage);

            // Don Costardo : les collègues mafieux absorbent d'abord les dégâts.
            if (DonCostardoSystem.Instance != null)
            {
                finalDamage = DonCostardoSystem.Instance.AbsorbDamageWithHenchman(this, finalDamage);
                if (finalDamage <= 0) return;
            }

            _currentHp = Mathf.Max(0, _currentHp - finalDamage);
            LastDamageReceived = finalDamage;
            OnDamaged?.Invoke(finalDamage);

            // Notifier tous les ennemis vivants qu'un allié
            // a pris des dégâts
            if (CombatManager.Instance != null)
                CombatManager.Instance.NotifyAllyDamaged(
                    this, finalDamage);

            if (_passiveRuntime != null)
                _passiveRuntime.NotifyTriggerWithContext(PassiveTrigger.OnTakeDamage, damageAmount: finalDamage);
            if (turnManager != null)
                turnManager.PropagateAllyTrigger(this, PassiveTrigger.OnAllyTakeDamage);

            DonCostardoSystem.Instance?.NotifyAllyDamaged(this);
            BrookeSystem brookeNotif = GetComponent<BrookeSystem>();
            if (brookeNotif != null)
                brookeNotif.OnOwnerTookDamage();

            if (_currentHp <= 0)
                HandleLethalDamage();
        }

        /// <summary>
        /// Inflige des dégâts directs non réductibles.
        /// Ignore la DEF, les réductions de dégâts et les boucliers.
        /// Utilisé par des mécaniques spéciales (ex. Néant Phase 3).
        /// </summary>
        public void TakeDamageUnreducible(int damage)
        {
            LastDamageWasContact = false;
            LastDamageReceived = 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ChezArthur.Debugging.DebugCheats.GodMode) return;
#endif
            if (IsGhost) return;
            if (damage <= 0) return;
            if (_isDead || _currentHp <= 0) return;

            _currentHp = Mathf.Max(0, _currentHp - damage);
            LastDamageReceived = damage;
            OnDamaged?.Invoke(damage);

            // Notifier tous les ennemis vivants qu'un allié
            // a pris des dégâts
            if (CombatManager.Instance != null)
                CombatManager.Instance.NotifyAllyDamaged(
                    this, damage);

            if (_passiveRuntime != null)
                _passiveRuntime.NotifyTriggerWithContext(PassiveTrigger.OnTakeDamage, damageAmount: damage);
            if (turnManager != null)
                turnManager.PropagateAllyTrigger(this, PassiveTrigger.OnAllyTakeDamage);

            DonCostardoSystem.Instance?.NotifyAllyDamaged(this);
            BrookeSystem brookeNotif = GetComponent<BrookeSystem>();
            if (brookeNotif != null)
                brookeNotif.OnOwnerTookDamage();

            if (_currentHp <= 0)
                HandleLethalDamage();
        }

        /// <summary>
        /// Inflige des dégâts purs (sans réduction DEF/bouclier). Utilisé pour les sacrifices.
        /// </summary>
        public void TakePureDamage(int amount)
        {
            LastDamageWasContact = false;
            LastDamageReceived = 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ChezArthur.Debugging.DebugCheats.GodMode) return;
#endif
            if (IsGhost) return;
            if (amount <= 0) return;
            if (_isDead || _currentHp <= 0) return;

            _currentHp = Mathf.Max(0, _currentHp - amount);
            LastDamageReceived = amount;
            OnDamaged?.Invoke(amount);

            if (_currentHp <= 0)
                HandleLethalDamage();
        }

        /// <summary>
        /// Applique le dégât de contact (1 PV) quand l'allié frappe un ennemi.
        /// </summary>
        private void ApplyContactDamage(int amount = 1)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ChezArthur.Debugging.DebugCheats.GodMode) return;
#endif
            if (IsGhost) return;
            if (amount <= 0 || _isDead || _currentHp <= 0) return;

            LastDamageWasContact = true;
            _currentHp = Mathf.Max(0, _currentHp - amount);
            LastDamageReceived = amount;
            OnDamaged?.Invoke(amount);

            if (_currentHp <= 0)
                HandleLethalDamage();
        }

        /// <summary>
        /// Tue le personnage : déclenche OnDeath et désactive le GameObject.
        /// </summary>
        public void Die()
        {
            if (_isDead || IsGhost) return;
            IsGhost = false;
            _ghostKiller = null;
            _isDead = true;

            if (CombatManager.Instance != null)
                CombatManager.Instance.NotifyAllyKilled(this);

            OnDeath?.Invoke();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Entre en état fantôme après un coup fatal (Épée de l'Ancien Roi).
        /// </summary>
        public void EnterGhost(Enemy killer)
        {
            if (IsGhost || _isDead) return;

            IsGhost = true;
            _ghostKiller = killer;
            _currentHp = 0;

            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            SetMovable(false);
            ApplyVisualPresentation();
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Résout le tour fantôme : revanche (revive) ou mort définitive selon le meurtrier.
        /// </summary>
        public void ResolveGhost()
        {
            if (!IsGhost) return;

            if (_ghostKiller == null || _ghostKiller.IsDead)
            {
                Debug.Log($"[Item] Épée de l'Ancien Roi : {Name} ressuscité à 1% PV");
                IsGhost = false;
                _ghostKiller = null;
                Revive(0.01f);
                return;
            }

            Debug.Log($"[Item] Épée de l'Ancien Roi : {Name} meurt définitivement (meurtrier vivant)");
            IsGhost = false;
            _ghostKiller = null;
            Die();
        }

        /// <summary>
        /// Ressuscite le personnage avec tous ses HP.
        /// </summary>
        public void Revive()
        {
            Revive(1f);
        }

        /// <summary>
        /// Ressuscite le personnage avec un pourcentage de ses HP max.
        /// </summary>
        public void Revive(float hpPercent)
        {
            IsGhost = false;
            _ghostKiller = null;
            _isDead = false;
            _currentHp = Mathf.Max(1, Mathf.RoundToInt(EffectiveMaxHp * Mathf.Clamp01(hpPercent)));
            RestoreVisuals();

            if (_circleCollider != null)
                _circleCollider.enabled = true;

            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            _hasBeenLaunched = false;
            _hasStoppedForThisLaunch = true;

            if (turnManager != null)
                turnManager.OnAllyRevived(this);

            OnStatsChanged?.Invoke();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Inflige un montant EXACT de dégâts (ignore DEF/boucliers, ignore God Mode),
        /// mais déclenche OnDamaged et la chaîne létale (passifs de survie, mort). Debug uniquement.
        /// </summary>
        public void DebugDamage(int amount)
        {
            if (amount <= 0 || _isDead || IsGhost || _currentHp <= 0) return;

            LastDamageWasContact = false;
            _currentHp = Mathf.Max(0, _currentHp - amount);
            LastDamageReceived = amount;
            OnDamaged?.Invoke(amount);

            if (_currentHp <= 0)
                HandleLethalDamage();
        }
#endif

        /// <summary>
        /// Soigne le personnage d'un montant donné (ne dépasse pas MaxHp).
        /// </summary>
        public void Heal(int amount)
        {
            if (IsGhost) return;
            if (amount <= 0) return;
            if (_isDead) return;

            // Applique le multiplicateur de salle spéciale (Happy Hour)
            if (SpecialRoomManager.Instance != null)
                amount = Mathf.RoundToInt(amount * SpecialRoomManager.Instance.HealMultiplier);

            if (_buffReceiver != null)
            {
                var (healPercent, healFlat) = _buffReceiver.GetStatModifier(BuffStatType.HealReceived);
                amount = Mathf.RoundToInt((amount + healFlat) * (1f + healPercent));
            }

            int previousHp = _currentHp;
            _currentHp = Mathf.Min(_currentHp + amount, EffectiveMaxHp);
            int actualHeal = _currentHp - previousHp;

            if (actualHeal > 0)
                OnHealed?.Invoke(actualHeal);
        }

        /// <summary>
        /// Définit un modificateur personnel de valise (additionné aux Effective* globaux).
        /// </summary>
        public void SetPersonalValiseModifier(ValiseStatType stat, float value)
        {
            if (_personalValiseModifiers == null)
                _personalValiseModifiers = new Dictionary<ValiseStatType, float>();
            _personalValiseModifiers[stat] = value;
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Stacks Discipline personnels (effet niv20 CritMulti).
        /// </summary>
        public void SetPersonalDisciplineStacks(int stacks)
        {
            _personalDisciplineStacks = Mathf.Max(0, stacks);
        }

        private float GetPersonalValiseModifier(ValiseStatType stat)
        {
            if (_personalValiseModifiers == null) return 0f;
            return _personalValiseModifiers.TryGetValue(stat, out float value) ? value : 0f;
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

            SyncTrackedEffectiveMaxHp();
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Synchronise le PV max effectif suivi (appelé à l'init et après refresh HP).
        /// </summary>
        public void SyncTrackedEffectiveMaxHp()
        {
            _trackedEffectiveMaxHp = EffectiveMaxHp;
        }

        /// <summary>
        /// Après ajout/amélioration de valise : augmente les PV courants du gain flat de PV max et notifie l'UI.
        /// </summary>
        public void ApplyEffectiveMaxHpGain()
        {
            if (_isDead) return;

            int newMax = EffectiveMaxHp;
            int delta = newMax - _trackedEffectiveMaxHp;
            _trackedEffectiveMaxHp = newMax;
            if (delta <= 0) return;

            _currentHp = Mathf.Min(_currentHp + delta, newMax);
            OnHealed?.Invoke(delta);
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
        /// Re-capture les bases du FloatController après normalisation Visual/Shadow (factory).
        /// </summary>
        public void RefreshFloatBase()
        {
            if (_floatController == null)
                _floatController = GetComponent<CharacterBallFloat>();
            _floatController?.CaptureBases();
        }

        /// <summary>
        /// Injecte la référence au TurnManager (appelé par la factory après l'instanciation).
        /// </summary>
        public void SetTurnManager(TurnManager tm)
        {
            turnManager = tm;
        }

        /// <summary>
        /// Retourne le TurnManager lié à cette balle (utile pour le contexte des passifs spéciaux).
        /// </summary>
        public TurnManager GetTurnManager()
        {
            return turnManager;
        }

        /// <summary>
        /// Associe le personnage possédé et le niveau (appelé après instanciation, ex. factory).
        /// Initialise la spé active depuis les données du joueur.
        /// </summary>
        public void SetOwnedCharacter(OwnedCharacter owned, int level)
        {
            _ownedCharacter = owned;
            _characterLevel = Mathf.Max(1, level);

            if (characterData != null && owned != null)
                _activeSpec = characterData.GetSpecialization(owned.GetSpecialization());

            InitializeStats();

            if (_passiveRuntime != null && _activeSpec != null && owned != null)
                _passiveRuntime.InitializeForRun(_activeSpec, _characterLevel, owned.GetSpecialization());
        }

        /// <summary>
        /// Change la spécialisation en combat : stats ATK/DEF/Speed, PV proportionnels, gel/dégel des passifs.
        /// </summary>
        public void SwitchSpecInCombat(int newSpecIndex)
        {
            if (characterData == null) return;
            if (newSpecIndex >= 0 && newSpecIndex >= characterData.GetSpecializationCount())
                return;

            SpecializationData newSpec = characterData.GetSpecialization(newSpecIndex);
            if (newSpec == null) return;

            int effectiveMaxBefore = EffectiveMaxHp;
            float hpRatio = effectiveMaxBefore > 0 ? (float)_currentHp / effectiveMaxBefore : 1f;

            int oldSpecIndex = _passiveRuntime != null ? _passiveRuntime.CurrentSpecIndex : -1;

            _activeSpec = newSpec;
            _atk = _activeSpec.GetAtkAtLevel(_characterLevel);
            _def = _activeSpec.GetDefAtLevel(_characterLevel);
            _speed = _activeSpec.GetSpeedAtLevel(_characterLevel);

            if (_passiveRuntime != null)
            {
                _passiveRuntime.SwitchSpec(_activeSpec, newSpecIndex, _characterLevel);
                if (newSpecIndex != oldSpecIndex)
                    _passiveRuntime.NotifySpecSwitch(newSpecIndex);
            }

            int effectiveMaxAfter = EffectiveMaxHp;
            _currentHp = Mathf.Max(1, Mathf.RoundToInt(hpRatio * effectiveMaxAfter));

            OnStatsChanged?.Invoke();

            string charName = characterData != null ? characterData.CharacterName : gameObject.name;
            Debug.Log($"[CharacterBall] SwitchSpec {charName} : {oldSpecIndex} -> {newSpecIndex}");
        }

        /// <summary>
        /// Enregistre la spé active au début du tour (appelé par le TurnManager).
        /// </summary>
        public void RecordSpecAtTurnStart()
        {
            if (_passiveRuntime != null)
                _passiveRuntime.RecordSpecAtTurnStart();
        }

        /// <summary>
        /// Notifie un éventuel switch de spé depuis le début du tour (ex. avant le lancer).
        /// </summary>
        public void NotifySpecSwitchIfNeeded()
        {
            if (_passiveRuntime != null)
                _passiveRuntime.NotifySpecSwitchIfNeeded();
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
        /// Ajoute un tour supplémentaire en file d'attente pour ce personnage.
        /// </summary>
        public void QueueExtraTurn(int count = 1)
        {
            if (count <= 0) return;
            _queuedExtraTurns += count;
        }

        /// <summary>
        /// Consomme un tour supplémentaire en attente, si disponible.
        /// </summary>
        public bool ConsumeQueuedExtraTurn()
        {
            if (_queuedExtraTurns <= 0) return false;
            _queuedExtraTurns--;
            return true;
        }

        /// <summary>
        /// Active ou désactive l'état invisible (ciblage IA + collisions gérées par le système appelant).
        /// </summary>
        public void SetInvisible(bool value)
        {
            IsInvisible = value;
            ApplyVisualPresentation();
        }

        /// <summary>
        /// Active ou met à jour l'état d'armement (visée drag) et l'intensité de charge.
        /// </summary>
        public void SetArming(bool isArming, float intensity01)
        {
            _isArming = isArming;
            _armingIntensity = Mathf.Clamp01(intensity01);
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

        private IEnumerator HitStopRoutine(float duration)
        {
            _isFrozenByHitStop = true;
            _hitStopCachedVelocity = _rb.velocity;
            _hitStopCachedAngular = _rb.angularVelocity;
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;

            yield return new WaitForSecondsRealtime(duration);

            _rb.velocity = _hitStopCachedVelocity;
            _rb.angularVelocity = _hitStopCachedAngular;
            _isFrozenByHitStop = false;
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

        private void SetupCircleCollider()
        {
            _circleCollider = GetComponent<CircleCollider2D>();
            if (_circleCollider == null)
                _circleCollider = gameObject.AddComponent<CircleCollider2D>();

            ApplyWorldColliderRadius();
        }

        /// <summary>
        /// Rayon collider en unités monde (racine scale = 1).
        /// </summary>
        private void ApplyWorldColliderRadius()
        {
            if (_circleCollider == null) return;
            _circleCollider.radius = ReferenceBallDiameter * 0.5f * _colliderBodyFactor;
        }

        /// <summary>
        /// Initialise les stats depuis CharacterData (ou valeurs par défaut si null).
        /// </summary>
        private void InitializeStats()
        {
            _isDead = false;

            if (characterData != null)
            {
                int level = Mathf.Max(1, _characterLevel);
                // PV max : toujours depuis CharacterData (profil commun, identique entre les spés).
                _maxHp = characterData.GetHpAtLevel(level);
                _currentHp = _maxHp;

                if (_activeSpec != null)
                {
                    _atk = _activeSpec.GetAtkAtLevel(level);
                    _def = _activeSpec.GetDefAtLevel(level);
                    _speed = _activeSpec.GetSpeedAtLevel(level);
                }
                else
                {
                    _atk = characterData.GetAtkAtLevel(level);
                    _def = characterData.GetDefAtLevel(level);
                    _speed = characterData.GetSpeedAtLevel(level);
                }
            }
            else
            {
                Debug.LogWarning("[CharacterBall] Aucun CharacterData assigné, utilisation des valeurs par défaut.", this);
                _maxHp = 100;
                _currentHp = _maxHp;
                _atk = 10;
                _def = 5;
                _speed = 50;
            }

            ApplyWorldColliderRadius();
            SyncTrackedEffectiveMaxHp();
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
        /// Réactive le GameObject et les renderers Visual / Shadow (Ticket Offert, Revive, etc.).
        /// </summary>
        private void RestoreVisuals()
        {
            gameObject.SetActive(true);

            if (_visual != null && !_visual.gameObject.activeSelf)
                _visual.gameObject.SetActive(true);
            if (_shadow != null && !_shadow.gameObject.activeSelf)
                _shadow.gameObject.SetActive(true);

            ApplyVisualPresentation();
        }

        /// <summary>
        /// Met en cache les SpriteRenderer des enfants Visual / Shadow (assignés dans le prefab).
        /// </summary>
        private void ResolveVisualRefs()
        {
            if (_visual != null)
                _visualRenderer = _visual.GetComponent<SpriteRenderer>();
            if (_shadow != null)
                _shadowRenderer = _shadow.GetComponent<SpriteRenderer>();

            if (_visualRenderer != null)
                _defaultVisualColor = _visualRenderer.color;
        }

        /// <summary>
        /// Applique l'état visuel selon fantôme / invisibilité / normal (Visual + Shadow).
        /// </summary>
        private void ApplyVisualPresentation()
        {
            if (IsInvisible)
            {
                if (_visualRenderer != null)
                    _visualRenderer.enabled = false;
                if (_shadowRenderer != null)
                    _shadowRenderer.enabled = false;
                return;
            }

            if (IsGhost)
            {
                if (_visualRenderer != null)
                {
                    _visualRenderer.enabled = true;
                    Color ghostColor = _defaultVisualColor;
                    ghostColor.a = GHOST_VISUAL_ALPHA;
                    _visualRenderer.color = ghostColor;
                }

                if (_shadowRenderer != null)
                    _shadowRenderer.enabled = false;
                return;
            }

            if (_visualRenderer != null)
            {
                _visualRenderer.enabled = true;
                _visualRenderer.color = _defaultVisualColor;
            }

            if (_shadowRenderer != null)
            {
                _shadowRenderer.enabled = true;
                _shadowRenderer.color = new Color(0f, 0f, 0f, SHADOW_ALPHA);
            }
        }

        /// <summary>
        /// Chaîne létale commune : passifs de survie, puis Épée de l'Ancien Roi, puis Die().
        /// </summary>
        private void HandleLethalDamage()
        {
            if (IsGhost) return;

            BrookeSystem brookeSystem = GetComponent<BrookeSystem>();
            if (brookeSystem != null && brookeSystem.TrySurviveLethal())
            {
                _currentHp = 1;
                return;
            }

            MorreVoeuxSystem morreSystem = GetComponent<MorreVoeuxSystem>();
            if (morreSystem != null && morreSystem.TryResurrect())
                return;

            if (RevvieRezSystem.Instance != null && RevvieRezSystem.Instance.TryRevvieResurrect(this))
                return;

            if (TryEnterGhostFromEpee())
                return;

            Die();
        }

        /// <summary>
        /// Épée de l'Ancien Roi : évite la mort immédiate et demande un tour fantôme différé.
        /// </summary>
        private bool TryEnterGhostFromEpee()
        {
            if (IsGhost || _isDead) return false;
            if (ItemManager.Instance == null) return false;

            ItemInstance sword = ItemManager.Instance.GetItemInstance("item_epee_ancien_roi");
            if (sword == null || sword.IsConsumed) return false;

            Enemy killer = null;
            if (turnManager != null)
            {
                ITurnParticipant current = turnManager.CurrentParticipant;
                if (current != null && !current.IsAlly)
                    killer = current as Enemy;
            }

            EnterGhost(killer);
            ItemManager.Instance.ConsumeItem(sword.Data.Id);

            if (RunManager.Instance != null)
                RunManager.Instance.RequestGhostTurn(this);
            else
                turnManager?.RequestGhostTurn(this);

            Debug.Log($"[Item] Épée de l'Ancien Roi : {Name} entre en état Fantôme");
            return true;
        }

        /// <summary>
        /// Calcule les dégâts à infliger : (ATK × velocityFactor) × damageMultiplier. velocityFactor = vélocité / 10. Min 1, arrondi au supérieur.
        /// </summary>
        private (int damage, bool isCrit) CalculateDamage()
        {
            float velocityFactor = _rb.velocity.magnitude / 10f;
            float raw = (EffectiveAtk * velocityFactor) * damageMultiplier;
            int baseDamage = Mathf.Max(1, Mathf.CeilToInt(raw));

            bool isCrit = UnityEngine.Random.value < EffectiveCritChance;
            if (isCrit)
            {
                ValiseInstance critiqueValise =
                    ValiseManager.Instance?.GetActiveValise("valise_critique");
                if (critiqueValise != null && critiqueValise.IsLevel20Unlocked)
                {
                    float megaCritChance = EffectiveCritChance / 3f;
                    bool isMegaCrit = UnityEngine.Random.value < megaCritChance;
                    if (isMegaCrit)
                    {
                        float megaMultiplier = EffectiveCritMultiplier * 2f;
                        int megaDamage = Mathf.CeilToInt(baseDamage * megaMultiplier);
                        Debug.Log($"[Crit] {Name} : x{megaMultiplier:0.##} → {megaDamage} dégâts");
                        return (megaDamage, true);
                    }
                }
                float critMultiplier = EffectiveCritMultiplier;
                int critDamage = Mathf.CeilToInt(baseDamage * critMultiplier);
                Debug.Log($"[Crit] {Name} : x{critMultiplier:0.##} → {critDamage} dégâts");
                return (critDamage, true);
            }

            return (baseDamage, false);
        }
    }
}
