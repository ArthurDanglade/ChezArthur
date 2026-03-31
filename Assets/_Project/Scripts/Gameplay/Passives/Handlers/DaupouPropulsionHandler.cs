using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Pousse-toi que je m'pousse" (daupou_propulsion).
    /// Le déclenchement réel vient de Enemy.OnCollisionEnter2D, ce handler initialise le système.
    /// </summary>
    public class DaupouPropulsionHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            DaupouPropulsionSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetState();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static DaupouPropulsionSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            DaupouPropulsionSystem system = context.Owner.GetComponent<DaupouPropulsionSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<DaupouPropulsionSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

