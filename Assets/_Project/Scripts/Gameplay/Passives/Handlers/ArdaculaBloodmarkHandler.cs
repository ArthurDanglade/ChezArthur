using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P2 ATK - Marque de sang (1 cible active, transfert à la mort).
    /// </summary>
    public class ArdaculaBloodmarkHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ArdaculaSystem system = EnsureSystem(context);
            if (system != null)
                system.TryApplyBloodMark(context.HitEnemy);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ArdaculaSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetForStage();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

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

