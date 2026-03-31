using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Réparation express" (bouclar_repair) :
    /// recharge les boucliers des alliés touchés par Bouclar.
    /// </summary>
    public class BouclarRepairHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitAlly == null) return;
            if (context.Owner == null) return;

            BouclarShieldSystem system = context.Owner.GetComponent<BouclarShieldSystem>();
            if (system != null)
                system.TryRechargeShield(context.HitAlly);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            BouclarShieldSystem system = context.Owner.GetComponent<BouclarShieldSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<BouclarShieldSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            system.SetEnhanced(true);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

