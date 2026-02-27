namespace ChezArthur.Core
{
    /// <summary>
    /// États possibles du jeu pour la machine à états globale.
    /// </summary>
    public enum GameState
    {
        /// <summary> Écran principal / sélection avant partie. </summary>
        Menu,

        /// <summary> Partie en cours, gameplay actif. </summary>
        Playing,

        /// <summary> Partie en pause (menu pause affiché). </summary>
        Paused,

        /// <summary> Victoire (niveau ou run terminé avec succès). </summary>
        Victory,

        /// <summary> Défaite (run terminée). </summary>
        Defeat
    }
}
