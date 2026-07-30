using UnityEngine;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Knockback visuel et squash sur l'enfant Visual quand l'ennemi est frappé.
    /// Inclut le wind-up de pré-lancer (R6) : flash montant + pulse d'échelle.
    /// </summary>
    public class EnemyHitReaction : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private Transform _visual;
        [SerializeField] private SpriteRenderer _visualRenderer;

        [Header("Réglages")]
        [SerializeField] private float _reactionDuration = 0.18f;
        [SerializeField] private float _knockbackDist = 0.15f;
        [SerializeField] private float _squashAmount = 0.25f;

        [Header("Flash")]
        [SerializeField] private float _flashDuration = 0.08f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Vector3 _baseLocalPos;
        private Vector3 _baseScale;
        private bool _captured;
        private float _timer;
        private Vector2 _hitDir = Vector2.up;
        private float _intensity = 1f;
        private float _flashTimer;
        private float _windupTimer;
        private float _windupDuration;
        private MaterialPropertyBlock _mpb;
        private static readonly int _flashId = Shader.PropertyToID("_FlashAmount");

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public bool IsPlaying => _timer > 0f || _flashTimer > 0f || _windupTimer > 0f;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Mémorise la position et l'échelle de base du Visual (après normalisation).
        /// </summary>
        public void CaptureBase()
        {
            if (_visual == null) return;
            _baseLocalPos = _visual.localPosition;
            _baseScale = _visual.localScale;
            _captured = true;
        }

        /// <summary>
        /// Déclenche le jolt visuel dans la direction du coup.
        /// Un hit pendant le wind-up prime : coupe le wind-up avant le hit-react.
        /// </summary>
        public void Trigger(Vector2 hitDirection, float intensity01 = 1f)
        {
            if (_visual == null) return;
            if (!_captured) CaptureBase();

            // Hit pendant wind-up → le hit-react reprend la main.
            if (_windupTimer > 0f)
            {
                _windupTimer = 0f;
                SetFlash(0f);
                _visual.localScale = _baseScale;
            }

            if (hitDirection.sqrMagnitude > 0.0001f)
                _hitDir = hitDirection.normalized;
            _intensity = Mathf.Clamp01(intensity01);
            _timer = _reactionDuration;
            _flashTimer = _flashDuration;
        }

        /// <summary>
        /// Wind-up de pré-lancer (R6) : flash ease-in + pulse d'échelle sur la durée.
        /// </summary>
        public void PlayWindup(float duration)
        {
            if (duration <= 0f)
                return;
            if (_visual == null)
                return;
            if (!_captured)
                CaptureBase();

            _windupDuration = duration;
            _windupTimer = duration;
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void LateUpdate()
        {
            if (_timer <= 0f && _flashTimer <= 0f && _windupTimer <= 0f)
                return;

            // Wind-up (R6) — flash montant + pulse ; zéro alloc (MPB réutilisé).
            if (_windupTimer > 0f && _visual != null)
            {
                _windupTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(_windupTimer / Mathf.Max(0.0001f, _windupDuration));
                // Ease-in : le flash culmine juste avant le lancer (R12).
                SetFlash(t * t * 0.6f);
                // Pulse 1 → ×1.08 → 1
                float scaleMul = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
                _visual.localScale = _baseScale * scaleMul;

                if (_windupTimer <= 0f)
                {
                    SetFlash(0f);
                    _visual.localScale = _baseScale;
                }
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                SetFlash(Mathf.Clamp01(_flashTimer / _flashDuration));
                if (_flashTimer <= 0f)
                    SetFlash(0f);
            }

            if (_timer > 0f && _visual != null)
            {
                _timer -= Time.deltaTime;
                float k = Mathf.Clamp01(_timer / _reactionDuration) * _intensity;
                _visual.localPosition = _baseLocalPos + (Vector3)(_hitDir * (_knockbackDist * k));
                _visual.localScale = _baseScale * (1f - _squashAmount * k);
                if (_timer <= 0f)
                {
                    _visual.localPosition = _baseLocalPos;
                    _visual.localScale = _baseScale;
                }
            }
        }

        private void SetFlash(float amount)
        {
            if (_visualRenderer == null) return;
            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();
            _visualRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_flashId, amount);
            _visualRenderer.SetPropertyBlock(_mpb);
        }
    }
}
