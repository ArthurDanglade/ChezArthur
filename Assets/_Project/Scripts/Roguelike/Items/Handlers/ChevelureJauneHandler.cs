using System.Collections.Generic;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Renforce les alliés survivants après la mort d'un allié.
    /// </summary>
    public class ChevelureJauneHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnAllyDeath) return;
            if (context.TurnManager == null) return;

            IReadOnlyList<CharacterBall> allies = context.TurnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally == context.SourceAlly) continue;
                if (ally.BuffReceiver == null) continue;

                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = "chevelure_jaune_atk",
                    Source = context.SourceAlly,
                    StatType = BuffStatType.ATK,
                    Value = item.Data.MainValue,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = false
                });

                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = "chevelure_jaune_def",
                    Source = context.SourceAlly,
                    StatType = BuffStatType.DEF,
                    Value = item.Data.MainValue,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = false
                });
            }
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
