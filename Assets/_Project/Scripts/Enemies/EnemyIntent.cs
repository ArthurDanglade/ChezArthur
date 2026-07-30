using ChezArthur.Gameplay;

namespace ChezArthur.Enemies
{
    /// <summary> Type d'intention affichable (R6). Ne JAMAIS réordonner : consommé par G6. </summary>
    public enum EnemyIntentKind
    {
        None = 0,
        Charge = 1,
        Projectile = 2,
        Zone = 3,
        Special = 4,
    }

    /// <summary> Description d'intention affichable (R6). </summary>
    public struct EnemyIntent
    {
        public EnemyIntentKind Kind;
        /// <summary> Cible surbrillée (anneau). Null = pas de cible individuelle (ex. zone pure). </summary>
        public CharacterBall Target;
        /// <summary> Glyphe placeholder de l'icône (TMP world-space) — remplacé au gate d'art. </summary>
        public string IconText;
    }

    /// <summary>
    /// Fournisseur d'intention d'un ennemi (handlers G6 : archere_branches, patriarche_eaux…).
    /// S'enregistre via EnemyIntentSystem.RegisterProvider dans Initialize,
    /// se désenregistre dans Cleanup.
    /// </summary>
    public interface IEnemyIntentProvider
    {
        /// <summary> Intention courante (réévaluée à chaque refresh — état vivant R6). </summary>
        bool TryGetIntent(out EnemyIntent intent);

        /// <summary>
        /// L'ennemi devient / cesse d'être le prochain à jouer. Le provider intensifie
        /// ici SES zones persistantes (GroundZone.SetHighlighted) — R7.
        /// </summary>
        void OnTelegraphStateChanged(bool isTelegraphing);
    }
}
