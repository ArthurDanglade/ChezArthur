using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Question d'ordre" (tribulle_order) :
    /// reset l'état de lancer à OnLaunch et assure l'initialisation du système.
    /// </summary>
    public class TribulleOrderHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            TribulleOrderSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetForLaunch();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static TribulleOrderSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            TribulleOrderSystem system = context.Owner.GetComponent<TribulleOrderSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<TribulleOrderSystem>();

            system.Initialize(context.Owner);
            return system;
        }
    }
}

