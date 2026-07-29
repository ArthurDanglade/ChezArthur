using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise Vol de Vie : soigne l'attaquant (ou toute l'équipe en lv20) sur hit ennemi.
    /// Synergie Bouclier Infini : si déjà full HP avant le heal → 50 % en shield.
    /// </summary>
    public class VolDeVieHandler : IValiseEffectHandler
    {
        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise)
        {
            if (context == null || context.Trigger != ValiseTrigger.OnEnemyHit) return;
            if (valise == null) return;

            CharacterBall ally = context.SourceAlly;
            if (ally == null || context.TargetEnemy == null) return;

            int healAmount = Mathf.RoundToInt(context.DamageAmount * valise.GetTotalStatValue());
            if (healAmount <= 0) return;

            if (valise.IsLevel20Unlocked)
            {
                TurnManager turnManager = context.TurnManager;
                if (turnManager == null) return;
                IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
                if (allies == null) return;

                for (int i = 0; i < allies.Count; i++)
                {
                    CharacterBall target = allies[i];
                    if (target == null || target.IsDead) continue;
                    ApplyHealWithBouclierInfini(target, healAmount);
                }
                return;
            }

            ApplyHealWithBouclierInfini(ally, healAmount);
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise) { }

        private static void ApplyHealWithBouclierInfini(CharacterBall target, int healAmount)
        {
            bool wasFull = target.CurrentHp >= target.MaxHp;
            target.Heal(healAmount);
            BouclierInfini.TryRegenShield(target, healAmount, wasFull);
        }
    }
}
