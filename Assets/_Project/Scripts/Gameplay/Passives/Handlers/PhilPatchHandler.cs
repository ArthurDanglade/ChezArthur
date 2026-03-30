using ChezArthur.Characters;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;
using UnityEngine;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Patch correctif » (phil_patch) :
    /// - allié déjà Optimisé : soin 10% HP max + prolonge Optimisé d'1 tour,
    /// - allié non Optimisé : soin 3% HP max (Optimisé sera appliqué par phil_optimize).
    /// </summary>
    /// <remarks>
    /// Pour un comportement exact, PhilOptimize doit être évalué avant PhilPatch dans les slots passifs.
    /// </remarks>
    public class PhilPatchHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null || context.HitAlly == null) return;

            CharacterBall ally = context.HitAlly;
            BuffReceiver br = ally.BuffReceiver;
            if (br == null) return;

            bool alreadyOptimized = br.HasBuff(PhilOptimizeHandler.OptimizeAtkBuffId);
            if (alreadyOptimized)
            {
                int heal = Mathf.RoundToInt(ally.MaxHp * 0.10f);
                if (heal > 0)
                    ally.Heal(heal);

                br.ExtendBuffTurns(PhilOptimizeHandler.OptimizeAtkBuffId, 1);
                br.ExtendBuffTurns(PhilOptimizeHandler.OptimizeDefBuffId, 1);
            }
            else
            {
                int heal = Mathf.RoundToInt(ally.MaxHp * 0.03f);
                if (heal > 0)
                    ally.Heal(heal);
            }
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;
        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

