using System;

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

        // TODO: Ajouter plus tard
        // public List<string> unlockedCharacterIds;
        // public List<string> selectedTeamIds;
    }
}
