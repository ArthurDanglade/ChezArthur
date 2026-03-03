namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Type de milestone (étages 10, 20, 30...).
    /// </summary>
    public enum MilestoneType
    {
        /// <summary> 1 boss puissant au centre. </summary>
        BossClassic,

        /// <summary> 2 mini-boss espacés. </summary>
        MiniBossDuo,

        /// <summary> 8-10 ennemis faibles. </summary>
        Horde,

        /// <summary> Boss + modificateur de salle (futur, traité comme BossClassic pour l'instant). </summary>
        BossWithRoom
    }
}
