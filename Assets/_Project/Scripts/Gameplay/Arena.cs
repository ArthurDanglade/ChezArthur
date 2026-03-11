using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Définit l'arène de jeu : rectangle avec murs en EdgeCollider2D pour les rebonds.
    /// Applique un Physics Material 2D (bounciness = 1, friction = 0) pour des rebonds parfaits.
    /// </summary>
    public class Arena : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BOUNCY_MATERIAL_NAME = "BouncyMaterial";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Dimensions")]
        [SerializeField] private float width = 10f;
        [SerializeField] private float height = 16f;

        [Header("Physique (optionnel)")]
        [Tooltip("Si non assigné, un matériau bounciness=1 / friction=0 est créé en Awake.")]
        [SerializeField] private PhysicsMaterial2D bouncyMaterial;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private EdgeCollider2D _edgeCollider;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Bounds de l'arène en espace monde (centre = position du GameObject). </summary>
        public Bounds Bounds => new Bounds(transform.position, new Vector3(width, height, 0f));

        /// <summary> Largeur de l'arène en unités monde. </summary>
        public float Width => width;

        /// <summary> Hauteur de l'arène en unités monde. </summary>
        public float Height => height;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            SetupEdgeCollider();
            ApplyBouncyMaterial();
        }

        private void OnValidate()
        {
            // Recalcule le collider quand les valeurs changent dans l'Inspector
            if (_edgeCollider != null)
            {
                SetupEdgeCollider();
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Récupère ou ajoute l'EdgeCollider2D et configure les points du rectangle.
        /// </summary>
        private void SetupEdgeCollider()
        {
            _edgeCollider = GetComponent<EdgeCollider2D>();
            if (_edgeCollider == null)
                _edgeCollider = gameObject.AddComponent<EdgeCollider2D>();

            float halfW = width * 0.5f;
            float halfH = height * 0.5f;

            // Rectangle fermé en espace local (centre = 0,0)
            _edgeCollider.points = new Vector2[]
            {
                new Vector2(-halfW, -halfH),
                new Vector2(halfW, -halfH),
                new Vector2(halfW, halfH),
                new Vector2(-halfW, halfH),
                new Vector2(-halfW, -halfH)
            };
        }

        /// <summary>
        /// Utilise le matériau assigné ou en crée un (bounciness=1, friction=0) et l'applique à l'EdgeCollider2D.
        /// </summary>
        private void ApplyBouncyMaterial()
        {
            if (_edgeCollider == null)
                return;

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

            _edgeCollider.sharedMaterial = material;
        }
    }
}
