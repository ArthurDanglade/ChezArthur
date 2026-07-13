using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Flottement, respiration, ombre et armement visuel sur une CharacterBall (Slice 2).
    /// </summary>
    public class CharacterBallFloat : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private CharacterBall _ball;
        [SerializeField] private Transform _visual;
        [SerializeField] private Transform _shadow;
        [SerializeField] private SpriteRenderer _shadowRenderer;

        [Header("Bob")]
        [SerializeField] private float _bobAmplitude = 0.03f;
        [SerializeField] private float _bobPeriod = 1.8f;

        [Header("Respiration")]
        [SerializeField] private float _breathScale = 0.012f;
        [SerializeField] private float _breathPeriod = 2.4f;

        [Header("Ombre")]
        [SerializeField] private float _shadowAlphaRange = 0.08f;
        [SerializeField] private float _shadowScaleRange = 0.06f;

        [Header("Lissage")]
        [SerializeField] private float _settleSpeed = 8f;

        [Header("Armement")]
        [SerializeField] private float _armTrembleMax = 0.045f;
        [SerializeField] private float _armChargeScale = 0.12f;

        [Header("Charge Super Lancer")]
        [Tooltip("Amplitude du tremblement pendant la charge — plus fort que l'armement")]
        [SerializeField] private float _chargeTrembleAmp = 0.09f;
        [Tooltip("Compression du visuel pendant la charge (ressort bandé)")]
        [SerializeField] private float _chargeCompressScale = 0.14f;

        [Header("Lâcher")]
        [SerializeField] private float _launchStretchAmount = 0.4f;
        [SerializeField] private float _launchStretchDuration = 0.14f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _phase;
        private Vector3 _visualBasePos;
        private Vector3 _visualBaseScale;
        private Vector3 _shadowBaseScale;
        private Color _shadowBaseColor;
        private float _launchStretchTimer;
        private Vector2 _launchDir = Vector2.up;
        private float _superChargeTimer;
        private AuraController _auraController;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_ball == null)
                _ball = GetComponent<CharacterBall>();

            _auraController = GetComponent<AuraController>();
            _phase = Random.value * Mathf.PI * 2f;
            CaptureBases();
        }

        /// <summary>
        /// Mémorise pos/scale Visual et scale/couleur Shadow (appelé après normalisation factory).
        /// </summary>
        public void CaptureBases()
        {
            if (_visual != null)
            {
                _visualBasePos = _visual.localPosition;
                _visualBaseScale = _visual.localScale;
            }

            if (_shadow != null)
                _shadowBaseScale = _shadow.localScale;

            if (_shadowRenderer != null)
                _shadowBaseColor = _shadowRenderer.color;
        }

        /// <summary>
        /// Squash-stretch directionnel au lâcher (prioritaire sur float / lerp).
        /// </summary>
        public void TriggerLaunchStretch(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
                _launchDir = direction.normalized;
            _launchStretchTimer = _launchStretchDuration;
        }

        /// <summary> Joue la charge Super Lancer (tremblement + compression) pendant la durée donnée. </summary>
        public void TriggerSuperCharge(float duration) => _superChargeTimer = duration;

        private void LateUpdate()
        {
            if (_ball != null && _ball.IsArming)
                ApplyArming(_ball.ArmingIntensity);
            else if (_superChargeTimer > 0f)
                ApplySuperCharge();
            else if (_launchStretchTimer > 0f)
                ApplyLaunchStretch();
            else if (_ball != null && _ball.IsAtRestForVisual)
                ApplyFloat();
            else
                LerpToBase();
        }

        private void ApplyArming(float intensity)
        {
            if (_visual == null) return;

            Vector3 jitter = (Vector3)(Random.insideUnitCircle * (_armTrembleMax * intensity));
            _visual.localPosition = _visualBasePos + jitter;
            float charge = 1f + _armChargeScale * intensity;
            _visual.localScale = _visualBaseScale * charge;
        }

        private void ApplySuperCharge()
        {
            if (_visual == null) return;

            _superChargeTimer -= Time.unscaledDeltaTime;
            // Temps réel : le gel (ApplyHitStop) est en WaitForSecondsRealtime,
            // la charge doit vivre sur la même horloge.
            Vector3 jitter = (Vector3)(Random.insideUnitCircle * _chargeTrembleAmp);
            _visual.localPosition = _visualBasePos + jitter;
            _visual.localScale = _visualBaseScale * (1f - _chargeCompressScale);

            if (_superChargeTimer <= 0f)
            {
                _superChargeTimer = 0f;
                _visual.localPosition = _visualBasePos;
                _visual.localScale = _visualBaseScale;
            }
        }

        private void ApplyLaunchStretch()
        {
            if (_visual == null) return;

            _launchStretchTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_launchStretchTimer / _launchStretchDuration);
            float stretch = _launchStretchAmount * t;
            float angle = Mathf.Atan2(_launchDir.y, _launchDir.x) * Mathf.Rad2Deg - 90f;

            _visual.localPosition = _visualBasePos;
            _visual.localRotation = Quaternion.Euler(0f, 0f, angle);
            _visual.localScale = new Vector3(
                _visualBaseScale.x * (1f - stretch * 0.6f),
                _visualBaseScale.y * (1f + stretch),
                _visualBaseScale.z);

            if (_launchStretchTimer <= 0f)
            {
                _visual.localRotation = Quaternion.identity;
                _visual.localScale = _visualBaseScale;
            }
        }

        private void ApplyFloat()
        {
            float t = Time.time;
            float bob = Mathf.Sin(_phase + t * (Mathf.PI * 2f / _bobPeriod)) * _bobAmplitude;
            float breath = 1f + Mathf.Sin(_phase + t * (Mathf.PI * 2f / _breathPeriod)) * _breathScale;

            if (_visual != null)
            {
                _visual.localPosition = _visualBasePos + new Vector3(0f, bob, 0f);
                _visual.localScale = _visualBaseScale * breath;
            }

            float up = bob / Mathf.Max(0.0001f, _bobAmplitude);
            if (_shadow != null)
                _shadow.localScale = _shadowBaseScale * (1f - _shadowScaleRange * up);

            if (_shadowRenderer != null && (_auraController == null || !_auraController.UsesGroundRing))
            {
                Color c = _shadowBaseColor;
                c.a = _shadowBaseColor.a - _shadowAlphaRange * up;
                _shadowRenderer.color = c;
            }
        }

        private void LerpToBase()
        {
            if (_visual == null) return;

            _visual.localPosition = Vector3.Lerp(_visual.localPosition, _visualBasePos, Time.deltaTime * _settleSpeed);
            _visual.localScale = Vector3.Lerp(_visual.localScale, _visualBaseScale, Time.deltaTime * _settleSpeed);
        }
    }
}
