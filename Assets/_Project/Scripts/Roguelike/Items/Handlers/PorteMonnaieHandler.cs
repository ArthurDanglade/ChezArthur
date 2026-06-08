using UnityEngine;
using ChezArthur.Core;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Handler passif : bonus d'ATK basé sur les Tals.
    /// </summary>
    public class PorteMonnaieHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item) { }

        public float GetAtkBonusFromTals(ItemInstance item)
        {
            if (item == null || item.Data == null) return 0f;
            if (RunManager.Instance == null) return 0f;

            float ratio = RunManager.Instance.TalsEarned / 1000f;
            return Mathf.Clamp(ratio, 0f, item.Data.MainValue);
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
