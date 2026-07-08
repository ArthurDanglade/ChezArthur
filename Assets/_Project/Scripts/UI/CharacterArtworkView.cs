using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche l'artwork d'un personnage dans un RawImage avec crop focal (Cover) ou letterbox (Fit).
    /// </summary>
    public class CharacterArtworkView : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // TYPES
        // ═══════════════════════════════════════════
        public enum DisplayMode
        {
            Cover,
            Fit
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private RawImage rawImage;
        [SerializeField] private DisplayMode mode = DisplayMode.Cover;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Texture2D _currentTexture;
        private Vector2 _focal;
        private bool _isFallbackIcon;
        private DisplayMode _appliedMode;
        private AspectRatioFitter _aspectRatioFitter;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnRectTransformDimensionsChange()
        {
            ApplyUvRect();
        }

        private void OnValidate()
        {
            // Permet de basculer Cover/Fit en Play depuis l'Inspector (validation Gate 3).
            if (!Application.isPlaying || rawImage == null || rawImage.texture == null || _isFallbackIcon)
                return;

            _appliedMode = mode;
            ApplyDisplayMode();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche le portrait Resources du personnage, ou l'icône en fallback.
        /// </summary>
        public void Show(CharacterData character)
        {
            if (rawImage == null || character == null)
                return;

            ReleaseInternal();

            Texture2D portraitTexture = PortraitLoader.Load(character.Id);
            if (portraitTexture != null)
            {
                _currentTexture = portraitTexture;
                _focal = character.portraitFocalPoint;
                _isFallbackIcon = false;
                _appliedMode = mode;
                rawImage.texture = portraitTexture;
                ApplyDisplayMode();
                return;
            }

            Sprite icon = character.Icon;
            if (icon == null || icon.texture == null)
                return;

            _currentTexture = null;
            _focal = new Vector2(0.5f, 0.5f);
            _isFallbackIcon = true;
            _appliedMode = DisplayMode.Fit;
            rawImage.texture = icon.texture;
            ApplyDisplayMode();
        }

        /// <summary>
        /// Assignation directe d'une texture (ex. écran Déchu→Prime), sans passer par PortraitLoader.
        /// </summary>
        public void ShowTexture(Texture2D tex, Vector2 focalPoint01)
        {
            if (rawImage == null || tex == null)
                return;

            ReleaseInternal();

            _currentTexture = tex;
            _focal = focalPoint01;
            _isFallbackIcon = false;
            _appliedMode = mode;
            rawImage.texture = tex;
            ApplyDisplayMode();
        }

        /// <summary>
        /// Libère la texture affichée et réinitialise l'état.
        /// </summary>
        public void Release()
        {
            ClearRawImage();

            if (!_isFallbackIcon && _currentTexture != null)
                PortraitLoader.Release(_currentTexture);

            ResetState();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ReleaseInternal()
        {
            ClearRawImage();

            if (!_isFallbackIcon && _currentTexture != null)
                PortraitLoader.Release(_currentTexture);

            ResetState();
        }

        private void ClearRawImage()
        {
            if (rawImage == null)
                return;

            rawImage.texture = null;
            rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        private void ResetState()
        {
            _currentTexture = null;
            _focal = new Vector2(0.5f, 0.65f);
            _isFallbackIcon = false;
            _appliedMode = mode;

            if (_aspectRatioFitter != null)
                _aspectRatioFitter.enabled = false;
        }

        private void ApplyDisplayMode()
        {
            if (rawImage == null || rawImage.texture == null)
                return;

            if (_appliedMode == DisplayMode.Fit)
            {
                EnsureAspectRatioFitter();
                _aspectRatioFitter.enabled = true;
                _aspectRatioFitter.aspectRatio = GetTextureAspect(rawImage.texture);
                rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            if (_aspectRatioFitter != null)
                _aspectRatioFitter.enabled = false;

            ApplyUvRect();
        }

        /// <summary>
        /// Recadre la texture en mode Cover (object-fit: cover), ancré sur le point focal.
        /// </summary>
        private void ApplyUvRect()
        {
            if (rawImage == null || rawImage.texture == null || _appliedMode != DisplayMode.Cover)
                return;

            RectTransform rectTransform = rawImage.rectTransform;
            float containerW = rectTransform.rect.width;
            float containerH = rectTransform.rect.height;
            if (containerW <= 0f || containerH <= 0f)
                return;

            float containerAspect = containerW / containerH;
            float texAspect = GetTextureAspect(rawImage.texture);

            float uvW;
            float uvH;
            float uvX;
            float uvY;

            if (texAspect > containerAspect)
            {
                // Texture plus large que le cadre : rognage horizontal.
                uvH = 1f;
                uvW = containerAspect / texAspect;
                uvX = Mathf.Clamp(_focal.x - uvW * 0.5f, 0f, 1f - uvW);
                uvY = 0f;
            }
            else
            {
                // Texture plus haute que le cadre : rognage vertical.
                uvW = 1f;
                uvH = texAspect / containerAspect;
                uvX = 0f;
                uvY = Mathf.Clamp(_focal.y - uvH * 0.5f, 0f, 1f - uvH);
            }

            rawImage.uvRect = new Rect(uvX, uvY, uvW, uvH);
        }

        private void EnsureAspectRatioFitter()
        {
            if (rawImage == null)
                return;

            if (_aspectRatioFitter == null)
            {
                _aspectRatioFitter = rawImage.GetComponent<AspectRatioFitter>();
                if (_aspectRatioFitter == null)
                    _aspectRatioFitter = rawImage.gameObject.AddComponent<AspectRatioFitter>();

                _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            }
        }

        private static float GetTextureAspect(Texture texture)
        {
            if (texture == null || texture.height <= 0)
                return 1f;

            return (float)texture.width / texture.height;
        }
    }
}
