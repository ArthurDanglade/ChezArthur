using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Feedbacks hold partagés : punch succès, shake échec, pulse Danger dock (Gate 5.c).
    /// Coroutines sur un runner fourni — zéro Animator.
    /// </summary>
    public static class HoldFeedback
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float PunchPeak = 1.12f;
        private const float PunchDuration = 0.15f;
        private const float ShakePx = 4f;
        private const float ShakeDuration = 0.12f;
        private const float DockPulseDuration = 0.2f;
        private const float DockPulseAlpha = 0.25f;

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        public static Coroutine PlaySuccess(
            MonoBehaviour runner,
            Transform target,
            Image borderFlash = null)
        {
            if (runner == null || target == null)
                return null;
            return runner.StartCoroutine(SuccessRoutine(target, borderFlash));
        }

        public static Coroutine PlayFailShake(
            MonoBehaviour runner,
            Transform target)
        {
            if (runner == null || target == null)
                return null;
            return runner.StartCoroutine(ShakeRoutine(target));
        }

        public static Coroutine PlayDockDangerPulse(
            MonoBehaviour runner,
            Graphic dockGraphic)
        {
            if (runner == null || dockGraphic == null)
                return null;
            return runner.StartCoroutine(DockPulseRoutine(dockGraphic));
        }

        // ═══════════════════════════════════════════
        // ROUTINES
        // ═══════════════════════════════════════════

        private static IEnumerator SuccessRoutine(Transform target, Image borderFlash)
        {
            Vector3 baseScale = target.localScale;
            Color borderRest = borderFlash != null ? borderFlash.color : default;

            float half = PunchDuration * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / half);
                target.localScale = Vector3.LerpUnclamped(baseScale, baseScale * PunchPeak, k);
                if (borderFlash != null)
                {
                    Color c = UiTheme.AccentAmber;
                    c.a = Mathf.Lerp(borderRest.a, 1f, k);
                    borderFlash.color = c;
                }

                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / half);
                target.localScale = Vector3.LerpUnclamped(baseScale * PunchPeak, baseScale, k);
                if (borderFlash != null)
                    borderFlash.color = Color.Lerp(
                        UiTheme.AccentAmber, borderRest, k);
                yield return null;
            }

            target.localScale = baseScale;
            if (borderFlash != null)
                borderFlash.color = borderRest;
        }

        private static IEnumerator ShakeRoutine(Transform target)
        {
            Vector3 basePos = target.localPosition;
            float t = 0f;
            while (t < ShakeDuration)
            {
                t += Time.unscaledDeltaTime;
                float ox = Mathf.Sin(t * 70f) * ShakePx;
                target.localPosition = basePos + new Vector3(ox, 0f, 0f);
                yield return null;
            }

            target.localPosition = basePos;
        }

        private static IEnumerator DockPulseRoutine(Graphic dockGraphic)
        {
            Color rest = dockGraphic.color;
            Color danger = UiTheme.Danger;
            danger.a = DockPulseAlpha;

            float half = DockPulseDuration * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                dockGraphic.color = Color.Lerp(rest, danger, Mathf.Clamp01(t / half));
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                dockGraphic.color = Color.Lerp(danger, rest, Mathf.Clamp01(t / half));
                yield return null;
            }

            dockGraphic.color = rest;
        }
    }
}
