namespace ChezArthur.Enemies
{
    /// <summary>
    /// Rôle de l'ennemi pour le filtrage des pools de spawn.
    /// </summary>
    public enum EnemyRole
    {
        Basique,
        MiniBoss,
        Boss,
        /// <summary> Hors pools de spawn (D29) — compagnon / pièce recyclée. </summary>
        Compagnon
    }
}
