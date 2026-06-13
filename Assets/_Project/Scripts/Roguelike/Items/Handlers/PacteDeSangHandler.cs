using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Sacrifie un allié à l'acquisition et buffe les survivants.
    /// </summary>
    public class PacteDeSangHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnItemAcquired) return;
            if (context.TurnManager == null) return;
            if (RunManager.Instance == null) return;

            // TODO UI choix allié — Phase 4
            IReadOnlyList<CharacterBall> allies = context.TurnManager.GetAllies();
            if (allies == null) return;

            int sacrificeIndex = -1;
            int lowestHp = int.MaxValue;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                if (ally.CurrentHp < lowestHp)
                {
                    lowestHp = ally.CurrentHp;
                    sacrificeIndex = i;
                }
            }

            if (sacrificeIndex < 0) return;

            CharacterBall sacrifice = allies[sacrificeIndex];
            Debug.Log($"[Item] {item.Data.ItemName} : {sacrifice.Name} sacrifié (auto, UI Phase 4)");
            RunManager.Instance.ConfirmPacteDeSang(sacrificeIndex, item.Data.MainValue);
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
