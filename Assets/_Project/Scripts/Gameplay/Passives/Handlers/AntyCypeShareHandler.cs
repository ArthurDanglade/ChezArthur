using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Dossier partagé" (antycype_share) :
    /// quand un allié touche Anty Cype, il reçoit un marqueur scanner consommable.
    /// </summary>
    public class AntyCypeShareHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null || context.HitAlly == null) return;

            AntyCypeScanSystem system = context.Owner.GetComponent<AntyCypeScanSystem>();
            if (system != null)
                system.MarkAllyAsScanner(context.HitAlly);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            AntyCypeScanSystem system = context.Owner.GetComponent<AntyCypeScanSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<AntyCypeScanSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            system.SetEnhanced(true);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

