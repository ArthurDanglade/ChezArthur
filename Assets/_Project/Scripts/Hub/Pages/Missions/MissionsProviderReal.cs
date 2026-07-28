using System;
using System.Collections.Generic;
using ChezArthur.Missions;
using UnityEngine;

namespace ChezArthur.Hub.Pages.Missions
{
    /// <summary>
    /// Adaptateur réel IMissionProvider → MissionManager (gate 4.b, zéro mock).
    /// </summary>
    public sealed class MissionsProviderReal : IMissionProvider
    {
        // ═══════════════════════════════════════════
        // SINGLETON PARTAGÉ (page + badge nav)
        // ═══════════════════════════════════════════
        public static MissionsProviderReal Shared { get; } = new MissionsProviderReal();

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<MissionRuntimeEntry> _layerBuffer =
            new List<MissionRuntimeEntry>(32);
        private bool _subscribed;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action OnChanged;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// S'abonne à MissionManager.OnMissionsChanged (idempotent).
        /// </summary>
        public void EnsureBound()
        {
            MissionManager mm = MissionManager.Instance;
            if (mm == null)
                return;

            mm.EnsureInitialized();

            if (_subscribed)
                return;

            mm.OnMissionsChanged += HandleMissionsChanged;
            _subscribed = true;
        }

        public void GetMissions(MissionLayer layer, List<MissionUiEntry> results)
        {
            results.Clear();
            MissionManager mm = MissionManager.Instance;
            if (mm == null)
                return;

            mm.EnsureInitialized();
            mm.GetEntriesForLayer(layer, _layerBuffer);

            for (int i = 0; i < _layerBuffer.Count; i++)
            {
                MissionRuntimeEntry entry = _layerBuffer[i];
                if (entry == null || entry.Data == null)
                    continue;
                if (entry.Data.IsLayerBonus)
                    continue;
                if (entry.Invalidated && entry.State != MissionClaimState.Claimed)
                    continue;

                results.Add(ToUi(entry));
            }
        }

        public bool TryGetLayerBonus(MissionLayer layer, out MissionUiEntry bonus)
        {
            bonus = default;
            MissionManager mm = MissionManager.Instance;
            if (mm == null)
                return false;

            mm.EnsureInitialized();
            mm.GetEntriesForLayer(layer, _layerBuffer);

            for (int i = 0; i < _layerBuffer.Count; i++)
            {
                MissionRuntimeEntry entry = _layerBuffer[i];
                if (entry == null || entry.Data == null)
                    continue;
                if (!entry.Data.IsLayerBonus)
                    continue;
                if (entry.Invalidated && entry.State != MissionClaimState.Claimed)
                    continue;

                bonus = ToUi(entry);
                return true;
            }

            return false;
        }

        public bool HasAnyClaimable()
        {
            MissionManager mm = MissionManager.Instance;
            if (mm == null)
                return false;

            mm.EnsureInitialized();

            for (int layer = 0; layer < 4; layer++)
            {
                mm.GetEntriesForLayer((MissionLayer)layer, _layerBuffer);
                for (int i = 0; i < _layerBuffer.Count; i++)
                {
                    MissionRuntimeEntry entry = _layerBuffer[i];
                    if (entry != null && entry.IsClaimable)
                        return true;
                }
            }

            return false;
        }

        public bool TryClaim(string missionId)
        {
            MissionManager mm = MissionManager.Instance;
            if (mm == null || string.IsNullOrEmpty(missionId))
                return false;

            // TryClaim → AddTals → PersistentManager.OnDataChanged (header Tals, pas de double).
            return mm.TryClaim(missionId);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void HandleMissionsChanged()
        {
            OnChanged?.Invoke();
        }

        private static MissionUiEntry ToUi(MissionRuntimeEntry entry)
        {
            MissionData data = entry.Data;
            return new MissionUiEntry
            {
                Id = data.MissionId,
                Layer = data.Layer,
                Label = data.GetResolvedDisplayName(),
                Target = Mathf.Max(1, data.TargetValue),
                Progress = entry.CurrentValue,
                RewardTals = data.RewardTals,
                State = MapState(entry),
                IsLayerBonus = data.IsLayerBonus
            };
        }

        /// <summary>
        /// InProgress/Completed/Claimed → EN COURS/RÉCLAMABLE/RÉCLAMÉE.
        /// Locked non produit ici (réserve UI).
        /// </summary>
        private static MissionUiState MapState(MissionRuntimeEntry entry)
        {
            switch (entry.State)
            {
                case MissionClaimState.Completed:
                    return MissionUiState.Claimable;
                case MissionClaimState.Claimed:
                    return MissionUiState.Claimed;
                default:
                    return MissionUiState.InProgress;
            }
        }
    }
}
