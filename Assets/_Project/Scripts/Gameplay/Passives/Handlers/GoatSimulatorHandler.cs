using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Goat P3 - Simulateur de rebonds.
    /// +10% ATK par rebond mur avant le 1er ennemi touché (sur le lancer).
    /// Au switch sur spé ATK: rebonds mur +15% vélocité jusqu'au 1er ennemi.
    /// </summary>
    public class GoatSimulatorHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            GoatSystem system = EnsureSystem(context);
            if (system != null)
                system.RegisterWallBounce();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            GoatSystem system = EnsureSystem(context);
            if (system != null)
                system.ActivateSimulatorSwitchBoost();
        }

        private static GoatSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            GoatSystem system = context.Owner.GetComponent<GoatSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<GoatSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

