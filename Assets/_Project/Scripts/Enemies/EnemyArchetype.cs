namespace ChezArthur.Enemies
{
    /// <summary>
    /// Archétype de comportement (R2). Ne JAMAIS réordonner : sérialisé par int.
    /// Mobile = 0 = comportement historique (drag) — défaut de tous les assets existants.
    /// </summary>
    public enum EnemyArchetype
    {
        /// <summary> Se déplace en drag vers sa cible (historique). </summary>
        Mobile = 0,
        /// <summary> Ne se déplace pas : agit par patterns télégraphés (G6). </summary>
        Fixed = 1,
    }
}
