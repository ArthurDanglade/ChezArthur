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

        /// <summary>
        /// Hint onboarding équipe déjà vu (Gate 5.b) — disparaît après le premier ajout réussi.
        /// </summary>
        public bool hintTeamDragSeen;

        // ── Saison (save v4) — REMIS À ZÉRO à chaque rollover ──
        /// <summary> Id de saison de progression (ex. "S1"), distinct de lastSeasonId missions. </summary>
        public string seasonId = "";
        public int bestScoreThisSeason;
        public int bestStageThisSeason;
        /// <summary> Cran du meilleur score (multiplicateur). </summary>
        public float bestTierThisSeason = 1f;
        public int runsThisSeason;
        public List<int> claimedTiers = new List<int>();
        public int prestigeTiersClaimed;

        // ── Progression de COMPTE — JAMAIS touchée par un reset de saison ──
        /// <summary> Indices de cran débloqués (0 = x1 implicite ; liste vide = valide). </summary>
        public List<int> unlockedDifficulties = new List<int>();
        public long lastSeasonRolloverUtcTicks;
        /// <summary> Garde anti-recul d'horloge (ticks UTC). </summary>
        public long lastSeenUtcTicks;
        public SeasonRecapData pendingSeasonRecap = new SeasonRecapData();
        /// <summary> LR entrés au portail cumulatif (compte — jamais reset saison). </summary>
        public List<string> pastSeasonLrIds = new List<string>();
    }

    /// <summary>
    /// Récapitulatif de fin de saison (survit au reset du bloc saison).
    /// </summary>
    [Serializable]
    public class SeasonRecapData
    {
        public string seasonId = "";
        public int finalScore;
        public int bestStage;
        public float bestTier = 1f;
        public int runs;
        public int lastTierReached;
        public bool pending;
        public int pendingTals;
        public int pendingLrLevels;
        public string lrCharacterId = "";
        public bool rewardsCredited;
    }
}
