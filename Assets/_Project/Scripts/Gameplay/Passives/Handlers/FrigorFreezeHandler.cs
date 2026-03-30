using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Sueur glaciale » (frigor_freeze) : au contact ennemi, applique le gel via <see cref="FreezeSystem"/>.
    /// </summary>
    /// <remarks>
    /// PassiveData en <see cref="PassiveTrigger.OnHitEnemy"/> ; prévoir un MaxStacks élevé (ex. 99) pour chaque touche.
    /// </remarks>
    public class FrigorFreezeHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitEnemy == null) return;
            if (FreezeSystem.Instance == null) return;

            FreezeSystem.Instance.FreezeEnemy(context.HitEnemy, context.Owner);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
