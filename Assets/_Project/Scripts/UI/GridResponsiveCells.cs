using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Dimensionne les cellules d'un GridLayoutGroup pour remplir exactement la largeur
    /// disponible sur N colonnes, en gardant un ratio de carte. Rend la grille responsive.
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class GridResponsiveCells : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private int _columns = 2;
        [Tooltip("Hauteur de cellule = largeur × ce ratio.")]
        [SerializeField] private float _cellAspect = 1.25f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private GridLayoutGroup _grid;
        private RectTransform _rt;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rt = (RectTransform)transform;
        }

        private void OnEnable() => Recalculate();

        private void OnRectTransformDimensionsChange()
        {
            if (_grid != null)
                Recalculate();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void Recalculate()
        {
            if (_columns < 1)
                _columns = 1;

            float spacing = _grid.spacing.x * (_columns - 1);
            float padding = _grid.padding.left + _grid.padding.right;
            float available = _rt.rect.width - padding - spacing;
            if (available <= 0f)
                return;

            float w = available / _columns;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = _columns;
            _grid.cellSize = new Vector2(w, w * _cellAspect);
        }
    }
}
