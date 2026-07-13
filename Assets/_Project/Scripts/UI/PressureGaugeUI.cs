using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Gameplay;

namespace ChezArthur.UI
{
    /// <summary>
    /// Manomètre vertical de la jauge de pression (HUD). Liseré fin au bord de l'écran,
    /// remplissage bas→haut, saillance progressive et drain rouge en Rupture.
    /// </summary>
    public class PressureGaugeUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private RectTransform barRoot;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image ghostFillImage;
        [SerializeField] private Image fillImage;

        [Header("Piste (repos)")]
        [Tooltip("Couleur du canal de fond — doit rester lisible sur l'arène.")]
        [SerializeField] private Color trackColor = new Color(0.9f, 0.77f, 0.35f, 0.82f);
        [Tooltip("Repère bas visible quand la jauge logique est à 0.")]
        [SerializeField] private float minIdleFill = 0.04f;

        [Header("Saillance")]
        [SerializeField] private Gradient fillGradient;
        [SerializeField] private float baseWidth = 16f;
        [SerializeField] private float alertWidth = 20f;
        [SerializeField] private float alertZoneStart = 0.75f;
        [SerializeField] private float pulseSpeed = 2.5f;
        [SerializeField] private float pulseAmplitude = 0.10f;

        [Header("Ghost (pertes)")]
        [SerializeField] private float ghostDelay = 0.3f;
        [SerializeField] private float ghostDrainSpeed = 1.5f;

        [Header("Flash de variation")]
        [SerializeField] private float changeFlashDuration = 0.15f;

        [Header("Rupture")]
        [SerializeField] private Color ruptureColor = Color.red;

        [Header("Refus de switch")]
        [SerializeField] private float deniedDuration = 0.25f;
        [SerializeField] private float deniedShakePixels = 3f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _displayFill;
        private float _displayGhost;
        private float _lastGhostDropTime;
        private float _flashTimer;
        private bool _isInRupture;
        private float _ruptureProgress01;
        private float _deniedTimer;
        private Vector2 _barRootRestAnchoredPos;
        private bool _hasBarRootRestPos;
        private bool _pressureSubscribed;
        private bool _ruptureSubscribed;

        private const float WidthLerpSpeed = 12f;
        private const float FillEpsilon = 0.001f;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            CacheBarRootRestPosition();
        }

        private void Reset()
        {
            fillGradient = new Gradient();
            fillGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.55f, 0.58f, 0.62f), 0f),
                    new GradientColorKey(new Color(0.68f, 0.71f, 0.76f), 0.5f),
                    new GradientColorKey(new Color(0.95f, 0.65f, 0.2f), 0.7f),
                    new GradientColorKey(new Color(0.9f, 0.22f, 0.22f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.55f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(1f, 1f)
                });

            ruptureColor = UiTheme.EnemyTypeBossColor;
            trackColor = new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.82f);
        }

        private void OnEnable()
        {
            ApplyFillToImages();
            ApplyColorAndWidth();
        }

        private void Start()
        {
            StartCoroutine(SubscribeDelayed());
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
            RestoreBarRootPosition();
        }

        private void Update()
        {
            if (_flashTimer > 0f)
                _flashTimer = Mathf.Max(0f, _flashTimer - Time.deltaTime);

            if (!NeedsAnimatedUpdate())
            {
                RefreshStaticVisuals();
                return;
            }

            UpdateGhostDrain();
            UpdateDeniedFeedback();
            ApplyFillToImages();
            ApplyColorAndWidth();
        }

        // ═══════════════════════════════════════════
        // ABONNEMENTS
        // ═══════════════════════════════════════════

        private IEnumerator SubscribeDelayed()
        {
            yield return null;
            TrySubscribe();
            SyncFromSystems();
            ApplyFillToImages();
            RefreshStaticVisuals();

            if (PressureGaugeSystem.Instance == null)
                Debug.LogWarning(
                    "[PressureGaugeUI] PressureGaugeSystem absent de la scène — " +
                    "le manomètre restera vide. Menu : Chez Arthur/UI/Monter systèmes Pression (logique).");
        }

        private void TrySubscribe()
        {
            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure != null && !_pressureSubscribed)
            {
                pressure.OnGaugeChanged += HandleGaugeChanged;
                pressure.OnRuptureTriggered += HandleRuptureTriggered;
                pressure.OnRuptureEnded += HandleRuptureEnded;
                pressure.OnRuptureProgressChanged += HandleRuptureProgress;
                _pressureSubscribed = true;
            }

            RuptureEffectsSystem rupture = RuptureEffectsSystem.Instance;
            if (rupture != null && !_ruptureSubscribed)
            {
                rupture.OnSpecSwitchDenied += HandleSpecSwitchDenied;
                _ruptureSubscribed = true;
            }
        }

        private void UnsubscribeAll()
        {
            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure != null && _pressureSubscribed)
            {
                pressure.OnGaugeChanged -= HandleGaugeChanged;
                pressure.OnRuptureTriggered -= HandleRuptureTriggered;
                pressure.OnRuptureEnded -= HandleRuptureEnded;
                pressure.OnRuptureProgressChanged -= HandleRuptureProgress;
                _pressureSubscribed = false;
            }

            RuptureEffectsSystem rupture = RuptureEffectsSystem.Instance;
            if (rupture != null && _ruptureSubscribed)
            {
                rupture.OnSpecSwitchDenied -= HandleSpecSwitchDenied;
                _ruptureSubscribed = false;
            }
        }

        private void SyncFromSystems()
        {
            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure == null)
                return;

            _isInRupture = pressure.IsInRupture;

            if (_isInRupture)
            {
                _ruptureProgress01 = pressure.RuptureProgress01;
                float drain = 1f - _ruptureProgress01;
                _displayFill = drain;
                _displayGhost = drain;
            }
            else
            {
                _ruptureProgress01 = 0f;
                _displayFill = Mathf.Clamp01(pressure.NormalizedValue);
                _displayGhost = _displayFill;
            }

            _flashTimer = 0f;
            _deniedTimer = 0f;
            _lastGhostDropTime = 0f;
        }

        // ═══════════════════════════════════════════
        // HANDLERS ÉVÉNEMENTS
        // ═══════════════════════════════════════════

        private void HandleGaugeChanged(float normalized)
        {
            if (_isInRupture)
                return;

            float newFill = Mathf.Clamp01(normalized);

            if (newFill > _displayFill + FillEpsilon)
            {
                _displayFill = newFill;
                _displayGhost = newFill;
                _flashTimer = changeFlashDuration;
            }
            else if (newFill < _displayFill - FillEpsilon)
            {
                _displayFill = newFill;
                _lastGhostDropTime = Time.time;
            }
            else
            {
                _displayFill = newFill;
            }

            ApplyFillToImages();
            if (!NeedsAnimatedUpdate())
                RefreshStaticVisuals();
        }

        private void HandleRuptureTriggered()
        {
            _isInRupture = true;
            _ruptureProgress01 = 0f;
            _displayFill = 1f;
            _displayGhost = 1f;
            _flashTimer = 0f;
            ApplyFillToImages();
        }

        private void HandleRuptureProgress(float progress)
        {
            if (!_isInRupture)
                return;

            _ruptureProgress01 = Mathf.Clamp01(progress);
            float drain = 1f - _ruptureProgress01;
            _displayFill = drain;
            _displayGhost = drain;
            ApplyFillToImages();
        }

        private void HandleRuptureEnded()
        {
            _isInRupture = false;
            _ruptureProgress01 = 0f;
        }

        private void HandleSpecSwitchDenied()
        {
            CacheBarRootRestPosition();
            _deniedTimer = deniedDuration;
        }

        // ═══════════════════════════════════════════
        // VISUEL
        // ═══════════════════════════════════════════

        private float GetEffectiveFill()
        {
            return _isInRupture ? 1f - _ruptureProgress01 : _displayFill;
        }

        private bool NeedsAnimatedUpdate()
        {
            if (_flashTimer > 0f)
                return true;

            if (_deniedTimer > 0f)
                return true;

            if (_isInRupture)
                return true;

            if (IsGhostDraining())
                return true;

            float effectiveFill = GetEffectiveFill();
            if (effectiveFill >= alertZoneStart)
                return true;

            if (barRoot != null)
            {
                float targetWidth = GetTargetWidth(effectiveFill);
                if (Mathf.Abs(barRoot.sizeDelta.x - targetWidth) > 0.05f)
                    return true;
            }

            return false;
        }

        private bool IsGhostDraining()
        {
            if (_isInRupture)
                return false;

            if (_displayGhost <= _displayFill + FillEpsilon)
                return false;

            return Time.time - _lastGhostDropTime > ghostDelay;
        }

        private void UpdateGhostDrain()
        {
            if (!IsGhostDraining())
                return;

            _displayGhost = Mathf.MoveTowards(
                _displayGhost,
                _displayFill,
                ghostDrainSpeed * Time.deltaTime);
        }

        private void UpdateDeniedFeedback()
        {
            if (_deniedTimer <= 0f || barRoot == null)
                return;

            _deniedTimer -= Time.deltaTime;

            float duration = Mathf.Max(0.001f, deniedDuration);
            float elapsed = duration - Mathf.Max(0f, _deniedTimer);
            float normalized = Mathf.Clamp01(elapsed / duration);
            float damping = 1f - normalized;

            float shake = Mathf.Sin(normalized * Mathf.PI * 4f) * deniedShakePixels * damping;
            barRoot.anchoredPosition = _barRootRestAnchoredPos + new Vector2(shake, 0f);

            if (_deniedTimer <= 0f)
            {
                _deniedTimer = 0f;
                RestoreBarRootPosition();
            }
        }

        private void ApplyFillToImages()
        {
            float shownFill = GetShownFillAmount();

            if (fillImage != null)
                fillImage.fillAmount = shownFill;

            if (ghostFillImage != null)
                ghostFillImage.fillAmount = Mathf.Max(_displayGhost, shownFill);
        }

        private float GetShownFillAmount()
        {
            if (_isInRupture || _displayFill > FillEpsilon)
                return _displayFill;

            return Mathf.Clamp01(minIdleFill);
        }

        private void RefreshStaticVisuals()
        {
            ApplyColorAndWidth();
        }

        private void ApplyColorAndWidth()
        {
            float effectiveFill = GetEffectiveFill();

            if (backgroundImage != null)
                backgroundImage.color = trackColor;

            if (fillImage != null)
                fillImage.color = ComputeFillColor(effectiveFill);

            if (barRoot != null)
            {
                float targetWidth = GetTargetWidth(effectiveFill);
                Vector2 size = barRoot.sizeDelta;
                size.x = Mathf.Lerp(size.x, targetWidth, Time.deltaTime * WidthLerpSpeed);
                barRoot.sizeDelta = size;
            }
        }

        private Color ComputeFillColor(float effectiveFill)
        {
            Color baseColor = _isInRupture
                ? ruptureColor
                : fillGradient != null
                    ? fillGradient.Evaluate(effectiveFill)
                    : Color.white;

            float flashBlend = 0f;
            if (_flashTimer > 0f && changeFlashDuration > 0f)
                flashBlend = _flashTimer / changeFlashDuration;

            float pulseFactor = 0f;
            if (_isInRupture)
                pulseFactor = 1f;
            else if (effectiveFill >= alertZoneStart)
                pulseFactor = Mathf.InverseLerp(alertZoneStart, 1f, effectiveFill);

            float alphaPulse = 1f;
            if (pulseFactor > 0f)
                alphaPulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude * pulseFactor;

            Color result = Color.Lerp(baseColor, Color.white, flashBlend);

            if (_deniedTimer > 0f && deniedDuration > 0f)
            {
                float deniedElapsed = 1f - (_deniedTimer / deniedDuration);
                float blink = Mathf.Abs(Mathf.Sin(deniedElapsed * Mathf.PI * 4f));
                Color blinkColor = blink > 0.5f ? Color.white : ruptureColor;
                result = Color.Lerp(result, blinkColor, blink * 0.65f);
            }

            result.a = Mathf.Clamp01(baseColor.a * alphaPulse);
            return result;
        }

        private float GetTargetWidth(float effectiveFill)
        {
            float widthT = _isInRupture
                ? 1f
                : Mathf.InverseLerp(alertZoneStart, 1f, effectiveFill);
            return Mathf.Lerp(baseWidth, alertWidth, widthT);
        }

        private void CacheBarRootRestPosition()
        {
            RectTransform rt = barRoot != null ? barRoot : transform as RectTransform;
            if (rt == null)
                return;

            _barRootRestAnchoredPos = rt.anchoredPosition;
            _hasBarRootRestPos = true;
        }

        private void RestoreBarRootPosition()
        {
            if (!_hasBarRootRestPos || barRoot == null)
                return;

            barRoot.anchoredPosition = _barRootRestAnchoredPos;
        }
    }
}
