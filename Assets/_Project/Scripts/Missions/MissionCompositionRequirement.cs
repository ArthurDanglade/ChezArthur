namespace ChezArthur.Missions
{
    /// <summary>
    /// Contrainte de composition pour une mission (évaluée via <see cref="MissionRunSnapshot"/>).
    /// </summary>
    public enum MissionCompositionRequirement
    {
        None = 0,

        /// <summary> Toute l'équipe = rôle de la semaine (ATK/DEF/SUP). </summary>
        FullSeasonRole = 1,

        /// <summary> Équipe uniquement SR. </summary>
        AllSr = 2,

        /// <summary> Aucun switch de spé pendant la run. </summary>
        NoSpecSwitch = 3,

        /// <summary> FullSeasonRole + aucun switch. </summary>
        FullSeasonRoleNoSwitch = 4
    }
}
