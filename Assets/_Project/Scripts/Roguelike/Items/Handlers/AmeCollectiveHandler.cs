using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Propage une partie des soins vers l'allié vivant le plus bas en PV.
    /// </summary>
    public class AmeCollectiveHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnAllyHeal) return;
            if (context.SourceAlly == null) return;
            if (context.TurnManager == null) return;

            int propagatedHeal = Mathf.RoundToInt(context.DamageAmount * item.Data.MainValue);
            if (propagatedHeal <= 0) return;

            IReadOnlyList<CharacterBall> allies = context.TurnManager.GetAllies();
            if (allies == null) return;

            CharacterBall allyWithLeastHp = null;
            float lowestRatio = float.MaxValue;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally == context.SourceAlly) continue;

                float ratio = ally.MaxHp > 0
                    ? (float)ally.CurrentHp / ally.MaxHp
                    : 1f;

                if (ratio < lowestRatio)
                {
                    lowestRatio = ratio;
                    allyWithLeastHp = ally;
                }
            }

            if (allyWithLeastHp != null)
            {
                allyWithLeastHp.Heal(propagatedHeal);
                Debug.Log($"[Item] {item.Data.ItemName} : +{propagatedHeal} PV à {allyWithLeastHp.Name}");
            }
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
