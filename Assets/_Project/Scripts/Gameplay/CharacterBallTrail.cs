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

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Reset()
        {
            _trail = GetComponentInChildren<TrailRenderer>();
            _ball = GetComponent<CharacterBall>();
        }

        private void LateUpdate()
        {
            if (_trail == null || _ball == null) return;

            float speed = _ball.CurrentVelocity;
            bool fast = speed > _minSpeedToEmit && !_ball.IsDead;
            _trail.emitting = fast;

            if (fast)
            {
                float t = Mathf.Clamp01(
                    (speed - _minSpeedToEmit) / Mathf.Max(0.01f, _speedForMaxWidth - _minSpeedToEmit));
                _trail.widthMultiplier = Mathf.Lerp(_minWidthMul, _maxWidthMul, t);
            }
        }
    }
}
