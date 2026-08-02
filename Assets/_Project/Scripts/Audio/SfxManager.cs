using UnityEngine;

namespace ChezArthur.Audio
{
    /// <summary>
    /// Service de lecture des effets sonores one-shot.
    /// Volume master via AudioBuses (bus SFX) — corrige le bug slider combat.
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
        [Tooltip("Source dédiée aux SFX interruptibles (ex. reveal pixel).")]
        [SerializeField] private AudioSource managedSfxSource;

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
            if (AudioBuses.SfxGroup != null)
                sfxSource.outputAudioMixerGroup = AudioBuses.SfxGroup;

            EnsureManagedSource();

            _volume = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f));
            AudioBuses.SetSfxVolume01(_volume);
            ApplyVolumeToSources();
        }

        private void EnsureManagedSource()
        {
            if (managedSfxSource != null)
            {
                if (AudioBuses.SfxGroup != null)
                    managedSfxSource.outputAudioMixerGroup = AudioBuses.SfxGroup;
                return;
            }

            managedSfxSource = gameObject.AddComponent<AudioSource>();
            managedSfxSource.playOnAwake = false;
            managedSfxSource.loop = false;
            managedSfxSource.spatialBlend = 0f;
            if (AudioBuses.SfxGroup != null)
                managedSfxSource.outputAudioMixerGroup = AudioBuses.SfxGroup;
        }

        private void ApplyVolumeToSources()
        {
            // Le master est le bus SFX — sources à 1.
            if (sfxSource != null)
                sfxSource.volume = 1f;

            if (managedSfxSource != null && !managedSfxSource.isPlaying)
                managedSfxSource.volume = 1f;
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
            sfxSource.PlayOneShot(clip, clampedScale);
        }

        /// <summary>
        /// Lecture interruptible (StopManagedSfx coupe immédiatement).
        /// </summary>
        public void PlayManagedSfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
                return;

            EnsureManagedSource();
            if (managedSfxSource == null)
                return;

            managedSfxSource.Stop();
            managedSfxSource.clip = clip;
            managedSfxSource.volume = Mathf.Clamp01(volumeScale);
            managedSfxSource.Play();
        }

        /// <summary>
        /// Coupe le SFX managed en cours (ex. reveal pixel au skip).
        /// </summary>
        public void StopManagedSfx()
        {
            if (managedSfxSource != null && managedSfxSource.isPlaying)
                managedSfxSource.Stop();
        }

        /// <summary>
        /// Définit le volume SFX, le persiste, et pilote le bus mixer.
        /// </summary>
        public void SetVolume(float normalized)
        {
            _volume = Mathf.Clamp01(normalized);
            ApplyVolumeToSources();
            AudioBuses.SetSfxVolume01(_volume);
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, _volume);
            PlayerPrefs.Save();
        }
    }
}
