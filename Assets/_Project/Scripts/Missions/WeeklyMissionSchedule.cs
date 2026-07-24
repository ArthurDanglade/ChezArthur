using ChezArthur.Characters;
using ChezArthur.Meta;

namespace ChezArthur.Missions
{
    /// <summary>
    /// Rôle demandé par la mission hebdo composition selon la semaine de saison (1–5).
    /// Sem.1 ATK, 2 DEF, 3 SUP, 4 ATK, 5 DEF.
    /// </summary>
    public static class WeeklyMissionSchedule
    {
        public static CharacterRole GetCompositionRoleForCurrentWeek()
        {
            return GetCompositionRoleForWeekIndex(SeasonRotationManager.CurrentWeekIndex);
        }

        public static CharacterRole GetCompositionRoleForWeekIndex(int weekIndex0To4)
        {
            switch (weekIndex0To4)
            {
                case 0: return CharacterRole.Attacker;
                case 1: return CharacterRole.Defender;
                case 2: return CharacterRole.Support;
                case 3: return CharacterRole.Attacker;
                default: return CharacterRole.Defender;
            }
        }

        public static string GetRoleDisplayName(CharacterRole role)
        {
            switch (role)
            {
                case CharacterRole.Attacker: return "ATK";
                case CharacterRole.Defender: return "DEF";
                case CharacterRole.Support: return "SUP";
                default: return role.ToString();
            }
        }
    }
}
