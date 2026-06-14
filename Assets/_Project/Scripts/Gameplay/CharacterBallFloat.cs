using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Flottement, respiration et ombre dynamique sur une CharacterBall au repos (Slice 2).
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
        [SerializeField] private float _bobAmplitude = 0.06f;
        [SerializeField] private float _bobPeriod = 1.8f;

        [Header("Respiration")]
        [SerializeField] private float _breathScale = 0.025f;
        [SerializeField] private float _breathPeriod = 2.4f;

        [Header("Ombre")]
        [SerializeField] private float _shadowAlphaRange = 0.12f;
        [SerializeField] private float _shadowScaleRange = 0.10f;

        [Header("Lissage")]
        [SerializeField] private float _settleSpeed = 8f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _phase;
        private Vector3 _visualBasePos;
        private Vector3 _visualBaseScale;
        private Vector3 _shadowBaseScale;
        private Color _shadowBaseColor;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_ball == null)
                _ball = GetComponent<CharacterBall>();

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

        private void LateUpdate()
        {
            bool atRest = _ball != null && _ball.IsAtRestForVisual;

            if (atRest)
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

                if (_shadowRenderer != null)
                {
                    Color c = _shadowBaseColor;
                    c.a = _shadowBaseColor.a - _shadowAlphaRange * up;
                    _shadowRenderer.color = c;
                }
            }
            else if (_visual != null)
            {
                _visual.localPosition = Vector3.Lerp(_visual.localPosition, _visualBasePos, Time.deltaTime * _settleSpeed);
                _visual.localScale = Vector3.Lerp(_visual.localScale, _visualBaseScale, Time.deltaTime * _settleSpeed);
            }
        }
    }
}
