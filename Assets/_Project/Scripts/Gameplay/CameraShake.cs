using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Tremblement de caméra par trauma (Perlin noise). Ne modifie que transform local ;
    /// l'orthographicSize reste géré par ArenaCamera.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private float _maxOffset = 0.25f;
        [SerializeField] private float _maxRotation = 1.5f;
        [SerializeField] private float _traumaDecay = 1.6f;
        [SerializeField] private float _noiseFrequency = 22f;
        [SerializeField] private float _traumaPower = 2f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _trauma;
        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private Vector3 _cineOffset = Vector3.zero;
        private float _cineTilt;
        private float _seedX;
        private float _seedY;
        private float _seedR;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public Vector3 BasePosition => _basePosition;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _basePosition = transform.localPosition;
            _baseRotation = transform.localRotation;
            _seedX = Random.value * 100f;
            _seedY = Random.value * 100f;
            _seedR = Random.value * 100f;
        }

        private void LateUpdate()
        {
            float shake = Mathf.Pow(_trauma, _traumaPower);
            float n = Time.unscaledTime * _noiseFrequency;
            float ox = (Mathf.PerlinNoise(_seedX, n) * 2f - 1f) * _maxOffset * shake;
            float oy = (Mathf.PerlinNoise(_seedY, n) * 2f - 1f) * _maxOffset * shake;
            float rz = (Mathf.PerlinNoise(_seedR, n) * 2f - 1f) * _maxRotation * shake;
            transform.localPosition = _basePosition + _cineOffset + new Vector3(ox, oy, 0f);
            transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, _cineTilt + rz);

            if (_trauma > 0f)
                _trauma = Mathf.Max(0f, _trauma - _traumaDecay * Time.unscaledDeltaTime);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Offset et tilt cinématiques (finisher) — le shake s'ajoute par-dessus en LateUpdate.
        /// </summary>
        public void SetCinematic(Vector2 offset, float tiltDegrees)
        {
            _cineOffset = new Vector3(offset.x, offset.y, 0f);
            _cineTilt = tiltDegrees;
        }

        /// <summary>
        /// Ajoute du trauma (0–1, cumul clampé).
        /// </summary>
        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }
    }
}
