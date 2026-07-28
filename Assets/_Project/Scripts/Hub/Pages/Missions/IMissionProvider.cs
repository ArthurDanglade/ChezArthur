using System;
using System.Collections.Generic;
using ChezArthur.Missions;

namespace ChezArthur.Hub.Pages.Missions
{
    /// <summary>
    /// État d'affichage d'une entrée mission (UI).
    /// Locked = réserve UI — non produit par le mapping InProgress/Completed/Claimed.
    /// </summary>
    public enum MissionUiState
    {
        InProgress = 0,
        Claimable = 1,
        Claimed = 2,
        Locked = 3
    }

    /// <summary>
    /// Vue plate pour MissionEntryUI (contrat doc v3).
    /// </summary>
    public struct MissionUiEntry
    {
        public string Id;
        public MissionLayer Layer;
        public string Label;
        public int Target;
        public int Progress;
        public int RewardTals;
        public MissionUiState State;
        public bool IsLayerBonus;
    }

    /// <summary>
    /// Source de données de la page Missions — adaptateur MissionManager (gate 4.b).
    /// </summary>
    public interface IMissionProvider
    {
        /// <summary> Notifie un refresh UI (progression / claim / reset). </summary>
        event Action OnChanged;

        /// <summary>
        /// Remplit <paramref name="results"/> avec les missions non-bonus de la couche.
        /// </summary>
        void GetMissions(MissionLayer layer, List<MissionUiEntry> results);

        /// <summary>
        /// Bonus de complétion de couche s'il existe ; sinon false (Permanent / Seasonal vide).
        /// </summary>
        bool TryGetLayerBonus(MissionLayer layer, out MissionUiEntry bonus);

        /// <summary> True s'il existe au moins une mission réclamable (toutes couches). </summary>
        bool HasAnyClaimable();

        /// <summary> Tente un claim ; true si réussi. </summary>
        bool TryClaim(string missionId);
    }
}
