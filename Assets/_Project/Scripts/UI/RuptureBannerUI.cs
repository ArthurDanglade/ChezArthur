using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Bannière d'annonce de la Rupture de pression — apparition chorégraphiée
    /// (flash, pop titre, sous-titre en retard, hold, fade), temps non-scalé.
    /// </summary>
    public class RuptureBannerUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float FlashPeakAlpha = 0.42f;
        private const float FlashInDuration = 0.08f;
        private const float FlashOutDuration = 0.28f;

        private const float TitlePopDuration = 0.28f;
        private const float SubtitleFadeDuration = 0.22f;
        private const float TitleToSubtitleGap = 0.12f;
        private const float HoldDuration = 1.05f;
        private const float FadeOutDuration = 0.35f;

        private const float ScaleStart = 0.72f;
        private const float ScalePeak = 1.1f;
        private const float ScaleEnd = 1f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private CanvasGroup bannerGroup;
        [SerializeField] private RectTransform bannerRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image flashImage;

        [Header("Couleurs")]
        [SerializeField] private Color titleColor = new Color(0.94f, 0.35f, 0.32f, 1f);
        [SerializeField] private Color subtitleColor = new Color(0.95f, 0.88f, 0.78f, 1f);
        [SerializeField] private Color flashColor = new Color(0.85f, 0.12f, 0.08f, 1f);

        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static RuptureBannerUI Instance { get; private set; }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Coroutine _playCoroutine;

        /// <summary>
        /// Instant où les VFX ennemis devraient commencer à apparaître
        /// (depuis le début de PlayAnnounce), pour sync Inspector / effets.
        /// Aligné sur visualRevealDelay de RuptureEffectsSystem (défaut 0.28s).
        /// </summary>
        public static float VisualRevealCueTime =>
            FlashInDuration + TitlePopDuration * 0.55f + 0.02f;

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
            ResetVisualState(hidden: true);
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
        /// Lance l'annonce complète. Si déjà en cours, relance proprement.
        /// </summary>
        public void PlayAnnounce(string title, string subtitle)
        {
            if (_playCoroutine != null)
                StopCoroutine(_playCoroutine);

            _playCoroutine = StartCoroutine(PlayAnnounceRoutine(title, subtitle));
        }

        /// <summary> Version coroutine pour attendre la fin depuis un orchestrateur. </summary>
        public IEnumerator PlayAnnounceRoutine(string title, string subtitle)
        {
            ApplyContent(title, subtitle);
            ResetVisualState(hidden: true);

            // ── Flash plein écran ──
            if (flashImage != null)
            {
                flashImage.gameObject.SetActive(true);
                Color c = flashColor;
                c.a = 0f;
                flashImage.color = c;

                float elapsed = 0f;
                while (elapsed < FlashInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / FlashInDuration);
                    c.a = FlashPeakAlpha * Smooth01(t);
                    flashImage.color = c;
                    yield return null;
                }

                elapsed = 0f;
                while (elapsed < FlashOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / FlashOutDuration);
                    c.a = FlashPeakAlpha * (1f - Smooth01(t));
                    flashImage.color = c;
                    yield return null;
                }

                c.a = 0f;
                flashImage.color = c;
                flashImage.gameObject.SetActive(false);
            }

            // ── Pop titre ──
            if (bannerGroup != null)
            {
                bannerGroup.blocksRaycasts = false;
                bannerGroup.interactable = false;
            }

            if (subtitleText != null)
            {
                Color sc = subtitleText.color;
                sc.a = 0f;
                subtitleText.color = sc;
            }

            float popElapsed = 0f;
            while (popElapsed < TitlePopDuration)
            {
                popElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(popElapsed / TitlePopDuration);
                float scale = EvaluatePopScale(t);

                if (bannerRect != null)
                    bannerRect.localScale = new Vector3(scale, scale, 1f);

                if (bannerGroup != null)
                    bannerGroup.alpha = Smooth01(t);

                yield return null;
            }

            if (bannerRect != null)
                bannerRect.localScale = Vector3.one;
            if (bannerGroup != null)
                bannerGroup.alpha = 1f;

            yield return WaitUnscaled(TitleToSubtitleGap);

            // ── Sous-titre en fondu ──
            if (subtitleText != null && !string.IsNullOrEmpty(subtitleText.text))
            {
                Color baseSub = subtitleColor;
                float subElapsed = 0f;
                while (subElapsed < SubtitleFadeDuration)
                {
                    subElapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(subElapsed / SubtitleFadeDuration);
                    baseSub.a = Smooth01(t);
                    subtitleText.color = baseSub;
                    yield return null;
                }

                baseSub.a = 1f;
                subtitleText.color = baseSub;
            }

            // ── Hold + pulse léger ──
            float holdElapsed = 0f;
            while (holdElapsed < HoldDuration)
            {
                holdElapsed += Time.unscaledDeltaTime;
                if (bannerRect != null)
                {
                    float pulse = 1f + Mathf.Sin(holdElapsed * 5.5f) * 0.018f;
                    bannerRect.localScale = new Vector3(pulse, pulse, 1f);
                }

                yield return null;
            }

            if (bannerRect != null)
                bannerRect.localScale = Vector3.one;

            // ── Fade out ──
            float fadeElapsed = 0f;
            float startAlpha = bannerGroup != null ? bannerGroup.alpha : 1f;
            while (fadeElapsed < FadeOutDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeElapsed / FadeOutDuration);
                if (bannerGroup != null)
                    bannerGroup.alpha = startAlpha * (1f - Smooth01(t));
                yield return null;
            }

            ResetVisualState(hidden: true);
            _playCoroutine = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void ApplyContent(string title, string subtitle)
        {
            if (titleText != null)
            {
                titleText.text = title;
                titleText.color = titleColor;
            }

            if (subtitleText != null)
            {
                subtitleText.text = subtitle ?? string.Empty;
                Color c = subtitleColor;
                c.a = 0f;
                subtitleText.color = c;
            }

            if (backgroundImage != null)
            {
                Color bg = UiTheme.SurfaceBar;
                bg.a = 0.94f;
                backgroundImage.color = bg;
            }
        }

        private void ResetVisualState(bool hidden)
        {
            if (bannerGroup != null)
            {
                bannerGroup.alpha = hidden ? 0f : 1f;
                bannerGroup.blocksRaycasts = false;
                bannerGroup.interactable = false;
            }

            if (bannerRect != null)
                bannerRect.localScale = Vector3.one;

            if (flashImage != null)
            {
                Color c = flashColor;
                c.a = 0f;
                flashImage.color = c;
                flashImage.gameObject.SetActive(false);
            }

            if (subtitleText != null)
            {
                Color sc = subtitleColor;
                sc.a = hidden ? 0f : 1f;
                subtitleText.color = sc;
            }
        }

        private static float EvaluatePopScale(float t)
        {
            if (t < 0.62f)
            {
                float local = t / 0.62f;
                return Mathf.Lerp(ScaleStart, ScalePeak, Smooth01(local));
            }

            float tail = (t - 0.62f) / 0.38f;
            return Mathf.Lerp(ScalePeak, ScaleEnd, Smooth01(tail));
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
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
