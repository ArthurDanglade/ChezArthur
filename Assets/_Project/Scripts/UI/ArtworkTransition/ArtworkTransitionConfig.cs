using UnityEngine;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>
    /// Tuning des transitions d'artwork SSR (défauts = feel validé preview AW0).
    /// </summary>
    [CreateAssetMenu(
        fileName = "ArtworkTransitionConfig",
        menuName = "Chez Arthur/UI/Artwork Transition Config")]
    public class ArtworkTransitionConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════

        [Header("Déchéance")]
        [Range(0.2f, 3f)] public float holdDuration = 1.00f;
        [Range(0.4f, 3f)] public float burnDuration = 1.45f;
        [Range(0.02f, 0.15f)] public float frontBand = 0.05f;
        [Range(0f, 400f)] public float emberRatePerSec = 170f;
        [Range(0f, 300f)] public float ashRatePerSec = 80f;

        [Header("Ascension")]
        [Range(2, 5)] public int pulseCount = 3;
        [Range(0.4f, 3f)] public float pulsePhaseDuration = 1.25f;
        [Range(0.3f, 3f)] public float reforgeDuration = 1.10f;
        [Range(0f, 1.5f)] public float rayIntensity = 0.80f;
        [Range(20, 400)] public int climaxBurstCount = 150;

        [Header("Global")]
        [Range(1f, 8f)] public float pixelSize = 1f;
        [Range(0f, 2f)] public float glowIntensity = 0.70f;
        [Range(0f, 2f)] public float shakeIntensity = 0.55f;
        [Range(0f, 1f)] public float dirWeight = 0.80f;
        [Range(0.5f, 4f)] public float noiseUvScale = 1.4f;
        public int noiseSeed = 1337;

        [Header("Sons (slots — null = silencieux, banque D2 en AW4)")]
        public AudioClip stingClip;
        public AudioClip shimmerLoopClip;
        public AudioClip igniteClip;
        public AudioClip crackleLoopClip;
        public AudioClip whooshDownClip;
        public AudioClip pulseClip;
        public AudioClip riserClip;
        public AudioClip climaxClip;
        public AudioClip reforgeLoopClip;
    }
}
