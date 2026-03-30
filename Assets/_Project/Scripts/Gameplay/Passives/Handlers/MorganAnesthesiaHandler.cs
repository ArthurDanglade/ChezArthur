using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Anesthésie générale » (morgan_anesthesia) : passif permanent ; la logique de soins est dans <see cref="MorganAnesthesiaSystem"/>.
    /// </summary>
    public class MorganAnesthesiaHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            MorganAnesthesiaSystem system = context.Owner.GetComponent<MorganAnesthesiaSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<MorganAnesthesiaSystem>();

            system.Initialize(context.Owner);
            system.ResetAll();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
