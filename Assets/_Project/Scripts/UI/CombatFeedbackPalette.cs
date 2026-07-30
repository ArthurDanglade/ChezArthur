using UnityEngine;

namespace ChezArthur.UI
{
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
    }
}
