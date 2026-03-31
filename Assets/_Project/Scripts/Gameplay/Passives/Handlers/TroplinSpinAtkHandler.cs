using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P4 ATK - Toupie tranchante (contre-dégâts immobile).
    /// </summary>
    public class TroplinSpinAtkHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            TroplinSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplySpinAtkSwitchBonus();
        }

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

