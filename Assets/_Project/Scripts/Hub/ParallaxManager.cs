using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Gère le défilement parallaxe de plusieurs couches de paysage.
    /// </summary>
    public class ParallaxManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CLASSES INTERNES
        // ═══════════════════════════════════════════

        [System.Serializable]
        public class ParallaxLayer
        {
            public RawImage image;
            public float scrollSpeed;
            [HideInInspector] public Rect uvRect;
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Couches de parallaxe (arrière vers avant)")]
        [SerializeField] private ParallaxLayer[] layers;

        [Header("Effet de tremblement (train)")]
        [SerializeField] private RectTransform wagonTransform;
        [SerializeField] private float shakeIntensity = 2f;
        [SerializeField] private float shakeSpeed = 15f;

        [Header("Contrôles")]
        [SerializeField] private bool isScrolling = true;
        [SerializeField] private bool isShaking = true;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Vector2 _wagonOriginalPosition;
        private bool _hasWagonTransform;
        private float _speedMultiplier = 1f;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            // Initialise les UV de chaque couche
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].image != null)
                {
                    layers[i].uvRect = layers[i].image.uvRect;
                }
            }

            // Initialise le wagon pour le shake
            if (wagonTransform != null)
            {
                _hasWagonTransform = true;
                _wagonOriginalPosition = wagonTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (isScrolling)
            {
                UpdateParallax();
            }

            if (isShaking && _hasWagonTransform)
            {
                UpdateShake();
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Met à jour le défilement de toutes les couches.
        /// </summary>
        private void UpdateParallax()
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].image == null) continue;

                float speed = layers[i].scrollSpeed * _speedMultiplier;
                layers[i].uvRect.x += speed * Time.deltaTime;

                // Garde entre 0 et 1
                if (layers[i].uvRect.x > 1f) layers[i].uvRect.x -= 1f;
                if (layers[i].uvRect.x < 0f) layers[i].uvRect.x += 1f;

                layers[i].image.uvRect = layers[i].uvRect;
            }
        }

        /// <summary>
        /// Applique l'effet de tremblement au wagon.
        /// </summary>
        private void UpdateShake()
        {
            float offsetY = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
            float offsetX = Mathf.Sin(Time.time * shakeSpeed * 0.7f) * (shakeIntensity * 0.3f);

            wagonTransform.anchoredPosition = _wagonOriginalPosition + new Vector2(offsetX, offsetY);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Active ou désactive le défilement.
        /// </summary>
        public void SetScrolling(bool value)
        {
            isScrolling = value;
        }

        /// <summary>
        /// Active ou désactive le tremblement.
        /// </summary>
        public void SetShaking(bool value)
        {
            isShaking = value;
            if (!value && _hasWagonTransform)
            {
                wagonTransform.anchoredPosition = _wagonOriginalPosition;
            }
        }

        /// <summary>
        /// Multiplie la vitesse de toutes les couches (pour accélérer/ralentir le train).
        /// Les vitesses de base restent celles définies dans l'Inspector.
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0f, multiplier);
        }
    }
}
