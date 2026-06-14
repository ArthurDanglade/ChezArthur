using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Champ de froid » (frigor_cold_field) : aura SPD −15 % + éclats de glace à la mort d'un ennemi gelé.
    /// </summary>
    public class FrigorColdFieldHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FrigorColdFieldSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyColdFieldToAllEnemies();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static FrigorColdFieldSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            FrigorColdFieldSystem system = context.Owner.GetComponent<FrigorColdFieldSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<FrigorColdFieldSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}
