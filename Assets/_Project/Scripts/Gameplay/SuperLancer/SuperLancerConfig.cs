using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Configuration du système Super Lancer (zone angulaire, rotation de l'indicateur, récompense).
    /// Convention angulaire unique : 0° = sommet de l'anneau (12h), sens horaire positif.
    /// Toute logique angulaire du système passe par cette convention — ne jamais la réinterpréter ailleurs.
    /// </summary>
    [CreateAssetMenu(fileName = "SuperLancerConfig", menuName = "Chez Arthur/Gameplay/Super Lancer Config")]
    public class SuperLancerConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Anneau — géométrie du geste")]
        [Tooltip("Fraction de l'anneau couverte par la zone Super Lancer.")]
        [Range(0.05f, 0.35f)]
        [SerializeField] private float zoneSizeNormalized = 0.18f;

        [Tooltip("Centre de la zone (0 = 12h, convention de classe).")]
        [SerializeField] private float zoneCenterAngleDeg = 0f;

        [Tooltip("Angle de départ de l'indicateur au début de la visée.")]
        [SerializeField] private float indicatorStartAngleDeg = 180f;

        [Header("Rotation de l'indicateur")]
        [Tooltip("Vitesse de rotation initiale de l'indicateur (°/s).")]
        [SerializeField] private float startSpeedDegPerSec = 140f;

        [Tooltip("Vitesse de rotation maximale de l'indicateur (°/s).")]
        [SerializeField] private float maxSpeedDegPerSec = 520f;

        [Tooltip("Durée (s) pour atteindre la vitesse maximale.")]
        [SerializeField] private float timeToMaxSpeedSeconds = 5f;

        [Tooltip("Forme de la rampe entre vitesse de départ et plafond.")]
        [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("True = rotation horaire, false = anti-horaire.")]
        [SerializeField] private bool clockwise = true;

        [Header("Récompense")]
        [Tooltip("Bonus de force appliqué en multiplicateur de la force finale, après cap.")]
        [SerializeField] private float baseLaunchForceBonus = 0.15f;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public float ZoneSizeNormalized => zoneSizeNormalized;
        public float ZoneCenterAngleDeg => zoneCenterAngleDeg;
        public float IndicatorStartAngleDeg => indicatorStartAngleDeg;
        public float StartSpeedDegPerSec => startSpeedDegPerSec;
        public float MaxSpeedDegPerSec => maxSpeedDegPerSec;
        public float TimeToMaxSpeedSeconds => timeToMaxSpeedSeconds;
        public AnimationCurve AccelerationCurve => accelerationCurve;
        public bool Clockwise => clockwise;
        public float BaseLaunchForceBonus => baseLaunchForceBonus;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Vitesse de rotation (°/s) après une durée de visée donnée. </summary>
        public float GetRotationSpeed(float aimDurationSeconds)
        {
            float t = timeToMaxSpeedSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(aimDurationSeconds / timeToMaxSpeedSeconds);

            return Mathf.Lerp(startSpeedDegPerSec, maxSpeedDegPerSec, accelerationCurve.Evaluate(t));
        }

        /// <summary> True si l'angle donné (convention de classe) est dans la zone Super Lancer. </summary>
        public bool IsInZone(float angleDeg)
        {
            float halfWindow = zoneSizeNormalized * 360f * 0.5f;
            return Mathf.Abs(Mathf.DeltaAngle(zoneCenterAngleDeg, angleDeg)) <= halfWindow;
        }
    }
}
