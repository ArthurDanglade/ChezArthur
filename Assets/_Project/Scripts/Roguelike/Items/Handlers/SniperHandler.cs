using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Applique une vulnérabilité temporaire sur un ennemi très haut en vie.
    /// </summary>
    public class SniperHandler : IItemEffectHandler
    {
        private const float MIN_HP_RATIO = 0.90f;

        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnEnemyHit) return;
            if (context.TargetEnemy == null) return;

            float hpRatio = context.TargetEnemy.MaxHp > 0
                ? (float)context.TargetEnemy.CurrentHp / context.TargetEnemy.MaxHp
                : 0f;
            if (hpRatio < MIN_HP_RATIO) return;

            BuffReceiver buffReceiver = context.TargetEnemy.BuffReceiver;
            if (buffReceiver == null) return;

            buffReceiver.AddBuff(new BuffData
            {
                BuffId = "sniper_bonus",
                Source = context.SourceAlly,
                StatType = BuffStatType.DamageAmplification,
                Value = item.Data.MainValue,
                IsPercent = true,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
