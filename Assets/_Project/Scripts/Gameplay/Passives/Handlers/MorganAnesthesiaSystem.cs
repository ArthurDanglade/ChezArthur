using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Compteur runtime « Anesthésie générale » : à chaque soin, +DR cumulé (cap 50 %) et -efficacité des soins (cap -30 %).
    /// Synchronise deux buffs sur le <see cref="BuffReceiver"/> de Morgan.
    /// </summary>
    public class MorganAnesthesiaSystem : MonoBehaviour
    {
        private const string DrBuffId = "morgan_dr";
        private const string HealDebuffId = "morgan_heal_debuff";

        private const float DrPerHeal = 0.02f;
        private const float MaxDr = 0.50f;
        private const float HealReductionPerHeal = 0.02f;
        private const float MaxHealReduction = 0.30f;

        private CharacterBall _owner;
        private float _currentDrBonus;
        private float _currentHealReduction;
        private bool _subscribed;

        public void Initialize(CharacterBall owner)
        {
            if (owner == null) return;

            _owner = owner;

            if (!_subscribed && _owner != null)
            {
                _owner.OnHealed += OnHealed;
                _subscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (_owner != null && _subscribed)
            {
                _owner.OnHealed -= OnHealed;
                _subscribed = false;
            }
        }

        private void OnHealed(int amount)
        {
            if (amount <= 0 || _owner == null) return;

            _currentDrBonus = Mathf.Min(_currentDrBonus + DrPerHeal, MaxDr);
            _currentHealReduction = Mathf.Min(_currentHealReduction + HealReductionPerHeal, MaxHealReduction);
            SyncBuffs();
        }

        /// <summary> Remet à zéro la pénalité de soins (appelé par le vomit). Le DR cumulé est conservé. </summary>
        public void ResetHealReduction()
        {
            _currentHealReduction = 0f;
            SyncBuffs();
        }

        /// <summary> Reset complet (nouvel étage). </summary>
        public void ResetAll()
        {
            _currentDrBonus = 0f;
            _currentHealReduction = 0f;
            if (_owner == null) return;

            BuffReceiver br = _owner.BuffReceiver;
            if (br != null)
            {
                br.RemoveBuffsById(DrBuffId);
                br.RemoveBuffsById(HealDebuffId);
            }
        }

        private void SyncBuffs()
        {
            if (_owner == null) return;

            BuffReceiver br = _owner.BuffReceiver;
            if (br == null) return;

            br.RemoveBuffsById(DrBuffId);
            br.RemoveBuffsById(HealDebuffId);

            if (_currentDrBonus > 0f)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = DrBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DamageReduction,
                    Value = _currentDrBonus,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }

            if (_currentHealReduction > 0f)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = HealDebuffId,
                    Source = _owner,
                    StatType = BuffStatType.HealReceived,
                    Value = -_currentHealReduction,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }
        }
    }
}
