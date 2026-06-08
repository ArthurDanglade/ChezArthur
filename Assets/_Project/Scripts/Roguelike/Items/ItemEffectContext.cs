using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Contexte partagé pour les déclenchements d'effets d'items.
    /// Réutilisable pour éviter les allocations runtime.
    /// </summary>
    public class ItemEffectContext
    {
        public ItemTrigger Trigger;
        public CharacterBall SourceAlly;
        public Enemy TargetEnemy;
        public CharacterBall TargetAlly;
        public TurnManager TurnManager;
        public int DamageAmount;
        public float VelocityRatio;
        public int WallBounceCount;
        public int EnemyHitCount;

        /// <summary>
        /// Réinitialise le contexte pour réutilisation.
        /// </summary>
        public void Clear()
        {
            Trigger = default;
            SourceAlly = null;
            TargetEnemy = null;
            TargetAlly = null;
            TurnManager = null;
            DamageAmount = 0;
            VelocityRatio = 0f;
            WallBounceCount = 0;
            EnemyHitCount = 0;
        }
    }
}
