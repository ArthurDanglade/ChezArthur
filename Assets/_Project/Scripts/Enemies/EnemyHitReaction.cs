using UnityEngine;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Knockback visuel et squash sur l'enfant Visual quand l'ennemi est frappé.
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
        private MaterialPropertyBlock _mpb;
        private static readonly int _flashId = Shader.PropertyToID("_FlashAmount");

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public bool IsPlaying => _timer > 0f || _flashTimer > 0f;

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
        /// </summary>
        public void Trigger(Vector2 hitDirection, float intensity01 = 1f)
        {
            if (_visual == null) return;
            if (!_captured) CaptureBase();
            if (hitDirection.sqrMagnitude > 0.0001f)
                _hitDir = hitDirection.normalized;
            _intensity = Mathf.Clamp01(intensity01);
            _timer = _reactionDuration;
            _flashTimer = _flashDuration;
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void LateUpdate()
        {
            if (_timer <= 0f && _flashTimer <= 0f) return;

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
