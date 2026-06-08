namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Types de stats modifiables par les valises et certains items.
    /// </summary>
    public enum ValiseStatType
    {
        ATK,
        DEF,
        HP,
        Speed,
        LaunchForce,
        ReboundDecay,
        CritChance,
        CritMultiplier,
        DamageReduction,
        HealingBonus,
        RegenBetweenStages,
        None
    }

    /// <summary>
    /// Catégories fonctionnelles des valises.
    /// </summary>
    public enum ValiseCategory
    {
        Fondamentale,
        Offensive,
        Defensive,
        Scaling,
        Specs,
        Systemique
    }

    /// <summary>
    /// Raretés d'amélioration des valises.
    /// </summary>
    public enum ValiseImprovementRarity
    {
        Commune,
        Rare,
        Epique,
        Legendaire
    }

    /// <summary>
    /// Helpers utilitaires pour le système de valises.
    /// </summary>
    public static class ValiseTypeUtility
    {
        /// <summary>
        /// Retourne le bonus de niveaux appliqué selon la rareté d'amélioration.
        /// </summary>
        public static int GetLevelBonus(ValiseImprovementRarity rarity) => rarity switch
        {
            ValiseImprovementRarity.Commune => 1,
            ValiseImprovementRarity.Rare => 3,
            ValiseImprovementRarity.Epique => 7,
            ValiseImprovementRarity.Legendaire => 15,
            _ => 1
        };
    }
}
