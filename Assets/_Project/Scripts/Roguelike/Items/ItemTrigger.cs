namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Déclencheurs possibles pour les effets d'items.
    /// </summary>
    public enum ItemTrigger
    {
        OnAllyKill,
        OnAllyDeath,
        OnAllyTakeDamage,
        OnAllyHeal,
        OnAllyLaunch,
        OnEnemyDeath,
        OnEnemyHit,
        OnWallBounce,
        OnStageStart,
        OnStageClear,
        OnRunStart,
        OnItemAcquired,
        OnGameOver,
        OnCriticalHit
    }
}
