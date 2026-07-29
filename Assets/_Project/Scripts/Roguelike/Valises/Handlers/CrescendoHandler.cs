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
        /// <summary>
        /// Plafond du bonus Crescendo par Super (évite explosion physique au lvl 99 Légendaire).
        /// Calibrable plus tard — valeur de sécurité provisoire.
        /// </summary>
        private const float MAX_CRESCENDO_BONUS = 1.25f;

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
            float rawBonus = _consecutiveSupers * bonusPerSuper;
            if (rawBonus <= 0f) return;

            float bonus = Mathf.Min(rawBonus, MAX_CRESCENDO_BONUS);
            SuperLancerSystem.Instance?.AddPendingLaunchBonus(bonus);
            if (bonus < rawBonus)
                Debug.Log($"[Valise] Crescendo série x{_consecutiveSupers} → LaunchForce +{bonus:P0} (cappé, brut {rawBonus:P0})");
            else
                Debug.Log($"[Valise] Crescendo série x{_consecutiveSupers} → LaunchForce +{bonus:P0}");
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise)
        {
            _consecutiveSupers = 0;
        }
    }
}
