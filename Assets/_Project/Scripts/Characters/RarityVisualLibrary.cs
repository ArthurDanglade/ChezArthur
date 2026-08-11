using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Source unique des frames de badges de rareté (flipbook).
    /// Les couleurs restent dans CharacterRarityPalette / UiTheme (Cas B — zéro recouvrement).
    /// </summary>
    [CreateAssetMenu(
        fileName = "RarityVisualLibrary",
        menuName = "Chez Arthur/Rarity Visual Library")]
    public class RarityVisualLibrary : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // TYPES
        // ═══════════════════════════════════════════

        [System.Serializable]
        public class RarityVisuals
        {
            public Sprite[] badgeFrames;
            public int idleFrameIndex = 0;
            public float framesPerSecond = 10f;
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════

        [Header("Visuels par rareté (explicites — pas de tableau indexé)")]
        [SerializeField] private RarityVisuals srVisuals = new RarityVisuals();
        [SerializeField] private RarityVisuals ssrVisuals = new RarityVisuals();
        [SerializeField] private RarityVisuals lrVisuals = new RarityVisuals();

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private bool _warnedSr;
        private bool _warnedSsr;
        private bool _warnedLr;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Frames du flipbook (null si absentes). </summary>
        public Sprite[] GetBadgeFrames(CharacterRarity rarity)
        {
            RarityVisuals visuals = Resolve(rarity);
            if (visuals == null || visuals.badgeFrames == null || visuals.badgeFrames.Length == 0)
            {
                WarnOnce(rarity);
                return null;
            }

            return visuals.badgeFrames;
        }

        /// <summary> Frame idle (null-safe). </summary>
        public Sprite GetIdleFrame(CharacterRarity rarity)
        {
            Sprite[] frames = GetBadgeFrames(rarity);
            if (frames == null || frames.Length == 0)
                return null;

            RarityVisuals visuals = Resolve(rarity);
            int idle = visuals != null ? visuals.idleFrameIndex : 0;
            if (idle < 0 || idle >= frames.Length)
                idle = 0;

            return frames[idle];
        }

        /// <summary> Cadence flipbook (fps). </summary>
        public float GetFps(CharacterRarity rarity)
        {
            RarityVisuals visuals = Resolve(rarity);
            if (visuals == null || visuals.framesPerSecond <= 0f)
                return 10f;
            return visuals.framesPerSecond;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private RarityVisuals Resolve(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SR: return srVisuals;
                case CharacterRarity.SSR: return ssrVisuals;
                case CharacterRarity.LR: return lrVisuals;
                default: return null;
            }
        }

        private void WarnOnce(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SR:
                    if (_warnedSr) return;
                    _warnedSr = true;
                    break;
                case CharacterRarity.SSR:
                    if (_warnedSsr) return;
                    _warnedSsr = true;
                    break;
                case CharacterRarity.LR:
                    if (_warnedLr) return;
                    _warnedLr = true;
                    break;
                default:
                    return;
            }

            Debug.LogWarning(
                "[RarityVisualLibrary] Frames manquantes pour " + rarity
                + " — badge masqué (filet : bordure teintée palette).");
        }
    }
}
