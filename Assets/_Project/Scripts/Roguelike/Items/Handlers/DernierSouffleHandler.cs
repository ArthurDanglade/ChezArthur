using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Déclenche une explosion de zone quand un allié meurt.
    /// </summary>
    public class DernierSouffleHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnAllyDeath) return;
            if (context.SourceAlly == null) return;
            if (context.TurnManager == null) return;

            int explosionDamage = Mathf.RoundToInt(context.SourceAlly.MaxHp * item.Data.MainValue);
            if (explosionDamage <= 0) return;

            IReadOnlyList<ITurnParticipant> participants = context.TurnManager.Participants;
            if (participants == null) return;

            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant participant = participants[i];
                if (participant == null || participant.IsAlly || participant.IsDead) continue;

                Enemy enemy = participant as Enemy;
                if (enemy == null) continue;
                enemy.TakeDamage(explosionDamage);
            }
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
