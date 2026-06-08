namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Interface des handlers d'effets d'items.
    /// </summary>
    public interface IItemEffectHandler
    {
        /// <summary>
        /// Appelé quand le trigger correspondant de l'item se déclenche.
        /// </summary>
        void OnTriggered(ItemEffectContext context, ItemInstance item);

        /// <summary>
        /// Appelé au début de chaque étage.
        /// </summary>
        void OnStageStart(ItemEffectContext context, ItemInstance item);

        /// <summary>
        /// Appelé au début de la run pour initialiser l'état si nécessaire.
        /// </summary>
        void OnRunStart(ItemEffectContext context, ItemInstance item);
    }
}
