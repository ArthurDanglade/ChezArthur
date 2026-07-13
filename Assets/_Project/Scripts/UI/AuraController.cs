using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChezArthur.UI
{
    /// <summary>
    /// Contrôle l'aura animée (halo autour du perso ou anneau au sol à la place de l'ombre).
    /// </summary>
    public class AuraController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float HIT_PULSE_MAX_SCALE = 1.2f;
        private const float HIT_PULSE_UP_DURATION = 0.1f;
        private const float HIT_PULSE_DOWN_DURATION = 0.2f;
        private const float DAMAGE_FLASH_DURATION = 0.3f;
        private const float DAMAGE_FLASH_INTENSITY = 1.5f;
        private const int CombatAuraHaloSortingOrder = 9;
        private const int CombatAuraGroundSortingOrder = 8;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Placement")]
        [Tooltip("Halo = autour du perso. GroundRing = au sol, remplace l'ombre.")]
        [SerializeField] private AuraPlacementMode placement = AuraPlacementMode.Halo;

        [Header("Références")]
        [Tooltip("Calque AuraLayer (mode Halo).")]
        [FormerlySerializedAs("auraRenderer")]
        [SerializeField] private SpriteRenderer haloAuraRenderer;
        [Tooltip("Ombre du prefab (mode GroundRing — l'aura s'affiche ici).")]
        [SerializeField] private SpriteRenderer groundAuraRenderer;
        [SerializeField] private SpriteRenderer characterRenderer;

        [Header("Animation spritesheet")]
        [Tooltip("Spritesheet source (PNG Multiple). Recharge les frames au démarrage — survit au remplacement du PNG.")]
        [SerializeField] private Texture2D auraSpriteSheet;

        [Tooltip("Frames de l'aura (remplies auto depuis auraSpriteSheet si assigné).")]
        [SerializeField] private Sprite[] auraFrames = new Sprite[0];

        [SerializeField] private float framesPerSecond = 10f;
        [SerializeField] private bool sortFramesByNameNumber = true;

        [Header("Courbes de feedback")]
        [SerializeField] private AnimationCurve hitPulseUpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve hitPulseDownCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve damageFlashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Échelle de base")]
        [Tooltip("Échelle locale de l'aura au repos (ajuster dans le prefab pour cadrer).")]
        [SerializeField] private Vector3 auraBaseScale = Vector3.one;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private SpriteRenderer _activeAuraRenderer;
        private Vector3 _groundRingBaseScale = Vector3.one;
        private Color _auraBaseColor = Color.white;
        private float _frameTimer;
        private int _frameIndex;
        private bool _isAnimating;
        private Coroutine _hitPulseRoutine;
        private Coroutine _damageFlashRoutine;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public AuraPlacementMode Placement => placement;
        public bool UsesGroundRing => placement == AuraPlacementMode.GroundRing;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            ReloadFramesFromSheet();
            ResolveActiveRenderer();
            CacheBaseState();
            StartAuraLoop();
        }

        private void Start()
        {
            RefreshLayout();
        }

        private void OnValidate()
        {
            ResolveActiveRenderer();
            if (_activeAuraRenderer != null)
                _activeAuraRenderer.sortingOrder = GetSortingOrderForPlacement();
        }

        private void Update()
        {
            TickAuraAnimation();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void OnEnemyHit()
        {
            if (_activeAuraRenderer == null)
                return;

            if (_hitPulseRoutine != null)
                StopCoroutine(_hitPulseRoutine);

            _hitPulseRoutine = StartCoroutine(HitPulseRoutine());
        }

        public void OnDamageTaken()
        {
            if (_activeAuraRenderer == null)
                return;

            if (_damageFlashRoutine != null)
                StopCoroutine(_damageFlashRoutine);

            _damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
        }

        /// <summary>
        /// Synchronise placement, sorting et scale après RefreshCombatVisual.
        /// </summary>
        public void RefreshLayout()
        {
            ResolveActiveRenderer();
            ApplyPlacementVisibility();

            if (_activeAuraRenderer == null)
                return;

            _activeAuraRenderer.sortingOrder = GetSortingOrderForPlacement();
            ApplyAuraBaseScale();

            if (UsesGroundRing)
            {
                _activeAuraRenderer.color = Color.white;
                _auraBaseColor = Color.white;
            }

            if (auraFrames != null && auraFrames.Length > 0)
            {
                if (_activeAuraRenderer.sprite == null)
                    _activeAuraRenderer.sprite = auraFrames[0];
                if (!_isAnimating)
                    StartAuraLoop();
            }
        }

        public void SetAuraFrames(Sprite[] frames)
        {
            auraFrames = frames ?? new Sprite[0];
            if (sortFramesByNameNumber && auraFrames.Length > 1)
                System.Array.Sort(auraFrames, (a, b) => TrailingNumber(a).CompareTo(TrailingNumber(b)));

            _frameIndex = 0;
            _frameTimer = 0f;
            _isAnimating = auraFrames.Length > 0;
            if (_isAnimating && _activeAuraRenderer != null)
                _activeAuraRenderer.sprite = auraFrames[0];
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ResolveActiveRenderer()
        {
            _activeAuraRenderer = UsesGroundRing ? groundAuraRenderer : haloAuraRenderer;
        }

        private void ApplyPlacementVisibility()
        {
            if (haloAuraRenderer != null && haloAuraRenderer != groundAuraRenderer)
                haloAuraRenderer.gameObject.SetActive(!UsesGroundRing);

            if (groundAuraRenderer != null)
                groundAuraRenderer.enabled = true;
        }

        private int GetSortingOrderForPlacement()
        {
            return UsesGroundRing ? CombatAuraGroundSortingOrder : CombatAuraHaloSortingOrder;
        }

        private void CacheBaseState()
        {
            if (_activeAuraRenderer == null)
                return;

            if (UsesGroundRing)
                _groundRingBaseScale = _activeAuraRenderer.transform.localScale;

            _auraBaseColor = UsesGroundRing ? Color.white : _activeAuraRenderer.color;
            ApplyAuraBaseScale();
        }

        private void ApplyAuraBaseScale()
        {
            if (_activeAuraRenderer == null)
                return;

            if (UsesGroundRing)
                _activeAuraRenderer.transform.localScale = Vector3.Scale(_groundRingBaseScale, auraBaseScale);
            else
                _activeAuraRenderer.transform.localScale = auraBaseScale;
        }

        private Vector3 GetAuraBaseScaleForPulse()
        {
            if (UsesGroundRing)
                return Vector3.Scale(_groundRingBaseScale, auraBaseScale);

            return auraBaseScale;
        }

        private void StartAuraLoop()
        {
            if (_activeAuraRenderer == null)
            {
                _isAnimating = false;
                return;
            }

            if (auraFrames == null || auraFrames.Length == 0 || HasBrokenFrameReference())
                ReloadFramesFromSheet();

            if (auraFrames == null || auraFrames.Length == 0)
            {
                _isAnimating = false;
                Debug.LogWarning("[AuraController] Aucune frame d'aura — assigne auraSpriteSheet ou relance le builder.", this);
                return;
            }

            if (sortFramesByNameNumber && auraFrames.Length > 1)
                System.Array.Sort(auraFrames, (a, b) => TrailingNumber(a).CompareTo(TrailingNumber(b)));

            _frameIndex = 0;
            _frameTimer = 0f;
            _isAnimating = true;
            _activeAuraRenderer.sprite = auraFrames[0];
        }

        private void TickAuraAnimation()
        {
            if (!_isAnimating || _activeAuraRenderer == null || framesPerSecond <= 0f || auraFrames.Length == 0)
                return;

            _frameTimer += Time.deltaTime;
            float frameDuration = 1f / framesPerSecond;
            if (_frameTimer < frameDuration)
                return;

            while (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                _frameIndex = (_frameIndex + 1) % auraFrames.Length;
            }

            _activeAuraRenderer.sprite = auraFrames[_frameIndex];
        }

        private bool HasBrokenFrameReference()
        {
            if (auraFrames == null)
                return true;

            for (int i = 0; i < auraFrames.Length; i++)
            {
                if (auraFrames[i] == null)
                    return true;
            }

            return false;
        }

        private void ReloadFramesFromSheet()
        {
            if (auraSpriteSheet == null)
                return;

            Sprite[] loaded = AuraSpriteSheetLoader.LoadSprites(auraSpriteSheet, sortFramesByNameNumber);
            if (loaded.Length == 0)
                return;

            auraFrames = loaded;
        }

        private IEnumerator HitPulseRoutine()
        {
            Transform auraTransform = _activeAuraRenderer.transform;
            Vector3 baseScale = GetAuraBaseScaleForPulse();
            Vector3 peakScale = baseScale * HIT_PULSE_MAX_SCALE;

            float elapsed = 0f;
            while (elapsed < HIT_PULSE_UP_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / HIT_PULSE_UP_DURATION);
                float k = hitPulseUpCurve.Evaluate(t);
                auraTransform.localScale = Vector3.LerpUnclamped(baseScale, peakScale, k);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < HIT_PULSE_DOWN_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / HIT_PULSE_DOWN_DURATION);
                float k = hitPulseDownCurve.Evaluate(t);
                auraTransform.localScale = Vector3.LerpUnclamped(peakScale, baseScale, k);
                yield return null;
            }

            auraTransform.localScale = baseScale;
            _hitPulseRoutine = null;
        }

        private IEnumerator DamageFlashRoutine()
        {
            Color intense = _auraBaseColor * DAMAGE_FLASH_INTENSITY;
            intense.a = _auraBaseColor.a;

            float elapsed = 0f;
            while (elapsed < DAMAGE_FLASH_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / DAMAGE_FLASH_DURATION);
                float k = damageFlashCurve.Evaluate(t);
                _activeAuraRenderer.color = Color.Lerp(intense, _auraBaseColor, k);
                yield return null;
            }

            _activeAuraRenderer.color = _auraBaseColor;
            _damageFlashRoutine = null;
        }

        private static int TrailingNumber(Sprite sprite)
        {
            if (sprite == null)
                return 0;

            string name = sprite.name;
            int i = name.Length - 1;
            while (i >= 0 && char.IsDigit(name[i]))
                i--;

            return int.TryParse(name.Substring(i + 1), out int number) ? number : 0;
        }
    }
}
