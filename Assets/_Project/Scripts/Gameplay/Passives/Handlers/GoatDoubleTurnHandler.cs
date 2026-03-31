using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Goat P12 - Premier allié touché après switch SUP rejoue une fois.
    /// 1 activation max par étage.
    /// </summary>
    public class GoatDoubleTurnHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            GoatSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetSupStageState();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            GoatSystem system = EnsureSystem(context);
            if (system != null)
                system.ArmDoubleTurnOnSupSwitch();
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

