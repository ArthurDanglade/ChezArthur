using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P3 SUP - Offre irrésistible (corruption 1x par étage).
    /// </summary>
    public class DonCorruptHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            DonCostardoSystem system = EnsureSystem(context);
            if (system != null)
                system.TryCorruptOnTurnStart();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            DonCostardoSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetForStage();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static DonCostardoSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            DonCostardoSystem system = context.Owner.GetComponent<DonCostardoSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<DonCostardoSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

