using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Cause sémantique d'un feedback (couleur unique via CombatFeedbackPalette).
    /// </summary>
    public enum FeedbackCause
    {
        None,
        Heal,
        BuffUp,
        DebuffDown,
        Shield,
        Burn,
        Poison,
        Stun,
        Freeze
    }

    /// <summary>
    /// D12 — LA source des couleurs de cause des flottants de combat.
    /// Une cause = une couleur, définie ici et nulle part ailleurs.
    /// </summary>
    public static class CombatFeedbackPalette
    {
        /// <summary> Réévaluation au switch de spé (R4). </summary>
        public static readonly Color SpecSwitchReeval = new Color(180f / 255f, 140f / 255f, 1f, 1f); // #B48CFF

        /// <summary> Tick de Brûlure (R8) — aligné sur le style ShowBurn existant. </summary>
        public static readonly Color Burn = new Color(1f, 140f / 255f, 60f / 255f, 1f); // #FF8C3C

        /// <summary> Transfert du Lien de Confession (G6b — réservé). </summary>
        public static readonly Color LienTransfert = new Color(192f / 255f, 90f / 255f, 120f / 255f, 1f); // #C05A78

        /// <summary> Rétro-soin du Confesseur (G6b — réservé). </summary>
        public static readonly Color LienRetroSoin = new Color(124f / 255f, 191f / 255f, 124f / 255f, 1f); // #7CBF7C

        /// <summary> Renvoi de la Chaîne Tournante (G6c — réservé). </summary>
        public static readonly Color ChaineRenvoi = new Color(159f / 255f, 180f / 255f, 199f / 255f, 1f); // #9FB4C7

        /// <summary> Buff de stat (charte §2). </summary>
        public static readonly Color BuffUp = new Color(102f / 255f, 184f / 255f, 1f, 1f); // #66B8FF

        /// <summary> Debuff de stat (charte §2). </summary>
        public static readonly Color DebuffDown = new Color(180f / 255f, 77f / 255f, 230f / 255f, 1f); // #B44DE6

        /// <summary> Bouclier (charte §2). </summary>
        public static readonly Color Shield = new Color(125f / 255f, 224f / 255f, 1f, 1f); // #7DE0FF

        /// <summary> Stun (charte §2). </summary>
        public static readonly Color Stun = new Color(1f, 224f / 255f, 102f / 255f, 1f); // #FFE066

        /// <summary> Gel (charte §2). </summary>
        public static readonly Color Freeze = new Color(174f / 255f, 233f / 255f, 1f, 1f); // #AEE9FF

        /// <summary> Soin (charte §2 / flottants existants). </summary>
        public static readonly Color Heal = new Color(77f / 255f, 1f, 102f / 255f, 1f); // #4DFF66

        /// <summary> Poison (charte §2 / flottants existants). </summary>
        public static readonly Color Poison = new Color(128f / 255f, 230f / 255f, 51f / 255f, 1f); // #80E633

        /// <summary>
        /// Couleur canonique pour une cause de feedback.
        /// </summary>
        public static Color GetColor(FeedbackCause cause)
        {
            switch (cause)
            {
                case FeedbackCause.Heal: return Heal;
                case FeedbackCause.BuffUp: return BuffUp;
                case FeedbackCause.DebuffDown: return DebuffDown;
                case FeedbackCause.Shield: return Shield;
                case FeedbackCause.Burn: return Burn;
                case FeedbackCause.Poison: return Poison;
                case FeedbackCause.Stun: return Stun;
                case FeedbackCause.Freeze: return Freeze;
                default: return Color.white;
            }
        }
    }
}
