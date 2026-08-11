using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gacha;
using ChezArthur.Meta;

namespace ChezArthur.Core
{
    /// <summary>
    /// Manager persistant entre les scènes. Stocke les données du joueur.
    /// </summary>
    public class PersistentManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static PersistentManager Instance { get; private set; }

        // ═══════════════════════════════════════════
        // DONNÉES JOUEUR
        // ═══════════════════════════════════════════
        [Header("Données joueur")]
        [SerializeField] private string playerName = "Voyageur";
        [SerializeField] private int tals = 0;
        [SerializeField] private int bestStage = 0;
        [SerializeField] private int bestSuperLancerHits = 0;

        [Header("Base de données")]
        [SerializeField] private CharacterDatabase characterDatabase;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private CharacterManager _characterManager;
        private GachaManager _gachaManager;

        private string _lastDailyResetId = "";
        private string _lastWeeklyResetId = "";
        private string _lastSeasonId = "";
        private readonly List<MissionProgressSaveEntry> _missionProgress = new List<MissionProgressSaveEntry>();
        private bool _bossRushUnlocked;
        private readonly List<string> _bossRushEnemyIds = new List<string>();
        private readonly List<string> _bossRushMajorBossIds = new List<string>();
        private readonly List<string> _bossRushWeeklyCountedIds = new List<string>();
        private int _accountScore;
        private bool _hintTeamDragSeen;
        private GameRunMode _pendingRunMode = GameRunMode.Normal;
        private int _pendingDifficultyIndex;
        private bool _hasPendingDifficulty;

        // Bloc saison (reset au rollover)
        private string _seasonId = "";
        private int _bestScoreThisSeason;
        private int _bestStageThisSeason;
        private float _bestTierThisSeason = 1f;
        private int _runsThisSeason;
        private readonly List<int> _claimedTiers = new List<int>();
        private int _prestigeTiersClaimed;

        // Bloc compte (jamais reset par rollover saison)
        private readonly List<int> _unlockedDifficulties = new List<int>();
        private long _lastSeasonRolloverUtcTicks;
        private long _lastSeenUtcTicks;
        private SeasonRecapData _pendingSeasonRecap = new SeasonRecapData();
        private readonly List<string> _pastSeasonLrIds = new List<string>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string PlayerName => playerName;
        public int Tals => tals;
        public int BestStage => bestStage;
        public int BestSuperLancerHits => bestSuperLancerHits;

        public string LastDailyResetId => _lastDailyResetId;
        public string LastWeeklyResetId => _lastWeeklyResetId;
        public string LastSeasonId => _lastSeasonId;
        public IReadOnlyList<MissionProgressSaveEntry> MissionProgress => _missionProgress;
        public bool BossRushUnlocked => _bossRushUnlocked;
        public IReadOnlyList<string> BossRushEnemyIds => _bossRushEnemyIds;
        public IReadOnlyList<string> BossRushMajorBossIds => _bossRushMajorBossIds;
        public IReadOnlyList<string> BossRushWeeklyCountedIds => _bossRushWeeklyCountedIds;
        public int AccountScore => _accountScore;
        public bool HintTeamDragSeen => _hintTeamDragSeen;

        public string SeasonId => _seasonId;
        public int BestScoreThisSeason => _bestScoreThisSeason;
        public int BestStageThisSeason => _bestStageThisSeason;
        public float BestTierThisSeason => _bestTierThisSeason;
        public int RunsThisSeason => _runsThisSeason;
        public IReadOnlyList<int> ClaimedTiers => _claimedTiers;
        public int PrestigeTiersClaimed => _prestigeTiersClaimed;
        public IReadOnlyList<int> UnlockedDifficulties => _unlockedDifficulties;
        public long LastSeasonRolloverUtcTicks => _lastSeasonRolloverUtcTicks;
        public long LastSeenUtcTicks => _lastSeenUtcTicks;
        public SeasonRecapData PendingSeasonRecap => _pendingSeasonRecap;
        public IReadOnlyList<string> PastSeasonLrIds => _pastSeasonLrIds;

        /// <summary> Mode de run à consommer au prochain LoadGame / StartRun. </summary>
        public GameRunMode PendingRunMode => _pendingRunMode;

        /// <summary>
        /// Accès au gestionnaire de personnages.
        /// </summary>
        public CharacterManager Characters => _characterManager;

        /// <summary>
        /// Accès au gestionnaire gacha.
        /// </summary>
        public GachaManager Gacha => _gachaManager;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand les données joueur sont modifiées. </summary>
        public event Action OnDataChanged;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            // Singleton pattern avec DontDestroyOnLoad
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialise le CharacterManager
            if (characterDatabase != null)
            {
                _characterManager = new CharacterManager(characterDatabase);
            }
            else
            {
                Debug.LogError("[PersistentManager] CharacterDatabase non assignée !");
            }

            _gachaManager = new GachaManager();

            LoadGame();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Définit le pseudo du joueur.
        /// </summary>
        public void SetPlayerName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                playerName = name;
                SaveGame();
                OnDataChanged?.Invoke();
            }
        }

        /// <summary>
        /// Ajoute des Tals au joueur.
        /// </summary>
        public void AddTals(int amount)
        {
            if (amount > 0)
            {
                tals += amount;
                SaveGame();
                OnDataChanged?.Invoke();
            }
        }

        /// <summary>
        /// Dépense des Tals si le joueur en a assez.
        /// </summary>
        /// <returns>True si la dépense a réussi, false sinon.</returns>
        public bool SpendTals(int amount)
        {
            if (amount <= 0 || tals < amount)
                return false;

            tals -= amount;
            SaveGame();
            OnDataChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Met à jour le meilleur étage si le nouveau est supérieur.
        /// </summary>
        public void UpdateBestStage(int stage)
        {
            if (stage > bestStage)
            {
                bestStage = stage;
                SaveGame();
                OnDataChanged?.Invoke();
            }
        }

        /// <summary>
        /// Sauvegarde le record de hits Super Lancer si supérieur au record actuel.
        /// </summary>
        /// <returns>True si nouveau record.</returns>
        public bool UpdateBestSuperLancerHits(int hits)
        {
            if (hits <= bestSuperLancerHits) return false;

            bestSuperLancerHits = hits;
            SaveGame();
            OnDataChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Réinitialise toutes les données (pour debug ou reset).
        /// </summary>
        public void ResetAllData()
        {
            playerName = "Voyageur";
            tals = 0;
            bestStage = 0;
            bestSuperLancerHits = 0;
            _lastDailyResetId = "";
            _lastWeeklyResetId = "";
            _lastSeasonId = "";
            _missionProgress.Clear();
            _bossRushUnlocked = false;
            _bossRushEnemyIds.Clear();
            _bossRushMajorBossIds.Clear();
            _bossRushWeeklyCountedIds.Clear();
            _accountScore = 0;
            _pendingRunMode = GameRunMode.Normal;
            _pendingDifficultyIndex = 0;
            _hasPendingDifficulty = false;
            ResetSeasonFields();
            _unlockedDifficulties.Clear();
            _lastSeasonRolloverUtcTicks = 0;
            _lastSeenUtcTicks = 0;
            _pendingSeasonRecap = new SeasonRecapData();
            _pastSeasonLrIds.Clear();
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Améliore le score de saison si le nouveau est supérieur. Persiste immédiatement.
        /// </summary>
        /// <returns>True si le score a été amélioré.</returns>
        public bool TryImproveSeasonScore(int score, int stage, float tier)
        {
            if (score <= _bestScoreThisSeason)
                return false;

            _bestScoreThisSeason = score;
            _bestStageThisSeason = stage;
            _bestTierThisSeason = tier;
            SaveGame();
            OnDataChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Incrémente le compteur de runs de la saison courante.
        /// </summary>
        public void IncrementSeasonRuns()
        {
            _runsThisSeason++;
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Applique le rollover : écrit le récap (pending), reset du bloc saison uniquement,
        /// pose le nouvel id et le timestamp de rollover.
        /// </summary>
        public void ApplySeasonRollover(string newSeasonId, SeasonRecapData recap)
        {
            _pendingSeasonRecap = CloneRecap(recap);
            if (_pendingSeasonRecap == null)
                _pendingSeasonRecap = new SeasonRecapData();
            _pendingSeasonRecap.pending = true;

            ResetSeasonFields();
            _seasonId = newSeasonId ?? "";
            _lastSeasonRolloverUtcTicks = GameClock.UtcNowGuarded.Ticks;
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Pose l'id de saison de progression (premier boot / sans rollover).
        /// </summary>
        public void SetSeasonId(string seasonId)
        {
            _seasonId = seasonId ?? "";
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Marque le récapitulatif de fin de saison comme affiché.
        /// </summary>
        public void MarkRecapShown()
        {
            if (_pendingSeasonRecap == null)
                _pendingSeasonRecap = new SeasonRecapData();
            _pendingSeasonRecap.pending = false;
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Marque les récompenses du récap comme déjà versées (anti double-crédit).
        /// </summary>
        public void MarkRecapRewardsCredited()
        {
            if (_pendingSeasonRecap == null)
                _pendingSeasonRecap = new SeasonRecapData();
            _pendingSeasonRecap.rewardsCredited = true;
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Enregistre un claim de palier (données pures — éligibilité dans SeasonRewards).
        /// </summary>
        public bool TryClaimSeasonTier(int tierIndex)
        {
            for (int i = 0; i < _claimedTiers.Count; i++)
            {
                if (_claimedTiers[i] == tierIndex)
                    return false;
            }

            _claimedTiers.Add(tierIndex);
            SaveGame();
            OnDataChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Incrémente le compteur de prestige réclamé.
        /// </summary>
        public void IncrementPrestigeClaimed(int count)
        {
            if (count <= 0)
                return;
            _prestigeTiersClaimed += count;
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Ajoute un LR au portail cumulatif (idempotent).
        /// </summary>
        public void AddPastSeasonLr(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return;

            for (int i = 0; i < _pastSeasonLrIds.Count; i++)
            {
                if (_pastSeasonLrIds[i] == characterId)
                    return;
            }

            _pastSeasonLrIds.Add(characterId);
            SaveGame();
            OnDataChanged?.Invoke();
            Debug.Log($"[Season] LR portail cumulatif : +{characterId}");
        }

        /// <summary>
        /// Met à jour le plancher anti-recul (ticks UTC).
        /// </summary>
        public void SetLastSeenUtcTicks(long ticks)
        {
            if (ticks <= _lastSeenUtcTicks)
                return;
            _lastSeenUtcTicks = ticks;
            SaveGame();
        }

        private void ResetSeasonFields()
        {
            _seasonId = "";
            _bestScoreThisSeason = 0;
            _bestStageThisSeason = 0;
            _bestTierThisSeason = 1f;
            _runsThisSeason = 0;
            _claimedTiers.Clear();
            _prestigeTiersClaimed = 0;
        }

        /// <summary>
        /// Définit le mode de la prochaine run (Hub → Game).
        /// </summary>
        public void SetPendingRunMode(GameRunMode mode)
        {
            _pendingRunMode = mode;
        }

        /// <summary>
        /// Lit et remet le mode pending à Normal.
        /// </summary>
        public GameRunMode ConsumePendingRunMode()
        {
            GameRunMode mode = _pendingRunMode;
            _pendingRunMode = GameRunMode.Normal;
            return mode;
        }

        /// <summary>
        /// Pose le cran choisi pour la prochaine run (non sauvegardé).
        /// </summary>
        public void SetPendingDifficulty(int index)
        {
            _pendingDifficultyIndex = Mathf.Max(0, index);
            _hasPendingDifficulty = true;
        }

        /// <summary>
        /// Consomme le cran pending (défaut 0 / ×1). Multiplicateur via DifficultyConfig.
        /// </summary>
        public (int index, float multiplier) ConsumePendingDifficulty()
        {
            int index = _hasPendingDifficulty ? _pendingDifficultyIndex : 0;
            _hasPendingDifficulty = false;
            _pendingDifficultyIndex = 0;

            DifficultyConfig config = DifficultyConfig.LoadDefault();
            if (config != null)
            {
                index = Mathf.Clamp(index, 0, Mathf.Max(0, config.TierCount - 1));
                return (index, config.GetMultiplier(index));
            }

            return (0, 1f);
        }

        /// <summary>
        /// True si le cran est débloqué (index ≤ 0 = x1 toujours).
        /// </summary>
        public bool IsDifficultyUnlocked(int index)
        {
            if (index <= 0)
                return true;

            for (int i = 0; i < _unlockedDifficulties.Count; i++)
            {
                if (_unlockedDifficulties[i] == index)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Débloque un cran de compte (persistant). Idempotent.
        /// </summary>
        public void UnlockDifficulty(int index)
        {
            if (index <= 0)
                return;

            DifficultyConfig config = DifficultyConfig.LoadDefault();
            int maxIndex = config != null ? config.TierCount - 1 : 4;
            if (index > maxIndex)
                return;

            if (IsDifficultyUnlocked(index))
                return;

            _unlockedDifficulties.Add(index);
            SaveGame();
            OnDataChanged?.Invoke();
            string label = config != null ? config.GetLabel(index) : $"idx{index}";
            Debug.Log($"[Season] Cran débloqué : {label} (index {index})");
        }

        /// <summary>
        /// Debug : débloque tous les crans de la config.
        /// </summary>
        public void UnlockAllDifficulties()
        {
            DifficultyConfig config = DifficultyConfig.LoadDefault();
            int count = config != null ? config.TierCount : 5;
            for (int i = 1; i < count; i++)
                UnlockDifficulty(i);
        }

        /// <summary>
        /// Debug : remet les crans de compte à zéro (x1 seul).
        /// </summary>
        public void ResetUnlockedDifficulties()
        {
            _unlockedDifficulties.Clear();
            SaveGame();
            OnDataChanged?.Invoke();
            Debug.Log("[Season] Crans de compte reset (x1 seul).");
        }

        /// <summary>
        /// Remplace le blob de progression missions (appelé par MissionManager).
        /// </summary>
        public void SetMissionProgress(List<MissionProgressSaveEntry> entries)
        {
            _missionProgress.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null)
                        _missionProgress.Add(entries[i]);
                }
            }
        }

        /// <summary>
        /// Met à jour les ids de reset clock (daily / weekly / season).
        /// </summary>
        public void SetResetIds(string dailyId, string weeklyId, string seasonId)
        {
            _lastDailyResetId = dailyId ?? "";
            _lastWeeklyResetId = weeklyId ?? "";
            _lastSeasonId = seasonId ?? "";
        }

        /// <summary>
        /// Débloque le Boss Rush de façon permanente.
        /// </summary>
        public void UnlockBossRush()
        {
            if (_bossRushUnlocked)
                return;

            _bossRushUnlocked = true;
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Debug / QA : remet Boss Rush à zéro (verrouillé, roster vide).
        /// </summary>
        public void ClearBossRushProgress()
        {
            _bossRushUnlocked = false;
            _bossRushEnemyIds.Clear();
            _bossRushMajorBossIds.Clear();
            _bossRushWeeklyCountedIds.Clear();
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Remplace le roster Boss Rush (ordre first-kill) et la liste des majeurs.
        /// </summary>
        public void SetBossRushRoster(List<string> enemyIds, List<string> majorBossIds)
        {
            _bossRushEnemyIds.Clear();
            _bossRushMajorBossIds.Clear();

            if (enemyIds != null)
            {
                for (int i = 0; i < enemyIds.Count; i++)
                {
                    if (!string.IsNullOrEmpty(enemyIds[i]))
                        _bossRushEnemyIds.Add(enemyIds[i]);
                }
            }

            if (majorBossIds != null)
            {
                for (int i = 0; i < majorBossIds.Count; i++)
                {
                    if (!string.IsNullOrEmpty(majorBossIds[i]))
                        _bossRushMajorBossIds.Add(majorBossIds[i]);
                }
            }
        }

        public void SetBossRushWeeklyCountedIds(List<string> ids)
        {
            _bossRushWeeklyCountedIds.Clear();
            if (ids == null)
                return;

            for (int i = 0; i < ids.Count; i++)
            {
                if (!string.IsNullOrEmpty(ids[i]))
                    _bossRushWeeklyCountedIds.Add(ids[i]);
            }
        }

        /// <summary>
        /// Met à jour le score de compte s'il est strictement supérieur (monotone).
        /// </summary>
        public void UpdateAccountScore(int score)
        {
            if (score <= _accountScore)
                return;

            _accountScore = score;
            SaveGame();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Marque le hint drag équipe comme vu (persisté).
        /// </summary>
        public void SetHintTeamDragSeen(bool seen)
        {
            if (_hintTeamDragSeen == seen)
                return;
            _hintTeamDragSeen = seen;
            SaveGame();
        }

        /// <summary>
        /// Sauvegarde les données du joueur sur le disque.
        /// </summary>
        public void SaveGame()
        {
            SaveData data = new SaveData
            {
                playerName = this.playerName,
                tals = this.tals,
                bestStage = this.bestStage,
                bestSuperLancerHits = this.bestSuperLancerHits,
                lastDailyResetId = _lastDailyResetId,
                lastWeeklyResetId = _lastWeeklyResetId,
                lastSeasonId = _lastSeasonId,
                missionProgress = new List<MissionProgressSaveEntry>(_missionProgress),
                bossRushUnlocked = _bossRushUnlocked,
                bossRushEnemyIds = new List<string>(_bossRushEnemyIds),
                bossRushMajorBossIds = new List<string>(_bossRushMajorBossIds),
                bossRushWeeklyCountedIds = new List<string>(_bossRushWeeklyCountedIds),
                accountScore = _accountScore,
                hintTeamDragSeen = _hintTeamDragSeen,
                seasonId = _seasonId,
                bestScoreThisSeason = _bestScoreThisSeason,
                bestStageThisSeason = _bestStageThisSeason,
                bestTierThisSeason = _bestTierThisSeason,
                runsThisSeason = _runsThisSeason,
                claimedTiers = new List<int>(_claimedTiers),
                prestigeTiersClaimed = _prestigeTiersClaimed,
                unlockedDifficulties = new List<int>(_unlockedDifficulties),
                lastSeasonRolloverUtcTicks = _lastSeasonRolloverUtcTicks,
                lastSeenUtcTicks = _lastSeenUtcTicks,
                pendingSeasonRecap = CloneRecap(_pendingSeasonRecap),
                pastSeasonLrIds = new List<string>(_pastSeasonLrIds)
            };

            // Sauvegarder les personnages
            if (_characterManager != null)
            {
                var (owned, activePresetIndex, teamPreset0, teamPreset1, teamPreset2, teamPreset3, teamPreset4) = _characterManager.GetSaveData();
                data.ownedCharacters = new List<OwnedCharacter>(owned);
                data.activePresetIndex = activePresetIndex;
                data.teamPreset0 = new List<string>(teamPreset0);
                data.teamPreset1 = new List<string>(teamPreset1);
                data.teamPreset2 = new List<string>(teamPreset2);
                data.teamPreset3 = new List<string>(teamPreset3);
                data.teamPreset4 = new List<string>(teamPreset4);
            }

            // Sauvegarder le pity gacha
            if (_gachaManager != null)
            {
                var pity = _gachaManager.GetPityData();
                data.pityBannerIds = new List<string>(pity.Keys);
                data.pityCounts = new List<int>(pity.Values);
            }

            SaveSystem.Save(data);
        }

        /// <summary>
        /// Charge les données du joueur depuis le disque.
        /// </summary>
        public void LoadGame()
        {
            SaveData data = SaveSystem.Load();
            playerName = data.playerName;
            tals = data.tals;
            bestStage = data.bestStage;
            bestSuperLancerHits = data.bestSuperLancerHits;

            _lastDailyResetId = data.lastDailyResetId ?? "";
            _lastWeeklyResetId = data.lastWeeklyResetId ?? "";
            _lastSeasonId = data.lastSeasonId ?? "";
            _bossRushUnlocked = data.bossRushUnlocked;
            _accountScore = data.accountScore;
            _hintTeamDragSeen = data.hintTeamDragSeen;

            _seasonId = data.seasonId ?? "";
            _bestScoreThisSeason = data.bestScoreThisSeason;
            _bestStageThisSeason = data.bestStageThisSeason;
            _bestTierThisSeason = data.bestTierThisSeason > 0f ? data.bestTierThisSeason : 1f;
            _runsThisSeason = data.runsThisSeason;
            _prestigeTiersClaimed = data.prestigeTiersClaimed;
            _claimedTiers.Clear();
            if (data.claimedTiers != null)
            {
                for (int i = 0; i < data.claimedTiers.Count; i++)
                    _claimedTiers.Add(data.claimedTiers[i]);
            }

            _unlockedDifficulties.Clear();
            if (data.unlockedDifficulties != null)
            {
                for (int i = 0; i < data.unlockedDifficulties.Count; i++)
                    _unlockedDifficulties.Add(data.unlockedDifficulties[i]);
            }

            _lastSeasonRolloverUtcTicks = data.lastSeasonRolloverUtcTicks;
            _lastSeenUtcTicks = data.lastSeenUtcTicks;
            _pendingSeasonRecap = CloneRecap(data.pendingSeasonRecap);

            _pastSeasonLrIds.Clear();
            if (data.pastSeasonLrIds != null)
            {
                for (int i = 0; i < data.pastSeasonLrIds.Count; i++)
                {
                    if (!string.IsNullOrEmpty(data.pastSeasonLrIds[i]))
                        _pastSeasonLrIds.Add(data.pastSeasonLrIds[i]);
                }
            }

            _missionProgress.Clear();
            if (data.missionProgress != null)
            {
                for (int i = 0; i < data.missionProgress.Count; i++)
                {
                    if (data.missionProgress[i] != null)
                        _missionProgress.Add(data.missionProgress[i]);
                }
            }

            _bossRushEnemyIds.Clear();
            if (data.bossRushEnemyIds != null)
            {
                for (int i = 0; i < data.bossRushEnemyIds.Count; i++)
                {
                    if (!string.IsNullOrEmpty(data.bossRushEnemyIds[i]))
                        _bossRushEnemyIds.Add(data.bossRushEnemyIds[i]);
                }
            }

            _bossRushMajorBossIds.Clear();
            if (data.bossRushMajorBossIds != null)
            {
                for (int i = 0; i < data.bossRushMajorBossIds.Count; i++)
                {
                    if (!string.IsNullOrEmpty(data.bossRushMajorBossIds[i]))
                        _bossRushMajorBossIds.Add(data.bossRushMajorBossIds[i]);
                }
            }

            _bossRushWeeklyCountedIds.Clear();
            if (data.bossRushWeeklyCountedIds != null)
            {
                for (int i = 0; i < data.bossRushWeeklyCountedIds.Count; i++)
                {
                    if (!string.IsNullOrEmpty(data.bossRushWeeklyCountedIds[i]))
                        _bossRushWeeklyCountedIds.Add(data.bossRushWeeklyCountedIds[i]);
                }
            }

            // Charger les personnages
            if (_characterManager != null)
            {
                _characterManager.LoadFromSaveData(
                    data.ownedCharacters,
                    data.activePresetIndex,
                    data.teamPreset0,
                    data.teamPreset1,
                    data.teamPreset2,
                    data.teamPreset3,
                    data.teamPreset4,
                    data.selectedTeamIds);
            }

            // Charger le pity gacha
            if (_gachaManager != null && data.pityBannerIds != null && data.pityCounts != null)
            {
                var pityDict = new Dictionary<string, int>();
                int count = Mathf.Min(data.pityBannerIds.Count, data.pityCounts.Count);
                for (int i = 0; i < count; i++)
                {
                    pityDict[data.pityBannerIds[i]] = data.pityCounts[i];
                }
                _gachaManager.LoadPityData(pityDict);
            }

            // Après tout le chargement en mémoire : nettoyer équipes puis sauver (sans écraser le pity chargé ci-dessus)
            if (_characterManager != null && _characterManager.SanitizeAllTeamPresets())
                SaveGame();
        }

        private static SeasonRecapData CloneRecap(SeasonRecapData source)
        {
            if (source == null)
                return new SeasonRecapData();

            return new SeasonRecapData
            {
                seasonId = source.seasonId ?? "",
                finalScore = source.finalScore,
                bestStage = source.bestStage,
                bestTier = source.bestTier > 0f ? source.bestTier : 1f,
                runs = source.runs,
                lastTierReached = source.lastTierReached,
                pending = source.pending,
                pendingTals = source.pendingTals,
                pendingLrLevels = source.pendingLrLevels,
                lrCharacterId = source.lrCharacterId ?? "",
                rewardsCredited = source.rewardsCredited
            };
        }
    }
}
