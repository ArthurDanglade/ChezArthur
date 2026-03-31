using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Ce qui est à toi est à moi" (shado_steal) :
    /// active le mode enhanced et tente le vol d'ATK sur OnHitEnemy.
    /// </summary>
    public class ShadoStealHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitEnemy == null) return;

            ShadoStealthSystem system = EnsureSystem(context);
            if (system != null)
                system.TryStealAtk(context.HitEnemy);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ShadoStealthSystem system = EnsureSystem(context);
            if (system != null)
                system.SetEnhanced(true);
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

