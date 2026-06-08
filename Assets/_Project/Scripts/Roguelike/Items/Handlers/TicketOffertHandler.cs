using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Réanime l'équipe lors d'un game over, puis se consomme.
    /// </summary>
    public class TicketOffertHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnGameOver) return;
            if (item.IsConsumed) return;
            if (context.TurnManager == null) return;

            IReadOnlyList<CharacterBall> allies = context.TurnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || !ally.IsDead) continue;

                ally.Revive();
                ally.Heal(Mathf.CeilToInt(ally.MaxHp * item.Data.MainValue));
            }

            if (ItemManager.Instance != null)
                ItemManager.Instance.ConsumeItem(item.Data.Id);
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
