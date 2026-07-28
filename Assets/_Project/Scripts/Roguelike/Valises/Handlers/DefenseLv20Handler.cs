using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Effet niveau 20 Valise Défense : transfert partiel des dégâts vers les Défenseurs.
    /// </summary>
    public class DefenseLv20Handler : IValiseEffectHandler
    {
        private bool _isApplyingDefenseTransfer;

        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise)
        {
            if (context == null || context.Trigger != ValiseTrigger.OnAllyTakeDamage) return;
            if (valise == null || !valise.IsLevel20Unlocked) return;
            if (_isApplyingDefenseTransfer) return;

            CharacterBall victim = context.SourceAlly;
            int damage = context.DamageAmount;
            // BoolFlag = LastDamageWasContact (ignoré pour le transfert).
            if (context.BoolFlag || victim == null || damage <= 0) return;

            TurnManager turnManager = context.TurnManager;
            if (turnManager == null) return;

            int absorbed = Mathf.RoundToInt(damage * 0.10f);
            if (absorbed <= 0) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            _isApplyingDefenseTransfer = true;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally == victim) continue;
                if (ally.Data == null || ally.Data.Role != CharacterRole.Defender) continue;
                ally.TakeDamage(absorbed);
            }
            _isApplyingDefenseTransfer = false;
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise)
        {
            _isApplyingDefenseTransfer = false;
        }
    }
}
