using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Vomir c'est repartir » (morgan_vomit) : sous 20 % PV, soin massif une fois par étage + DEF +20 % pour l'étage.
    /// Remet à zéro la pénalité de soins de l'anesthésie après le soin (le cran de DR du vomit reste).
    /// </summary>
    public class MorganVomitHandler : ISpecialPassiveHandler
    {
        private const string VomitDefBuffId = "morgan_vomit_def";

        private readonly HashSet<CharacterBall> _triggeredThisStage = new HashSet<CharacterBall>();

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            CharacterBall owner = context.Owner;
            if (_triggeredThisStage.Contains(owner)) return;

            float hpRatio = owner.MaxHp > 0 ? (float)owner.CurrentHp / owner.MaxHp : 1f;
            if (hpRatio > 0.20f) return;

            _triggeredThisStage.Add(owner);

            int healAmount = Mathf.RoundToInt(owner.MaxHp * 0.60f);
            if (healAmount > 0)
                owner.Heal(healAmount);

            BuffReceiver br = owner.BuffReceiver;
            if (br != null)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = VomitDefBuffId,
                    Source = owner,
                    StatType = BuffStatType.DEF,
                    Value = 0.20f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }

            MorganAnesthesiaSystem system = owner.GetComponent<MorganAnesthesiaSystem>();
            if (system != null)
                system.ResetHealReduction();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            _triggeredThisStage.Remove(context.Owner);

            BuffReceiver br = context.Owner.BuffReceiver;
            if (br != null)
                br.RemoveBuffsById(VomitDefBuffId);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
