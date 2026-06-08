using UnityEngine;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives.Handlers;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Applique une brûlure conditionnelle selon la vélocité du lancer.
    /// </summary>
    public class BouleDeFeuHandler : IItemEffectHandler
    {
        private const float MIN_VELOCITY_RATIO = 0.80f;
        private const int BURN_TURNS = 3;

        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnEnemyHit) return;
            if (context.TargetEnemy == null) return;
            if (context.VelocityRatio < MIN_VELOCITY_RATIO) return;

            BuffReceiver buffReceiver = context.TargetEnemy.BuffReceiver;
            if (buffReceiver != null)
            {
                buffReceiver.AddBuff(new BuffData
                {
                    BuffId = "boule_de_feu_burn",
                    Source = context.SourceAlly,
                    StatType = BuffStatType.DamageAmplification,
                    Value = 0f,
                    IsPercent = false,
                    RemainingTurns = BURN_TURNS,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = false
                });
            }

            // TODO: Brancher une API DOT dédiée si BurnTickSystem expose ApplyBurn plus tard.
            int burnDamage = Mathf.RoundToInt(item.Data.MainValue);
            context.TargetEnemy.TakeDamage(burnDamage);
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
