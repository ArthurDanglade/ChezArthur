using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Core;
using UnityEngine;

namespace ChezArthur.Missions
{
    /// <summary>
    /// Snapshot composition au lancement de run (spé principale Hub, jamais la spé combat).
    /// Option A : validation à OnStageReached si snapshot conforme.
    /// </summary>
    public sealed class MissionRunSnapshot
    {
        public bool IsValid { get; private set; }
        public bool MatchesFullSeasonRole { get; private set; }
        public bool AllSr { get; private set; }
        public bool SpecSwitchOccurred { get; private set; }
        public bool SynergyActivated { get; private set; }
        public CharacterRole SeasonRole { get; private set; }
        public int AllyCount { get; private set; }

        private readonly List<string> _characterIds = new List<string>(4);

        public IReadOnlyList<string> CharacterIds => _characterIds;

        public static MissionRunSnapshot CaptureFromHubTeam()
        {
            var snap = new MissionRunSnapshot();
            snap.SeasonRole = WeeklyMissionSchedule.GetCompositionRoleForCurrentWeek();

            PersistentManager pm = PersistentManager.Instance;
            if (pm == null || pm.Characters == null)
            {
                snap.IsValid = false;
                return snap;
            }

            List<(CharacterData data, OwnedCharacter owned)> team = pm.Characters.GetSelectedTeam();
            if (team == null || team.Count == 0)
            {
                snap.IsValid = false;
                return snap;
            }

            bool allRole = true;
            bool allSr = true;

            for (int i = 0; i < team.Count; i++)
            {
                CharacterData data = team[i].data;
                OwnedCharacter owned = team[i].owned;
                if (data == null || owned == null)
                {
                    allRole = false;
                    allSr = false;
                    continue;
                }

                snap._characterIds.Add(data.Id);

                if (data.Rarity != CharacterRarity.SR)
                    allSr = false;

                SpecializationData spec = pm.Characters.GetActiveSpecialization(data.Id);
                if (spec == null || spec.Role != snap.SeasonRole)
                    allRole = false;
            }

            snap.AllyCount = snap._characterIds.Count;
            snap.MatchesFullSeasonRole = allRole && snap.AllyCount > 0;
            snap.AllSr = allSr && snap.AllyCount > 0;
            snap.IsValid = true;
            return snap;
        }

        public void NotifySpecSwitch()
        {
            SpecSwitchOccurred = true;
        }

        public void NotifySynergyActivated()
        {
            SynergyActivated = true;
        }

        public bool HasCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return false;
            for (int i = 0; i < _characterIds.Count; i++)
            {
                if (_characterIds[i] == characterId)
                    return true;
            }
            return false;
        }
    }
}
