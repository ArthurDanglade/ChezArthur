namespace ChezArthur.UI
{
    /// <summary>
    /// Mode d'affichage de l'aura combat.
    /// </summary>
    public enum AuraPlacementMode
    {
        /// <summary>Anneau centré autour du perso (calque AuraLayer), ombre conservée.</summary>
        Halo = 0,

        /// <summary>Anneau au sol : remplace l'ombre (SpriteRenderer Shadow), pas d'ellipse noire.</summary>
        GroundRing = 1
    }
}
