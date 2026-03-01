using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Personnage placeholder en forme de balle : lancement, rebonds, arrêt.
    /// Aux impacts : decay dynamique (peu de perte rapide, forte perte lente). Sous un ratio de la vitesse de lancement, la vitesse baisse d'elle-même chaque frame → arrêt naturel sans traîner.
    /// </summary>
    public class CharacterBall : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BOUNCY_MATERIAL_NAME = "BouncyMaterial";
        /// <summary> En dessous de cette vitesse (magnitude), on considère arrêt total et on déclenche OnStopped. </summary>
        private const float FINAL_STOP_THRESHOLD = 0.01f;
        private static readonly float FINAL_STOP_THRESHOLD_SQR = FINAL_STOP_THRESHOLD * FINAL_STOP_THRESHOLD;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Collider")]
        [SerializeField] private float radius = 0.5f;

        [Header("Ralentissement dynamique (style Monster Strike)")]
        [Tooltip("Decay quand la balle est lente (perte très forte → arrêt net, évite qu'elle erre). Plus bas = arrêt plus rapide.")]
        [SerializeField] private float minDecay = 0.25f;
        [Tooltip("Decay quand la balle est rapide (perte faible).")]
        [SerializeField] private float maxDecay = 0.95f;

        [Header("Ralentissement continu (vitesse lente)")]
        [Tooltip("Quand la vitesse actuelle tombe sous ce ratio de la vitesse de lancement (ex: 0,5 = 50%), la balle perd de la vitesse toute seule chaque frame, sans attendre les impacts. Arrêt naturel rapide.")]
        [SerializeField] private float speedRatioThreshold = 0.5f;
        [Tooltip("Multiplicateur de vélocité appliqué chaque frame quand on est sous le ratio (vitesse baisse d'elle-même). Plus bas = arrêt plus rapide (ex: 0,9 = perd 10% par frame).")]
        [SerializeField] private float continuousDecayPerFrame = 0.9f;

        [Header("Physique (optionnel)")]
        [Tooltip("Si non assigné, un matériau bounciness=1 / friction=0 est créé en Awake.")]
        [SerializeField] private PhysicsMaterial2D bouncyMaterial;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Rigidbody2D _rb;
        private CircleCollider2D _circleCollider;
        private bool _hasStoppedForThisLaunch;
        private float _launchSpeed;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> True si la vélocité est au-dessus du seuil d'arrêt (personnage encore en mouvement). </summary>
        public bool IsMoving => _rb != null && _rb.velocity.sqrMagnitude > FINAL_STOP_THRESHOLD_SQR;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché une fois quand le personnage s'arrête (ralentissement progressif jusqu'à l'arrêt). </summary>
        public event System.Action OnStopped;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            SetupRigidbody();
            SetupCircleCollider();
            ApplyBouncyMaterial();
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;
            if (_hasStoppedForThisLaunch) return;

            float speedSqr = _rb.velocity.sqrMagnitude;
            if (speedSqr <= FINAL_STOP_THRESHOLD_SQR)
            {
                _rb.velocity = Vector2.zero;
                TriggerStopped();
                return;
            }

            // Vitesse baisse d'elle-même sous un certain ratio (ex: 50% de la vitesse de lancement), sans attendre les murs
            if (_launchSpeed >= 0.01f)
            {
                float launchSpeedSqr = _launchSpeed * _launchSpeed;
                float slowZoneSqr = launchSpeedSqr * speedRatioThreshold * speedRatioThreshold;
                if (speedSqr <= slowZoneSqr)
                    _rb.velocity *= continuousDecayPerFrame;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Decay dynamique : peu de perte quand rapide, perte forte quand lent → arrêt naturel
            if (_launchSpeed >= 0.01f)
            {
                float speedRatio = Mathf.Clamp01(_rb.velocity.magnitude / _launchSpeed);
                float decay = Mathf.Lerp(minDecay, maxDecay, speedRatio);
                _rb.velocity *= decay;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Lance le personnage dans la direction donnée avec la force donnée.
        /// </summary>
        public void Launch(Vector2 direction, float force)
        {
            if (_rb == null) return;
            if (force <= 0f) return;

            Vector2 dir = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
            _rb.AddForce(dir * force, ForceMode2D.Impulse);
            _launchSpeed = force / _rb.mass;
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
