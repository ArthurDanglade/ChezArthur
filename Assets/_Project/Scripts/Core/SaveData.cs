using System;
using System.Collections.Generic;
using ChezArthur.Characters;

namespace ChezArthur.Core
{
    /// <summary>
    /// Progression d'une mission (sérialisable JsonUtility).
    /// </summary>
    [Serializable]
    public class MissionProgressSaveEntry
    {
        public string missionId;
        public int currentValue;
        /// <summary> 0 = InProgress, 1 = Completed (claimable), 2 = Claimed. </summary>
        public int state;
        public bool invalidated;
    }

    /// <summary>
    /// Données du joueur à sauvegarder (sérialisable en JSON).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>
        /// Version du schéma de save. 0 = save antérieure au versioning
        /// (défaut de type sur vieux JSON). Stampé par SaveSystem.Save.
        /// </summary>
        public int saveVersion = 0;

        public string playerName = "Voyageur";
        public int tals = 0;
        public int bestStage = 0;
        public int bestSuperLancerHits = 0;

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

        // ── Meta / Missions (save v3+) ──────────────────────────────────────
        /// <summary> Dernier daily reset appliqué (GameClock id Paris). </summary>
        public string lastDailyResetId = "";

        /// <summary> Dernier weekly reset appliqué (lundi Paris). </summary>
        public string lastWeeklyResetId = "";

        /// <summary> Dernière saison appliquée (ex. "S1"). </summary>
        public string lastSeasonId = "";

        public List<MissionProgressSaveEntry> missionProgress = new List<MissionProgressSaveEntry>();

        /// <summary> Mode Boss Rush débloqué (permanent une fois true). </summary>
        public bool bossRushUnlocked;

        /// <summary> Roster Boss Rush (enemy ids), ordre = first-kill chronologique. </summary>
        public List<string> bossRushEnemyIds = new List<string>();

        /// <summary> Boss majeurs débloqués (sous-ensemble du roster, pour score / missions « N boss »). </summary>
        public List<string> bossRushMajorBossIds = new List<string>();

        /// <summary> Ids déjà comptés pour la mission hebdo Boss Rush (distincts, reset weekly). </summary>
        public List<string> bossRushWeeklyCountedIds = new List<string>();

        /// <summary> Score prestige de compte (monotone, jamais diminué). </summary>
        public int accountScore;
    }
}
