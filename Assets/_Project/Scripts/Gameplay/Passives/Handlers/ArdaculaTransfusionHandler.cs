using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P6 SUP - Transfusion : pacte de groupe (V1 = DEF +10% + marqueur).
    /// </summary>
    public class ArdaculaTransfusionHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ArdaculaSystem system = EnsureSystem(context);
            if (system != null)
                system.OnAllyHitForTransfusion(context.HitAlly);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ArdaculaSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetForStage();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ArdaculaSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyTransfusionSwitchBonus();
        }

        private static ArdaculaSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            ArdaculaSystem system = context.Owner.GetComponent<ArdaculaSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<ArdaculaSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

