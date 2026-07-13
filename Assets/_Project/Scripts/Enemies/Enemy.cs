using System;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives.Handlers;
using ChezArthur.Enemies.Passives;
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
        private const float FINAL_STOP_THRESHOLD = 3.5f;
        private static readonly float FINAL_STOP_THRESHOLD_SQR = FINAL_STOP_THRESHOLD * FINAL_STOP_THRESHOLD;
        private const string BOUNCY_MATERIAL_NAME = "BouncyMaterial";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Données de l'ennemi")]
        [SerializeField] private EnemyData enemyData;

        [Header("Ralentissement")]
        [Tooltip("% de vitesse conservé chaque frame (0.99 = perd 1%/frame). Plus haut = va plus loin.")]
        [SerializeField] private float velocityRetentionPerFrame = 0.96f;

        [Header("Decay aux collisions")]
        [Tooltip("Decay quand collision avec un MUR (peu de perte).")]
        [SerializeField] private float wallDecay = 0.65f;
        [Tooltip("Decay quand collision avec un ALLIÉ (plus de perte).")]
        [SerializeField] private float allyDecay = 0.50f;

        [Header("Physique")]
        [SerializeField] private PhysicsMaterial2D bouncyMaterial;

        [Header("Dégâts (collision alliés)")]
        [Tooltip("Dégâts = (ATK × velocityFactor) × multiplicateur. velocityFactor = vélocité / 10. Min 1.")]
        [SerializeField] private float damageMultiplier = 1.2f;

        [Header("Visuel (enfant du prefab)")]
        [SerializeField] private Transform _visual;
        [SerializeField] private SpriteRenderer _visualRenderer;
        [SerializeField] private EnemyHitReaction _hitReaction;
        [SerializeField] private EnemyIdleMotion _idleMotion;

        [Header("Visuel combat")]
        [Tooltip("Si activé, la taille visuelle est normalisée : dimension max = max(ColliderWidth, ColliderHeight) × multiplicateur × facteur.")]
        [SerializeField] private bool normalizeCombatVisualScale = true;
        [Tooltip("Débordement visuel par rapport à la hitbox (1 = le sprite épouse exactement la box).")]
        [SerializeField] private float combatVisualScaleFactor = 1.1f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private BoxCollider2D _boxCollider;
        private SpriteRenderer _spriteRenderer;
        private float _sizeMultiplier = 1f;
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
        private EnemyPassiveRuntime _enemyPassiveRuntime;
        private EnemyShieldSystem _shieldSystem;
        private float _launchForceBonusPercent;
        private bool _damageImmuneUntilOwnerTurnStart;
        private bool _lastDamageWasCrit;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Points de vie actuels (lecture seule). </summary>
        public int CurrentHp => _currentHp;
        /// <summary> PV max (lecture seule). </summary>
        public int MaxHp => _maxHp;
        /// <summary> PV max de base avant scaling. </summary>
        public int BaseMaxHp => _baseMaxHp;
        /// <summary> ATK de base (lecture seule). </summary>
        public int Atk => _atk;
        /// <summary> ATK de base avant scaling. </summary>
        public int BaseAtk => _baseAtk;
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

        /// <summary> True si le dernier TakeDamage reçu était un coup critique. </summary>
        public bool LastDamageWasCrit => _lastDamageWasCrit;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand l'ennemi prend des dégâts. Paramètre : dégâts reçus. </summary>
        public event Action<int> OnDamaged;

        /// <summary> Déclenché quand l'ennemi meurt. </summary>
        public event Action OnDeath;

        /// <summary> Déclenché quand un boss meurt (toutes causes : collision, DOT, réflexion, etc.). </summary>
        public static event Action OnBossDefeated;

        /// <summary>
        /// Déclenché juste avant le crédit logique des Tals à la mort.
        /// Position monde de la mort + montant final (après multiplicateurs Happy Hour / valise Difficulté).
        /// Cosmétique uniquement — consommé par TalsDropSystem.
        /// </summary>
        public static event Action<Vector3, int> OnTalsDropped;

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
            _enemyPassiveRuntime = GetComponent<EnemyPassiveRuntime>();
            _shieldSystem = GetComponent<EnemyShieldSystem>();
            if (_idleMotion == null)
                _idleMotion = GetComponent<EnemyIdleMotion>();
            // InitializeStats sera appelé par SetData() si spawné procéduralement
            // Sinon, on l'appelle ici si un EnemyData est déjà assigné dans l'éditeur
            if (enemyData != null)
            {
                InitializeStats();
                RefreshCombatVisual();
            }
        }

        private void OnEnable()
        {
            if (_boxCollider == null)
                SetupBoxCollider();
            ChezArthur.Debugging.HitboxDebugOverlay.Register(_boxCollider);
        }

        private void OnDisable()
        {
            ChezArthur.Debugging.HitboxDebugOverlay.Unregister(_boxCollider);
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
                // Shado invisible : l'ennemi traverse sans dégâts ni ralentissement.
                ShadoStealthSystem shadoStealth = ally.GetComponent<ShadoStealthSystem>();
                if (shadoStealth != null && shadoStealth.IsInvisible)
                    return;

                // Daupou : propulsion dans la direction opposée quand l'ennemi le percute.
                DaupouPropulsionSystem daupouSystem = ally.GetComponent<DaupouPropulsionSystem>();
                if (daupouSystem != null && _rb != null)
                    daupouSystem.TryPropulse(_rb.velocity);

                int damage = CalculateDamage();

                // Troplin : spin défensif/offensif quand il est immobile.
                TroplinSystem troplinSystem = ally.GetComponent<TroplinSystem>();
                if (troplinSystem != null)
                    damage = troplinSystem.ModifyIncomingDamageFromEnemy(damage, this);

                // Spenda : échange de position instantané avant application des dégâts.
                CharacterBall actualTarget = ally;
                SpendaTeleportSystem spendaSystem = SpendaTeleportSystem.Instance;
                if (spendaSystem != null)
                {
                    CharacterBall swapped = spendaSystem.TryTeleportSwap(ally);
                    if (swapped != null)
                        actualTarget = swapped;
                }

                GoatSystem goatSystem = actualTarget.GetComponent<GoatSystem>();
                if (goatSystem != null)
                    damage = goatSystem.ModifyIncomingCollisionDamageFromEnemy(damage, this);

                actualTarget.TakeDamage(damage);
                ValiseEventBridge.Instance?.TryRenvoiFromEnemyAttack(
                    this, actualTarget, actualTarget.LastDamageReceived);

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
            float velocityFactor = Mathf.Min(_rb.velocity.magnitude / 10f, 1.5f);
            float raw = (EffectiveAtk * velocityFactor) * damageMultiplier;
            return Mathf.Max(1, Mathf.CeilToInt(raw));
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Estime les dégâts finaux après mitigation (sans appliquer ni déclencher d'events).
        /// </summary>
        public int ComputeMitigatedDamage(int damage)
        {
            if (damage <= 0 || _isDead) return 0;
            if (_damageImmuneUntilOwnerTurnStart) return 0;

            if (_buffReceiver != null)
                damage = _buffReceiver.AbsorbDamageWithShield(damage);
            if (damage <= 0) return 0;

            if (_shieldSystem != null)
                damage = _shieldSystem.AbsorbDamage(damage);
            if (damage <= 0) return 0;

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

            return finalDamage;
        }

        /// <summary>
        /// True si les dégâts bruts létaux tueront l'ennemi (hors résurrection passive).
        /// </summary>
        public bool WouldDieFromDamage(int rawDamage)
        {
            int finalDamage = ComputeMitigatedDamage(rawDamage);
            return finalDamage > 0 && _currentHp - finalDamage <= 0;
        }

        /// <summary>
        /// Applique des dégâts à l'ennemi. Déclenche OnDamaged ; si PV &lt;= 0, appelle Die().
        /// </summary>
        public void TakeDamage(int damage, bool isCrit = false)
        {
            _lastDamageWasCrit = isCrit;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ChezArthur.Debugging.DebugCheats.OneShot)
                damage = 100000000;
#endif
            int finalDamage = ComputeMitigatedDamage(damage);
            if (finalDamage <= 0) return;
            if (_isDead) return;

            _currentHp = Mathf.Max(0, _currentHp - finalDamage);
            OnDamaged?.Invoke(finalDamage);

            if (_currentHp <= 0 && _enemyPassiveRuntime != null && _enemyPassiveRuntime.TryConsumeResurrection(out int reviveHp))
                _currentHp = reviveHp;

            if (_enemyPassiveRuntime != null)
                _enemyPassiveRuntime.NotifyHpChanged(_currentHp, _maxHp);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_currentHp <= 0 && TryBlockDebugDeath())
                return;
#endif

            if (_currentHp <= 0)
                Die();
        }

        /// <summary>
        /// Canal de dégâts fixes (renvois, DOT, explosions).
        /// Ignore boucliers, DEF, DamageReduction et DamageAmplification.
        /// </summary>
        public void TakePureDamage(int amount)
        {
            if (amount <= 0) return;
            if (_isDead) return;
            if (_damageImmuneUntilOwnerTurnStart)
                return;

            int finalDamage = amount;
            _currentHp = Mathf.Max(0, _currentHp - finalDamage);
            OnDamaged?.Invoke(finalDamage);

            if (_currentHp <= 0 && _enemyPassiveRuntime != null && _enemyPassiveRuntime.TryConsumeResurrection(out int reviveHp))
                _currentHp = reviveHp;

            if (_enemyPassiveRuntime != null)
                _enemyPassiveRuntime.NotifyHpChanged(_currentHp, _maxHp);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_currentHp <= 0 && TryBlockDebugDeath())
                return;
#endif

            if (_currentHp <= 0)
                Die();
        }

        /// <summary>
        /// Soigne l'ennemi (PV plafonnés au max).
        /// </summary>
        public void Heal(int amount)
        {
            if (amount <= 0 || _isDead) return;
            _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
            if (_enemyPassiveRuntime != null)
                _enemyPassiveRuntime.NotifyHpChanged(_currentHp, _maxHp);
        }

        /// <summary>
        /// Bonus additif sur la force de lancement (ex. 0.2f = +20 %). Cumulatif jusqu'à reset explicite.
        /// </summary>
        public void AddLaunchForceBonus(float percent)
        {
            _launchForceBonusPercent += percent;
        }

        /// <summary>
        /// Immunise aux dégâts jusqu'au prochain début de tour de cet ennemi (voir TurnManager).
        /// </summary>
        public void GrantDamageImmunityForOneEnemyTurn()
        {
            _damageImmuneUntilOwnerTurnStart = true;
        }

        /// <summary>
        /// Appelé au début du tour de cet ennemi pour lever l'immunité « un tour ».
        /// </summary>
        public void ClearDamageImmunityAtTurnStart()
        {
            _damageImmuneUntilOwnerTurnStart = false;
        }

        /// <summary>
        /// Tue l'ennemi : déclenche OnDeath et désactive le GameObject.
        /// </summary>
        public void Die()
        {
            if (_isDead) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (TryBlockDebugDeath())
                return;
#endif

            FrigorColdFieldSystem.TryIceShardsOnFrozenDeath(this);

            _isDead = true;

            // Récompense en Tals (avec multiplicateur si salle spéciale)
            if (_talsReward > 0 && RunManager.Instance != null)
            {
                int tals = _talsReward;
                if (SpecialRoomManager.Instance != null)
                    tals = Mathf.RoundToInt(tals * SpecialRoomManager.Instance.TalsMultiplier);
                if (ValiseManager.Instance != null)
                {
                    ValiseInstance difficulte = ValiseManager.Instance.GetActiveValise("valise_difficulte");
                    if (difficulte != null)
                    {
                        float talsRate = difficulte.GetTotalStatValue();
                        if (talsRate > 0f)
                        {
                            int before = tals;
                            tals = Mathf.RoundToInt(tals * (1f + talsRate));
                            Debug.Log($"[Valise] Difficulté Tals : {before} → {tals} (× {1f + talsRate:0.###})");
                        }
                    }
                }
                OnTalsDropped?.Invoke(transform.position, tals);
                RunManager.Instance.AddTals(tals);
            }

            OnDeath?.Invoke();

            if (Data != null && Data.EnemyType == EnemyType.Boss)
                OnBossDefeated?.Invoke();

            JuiceDirector.Instance?.PlayKill(transform.position);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Assigne des données ennemi à la volée et réinitialise les stats (pour le spawn procédural).
        /// </summary>
        public void SetData(EnemyData data)
        {
            _runtimeEnemyData = data;
            InitializeStats();
            RefreshCombatVisual();
        }

        /// <summary>
        /// Résout le sprite de combat depuis EnemyData, normalise l'échelle racine et synchronise la hitbox.
        /// </summary>
        public void RefreshCombatVisual()
        {
            EnsureSpriteRenderer();

            Sprite resolved = Data != null ? Data.CombatSprite : null;
            string source;

            if (resolved != null)
            {
                if (_spriteRenderer != null)
                    _spriteRenderer.sprite = resolved;
                source = "combatSprite";
            }
            else
            {
                Debug.LogWarning(
                    $"[CombatVisual] {Name} : combatSprite manquant → placeholder prefab conservé",
                    this);
                source = "placeholder";
            }

            Sprite spriteAffiche = _spriteRenderer != null ? _spriteRenderer.sprite : null;
            float effW = (Data != null ? Data.ColliderWidth : 1f) * _sizeMultiplier;
            float effH = (Data != null ? Data.ColliderHeight : 1f) * _sizeMultiplier;

            if (spriteAffiche == null)
            {
                ApplyColliderSize();
                Debug.LogWarning($"[CombatVisual] {Name} : aucun sprite affiché", this);
                return;
            }

            float s = transform.localScale.x;

            if (normalizeCombatVisualScale)
            {
                Vector2 boundsSize = spriteAffiche.bounds.size;
                float boundsMax = Mathf.Max(boundsSize.x, boundsSize.y);
                if (boundsMax > 0.0001f)
                {
                    float targetCollider = Data != null
                        ? Mathf.Max(Data.ColliderWidth, Data.ColliderHeight)
                        : 1f;
                    float target = targetCollider * _sizeMultiplier * combatVisualScaleFactor;
                    s = target / boundsMax;
                    transform.localScale = new Vector3(s, s, 1f);
                }
            }

            ApplyColliderSize();

            Debug.Log(
                $"[CombatVisual] {Name} : sprite={spriteAffiche.name} (source={source}), scale={s:F2}, boîte monde={effW:F2}×{effH:F2}",
                this);
        }

        /// <summary>
        /// SEUL point autorisé pour moduler la taille d'un ennemi (boss, horde...).
        /// Remplace toute assignation brute de localScale, qui désynchroniserait visuel et hitbox.
        /// </summary>
        public void SetSizeMultiplier(float multiplier)
        {
            _sizeMultiplier = Mathf.Clamp(multiplier, 0.25f, 3f);
            RefreshCombatVisual();
        }

        /// <summary>
        /// Réaction visuelle au coup (knockback + squash sur le Visual).
        /// </summary>
        public void OnHitReact(Vector2 hitDirection, float intensity = 1f)
        {
            _hitReaction?.Trigger(hitDirection, intensity);
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
        /// Applique un scaling additif sur les stats déjà scalées.
        /// </summary>
        public void ApplyAdditionalScaling(float bonusPercent)
        {
            if (bonusPercent <= 0f) return;
            _maxHp = Mathf.RoundToInt(_maxHp * (1f + bonusPercent));
            _currentHp = _maxHp;
            _atk = Mathf.RoundToInt(_atk * (1f + bonusPercent));
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
            float launchMultiplier = 1f + _launchForceBonusPercent;
            if (_buffReceiver != null)
            {
                var (buffPercent, buffFlat) = _buffReceiver.GetStatModifier(BuffStatType.LaunchForce);
                launchMultiplier += buffPercent + buffFlat;
            }

            float boostedForce = force * launchMultiplier;
            if (_buffReceiver != null && _buffReceiver.HasBuff("entoile"))
                Debug.Log($"[Item] Lanceur de Toile : lancer de {Name} réduit (-30%)");

            _rb.AddForce(dir * boostedForce, ForceMode2D.Impulse);
            _launchSpeed = boostedForce / _rb.mass;
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
            EnsureSpriteRenderer();
        }

        private void EnsureSpriteRenderer()
        {
            if (_spriteRenderer != null)
                return;

            if (_visualRenderer != null)
                _spriteRenderer = _visualRenderer;
            else if (_visual != null)
                _spriteRenderer = _visual.GetComponent<SpriteRenderer>();
            else
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        /// <summary>
        /// Compense l'échelle du transform pour que la boîte MONDE = dimensions data × multiplicateur, indépendamment du visuel.
        /// </summary>
        private void ApplyColliderSize()
        {
            if (_boxCollider == null)
                return;

            float effW = (Data != null ? Data.ColliderWidth : 1f) * _sizeMultiplier;
            float effH = (Data != null ? Data.ColliderHeight : 1f) * _sizeMultiplier;
            float s = Mathf.Max(Mathf.Abs(transform.localScale.x), 0.0001f);
            _boxCollider.size = new Vector2(effW / s, effH / s);
        }

        /// <summary>
        /// Initialise les stats depuis EnemyData (ou valeurs par défaut si null).
        /// </summary>
        private void InitializeStats()
        {
            _isDead = false;
            _launchForceBonusPercent = 0f;
            _damageImmuneUntilOwnerTurnStart = false;
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
                ApplyColliderSize();
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
                ApplyColliderSize();
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Cheat debug : bloque la mort de l'ennemi à 1 PV.
        /// </summary>
        private bool TryBlockDebugDeath()
        {
            if (!ChezArthur.Debugging.DebugCheats.EnemyGodMode)
                return false;

            _currentHp = 1;
            if (_enemyPassiveRuntime != null)
                _enemyPassiveRuntime.NotifyHpChanged(_currentHp, _maxHp);
            return true;
        }
#endif

        /// <summary>
        /// Termine le tour sans lancement (aucune cible valide, ex. seul fantôme vivant).
        /// </summary>
        public void CompleteTurnWithoutLaunch()
        {
            TriggerStopped();
        }
    }
}
