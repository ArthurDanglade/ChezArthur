using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Inflige des dégâts fixes à tous les ennemis vivants à chaque mort d'ennemi.
    /// </summary>
    public class FauxDeLaMortHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnEnemyDeath) return;
            if (context.TurnManager == null) return;

            int splashDamage = Mathf.RoundToInt(item.Data.MainValue);
            IReadOnlyList<ITurnParticipant> participants = context.TurnManager.Participants;
            if (participants == null) return;

            int hitCount = 0;
            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant participant = participants[i];
                if (participant == null || participant.IsAlly || participant.IsDead) continue;

                Enemy enemy = participant as Enemy;
                if (enemy == null) continue;
                enemy.TakePureDamage(splashDamage);
                hitCount++;
            }

            if (hitCount > 0)
                Debug.Log($"[Item] {item.Data.ItemName} : {splashDamage} dégâts de zone ({hitCount} ennemi(s))");
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
