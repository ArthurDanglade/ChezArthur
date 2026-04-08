using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Contrat que doit respecter tout handler de passif ennemi spécialisé.
    /// Chaque handler gère une mécanique complexe qu'on ne peut pas exprimer avec le système data-driven.
    /// </summary>
    public interface IEnemyPassiveHandler
    {
        /// <summary>
        /// Identifiant unique du handler. Doit correspondre à EnemyPassiveData.SpecialHandlerId.
        /// </summary>
        string HandlerId { get; }

        /// <summary>
        /// Initialise le handler avec les références nécessaires.
        /// Appelé par EnemyPassiveRuntime au début de l'étage.
        /// </summary>
        void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager);

        /// <summary>
        /// Appelé à chaque début de tour de l'ennemi propriétaire.
        /// </summary>
        void OnTurnStart();

        /// <summary>
        /// Appelé à chaque début de cycle.
        /// </summary>
        void OnCycleStart();

        /// <summary>
        /// Appelé quand l'ennemi reçoit des dégâts.
        /// </summary>
        /// <param name="damage">Dégâts reçus après réduction.</param>
        void OnTakeDamage(int damage);

        /// <summary>
        /// Appelé quand un allié (équipe joueur) reçoit des dégâts.
        /// </summary>
        /// <param name="ally">L'allié touché.</param>
        /// <param name="damage">Dégâts reçus.</param>
        void OnAllyDamaged(CharacterBall ally, int damage);

        /// <summary>
        /// Appelé quand un allié (équipe joueur) reçoit un soin.
        /// </summary>
        /// <param name="ally">L'allié soigné.</param>
        /// <param name="healAmount">Montant du soin.</param>
        void OnAllyHealed(CharacterBall ally, int healAmount);

        /// <summary>
        /// Appelé quand un allié (équipe joueur) meurt.
        /// </summary>
        /// <param name="ally">L'allié mort.</param>
        void OnAllyKilled(CharacterBall ally);

        /// <summary>
        /// Appelé quand un coéquipier (autre ennemi) meurt.
        /// </summary>
        /// <param name="mate">Le coéquipier mort.</param>
        void OnMateKilled(Enemy mate);

        /// <summary>
        /// Appelé quand les HP de l'ennemi changent.
        /// Permet de gérer les seuils HP et les phases.
        /// </summary>
        /// <param name="currentHp">HP actuels.</param>
        /// <param name="maxHp">HP max.</param>
        void OnHpChanged(int currentHp, int maxHp);

        /// <summary>
        /// Appelé quand l'ennemi touche un allié pendant son lancer.
        /// </summary>
        /// <param name="ally">L'allié touché.</param>
        void OnHitAlly(CharacterBall ally);

        /// <summary>
        /// Appelé quand l'ennemi est touché par un allié.
        /// </summary>
        /// <param name="attacker">L'allié attaquant.</param>
        void OnHitByAlly(CharacterBall attacker);

        /// <summary>
        /// Remet le handler à zéro pour un nouvel étage.
        /// </summary>
        void ResetForNewStage();

        /// <summary>
        /// Libère les abonnements aux events pour éviter les memory leaks.
        /// </summary>
        void Cleanup();
    }
}
