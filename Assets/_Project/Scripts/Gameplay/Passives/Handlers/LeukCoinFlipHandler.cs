using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Pile ou face" (leuk_coinflip) :
    /// la logique s'exécute dans CharacterBall.TakeDamage via LeukCoinFlipSystem.
    /// </summary>
    public class LeukCoinFlipHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            LeukCoinFlipSystem system = context.Owner.GetComponent<LeukCoinFlipSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<LeukCoinFlipSystem>();

            system.Initialize(context.Owner);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

