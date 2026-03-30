using ChezArthur.Characters;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Handler pour « L'odeur se répand » (pusamair_spread).
    /// Les alliés qui touchent Pusam Air deviennent porteurs et peuvent empoisonner via <see cref="PoisonTickSystem.TryApplyCarrierPoison"/>.
    /// </summary>
    /// <remarks>
    /// Trigger <see cref="PassiveTrigger.OnHitBySelf"/> sur Pusam Air. MaxStacks élevé sur l'asset pour chaque contact allié.
    /// </remarks>
    public class PusamAirSpreadHandler : ISpecialPassiveHandler
    {
        private const string CarrierBuffId = "pusamair_carrier";

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitAlly == null) return;

            BuffReceiver allyBuffReceiver = context.HitAlly.BuffReceiver;
            if (allyBuffReceiver == null) return;

            var carrierBuff = new BuffData
            {
                BuffId = CarrierBuffId,
                Source = context.Owner,
                StatType = BuffStatType.ATK,
                Value = 0f,
                IsPercent = false,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            };
            allyBuffReceiver.AddBuff(carrierBuff);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            return 0f;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
