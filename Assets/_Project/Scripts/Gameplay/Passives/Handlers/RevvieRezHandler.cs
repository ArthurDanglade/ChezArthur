using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Pas sous ma garde" (revvie_rez) :
    /// initialise le système, reset l'état d'étage, puis place le marqueur.
    /// </summary>
    public class RevvieRezHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            RevvieRezSystem system = EnsureSystem(context);
            if (system == null) return;

            system.ResetForStage();
            system.RefreshRezMarker();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static RevvieRezSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            RevvieRezSystem system = context.Owner.GetComponent<RevvieRezSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<RevvieRezSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

