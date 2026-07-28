using System;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise Mode Furie : jauge Super nets → prochain Super dévastateur (LF + ATK fixes).
    /// Le seuil scale avec le niveau ; la puissance du burst ne scale pas.
    /// </summary>
    public class ModeFurieHandler : IValiseEffectHandler
    {
        private const int BASE_THRESHOLD = 8;
        private const int MIN_THRESHOLD = 3;
        private const float DEVASTATION_LAUNCH_FORCE = 0.60f;
        private const float DEVASTATION_ATK = 0.40f;

        private int _gauge;
        private bool _readyForDevastation;
        private int _cachedThreshold = BASE_THRESHOLD;

        /// <summary> Charge actuelle / seuil (pour HUD). </summary>
        public event Action<int, int, bool> OnGaugeChanged;

        public int Gauge => _gauge;
        public int Threshold => _cachedThreshold;
        public bool IsReady => _readyForDevastation;

        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise)
        {
            if (context == null || valise == null) return;

            RefreshThreshold(valise);

            if (context.Trigger == ValiseTrigger.OnNormalLaunch)
            {
                // Jauge pleine : le miss ne consomme PAS l'effet et ne descend pas.
                if (_readyForDevastation)
                {
                    EmitGauge();
                    return;
                }

                if (_gauge > 0)
                {
                    _gauge--;
                    EmitGauge();
                }
                return;
            }

            if (context.Trigger == ValiseTrigger.OnValiseChanged)
            {
                RefreshThreshold(valise);
                if (_gauge > _cachedThreshold)
                    _gauge = _cachedThreshold;
                if (_gauge >= _cachedThreshold)
                    _readyForDevastation = true;
                EmitGauge();
                return;
            }

            if (context.Trigger != ValiseTrigger.OnSuperLancer) return;

            if (_readyForDevastation)
            {
                ApplyDevastation(context.SourceAlly);
                _gauge = 0;
                _readyForDevastation = false;
                EmitGauge();
                return;
            }

            _gauge++;
            if (_gauge >= _cachedThreshold)
            {
                _gauge = _cachedThreshold;
                _readyForDevastation = true;
                Debug.Log("[Valise] Mode Furie : jauge pleine — prochain Super dévastateur");
            }

            EmitGauge();
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise)
        {
            _gauge = 0;
            _readyForDevastation = false;
            if (valise != null)
                RefreshThreshold(valise);
            else
                _cachedThreshold = BASE_THRESHOLD;
            EmitGauge();
        }

        /// <summary>
        /// Force un refresh HUD (ex. à l'activation de la valise).
        /// </summary>
        public void NotifyUi()
        {
            EmitGauge();
        }

        private void ApplyDevastation(CharacterBall ally)
        {
            SuperLancerSystem.Instance?.AddPendingLaunchBonus(DEVASTATION_LAUNCH_FORCE);
            if (ally != null)
                ally.SetNextLaunchAtkBonus(DEVASTATION_ATK);

            Debug.Log($"[Valise] Mode Furie DÉVASTATEUR ! LF +{DEVASTATION_LAUNCH_FORCE:P0}, ATK +{DEVASTATION_ATK:P0}");
        }

        private void RefreshThreshold(ValiseInstance valise)
        {
            // AccumulatedValue = points de réduction (baseValuePerLevel × rareté).
            // Niveau 1 commune (1) → seuil 8 ; AccumulatedValue 6+ → seuil 3.
            int reduction = Mathf.Max(0, Mathf.RoundToInt(valise.AccumulatedValue) - 1);
            _cachedThreshold = Mathf.Max(MIN_THRESHOLD, BASE_THRESHOLD - reduction);
        }

        private void EmitGauge()
        {
            OnGaugeChanged?.Invoke(_gauge, _cachedThreshold, _readyForDevastation);
        }
    }
}
