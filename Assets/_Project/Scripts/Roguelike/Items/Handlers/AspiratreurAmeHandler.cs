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

            context.SourceAlly.Heal(Mathf.CeilToInt(context.SourceAlly.MaxHp * item.Data.MainValue));
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
