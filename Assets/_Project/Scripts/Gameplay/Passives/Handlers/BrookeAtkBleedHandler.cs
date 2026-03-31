using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    public class BrookeAtkBleedHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            BrookeSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyBleed(context.HitEnemy);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            BrookeSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetAtkStageState();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            BrookeSystem system = EnsureSystem(context);
            if (system != null)
            {
                system.HandleSpecSwitchState();
                system.ApplyBleedSwitchBonus();
            }
        }

        private static BrookeSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;
            BrookeSystem system = context.Owner.GetComponent<BrookeSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<BrookeSystem>();
            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

