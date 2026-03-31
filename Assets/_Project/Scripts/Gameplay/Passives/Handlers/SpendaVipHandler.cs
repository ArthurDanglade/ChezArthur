using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Échange VIP » (spenda_vip) :
    /// prépare SpendaTeleportSystem pour l'échange manuel branché UI.
    /// </summary>
    public class SpendaVipHandler : ISpecialPassiveHandler
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

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static SpendaTeleportSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            SpendaTeleportSystem system = context.Owner.GetComponent<SpendaTeleportSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<SpendaTeleportSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

