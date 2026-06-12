using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Ajoute des dégâts bonus si le lancer a assez rebondi sur les murs.
    /// </summary>
    public class MiroirBriseHandler : IItemEffectHandler
    {
        private const int REQUIRED_WALL_BOUNCES = 3;

        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnEnemyHit) return;
            if (context.TargetEnemy == null) return;
            if (context.WallBounceCount < REQUIRED_WALL_BOUNCES) return;

            int bonusDamage = Mathf.RoundToInt(context.DamageAmount * (item.Data.MainValue - 1f));
            if (bonusDamage <= 0) return;
            context.TargetEnemy.TakePureDamage(bonusDamage);
            Debug.Log($"[Item] {item.Data.ItemName} : dégâts doublés ({context.WallBounceCount} rebonds, +{bonusDamage})");
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
