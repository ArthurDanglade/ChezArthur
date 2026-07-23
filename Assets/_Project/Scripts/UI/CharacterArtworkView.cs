using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche l'artwork d'un personnage dans un RawImage avec crop focal (Cover) ou letterbox (Fit).
    /// SSR : sheet animé via PortraitAnimator ; état prime/déchu via PortraitStateResolver.
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
        private PortraitAnimator _animator;
        private float _contentAspect = 1f;
        /// <summary> True si le dernier affichage utilise un sheet animé (pas PlayStatic). </summary>
        private bool _isAnimatedPortrait;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnRectTransformDimensionsChange()
        {
            ApplyCoverCrop();
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
        /// Affiche le portrait (legacy) : toujours tente le sheet déchu en premier.
        /// </summary>
        public void Show(CharacterData character)
        {
            ShowWithAnimData(character, character != null ? character.AnimatedPortraitDechu : null);
        }

        /// <summary>
        /// Affiche le portrait selon l'état d'éveil / préférence (PortraitStateResolver).
        /// </summary>
        public void Show(CharacterData character, OwnedCharacter owned)
        {
            ShowWithAnimData(character, PortraitStateResolver.Resolve(character, owned));
        }

        /// <summary>
        /// Force un état d'artwork précis (cérémonie d'éveil).
        /// </summary>
        public void ShowState(CharacterData character, AnimatedPortraitData animData)
        {
            ShowWithAnimData(character, animData);
        }

        /// <summary>
        /// Pause / reprise de l'animation de sheet (uvRect figé pendant dissolve).
        /// </summary>
        public void SetAnimationPaused(bool paused)
        {
            if (_animator == null || !_isAnimatedPortrait)
                return;

            _animator.enabled = !paused;
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
            _contentAspect = GetTextureAspect(tex);
            rawImage.texture = tex;
            EnsurePortraitAnimator();
            _animator.PlayStatic();
            ApplyDisplayMode();
        }

        /// <summary>
        /// Force Cover (plein cadre) — obligatoire pour le reveal gacha (évite Fit/AspectRatioFitter).
        /// </summary>
        public void ForceCoverMode()
        {
            _appliedMode = DisplayMode.Cover;
            if (_aspectRatioFitter != null)
                _aspectRatioFitter.enabled = false;
            ApplyCoverCrop();
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

        /// <summary>
        /// Corps d'affichage commun : sheet animé optionnel, sinon Resources / icône.
        /// </summary>
        private void ShowWithAnimData(CharacterData character, AnimatedPortraitData animData)
        {
            if (rawImage == null || character == null)
                return;

            ReleaseInternal();

            if (animData != null && !string.IsNullOrEmpty(animData.ResourcesPath))
            {
                Texture2D sheet = PortraitLoader.LoadAtPath(animData.ResourcesPath);
                if (sheet != null)
                {
                    _currentTexture = sheet;
                    _focal = character.portraitFocalPoint;
                    _isFallbackIcon = false;
                    _appliedMode = mode;
                    _contentAspect = animData.CellHeight > 0
                        ? (float)animData.CellWidth / animData.CellHeight
                        : 1f;
                    rawImage.texture = sheet;
                    EnsurePortraitAnimator();
                    _animator.PlayAnimated(animData);
                    _isAnimatedPortrait = animData != null && !animData.IsStatic;
                    ApplyDisplayMode();
                    return;
                }

                Debug.LogWarning(
                    $"[CharacterArtworkView] Sheet SSR introuvable pour '{character.Id}' " +
                    $"(chemin : {animData.ResourcesPath}) — fallback portrait statique.");
            }

            Texture2D portraitTexture = PortraitLoader.Load(character.Id);
            if (portraitTexture != null)
            {
                _currentTexture = portraitTexture;
                _focal = character.portraitFocalPoint;
                _isFallbackIcon = false;
                _appliedMode = mode;
                _contentAspect = GetTextureAspect(portraitTexture);
                rawImage.texture = portraitTexture;
                EnsurePortraitAnimator();
                _animator.PlayStatic();
                _isAnimatedPortrait = false;
                ApplyDisplayMode();
                return;
            }

            Sprite icon = character.Icon;
            if (icon == null || icon.texture == null)
                return;

            _currentTexture = null;
            _focal = new Vector2(0.5f, 0.5f);
            _isFallbackIcon = true;
            _appliedMode = DisplayMode.Cover;
            _contentAspect = GetTextureAspect(icon.texture);
            rawImage.texture = icon.texture;
            EnsurePortraitAnimator();
            _animator.PlayStatic();
            _isAnimatedPortrait = false;
            ApplyDisplayMode();
        }

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

            if (_animator != null)
            {
                _animator.PlayStatic();
                _animator.SetCropRect(new Rect(0f, 0f, 1f, 1f));
            }
            else
            {
                rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            }

            rawImage.texture = null;
        }

        private void ResetState()
        {
            _currentTexture = null;
            _focal = new Vector2(0.5f, 0.65f);
            _isFallbackIcon = false;
            _appliedMode = mode;
            _contentAspect = 1f;
            _isAnimatedPortrait = false;

            if (_aspectRatioFitter != null)
                _aspectRatioFitter.enabled = false;
        }

        private void ApplyDisplayMode()
        {
            if (rawImage == null || rawImage.texture == null)
                return;

            EnsurePortraitAnimator();

            if (_appliedMode == DisplayMode.Fit)
            {
                EnsureAspectRatioFitter();
                _aspectRatioFitter.enabled = true;
                _aspectRatioFitter.aspectRatio = _contentAspect;
                _animator.SetCropRect(new Rect(0f, 0f, 1f, 1f));
                return;
            }

            if (_aspectRatioFitter != null)
                _aspectRatioFitter.enabled = false;

            ApplyCoverCrop();
        }

        /// <summary>
        /// Recadre en mode Cover (object-fit: cover), ancré sur le point focal.
        /// Crop exprimé en espace cellule ; composition UV via PortraitAnimator.
        /// </summary>
        private void ApplyCoverCrop()
        {
            if (rawImage == null || rawImage.texture == null || _appliedMode != DisplayMode.Cover)
                return;

            EnsurePortraitAnimator();

            RectTransform rectTransform = rawImage.rectTransform;
            float containerW = rectTransform.rect.width;
            float containerH = rectTransform.rect.height;
            if (containerW <= 0f || containerH <= 0f)
                return;

            float containerAspect = containerW / containerH;
            float texAspect = _contentAspect;

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

            _animator.SetCropRect(new Rect(uvX, uvY, uvW, uvH));
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

        private void EnsurePortraitAnimator()
        {
            if (rawImage == null)
                return;

            if (_animator == null)
            {
                _animator = rawImage.GetComponent<PortraitAnimator>();
                if (_animator == null)
                    _animator = rawImage.gameObject.AddComponent<PortraitAnimator>();

                _animator.Initialize(rawImage);
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
