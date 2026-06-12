using UnityEngine;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Applique une brûlure conditionnelle selon la vélocité du lancer.
    /// </summary>
    public class BouleDeFeuHandler : IItemEffectHandler
    {
        private const float MIN_VELOCITY_RATIO = 0.80f;
        private const int BURN_TURNS = 3;
        private bool _loggedVelocityThisLaunch;

        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;

            if (context.Trigger == ItemTrigger.OnAllyLaunch)
            {
                _loggedVelocityThisLaunch = false;
                return;
            }

            if (context.Trigger != ItemTrigger.OnEnemyHit) return;
            if (context.TargetEnemy == null) return;

            if (!_loggedVelocityThisLaunch && context.SourceAlly != null)
            {
                float v = context.SourceAlly.CurrentVelocity;
                float max = context.SourceAlly.LaunchSpeedThisLaunch;
                float pct = max > 0f ? (v / max) * 100f : 0f;
                bool seuilAtteint = pct >= MIN_VELOCITY_RATIO * 100f;
                Debug.Log($"[Item] {item.Data.ItemName} : vélocité {v:F2}/{max:F2} ({pct:F0}%) — seuil 80% {(seuilAtteint ? "atteint" : "raté")}");
                _loggedVelocityThisLaunch = true;
            }

            if (context.VelocityRatio < MIN_VELOCITY_RATIO) return;

            BuffReceiver buffReceiver = context.TargetEnemy.BuffReceiver;
            if (buffReceiver == null) return;

            buffReceiver.AddBuff(new BuffData
            {
                BuffId = "boule_de_feu_burn",
                Source = context.SourceAlly,
                StatType = BuffStatType.DamageAmplification,
                Value = 0f,
                IsPercent = false,
                RemainingTurns = BURN_TURNS,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = false
            });
            Debug.Log($"[Item] {item.Data.ItemName} : brûlure appliquée à {context.TargetEnemy.Name} ({BURN_TURNS} tours)");
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item)
        {
            _loggedVelocityThisLaunch = false;
        }

        public void OnRunStart(ItemEffectContext context, ItemInstance item)
        {
            _loggedVelocityThisLaunch = false;
        }
    }
}
