using ChezArthur.Characters;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Mise à jour disponible » (phil_optimize) :
    /// applique l'état Optimisé (ATK/DEF +20% 2 tours) et déclenche un bonus d'équipe
    /// si tous les alliés vivants sont Optimisés.
    /// </summary>
    public class PhilOptimizeHandler : ISpecialPassiveHandler
    {
        public const string OptimizeAtkBuffId = "phil_optimize_atk";
        public const string OptimizeDefBuffId = "phil_optimize_def";
        private const string TeamBonusAtkBuffId = "phil_team_atk";
        private const string TeamBonusDefBuffId = "phil_team_def";

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null || context.HitAlly == null) return;
            if (context.TurnManager == null) return;

            CharacterBall owner = context.Owner;
            CharacterBall hitAlly = context.HitAlly;
            BuffReceiver hitBr = hitAlly.BuffReceiver;
            if (hitBr == null) return;

            // État Optimisé sur l'allié touché.
            hitBr.AddBuff(new BuffData
            {
                BuffId = OptimizeAtkBuffId,
                Source = owner,
                StatType = BuffStatType.ATK,
                Value = 0.20f,
                IsPercent = true,
                RemainingTurns = 2,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            hitBr.AddBuff(new BuffData
            {
                BuffId = OptimizeDefBuffId,
                Source = owner,
                StatType = BuffStatType.DEF,
                Value = 0.20f,
                IsPercent = true,
                RemainingTurns = 2,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            var allies = context.TurnManager.GetAllies();
            if (allies == null) return;

            bool allOptimized = true;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                if (ally.BuffReceiver == null || !ally.BuffReceiver.HasBuff(OptimizeAtkBuffId))
                {
                    allOptimized = false;
                    break;
                }
            }

            // Bonus « tant que » : réappliqué à chaque OnHitAlly si la condition est vraie.
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;

                BuffReceiver br = ally.BuffReceiver;
                if (allOptimized)
                {
                    br.AddBuff(new BuffData
                    {
                        BuffId = TeamBonusAtkBuffId,
                        Source = owner,
                        StatType = BuffStatType.ATK,
                        Value = 0.05f,
                        IsPercent = true,
                        RemainingTurns = 1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });

                    br.AddBuff(new BuffData
                    {
                        BuffId = TeamBonusDefBuffId,
                        Source = owner,
                        StatType = BuffStatType.DEF,
                        Value = 0.05f,
                        IsPercent = true,
                        RemainingTurns = 1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });
                }
                else
                {
                    br.RemoveBuffsById(TeamBonusAtkBuffId);
                    br.RemoveBuffsById(TeamBonusDefBuffId);
                }
            }
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;
        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

