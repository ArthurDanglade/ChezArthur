using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Marque le premier ennemi touché de l'étage avec une vulnérabilité permanente.
    /// </summary>
    public class CibleMarqueeHandler : IItemEffectHandler
    {
        private string _markedEnemyId = null;

        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnEnemyHit) return;
            if (context.TargetEnemy == null) return;
            if (_markedEnemyId != null) return;

            _markedEnemyId = context.TargetEnemy.GetInstanceID().ToString();

            BuffReceiver buffReceiver = context.TargetEnemy.BuffReceiver;
            if (buffReceiver == null) return;

            buffReceiver.AddBuff(new BuffData
            {
                BuffId = "cible_marquee",
                Source = context.SourceAlly,
                StatType = BuffStatType.DamageAmplification,
                Value = item.Data.MainValue,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item)
        {
            _markedEnemyId = null;
        }

        public void OnRunStart(ItemEffectContext context, ItemInstance item)
        {
            _markedEnemyId = null;
        }
    }
}
