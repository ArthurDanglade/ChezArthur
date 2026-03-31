using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    public class BrookeDefArmorHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffDEF) return 0f;
            BrookeSystem system = EnsureSystem(context);
            return system != null ? system.GetArmorDefBonus() : 0f;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            BrookeSystem system = EnsureSystem(context);
            if (system == null) return;
            system.ResetDefStageState();
            system.ApplyArmorDR();
            system.SyncHighFiveBuff();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            BrookeSystem system = EnsureSystem(context);
            if (system != null)
            {
                system.HandleSpecSwitchState();
                system.ApplyArmorSwitchBonus();
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

