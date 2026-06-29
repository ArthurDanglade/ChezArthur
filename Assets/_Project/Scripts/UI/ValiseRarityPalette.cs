using UnityEngine;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Source unique des couleurs et libellés de rareté de valise (puisés dans UiTheme).
    /// Utilisé partout où une valise doit s'afficher dans la couleur de sa dernière rareté prise.
    /// </summary>
    public static class ValiseRarityPalette
    {
        /// <summary> Couleur d'affichage selon la rareté d'amélioration. </summary>
        public static Color Color(ValiseImprovementRarity rarity) => rarity switch
        {
            ValiseImprovementRarity.Commune    => UiTheme.ValiseCommune,
            ValiseImprovementRarity.Rare       => UiTheme.ValiseRare,
            ValiseImprovementRarity.Epique     => UiTheme.ValiseEpique,
            ValiseImprovementRarity.Legendaire => UiTheme.ValiseLegendaire,
            _ => UiTheme.ValiseCommune
        };

        /// <summary> Libellé joueur de la rareté. </summary>
        public static string Label(ValiseImprovementRarity rarity) => rarity switch
        {
            ValiseImprovementRarity.Commune    => "Commune",
            ValiseImprovementRarity.Rare       => "Rare",
            ValiseImprovementRarity.Epique     => "Épique",
            ValiseImprovementRarity.Legendaire => "Légendaire",
            _ => ""
        };
    }
}
