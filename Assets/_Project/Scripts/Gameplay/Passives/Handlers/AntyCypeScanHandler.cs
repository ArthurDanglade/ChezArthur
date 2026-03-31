using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Je t'ai vu venir" (antycype_scan) :
    /// scan direct quand Anty Cype touche un ennemi.
    /// </summary>
    public class AntyCypeScanHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            AntyCypeScanSystem system = EnsureSystem(context);
            if (system != null && context.HitEnemy != null)
                system.ScanEnemy(context.HitEnemy);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            AntyCypeScanSystem system = EnsureSystem(context);
            if (system != null)
                system.ResetForStage();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static AntyCypeScanSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            AntyCypeScanSystem system = context.Owner.GetComponent<AntyCypeScanSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<AntyCypeScanSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

