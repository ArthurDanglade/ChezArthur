using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Double dose" (tribulle_double) :
    /// active le mode amélioré de TribulleOrderSystem.
    /// </summary>
    public class TribulleDoubleHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            TribulleOrderSystem system = context.Owner.GetComponent<TribulleOrderSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<TribulleOrderSystem>();

            system.Initialize(context.Owner);
            system.SetEnhanced(true);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

