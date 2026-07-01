using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Vue de l'anneau Super Lancer — data binding pur (zone, indicateur, visibilité).
    /// PARE-FEU DE CONVENTION angulaire : convention système (SuperLancerConfig) =
    /// 0° = 12h, sens horaire positif ; convention Unity = rotation Z anti-horaire positive.
    /// La conversion (euler Z = -angle système) est faite ici et uniquement ici.
    /// </summary>
    public class SuperLancerRingView : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image ringBase;
        [SerializeField] private Image zoneArc;
        [SerializeField] private RectTransform indicatorPivot;

        [Tooltip("Diamètre de l'anneau en unités monde — présentation, pas skill check.")]
        [SerializeField] private float worldDiameter = 1.5f;

        [Tooltip("Taille du rect du canvas en px, pour le calcul d'échelle monde.")]
        [SerializeField] private float referenceSizePx = 512f;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Affiche l'anneau à la position monde et synchronise zone + indicateur. </summary>
        public void Show(Vector3 worldPosition, SuperLancerConfig config)
        {
            if (config == null) return;

            transform.position = worldPosition;
            transform.localScale = Vector3.one * (worldDiameter / referenceSizePx);

            SyncZone(config);
            SetIndicatorAngle(config.IndicatorStartAngleDeg);
            gameObject.SetActive(true);
        }

        /// <summary> Masque l'anneau. </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary> Oriente l'indicateur selon l'angle système (0° = 12h, horaire positif). </summary>
        public void SetIndicatorAngle(float systemAngleDeg)
        {
            if (indicatorPivot == null) return;

            // Conversion convention système → Unity, seule occurrence autorisée.
            indicatorPivot.localEulerAngles = new Vector3(0f, 0f, -systemAngleDeg);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void SyncZone(SuperLancerConfig config)
        {
            if (zoneArc == null) return;

            zoneArc.fillAmount = config.ZoneSizeNormalized;

            // Centrage : en Filled/Radial360, origine Top, fillClockwise true,
            // l'arc se déroule DEPUIS 12h en horaire. Son centre angulaire est donc
            // à +demiArc (horaire). Pour le centrer sur ZoneCenterAngleDeg (horaire
            // depuis 12h), on tourne l'Image en Z Unity (anti-horaire positif) de :
            float halfArcDeg = config.ZoneSizeNormalized * 180f;
            zoneArc.rectTransform.localEulerAngles =
                new Vector3(0f, 0f, halfArcDeg - config.ZoneCenterAngleDeg);
        }
    }
}
