using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Classe de base abstraite pour les handlers spécialisés.
    /// Fournit les références communes et des implémentations vides par défaut pour toutes les méthodes de l'interface.
    /// Les handlers concrets n'overrident que ce dont ils ont besoin.
    /// </summary>
    public abstract class EnemyPassiveHandlerBase : IEnemyPassiveHandler
    {
        // ═══════════════════════════════════════════
        // VARIABLES PROTÉGÉES
        // ═══════════════════════════════════════════

        protected Enemy _owner;
        protected EnemyPassiveData _data;
        protected TurnManager _turnManager;
        protected bool _initialized;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════

        public abstract string HandlerId { get; }

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════

        public virtual void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            _owner = owner;
            _data = data;
            _turnManager = turnManager;
            _initialized = true;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES VIRTUELLES
        // ═══════════════════════════════════════════

        public virtual void OnTurnStart() { }

        public virtual void OnCycleStart() { }

        public virtual void OnTakeDamage(int damage) { }

        public virtual void OnAllyDamaged(CharacterBall ally, int damage) { }

        public virtual void OnAllyHealed(CharacterBall ally, int healAmount) { }

        public virtual void OnAllyKilled(CharacterBall ally) { }

        public virtual void OnMateKilled(Enemy mate) { }

        public virtual void OnHpChanged(int currentHp, int maxHp) { }

        public virtual void OnHitAlly(CharacterBall ally) { }

        public virtual void OnHitByAlly(CharacterBall attacker) { }

        public virtual void ResetForNewStage() { }

        public virtual void Cleanup() { }

        // ═══════════════════════════════════════════
        // MÉTHODES PROTÉGÉES UTILITAIRES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Vérifie que le handler est initialisé.
        /// À appeler en début de chaque méthode si nécessaire.
        /// </summary>
        protected bool IsReady => _initialized && _owner != null && !_owner.IsDead;

        /// <summary>
        /// Logue un avertissement si le handler n'est pas initialisé.
        /// </summary>
        protected void LogNotInitialized()
        {
            Debug.LogWarning($"[{HandlerId}] Handler non initialisé ou owner mort.");
        }
    }
}
