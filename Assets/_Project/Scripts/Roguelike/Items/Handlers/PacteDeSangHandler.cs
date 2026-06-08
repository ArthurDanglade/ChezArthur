using ChezArthur.Core;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Demande un sacrifice au joueur quand l'item est acquis.
    /// </summary>
    public class PacteDeSangHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnItemAcquired) return;
            if (context.TurnManager == null) return;
            if (RunManager.Instance == null) return;

            RunManager.Instance.RequestPacteDeSang(context.TurnManager.GetAllies());
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
