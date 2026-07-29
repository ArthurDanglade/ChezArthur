using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Synergie Bouclier Infini (Bouclier + Vol de Vie) :
    /// si le perso est déjà full HP avant le hit, 50 % du heal Vol de Vie régénère le shield.
    /// </summary>
    public static class BouclierInfini
    {
        public const string SynergyId = "synergie_bouclier_infini";
        public const string ShieldBuffId = "valise_bouclier_infini_shield";
        private const float ShieldHealRatio = 0.5f;

        /// <summary>
        /// Applique la regen shield si la synergie est active et que wasFullHpBeforeHeal.
        /// </summary>
        public static void TryRegenShield(CharacterBall ally, int volDeVieHealAmount, bool wasFullHpBeforeHeal)
        {
            if (ally == null || ally.IsDead || ally.BuffReceiver == null) return;
            if (!wasFullHpBeforeHeal) return;
            if (volDeVieHealAmount <= 0) return;
            if (!IsActive()) return;

            int shieldGain = Mathf.RoundToInt(volDeVieHealAmount * ShieldHealRatio);
            if (shieldGain <= 0) return;

            AddOrStackShield(ally.BuffReceiver, shieldGain);
            Debug.Log($"[Valise] Bouclier Infini : +{shieldGain} shield sur {ally.Name} (VdV {volDeVieHealAmount})");
        }

        public static bool IsActive()
        {
            if (ValiseManager.Instance == null) return false;

            bool synergyOk = SynergyManager.Instance != null &&
                             SynergyManager.Instance.IsSynergyActive(SynergyId);
            bool bothActive = ValiseManager.Instance.IsValiseActive(BouclierHandler.ValiseId) &&
                              ValiseManager.Instance.IsValiseActive("valise_vol_de_vie");
            return synergyOk || bothActive;
        }

        private static void AddOrStackShield(BuffReceiver receiver, int amount)
        {
            // Empile sur un buff shield existant (Bouclier d'étage ou Infini) si possible.
            var buffs = receiver.ActiveBuffs;
            if (buffs != null)
            {
                for (int i = 0; i < buffs.Count; i++)
                {
                    BuffData b = buffs[i];
                    if (b == null || b.StatType != BuffStatType.Shield) continue;
                    b.Value += amount;
                    receiver.NotifyBuffsChanged();
                    return;
                }
            }

            receiver.AddBuff(new BuffData
            {
                BuffId = ShieldBuffId,
                Source = null,
                StatType = BuffStatType.Shield,
                Value = amount,
                IsPercent = false,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }
    }
}
