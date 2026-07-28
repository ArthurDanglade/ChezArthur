using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise « La pression je la bois » :
    /// 1) accélère la montée de jauge sur les tours ATK ;
    /// 2) bonus ATK proportionnel à la hauteur de jauge (plafonné).
    /// La logique live est consultée via helpers statiques (PressureGauge / CharacterBall).
    /// </summary>
    public class PressionJeLaBoisHandler : IValiseEffectHandler
    {
        public const string ValiseId = "valise_pression_je_la_bois";
        public const string EffectId = "pression_je_la_bois";

        /// <summary> Plafond ATK à jauge pleine (doc — calibrable). </summary>
        public const float MaxAtkBonusAtFullGauge = 0.30f;

        /// <summary> Descente jauge sur Super Lancer (synergie Crescendo+Furie). </summary>
        public const float SuperLancerPressureDrop = 4f;

        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise) { }

        /// <summary>
        /// Multiplicateur de montée pour un tour ATK allié (1 = neutre).
        /// </summary>
        public static float GetAllyAtkRiseMultiplier()
        {
            ValiseInstance instance = GetActiveInstance();
            if (instance == null) return 1f;

            float bonus = instance.GetTotalStatValue();
            if (bonus < 0f) bonus = 0f;
            return 1f + bonus;
        }

        /// <summary>
        /// Bonus ATK % courant selon la hauteur de jauge (0 si valise absente / pas de jauge).
        /// </summary>
        public static float GetCurrentAtkBonusPercent()
        {
            if (GetActiveInstance() == null) return 0f;

            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure == null) return 0f;

            return pressure.NormalizedValue * MaxAtkBonusAtFullGauge;
        }

        /// <summary>
        /// Synergie Crescendo + Mode Furie : descend la jauge hors Rupture.
        /// </summary>
        public static void TryDecreaseOnSuperLancer()
        {
            SynergyManager synergy = SynergyManager.Instance;
            if (synergy == null || !synergy.IsSynergyActive("synergie_crescendo_mode_furie"))
                return;

            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure == null || pressure.IsInRupture)
                return;

            pressure.Decrease(SuperLancerPressureDrop, "synergie Super Lancer (Bullet Time)");
        }

        private static ValiseInstance GetActiveInstance()
        {
            if (ValiseManager.Instance == null) return null;
            return ValiseManager.Instance.GetActiveValise(ValiseId);
        }
    }
}
