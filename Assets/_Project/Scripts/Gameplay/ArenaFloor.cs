using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Sol procédural de l'arène : grille de tiles (SpriteRenderer) avec object pooling.
    /// Les tiles sont créées une fois en Awake et repositionnées dans BuildFloor.
    /// </summary>
    public class ArenaFloor : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private Arena arena;

        [Header("Tile")]
        [SerializeField] private Sprite tileSprite;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Color tileColor = Color.white;
        [SerializeField] private int sortingOrder = -1;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private GameObject[] _tiles;
        private int _tilesX;
        private int _tilesY;
        private Vector3 _reusePosition;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (arena == null)
            {
                Debug.LogWarning("[ArenaFloor] Arena non assignée.", this);
                return;
            }
            if (tileSprite == null)
            {
                Debug.LogWarning("[ArenaFloor] tileSprite non assigné.", this);
                return;
            }

            _tilesX = Mathf.CeilToInt(arena.Width / tileSize);
            _tilesY = Mathf.CeilToInt(arena.Height / tileSize);
            int count = _tilesX * _tilesY;
            _tiles = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                GameObject tile = new GameObject("ArenaFloorTile");
                tile.transform.SetParent(transform);

                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = tileSprite;
                sr.color = tileColor;
                sr.sortingOrder = sortingOrder;

                tile.SetActive(false);
                _tiles[i] = tile;
            }
        }

        private void Start()
        {
            BuildFloor();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Repositionne et active les tiles du pool sur la grille de l'arène.
        /// Les tiles qui dépassent les bounds restent désactivées.
        /// </summary>
        public void BuildFloor()
        {
            if (arena == null)
            {
                Debug.LogWarning("[ArenaFloor] BuildFloor: Arena non assignée.", this);
                return;
            }
            if (_tiles == null) return;

            Bounds bounds = arena.Bounds;
            Vector3 min = bounds.min;

            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i].SetActive(false);

                int ix = i % _tilesX;
                int iy = i / _tilesX;

                _reusePosition.x = min.x + (ix + 0.5f) * tileSize;
                _reusePosition.y = min.y + (iy + 0.5f) * tileSize;
                _reusePosition.z = 0f;

                if (bounds.Contains(_reusePosition))
                {
                    _tiles[i].transform.position = _reusePosition;
                    _tiles[i].SetActive(true);
                }
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
    }
}
