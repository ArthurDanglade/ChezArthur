using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Goat P1 - Berserk de meute.
    /// - +5% LaunchForce par allié vivant
    /// - équipe full vivante => +10% ATK et +10% DEF
    /// - au switch sur la spé ATK => +5% LaunchForce (1 tour)
    /// </summary>
    public class GoatBerserkHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.TurnManager == null || context.Owner == null) return 0f;

            int aliveAllies = 0;
            var allies = context.TurnManager.GetAllies();
            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                {
                    CharacterBall ally = allies[i];
                    if (ally == null || ally == context.Owner || ally.IsDead) continue;
                    aliveAllies++;
                }
            }
            int aliveTeamCount = aliveAllies + 1; // owner inclus

            switch (passiveData.Effect)
            {
                case PassiveEffect.BuffLaunchForce:
                    return aliveTeamCount * 0.05f;
                case PassiveEffect.BuffATK:
                case PassiveEffect.BuffDEF:
                    return aliveTeamCount >= 4 ? 0.10f : 0f;
                default:
                    return 0f;
            }
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            GoatSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyBerserkSwitchBonus();
        }

        private static GoatSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            GoatSystem system = context.Owner.GetComponent<GoatSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<GoatSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

