namespace ChezArthur.Characters
{
    /// <summary>
    /// Type de déclenchement d'un passif.
    /// </summary>
    public enum PassiveType
    {
        Permanent,      // Toujours actif
        OnTurnStart,   // Au début du tour
        OnTurnEnd,     // À la fin du tour
        OnHit,         // Quand le perso touche un ennemi
        OnKill,        // Quand le perso tue un ennemi
        OnDamageTaken, // Quand le perso prend des dégâts
        OnAllyHit,     // Quand un allié touche un ennemi
        OnBounce       // À chaque rebond
    }
}
