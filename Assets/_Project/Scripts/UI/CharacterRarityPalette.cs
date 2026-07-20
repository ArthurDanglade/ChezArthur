using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Source unique d'ACCÈS aux couleurs de rareté personnage ; les valeurs
    /// vivent dans UiTheme (Rarity*). Toute utilisation d'une couleur de rareté
    /// personnage DOIT passer par cette classe.
    /// </summary>
    public static class CharacterRarityPalette
    {
        /// <summary> SR — bleu clair. </summary>
        public static readonly Color SR = UiTheme.RaritySR;
        /// <summary> SSR — or. </summary>
        public static readonly Color SSR = UiTheme.RaritySSR;
        /// <summary> LR — violet. </summary>
        public static readonly Color LR = UiTheme.RarityLR;

        /// <summary> Couleur associée à une rareté (blanc si inconnue). </summary>
        public static Color GetColor(CharacterRarity rarity) => rarity switch
        {
            CharacterRarity.SR => SR,
            CharacterRarity.SSR => SSR,
            CharacterRarity.LR => LR,
            _ => Color.white
        };
    }
}
