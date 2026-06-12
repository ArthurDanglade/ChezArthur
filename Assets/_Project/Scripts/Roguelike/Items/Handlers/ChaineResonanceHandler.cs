using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Ajoute des dégâts bonus à partir du deuxième ennemi touché du lancer.
    /// </summary>
    public class ChaineResonanceHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnEnemyHit) return;
            if (context.TargetEnemy == null) return;
            if (context.EnemyHitCount <= 1) return;

            int bonusDamage = Mathf.RoundToInt(context.DamageAmount * item.Data.MainValue);
            if (bonusDamage <= 0) return;
            context.TargetEnemy.TakeDamage(bonusDamage);
            Debug.Log($"[Item] {item.Data.ItemName} : +{bonusDamage} dégâts résonance (ennemi #{context.EnemyHitCount})");
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
