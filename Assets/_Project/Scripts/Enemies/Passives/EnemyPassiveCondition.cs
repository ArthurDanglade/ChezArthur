namespace ChezArthur.Enemies.Passives
{
    /// <summary>
    /// Conditions optionnelles à vérifier avant d'appliquer un effet passif.
    /// </summary>
    public enum EnemyPassiveCondition
    {
        /// <summary> Pas de condition — effet toujours appliqué si trigger OK. </summary>
        None,

        /// <summary> HP de cet ennemi sous X% (conditionThreshold). </summary>
        SelfHpBelow,

        /// <summary> HP de cet ennemi au-dessus de X% (conditionThreshold). </summary>
        SelfHpAbove,

        /// <summary> HP de cet ennemi exactement à 100%. </summary>
        SelfHpFull,

        /// <summary> Au moins X coéquipiers en vie (conditionCount). </summary>
        MinMatesAlive,

        /// <summary> Exactement 0 coéquipier en vie (seul survivant). </summary>
        NoMatesAlive,

        /// <summary> Nombre de stacks internes atteint X (conditionCount). </summary>
        StacksReachedMax,

        /// <summary> L'allié attaqué est du rôle spécifié (conditionRole). </summary>
        TargetAllyRole,

        /// <summary> Au moins X alliés (équipe joueur) morts (conditionCount). </summary>
        MinAlliesKilled,

        /// <summary> Tous les alliés (équipe joueur) sont du même rôle. </summary>
        AllAlliesSameRole,

        /// <summary>
        /// L'équipe alliée contient exactement
        /// 1 Attaquant + 1 Défenseur + 1 Soutien.
        /// </summary>
        TeamHasAllThreeRoles,

        /// <summary> Cet ennemi vient de survivre à un coup fatal. </summary>
        SurvivedFatalBlow,

        /// <summary> La jauge spéciale interne est à 100%. </summary>
        SpecialGaugeFull,
    }
}
