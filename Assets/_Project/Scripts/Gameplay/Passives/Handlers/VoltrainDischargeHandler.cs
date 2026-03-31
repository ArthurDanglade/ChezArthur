using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Court-circuit » (voltrain_discharge) :
    /// active la décharge de ElectricWallSystem (niveau 10).
    /// </summary>
    public class VoltrainDischargeHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            ElectricWallSystem system = context.Owner.GetComponent<ElectricWallSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<ElectricWallSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            system.SetEnhanced(true);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

