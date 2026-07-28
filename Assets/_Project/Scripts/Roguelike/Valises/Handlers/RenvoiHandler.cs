using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise Renvoi : renvoie une partie des dégâts reçus à l'attaquant (ou AoE lv20).
    /// Synergie Vol de Vie : soigne la victime du montant renvoyé.
    /// </summary>
    public class RenvoiHandler : IValiseEffectHandler
    {
        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise)
        {
            if (context == null || context.Trigger != ValiseTrigger.OnAllyDamagedByEnemy)
                return;
            if (valise == null || ValiseManager.Instance == null) return;

            Enemy attacker = context.TargetEnemy;
            CharacterBall victim = context.SourceAlly;
            int damageReceived = context.DamageAmount;
            if (attacker == null || victim == null || damageReceived <= 0) return;

            int renvoiDamage = Mathf.RoundToInt(damageReceived * valise.GetTotalStatValue());
            if (renvoiDamage <= 0) return;

            if (valise.IsLevel20Unlocked)
            {
                TurnManager turnManager = context.TurnManager;
                if (turnManager == null) return;
                IReadOnlyList<ITurnParticipant> participants = turnManager.Participants;
                if (participants == null) return;

                for (int i = 0; i < participants.Count; i++)
                {
                    ITurnParticipant participant = participants[i];
                    if (participant == null || participant.IsAlly || participant.IsDead) continue;
                    Enemy enemy = participant as Enemy;
                    if (enemy == null || enemy.IsDead) continue;
                    enemy.TakePureDamage(renvoiDamage);
                }

                Debug.Log($"[Valise] Renvoi : {renvoiDamage} dégâts renvoyés");
                return;
            }

            if (attacker.IsDead) return;

            attacker.TakePureDamage(renvoiDamage);
            Debug.Log($"[Valise] Renvoi : {renvoiDamage} dégâts renvoyés");

            if (ValiseManager.Instance.IsValiseActive("valise_vol_de_vie"))
                victim.Heal(renvoiDamage);
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise) { }
    }
}
