using ChezArthur.Characters;

namespace ChezArthur.Gameplay.Passives
{
    /// <summary>
    /// Interface pour les handlers de passifs spéciaux.
    /// Chaque handler gère un ou plusieurs specialEffectId.
    /// </summary>
    public interface ISpecialPassiveHandler
    {
        /// <summary>
        /// Appelé quand le trigger du passif se déclenche.
        /// </summary>
        /// <param name="context">Contexte du déclenchement (perso source, cible, etc.).</param>
        /// <param name="passiveData">Données du passif déclenché.</param>
        /// <param name="instance">Instance runtime du passif (stacks, etc.).</param>
        void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance);

        /// <summary>
        /// Retourne le bonus de stat de ce passif spécial (appelé par GetStatBonus).
        /// Retourner 0 si pas de bonus stat.
        /// </summary>
        float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance);

        /// <summary>
        /// Appelé au début de chaque étage pour reset/init si nécessaire.
        /// </summary>
        void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance);

        /// <summary>
        /// Appelé au switch de spé pour nettoyer/sauvegarder l'état si nécessaire.
        /// </summary>
        void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance);
    }
}
