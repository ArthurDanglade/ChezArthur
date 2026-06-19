using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Découpe visuellement l'arène en trois bandes (identité haute / sol jouable / bande basse UI),
    /// dérivées des bounds de l'Arena via des ratios. N'a AUCUNE incidence sur la physique :
    /// sert d'ancrage pour les calques de décor (Dharu) et de référence pour la safe area.
    /// Les bandes sont recalculées à la volée → suivent automatiquement un redimensionnement.
    /// </summary>
    [RequireComponent(typeof(Arena))]
    public class ArenaLayout : MonoBehaviour
    {
        [Header("Ratios des bandes (haut + bas, le reste = sol)")]
        [SerializeField] [Range(0f, 0.45f)] private float topBandRatio = 0.15f;
        [SerializeField] [Range(0f, 0.45f)] private float bottomBandRatio = 0.15f;

        [Header("Gizmos (édition)")]
        [SerializeField] private bool drawGizmos = true;

        private Arena _arena;

        private Arena ArenaRef
        {
            get
            {
                if (_arena == null)
                    _arena = GetComponent<Arena>();
                return _arena;
            }
        }

        /// <summary> Cadre 9:16 complet = centre sûr, toujours visible sur tous les écrans. </summary>
        public Bounds SafeCenter => ArenaRef.Bounds;

        /// <summary> Bande d'identité haute (décor : trône, vitrail). </summary>
        public Bounds TopBand => ComputeBand(1f - topBandRatio, 1f);

        /// <summary> Sol jouable central : zone lisible où se lit l'action. </summary>
        public Bounds Floor => ComputeBand(bottomBandRatio, 1f - topBandRatio);

        /// <summary> Bande basse, sous l'UI. </summary>
        public Bounds BottomBand => ComputeBand(0f, bottomBandRatio);

        /// <summary>
        /// Calcule une bande horizontale entre deux fractions de hauteur (0 = bas, 1 = haut).
        /// </summary>
        private Bounds ComputeBand(float fromFraction, float toFraction)
        {
            Bounds b = ArenaRef.Bounds;
            float yBottom = b.min.y + fromFraction * b.size.y;
            float yTop = b.min.y + toFraction * b.size.y;
            float center = (yBottom + yTop) * 0.5f;
            float height = yTop - yBottom;
            return new Bounds(
                new Vector3(b.center.x, center, 0f),
                new Vector3(b.size.x, height, 0f));
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || ArenaRef == null)
                return;

            DrawBand(TopBand, new Color(0.55f, 0.35f, 0.85f, 0.20f));    // Identité — violet
            DrawBand(Floor, new Color(0.25f, 0.75f, 0.60f, 0.15f));      // Sol jouable — turquoise
            DrawBand(BottomBand, new Color(0.90f, 0.65f, 0.25f, 0.20f)); // Bande UI — ambre
        }

        private void DrawBand(Bounds band, Color fill)
        {
            Gizmos.color = fill;
            Gizmos.DrawCube(band.center, band.size);
            Gizmos.color = new Color(fill.r, fill.g, fill.b, 0.8f);
            Gizmos.DrawWireCube(band.center, band.size);
        }
    }
}
