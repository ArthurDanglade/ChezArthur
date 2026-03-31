using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "T'as vu ? Non." (shado_stealth).
    /// Incrémente le compteur d'ennemis touchés pendant le lancer.
    /// </summary>
    public class ShadoStealthHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ShadoStealthSystem system = EnsureSystem(context);
            if (system != null)
                system.IncrementEnemyHitCount();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ShadoStealthSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetForStage();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static ShadoStealthSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            ShadoStealthSystem system = context.Owner.GetComponent<ShadoStealthSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<ShadoStealthSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

