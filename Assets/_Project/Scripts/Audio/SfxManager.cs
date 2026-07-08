using UnityEngine;

namespace ChezArthur.Audio
{
    /// <summary>
    /// Service de lecture des effets sonores one-shot.
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string PREF_SFX_VOLUME = "AudioManager_SfxVolume";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private AudioSource sfxSource;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static SfxManager Instance { get; private set; }

        /// <summary> Volume SFX normalisé (0–1). </summary>
        public float CurrentVolume => _volume;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _volume = 1f;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;

            _volume = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f));
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
        /// Joue un effet sonore au volume courant.
        /// </summary>
        public void PlaySfx(AudioClip clip)
        {
            PlaySfx(clip, 1f);
        }

        /// <summary>
        /// Joue un effet sonore avec un multiplicateur de volume (0–1).
        /// </summary>
        public void PlaySfx(AudioClip clip, float volumeScale)
        {
            if (clip == null || sfxSource == null) return;

            float clampedScale = Mathf.Clamp01(volumeScale);
            sfxSource.PlayOneShot(clip, _volume * clampedScale);
        }

        /// <summary>
        /// Définit le volume SFX et le persiste dans PlayerPrefs.
        /// </summary>
        public void SetVolume(float normalized)
        {
            _volume = Mathf.Clamp01(normalized);
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, _volume);
            PlayerPrefs.Save();
        }
    }
}
