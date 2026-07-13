using UnityEngine;

namespace ChezArthur.Audio
{
    /// <summary>
    /// Lecteur SFX combat 2D avec pool round-robin d'AudioSources.
    /// Singleton de scène — point d'accroche volume SFX (SettingsPanelUI).
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static SfxPlayer Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private int _poolSize = 10;
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private AudioSource[] _sources;
        private int _next;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Instance = this;
            _sources = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _sources[i] = src;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Joue un clip avec pitch modifié (volume 1, volume maître appliqué).
        /// </summary>
        public void PlayPitched(AudioClip clip, float pitch)
        {
            Play(clip, 1f, pitch);
        }

        /// <summary>
        /// Joue un clip via la prochaine source libre du pool.
        /// </summary>
        public void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || _sources == null || _sources.Length == 0) return;

            AudioSource src = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume) * _masterVolume;
            src.pitch = pitch;
            src.Play();
        }
    }
}
