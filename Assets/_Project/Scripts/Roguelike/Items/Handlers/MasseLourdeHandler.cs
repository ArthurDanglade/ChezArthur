using ChezArthur.Enemies;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Applique une vulnérabilité temporaire sur boss et mini-boss.
    /// </summary>
    public class MasseLourdeHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnEnemyHit) return;
            if (context.TargetEnemy == null) return;

            Enemy enemy = context.TargetEnemy;
            EnemyData data = enemy.Data;
            if (data == null) return;

            bool isBossType = data.EnemyType == EnemyType.Boss || data.EnemyType == EnemyType.MiniBoss;
            bool isBossRole = data.EnemyRole == EnemyRole.Boss || data.EnemyRole == EnemyRole.MiniBoss;
            if (!isBossType && !isBossRole) return;

            BuffReceiver buffReceiver = enemy.BuffReceiver;
            if (buffReceiver == null) return;

            buffReceiver.AddBuff(new BuffData
            {
                BuffId = "masse_lourde_bonus",
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
