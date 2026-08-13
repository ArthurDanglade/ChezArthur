using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Pulse d'échelle Visual-only (callout de source F5-L2).
    /// Jamais le Rigidbody2D — pattern timer EnemyHitReaction, zéro alloc.
    /// </summary>
    public class UnitPulse : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float Duration = 0.25f;
        private const float PeakScale = 1.08f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Vector3 _baseScale = Vector3.one;
        private bool _captured;
        private float _timer;

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        /// <summary>
        /// Déclenche un pulse 1,0 → 1,08 → 1,0. Ré-entrant = repart du pic.
        /// </summary>
        public void PulseOnce()
        {
            if (!_captured)
            {
                _baseScale = transform.localScale;
                _captured = true;
            }

            _timer = Duration;
        }

        /// <summary>
        /// Récupère ou crée le pulse sur le Visual (paresseux).
        /// </summary>
        public static UnitPulse Ensure(Transform visualOrRoot)
        {
            if (visualOrRoot == null)
                return null;

            UnitPulse pulse = visualOrRoot.GetComponent<UnitPulse>();
            if (pulse == null)
                pulse = visualOrRoot.gameObject.AddComponent<UnitPulse>();
            return pulse;
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Update()
        {
            if (_timer <= 0f)
                return;

            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f)
            {
                _timer = 0f;
                transform.localScale = _baseScale;
                return;
            }

            // Aller-retour triangular : 0→0.5 pic, 0.5→1 retour.
            float t = 1f - (_timer / Duration);
            float amp = t < 0.5f
                ? (t / 0.5f)
                : (1f - ((t - 0.5f) / 0.5f));
            float mul = Mathf.Lerp(1f, PeakScale, amp);
            transform.localScale = _baseScale * mul;
        }
    }
}
