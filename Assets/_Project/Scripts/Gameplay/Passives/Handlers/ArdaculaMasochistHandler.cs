using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P5 SUP - Masochiste : stacks ATK/DEF au dommage subi/sacrifice.
    /// </summary>
    public class ArdaculaMasochistHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ArdaculaSystem system = EnsureSystem(context);
            if (system != null)
                system.AddMasochistStack();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffATK && passiveData.Effect != PassiveEffect.BuffDEF)
                return 0f;

            ArdaculaSystem system = EnsureSystem(context);
            if (system == null) return 0f;
            return system.GetMasochistBonus();
        }

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
                system.ApplyMasochistSwitchBonus();
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

