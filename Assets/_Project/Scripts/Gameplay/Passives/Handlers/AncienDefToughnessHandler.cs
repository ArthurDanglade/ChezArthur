using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P5 DEF - Toughness : +1% DEF par attaque reçue (cap 30).
    /// </summary>
    public class AncienDefToughnessHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            AncienSystem system = EnsureSystem(context);
            if (system != null)
                system.AddToughnessStack();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffDEF) return 0f;

            AncienSystem system = EnsureSystem(context);
            if (system == null) return 0f;
            return system.GetToughnessBonus();
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
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

