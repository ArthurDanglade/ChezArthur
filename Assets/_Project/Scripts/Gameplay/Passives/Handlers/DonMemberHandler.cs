using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P2 SUP - Membre de la Familia (+ATK/+DEF sur allié touché).
    /// </summary>
    public class DonMemberHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            DonCostardoSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyMemberOnAllyHit(context.HitAlly);
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
                system.ApplyMemberSwitchBonus();
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

