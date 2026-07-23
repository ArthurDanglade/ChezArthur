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
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public RectTransform RootRect => transform as RectTransform;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            EnsureUvInitialized();

            if (wagonTransform != null)
            {
                _hasWagonTransform = true;
                _wagonOriginalPosition = wagonTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (isScrolling)
                UpdateParallax();

            if (isShaking && _hasWagonTransform)
                UpdateShake();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureUvInitialized()
        {
            if (layers == null)
                return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].image != null
                    && (layers[i].uvRect.width <= 0f || layers[i].uvRect.height <= 0f))
                {
                    layers[i].uvRect = layers[i].image.uvRect;
                }
            }
        }

        private void UpdateParallax()
        {
            if (layers == null)
                return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].image == null)
                    continue;

                float speed = layers[i].scrollSpeed * _speedMultiplier;
                layers[i].uvRect.x += speed * Time.unscaledDeltaTime;

                if (layers[i].uvRect.x > 1f)
                    layers[i].uvRect.x -= 1f;
                if (layers[i].uvRect.x < 0f)
                    layers[i].uvRect.x += 1f;

                layers[i].image.uvRect = layers[i].uvRect;
            }
        }

        private void UpdateShake()
        {
            float offsetY = Mathf.Sin(Time.unscaledTime * shakeSpeed) * shakeIntensity;
            float offsetX = Mathf.Sin(Time.unscaledTime * shakeSpeed * 0.7f)
                * (shakeIntensity * 0.3f);

            wagonTransform.anchoredPosition =
                _wagonOriginalPosition + new Vector2(offsetX, offsetY);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void SetScrolling(bool value)
        {
            isScrolling = value;
        }

        public void SetShaking(bool value)
        {
            isShaking = value;
            if (!value && _hasWagonTransform)
                wagonTransform.anchoredPosition = _wagonOriginalPosition;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Mode invocation : scroll sans shake wagon Hub.
        /// </summary>
        public void BeginGachaBorrow()
        {
            EnsureUvInitialized();
            SetShaking(false);
            SetScrolling(true);
            SetSpeedMultiplier(1f);
            gameObject.SetActive(true);
        }

        public void EndGachaBorrow()
        {
            SetSpeedMultiplier(1f);
            SetScrolling(true);
            SetShaking(true);
        }
    }
}
