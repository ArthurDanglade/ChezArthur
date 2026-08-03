using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives.Handlers;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Famille de feedback pour un buff (classification F3 — pas de flag IsDebuff).
    /// </summary>
    public enum BuffFeedbackKind
    {
        None,
        Buff,
        Debuff,
        Shield,
        Burn,
        Poison
    }

    /// <summary>
    /// Règles de classification buff → événement feedback (groupe B).
    /// </summary>
    public static class FeedbackCauses
    {
        /// <summary>
        /// Classe un buff pour l'émission. Ordre : marqueurs exclus → DoT → Shield → debuff → buff.
        /// </summary>
        public static BuffFeedbackKind Classify(BuffData b)
        {
            if (b == null)
                return BuffFeedbackKind.None;

            string id = b.BuffId;
            if (id == StunSystem.StunBuffId
                || id == FreezeSystem.FreezeBuffId
                || id == PoisonTickSystem.CarrierBuffId)
                return BuffFeedbackKind.None;

            if (id == BurnTickSystem.KramBurnBuffId
                || id == BurnTickSystem.BouleDeFeuBurnBuffId)
                return BuffFeedbackKind.Burn;

            if (id == PoisonTickSystem.PoisonBuffId)
                return BuffFeedbackKind.Poison;

            if (b.StatType == BuffStatType.Shield)
                return BuffFeedbackKind.Shield;

            if (b.StatType == BuffStatType.MissChance
                || (b.StatType == BuffStatType.DamageAmplification && b.Value > 0f))
                return BuffFeedbackKind.Debuff;

            if (b.Value < 0f)
                return BuffFeedbackKind.Debuff;

            return BuffFeedbackKind.Buff;
        }
    }
}
