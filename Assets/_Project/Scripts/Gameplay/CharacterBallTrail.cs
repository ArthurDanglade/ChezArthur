using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Traînée de vol sur la bille : émission et largeur scalées sur la vélocité physique.
    /// </summary>
    public class CharacterBallTrail : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private CharacterBall _ball;

        [Header("Réglages")]
        [SerializeField] private float _minSpeedToEmit = 4f;
        [SerializeField] private float _speedForMaxWidth = 30f;
        [SerializeField] private float _minWidthMul = 0.4f;
        [SerializeField] private float _maxWidthMul = 1f;

        [Header("Variante Super Lancer")]
        [Tooltip("Multiplicateur de largeur appliqué par-dessus la largeur vélocité pendant un vol Super")]
        [SerializeField] private float _superWidthBoost = 1.5f;
        [Tooltip("Multiplicateur de durée de vie de la traînée pendant un vol Super")]
        [SerializeField] private float _superTimeBoost = 1.8f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _baseTime;
        private Color _baseStartColor;
        private Color _baseEndColor;
        private bool _superApplied;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Reset()
        {
            _trail = GetComponentInChildren<TrailRenderer>();
            _ball = GetComponent<CharacterBall>();
        }

        private void Awake()
        {
            if (_trail == null) return;

            _baseTime = _trail.time;
            _baseStartColor = _trail.startColor;
            _baseEndColor = _trail.endColor;
        }

        private void LateUpdate()
        {
            if (_trail == null || _ball == null) return;

            // Synchronisation Super uniquement au changement — pas d'écriture couleur/temps par frame.
            bool isSuper = _ball.IsSuperLaunch;
            if (isSuper != _superApplied)
            {
                _superApplied = isSuper;
                if (isSuper)
                {
                    _trail.time = _baseTime * _superTimeBoost;
                    Color zone = UiTheme.SuperLancerZone;
                    _trail.startColor = zone;
                    _trail.endColor = new Color(zone.r, zone.g, zone.b, 0f);
                }
                else
                {
                    _trail.time = _baseTime;
                    _trail.startColor = _baseStartColor;
                    _trail.endColor = _baseEndColor;
                }
            }

            float speed = _ball.CurrentVelocity;
            bool fast = speed > _minSpeedToEmit && !_ball.IsDead;
            _trail.emitting = fast;

            if (fast)
            {
                float t = Mathf.Clamp01(
                    (speed - _minSpeedToEmit) / Mathf.Max(0.01f, _speedForMaxWidth - _minSpeedToEmit));
                float widthMultiplier = Mathf.Lerp(_minWidthMul, _maxWidthMul, t);
                if (_superApplied)
                    widthMultiplier *= _superWidthBoost;
                _trail.widthMultiplier = widthMultiplier;
            }
        }
    }
}
