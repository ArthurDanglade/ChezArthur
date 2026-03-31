using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// "Mise gagnante" (leuk_gamble) :
    /// tirage au début du tour de Leuk pour des gains DEF (étage/run) et jackpot soin.
    /// </summary>
    public class LeukGambleHandler : ISpecialPassiveHandler
    {
        private const string StageDefBuffId = "leuk_def_stage";

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            LeukGambleSystem system = EnsureSystem(context.Owner);

            float roll = Random.value;
            if (roll < 0.50f)
                return; // 50% : rien

            if (roll < 0.75f)
            {
                // 25% : DEF +5% étage
                AddStageDef(context.Owner, 0.05f);
                return;
            }

            if (roll < 0.90f)
            {
                // 15% : DEF +5% run (cap 40%)
                if (system != null)
                    system.TryAddRunDef(0.05f);
                return;
            }

            // 10% : jackpot => DEF +10% run (cap) + heal 15%
            if (system != null)
                system.TryAddRunDef(0.10f);

            int heal = Mathf.RoundToInt(context.Owner.MaxHp * 0.15f);
            if (heal > 0)
                context.Owner.Heal(heal);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            BuffReceiver br = context.Owner.BuffReceiver;
            if (br != null)
                br.RemoveBuffsById(StageDefBuffId);

            EnsureSystem(context.Owner);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static LeukGambleSystem EnsureSystem(CharacterBall owner)
        {
            LeukGambleSystem system = owner.GetComponent<LeukGambleSystem>();
            if (system == null)
                system = owner.gameObject.AddComponent<LeukGambleSystem>();
            system.Initialize(owner);
            return system;
        }

        private static void AddStageDef(CharacterBall owner, float value)
        {
            BuffReceiver br = owner.BuffReceiver;
            if (br == null) return;

            br.AddBuff(new BuffData
            {
                BuffId = StageDefBuffId,
                Source = owner,
                StatType = BuffStatType.DEF,
                Value = value,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = false
            });
        }
    }
}

