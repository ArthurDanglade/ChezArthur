using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Lien vital" (revvie_link) :
    /// quand Revvie prend des dégâts, applique le bonus DEF temporaire lié à l'allié marqué.
    /// </summary>
    public class RevvieLinkHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            RevvieRezSystem system = context.Owner.GetComponent<RevvieRezSystem>();
            if (system != null)
                system.OnRevvieTakeDamage();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            RevvieRezSystem system = context.Owner.GetComponent<RevvieRezSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<RevvieRezSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            system.SetEnhanced(true);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

