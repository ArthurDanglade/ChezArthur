using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Anime un composant Image UI en cyclant une liste de sprites (frames) à une cadence donnée.
    /// Utilise Time.unscaledDeltaTime pour fonctionner même si le timeScale est à 0 (Hub en pause).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UISpriteSheetAnimator : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Animation")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameRate = 10f;
        [SerializeField] private bool loop = true;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Image _image;
        private int _currentFrame;
        private float _timer;
        private float _frameDuration;
        private bool _isStopped;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _image = GetComponent<Image>();
            RecomputeFrameDuration();
        }

        private void Update()
        {
            if (_isStopped) return;
            if (_image == null) return;
            if (frames == null || frames.Length == 0) return;
            if (_frameDuration <= 0f) return;

            _timer += Time.unscaledDeltaTime;
            if (_timer < _frameDuration) return;

            // Avance de plusieurs frames si nécessaire (ex : frameRate élevé + gros delta).
            int steps = (int)(_timer / _frameDuration);
            _timer -= steps * _frameDuration;

            int nextFrame = _currentFrame + steps;

            if (loop)
            {
                int len = frames.Length;
                if (len <= 0) return;
                nextFrame %= len;
            }
            else
            {
                if (nextFrame >= frames.Length - 1)
                {
                    nextFrame = frames.Length - 1;
                    _isStopped = true;
                }
            }

            if (nextFrame == _currentFrame) return;

            _currentFrame = nextFrame;
            Sprite sprite = frames[_currentFrame];
            if (sprite != null)
                _image.sprite = sprite;
        }

        private void OnValidate()
        {
            RecomputeFrameDuration();
            if (_currentFrame < 0) _currentFrame = 0;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void RecomputeFrameDuration()
        {
            // Évite une division par zéro et des comportements non déterministes.
            _frameDuration = frameRate > 0f ? 1f / frameRate : 0f;
        }
    }
}

