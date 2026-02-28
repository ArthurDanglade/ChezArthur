using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Personnage placeholder en forme de balle : lancement, rebonds, arrêt.
    /// Le ralentissement est géré par le système de vitesse GDD (impacts), pas par le drag.
    /// </summary>
    public class CharacterBall : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BOUNCY_MATERIAL_NAME = "BouncyMaterial";
        private const float SPEED_DECREMENT = 5f;
        private const int IMPACTS_PER_DECREMENT = 5;
        private const float MIN_SPEED_TO_MOVE = 10f;
        private const float VELOCITY_STOP_THRESHOLD = 0.5f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Collider")]
        [SerializeField] private float radius = 0.5f;

        [Header("Vitesse (GDD)")]
        [SerializeField] private float initialSpeed = 100f;

        [Header("Physique (optionnel)")]
        [Tooltip("Si non assigné, un matériau bounciness=1 / friction=0 est créé en Awake.")]
        [SerializeField] private PhysicsMaterial2D bouncyMaterial;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Rigidbody2D _rb;
        private CircleCollider2D _circleCollider;
        private int _impactCount;
        private float _currentSpeed;
        private bool _hasStoppedForThisLaunch;
        private float _velocityStopThresholdSqr;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> True si la vélocité est au-dessus du seuil d'arrêt. </summary>
        public bool IsMoving => _rb != null && _rb.velocity.sqrMagnitude > _velocityStopThresholdSqr;

        /// <summary> Vitesse actuelle (ressource GDD, diminue tous les 5 impacts). </summary>
        public float CurrentSpeed => _currentSpeed;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché une fois quand le personnage s'arrête (vélocité sous le seuil ou vitesse &lt; 10). </summary>
        public event System.Action OnStopped;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _velocityStopThresholdSqr = VELOCITY_STOP_THRESHOLD * VELOCITY_STOP_THRESHOLD;
            _currentSpeed = initialSpeed;

            SetupRigidbody();
            SetupCircleCollider();
            ApplyBouncyMaterial();
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;

            // Arrêt forcé si la vitesse GDD est épuisée
            if (_currentSpeed < MIN_SPEED_TO_MOVE)
            {
                if (_rb.velocity.sqrMagnitude > 0f)
                {
                    _rb.velocity = Vector2.zero;
                    TriggerStopped();
                }
                return;
            }

            // Arrêt naturel : vélocité sous le seuil
            if (_hasStoppedForThisLaunch) return;

            if (_rb.velocity.sqrMagnitude <= _velocityStopThresholdSqr)
            {
                _rb.velocity = Vector2.zero;
                TriggerStopped();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            _impactCount++;

            if (_impactCount % IMPACTS_PER_DECREMENT == 0)
            {
                _currentSpeed -= SPEED_DECREMENT;
                if (_currentSpeed < MIN_SPEED_TO_MOVE)
                    _currentSpeed = Mathf.Max(0f, _currentSpeed);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Lance le personnage dans la direction donnée avec la force donnée (plafonnée par la vitesse GDD).
        /// Ignoré si la vitesse est &lt; 10.
        /// </summary>
        public void Launch(Vector2 direction, float force)
        {
            if (_rb == null) return;
            if (_currentSpeed < MIN_SPEED_TO_MOVE) return;

            float effectiveForce = Mathf.Min(force, _currentSpeed);
            if (effectiveForce <= 0f) return;

            Vector2 dir = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
            _rb.AddForce(dir * effectiveForce, ForceMode2D.Impulse);
            _hasStoppedForThisLaunch = false;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void SetupRigidbody()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
                _rb = gameObject.AddComponent<Rigidbody2D>();

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.drag = 0f;
            _rb.angularDrag = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void SetupCircleCollider()
        {
            _circleCollider = GetComponent<CircleCollider2D>();
            if (_circleCollider == null)
                _circleCollider = gameObject.AddComponent<CircleCollider2D>();

            _circleCollider.radius = radius;
        }

        private void ApplyBouncyMaterial()
        {
            if (_circleCollider == null) return;

            PhysicsMaterial2D material = bouncyMaterial;
            if (material == null)
            {
                material = new PhysicsMaterial2D
                {
                    name = BOUNCY_MATERIAL_NAME,
                    bounciness = 1f,
                    friction = 0f
                };
            }

            _circleCollider.sharedMaterial = material;
        }

        private void TriggerStopped()
        {
            if (_hasStoppedForThisLaunch) return;
            _hasStoppedForThisLaunch = true;
            OnStopped?.Invoke();
        }
    }
}
