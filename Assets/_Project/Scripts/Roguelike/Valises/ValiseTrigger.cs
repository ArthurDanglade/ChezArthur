namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Déclencheurs possibles pour les effets comportementaux de valises.
    /// </summary>
    public enum ValiseTrigger
    {
        OnAllyKill,
        OnAllyDeath,
        OnAllyTakeDamage,
        OnAllyDamagedByEnemy,
        OnAllyTurnStart,
        OnEnemyHit,
        OnCriticalHit,
        OnTalsChanged,
        OnItemSlotsChanged,
        OnValiseChanged,
        OnStageStart,
        OnRunStart,
        OnSuperLancer,
        OnNormalLaunch
    }
}
