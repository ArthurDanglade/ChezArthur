using System;

namespace ChezArthur.Missions
{
    /// <summary>
    /// État runtime d'une mission (miroir save).
    /// </summary>
    [Serializable]
    public class MissionRuntimeEntry
    {
        public MissionData Data;
        public int CurrentValue;
        public MissionClaimState State;
        public bool Invalidated;

        public bool IsClaimable => State == MissionClaimState.Completed && !Invalidated;
        public bool IsDone => State == MissionClaimState.Claimed;
    }
}
