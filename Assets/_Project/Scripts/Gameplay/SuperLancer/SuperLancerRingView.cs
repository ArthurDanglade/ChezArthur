using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.UI;

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

        [Header("Flash de réussite")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _successFlashDuration = 0.25f;
        [SerializeField] private float _successScalePunch = 1.15f;

        // ═══════════════════════════════════════════
        // ÉTAT
        // ═══════════════════════════════════════════
        private float _baseScale;
        private Color _zoneBaseColor;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Affiche l'anneau à la position monde et synchronise zone + indicateur. </summary>
        public void Show(Vector3 worldPosition, SuperLancerConfig config)
        {
            if (config == null) return;

            StopAllCoroutines();

            _baseScale = worldDiameter / referenceSizePx;
            _zoneBaseColor = UiTheme.SuperLancerZone;

            transform.position = worldPosition;
            transform.localScale = Vector3.one * _baseScale;

            if (zoneArc != null)
                zoneArc.color = _zoneBaseColor;
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            SyncZone(config);
            SetIndicatorAngle(config.IndicatorStartAngleDeg);
            gameObject.SetActive(true);
        }

        /// <summary> Masque l'anneau. </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Masque l'anneau avec le flash de réussite du Super Lancer
        /// (pop d'échelle + blanchiment de la zone + fondu).
        /// </summary>
        public void HideWithSuccessFlash()
        {
            if (!gameObject.activeSelf) return;

            StopAllCoroutines();
            StartCoroutine(SuccessFlashRoutine());
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

        // Exception documentée à la règle « vue sans logique » : animation transitoire
        // autonome, aucune décision de gameplay.
        private IEnumerator SuccessFlashRoutine()
        {
            if (zoneArc != null)
                zoneArc.color = UiTheme.TextPrimary;

            float elapsed = 0f;
            while (elapsed < _successFlashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t01 = _successFlashDuration > 0f
                    ? Mathf.Clamp01(elapsed / _successFlashDuration)
                    : 1f;

                transform.localScale = Vector3.one * (_baseScale * Mathf.Lerp(_successScalePunch, 1f, t01));

                if (_canvasGroup != null)
                    _canvasGroup.alpha = 1f - t01;

                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
