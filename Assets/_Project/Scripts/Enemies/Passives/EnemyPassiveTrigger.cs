namespace ChezArthur.Enemies.Passives
{
    /// <summary>
    /// Événements qui déclenchent un passif ennemi.
    /// </summary>
    public enum EnemyPassiveTrigger
    {
        /// <summary> Effet permanent, recalculé à chaque frame ou tick. </summary>
        Permanent,

        /// <summary> Début du tour de cet ennemi. </summary>
        OnTurnStart,

        /// <summary> Début d'un nouveau cycle (tous les participants ont joué). </summary>
        OnCycleStart,

        /// <summary> Début de l'étage. </summary>
        OnStageStart,

        /// <summary> Cet ennemi reçoit des dégâts. </summary>
        OnTakeDamage,

        /// <summary> Un allié (équipe du joueur) reçoit des dégâts. </summary>
        OnAllyDamaged,

        /// <summary> Un allié (équipe du joueur) reçoit un soin. </summary>
        OnAllyHealed,

        /// <summary> Un allié (équipe du joueur) meurt. </summary>
        OnAllyKilled,

        /// <summary> Un coéquipier (autre ennemi du même camp) meurt. </summary>
        OnMateKilled,

        /// <summary> N'importe quelle entité (allié ou ennemi) meurt. </summary>
        OnAnyEntityKilled,

        /// <summary> Cet ennemi tue un allié. </summary>
        OnKillAlly,

        /// <summary> Cet ennemi touche un allié pendant son lancer. </summary>
        OnHitAlly,

        /// <summary> Cet ennemi est touché par un allié pendant le lancer de l'allié. </summary>
        OnHitByAlly,

        /// <summary> Le bouclier de cet ennemi est réduit à 0. </summary>
        OnShieldBroken,

        /// <summary> Le bouclier de cet ennemi vient de se régénérer. </summary>
        OnShieldRegenerated,

        /// <summary> Les HP de cet ennemi passent sous un seuil défini. </summary>
        OnHpThreshold,

        /// <summary> Cet ennemi est le dernier coéquipier en vie. </summary>
        OnLastMateAlive,

        /// <summary> Cet ennemi attaque un allié d'un rôle spécifique. </summary>
        OnSpecificRoleHit,
    }
}
