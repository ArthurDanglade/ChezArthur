using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    public class MorreUndyingSupHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => Ensure(context);
        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;
        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => Ensure(context);
        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

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

