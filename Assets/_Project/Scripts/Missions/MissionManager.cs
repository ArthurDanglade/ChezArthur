using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.BossRush;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Meta;

namespace ChezArthur.Missions
{
    /// <summary>
    /// Cœur du système de missions : progression, claim manuel, resets daily/weekly/season.
    /// Vit sur le même GameObject que PersistentManager (DontDestroyOnLoad).
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class MissionManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static MissionManager Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Données")]
        [SerializeField] private MissionCatalog catalog;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly Dictionary<string, MissionRuntimeEntry> _entries =
            new Dictionary<string, MissionRuntimeEntry>(64);
        private readonly List<MissionData> _layerBuffer = new List<MissionData>(32);
        private readonly List<string> _completedThisRunIds = new List<string>(16);
        private readonly HashSet<string> _completedThisRunSet = new HashSet<string>();
        private bool _initialized;
        private MissionTriggerRelay _relay;
        private MissionRunSnapshot _runSnapshot;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action<MissionData> OnMissionCompleted;
        public event Action<MissionData> OnMissionClaimed;
        public event Action OnMissionsChanged;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public MissionCatalog Catalog => catalog;
        public bool IsInitialized => _initialized;
        public IReadOnlyList<string> CompletedMissionIdsThisRun => _completedThisRunIds;
        public MissionRunSnapshot CurrentRunSnapshot => _runSnapshot;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (_relay != null)
                _relay.Teardown();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise catalogue + progression + resets + relay (idempotent).
        /// </summary>
        public void EnsureInitialized()
        {
            if (_initialized)
            {
                ApplyResetsIfNeeded();
                return;
            }

            if (catalog == null)
            {
                Debug.LogError("[MissionManager] Catalog non assigné.");
                return;
            }

            BuildEntriesFromCatalog();
            LoadProgressFromPersistent();
            ApplyResetsIfNeeded();
            EvaluateBestStageMissions();
            EvaluateLayerBonuses(MissionLayer.Daily);
            EvaluateLayerBonuses(MissionLayer.Weekly);
            EvaluateLayerBonuses(MissionLayer.Seasonal);
            EvaluateLayerBonuses(MissionLayer.Permanent);

            if (_relay == null)
                _relay = gameObject.GetComponent<MissionTriggerRelay>()
                         ?? gameObject.AddComponent<MissionTriggerRelay>();
            _relay.Bind(this);

            _initialized = true;
            SyncToPersistent(save: true);
            OnMissionsChanged?.Invoke();
        }

        /// <summary>
        /// Appelé au démarrage d'une run : reset bandeaux + snapshot composition Hub.
        /// </summary>
        public void NotifyRunStarted()
        {
            _completedThisRunIds.Clear();
            _completedThisRunSet.Clear();
            _runSnapshot = MissionRunSnapshot.CaptureFromHubTeam();
            Debug.Log(
                $"[MissionManager] Run snapshot — fullRole={_runSnapshot.MatchesFullSeasonRole} " +
                $"role={_runSnapshot.SeasonRole} allSr={_runSnapshot.AllSr} allies={_runSnapshot.AllyCount}");
        }

        /// <summary>
        /// Switch de spé combat détecté (invalide NoSpecSwitch).
        /// </summary>
        public void NotifySpecSwitchInCombat()
        {
            _runSnapshot?.NotifySpecSwitch();
        }

        /// <summary>
        /// Synergie activée (invalide missions anti-synergie futures).
        /// </summary>
        public void NotifySynergyActivated()
        {
            _runSnapshot?.NotifySynergyActivated();
        }

        public bool TryGetEntry(string missionId, out MissionRuntimeEntry entry)
        {
            return _entries.TryGetValue(missionId, out entry);
        }

        /// <summary>
        /// Remplit la liste des entrées d'une couche (tri catalogue).
        /// </summary>
        public void GetEntriesForLayer(MissionLayer layer, List<MissionRuntimeEntry> results)
        {
            results.Clear();
            if (catalog == null)
                return;

            catalog.CollectByLayer(layer, _layerBuffer);
            for (int i = 0; i < _layerBuffer.Count; i++)
            {
                MissionData data = _layerBuffer[i];
                if (data == null)
                    continue;
                if (_entries.TryGetValue(data.MissionId, out MissionRuntimeEntry entry))
                    results.Add(entry);
            }
        }

        /// <summary>
        /// Claim manuel : grant Tals + état Claimed.
        /// </summary>
        public bool TryClaim(string missionId)
        {
            EnsureInitialized();
            if (!_entries.TryGetValue(missionId, out MissionRuntimeEntry entry))
                return false;
            if (!entry.IsClaimable)
                return false;
            if (PersistentManager.Instance == null)
                return false;

            entry.State = MissionClaimState.Claimed;
            int reward = entry.Data != null ? entry.Data.RewardTals : 0;

            SyncToPersistent(save: false);
            if (reward > 0)
                PersistentManager.Instance.AddTals(reward);
            else
                PersistentManager.Instance.SaveGame();

            OnMissionClaimed?.Invoke(entry.Data);
            OnMissionsChanged?.Invoke();

            // Un claim peut débloquer un bonus de couche déjà Completed côté progress,
            // mais le bonus se valide à la complétion des autres — déjà géré.
            return true;
        }

        /// <summary>
        /// Incrémente les missions d'un trigger cumulatif (+amount).
        /// </summary>
        public void ReportCounter(MissionTriggerType trigger, int amount = 1)
        {
            if (amount <= 0 || !_initialized)
                return;

            bool any = false;
            foreach (KeyValuePair<string, MissionRuntimeEntry> pair in _entries)
            {
                MissionRuntimeEntry entry = pair.Value;
                MissionData data = entry.Data;
                if (data == null || data.TriggerType != trigger)
                    continue;
                if (data.IsLayerBonus)
                    continue;
                if (!CanReceiveProgress(entry))
                    continue;

                entry.CurrentValue += amount;
                if (entry.CurrentValue > data.TargetValue)
                    entry.CurrentValue = data.TargetValue;

                if (TryComplete(entry))
                    any = true;
                else
                    any = true;
            }

            if (any)
            {
                EvaluateLayerBonusesForTrigger(trigger);
                SyncToPersistent(save: true);
                OnMissionsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Seuil d'étage atteint (missions StageReached + composition Option A).
        /// </summary>
        public void ReportStageReached(int stage)
        {
            if (!_initialized || stage < 1)
                return;

            bool any = false;
            foreach (KeyValuePair<string, MissionRuntimeEntry> pair in _entries)
            {
                MissionRuntimeEntry entry = pair.Value;
                MissionData data = entry.Data;
                if (data == null || data.TriggerType != MissionTriggerType.StageReached)
                    continue;
                if (!CanReceiveProgress(entry))
                    continue;

                if (!PassesCompositionGate(data, stage))
                    continue;

                if (stage >= data.TargetValue)
                {
                    entry.CurrentValue = data.TargetValue;
                    if (TryComplete(entry))
                        any = true;
                }
                else if (stage > entry.CurrentValue)
                {
                    entry.CurrentValue = stage;
                    any = true;
                }
            }

            if (PersistentManager.Instance != null)
                any |= EvaluateBestStageMissionsInternal(PersistentManager.Instance.BestStage);

            if (any)
            {
                EvaluateLayerBonuses(MissionLayer.Daily);
                EvaluateLayerBonuses(MissionLayer.Weekly);
                EvaluateLayerBonuses(MissionLayer.Seasonal);
                EvaluateLayerBonuses(MissionLayer.Permanent);
                SyncToPersistent(save: true);
                OnMissionsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Univers logique terminé (boss final d'un bloc de 20 étages).
        /// </summary>
        public void ReportUniverseCompleted(int logicalUniverseId)
        {
            if (!_initialized || !UniverseIds.IsValid(logicalUniverseId))
                return;

            bool any = false;
            foreach (KeyValuePair<string, MissionRuntimeEntry> pair in _entries)
            {
                MissionRuntimeEntry entry = pair.Value;
                MissionData data = entry.Data;
                if (data == null || data.TriggerType != MissionTriggerType.UniverseCompleted)
                    continue;
                if (!CanReceiveProgress(entry))
                    continue;

                int requiredUniverse = data.UseSeasonSlot1Universe
                    ? SeasonRotationManager.GetCurrentUniverseAtSlot(0)
                    : logicalUniverseId;

                if (data.UseSeasonSlot1Universe && logicalUniverseId != requiredUniverse)
                    continue;

                entry.CurrentValue = data.TargetValue;
                if (TryComplete(entry))
                    any = true;
            }

            if (any)
            {
                EvaluateLayerBonuses(MissionLayer.Weekly);
                EvaluateLayerBonuses(MissionLayer.Seasonal);
                SyncToPersistent(save: true);
                OnMissionsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Personnage obtenu (gacha / autre) avec rareté.
        /// </summary>
        public void ReportCharacterObtained(CharacterRarity rarity)
        {
            if (!_initialized)
                return;

            bool any = false;
            foreach (KeyValuePair<string, MissionRuntimeEntry> pair in _entries)
            {
                MissionRuntimeEntry entry = pair.Value;
                MissionData data = entry.Data;
                if (data == null || data.TriggerType != MissionTriggerType.CharacterObtained)
                    continue;
                if (!CanReceiveProgress(entry))
                    continue;
                if (data.FilterByRarity && data.RequiredRarity != rarity)
                    continue;

                entry.CurrentValue = data.TargetValue;
                if (TryComplete(entry))
                    any = true;
            }

            if (any)
            {
                EvaluateLayerBonuses(MissionLayer.Permanent);
                SyncToPersistent(save: true);
                OnMissionsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Force un recalcul des resets (debug).
        /// </summary>
        public void DebugForceApplyResets()
        {
            ApplyResetsIfNeeded(forceLog: true);
            OnMissionsChanged?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorSetCatalog(MissionCatalog missionCatalog)
        {
            catalog = missionCatalog;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void BuildEntriesFromCatalog()
        {
            _entries.Clear();
            IReadOnlyList<MissionData> list = catalog.Missions;
            for (int i = 0; i < list.Count; i++)
            {
                MissionData data = list[i];
                if (data == null || string.IsNullOrEmpty(data.MissionId))
                    continue;

                if (_entries.ContainsKey(data.MissionId))
                {
                    Debug.LogWarning($"[MissionManager] Id dupliqué ignoré : {data.MissionId}");
                    continue;
                }

                _entries.Add(data.MissionId, new MissionRuntimeEntry
                {
                    Data = data,
                    CurrentValue = 0,
                    State = MissionClaimState.InProgress,
                    Invalidated = false
                });
            }
        }

        private void LoadProgressFromPersistent()
        {
            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
                return;

            IReadOnlyList<MissionProgressSaveEntry> saved = pm.MissionProgress;
            for (int i = 0; i < saved.Count; i++)
            {
                MissionProgressSaveEntry s = saved[i];
                if (s == null || string.IsNullOrEmpty(s.missionId))
                    continue;
                if (!_entries.TryGetValue(s.missionId, out MissionRuntimeEntry entry))
                    continue;

                entry.CurrentValue = s.currentValue;
                entry.State = (MissionClaimState)s.state;
                entry.Invalidated = s.invalidated;
            }
        }

        private void SyncToPersistent(bool save)
        {
            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
                return;

            List<MissionProgressSaveEntry> list = new List<MissionProgressSaveEntry>(_entries.Count);
            foreach (KeyValuePair<string, MissionRuntimeEntry> pair in _entries)
            {
                MissionRuntimeEntry e = pair.Value;
                list.Add(new MissionProgressSaveEntry
                {
                    missionId = pair.Key,
                    currentValue = e.CurrentValue,
                    state = (int)e.State,
                    invalidated = e.Invalidated
                });
            }

            pm.SetMissionProgress(list);

            if (save)
                pm.SaveGame();
        }

        private void ApplyResetsIfNeeded(bool forceLog = false)
        {
            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
                return;

            string dailyId = GameClock.GetDailyResetId();
            string weeklyId = GameClock.GetWeeklyResetId();
            string seasonId = SeasonRotationManager.CurrentSeasonId;

            bool dirty = false;

            if (string.IsNullOrEmpty(pm.LastDailyResetId) || pm.LastDailyResetId != dailyId)
            {
                ResetLayer(MissionLayer.Daily);
                dirty = true;
                if (forceLog)
                    Debug.Log($"[MissionManager] Reset daily → {dailyId}");
            }

            if (string.IsNullOrEmpty(pm.LastWeeklyResetId) || pm.LastWeeklyResetId != weeklyId)
            {
                ResetLayer(MissionLayer.Weekly);
                BossRushManager.Instance?.ClearWeeklyDistinctKills();
                dirty = true;
                if (forceLog)
                    Debug.Log($"[MissionManager] Reset weekly → {weeklyId}");
            }

            if (string.IsNullOrEmpty(pm.LastSeasonId) || pm.LastSeasonId != seasonId)
            {
                ResetLayer(MissionLayer.Seasonal);
                dirty = true;
                if (forceLog)
                    Debug.Log($"[MissionManager] Reset season → {seasonId}");
            }

            if (dirty)
            {
                pm.SetResetIds(dailyId, weeklyId, seasonId);
                SyncToPersistent(save: true);
            }
            else if (string.IsNullOrEmpty(pm.LastDailyResetId))
            {
                // Premier lancement : stamp sans reset destructif (déjà InProgress).
                pm.SetResetIds(dailyId, weeklyId, seasonId);
                SyncToPersistent(save: true);
            }
        }

        private void ResetLayer(MissionLayer layer)
        {
            foreach (KeyValuePair<string, MissionRuntimeEntry> pair in _entries)
            {
                MissionRuntimeEntry entry = pair.Value;
                if (entry.Data == null || entry.Data.Layer != layer)
                    continue;

                entry.CurrentValue = 0;
                entry.State = MissionClaimState.InProgress;
                entry.Invalidated = false;
            }
        }

        private static bool CanReceiveProgress(MissionRuntimeEntry entry)
        {
            if (entry == null || entry.Data == null)
                return false;
            if (entry.Invalidated)
                return false;
            if (entry.State == MissionClaimState.Claimed)
                return false;
            if (entry.State == MissionClaimState.Completed)
                return false;
            return true;
        }

        /// <summary>
        /// Option A : composition Hub au lancement + trackers d'invalidation run.
        /// </summary>
        private bool PassesCompositionGate(MissionData data, int stage)
        {
            if (data == null || !data.HasCompositionRequirement)
                return true;

            if (data.MinStageForComposition > 0 && stage < data.MinStageForComposition)
                return false;

            if (_runSnapshot == null || !_runSnapshot.IsValid)
                return false;

            switch (data.CompositionRequirement)
            {
                case MissionCompositionRequirement.FullSeasonRole:
                    return _runSnapshot.MatchesFullSeasonRole;

                case MissionCompositionRequirement.AllSr:
                    return _runSnapshot.AllSr;

                case MissionCompositionRequirement.NoSpecSwitch:
                    return !_runSnapshot.SpecSwitchOccurred;

                case MissionCompositionRequirement.FullSeasonRoleNoSwitch:
                    return _runSnapshot.MatchesFullSeasonRole && !_runSnapshot.SpecSwitchOccurred;

                default:
                    return true;
            }
        }

        private bool TryComplete(MissionRuntimeEntry entry)
        {
            if (entry == null || entry.Data == null)
                return false;
            if (entry.State != MissionClaimState.InProgress)
                return false;
            if (entry.CurrentValue < entry.Data.TargetValue)
                return false;

            entry.State = MissionClaimState.Completed;
            entry.CurrentValue = entry.Data.TargetValue;

            if (_completedThisRunSet.Add(entry.Data.MissionId))
                _completedThisRunIds.Add(entry.Data.MissionId);

            OnMissionCompleted?.Invoke(entry.Data);
            Debug.Log($"[MissionManager] Complétée (claimable) : {entry.Data.MissionId}");
            return true;
        }

        private void EvaluateBestStageMissions()
        {
            if (PersistentManager.Instance == null)
                return;
            EvaluateBestStageMissionsInternal(PersistentManager.Instance.BestStage);
        }

        private bool EvaluateBestStageMissionsInternal(int bestStage)
        {
            bool any = false;
            foreach (KeyValuePair<string, MissionRuntimeEntry> pair in _entries)
            {
                MissionRuntimeEntry entry = pair.Value;
                MissionData data = entry.Data;
                if (data == null || data.TriggerType != MissionTriggerType.BestStageReached)
                    continue;
                if (!CanReceiveProgress(entry))
                    continue;

                if (bestStage >= data.TargetValue)
                {
                    entry.CurrentValue = data.TargetValue;
                    if (TryComplete(entry))
                        any = true;
                }
                else if (bestStage > entry.CurrentValue)
                {
                    entry.CurrentValue = bestStage;
                    any = true;
                }
            }

            return any;
        }

        private void EvaluateLayerBonusesForTrigger(MissionTriggerType trigger)
        {
            // Les compteurs daily/weekly touchent surtout Daily ; on évalue large.
            EvaluateLayerBonuses(MissionLayer.Daily);
            EvaluateLayerBonuses(MissionLayer.Weekly);
            EvaluateLayerBonuses(MissionLayer.Seasonal);
            EvaluateLayerBonuses(MissionLayer.Permanent);
        }

        private void EvaluateLayerBonuses(MissionLayer layer)
        {
            MissionRuntimeEntry bonusEntry = null;
            int required = 0;
            int done = 0;

            foreach (KeyValuePair<string, MissionRuntimeEntry> pair in _entries)
            {
                MissionRuntimeEntry entry = pair.Value;
                MissionData data = entry.Data;
                if (data == null || data.Layer != layer)
                    continue;

                if (data.IsLayerBonus)
                {
                    bonusEntry = entry;
                    continue;
                }

                required++;
                if (entry.State == MissionClaimState.Completed || entry.State == MissionClaimState.Claimed)
                    done++;
            }

            if (bonusEntry == null || !CanReceiveProgress(bonusEntry))
                return;

            bonusEntry.CurrentValue = done;
            // TargetValue du bonus = nombre de missions non-bonus de la couche (ou valeur SO).
            int need = bonusEntry.Data.TargetValue > 0 ? bonusEntry.Data.TargetValue : required;
            if (done >= need && required > 0)
            {
                bonusEntry.CurrentValue = need;
                TryComplete(bonusEntry);
            }
        }
    }
}
