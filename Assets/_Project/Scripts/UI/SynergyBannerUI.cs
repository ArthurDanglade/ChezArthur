using System.Collections;
using System.Collections.Generic;
using ChezArthur.Audio;
using ChezArthur.Roguelike;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Bandeau d'annonce des synergies de valises (activation / rupture).
    /// File d'attente non bloquante, animation en temps non-scalé.
    /// </summary>
    public class SynergyBannerUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float PopInDuration = 0.25f;
        private const float HoldDuration = 0.8f;
        private const float FadeOutDuration = 0.3f;
        private const float QueueGap = 0.15f;
        private const float ScaleStart = 0.85f;
        private const float ScalePeak = 1.05f;
        private const float ScaleEnd = 1f;

        private const string LabelActivation = "SYNERGIE ACTIVÉE";
        private const string LabelDeactivation = "SYNERGIE ROMPUE";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image backgroundImage;

        [Header("SFX par défaut")]
        [SerializeField] private AudioClip defaultActivationSfx;
        [SerializeField] private AudioClip defaultDeactivationSfx;

        // ═══════════════════════════════════════════
        // TYPES
        // ═══════════════════════════════════════════
        private struct BannerRequest
        {
            public bool isActivation;
            public SynergyData data;
        }

        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static SynergyBannerUI Instance { get; private set; }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly Queue<BannerRequest> _queue = new Queue<BannerRequest>();
        private Coroutine _drainCoroutine;
        private RectTransform _bannerRect;

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

            if (canvasGroup != null)
            {
                _bannerRect = canvasGroup.transform as RectTransform;
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void Start()
        {
            if (SynergyManager.Instance != null)
            {
                SynergyManager.Instance.OnSynergyActivated += OnSynergyActivated;
                SynergyManager.Instance.OnSynergyDeactivated += OnSynergyDeactivated;
            }
        }

        private void OnDestroy()
        {
            if (SynergyManager.Instance != null)
            {
                SynergyManager.Instance.OnSynergyActivated -= OnSynergyActivated;
                SynergyManager.Instance.OnSynergyDeactivated -= OnSynergyDeactivated;
            }

            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // HANDLERS
        // ═══════════════════════════════════════════
        private void OnSynergyActivated(SynergyData data)
        {
            EnqueueRequest(true, data);
        }

        private void OnSynergyDeactivated(SynergyData data)
        {
            EnqueueRequest(false, data);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void EnqueueRequest(bool isActivation, SynergyData data)
        {
            if (data == null)
                return;

            _queue.Enqueue(new BannerRequest { isActivation = isActivation, data = data });

            if (_drainCoroutine == null)
                _drainCoroutine = StartCoroutine(DrainQueue());
        }

        private IEnumerator DrainQueue()
        {
            while (_queue.Count > 0)
            {
                BannerRequest request = _queue.Dequeue();
                yield return PlayBanner(request);

                if (_queue.Count > 0)
                    yield return WaitUnscaled(QueueGap);
            }

            _drainCoroutine = null;
        }

        private IEnumerator PlayBanner(BannerRequest request)
        {
            if (canvasGroup == null || _bannerRect == null)
                yield break;

            ApplyContent(request);
            PlayBannerSfx(request);

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Pop-in scale 0.85 → 1.05 → 1 + fade-in
            float elapsed = 0f;
            while (elapsed < PopInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / PopInDuration);
                float scale = EvaluatePopScale(t);
                _bannerRect.localScale = new Vector3(scale, scale, 1f);
                canvasGroup.alpha = t;
                yield return null;
            }

            _bannerRect.localScale = Vector3.one;
            canvasGroup.alpha = 1f;

            yield return WaitUnscaled(HoldDuration);

            // Fade-out
            elapsed = 0f;
            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FadeOutDuration);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            canvasGroup.alpha = 0f;
            _bannerRect.localScale = Vector3.one;
        }

        private void ApplyContent(BannerRequest request)
        {
            SynergyData data = request.data;
            if (data == null)
                return;

            Color accent = request.isActivation ? UiTheme.Gold : UiTheme.SynergyBroken;
            Color labelColor = request.isActivation ? UiTheme.TextPrimary : UiTheme.SynergyBroken;

            if (labelText != null)
            {
                labelText.text = request.isActivation ? LabelActivation : LabelDeactivation;
                labelText.color = labelColor;
            }

            if (nameText != null)
            {
                nameText.text = data.DisplayName;
                nameText.color = accent;
            }

            if (backgroundImage != null)
            {
                Color bg = UiTheme.SurfaceBar;
                bg.a = 0.92f;
                backgroundImage.color = bg;
            }
        }

        private void PlayBannerSfx(BannerRequest request)
        {
            if (SfxManager.Instance == null)
                return;

            SynergyData data = request.data;
            AudioClip clip = null;

            if (data != null)
            {
                clip = request.isActivation ? data.ActivationSfx : data.DeactivationSfx;
            }

            if (clip == null)
            {
                clip = request.isActivation ? defaultActivationSfx : defaultDeactivationSfx;
            }

            if (clip != null)
                SfxManager.Instance.PlaySfx(clip);
        }

        private static float EvaluatePopScale(float t)
        {
            if (t < 0.6f)
            {
                float local = t / 0.6f;
                return Mathf.Lerp(ScaleStart, ScalePeak, local);
            }

            float tail = (t - 0.6f) / 0.4f;
            return Mathf.Lerp(ScalePeak, ScaleEnd, tail);
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
