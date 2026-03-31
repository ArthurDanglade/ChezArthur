using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P3 DEF - Buff allié au contact immobile + stacks HP max Troplin.
    /// </summary>
    public class TroplinSpinBuffDefHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            TroplinSystem system = EnsureSystem(context);
            if (system != null)
                system.OnAllyHitTroplin(context.HitAlly);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffHP) return 0f;
            TroplinSystem system = EnsureSystem(context);
            return system != null ? system.GetHpStackBonus() : 0f;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static TroplinSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;
            TroplinSystem system = context.Owner.GetComponent<TroplinSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<TroplinSystem>();
            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

