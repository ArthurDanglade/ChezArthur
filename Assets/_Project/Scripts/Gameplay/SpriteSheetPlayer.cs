using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Joue une animation de sprites en boucle sur un SpriteRenderer (flipbook).
    /// Pensé pour les décors animés d'arène : défilement simple, sans allocation par frame.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteSheetPlayer : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("Les frames de l'animation. Glisse les sous-sprites ici (l'ordre est corrigé automatiquement par le numéro).")]
        [SerializeField] private List<Sprite> frames = new List<Sprite>();

        [Tooltip("Images par seconde.")]
        [SerializeField] private float framesPerSecond = 10f;

        [Tooltip("Trie les frames par le numéro à la fin de leur nom (robuste contre l'ordre de glisser-déposer).")]
        [SerializeField] private bool sortByNameNumber = true;

        [Tooltip("Joue dès le réveil.")]
        [SerializeField] private bool playOnAwake = true;

        private SpriteRenderer _renderer;
        private float _timer;
        private int _index;
        private bool _playing;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (sortByNameNumber && frames != null)
                frames.Sort((a, b) => TrailingNumber(a).CompareTo(TrailingNumber(b)));
            if (playOnAwake)
                Play();
        }

        /// <summary> Démarre l'animation depuis la première frame. </summary>
        public void Play()
        {
            if (frames == null || frames.Count == 0) return;
            _playing = true;
            _index = 0;
            _timer = 0f;
            _renderer.sprite = frames[0];
        }

        /// <summary> Met l'animation en pause (garde la frame courante). </summary>
        public void Stop() => _playing = false;

        private void Update()
        {
            if (!_playing || framesPerSecond <= 0f || frames.Count == 0) return;

            _timer += Time.deltaTime;
            float frameDuration = 1f / framesPerSecond;
            if (_timer < frameDuration) return;

            // Avance d'autant de frames que nécessaire (robuste en cas de lag).
            int advance = 0;
            while (_timer >= frameDuration)
            {
                _timer -= frameDuration;
                advance++;
            }
            _index = (_index + advance) % frames.Count;
            _renderer.sprite = frames[_index];
        }

        /// <summary> Récupère le numéro à la fin du nom d'un sprite (ex. "sheet_8x4_12" → 12). </summary>
        private static int TrailingNumber(Sprite s)
        {
            if (s == null) return 0;
            string name = s.name;
            int i = name.Length - 1;
            while (i >= 0 && char.IsDigit(name[i])) i--;
            return int.TryParse(name.Substring(i + 1), out int n) ? n : 0;
        }
    }
}
