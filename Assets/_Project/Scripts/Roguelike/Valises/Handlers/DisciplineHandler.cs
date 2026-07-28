using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise Discipline : stacks personnels si la spé reste identique d'un tour à l'autre.
    /// </summary>
    public class DisciplineHandler : IValiseEffectHandler
    {
        private readonly Dictionary<CharacterBall, int> _stacksByAlly = new Dictionary<CharacterBall, int>();

        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise)
        {
            if (context == null || valise == null) return;

            if (context.Trigger == ValiseTrigger.OnAllyTurnStart)
            {
                CharacterBall ally = context.SourceAlly;
                if (ally == null) return;

                if (context.HasPreviousTurn &&
                    ReferenceEquals(context.CurrentSpec, context.PreviousSpec))
                {
                    if (!_stacksByAlly.TryGetValue(ally, out int stacks))
                        stacks = 0;
                    stacks++;
                    _stacksByAlly[ally] = stacks;
                    ApplyPersonalModifiers(ally, valise, stacks);
                }
                return;
            }

            if (context.Trigger == ValiseTrigger.OnValiseChanged)
                RefreshAll(context.TurnManager, valise);
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise)
        {
            _stacksByAlly.Clear();
        }

        private void ApplyPersonalModifiers(CharacterBall ally, ValiseInstance valise, int stacks)
        {
            if (ally == null || valise == null) return;

            float bonusPercent = stacks * valise.AccumulatedValue;
            ally.SetPersonalDisciplineStacks(stacks);
            ally.SetPersonalValiseModifier(ValiseStatType.ATK, bonusPercent);
            ally.SetPersonalValiseModifier(ValiseStatType.DEF, bonusPercent);
            Debug.Log($"[Valise] Discipline {ally.Name} stacks: {stacks}");
        }

        private void RefreshAll(TurnManager turnManager, ValiseInstance valise)
        {
            if (turnManager == null || valise == null) return;
            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                if (!_stacksByAlly.TryGetValue(ally, out int stacks) || stacks <= 0)
                    continue;
                ApplyPersonalModifiers(ally, valise, stacks);
            }
        }
    }
}
