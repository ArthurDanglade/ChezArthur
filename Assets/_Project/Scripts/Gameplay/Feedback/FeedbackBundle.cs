using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Bundle VFX/SFX/caméra pour un événement de feedback (données catalogue).
    /// </summary>
    [System.Serializable]
    public class FeedbackBundle
    {
        public enum TintMode
        {
            None,
            Cause,
            Custom
        }

        public enum AttachMode
        {
            World,
            FollowTarget
        }

        public enum VoiceFamily
        {
            Impacts,
            Statuts,
            Moments,
            UI
        }

        public enum HapticLevel
        {
            None,
            Light,
            Medium,
            Heavy
        }

        // ═══════════════════════════════════════════
        // VFX
        // ═══════════════════════════════════════════
        [Tooltip("Prefab particules (null = pas de visuel). One-shot only — pas de loop.")]
        public ParticleSystem vfxPrefab;

        [Tooltip("Cause de teinte si TintMode = Cause.")]
        public FeedbackCause tintCause = FeedbackCause.None;

        public TintMode tintMode = TintMode.None;

        [Tooltip("Teinte custom si TintMode = Custom.")]
        public Color customTint = Color.white;

        public AttachMode attachMode = AttachMode.World;

        [Tooltip("Échelle locale du VFX au spawn.")]
        public float vfxScale = 1f;

        // ═══════════════════════════════════════════
        // SFX
        // ═══════════════════════════════════════════
        [Tooltip("Variations de clip (tirage aléatoire).")]
        public AudioClip[] clips;

        [Range(0f, 1f)]
        public float volumeScale = 0.8f;

        public float pitchMin = 0.96f;
        public float pitchMax = 1.04f;

        public VoiceFamily voiceFamily = VoiceFamily.Statuts;

        [Tooltip("Cooldown anti-spam (ms) pour cet événement.")]
        public int cooldownMs = 100;

        // ═══════════════════════════════════════════
        // CAMÉRA / TEMPS
        // ═══════════════════════════════════════════
        public float shakeTrauma = 0f;
        public float hitstopMs = 0f;

        // ═══════════════════════════════════════════
        // RÉSERVÉS
        // ═══════════════════════════════════════════
        [Tooltip("Réservé F4.")]
        public HapticLevel haptic = HapticLevel.None;

        [Tooltip("Réservé F5 — accessibilité réduire mouvements.")]
        public bool respectsReduceMotion = true;

        // ═══════════════════════════════════════════
        // GOUVERNANCE
        // ═══════════════════════════════════════════
        [Tooltip("Emphase 1–6 (charte §3). ≥ 5 vole une voix / ignore budget FX.")]
        [Range(1, 6)]
        public int emphasis = 2;

        public bool HasSfx => clips != null && clips.Length > 0;
        public bool HasVfx => vfxPrefab != null;
    }
}
