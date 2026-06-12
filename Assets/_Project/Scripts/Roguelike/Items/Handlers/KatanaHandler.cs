using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Inflige des dégâts bonus fixes sur coup critique (+10 % du crit).
    /// </summary>
    public class KatanaHandler : IItemEffectHandler
    {
        private const float BONUS_RATE = 0.10f;

        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnCriticalHit) return;
            if (context.SourceAlly == null) return;
            if (context.TargetEnemy == null) return;

            int critDamage = context.DamageAmount;
            int bonusDamage = Mathf.RoundToInt(critDamage * BONUS_RATE);
            if (bonusDamage <= 0) return;

            context.TargetEnemy.TakePureDamage(bonusDamage);
            Debug.Log($"[Item] {item.Data.ItemName} : +{bonusDamage} (crit {critDamage})");
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
