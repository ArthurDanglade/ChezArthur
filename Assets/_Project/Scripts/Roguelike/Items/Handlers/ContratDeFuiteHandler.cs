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

            context.SourceAlly.Revive();
            // Force HP à 1 après résurrection — Revive remet au max.
            // On applique les dégâts pour ramener à 1 HP.
            int damageToReduce = context.SourceAlly.CurrentHp - 1;
            if (damageToReduce > 0)
                context.SourceAlly.TakeDamage(damageToReduce);

            if (ItemManager.Instance != null)
                ItemManager.Instance.ConsumeItem(item.Data.Id);
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
