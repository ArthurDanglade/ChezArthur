using System;
using System.Collections;
using UnityEngine;

namespace ChezArthur.Audio
{
    /// <summary>
    /// Gère l'ambiance (train, vinyl) et la musique (playlist) du Hub.
    /// Persiste entre les scènes (DontDestroyOnLoad). Volumes sauvegardés dans PlayerPrefs.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string PREF_TRAIN_VOLUME = "AudioManager_TrainVolume";
        private const string PREF_VINYL_VOLUME = "AudioManager_VinylVolume";
        private const string PREF_MUSIC_VOLUME = "AudioManager_MusicVolume";

        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static AudioManager Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Ambiance (boucles)")]
        [SerializeField] private AudioClip trainSound;
        [SerializeField] private float trainVolume = 0.2f;
        [SerializeField] private AudioClip vinylSound;
        [SerializeField] private float vinylVolume = 0.15f;

        [Header("Musique (playlist)")]
        [SerializeField] private AudioClip[] playlist;
        [SerializeField] private float musicVolume = 0.5f;

        [Header("Contrôles")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float fadeDuration = 2f;
        [SerializeField] private float fadeInDurationOnStart = 5f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private AudioSource _trainSource;
        private AudioSource _vinylSource;
        private AudioSource _musicSource;
        private int _currentTrackIndex;
        private bool _musicShouldBePlaying;
        private float _musicFadeTarget;
        private float _musicFadeDuration;
        private float _musicFadeTimer = -1f;
        private bool _isFirstPlay = true;
        private Coroutine _aimDuckRoutine;
        private float _musicVolumeBeforeAimDuck = -1f;
        /// <summary> Multiplicateur de sortie musique (cérémonie, etc.) — non persisté. </summary>
        private float _musicDuckFactor = 1f;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Nom de la piste en cours (pour l'UI). </summary>
        public string CurrentTrackName =>
            playlist != null && playlist.Length > 0 && _currentTrackIndex >= 0 && _currentTrackIndex < playlist.Length && playlist[_currentTrackIndex] != null
                ? playlist[_currentTrackIndex].name
                : string.Empty;

        /// <summary> True si la musique est en cours de lecture. </summary>
        public bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;

        /// <summary> Volume actuel de la musique (0–1), pour initialiser l'UI. </summary>
        public float MusicVolume => musicVolume;

        /// <summary> Temps écoulé de la piste en cours (pour la barre de progression). </summary>
        public float MusicTime => _musicSource != null ? _musicSource.time : 0f;

        /// <summary> Durée totale de la piste en cours (pour la barre de progression). </summary>
        public float MusicLength => _musicSource != null && _musicSource.clip != null ? _musicSource.clip.length : 1f;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand on change de piste (nom de la piste). </summary>
        public event Action<string> OnTrackChanged;

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
            DontDestroyOnLoad(gameObject);
            _musicDuckFactor = 1f;

            // Création des AudioSources dynamiquement
            _trainSource = gameObject.AddComponent<AudioSource>();
            _trainSource.playOnAwake = false;
            _trainSource.loop = true;
            _trainSource.clip = trainSound;

            _vinylSource = gameObject.AddComponent<AudioSource>();
            _vinylSource.playOnAwake = false;
            _vinylSource.loop = true;
            _vinylSource.clip = vinylSound;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = false;

            // Chargement des volumes depuis PlayerPrefs
            float savedTrain = PlayerPrefs.GetFloat(PREF_TRAIN_VOLUME, trainVolume);
            float savedVinyl = PlayerPrefs.GetFloat(PREF_VINYL_VOLUME, vinylVolume);
            float savedMusic = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, musicVolume);

            _trainSource.volume = savedTrain;
            _vinylSource.volume = savedVinyl;
            _musicSource.volume = savedMusic;
        }

        private void Start()
        {
            if (playOnStart)
            {
                PlayAll();
            }
        }

        private void Update()
        {
            // Fin de piste détectée → piste suivante
            if (_musicShouldBePlaying && _musicSource != null && !_musicSource.isPlaying && _musicSource.clip != null)
            {
                NextTrack();
            }

            // Mise à jour du fade musique (Time.unscaledDeltaTime pour fonctionner en pause)
            if (_musicFadeTimer >= 0f && _musicSource != null)
            {
                _musicFadeTimer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_musicFadeTimer / _musicFadeDuration);
                float outVol = GetDuckedMusicVolume();
                _musicSource.volume = Mathf.Lerp(
                    _musicFadeTarget == 0f ? outVol : 0f,
                    _musicFadeTarget == 0f ? 0f : outVol,
                    t);
                if (t >= 1f)
                {
                    _musicFadeTimer = -1f;
                    if (_musicFadeTarget == 0f)
                    {
                        _musicSource.Pause();
                        _musicShouldBePlaying = false;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Lance l'ambiance et la musique.
        /// </summary>
        public void PlayAll()
        {
            PlayAmbiance();
            PlayMusic();
        }

        /// <summary>
        /// Arrête l'ambiance et la musique.
        /// </summary>
        public void StopAll()
        {
            StopAmbiance();
            StopMusic();
        }

        /// <summary>
        /// Lance les sons d'ambiance (train + vinyl).
        /// </summary>
        public void PlayAmbiance()
        {
            if (_trainSource != null && trainSound != null)
            {
                _trainSource.clip = trainSound;
                _trainSource.Play();
            }
            if (_vinylSource != null && vinylSound != null)
            {
                _vinylSource.clip = vinylSound;
                _vinylSource.Play();
            }
        }

        /// <summary>
        /// Arrête les sons d'ambiance.
        /// </summary>
        public void StopAmbiance()
        {
            if (_trainSource != null) _trainSource.Stop();
            if (_vinylSource != null) _vinylSource.Stop();
        }

        /// <summary>
        /// Lance la musique (piste courante de la playlist).
        /// Au premier démarrage, applique un fade in ; les changements de piste restent sans fade.
        /// </summary>
        public void PlayMusic()
        {
            if (playlist == null || playlist.Length == 0) return;

            _musicShouldBePlaying = true;
            _musicFadeTimer = -1f;

            if (_isFirstPlay)
            {
                // Fade in au premier démarrage (Hub)
                if (_musicSource != null) _musicSource.volume = 0f;
                ApplyCurrentTrack();
                _musicFadeTarget = 1f;
                _musicFadeDuration = fadeInDurationOnStart;
                _musicFadeTimer = 0f;
                _isFirstPlay = false;
            }
            else
            {
                ApplyCurrentTrack();
            }
        }

        /// <summary>
        /// Met la musique en pause.
        /// </summary>
        public void PauseMusic()
        {
            _musicShouldBePlaying = false;
            if (_musicSource != null) _musicSource.Pause();
        }

        /// <summary>
        /// Reprend la musique après PauseMusic (garde la position de lecture).
        /// </summary>
        public void ResumeMusic()
        {
            if (_musicSource == null || _musicSource.clip == null)
                return;

            _musicShouldBePlaying = true;
            _musicFadeTimer = -1f;
            _musicSource.UnPause();
            if (!_musicSource.isPlaying)
                _musicSource.Play();
            ApplyMusicOutputVolume();
        }

        /// <summary>
        /// Arrête la musique et remet l'index à 0.
        /// </summary>
        public void StopMusic()
        {
            _musicShouldBePlaying = false;
            _musicFadeTimer = -1f;
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
            _currentTrackIndex = 0;
        }

        /// <summary>
        /// Passe à la piste suivante (boucle en fin de playlist).
        /// </summary>
        public void NextTrack()
        {
            if (playlist == null || playlist.Length == 0) return;

            _currentTrackIndex = (_currentTrackIndex + 1) % playlist.Length;
            ApplyCurrentTrack();
        }

        /// <summary>
        /// Passe à la piste précédente (boucle au début).
        /// </summary>
        public void PreviousTrack()
        {
            if (playlist == null || playlist.Length == 0) return;

            _currentTrackIndex = (_currentTrackIndex - 1 + playlist.Length) % playlist.Length;
            ApplyCurrentTrack();
        }

        /// <summary>
        /// Définit le volume du train et le sauvegarde.
        /// </summary>
        public void SetTrainVolume(float volume)
        {
            float v = Mathf.Clamp01(volume);
            if (_trainSource != null) _trainSource.volume = v;
            trainVolume = v;
            PlayerPrefs.SetFloat(PREF_TRAIN_VOLUME, v);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Définit le volume du vinyl et le sauvegarde.
        /// </summary>
        public void SetVinylVolume(float volume)
        {
            float v = Mathf.Clamp01(volume);
            if (_vinylSource != null) _vinylSource.volume = v;
            vinylVolume = v;
            PlayerPrefs.SetFloat(PREF_VINYL_VOLUME, v);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Définit le volume de la musique et le sauvegarde.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            float v = Mathf.Clamp01(volume);
            musicVolume = v;
            if (_musicSource != null && _musicFadeTimer < 0f)
                ApplyMusicOutputVolume();
            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, v);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Multiplicateur de sortie musique (0–1), non persisté. N'altère pas le réglage joueur.
        /// </summary>
        public void SetMusicDuck(float factor01)
        {
            _musicDuckFactor = Mathf.Clamp01(factor01);
            if (_musicSource != null && _musicFadeTimer < 0f && _aimDuckRoutine == null)
                ApplyMusicOutputVolume();
        }

        /// <summary> Baisse temporairement la musique pendant la visée Super Lancer. </summary>
        public void DuckMusicForAim(float volumeMultiplier, float fadeSeconds)
        {
            if (_musicSource == null || !_musicSource.isPlaying) return;

            if (_aimDuckRoutine != null)
                StopCoroutine(_aimDuckRoutine);

            _musicFadeTimer = -1f; // priorité au duck de visée sur un fade en cours
            _musicVolumeBeforeAimDuck = _musicSource.volume;
            float target = _musicVolumeBeforeAimDuck * Mathf.Clamp01(volumeMultiplier);
            _aimDuckRoutine = StartCoroutine(DuckMusicRoutine(_musicSource.volume, target, fadeSeconds));
        }

        /// <summary> Restaure le volume musique après visée ou annulation. </summary>
        public void RestoreMusicAfterAim(float fadeSeconds)
        {
            if (_musicSource == null || _musicVolumeBeforeAimDuck < 0f) return;

            if (_aimDuckRoutine != null)
                StopCoroutine(_aimDuckRoutine);

            float current = _musicSource.volume;
            _aimDuckRoutine = StartCoroutine(DuckMusicRoutine(current, _musicVolumeBeforeAimDuck, fadeSeconds, true));
        }

        /// <summary>
        /// Fait fondre le volume de la musique vers 0 (durée = fadeDuration). Fonctionne en pause (unscaledTime).
        /// </summary>
        public void FadeOutMusic()
        {
            if (_musicSource == null) return;
            _musicFadeTarget = 0f;
            _musicFadeDuration = fadeDuration;
            _musicFadeTimer = 0f;
        }

        /// <summary>
        /// Fait fondre le volume de la musique de 0 vers musicVolume. Lance la lecture si besoin.
        /// </summary>
        public void FadeInMusic()
        {
            if (_musicSource == null) return;
            _musicShouldBePlaying = true;
            if (playlist != null && playlist.Length > 0 && (_musicSource.clip == null || !_musicSource.isPlaying))
            {
                ApplyCurrentTrack();
            }
            _musicSource.volume = 0f;
            _musicFadeTarget = 1f;
            _musicFadeDuration = fadeDuration;
            _musicFadeTimer = 0f;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Applique la piste courante à la source musique et déclenche OnTrackChanged.
        /// </summary>
        private void ApplyCurrentTrack()
        {
            if (playlist == null || playlist.Length == 0 || _musicSource == null) return;

            AudioClip clip = playlist[_currentTrackIndex];
            if (clip == null) return;

            _musicSource.clip = clip;
            _musicSource.Play();
            if (_musicFadeTimer < 0f)
                ApplyMusicOutputVolume();

            string name = clip.name;
            OnTrackChanged?.Invoke(name);
        }

        private float GetDuckedMusicVolume()
        {
            return musicVolume * _musicDuckFactor;
        }

        private void ApplyMusicOutputVolume()
        {
            if (_musicSource == null)
                return;
            _musicSource.volume = GetDuckedMusicVolume();
        }

        private IEnumerator DuckMusicRoutine(float from, float to, float duration, bool clearSaved = false)
        {
            if (_musicSource == null)
            {
                if (clearSaved) _musicVolumeBeforeAimDuck = -1f;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                _musicSource.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _musicSource.volume = to;
            _aimDuckRoutine = null;

            if (clearSaved)
                _musicVolumeBeforeAimDuck = -1f;
        }
    }
}
