using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Source unique des couleurs de rareté des personnages (bordures,
    /// badges, textes). Toute utilisation d'une couleur de rareté
    /// personnage DOIT passer par cette classe.
    /// </summary>
    public static class CharacterRarityPalette
    {
        /// <summary> SR — bleu clair. </summary>
        public static readonly Color SR = new Color(0.6f, 0.8f, 1f);
        /// <summary> SSR — or. </summary>
        public static readonly Color SSR = new Color(1f, 0.84f, 0f);
        /// <summary> LR — violet. </summary>
        public static readonly Color LR = new Color(0.8f, 0.5f, 1f);

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
