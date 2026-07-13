using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Enemies;

namespace ChezArthur.UI
{
    /// <summary>
    /// Barre de vie boss en haut de l'écran (HUD). Polling en LateUpdate, sans événements.
    /// Le GameObject host reste actif (coroutines) ; visibilité via CanvasGroup.
    /// </summary>
    public class BossHPBarUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image ghostFillImage;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float slideOffset = 40f;

        [Header("Ghost")]
        [SerializeField] private float ghostDelay = 0.35f;
        [SerializeField] private float ghostDrainSpeed = 1.2f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Enemy _enemy;
        private float _lastDamageTime;
        private Coroutine _fadeRoutine;
        private Vector2 _restAnchoredPosition;
        private bool _hasRestPosition;
        private bool _isShown;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            EnsureHostActive();
            CacheRestPosition();
            SetVisuallyHidden();
        }

        private void LateUpdate()
        {
            if (!_isShown)
                return;

            if (_enemy == null)
                return;

            if (_enemy.IsDead || !_enemy.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            int max = Mathf.Max(1, _enemy.MaxHp);
            float ratio = (float)_enemy.CurrentHp / max;

            if (fillImage != null)
            {
                if (ratio < fillImage.fillAmount)
                    _lastDamageTime = Time.time;

                fillImage.fillAmount = ratio;
            }

            if (ghostFillImage != null)
            {
                float ghost = ghostFillImage.fillAmount;

                if (ratio > ghost)
                {
                    ghostFillImage.fillAmount = ratio;
                }
                else if (Time.time - _lastDamageTime > ghostDelay)
                {
                    ghostFillImage.fillAmount = Mathf.MoveTowards(
                        ghost,
                        ratio,
                        ghostDrainSpeed * Time.deltaTime);
                }
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche la barre pour le boss donné (fade-in + slide).
        /// </summary>
        public void Show(Enemy enemy)
        {
            if (enemy == null)
                return;

            if (_enemy != null && _enemy != enemy)
                Debug.LogWarning("[BossHPBarUI] Remplacement du boss affiché.");

            EnsureHostActive();

            _enemy = enemy;

            if (bossNameText != null)
                bossNameText.text = enemy.Name;

            int max = Mathf.Max(1, enemy.MaxHp);
            float ratio = (float)enemy.CurrentHp / max;

            if (fillImage != null)
                fillImage.fillAmount = ratio;

            if (ghostFillImage != null)
                ghostFillImage.fillAmount = ratio;

            _lastDamageTime = 0f;

            CacheRestPosition();
            _isShown = true;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            StopFadeRoutine();
            _fadeRoutine = StartCoroutine(FadeInRoutine());
        }

        /// <summary>
        /// Masque la barre (fade-out). Idempotent.
        /// </summary>
        public void Hide()
        {
            if (!_isShown)
                return;

            EnsureHostActive();
            StopFadeRoutine();
            _fadeRoutine = StartCoroutine(FadeOutRoutine());
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private IEnumerator FadeInRoutine()
        {
            float duration = Mathf.Max(0.01f, fadeDuration);
            Vector2 startPos = _hasRestPosition
                ? _restAnchoredPosition + Vector2.down * slideOffset
                : Vector2.zero;

            if (panelRoot != null && _hasRestPosition)
                panelRoot.anchoredPosition = startPos;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = SmoothStep(t);

                if (canvasGroup != null)
                    canvasGroup.alpha = eased;

                if (panelRoot != null && _hasRestPosition)
                    panelRoot.anchoredPosition = Vector2.Lerp(startPos, _restAnchoredPosition, eased);

                yield return null;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            if (panelRoot != null && _hasRestPosition)
                panelRoot.anchoredPosition = _restAnchoredPosition;

            _fadeRoutine = null;
        }

        private IEnumerator FadeOutRoutine()
        {
            float duration = Mathf.Max(0.01f, fadeDuration);
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = SmoothStep(t);

                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);

                yield return null;
            }

            _enemy = null;
            SetVisuallyHidden();
            _fadeRoutine = null;
        }

        private void StopFadeRoutine()
        {
            if (_fadeRoutine == null)
                return;

            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        private void CacheRestPosition()
        {
            RectTransform rt = panelRoot != null ? panelRoot : transform as RectTransform;
            if (rt == null)
                return;

            _restAnchoredPosition = rt.anchoredPosition;
            _hasRestPosition = true;
        }

        private void SetVisuallyHidden()
        {
            _isShown = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void EnsureHostActive()
        {
            Transform t = transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    t.gameObject.SetActive(true);
                t = t.parent;
            }
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
