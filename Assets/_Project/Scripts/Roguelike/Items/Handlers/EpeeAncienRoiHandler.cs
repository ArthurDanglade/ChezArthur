using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Tour fantôme après un coup fatal : revanche ou mort définitive.
    /// </summary>
    public class EpeeAncienRoiHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnAllyTakeDamage) return;
            if (item.IsConsumed) return;
            if (context.SourceAlly == null) return;
            if (context.SourceAlly.CurrentHp > 0) return;
            if (context.SourceAlly.IsGhostState) return;

            Enemy killer = null;
            if (context.TurnManager != null)
            {
                ITurnParticipant current = context.TurnManager.CurrentParticipant;
                if (current != null && !current.IsAlly)
                    killer = current as Enemy;
            }

            if (!context.SourceAlly.TryBeginGhostTurn(killer, item.Data.MainValue))
                return;

            if (ItemManager.Instance != null)
                ItemManager.Instance.ConsumeItem(item.Data.Id);

            if (RunManager.Instance != null)
                RunManager.Instance.RequestGhostTurn(context.SourceAlly);

            Debug.Log($"[Item] {item.Data.ItemName} : {context.SourceAlly.Name} entre en état Fantôme");
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
