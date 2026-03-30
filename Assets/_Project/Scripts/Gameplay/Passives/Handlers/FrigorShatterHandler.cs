using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Coup de froid » (frigor_shatter) : la brisure est gérée par <see cref="FreezeSystem.TryShatter"/> depuis <see cref="CharacterBall"/>.
    /// Handler présent uniquement pour un specialEffectId valide dans le registre.
    /// </summary>
    public class FrigorShatterHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
