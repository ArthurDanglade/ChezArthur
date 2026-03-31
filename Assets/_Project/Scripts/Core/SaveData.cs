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
        public int activePresetIndex = 0; // 0-4, preset actif
        public List<string> teamPreset0 = new List<string>();
        public List<string> teamPreset1 = new List<string>();
        public List<string> teamPreset2 = new List<string>();
        public List<string> teamPreset3 = new List<string>();
        public List<string> teamPreset4 = new List<string>();

        // Legacy (migration anciennes sauvegardes mono-équipe).
        public List<string> selectedTeamIds = new List<string>();

        // Pity gacha (deux listes pour compatibilité JsonUtility)
        public List<string> pityBannerIds = new List<string>();
        public List<int> pityCounts = new List<int>();
    }
}
