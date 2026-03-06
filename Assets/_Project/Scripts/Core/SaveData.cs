using System;
using System.Collections.Generic;
using ChezArthur.Characters;

namespace ChezArthur.Core
{
    /// <summary>
    /// Données du joueur à sauvegarder (sérialisable en JSON).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string playerName = "Voyageur";
        public int tals = 0;
        public int bestStage = 0;

        public List<OwnedCharacter> ownedCharacters = new List<OwnedCharacter>();
        public List<string> selectedTeamIds = new List<string>(); // Max 4
    }
}
