namespace ChezArthur.Characters
{
    /// <summary>
    /// Type d'effet appliqué par un passif en combat.
    /// </summary>
    public enum PassiveEffect
    {
        /// <summary>Augmente l'ATK en %.</summary>
        BuffATK,

        /// <summary>Augmente la DEF en %.</summary>
        BuffDEF,

        /// <summary>Augmente les HP max en %.</summary>
        BuffHP,

        /// <summary>Augmente la Speed en %.</summary>
        BuffSpeed,

        /// <summary>Augmente la force de lancement en %.</summary>
        BuffLaunchForce,

        /// <summary>Soigne ce personnage à chaque trigger.</summary>
        HealSelf,

        /// <summary>Soigne toute l'équipe à chaque trigger.</summary>
        HealTeam,

        /// <summary>Réduit les prochains dégâts reçus (bouclier).</summary>
        ShieldSelf,

        /// <summary>Augmente l'ATK de toute l'équipe en %.</summary>
        BuffTeamATK,

        /// <summary>Augmente la DEF de toute l'équipe en %.</summary>
        BuffTeamDEF,

        /// <summary>Réduit l'ATK des ennemis touchés.</summary>
        DebuffEnemyATK,

        /// <summary>Réduit la Speed des ennemis touchés.</summary>
        DebuffEnemySpeed
    }
}
