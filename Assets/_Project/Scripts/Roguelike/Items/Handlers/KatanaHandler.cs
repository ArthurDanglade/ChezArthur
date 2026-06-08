using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Inflige des dégâts bonus sur coup critique.
    /// </summary>
    public class KatanaHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnCriticalHit) return;
            if (context.SourceAlly == null) return;
            if (context.TargetEnemy == null) return;

            int bonusDamage = Mathf.RoundToInt(context.DamageAmount * item.Data.MainValue);
            context.TargetEnemy.TakeDamage(bonusDamage);
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
