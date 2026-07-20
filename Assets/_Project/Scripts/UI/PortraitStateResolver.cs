using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Source unique de la règle d'état d'artwork (prime vs déchu).
    /// Aucun autre code ne doit décider cet état.
    /// </summary>
    public static class PortraitStateResolver
    {
        /// <summary>
        /// Résout le sheet animé à afficher selon l'éveil et la préférence joueur.
        /// </summary>
        public static AnimatedPortraitData Resolve(CharacterData data, OwnedCharacter owned)
        {
            if (data == null)
                return null;

            // Prime uniquement post-cérémonie, sans spoiler mid-run.
            bool usePrime = owned != null
                && owned.isAwakened
                && owned.awakeningCeremonySeen
                && !owned.prefersDechuArtwork
                && data.AnimatedPortraitPrime != null;

            if (usePrime)
                return data.AnimatedPortraitPrime;

            return data.AnimatedPortraitDechu;
        }

        /// <summary>
        /// True si le joueur peut basculer librement entre prime et déchu.
        /// </summary>
        public static bool CanSwitchArtwork(CharacterData data, OwnedCharacter owned)
        {
            return owned != null
                && owned.isAwakened
                && owned.awakeningCeremonySeen
                && data != null
                && data.AnimatedPortraitPrime != null
                && data.AnimatedPortraitDechu != null;
        }
    }
}
