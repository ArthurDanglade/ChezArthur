using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    public class MorreDecayHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            MorreVoeuxSystem s = Ensure(context);
            if (s != null) s.EnableDecay(true);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            MorreVoeuxSystem s = Ensure(context);
            if (s != null) s.EnableDecay(true);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            MorreVoeuxSystem s = Ensure(context);
            if (s != null) s.ApplyDecaySwitchBonus();
        }

        private static MorreVoeuxSystem Ensure(PassiveContext context)
        {
            if (context.Owner == null) return null;
            MorreVoeuxSystem s = context.Owner.GetComponent<MorreVoeuxSystem>();
            if (s == null) s = context.Owner.gameObject.AddComponent<MorreVoeuxSystem>();
            s.Initialize(context.Owner, context.TurnManager);
            return s;
        }
    }
}

