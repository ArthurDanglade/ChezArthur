using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Contexte partagé pour les déclenchements d'effets de valises.
    /// Réutilisable pour éviter les allocations runtime.
    /// </summary>
    public class ValiseEffectContext
    {
        public ValiseTrigger Trigger;
        public CharacterBall SourceAlly;
        public Enemy TargetEnemy;
        public CharacterBall TargetAlly;
        public TurnManager TurnManager;
        public SpecializationData PreviousSpec;
        public SpecializationData CurrentSpec;
        public int DamageAmount;
        public int IntValue;
        public bool HasPreviousTurn;
        public bool BoolFlag;

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
            PreviousSpec = null;
            CurrentSpec = null;
            DamageAmount = 0;
            IntValue = 0;
            HasPreviousTurn = false;
            BoolFlag = false;
        }
    }
}
