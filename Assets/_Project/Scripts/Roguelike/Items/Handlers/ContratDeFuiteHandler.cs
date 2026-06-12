using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Sauve une fois un allié d'un coup létal et consomme l'item.
    /// </summary>
    public class ContratDeFuiteHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnAllyTakeDamage) return;
            if (item.IsConsumed) return;
            if (context.SourceAlly == null) return;
            if (context.SourceAlly.CurrentHp > 0) return;

            int maxHp = context.SourceAlly.MaxHp;
            if (maxHp <= 0) return;
            context.SourceAlly.Revive(1f / maxHp);

            Debug.Log($"[Item] {item.Data.ItemName} : {context.SourceAlly.Name} sauvé à 1 PV");

            if (ItemManager.Instance != null)
                ItemManager.Instance.ConsumeItem(item.Data.Id);
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
