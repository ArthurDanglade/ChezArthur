using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Gameplay;
using ChezArthur.Core;

namespace ChezArthur.UI
{
    /// <summary>
    /// Périmètre de pression HUD : cadre rectangulaire continu sous le HeaderBar.
    /// Le haut s'aligne automatiquement sur le bas du header (tous écrans / résolutions).
    /// Remplissage symétrique depuis le bas-centre ; boucle complète = Rupture.
    /// </summary>
    public class PressurePerimeterUI : MonoBehaviour
    {
        private struct SegmentCache
        {
            public Image Image;
            public float Length;
            public float HuePhase01;
            public Vector2 LocalStart;
            public Vector2 LocalEnd;
            public bool IsHorizontal;
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Chaînes (ordre : départ → arrivée)")]
        [SerializeField] private List<Image> leftChain = new List<Image>();
        [SerializeField] private List<Image> rightChain = new List<Image>();
        [SerializeField] private List<Image> ghostLeftChain = new List<Image>();
        [SerializeField] private List<Image> ghostRightChain = new List<Image>();

        [Header("Pointes")]
        [SerializeField] private RectTransform leftTip;
        [SerializeField] private RectTransform rightTip;
        [SerializeField] private Image leftTipImage;
        [SerializeField] private Image rightTipImage;

        [Header("Cadre (layout)")]
        [Tooltip("Si vide : cherche un sibling nommé HeaderBar.")]
        [SerializeField] private RectTransform topBoundary;
        [SerializeField] private float edgeInset = 3f;
        [SerializeField] private float topGap = 2f;
        [Tooltip("Vitesse d'affichage live (fraction de jauge / seconde).")]
        [SerializeField] private float fillLiveSpeed = 0.55f;

        [Header("Saillance")]
        [SerializeField] private Gradient fillGradient;
        [SerializeField] private float thicknessBase = 3f;
        [SerializeField] private float thicknessAlert = 5f;
        [SerializeField] private float alertZoneStart = 0.75f;
        [SerializeField] private float pulseSpeed = 1.8f;
        [SerializeField] private float pulseAmplitude = 0.06f;

        [Header("Ghost (pertes)")]
        [SerializeField] private float ghostDelay = 0.3f;
        [SerializeField] private float ghostDrainSpeed = 1.5f;

        [Header("Variation")]
        [SerializeField] private float changeFlashDuration = 0.15f;

        [Header("Rupture")]
        [SerializeField] private float completionFlashDuration = 0.25f;
        [SerializeField] private float hueSpeed = 0.35f;
        [SerializeField] private float hueSaturation = 0.85f;
        [SerializeField] private float hueValue = 1f;

        [Header("Refus de switch")]
        [SerializeField] private float deniedFlashDuration = 0.3f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _root;
        private SegmentCache[] _leftCache = Array.Empty<SegmentCache>();
        private SegmentCache[] _rightCache = Array.Empty<SegmentCache>();
        private SegmentCache[] _ghostLeftCache = Array.Empty<SegmentCache>();
        private SegmentCache[] _ghostRightCache = Array.Empty<SegmentCache>();
        private float _leftTotalLength;
        private float _rightTotalLength;
        private float _ghostLeftTotalLength;
        private float _ghostRightTotalLength;

        private float _displayFill;
        private float _targetFill;
        private float _displayGhost;
        private float _lastGhostDropTime;
        private float _changeFlashTimer;
        private float _completionFlashTimer;
        private float _deniedTimer;
        private bool _isInRupture;
        private bool _ruptureRainbowActive;
        private float _ruptureProgress01;
        private float _currentThickness;
        private float _tipPulsePhase;
        private Vector3 _leftTipBaseScale = Vector3.one;
        private Vector3 _rightTipBaseScale = Vector3.one;

        private bool _geometryReady;
        private bool _pressureSubscribed;
        private bool _ruptureSubscribed;
        private bool _runSubscribed;
        private bool _isFittingLayout;
        private float _lastAppliedTopInset = -1f;
        private float _lastParentHeight = -1f;

        private const float FillEpsilon = 0.001f;
        private const float ThicknessEpsilon = 0.05f;
        private const float ThicknessLerpSpeed = 12f;
        private const float TipPulseScale = 0.06f;
        private const string HeaderBarObjectName = "HeaderBar";
        private static readonly Color DeniedColor = UiTheme.EnemyTypeBossColor;
        private static readonly Vector3[] CornerBuffer = new Vector3[4];

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _root = transform as RectTransform;
            CacheTipBaseScales();
        }

        private void Reset()
        {
            // Palette UiTheme : Filet → AccentSection → SuperLancerZone → Negative
            fillGradient = new Gradient();
            fillGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(UiTheme.Filet, 0f),
                    new GradientColorKey(UiTheme.AccentSection, 0.5f),
                    new GradientColorKey(UiTheme.SuperLancerZone, 0.75f),
                    new GradientColorKey(UiTheme.Negative, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.90f, 0f),
                    new GradientAlphaKey(0.95f, 0.5f),
                    new GradientAlphaKey(1f, 0.75f),
                    new GradientAlphaKey(1f, 1f)
                });
        }

        private void Start()
        {
            StartCoroutine(InitializeDelayed());
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_isFittingLayout || !_geometryReady || _root == null)
                return;

            if (TryFitBelowHeader(force: false))
            {
                CacheGeometry();
                ApplyChainFills();
                RefreshStaticVisuals();
            }
        }

        private void Update()
        {
            if (_changeFlashTimer > 0f)
                _changeFlashTimer = Mathf.Max(0f, _changeFlashTimer - Time.deltaTime);

            if (_completionFlashTimer > 0f)
            {
                _completionFlashTimer = Mathf.Max(0f, _completionFlashTimer - Time.deltaTime);
                if (_completionFlashTimer <= 0f && _isInRupture)
                    _ruptureRainbowActive = true;
            }

            if (_deniedTimer > 0f)
                _deniedTimer = Mathf.Max(0f, _deniedTimer - Time.deltaTime);

            if (!_geometryReady)
                return;

            if (!_pressureSubscribed)
                TrySubscribe();

            if (!_isInRupture)
                UpdateLiveFill();

            if (!NeedsAnimatedUpdate())
            {
                RefreshStaticVisuals();
                return;
            }

            UpdateGhostDrain();
            ApplyChainFills();
            ApplyColorsAndThickness();
            UpdateTips();
        }

        // ═══════════════════════════════════════════
        // INITIALISATION
        // ═══════════════════════════════════════════

        private IEnumerator InitializeDelayed()
        {
            // Deux frames : laisse SafeArea + HeaderBar se poser avant la mesure.
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            TryFitBelowHeader(force: true);
            CacheGeometry();
            TrySubscribe();
            SyncFromSystems();
            ApplyChainFills();
            RefreshStaticVisuals();

            if (PressureGaugeSystem.Instance == null)
                Debug.LogWarning(
                    "[PressurePerimeterUI] PressureGaugeSystem absent — périmètre inactif. " +
                    "Menu : Chez Arthur/UI/Monter systèmes Pression (logique).");
        }

        /// <summary>
        /// Aligne le haut du cadre sur le bas du HeaderBar (ou topBoundary).
        /// Comme si le bas du header était le bord haut de l'écran.
        /// </summary>
        /// <returns>True si le rect a changé.</returns>
        private bool TryFitBelowHeader(bool force)
        {
            if (_root == null)
                _root = transform as RectTransform;

            if (_root == null || _isFittingLayout)
                return false;

            RectTransform parent = _root.parent as RectTransform;
            if (parent == null)
                return false;

            float parentHeight = parent.rect.height;
            float topInset = edgeInset;

            RectTransform header = ResolveTopBoundary(parent);
            if (header != null)
            {
                header.GetWorldCorners(CornerBuffer);
                // Coin bas-gauche du header → espace local du parent.
                Vector2 headerBottomLocal = parent.InverseTransformPoint(CornerBuffer[0]);
                topInset = parent.rect.yMax - headerBottomLocal.y + topGap;
                if (topInset < edgeInset)
                    topInset = edgeInset;
            }

            if (!force
                && Mathf.Abs(topInset - _lastAppliedTopInset) < 0.5f
                && Mathf.Abs(parentHeight - _lastParentHeight) < 0.5f)
                return false;

            _isFittingLayout = true;
            try
            {
                _root.anchorMin = Vector2.zero;
                _root.anchorMax = Vector2.one;
                _root.pivot = new Vector2(0.5f, 0.5f);
                _root.anchoredPosition = Vector2.zero;
                _root.offsetMin = new Vector2(edgeInset, edgeInset);
                _root.offsetMax = new Vector2(-edgeInset, -topInset);
            }
            finally
            {
                _isFittingLayout = false;
            }

            _lastAppliedTopInset = topInset;
            _lastParentHeight = parentHeight;
            return true;
        }

        private RectTransform ResolveTopBoundary(RectTransform parent)
        {
            if (topBoundary != null)
                return topBoundary;

            // Recherche sibling (même SafeArea) — pas de FindObjectOfType.
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name == HeaderBarObjectName)
                {
                    topBoundary = child as RectTransform;
                    return topBoundary;
                }
            }

            return null;
        }

        private void CacheGeometry()
        {
            _geometryReady = false;
            if (_root == null)
                _root = transform as RectTransform;

            CacheChain(leftChain, ref _leftCache, ref _leftTotalLength);
            CacheChain(rightChain, ref _rightCache, ref _rightTotalLength);
            CacheChain(ghostLeftChain, ref _ghostLeftCache, ref _ghostLeftTotalLength);
            CacheChain(ghostRightChain, ref _ghostRightCache, ref _ghostRightTotalLength);

            _currentThickness = thicknessBase;
            CacheTipBaseScales();
            _geometryReady = true;
        }

        private void CacheChain(List<Image> chain, ref SegmentCache[] cache, ref float totalLength)
        {
            totalLength = 0f;
            if (chain == null || chain.Count == 0)
            {
                cache = Array.Empty<SegmentCache>();
                return;
            }

            if (cache == null || cache.Length != chain.Count)
                cache = new SegmentCache[chain.Count];

            float cumul = 0f;
            for (int i = 0; i < chain.Count; i++)
            {
                Image img = chain[i];
                if (img == null)
                {
                    cache[i] = default;
                    continue;
                }

                RectTransform rt = img.rectTransform;
                float length = GetSegmentLength(img);
                Vector2 localStart;
                Vector2 localEnd;
                GetSegmentEndpoints(rt, img, out localStart, out localEnd);

                cache[i] = new SegmentCache
                {
                    Image = img,
                    Length = length,
                    HuePhase01 = 0f,
                    LocalStart = localStart,
                    LocalEnd = localEnd,
                    IsHorizontal = img.fillMethod == Image.FillMethod.Horizontal
                };

                cumul += length;
            }

            totalLength = cumul;
            if (totalLength <= 0f)
                return;

            float running = 0f;
            for (int i = 0; i < cache.Length; i++)
            {
                SegmentCache seg = cache[i];
                seg.HuePhase01 = running / totalLength;
                cache[i] = seg;
                running += seg.Length;
            }
        }

        private static float GetSegmentLength(Image img)
        {
            if (img == null)
                return 0f;

            RectTransform rt = img.rectTransform;
            return img.fillMethod == Image.FillMethod.Horizontal
                ? rt.rect.width
                : rt.rect.height;
        }

        private void GetSegmentEndpoints(RectTransform segmentRt, Image img, out Vector2 localStart, out Vector2 localEnd)
        {
            Vector3[] corners = new Vector3[4];
            segmentRt.GetWorldCorners(corners);

            Vector2 bl = _root.InverseTransformPoint(corners[0]);
            Vector2 tl = _root.InverseTransformPoint(corners[1]);
            Vector2 tr = _root.InverseTransformPoint(corners[2]);
            Vector2 br = _root.InverseTransformPoint(corners[3]);

            if (img.fillMethod == Image.FillMethod.Horizontal)
            {
                Vector2 left = (bl + tl) * 0.5f;
                Vector2 right = (br + tr) * 0.5f;
                bool originRight = img.fillOrigin == (int)Image.OriginHorizontal.Right;
                localStart = originRight ? right : left;
                localEnd = originRight ? left : right;
            }
            else
            {
                Vector2 bottom = (bl + br) * 0.5f;
                Vector2 top = (tl + tr) * 0.5f;
                bool originTop = img.fillOrigin == (int)Image.OriginVertical.Top;
                localStart = originTop ? top : bottom;
                localEnd = originTop ? bottom : top;
            }
        }

        private void CacheTipBaseScales()
        {
            if (leftTip != null)
                _leftTipBaseScale = leftTip.localScale;
            if (rightTip != null)
                _rightTipBaseScale = rightTip.localScale;
        }

        // ═══════════════════════════════════════════
        // ABONNEMENTS
        // ═══════════════════════════════════════════

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

            RunManager run = RunManager.Instance;
            if (run != null && !_runSubscribed)
            {
                run.OnRunStarted += HandleRunStarted;
                _runSubscribed = true;
            }
        }

        private void HandleRunStarted()
        {
            SyncFromSystems();
            ApplyChainFills();
            RefreshStaticVisuals();
        }

        private void UpdateLiveFill()
        {
            if (Mathf.Abs(_displayFill - _targetFill) <= FillEpsilon)
                return;

            float previous = _displayFill;
            _displayFill = Mathf.MoveTowards(_displayFill, _targetFill, fillLiveSpeed * Time.deltaTime);

            if (_displayFill > previous)
            {
                _displayGhost = Mathf.Max(_displayGhost, _displayFill);
                _changeFlashTimer = changeFlashDuration;
            }

            ApplyChainFills();
            UpdateTips();
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

            RunManager run = RunManager.Instance;
            if (run != null && _runSubscribed)
            {
                run.OnRunStarted -= HandleRunStarted;
                _runSubscribed = false;
            }
        }

        private void SyncFromSystems()
        {
            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure == null)
                return;

            _isInRupture = pressure.IsInRupture;
            _ruptureRainbowActive = _isInRupture;
            _ruptureProgress01 = _isInRupture ? pressure.RuptureProgress01 : 0f;

            if (_isInRupture)
            {
                float drain = 1f - _ruptureProgress01;
                _displayFill = drain;
                _displayGhost = drain;
            }
            else
            {
                _targetFill = Mathf.Clamp01(pressure.NormalizedValue);
                _displayFill = _targetFill;
                _displayGhost = _targetFill;
            }

            _changeFlashTimer = 0f;
            _completionFlashTimer = 0f;
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
            _targetFill = newFill;

            if (newFill < _displayFill - FillEpsilon)
            {
                _displayFill = newFill;
                _lastGhostDropTime = Time.time;
            }

            ApplyChainFills();
            UpdateTips();

            if (!NeedsAnimatedUpdate())
                RefreshStaticVisuals();
        }

        private void HandleRuptureTriggered()
        {
            _isInRupture = true;
            _ruptureRainbowActive = false;
            _ruptureProgress01 = 0f;
            _displayFill = 1f;
            _displayGhost = 1f;
            _completionFlashTimer = completionFlashDuration;
            _changeFlashTimer = 0f;
            SetTipsVisible(false);
            ApplyChainFills();
        }

        private void HandleRuptureProgress(float progress)
        {
            if (!_isInRupture)
                return;

            _ruptureProgress01 = Mathf.Clamp01(progress);
            float drain = 1f - _ruptureProgress01;
            _displayFill = drain;
            _displayGhost = drain;
            ApplyChainFills();
        }

        private void HandleRuptureEnded()
        {
            _isInRupture = false;
            _ruptureRainbowActive = false;
            _ruptureProgress01 = 0f;
            _completionFlashTimer = 0f;
            _targetFill = 0f;
            _displayFill = 0f;
            _displayGhost = 0f;
            ApplyChainFills();
        }

        private void HandleSpecSwitchDenied()
        {
            _deniedTimer = deniedFlashDuration;
        }

        // ═══════════════════════════════════════════
        // REMPLISSAGE & POINTES
        // ═══════════════════════════════════════════

        private void ApplyChainFills()
        {
            FillChain(_leftCache, _leftTotalLength, _displayFill);
            FillChain(_rightCache, _rightTotalLength, _displayFill);
            FillChain(_ghostLeftCache, _ghostLeftTotalLength, _displayGhost);
            FillChain(_ghostRightCache, _ghostRightTotalLength, _displayGhost);
        }

        private static void FillChain(SegmentCache[] cache, float totalLength, float v01)
        {
            if (cache == null || cache.Length == 0 || totalLength <= 0f)
                return;

            float target = Mathf.Clamp01(v01) * totalLength;
            float cumul = 0f;

            for (int i = 0; i < cache.Length; i++)
            {
                Image img = cache[i].Image;
                if (img == null)
                    continue;

                float segLen = cache[i].Length;
                if (segLen <= 0f)
                {
                    img.fillAmount = 0f;
                    continue;
                }

                float segFill = Mathf.Clamp01((target - cumul) / segLen);
                img.fillAmount = segFill;
                cumul += segLen;
            }
        }

        private void UpdateTips()
        {
            if (_isInRupture)
            {
                SetTipsVisible(false);
                return;
            }

            PositionTip(_leftCache, _leftTotalLength, leftTip, leftTipImage, _displayFill);
            PositionTip(_rightCache, _rightTotalLength, rightTip, rightTipImage, _displayFill);
        }

        private void PositionTip(
            SegmentCache[] cache,
            float totalLength,
            RectTransform tip,
            Image tipImage,
            float v01)
        {
            bool show = v01 > FillEpsilon && v01 < 1f - FillEpsilon;
            if (tip != null)
                tip.gameObject.SetActive(show);

            if (!show || tip == null || cache == null || totalLength <= 0f)
                return;

            float pos = v01 * totalLength;
            float cumul = 0f;
            Vector2 localPos = Vector2.zero;
            bool found = false;

            for (int i = 0; i < cache.Length; i++)
            {
                float segLen = cache[i].Length;
                if (segLen <= 0f)
                    continue;

                if (pos <= cumul + segLen + FillEpsilon)
                {
                    float t = segLen > 0f ? Mathf.Clamp01((pos - cumul) / segLen) : 0f;
                    localPos = Vector2.Lerp(cache[i].LocalStart, cache[i].LocalEnd, t);
                    found = true;
                    break;
                }

                cumul += segLen;
            }

            if (!found && cache.Length > 0)
                localPos = cache[cache.Length - 1].LocalEnd;

            tip.anchoredPosition = localPos;

            if (tipImage != null && fillGradient != null)
            {
                Color c = fillGradient.Evaluate(v01);
                tipImage.color = c;
            }
        }

        private void SetTipsVisible(bool visible)
        {
            if (leftTip != null)
                leftTip.gameObject.SetActive(visible);
            if (rightTip != null)
                rightTip.gameObject.SetActive(visible);
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
            if (_changeFlashTimer > 0f)
                return true;

            if (_completionFlashTimer > 0f)
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

            float targetThickness = GetTargetThickness(effectiveFill);
            if (Mathf.Abs(_currentThickness - targetThickness) > ThicknessEpsilon)
                return true;

            if (_displayFill > FillEpsilon && _displayFill < 1f - FillEpsilon)
                return true;

            if (!_isInRupture && Mathf.Abs(_displayFill - _targetFill) > FillEpsilon)
                return true;

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

        private void RefreshStaticVisuals()
        {
            ApplyColorsAndThickness();
            UpdateTips();
        }

        private float GetTargetThickness(float effectiveFill)
        {
            if (_isInRupture)
                return thicknessAlert;

            float t = Mathf.InverseLerp(alertZoneStart, 1f, effectiveFill);
            return Mathf.Lerp(thicknessBase, thicknessAlert, t);
        }

        private void ApplyColorsAndThickness()
        {
            float effectiveFill = GetEffectiveFill();
            float targetThickness = GetTargetThickness(effectiveFill);
            _currentThickness = Mathf.Lerp(
                _currentThickness,
                targetThickness,
                Time.deltaTime * ThicknessLerpSpeed);

            float pulseFactor = 0f;
            if (_isInRupture)
                pulseFactor = 1f;
            else if (effectiveFill >= alertZoneStart)
                pulseFactor = Mathf.InverseLerp(alertZoneStart, 1f, effectiveFill);

            float alphaPulse = 1f;
            if (pulseFactor > 0f)
                alphaPulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude * pulseFactor;

            float changeFlashBlend = 0f;
            if (_changeFlashTimer > 0f && changeFlashDuration > 0f)
                changeFlashBlend = _changeFlashTimer / changeFlashDuration;

            float completionFlashBlend = 0f;
            if (_completionFlashTimer > 0f && completionFlashDuration > 0f)
                completionFlashBlend = _completionFlashTimer / completionFlashDuration;

            float deniedBlend = 0f;
            if (_deniedTimer > 0f && deniedFlashDuration > 0f)
            {
                float elapsed = 1f - (_deniedTimer / deniedFlashDuration);
                deniedBlend = Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 4f)) * 0.75f;
            }

            ApplyChainVisuals(
                _leftCache,
                effectiveFill,
                alphaPulse,
                changeFlashBlend,
                completionFlashBlend,
                deniedBlend,
                applyThickness: true);

            ApplyChainVisuals(
                _rightCache,
                effectiveFill,
                alphaPulse,
                changeFlashBlend,
                completionFlashBlend,
                deniedBlend,
                applyThickness: true);

            ApplyChainVisuals(
                _ghostLeftCache,
                _displayGhost,
                alphaPulse * 0.65f,
                0f,
                0f,
                0f,
                applyThickness: false);

            ApplyChainVisuals(
                _ghostRightCache,
                _displayGhost,
                alphaPulse * 0.65f,
                0f,
                0f,
                0f,
                applyThickness: false);

            ApplyTipPulse();
        }

        private void ApplyChainVisuals(
            SegmentCache[] cache,
            float fillValue,
            float alphaPulse,
            float changeFlashBlend,
            float completionFlashBlend,
            float deniedBlend,
            bool applyThickness)
        {
            if (cache == null)
                return;

            for (int i = 0; i < cache.Length; i++)
            {
                Image img = cache[i].Image;
                if (img == null)
                    continue;

                Color baseColor;
                if (_isInRupture && _ruptureRainbowActive)
                {
                    float hue = Mathf.Repeat(Time.time * hueSpeed + cache[i].HuePhase01, 1f);
                    baseColor = Color.HSVToRGB(hue, hueSaturation, hueValue);
                    baseColor.a = 1f;
                }
                else if (fillGradient != null)
                {
                    baseColor = fillGradient.Evaluate(fillValue);
                }
                else
                {
                    baseColor = Color.white;
                }

                Color result = baseColor;

                if (completionFlashBlend > 0f)
                    result = Color.Lerp(result, Color.white, completionFlashBlend);
                else if (changeFlashBlend > 0f)
                    result = Color.Lerp(result, Color.white, changeFlashBlend);

                if (deniedBlend > 0f)
                    result = Color.Lerp(result, DeniedColor, deniedBlend);

                result.a = Mathf.Clamp01(baseColor.a * alphaPulse);
                img.color = result;

                if (!applyThickness)
                    continue;

                RectTransform rt = img.rectTransform;
                Vector2 size = rt.sizeDelta;
                if (cache[i].IsHorizontal)
                {
                    if (Mathf.Abs(size.y - _currentThickness) > ThicknessEpsilon)
                    {
                        size.y = _currentThickness;
                        rt.sizeDelta = size;
                    }
                }
                else
                {
                    if (Mathf.Abs(size.x - _currentThickness) > ThicknessEpsilon)
                    {
                        size.x = _currentThickness;
                        rt.sizeDelta = size;
                    }
                }
            }
        }

        private void ApplyTipPulse()
        {
            _tipPulsePhase += Time.deltaTime * pulseSpeed;
            float pulse = 1f + Mathf.Sin(_tipPulsePhase) * TipPulseScale;

            if (leftTip != null && leftTip.gameObject.activeSelf)
                leftTip.localScale = _leftTipBaseScale * pulse;

            if (rightTip != null && rightTip.gameObject.activeSelf)
                rightTip.localScale = _rightTipBaseScale * pulse;
        }
    }
}
