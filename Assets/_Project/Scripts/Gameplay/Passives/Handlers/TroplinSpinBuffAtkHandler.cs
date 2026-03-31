using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P6 ATK - Buff allié au contact immobile + stacks ATK Troplin.
    /// </summary>
    public class TroplinSpinBuffAtkHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            TroplinSystem system = EnsureSystem(context);
            if (system != null)
                system.OnAllyHitTroplin(context.HitAlly);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffATK) return 0f;
            TroplinSystem system = EnsureSystem(context);
            return system != null ? system.GetAtkStackBonus() : 0f;
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

