using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Racines profondes » (ronss_roots) :
    /// - déclenche l'enracinement sous 30% HP (une fois par étage),
    /// - applique DR +40% / HealReceived +20% pendant 2 cycles,
    /// - active le soin de proximité via <see cref="RonssRootsSystem"/>.
    /// </summary>
    public class RonssRootsHandler : ISpecialPassiveHandler
    {
        private const string RootsBuffId = "ronss_roots_dr";
        private const string RootsHealBuffId = "ronss_roots_heal";

        // Ronss qui ont déjà consommé le trigger sur l'étage courant.
        private readonly HashSet<CharacterBall> _triggeredThisStage = new HashSet<CharacterBall>();

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;
            CharacterBall owner = context.Owner;

            if (_triggeredThisStage.Contains(owner)) return;

            float hpRatio = owner.MaxHp > 0 ? (float)owner.CurrentHp / owner.MaxHp : 1f;
            if (hpRatio > 0.30f) return;

            BuffReceiver br = owner.BuffReceiver;
            if (br == null) return;

            _triggeredThisStage.Add(owner);

            br.AddBuff(new BuffData
            {
                BuffId = RootsBuffId,
                Source = owner,
                StatType = BuffStatType.DamageReduction,
                Value = 0.40f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = 2,
                UniquePerSource = false,
                UniqueGlobal = true
            });

            br.AddBuff(new BuffData
            {
                BuffId = RootsHealBuffId,
                Source = owner,
                StatType = BuffStatType.HealReceived,
                Value = 0.20f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = 2,
                UniquePerSource = false,
                UniqueGlobal = true
            });

            RonssRootsSystem rootsSystem = owner.GetComponent<RonssRootsSystem>();
            if (rootsSystem == null)
            {
                rootsSystem = owner.gameObject.AddComponent<RonssRootsSystem>();
                rootsSystem.Initialize(owner, context.TurnManager);
            }
            else
            {
                rootsSystem.Initialize(owner, context.TurnManager);
            }

            rootsSystem.SetRootsActive(true);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            _triggeredThisStage.Remove(context.Owner);

            RonssRootsSystem rootsSystem = context.Owner.GetComponent<RonssRootsSystem>();
            if (rootsSystem != null)
            {
                rootsSystem.Initialize(context.Owner, context.TurnManager);
                rootsSystem.SetRootsActive(false);
            }
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

