using ChezArthur.Characters;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Guide lumineux » (lumino_light) : buff « Éclairé » sur l'allié touché (OnHitAlly).
    /// Le +10 % dégâts subis par les ennemis touchés est appliqué dans <see cref="CharacterBall"/> au contact.
    /// </summary>
    /// <remarks>
    /// TODO Phase 3 : bonus +3 % ATK/DEF permanent à chaque boss vaincu (événement boss / run).
    /// </remarks>
    public class LuminoLightHandler : ISpecialPassiveHandler
    {
        private const string BuffEclaireAtkId = "lumino_eclaire_atk";

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitAlly == null) return;
            CharacterBall owner = context.Owner;
            if (owner == null) return;

            BuffReceiver br = context.HitAlly.BuffReceiver;
            if (br == null) return;

            br.AddBuff(new BuffData
            {
                BuffId = BuffEclaireAtkId,
                Source = owner,
                StatType = BuffStatType.ATK,
                Value = 0.10f,
                IsPercent = true,
                RemainingTurns = 2,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
