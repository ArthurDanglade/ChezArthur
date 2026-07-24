using System.Collections;
using ChezArthur.Hub;
using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Fade croisé 0,15 s entre pages Hub. Orchestration SetActive via HubManager.ShowPage.
    /// Zéro Animator ; coroutine + caches (pas d'alloc en régime hors transition).
    /// </summary>
    [DisallowMultipleComponent]
    public class PageTransitionController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float FadeDuration = 0.15f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private HubManager hubManager;
        [SerializeField] private HubNavBarUI navBar;
        [SerializeField] private CanvasGroup[] pageGroups;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _busy;
        private Coroutine _fadeRoutine;
        private bool _subscribed;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
            SyncImmediate();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }
            _busy = false;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Subscribe()
        {
            if (_subscribed)
                return;

            if (navBar != null)
                navBar.OnTabTapped += HandleTabTapped;
            if (hubManager != null)
                hubManager.OnPageChanged += HandlePageChanged;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            if (navBar != null)
                navBar.OnTabTapped -= HandleTabTapped;
            if (hubManager != null)
                hubManager.OnPageChanged -= HandlePageChanged;

            _subscribed = false;
        }

        private void SyncImmediate()
        {
            if (hubManager == null)
                return;

            int index = hubManager.CurrentPageIndex;
            ApplyAlphasImmediate(index);
            if (navBar != null)
                navBar.Select(index);
        }

        private void HandlePageChanged(int index)
        {
            // Pendant un fade, Select est appliqué en fin de coroutine.
            if (_busy)
                return;

            ApplyAlphasImmediate(index);
            if (navBar != null)
                navBar.Select(index);
        }

        private void HandleTabTapped(int pageIndex)
        {
            if (_busy || hubManager == null)
                return;
            if (pageIndex == hubManager.CurrentPageIndex)
                return;
            if (pageGroups == null || pageIndex < 0 || pageIndex >= pageGroups.Length)
                return;
            if (pageGroups[pageIndex] == null)
                return;

            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(CrossfadeRoutine(pageIndex));
        }

        private IEnumerator CrossfadeRoutine(int toIndex)
        {
            _busy = true;

            int fromIndex = hubManager.CurrentPageIndex;
            CanvasGroup from = GetGroup(fromIndex);
            CanvasGroup to = GetGroup(toIndex);

            if (to == null)
            {
                _busy = false;
                _fadeRoutine = null;
                yield break;
            }

            // Active l'entrante pour le croisé ; ShowPage finalisera les SetActive.
            if (!to.gameObject.activeSelf)
                to.gameObject.SetActive(true);

            LockGroup(from, locked: true);
            LockGroup(to, locked: true);
            to.alpha = 0f;
            if (from != null)
                from.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = FadeDuration > 0f ? Mathf.Clamp01(elapsed / FadeDuration) : 1f;
                if (from != null)
                    from.alpha = 1f - u;
                to.alpha = u;
                yield return null;
            }

            if (from != null)
                from.alpha = 0f;
            to.alpha = 1f;

            hubManager.ShowPage(toIndex);

            // ShowPage a désactivé les autres ; restaure l'interactable sur la page active.
            CanvasGroup active = GetGroup(toIndex);
            if (active != null)
            {
                active.alpha = 1f;
                LockGroup(active, locked: false);
            }

            if (navBar != null)
                navBar.Select(toIndex);

            _busy = false;
            _fadeRoutine = null;
        }

        private void ApplyAlphasImmediate(int activeIndex)
        {
            if (pageGroups == null)
                return;

            for (int i = 0; i < pageGroups.Length; i++)
            {
                CanvasGroup cg = pageGroups[i];
                if (cg == null)
                    continue;

                bool on = i == activeIndex;
                cg.alpha = on ? 1f : 0f;
                LockGroup(cg, locked: !on);
                if (on)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
        }

        private CanvasGroup GetGroup(int index)
        {
            if (pageGroups == null || index < 0 || index >= pageGroups.Length)
                return null;
            return pageGroups[index];
        }

        private static void LockGroup(CanvasGroup cg, bool locked)
        {
            if (cg == null)
                return;
            cg.interactable = !locked;
            cg.blocksRaycasts = !locked;
        }
    }
}
