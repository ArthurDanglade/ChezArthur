using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Badge de rareté UGUI : frame idle ou flipbook léger (ni Animator ni DOTween).
    /// 1 frame = statique (composant inerte, zéro coût Update).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class RarityBadgeView : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════

        [Header("Source")]
        [SerializeField] private RarityVisualLibrary library;

        [Header("Animation")]
        [Tooltip("True = flipbook. Grille et popup : animés (BR-D4 assoupli au device).")]
        [SerializeField] private bool playAnimation;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private Image _image;
        private Sprite[] _frames;
        private int _frameCount;
        private float _fps;
        private int _frameIndex;
        private float _nextFrameTime;
        private int _idleIndex;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void Awake()
        {
            CacheImage();
        }

        private void Update()
        {
            if (_frameCount <= 1 || _frames == null || _image == null)
                return;

            float now = Time.unscaledTime;
            if (now < _nextFrameTime)
                return;

            _frameIndex++;
            if (_frameIndex >= _frameCount)
                _frameIndex = 0;

            Sprite next = _frames[_frameIndex];
            if (_image.sprite != next)
                _image.sprite = next;

            float step = _fps > 0f ? 1f / _fps : 0.1f;
            _nextFrameTime = now + step;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Pose la frame idle pour la rareté. Active le flipbook seulement si
        /// playAnimation et plus d'une frame.
        /// </summary>
        public void Bind(CharacterRarity rarity)
        {
            CacheImage();
            if (_image == null)
                return;

            _image.preserveAspect = true;
            _image.raycastTarget = false;

            if (library == null)
            {
                _image.enabled = false;
                enabled = false;
                _frames = null;
                _frameCount = 0;
                return;
            }

            _frames = library.GetBadgeFrames(rarity);
            if (_frames == null || _frames.Length == 0)
            {
                _image.enabled = false;
                enabled = false;
                _frameCount = 0;
                return;
            }

            _frameCount = _frames.Length;
            _fps = library.GetFps(rarity);
            _idleIndex = 0;
            Sprite idle = library.GetIdleFrame(rarity);
            if (idle != null)
            {
                for (int i = 0; i < _frameCount; i++)
                {
                    if (_frames[i] == idle)
                    {
                        _idleIndex = i;
                        break;
                    }
                }
            }

            _frameIndex = _idleIndex;
            _image.sprite = _frames[_frameIndex];
            _image.color = Color.white;
            // Simple obligatoire : Sliced/Tiled sur une frame de sheet = artefacts de défilement.
            _image.type = Image.Type.Simple;
            _image.useSpriteMesh = false;
            _image.enabled = true;
            gameObject.SetActive(true);

            bool shouldPlay = playAnimation && _frameCount > 1;
            enabled = shouldPlay;
            if (shouldPlay)
            {
                float step = _fps > 0f ? 1f / _fps : 0.1f;
                _nextFrameTime = Time.unscaledTime + step;
            }
        }

        /// <summary>
        /// Démarre ou coupe le flipbook (popup CanvasGroup actif hors écran).
        /// </summary>
        public void SetPlaying(bool playing)
        {
            CacheImage();

            bool shouldPlay = playing && playAnimation && _frameCount > 1;
            enabled = shouldPlay;

            if (!shouldPlay && _image != null && _frames != null && _frameCount > 0)
            {
                _frameIndex = _idleIndex;
                if (_idleIndex >= 0 && _idleIndex < _frameCount)
                    _image.sprite = _frames[_idleIndex];
            }
            else if (shouldPlay)
            {
                float step = _fps > 0f ? 1f / _fps : 0.1f;
                _nextFrameTime = Time.unscaledTime + step;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void CacheImage()
        {
            if (_image == null)
                _image = GetComponent<Image>();
        }
    }
}
