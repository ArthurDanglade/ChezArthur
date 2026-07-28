using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise Bouclier : shield % HP max à chaque début d'étage.
    /// Synergie Shield Dévastateur (avec Renvoi) : renvoie les dégâts absorbés par le shield.
    /// </summary>
    public class BouclierHandler : IValiseEffectHandler
    {
        public const string ValiseId = "valise_shield";
        public const string EffectId = "bouclier";
        public const string ShieldBuffId = "valise_bouclier_shield";
        public const string DevastatorSynergyId = "synergie_shield_renvoi";

        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise)
        {
            if (valise == null || context == null) return;

            TurnManager turnManager = context.TurnManager;
            if (turnManager == null) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            float ratio = valise.GetTotalStatValue();
            if (ratio <= 0f) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;

                int shieldAmount = Mathf.RoundToInt(ally.MaxHp * ratio);
                if (shieldAmount <= 0) continue;

                ally.BuffReceiver.RemoveBuffsById(ShieldBuffId);
                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = ShieldBuffId,
                    Source = null,
                    StatType = BuffStatType.Shield,
                    Value = shieldAmount,
                    IsPercent = false,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }

            Debug.Log($"[Valise] Bouclier : shield {ratio:P0} HP max appliqué à l'équipe");
        }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise) { }

        /// <summary>
        /// Synergie Shield Dévastateur : renvoie les dégâts absorbés par le shield.
        /// </summary>
        public static void TryDevastatorFromEnemyAttack(Enemy attacker, CharacterBall victim, int shieldAbsorbed)
        {
            if (attacker == null || victim == null || shieldAbsorbed <= 0) return;
            if (ValiseManager.Instance == null) return;

            bool synergyOk = SynergyManager.Instance != null &&
                             SynergyManager.Instance.IsSynergyActive(DevastatorSynergyId);
            bool bothActive = ValiseManager.Instance.IsValiseActive(ValiseId) &&
                              ValiseManager.Instance.IsValiseActive("valise_renvoi");

            if (!synergyOk && !bothActive) return;

            ValiseInstance renvoi = ValiseManager.Instance.GetActiveValise("valise_renvoi");
            if (renvoi == null) return;

            int reflect = Mathf.RoundToInt(shieldAbsorbed * renvoi.GetTotalStatValue());
            if (reflect <= 0) return;
            if (attacker.IsDead) return;

            attacker.TakePureDamage(reflect);
            Debug.Log($"[Valise] Shield Dévastateur : {reflect} dégâts renvoyés (absorbés {shieldAbsorbed})");
        }
    }
}
