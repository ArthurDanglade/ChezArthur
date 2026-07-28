namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Interface des handlers d'effets comportementaux de valises.
    /// </summary>
    public interface IValiseEffectHandler
    {
        /// <summary>
        /// Appelé quand le trigger correspondant se déclenche.
        /// </summary>
        void OnTriggered(ValiseEffectContext context, ValiseInstance valise);

        /// <summary>
        /// Appelé au début de chaque étage.
        /// </summary>
        void OnStageStart(ValiseEffectContext context, ValiseInstance valise);

        /// <summary>
        /// Appelé au début de la run pour initialiser l'état si nécessaire.
        /// </summary>
        void OnRunStart(ValiseEffectContext context, ValiseInstance valise);
    }
}
