using UnityEngine;
using UnityEngine.UI;
using ChezArthur.UI;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Calcule la taille des cellules 5×2 selon la largeur/hauteur réelle du conteneur,
    /// avec marges UiTheme garanties — jamais de débordement horizontal.
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class GachaSummaryGridFitter : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int Columns = 5;
        private const int Rows = 2;
        private const float CardAspect = 1.55f; // H / W

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private GridLayoutGroup grid;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (grid == null)
                grid = GetComponent<GridLayoutGroup>();
        }

        private void OnEnable()
        {
            Fit();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                Fit();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Recalcule cellSize pour tenir 5×2 dans le rect courant + marges PadCard.
        /// </summary>
        public void Fit()
        {
            if (grid == null)
                grid = GetComponent<GridLayoutGroup>();
            if (grid == null)
                return;

            RectTransform rt = transform as RectTransform;
            if (rt == null)
                return;

            float width = rt.rect.width;
            float height = rt.rect.height;
            if (width < 8f || height < 8f)
                return;

            int margin = UiTheme.PadCard;
            int spacing = UiTheme.SpacingRow;

            grid.padding = new RectOffset(margin, margin, margin, margin);
            grid.spacing = new Vector2(spacing, spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            float maxCellW = (width - margin * 2f - spacing * (Columns - 1)) / Columns;
            float maxCellH = (height - margin * 2f - spacing * (Rows - 1)) / Rows;

            if (maxCellW < 1f || maxCellH < 1f)
                return;

            // Dimension limitante : largeur d'abord, sinon hauteur (portrait).
            float cellW = maxCellW;
            float cellH = cellW * CardAspect;
            if (cellH > maxCellH)
            {
                cellH = maxCellH;
                cellW = cellH / CardAspect;
            }

            // Sécurité : ne jamais dépasser la largeur dispo (float drift).
            if (cellW > maxCellW)
            {
                cellW = maxCellW;
                cellH = Mathf.Min(cellW * CardAspect, maxCellH);
            }

            grid.cellSize = new Vector2(cellW, cellH);
        }
    }
}
