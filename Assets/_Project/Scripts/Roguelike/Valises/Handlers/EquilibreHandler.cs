using System.Collections.Generic;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise Équilibre : stacks = nombre d'items actifs ; override valeur lv20.
    /// </summary>
    public class EquilibreHandler : IValiseEffectHandler
    {
        private const float Lv20ValuePerLevel = 0.015f;

        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise)
        {
            if (context == null || valise == null || ValiseManager.Instance == null) return;

            if (context.Trigger == ValiseTrigger.OnItemSlotsChanged)
            {
                SyncFromItems(valise);
                return;
            }

            if (context.Trigger == ValiseTrigger.OnValiseChanged)
                ApplyLv20Override(valise);
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise)
        {
            ValiseInstance active = valise;
            if (active == null && ValiseManager.Instance != null)
                active = ValiseManager.Instance.GetActiveValise("valise_equilibre");

            ApplyLv20Override(active);
            SyncFromItems(active);
        }

        private static void SyncFromItems(ValiseInstance valise)
        {
            if (ValiseManager.Instance == null || ItemManager.Instance == null) return;

            IReadOnlyList<ItemInstance> items = ItemManager.Instance.GetActiveSlots();
            int targetStacks = items != null ? items.Count : 0;
            ValiseManager.Instance.SyncStacksToTarget("valise_equilibre", targetStacks);
            ApplyLv20Override(valise);
        }

        private static void ApplyLv20Override(ValiseInstance valise)
        {
            if (valise == null) return;

            if (valise.IsLevel20Unlocked)
                valise.SetValuePerLevelOverride(Lv20ValuePerLevel);
            else
                valise.ClearValuePerLevelOverride();
        }
    }
}
