using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Audio;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay
{
        /// <summary>
        /// Applique les effets ennemis et alliés pendant la Rupture de pression (Gates 5–6).
        /// Ennemis : buffs + aura au sol (niveau ombre) + traits montants (aura de puissance).
        /// Alliés : salves DEF, soin SUP, bonus ATK ; refus du switch de spé joueur.
        /// Les VFX ennemis montent en douceur (délai + durée de reveal) pour coller à l'annonce UI.
        /// </summary>
    public class RuptureEffectsSystem : MonoBehaviour
    {
        private const string RuptureAtkBuffId = "rupture_atk";
        private const string RuptureResistanceBuffId = "rupture_resistance";
        private const string RuptureLaunchForceBuffId = "rupture_launchforce";
        private const string RuptureDefShieldBuffId = "rupture_def_shield";
        private const string RuptureAtkAllyBuffId = "rupture_atk_ally";
        /// <summary> Comme CharacterBall : ombre sous le visuel de combat. </summary>
        private const int GroundAuraSortingOrder = 8;
        private const int MaxStreaksParticles = 64;

        private static RuptureEffectsSystem _instance;

        /// <summary> Instance unique de la scène courante. </summary>
        public static RuptureEffectsSystem Instance => _instance;

        [Header("Buffs ennemis (calibrage Gate 8)")]
        [SerializeField] private float atkBonusPercent = 0.25f;
        [SerializeField] private float damageReductionPercent = 0.25f;
        [SerializeField] private float launchForceBonusPercent = 0.30f;

        [Header("Aura rupture (sol / ombre)")]
        [Tooltip("Variants d'aura. Remplir via Chez Arthur/UI/Charger auras rupture (6).")]
        [SerializeField] private List<RuptureAuraVariant> auraVariants = new List<RuptureAuraVariant>();
        [SerializeField] private int activeAuraVariantIndex;
        [SerializeField] private float auraFps = 12f;
        [Tooltip("Largeur aura ≈ largeur du sprite × ce facteur.")]
        [SerializeField] private float auraFitPadding = 1.35f;
        [Tooltip("Aplatissement vertical style ombre (1 = cercle, 0.5 = ellipse).")]
        [Range(0.3f, 1f)]
        [SerializeField] private float groundFlatten = 0.55f;
        [Tooltip("Décalage vertical monde depuis le bas du sprite (pieds).")]
        [SerializeField] private float groundYOffset = 0.02f;
        [Tooltip("Fallback si aucune variante : sprite unique (sinon glow procédural).")]
        [SerializeField] private Sprite haloSpriteOverride;
        [SerializeField] private Color haloColor = Color.white;
        [SerializeField] private float pulseSpeed = 2.2f;
        [SerializeField] private float pulseAmplitude = 0.08f;

        [Header("Apparition VFX (chorégraphie)")]
        [Tooltip("Délai avant montée des auras — aligné sur le pop du bandeau Rupture.")]
        [SerializeField] private float visualRevealDelay = 0.15f;
        [SerializeField] private float visualRevealDuration = 0.45f;
        [Tooltip("Échelle de départ de l'aura sol pendant le reveal (0–1).")]
        [SerializeField] [Range(0.2f, 1f)] private float visualRevealStartScale = 0.4f;

        [Header("Aura de puissance (traits montants)")]
        [Tooltip("Prefab ParticleSystem. Si null, un système runtime est créé.")]
        [SerializeField] private ParticleSystem powerStreaksPrefab;
        [SerializeField] private bool enablePowerStreaks = true;
        [SerializeField] private float streaksRate = 22f;
        [SerializeField] private float streaksSpeedMin = 0.9f;
        [SerializeField] private float streaksSpeedMax = 2.1f;
        [SerializeField] private float streaksLifetime = 0.55f;
        [SerializeField] private float streaksSize = 0.07f;
        [Tooltip("Rayon d'émission ≈ largeur du sprite × ce facteur.")]
        [SerializeField] private float streaksRadiusFactor = 0.28f;
        [SerializeField] private Color streaksFallbackColor = new Color(1f, 0.35f, 0.15f, 0.9f);

        [Header("Effets alliés (calibrage Gate 8)")]
        [Tooltip("Bouclier accordé à toute l'équipe = % des HP max du DEF qui joue sa salve.")]
        [SerializeField] private float defShieldPercent = 0.15f;
        [Tooltip("Soin d'équipe unique par rupture = % des HP max de chaque allié.")]
        [SerializeField] private float supHealPercent = 0.15f;
        [Tooltip("Bonus ATK cumulatif par allié ATK vivant (spé active), appliqué au perso ATK en tour.")]
        [SerializeField] private float atkPerAtkAllyPercent = 0.10f;

        [Header("Audio (null toléré)")]
        [SerializeField] private AudioClip defSalveClip;
        [SerializeField] private AudioClip supHealClip;
        [SerializeField] private AudioClip switchDeniedClip;

        private TurnManager _turnManager;
        private bool _ruptureActive;
        private bool _supHealDone;
        private readonly HashSet<CharacterBall> _defSalveDone = new HashSet<CharacterBall>();
        private Transform _haloPoolContainer;
        private Transform _streaksPoolContainer;
        private Sprite _runtimeHaloSprite;
        private Texture2D _runtimeHaloTexture;
        private Material _runtimeStreaksMaterial;

        private readonly List<SpriteRenderer> _haloPool = new List<SpriteRenderer>(16);
        private readonly List<ParticleSystem> _streaksPool = new List<ParticleSystem>(16);
        private float _auraFrameTimer;
        private int _auraFrameIndex;

        // Reveal VFX piloté par Update (plus fiable qu'une coroutine seule).
        private float _visualReveal = 1f;
        private bool _visualRevealRunning;
        private float _visualRevealDelayLeft;
        private float _visualRevealElapsed;
        private float _visualRevealDurationActive;

        private struct TrackedEnemyEntry
        {
            public Enemy Enemy;
            public SpriteRenderer HaloRenderer;
            public ParticleSystem PowerStreaks;
        }

        private readonly List<TrackedEnemyEntry> _trackedEnemies = new List<TrackedEnemyEntry>(16);

        /// <summary> Variante d'aura animée (frames Sanctum Pixel). </summary>
        [Serializable]
        public class RuptureAuraVariant
        {
            public string id;
            public Sprite[] frames;
        }

        /// <summary> Déclenché quand le joueur tente un switch de spé refusé pendant la Rupture (Gate 7). </summary>
        public event Action OnSpecSwitchDenied;

        public int AuraVariantCount => auraVariants != null ? auraVariants.Count : 0;

        public int ActiveAuraVariantIndex => activeAuraVariantIndex;

        public string ActiveAuraVariantId
        {
            get
            {
                RuptureAuraVariant v = GetActiveVariant();
                return v != null && !string.IsNullOrEmpty(v.id) ? v.id : "(aucune)";
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;

            var poolGo = new GameObject("RuptureHaloPool");
            poolGo.transform.SetParent(transform, false);
            _haloPoolContainer = poolGo.transform;

            var streaksPoolGo = new GameObject("RupturePowerStreaksPool");
            streaksPoolGo.transform.SetParent(transform, false);
            _streaksPoolContainer = streaksPoolGo.transform;

            if (haloSpriteOverride == null && !HasAnimatedAura())
                CreateProceduralHaloSprite();
        }

        private void OnDestroy()
        {
            Cleanup();

            if (_instance == this)
                _instance = null;

            if (_runtimeHaloSprite != null)
                Destroy(_runtimeHaloSprite);

            if (_runtimeHaloTexture != null)
                Destroy(_runtimeHaloTexture);

            if (_runtimeStreaksMaterial != null)
                Destroy(_runtimeStreaksMaterial);
        }

        private void Update()
        {
            TickVisualReveal();

            if (!_ruptureActive)
                return;

            Sprite[] frames = GetActiveFrames();
            bool animated = frames != null && frames.Length > 0;

            if (animated && auraFps > 0f)
            {
                _auraFrameTimer += Time.deltaTime;
                float frameDuration = 1f / auraFps;
                while (_auraFrameTimer >= frameDuration)
                {
                    _auraFrameTimer -= frameDuration;
                    _auraFrameIndex++;
                    if (_auraFrameIndex >= frames.Length)
                        _auraFrameIndex = 0;
                }
            }

            Sprite currentSprite = ResolveCurrentHaloSprite(frames);
            // Pulse léger sans jamais anéantir la visibilité une fois le reveal lancé.
            float alphaPulse = 1f;
            if (pulseAmplitude > 0f && _visualReveal > 0.05f)
                alphaPulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;

            Color tint = HasAnimatedAura() ? Color.white : haloColor;
            float baseAlpha = HasAnimatedAura() ? 1f : Mathf.Clamp01(haloColor.a);
            tint.a = Mathf.Clamp01(baseAlpha * alphaPulse * _visualReveal);

            for (int i = _trackedEnemies.Count - 1; i >= 0; i--)
            {
                TrackedEnemyEntry entry = _trackedEnemies[i];
                Enemy enemy = entry.Enemy;

                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                {
                    RemoveTrackedAt(i);
                    continue;
                }

                ApplyHaloVisuals(entry, currentSprite, tint);
                LayoutGroundAura(entry);
                LayoutPowerStreaks(entry);
                ApplyStreaksReveal(entry);
            }
        }

        private void TickVisualReveal()
        {
            if (!_visualRevealRunning)
                return;

            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
                dt = 0.016f;

            if (_visualRevealDelayLeft > 0f)
            {
                _visualRevealDelayLeft -= dt;
                _visualReveal = 0f;
                if (_visualRevealDelayLeft > 0f)
                    return;

                _visualRevealDelayLeft = 0f;
                _visualRevealElapsed = 0f;
            }

            float duration = Mathf.Max(0.05f, _visualRevealDurationActive);
            _visualRevealElapsed += dt;
            float t = Mathf.Clamp01(_visualRevealElapsed / duration);
            _visualReveal = SmoothStep01(t);

            if (t >= 1f)
            {
                _visualReveal = 1f;
                _visualRevealRunning = false;
            }
        }

        /// <summary> Change la variante d'aura (utile en debug / calibrage). </summary>
        public void SetActiveAuraVariant(int index)
        {
            if (auraVariants == null || auraVariants.Count == 0)
                return;

            activeAuraVariantIndex = Mathf.Clamp(index, 0, auraVariants.Count - 1);
            _auraFrameIndex = 0;
            _auraFrameTimer = 0f;
            RefreshTrackedHaloSprites();
            RefreshTrackedStreaksColors();
            Debug.Log($"[Rupture] Aura active : {ActiveAuraVariantId} ({activeAuraVariantIndex + 1}/{auraVariants.Count})");
        }

        /// <summary> Passe à la variante suivante / précédente. </summary>
        public void CycleAuraVariant(int delta)
        {
            if (auraVariants == null || auraVariants.Count == 0)
                return;

            int count = auraVariants.Count;
            int next = (activeAuraVariantIndex + delta) % count;
            if (next < 0)
                next += count;
            SetActiveAuraVariant(next);
        }

#if UNITY_EDITOR
        /// <summary> Remplace la liste de variants (outil Editor). </summary>
        public void EditorReplaceAuraVariants(List<RuptureAuraVariant> variants, int preferredIndex = 0)
        {
            auraVariants = variants ?? new List<RuptureAuraVariant>();
            activeAuraVariantIndex = auraVariants.Count > 0
                ? Mathf.Clamp(preferredIndex, 0, auraVariants.Count - 1)
                : 0;
        }
#endif

        private bool HasAnimatedAura()
        {
            Sprite[] frames = GetActiveFrames();
            return frames != null && frames.Length > 0;
        }

        private RuptureAuraVariant GetActiveVariant()
        {
            if (auraVariants == null || auraVariants.Count == 0)
                return null;

            int index = Mathf.Clamp(activeAuraVariantIndex, 0, auraVariants.Count - 1);
            return auraVariants[index];
        }

        private Sprite[] GetActiveFrames()
        {
            RuptureAuraVariant variant = GetActiveVariant();
            return variant != null ? variant.frames : null;
        }

        private Sprite ResolveCurrentHaloSprite(Sprite[] frames)
        {
            if (frames != null && frames.Length > 0)
            {
                int idx = _auraFrameIndex % frames.Length;
                if (idx < 0)
                    idx = 0;
                return frames[idx];
            }

            if (haloSpriteOverride != null)
                return haloSpriteOverride;

            return _runtimeHaloSprite;
        }

        private void RefreshTrackedHaloSprites()
        {
            Sprite sprite = ResolveCurrentHaloSprite(GetActiveFrames());
            Color tint = HasAnimatedAura() ? Color.white : haloColor;
            tint.a = Mathf.Clamp01(tint.a * _visualReveal);

            for (int i = 0; i < _trackedEnemies.Count; i++)
            {
                TrackedEnemyEntry entry = _trackedEnemies[i];
                ApplyHaloVisuals(entry, sprite, tint);
                LayoutGroundAura(entry);
                LayoutPowerStreaks(entry);
                ApplyStreaksReveal(entry);
            }
        }

        private static void ApplyHaloVisuals(TrackedEnemyEntry entry, Sprite sprite, Color tint)
        {
            SpriteRenderer halo = entry.HaloRenderer;
            if (halo == null)
                return;

            if (sprite != null && halo.sprite != sprite)
                halo.sprite = sprite;
            halo.color = tint;
        }

        /// <summary> Branche le système sur le TurnManager (ex. RunManager.StartRun). </summary>
        public void Initialize(TurnManager turnManager)
        {
            Cleanup();
            _turnManager = turnManager;

            if (_turnManager != null)
            {
                _turnManager.OnEnemyAddedMidCombat += HandleEnemyAddedMidCombat;
                _turnManager.OnTurnChanged += HandleTurnStarted;
            }

            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure != null)
            {
                pressure.OnRuptureTriggered += HandleRuptureTriggered;
                pressure.OnRuptureEnded += HandleRuptureEnded;
                _ruptureActive = pressure.IsInRupture;

                if (_ruptureActive)
                {
                    _visualReveal = 1f;
                    ApplyToAllEnemies();
                }
            }
            else
            {
                _ruptureActive = false;
            }
        }

        /// <summary> Désabonne le système des événements combat / pression. </summary>
        public void Cleanup()
        {
            if (_ruptureActive)
                RemoveFromAll();

            _ruptureActive = false;
            StopVisualRevealSequence();
            _visualReveal = 1f;

            if (_turnManager != null)
            {
                _turnManager.OnEnemyAddedMidCombat -= HandleEnemyAddedMidCombat;
                _turnManager.OnTurnChanged -= HandleTurnStarted;
            }

            _turnManager = null;

            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure != null)
            {
                pressure.OnRuptureTriggered -= HandleRuptureTriggered;
                pressure.OnRuptureEnded -= HandleRuptureEnded;
            }
        }

        /// <summary>
        /// Signale un switch de spé refusé pendant la Rupture (verrou joueur via DragDropController).
        /// </summary>
        public void NotifySpecSwitchDenied()
        {
            Debug.Log("[Pression] Switch de spé refusé — Rupture active.");
            PlaySfxClip(switchDeniedClip);
            OnSpecSwitchDenied?.Invoke();
        }

        private void ResetAllyRuptureState()
        {
            _defSalveDone.Clear();
            _supHealDone = false;
        }

        private void HandleRuptureTriggered()
        {
            _ruptureActive = true;
            _auraFrameIndex = 0;
            _auraFrameTimer = 0f;
            ResetAllyRuptureState();
            _visualReveal = 0f;
            ApplyToAllEnemies();
            StartVisualRevealSequence();
        }

        private void HandleRuptureEnded()
        {
            _ruptureActive = false;
            ResetAllyRuptureState();
            StopVisualRevealSequence();
            _visualReveal = 1f;
            RemoveFromAll();
        }

        /// <summary>
        /// Démarre la montée douce des VFX (appelable aussi depuis la présentation).
        /// </summary>
        public void BeginVisualReveal(float duration)
        {
            StartVisualRevealInternal(0f, duration);
        }

        private void StartVisualRevealSequence()
        {
            // Délai calé sur le cue bandeau (~flash + mi-pop titre).
            StartVisualRevealInternal(
                Mathf.Max(0f, visualRevealDelay),
                Mathf.Max(0.05f, visualRevealDuration));
        }

        private void StartVisualRevealInternal(float delay, float duration)
        {
            _visualRevealRunning = true;
            _visualRevealDelayLeft = Mathf.Max(0f, delay);
            _visualRevealElapsed = 0f;
            _visualRevealDurationActive = Mathf.Max(0.05f, duration);
            _visualReveal = 0f;
        }

        private void StopVisualRevealSequence()
        {
            _visualRevealRunning = false;
            _visualRevealDelayLeft = 0f;
            _visualRevealElapsed = 0f;
        }

        private static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void HandleTurnStarted(ITurnParticipant p)
        {
            if (!_ruptureActive || p == null || p.IsDead)
                return;

            if (p is not CharacterBall ball)
                return;

            SpecializationData activeSpec = ball.ActiveSpec;
            if (activeSpec == null)
                return;

            switch (activeSpec.Role)
            {
                case CharacterRole.Defender:
                    ApplyDefSalveIfNeeded(ball);
                    break;
                case CharacterRole.Support:
                    ApplySupHealIfNeeded(ball);
                    break;
                case CharacterRole.Attacker:
                    ApplyAtkAllyBonus(ball);
                    break;
            }
        }

        private void ApplyDefSalveIfNeeded(CharacterBall defBall)
        {
            if (defBall == null || defBall.IsDead || _turnManager == null)
                return;

            if (!_defSalveDone.Add(defBall))
                return;

            int shieldAmount = Mathf.RoundToInt(defBall.MaxHp * defShieldPercent);
            if (shieldAmount <= 0)
                return;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                {
                    CharacterBall ally = allies[i];
                    if (ally == null || ally.IsDead)
                        continue;

                    BuffReceiver br = ally.BuffReceiver;
                    if (br == null)
                        continue;

                    br.AddBuff(new BuffData
                    {
                        BuffId = RuptureDefShieldBuffId,
                        Source = defBall,
                        StatType = BuffStatType.Shield,
                        Value = shieldAmount,
                        IsPercent = false,
                        RemainingTurns = -1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });
                }
            }

            Debug.Log(
                $"[Rupture] Salve de protection de {defBall.Name} : bouclier {shieldAmount} " +
                "pour toute l'équipe.");
            PlaySfxClip(defSalveClip);
        }

        private void ApplySupHealIfNeeded(CharacterBall supBall)
        {
            if (supBall == null || supBall.IsDead || _turnManager == null)
                return;

            if (_supHealDone)
                return;

            _supHealDone = true;
            _turnManager.HealAllAllies(supHealPercent);

            Debug.Log(
                $"[Rupture] Soin d'équipe par {supBall.Name} : " +
                $"{supHealPercent * 100f:F0}% des HP max de chaque allié.");
            PlaySfxClip(supHealClip);
        }

        private void ApplyAtkAllyBonus(CharacterBall atkBall)
        {
            if (atkBall == null || atkBall.IsDead)
                return;

            int atkAllyCount = CountAliveAtkAllies();
            if (atkAllyCount <= 0)
                return;

            BuffReceiver br = atkBall.BuffReceiver;
            if (br == null)
                return;

            float atkBonus = atkAllyCount * atkPerAtkAllyPercent;

            br.RemoveBuffsById(RuptureAtkAllyBuffId);
            br.AddBuff(new BuffData
            {
                BuffId = RuptureAtkAllyBuffId,
                Source = atkBall,
                StatType = BuffStatType.ATK,
                Value = atkBonus,
                IsPercent = true,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            Debug.Log(
                $"[Rupture] {atkBall.Name} : ATK +{atkBonus * 100f:F0}% ({atkAllyCount} perso(s) ATK)");
        }

        private int CountAliveAtkAllies()
        {
            if (_turnManager == null)
                return 0;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null)
                return 0;

            int count = 0;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                SpecializationData spec = ally.ActiveSpec;
                if (spec != null && spec.Role == CharacterRole.Attacker)
                    count++;
            }

            return count;
        }

        private void PlaySfxClip(AudioClip clip)
        {
            if (clip == null)
                return;

            SfxManager sfx = SfxManager.Instance;
            if (sfx != null)
                sfx.PlaySfx(clip);
        }

        private void HandleEnemyAddedMidCombat(Enemy enemy)
        {
            if (!_ruptureActive)
                return;

            ApplyToEnemy(enemy);
        }

        private void ApplyToAllEnemies()
        {
            if (_turnManager == null)
                return;

            int applied = 0;
            IReadOnlyList<ITurnParticipant> participants = _turnManager.Participants;
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    if (participants[i] is Enemy enemy && ApplyToEnemy(enemy))
                        applied++;
                }
            }

            Debug.Log(
                $"[Rupture] Effets appliqués à {applied} ennemis " +
                $"(+{atkBonusPercent * 100f:F0}% ATK, +{damageReductionPercent * 100f:F0}% résistance, " +
                $"+{launchForceBonusPercent * 100f:F0}% force)");
        }

        private bool ApplyToEnemy(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead)
                return false;

            if (IsEnemyTracked(enemy))
                return false;

            ApplyBuffsToEnemy(enemy);

            SpriteRenderer halo = AcquireHalo("RuptureHaloGround");
            if (halo == null)
                return true;

            SpriteRenderer enemyVisual = ResolveEnemyVisualRenderer(enemy);
            Transform parent = enemy.transform;

            Transform haloTransform = halo.transform;
            haloTransform.SetParent(parent, false);
            haloTransform.localRotation = Quaternion.identity;
            halo.maskInteraction = SpriteMaskInteraction.None;

            if (enemyVisual != null)
            {
                halo.sortingLayerID = enemyVisual.sortingLayerID;
                // Ennemis souvent en order 0 : garder le calque ombre (8) pour rester au-dessus du sol.
                halo.sortingOrder = enemyVisual.sortingOrder > GroundAuraSortingOrder
                    ? enemyVisual.sortingOrder - 1
                    : GroundAuraSortingOrder;
            }
            else
            {
                halo.sortingOrder = GroundAuraSortingOrder;
            }

            Sprite sprite = ResolveCurrentHaloSprite(GetActiveFrames());
            Color tint = HasAnimatedAura() ? Color.white : haloColor;
            float baseAlpha = HasAnimatedAura() ? 1f : Mathf.Clamp01(haloColor.a);
            tint.a = Mathf.Clamp01(baseAlpha * _visualReveal);

            var entry = new TrackedEnemyEntry
            {
                Enemy = enemy,
                HaloRenderer = halo,
                PowerStreaks = null
            };

            // Traits optionnels : un échec ne doit jamais bloquer l'aura sol.
            if (enablePowerStreaks)
            {
                try
                {
                    ParticleSystem streaks = AcquirePowerStreaks();
                    if (streaks != null)
                    {
                        Transform streaksTransform = streaks.transform;
                        streaksTransform.SetParent(parent, false);
                        streaksTransform.localRotation = Quaternion.identity;
                        streaksTransform.localScale = Vector3.one;
                        ApplyPowerStreaksColor(streaks, ResolvePowerStreaksColor());
                        ConfigurePowerStreaksPlayback(streaks);
                        entry.PowerStreaks = streaks;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Rupture] Traits montants ignorés : {ex.Message}");
                    entry.PowerStreaks = null;
                }
            }

            ApplyHaloVisuals(entry, sprite, tint);
            LayoutGroundAura(entry);
            LayoutPowerStreaks(entry);
            ApplyStreaksReveal(entry);
            halo.gameObject.SetActive(true);

            if (entry.PowerStreaks != null)
            {
                entry.PowerStreaks.gameObject.SetActive(true);
                entry.PowerStreaks.Clear(true);
                entry.PowerStreaks.Play(true);
            }

            _trackedEnemies.Add(entry);

            return true;
        }

        private static SpriteRenderer ResolveEnemyVisualRenderer(Enemy enemy)
        {
            if (enemy == null)
                return null;

            // Prefab peut s'appeler "Visual" ou "Visual " (espace).
            Transform visual = FindChildVisual(enemy.transform);
            if (visual != null)
            {
                SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null && IsFiniteBounds(sr.bounds))
                    return sr;
            }

            SpriteRenderer[] renderers = enemy.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer best = null;
            float bestArea = -1f;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr == null || sr.sprite == null)
                    continue;

                if (IsRuptureAuraObject(sr.gameObject))
                    continue;

                if (!IsFiniteBounds(sr.bounds))
                    continue;

                Vector3 size = sr.bounds.size;
                float area = size.x * size.y;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = sr;
                }
            }

            return best;
        }

        private static Transform FindChildVisual(Transform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;

                string n = child.name.Trim();
                if (string.Equals(n, "Visual", StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return root.Find("Visual");
        }

        private static bool IsRuptureAuraObject(GameObject go)
        {
            if (go == null)
                return false;

            string n = go.name;
            return n.StartsWith("RuptureHalo", StringComparison.Ordinal)
                || n.StartsWith("RuptureAura", StringComparison.Ordinal)
                || n.StartsWith("RupturePower", StringComparison.Ordinal);
        }

        private static bool IsFiniteBounds(Bounds bounds)
        {
            Vector3 c = bounds.center;
            Vector3 s = bounds.size;
            return IsFinite(c.x) && IsFinite(c.y) && IsFinite(c.z)
                && IsFinite(s.x) && IsFinite(s.y) && IsFinite(s.z)
                && s.x < 1000f && s.y < 1000f;
        }

        private static bool IsFinite(float v)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v);
        }

        /// <summary>
        /// Aura au sol (niveau ombre) : centrée sous les pieds, largeur calée sur le sprite.
        /// </summary>
        private void LayoutGroundAura(TrackedEnemyEntry entry)
        {
            if (entry.Enemy == null || entry.HaloRenderer == null)
                return;

            SpriteRenderer enemyVisual = ResolveEnemyVisualRenderer(entry.Enemy);
            Sprite auraSprite = entry.HaloRenderer.sprite;
            if (auraSprite == null)
                auraSprite = ResolveCurrentHaloSprite(GetActiveFrames());
            if (auraSprite == null)
                return;

            FitGroundHalo(entry.HaloRenderer, entry.Enemy.transform, enemyVisual, auraSprite);
        }

        private void FitGroundHalo(
            SpriteRenderer halo,
            Transform parent,
            SpriteRenderer enemyVisual,
            Sprite auraSprite)
        {
            if (halo == null || auraSprite == null || parent == null)
                return;

            float auraSize = Mathf.Max(auraSprite.bounds.size.x, auraSprite.bounds.size.y);
            if (auraSize < 0.0001f || !IsFinite(auraSize))
                return;

            float parentLossy = Mathf.Abs(parent.lossyScale.x);
            if (parentLossy < 0.0001f || !IsFinite(parentLossy))
                parentLossy = 1f;

            float targetWorldSize;
            Vector3 worldFeet;

            if (enemyVisual != null && enemyVisual.sprite != null && IsFiniteBounds(enemyVisual.bounds))
            {
                Bounds b = enemyVisual.bounds;
                // Empreinte au sol ≈ largeur du sprite (pas la hauteur des capes).
                float footprint = Mathf.Max(b.size.x, b.size.y * 0.35f);
                targetWorldSize = footprint * auraFitPadding;
                worldFeet = new Vector3(b.center.x, b.min.y + groundYOffset, b.center.z);
            }
            else
            {
                targetWorldSize = Mathf.Max(auraFitPadding, 0.5f);
                worldFeet = parent.position;
            }

            if (!IsFinite(targetWorldSize) || targetWorldSize < 0.01f)
                targetWorldSize = 1f;

            float scaleX = (targetWorldSize / parentLossy) / auraSize;
            scaleX = Mathf.Clamp(scaleX, 0.01f, 50f);
            if (!IsFinite(scaleX))
                scaleX = 1f;

            float revealScale = Mathf.Lerp(
                visualRevealStartScale,
                1f,
                Mathf.Clamp01(_visualReveal));
            scaleX *= revealScale;

            float scaleY = scaleX * Mathf.Clamp(groundFlatten, 0.3f, 1f);
            halo.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            Vector3 localPos = parent.InverseTransformPoint(worldFeet);
            if (IsFinite(localPos.x) && IsFinite(localPos.y) && IsFinite(localPos.z))
                halo.transform.localPosition = localPos;
            else
                halo.transform.localPosition = Vector3.zero;

            if (enemyVisual != null)
            {
                halo.sortingLayerID = enemyVisual.sortingLayerID;
                halo.sortingOrder = enemyVisual.sortingOrder > GroundAuraSortingOrder
                    ? enemyVisual.sortingOrder - 1
                    : GroundAuraSortingOrder;
            }
        }

        private void RemoveFromEnemy(Enemy enemy)
        {
            for (int i = _trackedEnemies.Count - 1; i >= 0; i--)
            {
                if (_trackedEnemies[i].Enemy == enemy)
                {
                    RemoveTrackedAt(i);
                    return;
                }
            }
        }

        private void RemoveFromAll()
        {
            int count = _trackedEnemies.Count;

            for (int i = _trackedEnemies.Count - 1; i >= 0; i--)
                RemoveTrackedAt(i);

            if (count > 0)
                Debug.Log($"[Rupture] Effets retirés de {count} ennemis");
        }

        private void RemoveTrackedAt(int index)
        {
            if (index < 0 || index >= _trackedEnemies.Count)
                return;

            TrackedEnemyEntry entry = _trackedEnemies[index];
            Enemy enemy = entry.Enemy;

            if (enemy != null && !enemy.IsDead)
                RemoveBuffsFromEnemy(enemy);

            ReleaseHalo(entry.HaloRenderer);
            ReleasePowerStreaks(entry.PowerStreaks);
            _trackedEnemies.RemoveAt(index);
        }

        private void ApplyBuffsToEnemy(Enemy enemy)
        {
            BuffReceiver br = enemy.BuffReceiver;
            if (br == null)
                return;

            br.RemoveBuffsById(RuptureAtkBuffId);
            br.AddBuff(new BuffData
            {
                BuffId = RuptureAtkBuffId,
                Source = null,
                StatType = BuffStatType.ATK,
                Value = atkBonusPercent,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });

            br.RemoveBuffsById(RuptureResistanceBuffId);
            br.AddBuff(new BuffData
            {
                BuffId = RuptureResistanceBuffId,
                Source = null,
                StatType = BuffStatType.DamageReduction,
                Value = damageReductionPercent,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });

            br.RemoveBuffsById(RuptureLaunchForceBuffId);
            br.AddBuff(new BuffData
            {
                BuffId = RuptureLaunchForceBuffId,
                Source = null,
                StatType = BuffStatType.LaunchForce,
                Value = launchForceBonusPercent,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private void RemoveBuffsFromEnemy(Enemy enemy)
        {
            BuffReceiver br = enemy.BuffReceiver;
            if (br == null)
                return;

            br.RemoveBuffsById(RuptureAtkBuffId);
            br.RemoveBuffsById(RuptureResistanceBuffId);
            br.RemoveBuffsById(RuptureLaunchForceBuffId);
        }

        private bool IsEnemyTracked(Enemy enemy)
        {
            for (int i = 0; i < _trackedEnemies.Count; i++)
            {
                if (_trackedEnemies[i].Enemy == enemy)
                    return true;
            }

            return false;
        }

        private SpriteRenderer AcquireHalo(string objectName)
        {
            int lastIndex = _haloPool.Count - 1;
            if (lastIndex >= 0)
            {
                SpriteRenderer pooled = _haloPool[lastIndex];
                _haloPool.RemoveAt(lastIndex);
                if (pooled != null)
                {
                    pooled.gameObject.name = objectName;
                    pooled.maskInteraction = SpriteMaskInteraction.None;
                    pooled.transform.localScale = Vector3.one;
                    pooled.transform.localPosition = Vector3.zero;
                }
                return pooled;
            }

            var haloGo = new GameObject(objectName);
            haloGo.transform.SetParent(_haloPoolContainer, false);
            var renderer = haloGo.AddComponent<SpriteRenderer>();
            renderer.maskInteraction = SpriteMaskInteraction.None;
            haloGo.SetActive(false);
            return renderer;
        }

        private void ReleaseHalo(SpriteRenderer haloRenderer)
        {
            if (haloRenderer == null)
                return;

            haloRenderer.maskInteraction = SpriteMaskInteraction.None;
            haloRenderer.transform.SetParent(_haloPoolContainer, false);
            haloRenderer.transform.localScale = Vector3.one;
            haloRenderer.transform.localPosition = Vector3.zero;
            haloRenderer.gameObject.SetActive(false);
            _haloPool.Add(haloRenderer);
        }

        private void RefreshTrackedStreaksColors()
        {
            Color color = ResolvePowerStreaksColor();
            for (int i = 0; i < _trackedEnemies.Count; i++)
            {
                ParticleSystem streaks = _trackedEnemies[i].PowerStreaks;
                if (streaks == null)
                    continue;

                ApplyPowerStreaksColor(streaks, color);
            }
        }

        private void ApplyStreaksReveal(TrackedEnemyEntry entry)
        {
            ParticleSystem streaks = entry.PowerStreaks;
            if (streaks == null)
                return;

            var emission = streaks.emission;
            emission.rateOverTime = streaksRate * Mathf.Clamp01(_visualReveal);

            var main = streaks.main;
            Color c = ResolvePowerStreaksColor();
            c.a *= Mathf.Clamp01(_visualReveal);
            main.startColor = c;
        }

        private void LayoutPowerStreaks(TrackedEnemyEntry entry)
        {
            ParticleSystem streaks = entry.PowerStreaks;
            if (streaks == null || entry.Enemy == null)
                return;

            Transform parent = entry.Enemy.transform;
            SpriteRenderer enemyVisual = ResolveEnemyVisualRenderer(entry.Enemy);

            float footprint = 0.6f;
            Vector3 worldFeet = parent.position;

            if (enemyVisual != null && enemyVisual.sprite != null && IsFiniteBounds(enemyVisual.bounds))
            {
                Bounds b = enemyVisual.bounds;
                footprint = Mathf.Max(b.size.x, b.size.y * 0.35f);
                worldFeet = new Vector3(b.center.x, b.min.y + groundYOffset, b.center.z);
            }

            Vector3 localPos = parent.InverseTransformPoint(worldFeet);
            if (IsFinite(localPos.x) && IsFinite(localPos.y) && IsFinite(localPos.z))
                streaks.transform.localPosition = localPos;
            else
                streaks.transform.localPosition = Vector3.zero;

            float parentLossy = Mathf.Abs(parent.lossyScale.x);
            if (parentLossy < 0.0001f || !IsFinite(parentLossy))
                parentLossy = 1f;

            float radiusWorld = footprint * Mathf.Clamp(streaksRadiusFactor, 0.08f, 0.6f);
            float radiusLocal = radiusWorld / parentLossy;
            if (!IsFinite(radiusLocal) || radiusLocal < 0.02f)
                radiusLocal = 0.15f;

            var shape = streaks.shape;
            shape.radius = radiusLocal;

            ParticleSystemRenderer renderer = streaks.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && enemyVisual != null)
            {
                renderer.sortingLayerID = enemyVisual.sortingLayerID;
                // Devant l'ombre, au niveau du corps.
                renderer.sortingOrder = enemyVisual.sortingOrder;
            }
        }

        private void ConfigurePowerStreaksPlayback(ParticleSystem streaks)
        {
            if (streaks == null)
                return;

            var main = streaks.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = streaksLifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(streaksSpeedMin, streaksSpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(streaksSize * 0.7f, streaksSize * 1.25f);
            main.maxParticles = MaxStreaksParticles;

            var emission = streaks.emission;
            emission.rateOverTime = streaksRate;
        }

        private static void ApplyPowerStreaksColor(ParticleSystem streaks, Color color)
        {
            if (streaks == null)
                return;

            var main = streaks.main;
            main.startColor = color;
        }

        private Color ResolvePowerStreaksColor()
        {
            RuptureAuraVariant variant = GetActiveVariant();
            if (variant != null && !string.IsNullOrEmpty(variant.id))
            {
                Color fromId;
                if (TryResolveColorFromAuraId(variant.id, out fromId))
                    return fromId;
            }

            return streaksFallbackColor;
        }

        private static bool TryResolveColorFromAuraId(string id, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(id))
                return false;

            string lower = id.ToLowerInvariant();

            if (lower.EndsWith("_red"))
            {
                color = new Color(1f, 0.28f, 0.18f, 0.9f);
                return true;
            }

            if (lower.EndsWith("_orange"))
            {
                color = new Color(1f, 0.5f, 0.12f, 0.9f);
                return true;
            }

            if (lower.EndsWith("_yellow"))
            {
                color = new Color(1f, 0.88f, 0.25f, 0.9f);
                return true;
            }

            if (lower.EndsWith("_magic") || lower.EndsWith("_purple"))
            {
                color = new Color(0.72f, 0.35f, 1f, 0.9f);
                return true;
            }

            if (lower.EndsWith("_royal"))
            {
                color = new Color(0.4f, 0.45f, 1f, 0.9f);
                return true;
            }

            if (lower.EndsWith("_blue"))
            {
                color = new Color(0.3f, 0.65f, 1f, 0.9f);
                return true;
            }

            if (lower.EndsWith("_pink"))
            {
                color = new Color(1f, 0.4f, 0.75f, 0.9f);
                return true;
            }

            if (lower.EndsWith("_lime") || lower.EndsWith("_limegreen") || lower.EndsWith("_springgreen"))
            {
                color = new Color(0.45f, 1f, 0.35f, 0.9f);
                return true;
            }

            return false;
        }

        private ParticleSystem AcquirePowerStreaks()
        {
            int lastIndex = _streaksPool.Count - 1;
            if (lastIndex >= 0)
            {
                ParticleSystem pooled = _streaksPool[lastIndex];
                _streaksPool.RemoveAt(lastIndex);
                if (pooled != null)
                {
                    pooled.gameObject.name = "RupturePowerStreaks";
                    return pooled;
                }
            }

            return CreatePowerStreaksInstance();
        }

        private void ReleasePowerStreaks(ParticleSystem streaks)
        {
            if (streaks == null)
                return;

            streaks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            streaks.transform.SetParent(_streaksPoolContainer, false);
            streaks.transform.localScale = Vector3.one;
            streaks.transform.localPosition = Vector3.zero;
            streaks.gameObject.SetActive(false);
            _streaksPool.Add(streaks);
        }

        private ParticleSystem CreatePowerStreaksInstance()
        {
            ParticleSystem instance;

            if (powerStreaksPrefab != null)
            {
                instance = Instantiate(powerStreaksPrefab, _streaksPoolContainer);
                instance.gameObject.name = "RupturePowerStreaks";
            }
            else
            {
                GameObject go = BuildRuntimePowerStreaksGO();
                go.transform.SetParent(_streaksPoolContainer, false);
                instance = go.GetComponent<ParticleSystem>();
            }

            instance.gameObject.SetActive(false);
            return instance;
        }

        private GameObject BuildRuntimePowerStreaksGO()
        {
            GameObject go = new GameObject("RupturePowerStreaks", typeof(ParticleSystem));
            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = MaxStreaksParticles;
            main.gravityModifier = 0f;
            main.startColor = streaksFallbackColor;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = streaksRate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.22f;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            // Circle Unity = plan XZ par défaut → -90° pour le plan XY (2D).
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            // Même mode (TwoConstants) sur X/Y/Z — sinon Unity : "curves must all be in the same mode".
            velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.55f), 0.45f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.2f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.12f),
                    new GradientAlphaKey(0.55f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(g);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.55f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f, 0.15f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var noise = ps.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            noise.frequency = 0.45f;
            noise.scrollSpeed = 0.15f;
            noise.damping = true;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.enabled = true;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3.2f;
            renderer.velocityScale = 0.12f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (_runtimeStreaksMaterial == null)
                _runtimeStreaksMaterial = new Material(Shader.Find("Sprites/Default"));
            renderer.material = _runtimeStreaksMaterial;

            return go;
        }

        private void CreateProceduralHaloSprite()
        {
            const int size = 64;

            _runtimeHaloTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            // Exception volontaire au Point — c'est un glow, pas du pixel art combat.
            _runtimeHaloTexture.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            float maxDist = center;
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            _runtimeHaloTexture.SetPixels(pixels);
            _runtimeHaloTexture.Apply();

            _runtimeHaloSprite = Sprite.Create(
                _runtimeHaloTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                64f);
        }
    }
}
