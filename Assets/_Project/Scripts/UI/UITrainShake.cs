using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Applique un micro-tremblement subtil et permanent à un RectTransform pour simuler un train en mouvement.
    /// Effet organique via Perlin Noise et indépendant du timeScale (Hub en pause).
    /// </summary>
    public class UITrainShake : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float Y_SPEED_MULTIPLIER = 1.3f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Configuration")]
        [Tooltip("Amplitude max du tremblement en pixels.")]
        [SerializeField] private float shakeIntensity = 1.5f;

        [Tooltip("Vitesse de l'oscillation (plus haut = tremble plus vite).")]
        [SerializeField] private float shakeSpeed = 3f;

        [Header("Axes")]
        [SerializeField] private bool shakeX = true;
        [SerializeField] private bool shakeY = true;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPosition;
        private float _xSeed;
        private float _ySeed;
        private bool _hasBasePosition;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            if (_rectTransform == null)
                return;

            _baseAnchoredPosition = _rectTransform.anchoredPosition;
            _hasBasePosition = true;

            // Seeds stables par instance : évite que toutes les UI tremblent exactement pareil.
            int id = GetInstanceID();
            _xSeed = id * 0.017f + 11.3f;
            _ySeed = id * 0.031f + 27.7f;
        }

        private void Update()
        {
            if (!_hasBasePosition) return;
            if (_rectTransform == null) return;
            if (shakeIntensity <= 0f) return;
            if (!shakeX && !shakeY) return;

            float t = Time.unscaledTime;
            float intensity = shakeIntensity;

            float xOffset = 0f;
            float yOffset = 0f;

            if (shakeX)
            {
                float n = Mathf.PerlinNoise(_xSeed, t * shakeSpeed);
                xOffset = (n * 2f - 1f) * intensity;
            }

            if (shakeY)
            {
                float n = Mathf.PerlinNoise(_ySeed, t * shakeSpeed * Y_SPEED_MULTIPLIER);
                yOffset = (n * 2f - 1f) * intensity;
            }

            Vector2 p = _baseAnchoredPosition;
            p.x += xOffset;
            p.y += yOffset;
            _rectTransform.anchoredPosition = p;
        }

        private void OnDisable()
        {
            // Remet la position initiale pour éviter un décalage résiduel lors d'un disable/enable.
            if (!_hasBasePosition) return;
            if (_rectTransform == null) return;

            _rectTransform.anchoredPosition = _baseAnchoredPosition;
        }
    }
}

