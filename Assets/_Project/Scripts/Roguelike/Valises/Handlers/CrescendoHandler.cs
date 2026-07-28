using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise Crescendo : bonus LaunchForce croissant sur une série de Super Lancers consécutifs.
    /// Un lancer normal brise la série.
    /// </summary>
    public class CrescendoHandler : IValiseEffectHandler
    {
        private int _consecutiveSupers;

        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise)
        {
            if (context == null || valise == null) return;

            if (context.Trigger == ValiseTrigger.OnNormalLaunch)
            {
                if (_consecutiveSupers > 0)
                    Debug.Log("[Valise] Crescendo : série rompue");
                _consecutiveSupers = 0;
                return;
            }

            if (context.Trigger != ValiseTrigger.OnSuperLancer) return;

            _consecutiveSupers++;
            float bonusPerSuper = valise.AccumulatedValue;
            float bonus = _consecutiveSupers * bonusPerSuper;
            if (bonus <= 0f) return;

            SuperLancerSystem.Instance?.AddPendingLaunchBonus(bonus);
            Debug.Log($"[Valise] Crescendo série x{_consecutiveSupers} → LaunchForce +{bonus:P0}");
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise)
        {
            _consecutiveSupers = 0;
        }
    }
}
