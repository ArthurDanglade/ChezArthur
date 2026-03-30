using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Bonne étoile » (elfert_luck) : loterie de buff au contact allié (OnHitAlly).
    /// UniquePerSource évite le stacking sur le même allié si Elfert retouche dans le même tour.
    /// </summary>
    public class ElfertLuckHandler : ISpecialPassiveHandler
    {
        private const string BuffAtkId = "elfert_atk";
        private const string BuffDefId = "elfert_def";
        private const string BuffShieldId = "elfert_shield";

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitAlly == null) return;
            CharacterBall ally = context.HitAlly;
            CharacterBall owner = context.Owner;
            if (owner == null) return;

            float roll = Random.value;

            if (roll < 0.40f)
            {
                int heal = Mathf.RoundToInt(ally.MaxHp * 0.08f);
                if (heal > 0)
                    ally.Heal(heal);
                return;
            }

            BuffReceiver br = ally.BuffReceiver;
            if (br == null) return;

            if (roll < 0.70f)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = BuffAtkId,
                    Source = owner,
                    StatType = BuffStatType.ATK,
                    Value = 0.15f,
                    IsPercent = true,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
                return;
            }

            if (roll < 0.90f)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = BuffDefId,
                    Source = owner,
                    StatType = BuffStatType.DEF,
                    Value = 0.15f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = 1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
                return;
            }

            br.AddBuff(new BuffData
            {
                BuffId = BuffShieldId,
                Source = owner,
                StatType = BuffStatType.Shield,
                Value = 100f,
                IsPercent = false,
                RemainingTurns = -1,
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
