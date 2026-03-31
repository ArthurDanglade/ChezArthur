using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Ça va retomber » (lanshimmer_lance).
    /// Le handler initialise le système ; la logique d'impact est dans <see cref="LanceImpactSystem"/>.
    /// </summary>
    public class LansHimmerLanceHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            LanceImpactSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetMarkedPosition();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static LanceImpactSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            LanceImpactSystem system = context.Owner.GetComponent<LanceImpactSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<LanceImpactSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

