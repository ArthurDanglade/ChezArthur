using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Vulnérabilité permanente sur boss/mini-boss appliquée au début d'étage.
    /// </summary>
    public class MasseLourdeHandler : IItemEffectHandler
    {
        private const string BuffId = "masse_lourde_bonus";

        public void OnTriggered(ItemEffectContext context, ItemInstance item) { }

        public void OnStageStart(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null || context.TurnManager == null) return;

            IReadOnlyList<ITurnParticipant> participants = context.TurnManager.Participants;
            if (participants == null) return;

            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] == null || participants[i].IsAlly || participants[i].IsDead) continue;
                Enemy enemy = participants[i] as Enemy;
                if (enemy == null) continue;

                EnemyData data = enemy.Data;
                if (data == null) continue;

                bool isBossType = data.EnemyType == EnemyType.Boss || data.EnemyType == EnemyType.MiniBoss;
                bool isBossRole = data.EnemyRole == EnemyRole.Boss || data.EnemyRole == EnemyRole.MiniBoss;
                if (!isBossType && !isBossRole) continue;

                BuffReceiver buffReceiver = enemy.BuffReceiver;
                if (buffReceiver == null) continue;

                buffReceiver.AddBuff(new BuffData
                {
                    BuffId = BuffId,
                    Source = null,
                    StatType = BuffStatType.DamageAmplification,
                    Value = item.Data.MainValue,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
                Debug.Log($"[Item] {item.Data.ItemName} : vulnérabilité boss sur {enemy.Name} (+{Mathf.RoundToInt(item.Data.MainValue * 100f)}%)");
            }
        }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
