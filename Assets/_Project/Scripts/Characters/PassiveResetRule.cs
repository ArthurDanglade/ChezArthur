namespace ChezArthur.Characters
{
    /// <summary>
    /// Règle de réinitialisation des stacks d'un passif.
    /// </summary>
    public enum PassiveResetRule
    {
        /// <summary>Les stacks se remettent à 0 à chaque nouvel étage.</summary>
        ResetPerStage,

        /// <summary>Les stacks persistent toute la run ; une fois le max atteint, c'est fini.</summary>
        PermanentInRun,

        /// <summary>Reset quand le joueur change de spécialisation en salle spéciale.</summary>
        ResetOnSpecSwitch
    }
}
