using ChezArthur.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Lecteur lofi Accueil (Gate 3.3) — vue chrome sur la bande Shop/News.
    /// Logique audio : lecture seule via AudioManager (aucun changement audio).
    /// </summary>
    [DisallowMultipleComponent]
    public class LofiPlayerBarUI : MonoBehaviour, IPointerClickHandler
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float ProgressRefreshHz = 4f;
        private const float MarqueeSpeed = 28f;
        private const float MarqueePause = 0.85f;
        private const string EmptyTitle = "—";
        private const string SubtitleText = "Lofi du train";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Titre")]
        [SerializeField] private TextMeshProUGUI trackNameText;
        [SerializeField] private RectTransform trackNameRt;
        [SerializeField] private RectTransform trackNameViewport;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Contrôles")]
        [SerializeField] private Button btnPrevious;
        [SerializeField] private Button btnPlayPause;
        [SerializeField] private Button btnNext;
        [SerializeField] private Image btnPlayPauseImage;
        [SerializeField] private Sprite iconPlay;
        [SerializeField] private Sprite iconPause;

        [Header("Progression")]
        [SerializeField] private Image progressTrack;
        [SerializeField] private Image progressFill;

        [Header("Réservé")]
        [Tooltip("Slot vignette 80×80 — désactivé. TODO Dharu (artwork piste).")]
        [SerializeField] private GameObject artworkSlot;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private AudioManager _audio;
        private float _progressAccum;
        private float _marqueeDir = 1f;
        private float _marqueePauseLeft;
        private float _lastOverflow;
        private bool _hasTrack;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _audio = AudioManager.Instance;

            if (btnPrevious != null)
                btnPrevious.onClick.AddListener(OnPreviousClicked);
            if (btnPlayPause != null)
                btnPlayPause.onClick.AddListener(OnPlayPauseClicked);
            if (btnNext != null)
                btnNext.onClick.AddListener(OnNextClicked);

            if (artworkSlot != null)
                artworkSlot.SetActive(false);
        }

        private void OnEnable()
        {
            if (_audio == null)
                _audio = AudioManager.Instance;

            if (_audio != null)
                _audio.OnTrackChanged += OnTrackChanged;

            RefreshAll();
        }

        private void OnDisable()
        {
            if (_audio != null)
                _audio.OnTrackChanged -= OnTrackChanged;
        }

        private void Update()
        {
            // Progression : 4 Hz (pas d'event durée/position côté AudioManager).
            _progressAccum += Time.unscaledDeltaTime;
            float interval = 1f / ProgressRefreshHz;
            if (_progressAccum >= interval)
            {
                _progressAccum = 0f;
                RefreshProgress();
                RefreshPlayPauseIcon();
            }

            TickMarquee();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Recalcule titre / contrôles / progress (builder / OnEnable). </summary>
        public void RefreshAll()
        {
            RefreshTitle();
            RefreshControlsInteractable();
            RefreshPlayPauseIcon();
            RefreshProgress();
            ResetMarquee();
        }

        /// <summary>
        /// Tap hors contrôles — tracklist future.
        /// TODO : ouvrir la tracklist (gate ultérieur).
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            // Ignorer si le clic part d'un contrôle (évite double-fire).
            GameObject go = eventData.pointerCurrentRaycast.gameObject;
            if (go != null && IsUnderControls(go.transform))
                return;

            // TODO tracklist future — handler volontairement vide.
        }

        // ═══════════════════════════════════════════
        // CALLBACKS
        // ═══════════════════════════════════════════

        private void OnPreviousClicked()
        {
            if (_audio == null || !_hasTrack)
                return;
            _audio.PreviousTrack();
            RefreshPlayPauseIcon();
        }

        private void OnNextClicked()
        {
            if (_audio == null || !_hasTrack)
                return;
            _audio.NextTrack();
            RefreshPlayPauseIcon();
        }

        private void OnPlayPauseClicked()
        {
            if (_audio == null || !_hasTrack)
                return;

            if (_audio.IsMusicPlaying)
                _audio.PauseMusic();
            else
                _audio.PlayMusic();

            RefreshPlayPauseIcon();
        }

        private void OnTrackChanged(string _)
        {
            RefreshTitle();
            RefreshControlsInteractable();
            RefreshPlayPauseIcon();
            RefreshProgress();
            ResetMarquee();
        }

        // ═══════════════════════════════════════════
        // AFFICHAGE
        // ═══════════════════════════════════════════

        private void RefreshTitle()
        {
            string name = null;
            if (_audio != null)
                name = _audio.CurrentTrackName;

            _hasTrack = !string.IsNullOrEmpty(name);
            if (trackNameText == null)
                return;

            if (_hasTrack)
            {
                trackNameText.text = name;
                trackNameText.color = UiTheme.TextPrimary;
            }
            else
            {
                trackNameText.text = EmptyTitle;
                trackNameText.color = UiTheme.TextDisabled;
            }

            if (subtitleText != null)
            {
                subtitleText.text = SubtitleText;
                subtitleText.color = _hasTrack ? UiTheme.TextMuted : UiTheme.TextDisabled;
            }
        }

        private void RefreshControlsInteractable()
        {
            SetButtonState(btnPrevious, _hasTrack);
            SetButtonState(btnPlayPause, _hasTrack);
            SetButtonState(btnNext, _hasTrack);
        }

        private static void SetButtonState(Button btn, bool enabled)
        {
            if (btn == null)
                return;
            btn.interactable = enabled;
            Transform iconTx = btn.transform.Find("Icon");
            Image img = iconTx != null
                ? iconTx.GetComponent<Image>()
                : btn.targetGraphic as Image;
            if (img == null)
                img = btn.GetComponent<Image>();
            if (img == null)
                return;

            // Play/Pause garde AccentAmber ; prev/next TextPrimary / Disabled.
            bool isPlayPause = btn.name == "BtnPlayPause";
            if (!enabled)
                img.color = UiTheme.TextDisabled;
            else if (isPlayPause)
                img.color = UiTheme.AccentAmber;
            else
                img.color = UiTheme.TextPrimary;
        }

        private void RefreshPlayPauseIcon()
        {
            if (btnPlayPauseImage == null)
                return;

            bool playing = _audio != null && _audio.IsMusicPlaying && _hasTrack;
            if (iconPlay != null && iconPause != null)
                btnPlayPauseImage.sprite = playing ? iconPause : iconPlay;

            btnPlayPauseImage.color = _hasTrack ? UiTheme.AccentAmber : UiTheme.TextDisabled;
            btnPlayPauseImage.enabled = true;
        }

        private void RefreshProgress()
        {
            if (progressFill == null)
                return;

            float t = 0f;
            if (_audio != null && _hasTrack)
            {
                float len = _audio.MusicLength;
                if (len > 0.001f)
                    t = Mathf.Clamp01(_audio.MusicTime / len);
            }

            RectTransform fillRt = progressFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(t, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
        }

        // ═══════════════════════════════════════════
        // MARQUEE (ping-pong, zéro allocation)
        // ═══════════════════════════════════════════

        private void ResetMarquee()
        {
            _marqueeDir = 1f;
            _marqueePauseLeft = MarqueePause;
            if (trackNameRt != null)
                trackNameRt.anchoredPosition = new Vector2(0f, trackNameRt.anchoredPosition.y);
            _lastOverflow = 0f;
        }

        private void TickMarquee()
        {
            if (trackNameRt == null || trackNameViewport == null || trackNameText == null)
                return;
            if (!_hasTrack)
                return;

            float viewW = trackNameViewport.rect.width;
            float textW = trackNameText.preferredWidth;
            float overflow = textW - viewW;
            if (overflow <= 1f)
            {
                if (_lastOverflow > 1f)
                    ResetMarquee();
                _lastOverflow = overflow;
                return;
            }

            _lastOverflow = overflow;

            if (_marqueePauseLeft > 0f)
            {
                _marqueePauseLeft -= Time.unscaledDeltaTime;
                return;
            }

            float x = trackNameRt.anchoredPosition.x;
            x -= _marqueeDir * MarqueeSpeed * Time.unscaledDeltaTime;

            float minX = -overflow;
            float maxX = 0f;
            if (x <= minX)
            {
                x = minX;
                _marqueeDir = -1f;
                _marqueePauseLeft = MarqueePause;
            }
            else if (x >= maxX)
            {
                x = maxX;
                _marqueeDir = 1f;
                _marqueePauseLeft = MarqueePause;
            }

            trackNameRt.anchoredPosition = new Vector2(x, trackNameRt.anchoredPosition.y);
        }

        private bool IsUnderControls(Transform t)
        {
            while (t != null && t != transform)
            {
                if ((btnPrevious != null && t == btnPrevious.transform)
                    || (btnPlayPause != null && t == btnPlayPause.transform)
                    || (btnNext != null && t == btnNext.transform))
                    return true;
                t = t.parent;
            }

            return false;
        }
    }
}
