using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Meilleur en freestyle" (daupou_freestyle) :
    /// active le mode enhanced du système de propulsion (ATK x2 pendant propulsion passive).
    /// </summary>
    public class DaupouFreestyleHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            DaupouPropulsionSystem system = context.Owner.GetComponent<DaupouPropulsionSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<DaupouPropulsionSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            system.SetEnhanced(true);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

