namespace ChezArthur.Characters
{
    /// <summary>
    /// Type de déclenchement d'un passif en combat.
    /// </summary>
    public enum PassiveTrigger
    {
        /// <summary>Toujours actif, pas de trigger.</summary>
        Permanent,

        /// <summary>Quand ce perso touche un ennemi.</summary>
        OnHitEnemy,

        /// <summary>Quand ce perso tue un ennemi.</summary>
        OnKillEnemy,

        /// <summary>Quand ce perso prend des dégâts.</summary>
        OnTakeDamage,

        /// <summary>Quand un allié tue un ennemi.</summary>
        OnAllyKill,

        /// <summary>Quand un allié prend des dégâts.</summary>
        OnAllyTakeDamage,

        /// <summary>Au début du tour de ce perso.</summary>
        OnTurnStart,

        /// <summary>Au début d'un nouvel étage.</summary>
        OnStageStart,

        /// <summary>Quand ce perso est lancé.</summary>
        OnLaunch,

        /// <summary>Quand ce perso rebondit sur un mur.</summary>
        OnBounceWall,

        /// <summary>Quand ce perso rebondit sur un ennemi.</summary>
        OnBounceEnemy,

        /// <summary>Quand ce perso confirme un switch de spé (spé au lancer ≠ spé au début du tour).</summary>
        OnSpecSwitch
    }
}
