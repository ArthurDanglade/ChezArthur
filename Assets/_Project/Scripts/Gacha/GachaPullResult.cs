using System;
using System.Collections.Generic;
using ChezArthur.Characters;

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
    }
}
