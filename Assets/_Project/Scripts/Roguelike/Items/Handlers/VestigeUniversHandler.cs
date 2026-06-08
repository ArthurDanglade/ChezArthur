using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Soigne les alliés vivants si toute l'équipe vivante est sous 20% HP.
    /// </summary>
    public class VestigeUniversHandler : IItemEffectHandler
    {
        private const float HP_THRESHOLD = 0.20f;

        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnAllyTakeDamage) return;
            if (item.IsConsumed) return;
            if (context.TurnManager == null) return;

            IReadOnlyList<CharacterBall> allies = context.TurnManager.GetAllies();
            if (allies == null) return;

            bool hasLivingAlly = false;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;

                hasLivingAlly = true;
                float ratio = ally.MaxHp > 0 ? (float)ally.CurrentHp / ally.MaxHp : 0f;
                if (ratio >= HP_THRESHOLD)
                    return;
            }

            if (!hasLivingAlly) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                ally.Heal(Mathf.CeilToInt(ally.MaxHp * item.Data.MainValue));
            }

            if (ItemManager.Instance != null)
                ItemManager.Instance.ConsumeItem(item.Data.Id);
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
