using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Audio;

namespace ChezArthur.UI
{
    /// <summary>
    /// UI du lecteur de musique sur la page Musique du Hub.
    /// Affiche le nom de la piste, la barre de progression et les boutons Précédent / Play-Pause / Suivant.
    /// </summary>
    public class MusicPlayerUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Affichage")]
        [SerializeField] private TextMeshProUGUI trackNameText;
        [SerializeField] private Slider progressBar;

        [Header("Boutons")]
        [SerializeField] private Button btnPrevious;
        [SerializeField] private Button btnPlayPause;
        [SerializeField] private Button btnNext;
        [SerializeField] private Sprite iconPlay;
        [SerializeField] private Sprite iconPause;
        [SerializeField] private Image btnPlayPauseImage;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private AudioManager _audioManager;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _audioManager = AudioManager.Instance;

            if (progressBar != null)
                progressBar.interactable = false;

            if (btnPrevious != null)
                btnPrevious.onClick.AddListener(OnPreviousClicked);
            if (btnPlayPause != null)
                btnPlayPause.onClick.AddListener(OnPlayPauseClicked);
            if (btnNext != null)
                btnNext.onClick.AddListener(OnNextClicked);
        }

        private void OnEnable()
        {
            if (_audioManager == null)
                _audioManager = AudioManager.Instance;

            if (_audioManager != null)
                _audioManager.OnTrackChanged += OnTrackChangedHandler;

            UpdateTrackName();
            RefreshPlayPauseIcon();
        }

        private void OnDisable()
        {
            if (_audioManager != null)
                _audioManager.OnTrackChanged -= OnTrackChangedHandler;
        }

        private void Update()
        {
            if (_audioManager == null || progressBar == null) return;

            float length = _audioManager.MusicLength;
            if (length <= 0f) return;

            float time = _audioManager.MusicTime;
            progressBar.value = Mathf.Clamp01(time / length);
        }

        // ═══════════════════════════════════════════
        // CALLBACKS BOUTONS
        // ═══════════════════════════════════════════
        private void OnPreviousClicked()
        {
            if (_audioManager != null)
                _audioManager.PreviousTrack();
        }

        private void OnNextClicked()
        {
            if (_audioManager != null)
                _audioManager.NextTrack();
        }

        private void OnPlayPauseClicked()
        {
            if (_audioManager == null) return;

            if (_audioManager.IsMusicPlaying)
                _audioManager.PauseMusic();
            else
                _audioManager.PlayMusic();

            RefreshPlayPauseIcon();
        }

        // ═══════════════════════════════════════════
        // HANDLERS ÉVÉNEMENTS
        // ═══════════════════════════════════════════
        private void OnTrackChangedHandler(string trackName)
        {
            UpdateTrackName();
            RefreshPlayPauseIcon();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void UpdateTrackName()
        {
            if (trackNameText == null) return;

            if (_audioManager != null)
                trackNameText.text = _audioManager.CurrentTrackName;
            else
                trackNameText.text = string.Empty;
        }

        private void RefreshPlayPauseIcon()
        {
            if (btnPlayPauseImage == null || iconPlay == null || iconPause == null) return;
            if (_audioManager == null) return;

            btnPlayPauseImage.sprite = _audioManager.IsMusicPlaying ? iconPause : iconPlay;
        }
    }
}
