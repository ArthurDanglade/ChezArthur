using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Bol monstre » (elfert_badluck) : chance de stun au contact ennemi (OnHitEnemy).
    /// </summary>
    public class ElfertBadLuckHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitEnemy == null) return;
            if (Random.value >= 0.25f) return;
            if (StunSystem.Instance == null) return;

            StunSystem.Instance.StunEnemy(context.HitEnemy, context.Owner);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
