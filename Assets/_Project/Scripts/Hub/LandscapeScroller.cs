using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Fait défiler une image de paysage horizontalement pour simuler le mouvement du train.
    /// Utilise une RawImage pour pouvoir manipuler les UV et créer un défilement seamless.
    /// </summary>
    public class LandscapeScroller : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Configuration")]
        [SerializeField] private RawImage landscapeImage;
        [SerializeField] private float scrollSpeed = 0.1f;

        [Header("Effet de tremblement (train)")]
        [SerializeField] private RectTransform wagonTransform;
        [SerializeField] private float shakeIntensity = 1.5f;
        [SerializeField] private float shakeSpeed = 12f;

        [Header("Debug")]
        [SerializeField] private bool isScrolling = true;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Rect _uvRect;
        private Vector2 _wagonOriginalPosition;
        private bool _hasWagonTransform;
        private bool _shakeEnabled = true;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            if (landscapeImage == null)
            {
                landscapeImage = GetComponent<RawImage>();
            }

            if (landscapeImage != null)
            {
                _uvRect = landscapeImage.uvRect;
            }

            if (wagonTransform != null)
            {
                _hasWagonTransform = true;
                _wagonOriginalPosition = wagonTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (!isScrolling || landscapeImage == null) return;

            // Défile les UV horizontalement
            _uvRect.x += scrollSpeed * Time.deltaTime;

            // Garde la valeur entre 0 et 1 pour éviter les problèmes de précision
            if (_uvRect.x > 1f) _uvRect.x -= 1f;
            if (_uvRect.x < 0f) _uvRect.x += 1f;

            landscapeImage.uvRect = _uvRect;

            // Tremblement du wagon (effet train sur les rails)
            if (_shakeEnabled && _hasWagonTransform)
            {
                float offsetY = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
                float offsetX = Mathf.Sin(Time.time * shakeSpeed * 0.7f) * (shakeIntensity * 0.3f);

                wagonTransform.anchoredPosition = _wagonOriginalPosition + new Vector2(offsetX, offsetY);
            }
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
        /// Change la vitesse de défilement.
        /// </summary>
        public void SetScrollSpeed(float speed)
        {
            scrollSpeed = speed;
        }

        /// <summary>
        /// Change la texture du paysage (pour les transitions entre univers).
        /// </summary>
        public void SetLandscapeTexture(Texture texture)
        {
            if (landscapeImage != null)
            {
                landscapeImage.texture = texture;
            }
        }

        /// <summary>
        /// Active ou désactive l'effet de tremblement.
        /// </summary>
        public void SetShakeEnabled(bool enabled)
        {
            if (!enabled && _hasWagonTransform)
            {
                wagonTransform.anchoredPosition = _wagonOriginalPosition;
            }
            _shakeEnabled = enabled;
        }
    }
}
