using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Pluie de lances » (lanshimmer_rain) :
    /// - active le mode enhanced (rayon impact agrandi),
    /// - applique +20% launch force permanent pour le stage,
    /// - ajoute 20% de chance de stun sur OnHitEnemy.
    /// </summary>
    public class LansHimmerRainHandler : ISpecialPassiveHandler
    {
        private const string LaunchForceBuffId = "lanshimmer_lf";

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitEnemy == null) return;
            if (StunSystem.Instance == null) return;

            if (Random.value < 0.20f)
                StunSystem.Instance.StunEnemy(context.HitEnemy, context.Owner);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            CharacterBall owner = context.Owner;

            LanceImpactSystem system = owner.GetComponent<LanceImpactSystem>();
            if (system == null)
                system = owner.gameObject.AddComponent<LanceImpactSystem>();

            system.Initialize(owner, context.TurnManager);
            system.SetEnhanced(true);

            BuffReceiver br = owner.BuffReceiver;
            if (br == null) return;

            br.AddBuff(new BuffData
            {
                BuffId = LaunchForceBuffId,
                Source = owner,
                StatType = BuffStatType.LaunchForce,
                Value = 0.20f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

