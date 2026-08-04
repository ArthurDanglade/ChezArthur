using UnityEngine;

namespace ChezArthur.UI.InvocationFlow
{
    /// <summary>
    /// Tuning du polish d'invocation (défauts = feel validé preview INV0).
    /// </summary>
    [CreateAssetMenu(
        fileName = "InvocationFlowConfig",
        menuName = "Chez Arthur/UI/Invocation Flow Config")]
    public class InvocationFlowConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════

        [Header("Voile")]
        [Range(0.4f, 1.4f)] public float veilDuration = 0.70f;
        [Range(8f, 26f)] public float veilCellSize = 14f;

        [Header("Apparition")]
        [Range(0.8f, 2.6f)] public float resolveDurationSR = 1.6f;
        [Range(1.4f, 3.4f)] public float resolveDurationSSR = 2.4f;
        [Range(0f, 0.6f)] public float lrResolveBonus = 0.2f;
        [Range(0f, 0.9f)] public float monteeDuration = 0.35f;
        [Range(0f, 1f)] public float punchIntensity = 0.7f;
        [Range(0f, 1f)] public float rarityGlowIntensity = 0.7f;

        [Header("Bandeau")]
        [Range(0.5f, 1.6f)] public float bannerFullDuration = 0.9f;
        [Range(0.2f, 0.9f)] public float bannerCompactDuration = 0.4f;

        [Header("Sons — slots INV3, null = silencieux")]
        public AudioClip veilWhooshClip;
        public AudioClip resolveTickClip;
        public AudioClip confirmSrClip;
        public AudioClip confirmSsrClip;
        public AudioClip confirmLrClip;
        public AudioClip xpClip;
        public AudioClip levelUpClip;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>Durée de résolution selon la rareté (LR = SSR + bonus).</summary>
        public float GetResolveDuration(ChezArthur.Characters.CharacterRarity rarity)
        {
            switch (rarity)
            {
                case ChezArthur.Characters.CharacterRarity.SR:
                    return resolveDurationSR;
                case ChezArthur.Characters.CharacterRarity.LR:
                    return resolveDurationSSR + lrResolveBonus;
                default:
                    return resolveDurationSSR;
            }
        }
    }
}
