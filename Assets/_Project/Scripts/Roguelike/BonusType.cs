namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Type de stat affectée par un bonus.
    /// </summary>
    public enum BonusStatType
    {
        None,
        ATK,
        HP,
        Speed,
        LaunchForce,
        ReboundDecay,
        CritChance,
        CritMultiplier,
        DamageReduction,
        HealingBonus,
        TalsBonus,
        RegenBetweenStages
    }

    /// <summary>
    /// Rareté des bonus de stat.
    /// </summary>
    public enum BonusRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    /// <summary>
    /// Rareté des bonus spéciaux.
    /// </summary>
    public enum SpecialBonusRarity
    {
        Mid,
        Cool,
        Broken,
        Legendary
    }

    /// <summary>
    /// Catégorie de bonus (pour filtrage et affichage).
    /// </summary>
    public enum BonusCategory
    {
        Offensive,
        Defensive,
        Physical,
        Utility,
        Special
    }
}
