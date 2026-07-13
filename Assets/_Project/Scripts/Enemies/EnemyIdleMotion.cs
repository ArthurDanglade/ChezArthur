using UnityEngine;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Bob vertical et respiration légère sur le Visual ennemi au repos (hors lancement / hit react).
    /// </summary>
    public class EnemyIdleMotion : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private Enemy _enemy;
        [SerializeField] private Transform _visual;
        [SerializeField] private EnemyHitReaction _hitReaction;

        [Header("Bob")]
        [SerializeField] private float _bobAmplitude = 0.035f;
        [SerializeField] private float _bobPeriod = 2f;

        [Header("Respiration")]
        [SerializeField] private float _breathScale = 0.018f;
        [SerializeField] private float _breathPeriod = 2.6f;

        [Header("Lissage")]
        [SerializeField] private float _settleSpeed = 8f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _phase;
        private Vector3 _visualBasePos;
        private Vector3 _visualBaseScale;
        private bool _captured;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_enemy == null)
                _enemy = GetComponent<Enemy>();
            if (_hitReaction == null)
                _hitReaction = GetComponent<EnemyHitReaction>();

            _phase = Random.value * Mathf.PI * 2f;
            CaptureBase();
        }

        private void LateUpdate()
        {
            if (_visual == null)
                return;

            if (!CanPlayIdle())
            {
                LerpToBase();
                return;
            }

            ApplyIdle();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Mémorise la pose de base du Visual (après normalisation sprite / hitbox).
        /// </summary>
        public void CaptureBase()
        {
            if (_visual == null)
                return;

            _visualBasePos = _visual.localPosition;
            _visualBaseScale = _visual.localScale;
            _captured = true;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private bool CanPlayIdle()
        {
            if (!_captured || _enemy == null || _enemy.IsDead)
                return false;

            if (_enemy.IsMoving)
                return false;

            if (_hitReaction != null && _hitReaction.IsPlaying)
                return false;

            return true;
        }

        private void ApplyIdle()
        {
            float t = Time.time;
            float bob = Mathf.Sin(_phase + t * (Mathf.PI * 2f / _bobPeriod)) * _bobAmplitude;
            float breath = 1f + Mathf.Sin(_phase + t * (Mathf.PI * 2f / _breathPeriod)) * _breathScale;

            _visual.localPosition = _visualBasePos + new Vector3(0f, bob, 0f);
            _visual.localScale = _visualBaseScale * breath;
        }

        private void LerpToBase()
        {
            if (!_captured)
                return;

            _visual.localPosition = Vector3.Lerp(_visual.localPosition, _visualBasePos, Time.deltaTime * _settleSpeed);
            _visual.localScale = Vector3.Lerp(_visual.localScale, _visualBaseScale, Time.deltaTime * _settleSpeed);
        }
    }
}
