using UnityEngine;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Peut entoiler un ennemi touché, une seule fois par lancer.
    /// </summary>
    public class LanceurDeToileHandler : IItemEffectHandler
    {
        private bool _hasTriggeredThisLaunch;

        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;

            if (context.Trigger == ItemTrigger.OnAllyLaunch)
            {
                _hasTriggeredThisLaunch = false;
                return;
            }

            if (context.Trigger != ItemTrigger.OnEnemyHit) return;
            if (context.TargetEnemy == null) return;
            if (_hasTriggeredThisLaunch) return;
            if (Random.value > item.Data.MainValue) return;

            if (context.TargetEnemy.BuffReceiver == null) return;

            context.TargetEnemy.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = "entoile",
                Source = context.SourceAlly,
                StatType = BuffStatType.LaunchForce,
                Value = -0.30f,
                IsPercent = true,
                RemainingTurns = 2,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });

            _hasTriggeredThisLaunch = true;
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item)
        {
            _hasTriggeredThisLaunch = false;
        }

        public void OnRunStart(ItemEffectContext context, ItemInstance item)
        {
            _hasTriggeredThisLaunch = false;
        }
    }
}
