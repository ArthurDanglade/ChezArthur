using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Soigne l'allié qui vient de tuer un ennemi.
    /// </summary>
    public class AspiratreurAmeHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnAllyKill) return;
            if (context.SourceAlly == null) return;

            int healAmount = Mathf.CeilToInt(context.SourceAlly.MaxHp * item.Data.MainValue);
            context.SourceAlly.Heal(healAmount);
            Debug.Log($"[Item] {item.Data.ItemName} : +{healAmount} PV à {context.SourceAlly.Name}");
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
