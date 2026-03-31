using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P6 ATK - Salve déclenchée après 3 alliés différents touchés (V1 simplifiée).
    /// </summary>
    public class DonSalvoHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            DonCostardoSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetForStage();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            DonCostardoSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplySalvoSwitchBonus();
        }

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

