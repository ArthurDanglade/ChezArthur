using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.UI;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Résultat d'un tirage gacha.
    /// </summary>
    [Serializable]
    public class GachaPullResult
    {
        public List<PulledCharacter> characters = new List<PulledCharacter>();
        public int talsSpent;
        public string bannerId;
    }

    /// <summary>
    /// Un personnage obtenu lors d'un tirage.
    /// </summary>
    [Serializable]
    public class PulledCharacter
    {
        public string characterId;
        public CharacterRarity rarity;
        public bool isNew;           // Nouveau personnage ou doublon ?
        public bool isRateUp;        // Est-ce le SSR rate up ?
        public int previousLevel;   // Niveau avant (si doublon)
        public int newLevel;         // Niveau après

        /// <summary>
        /// Texte de statut reveal / récap (respecte le plafond MAX_LEVEL).
        /// </summary>
        public string FormatStatusText()
        {
            if (isNew)
                return "NOUVEAU !";

            if (previousLevel >= CharacterData.MAX_LEVEL
                || newLevel <= previousLevel)
            {
                return "Nv." + CharacterData.MAX_LEVEL.ToString() + " MAX";
            }

            return "Nv." + previousLevel.ToString() + " → Nv." + newLevel.ToString();
        }

        /// <summary> Couleur de statut (tokens UiTheme — jamais de couleur en dur). </summary>
        public Color FormatStatusColor()
        {
            if (isNew)
                return UiTheme.BadgeNew;

            if (previousLevel >= CharacterData.MAX_LEVEL
                || newLevel <= previousLevel)
            {
                return UiTheme.TextMuted;
            }

            return UiTheme.Gold;
        }
    }
}
