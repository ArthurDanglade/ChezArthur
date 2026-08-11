using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.UI.RevealStage
{
    /// <summary>
    /// Tuning « Entrée en scène » — défauts = soft P1 (grille INVR4), freeze = INVR4.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RevealStageConfig",
        menuName = "Chez Arthur/UI/Reveal Stage Config")]
    public class RevealStageConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS — GLOBAL
        // ═══════════════════════════════════════════

        [Header("Global")]
        [Tooltip("Cellule dither (px art @ largeur réf. 360). Director : cellPx = ditherCell * rectW / 360.")]
        [Range(1f, 12f)] public float ditherCell = 4f;
        [Range(0f, 1f)] public float shadowLevel = 0.42f;
        [Range(0.02f, 0.3f)] public float cutDuration = 0.08f;
        [Range(0.02f, 0.4f)] public float frontSoft = 0.10f;
        [Range(0f, 1f)] public float vignette = 0.34f;
        [Range(0.05f, 1f)] public float exitDim = 0.28f;
        [Range(0f, 0.5f)] public float entryOverlap = 0.15f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS — PAR RARETÉ
        // ═══════════════════════════════════════════

        [Header("SR")]
        [Range(0.3f, 3f)] public float entrySR = 1.40f;
        [Range(1, 6)] public int pulsesSR = 2;
        [Range(0f, 1f)] public float holdSR = 0f;
        [Range(0.08f, 0.8f)] public float snapSR = 0.24f;
        [Range(0.1f, 1f)] public float lightMaxSR = 0.24f;
        [Range(0f, 0.2f)] public float punchSR = 0.045f;
        [Range(0, 256)] public int partsSR = 30;

        [Header("SSR")]
        [Range(0.3f, 3f)] public float entrySSR = 2.15f;
        [Range(1, 6)] public int pulsesSSR = 3;
        [Range(0f, 1f)] public float holdSSR = 0.28f;
        [Range(0.08f, 0.8f)] public float snapSSR = 0.30f;
        [Range(0.1f, 1f)] public float lightMaxSSR = 0.32f;
        [Range(0f, 0.2f)] public float punchSSR = 0.065f;
        [Range(0, 256)] public int partsSSR = 95;

        [Header("LR")]
        [Range(0.3f, 3f)] public float entryLR = 2.50f;
        [Range(1, 6)] public int pulsesLR = 4;
        [Range(0f, 1f)] public float holdLR = 0.38f;
        [Range(0.08f, 0.8f)] public float snapLR = 0.34f;
        [Range(0.1f, 1f)] public float lightMaxLR = 0.36f;
        [Range(0f, 0.2f)] public float punchLR = 0.075f;
        [Range(0, 256)] public int partsLR = 140;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS — FAKEOUT
        // ═══════════════════════════════════════════

        [Header("Fakeout")]
        [Range(0f, 1f)] public float fakeHold = 0.18f;
        [Range(0f, 0.5f)] public float fakeCutBonus = 0.14f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS — SONS (INVR3, null = silencieux)
        // ═══════════════════════════════════════════

        [Header("Sons (slots INVR3 — null = silencieux)")]
        public AudioClip entryRiserClip;
        public AudioClip snapSrClip;
        public AudioClip snapSsrClip;
        public AudioClip snapLrClip;
        public AudioClip stampClip;
        public AudioClip statTickClip;
        public AudioClip exitDimClip;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS — PRÉSENTATION
        // ═══════════════════════════════════════════

        [Header("Présentation")]
        [Range(0f, 1f)] public float nameDelay = 0.10f;
        [Range(0.05f, 1f)] public float nameDur = 0.25f;
        [Range(0f, 1.5f)] public float statusDelay = 1.15f;
        [Range(0.1f, 1.5f)] public float chipFill = 0.45f;
        [Range(0.02f, 0.5f)] public float tickStagger = 0.12f;

        // ═══════════════════════════════════════════
        // ACCESSEURS PAR RARETÉ
        // ═══════════════════════════════════════════

        public float GetEntry(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SR: return entrySR;
                case CharacterRarity.LR: return entryLR;
                default: return entrySSR;
            }
        }

        public int GetPulses(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SR: return pulsesSR;
                case CharacterRarity.LR: return pulsesLR;
                default: return pulsesSSR;
            }
        }

        public float GetHold(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SR: return holdSR;
                case CharacterRarity.LR: return holdLR;
                default: return holdSSR;
            }
        }

        public float GetSnap(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SR: return snapSR;
                case CharacterRarity.LR: return snapLR;
                default: return snapSSR;
            }
        }

        public float GetLightMax(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SR: return lightMaxSR;
                case CharacterRarity.LR: return lightMaxLR;
                default: return lightMaxSSR;
            }
        }

        public float GetPunch(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SR: return punchSR;
                case CharacterRarity.LR: return punchLR;
                default: return punchSSR;
            }
        }

        public int GetParts(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SR: return partsSR;
                case CharacterRarity.LR: return partsLR;
                default: return partsSSR;
            }
        }

        public AudioClip GetSnapClip(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SR: return snapSrClip;
                case CharacterRarity.LR: return snapLrClip;
                default: return snapSsrClip;
            }
        }
    }
}
