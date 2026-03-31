using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P5 ATK - Riposte des collègues vivants à chaque hit ennemi.
    /// </summary>
    public class DonHenchmenHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            DonCostardoSystem system = EnsureSystem(context);
            if (system != null)
                system.TriggerHenchmenAttack(context.HitEnemy);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            DonCostardoSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyHenchmenSwitchBonus();
        }

        private static DonCostardoSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            DonCostardoSystem system = context.Owner.GetComponent<DonCostardoSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<DonCostardoSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

