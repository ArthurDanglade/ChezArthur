using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P1 ATK - Ambidextre : bonus partagé de switch reward vers l'ATK.
    /// </summary>
    public class AncienAtkAmbidextreHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffATK) return 0f;

            AncienSystem system = EnsureSystem(context);
            if (system == null) return 0f;
            return system.GetSwitchRewardBonus();
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            AncienSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyAtkSwitchBonus();
        }

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

