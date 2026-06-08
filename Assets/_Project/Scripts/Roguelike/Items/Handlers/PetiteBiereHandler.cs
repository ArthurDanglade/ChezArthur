namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Item passif : bonus de soin entre étages.
    /// </summary>
    public class PetiteBiereHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            // Effet appliqué via ItemManager.GetHealBetweenStagesBonus().
        }

        /// <summary>
        /// Retourne le bonus de soin entre étages de l'instance.
        /// </summary>
        public float GetHealBetweenStagesBonus(ItemInstance item)
        {
            if (item == null || item.Data == null) return 0f;
            return item.Data.MainValue;
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
