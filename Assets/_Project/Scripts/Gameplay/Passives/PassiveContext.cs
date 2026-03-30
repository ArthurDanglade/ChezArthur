using ChezArthur.Characters;
using ChezArthur.Enemies;

namespace ChezArthur.Gameplay.Passives
{
    /// <summary>
    /// Contexte passé aux handlers de passifs spéciaux.
    /// Réutilisable (pas de new à chaque trigger).
    /// </summary>
    public class PassiveContext
    {
        /// <summary>Le CharacterBall qui possède ce passif.</summary>
        public CharacterBall Owner { get; set; }

        /// <summary>Le TurnManager (pour accéder aux alliés, ennemis, etc.).</summary>
        public TurnManager TurnManager { get; set; }

        /// <summary>L'ennemi touché (si applicable, null sinon).</summary>
        public Enemy HitEnemy { get; set; }

        /// <summary>L'allié touché (si applicable, null sinon).</summary>
        public CharacterBall HitAlly { get; set; }

        /// <summary>Dégâts infligés ou reçus (selon le trigger).</summary>
        public int DamageAmount { get; set; }

        /// <summary>Le trigger qui a déclenché ce passif.</summary>
        public PassiveTrigger Trigger { get; set; }

        /// <summary>
        /// Reset le contexte pour réutilisation (évite les allocations).
        /// </summary>
        public void Clear()
        {
            Owner = null;
            TurnManager = null;
            HitEnemy = null;
            HitAlly = null;
            DamageAmount = 0;
            Trigger = default;
        }
    }
}
