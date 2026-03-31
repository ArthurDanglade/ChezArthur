using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « C'est pas ta place » (spenda_teleport) :
    /// initialise le système et force un recalcul du marqueur en début d'étage.
    /// </summary>
    public class SpendaTeleportHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            SpendaTeleportSystem system = EnsureSystem(context);
            if (system != null)
                system.RefreshTeleportMarker();
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

