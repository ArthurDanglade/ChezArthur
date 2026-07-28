using UnityEngine;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Portail de Faille posé sur une bordure d'arène (orange / cyan).
    /// </summary>
    public class FaillePortal : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        public const float PortalRadius = 0.55f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private FailleSystem _system;
        private FaillePortalEdge _edge;
        private int _slotIndex;
        private Vector2 _outwardNormal;
        private SpriteRenderer _renderer;
        private CircleCollider2D _collider;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public FaillePortalEdge Edge => _edge;
        public int SlotIndex => _slotIndex;
        public Vector2 OutwardNormal => _outwardNormal;
        public Vector2 WorldPosition => transform.position;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise le portail (slot 0 = orange, 1 = cyan).
        /// </summary>
        public void Initialize(FailleSystem system, int slotIndex)
        {
            _system = system;
            _slotIndex = slotIndex;

            EnsureComponents();
            ApplyColor(slotIndex == 0
                ? new Color(1f, 0.45f, 0.12f, 0.85f)
                : new Color(0.15f, 0.85f, 0.95f, 0.85f));
        }

        /// <summary>
        /// Place le portail sur une bordure à la position monde donnée.
        /// </summary>
        public void Place(FaillePortalEdge edge, Vector2 worldPos, Vector2 outwardNormal)
        {
            _edge = edge;
            _outwardNormal = outwardNormal.sqrMagnitude > 0.0001f
                ? outwardNormal.normalized
                : Vector2.up;

            transform.position = worldPos;
            float angle = Mathf.Atan2(_outwardNormal.y, _outwardNormal.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_system == null || other == null) return;
            _system.TryTeleportThrough(this, other);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void EnsureComponents()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
                _renderer = gameObject.AddComponent<SpriteRenderer>();

            if (_renderer.sprite == null)
                _renderer.sprite = CreateDiscSprite();

            _renderer.sortingOrder = 40;

            _collider = GetComponent<CircleCollider2D>();
            if (_collider == null)
                _collider = gameObject.AddComponent<CircleCollider2D>();

            _collider.isTrigger = true;
            _collider.radius = PortalRadius;
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null) return;
            _renderer.color = color;
        }

        private static Sprite CreateDiscSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = (size - 1) * 0.5f;
            float radius = center - 1f;
            float inner = radius * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 0f;
                    if (dist <= radius && dist >= inner)
                        a = 1f;
                    else if (dist < inner)
                        a = 0.35f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }

    /// <summary>
    /// Bordure d'arène utilisable pour poser un portail.
    /// </summary>
    public enum FaillePortalEdge
    {
        Top,
        Bottom,
        Left,
        Right
    }
}
