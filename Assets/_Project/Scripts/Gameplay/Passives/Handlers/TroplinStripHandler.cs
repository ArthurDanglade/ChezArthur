using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P2 DEF - Strip DEF des ennemis ralentis.
    /// </summary>
    public class TroplinStripHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            TroplinSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetForStage();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            TroplinSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyStripSwitchBonus();
        }

        private static TroplinSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;
            TroplinSystem system = context.Owner.GetComponent<TroplinSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<TroplinSystem>();
            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

