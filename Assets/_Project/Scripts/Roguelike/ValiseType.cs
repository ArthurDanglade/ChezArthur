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
        /// <summary> Bonus appliqué à ATK, DEF, HP et Vitesse (ex. Équilibre). </summary>
        AllStats,
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
    /// Mode d'affichage d'une valise dans l'encart de comparaison du sacrifice.
    /// FlatStats : lignes de stat chiffrées. EffectLine : une ligne d'effet rédigée (valises conditionnelles).
    /// </summary>
    public enum ValiseComparisonMode
    {
        FlatStats,
        EffectLine
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

        /// <summary>
        /// Libellé joueur d'un type de stat de valise.
        /// </summary>
        public static string GetStatLabel(ValiseStatType stat) => stat switch
        {
            ValiseStatType.ATK => "Attaque",
            ValiseStatType.DEF => "Défense",
            ValiseStatType.HP => "PV",
            ValiseStatType.Speed => "Vitesse",
            ValiseStatType.LaunchForce => "Force de lancer",
            ValiseStatType.ReboundDecay => "Rebond",
            ValiseStatType.CritChance => "Chance critique",
            ValiseStatType.CritMultiplier => "Dégâts critiques",
            ValiseStatType.DamageReduction => "Réduction de dégâts",
            ValiseStatType.HealingBonus => "Bonus de soins",
            ValiseStatType.RegenBetweenStages => "Régén. entre étages",
            ValiseStatType.AllStats => "Toutes stats",
            _ => ""
        };
    }
}
