using System.Collections;
using System.Collections.Generic;
using ChezArthur.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Bandeaux de fin de run (missions / nouveaux boss) — style félicitation studio.
    /// File séquentielle, jamais superposée, gap 0,5 s, temps non scalé.
    /// </summary>
    public class EndRunAnnouncementBanner : MonoBehaviour
    {
        public static EndRunAnnouncementBanner Instance { get; private set; }

        private const float PopInDuration = 0.32f;
        private const float HoldDuration = 1.45f;
        private const float FadeOutDuration = 0.35f;
        private const float BetweenGap = 0.5f;
        private const float SlidePixels = 36f;

        public enum BannerKind
        {
            Missions = 0,
            NewBoss = 1
        }

        [Header("Références")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image accentBar;
        [SerializeField] private Image glowImage;
        [SerializeField] private TextMeshProUGUI eyebrowText;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Jingles (Epidemic — placeholders OK)")]
        [SerializeField] private AudioClip missionsCompletedJingle;
        [SerializeField] private AudioClip newBossUnlockedJingle;

        private struct Request
        {
            public string eyebrow;
            public string title;
            public BannerKind kind;
            public AudioClip jingle;
        }

        private readonly Queue<Request> _queue = new Queue<Request>();
        private Vector2 _restAnchoredPos;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (panelRect != null)
                _restAnchoredPos = panelRect.anchoredPosition;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            HideVisual();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Enfile les bandeaux selon le récap de run, puis les joue (coroutine awaitable).
        /// </summary>
        public IEnumerator PlayForRunEnd(int missionsCompletedThisRun, int newBossesThisRun)
        {
            _queue.Clear();

            if (missionsCompletedThisRun > 0)
            {
                string plural = missionsCompletedThisRun > 1 ? "s" : "";
                Enqueue(
                    "MISSION ACCOMPLIE",
                    $"{missionsCompletedThisRun} mission{plural} terminée{plural} !",
                    BannerKind.Missions,
                    missionsCompletedJingle);
            }

            if (newBossesThisRun > 0)
            {
                Enqueue(
                    "BOSS RUSH",
                    "Nouveau boss débloqué !",
                    BannerKind.NewBoss,
                    newBossUnlockedJingle);
            }

            if (_queue.Count == 0)
                yield break;

            yield return DrainQueue();
        }

        public void Enqueue(string eyebrow, string title, BannerKind kind, AudioClip jingle)
        {
            if (string.IsNullOrEmpty(title))
                return;

            _queue.Enqueue(new Request
            {
                eyebrow = eyebrow ?? string.Empty,
                title = title,
                kind = kind,
                jingle = jingle
            });
        }

#if UNITY_EDITOR
        public void EditorWire(
            CanvasGroup group,
            RectTransform panel,
            Image bg,
            Image accent,
            Image glow,
            TextMeshProUGUI eyebrow,
            TextMeshProUGUI title)
        {
            canvasGroup = group;
            panelRect = panel;
            backgroundImage = bg;
            accentBar = accent;
            glowImage = glow;
            eyebrowText = eyebrow;
            titleText = title;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void EditorSetJingles(AudioClip missions, AudioClip bosses)
        {
            missionsCompletedJingle = missions;
            newBossUnlockedJingle = bosses;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        private IEnumerator DrainQueue()
        {
            bool first = true;
            while (_queue.Count > 0)
            {
                if (!first)
                    yield return WaitUnscaled(BetweenGap);
                first = false;
                yield return PlayOne(_queue.Dequeue());
            }

            HideVisual();
        }

        private IEnumerator PlayOne(Request req)
        {
            ApplyVisualTheme(req.kind);

            if (eyebrowText != null)
                eyebrowText.text = req.eyebrow;
            if (titleText != null)
                titleText.text = req.title;

            if (req.jingle != null && SfxManager.Instance != null)
                SfxManager.Instance.PlaySfx(req.jingle);

            if (canvasGroup == null)
            {
                yield return WaitUnscaled(HoldDuration);
                yield break;
            }

            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = false;

            Vector2 startPos = _restAnchoredPos + new Vector2(0f, -SlidePixels);
            if (panelRect != null)
            {
                panelRect.anchoredPosition = startPos;
                panelRect.localScale = new Vector3(0.92f, 0.92f, 1f);
            }

            float elapsed = 0f;
            while (elapsed < PopInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / PopInDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                canvasGroup.alpha = eased;
                if (panelRect != null)
                {
                    panelRect.anchoredPosition = Vector2.Lerp(startPos, _restAnchoredPos, eased);
                    float s = Mathf.Lerp(0.92f, 1.03f, eased);
                    panelRect.localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }

            canvasGroup.alpha = 1f;
            if (panelRect != null)
            {
                panelRect.anchoredPosition = _restAnchoredPos;
                panelRect.localScale = Vector3.one;
            }

            yield return WaitUnscaled(HoldDuration);

            elapsed = 0f;
            Vector2 endPos = _restAnchoredPos + new Vector2(0f, SlidePixels * 0.35f);
            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FadeOutDuration);
                canvasGroup.alpha = 1f - t;
                if (panelRect != null)
                    panelRect.anchoredPosition = Vector2.Lerp(_restAnchoredPos, endPos, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            if (panelRect != null)
                panelRect.anchoredPosition = _restAnchoredPos;
        }

        private void ApplyVisualTheme(BannerKind kind)
        {
            Color accent = kind == BannerKind.Missions ? UiTheme.Gold : UiTheme.AccentAmber;
            Color bg = UiTheme.BgElevated;
            bg.a = 0.96f;

            if (backgroundImage != null)
                backgroundImage.color = bg;
            if (accentBar != null)
                accentBar.color = accent;
            if (glowImage != null)
            {
                Color g = accent;
                g.a = 0.22f;
                glowImage.color = g;
            }
            if (eyebrowText != null)
                eyebrowText.color = accent;
            if (titleText != null)
                titleText.color = UiTheme.TextPrimary;
        }

        private void HideVisual()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float e = 0f;
            while (e < seconds)
            {
                e += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
