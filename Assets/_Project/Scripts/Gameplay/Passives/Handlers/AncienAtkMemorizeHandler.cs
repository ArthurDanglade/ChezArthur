using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P2 ATK - Mémorisation des ennemis touchés (max 4 / étage).
    /// </summary>
    public class AncienAtkMemorizeHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            AncienSystem system = EnsureSystem(context);
            if (system != null)
                system.TryMemorizeEnemy(context.HitEnemy);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            AncienSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetForStage();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static AncienSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            AncienSystem system = context.Owner.GetComponent<AncienSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<AncienSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

