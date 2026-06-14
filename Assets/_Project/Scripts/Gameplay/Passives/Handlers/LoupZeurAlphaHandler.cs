using System.Collections.Generic;
using System.Text;
using ChezArthur.Characters;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;
using UnityEngine;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Handler pour « Alpha ou pas » (loupzeur_alpha).
    /// 0 SSR/LR vivant → +50 % ATK/DEF sur Loup Zeur ; ≥ 1 → +50 % ATK/DEF sur chaque allié SSR/LR (pas Loup Zeur).
    /// Buffs permanents recomputés à chaque début d'étage.
    /// </summary>
    public class LoupZeurAlphaHandler : ISpecialPassiveHandler
    {
        private const string AlphaAtkBuffId = "loupzeur_alpha_atk";
        private const string AlphaDefBuffId = "loupzeur_alpha_def";
        private const float ALPHA_BONUS = 0.5f;

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            // Deux PassiveData partagent loupzeur_alpha (ATK + DEF) : une seule application par étage.
            if (passiveData.Effect != PassiveEffect.BuffATK) return;

            ApplyAlphaBonus(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static void ApplyAlphaBonus(PassiveContext context)
        {
            if (context.Owner == null || context.TurnManager == null) return;

            CharacterBall owner = context.Owner;
            IReadOnlyList<CharacterBall> allies = context.TurnManager.GetAllies();
            if (allies == null) return;

            ClearAlphaBuffsFromTeam(allies);

            var ssrOrLrAllies = new List<CharacterBall>(4);
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;

                CharacterData data = ally.Data;
                if (data == null) continue;

                if (data.Rarity == CharacterRarity.SSR || data.Rarity == CharacterRarity.LR)
                    ssrOrLrAllies.Add(ally);
            }

            var logTargets = new StringBuilder(64);

            if (ssrOrLrAllies.Count == 0)
            {
                ApplyAlphaBuffs(owner, owner);
                logTargets.Append(owner.name);
            }
            else
            {
                for (int i = 0; i < ssrOrLrAllies.Count; i++)
                {
                    CharacterBall target = ssrOrLrAllies[i];
                    if (target == owner) continue;

                    ApplyAlphaBuffs(target, owner);

                    if (logTargets.Length > 0)
                        logTargets.Append(", ");
                    logTargets.Append(target.name);
                }
            }

            Debug.Log($"[Passif] Loup Zeur : Alpha → {logTargets}");
        }

        private static void ClearAlphaBuffsFromTeam(IReadOnlyList<CharacterBall> allies)
        {
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.BuffReceiver == null) continue;

                ally.BuffReceiver.RemoveBuffsById(AlphaAtkBuffId);
                ally.BuffReceiver.RemoveBuffsById(AlphaDefBuffId);
            }
        }

        private static void ApplyAlphaBuffs(CharacterBall target, CharacterBall source)
        {
            BuffReceiver br = target.BuffReceiver;
            if (br == null) return;

            br.AddBuff(new BuffData
            {
                BuffId = AlphaAtkBuffId,
                Source = source,
                StatType = BuffStatType.ATK,
                Value = ALPHA_BONUS,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            br.AddBuff(new BuffData
            {
                BuffId = AlphaDefBuffId,
                Source = source,
                StatType = BuffStatType.DEF,
                Value = ALPHA_BONUS,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }
    }
}
