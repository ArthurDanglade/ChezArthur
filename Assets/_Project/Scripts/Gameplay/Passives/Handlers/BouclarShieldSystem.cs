using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime Bouclar :
    /// - applique les boucliers d'équipe en début d'étage,
    /// - maintient le bonus DEF de Bouclar tant qu'un bouclier équipe est actif,
    /// - recharge les boucliers en mode "Réparation express".
    /// </summary>
    public class BouclarShieldSystem : MonoBehaviour
    {
        private const string ShieldBuffId = "bouclar_shield";
        private const string DefBonusBuffId = "bouclar_def_bonus";

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private bool _enhanced;

        private readonly Dictionary<CharacterBall, float> _maxShieldByAlly = new Dictionary<CharacterBall, float>(8);
        private readonly HashSet<CharacterBall> _rechargedThisTurn = new HashSet<CharacterBall>();
        private bool _subscribedToTurnChanged;

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (owner == null) return;

            if (_subscribedToTurnChanged && _turnManager != null && _turnManager != turnManager)
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
                _subscribedToTurnChanged = false;
            }

            _owner = owner;
            _turnManager = turnManager;

            if (!_subscribedToTurnChanged && _turnManager != null)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
                _subscribedToTurnChanged = true;
            }
        }

        public void SetEnhanced(bool value)
        {
            _enhanced = value;
        }

        public void ApplyShieldsToTeam()
        {
            if (_owner == null || _turnManager == null) return;

            var allies = _turnManager.GetAllies();
            if (allies == null) return;

            _maxShieldByAlly.Clear();
            _rechargedThisTurn.Clear();

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;

                float shieldAmount = Mathf.RoundToInt(ally.MaxHp * 0.15f);
                ally.BuffReceiver.RemoveBuffsById(ShieldBuffId);
                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = ShieldBuffId,
                    Source = _owner,
                    StatType = BuffStatType.Shield,
                    Value = shieldAmount,
                    IsPercent = false,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });

                _maxShieldByAlly[ally] = shieldAmount;
            }

            EnsureOwnerDefBonus();
        }

        public void TryRechargeShield(CharacterBall ally)
        {
            if (!_enhanced || ally == null || ally.IsDead) return;
            if (_owner == null || _owner.BuffReceiver == null) return;
            if (ally.BuffReceiver == null) return;
            if (_rechargedThisTurn.Contains(ally)) return;

            _rechargedThisTurn.Add(ally);

            float maxShield = _maxShieldByAlly.TryGetValue(ally, out float maxStored)
                ? maxStored
                : Mathf.RoundToInt(ally.MaxHp * 0.15f);

            float ownerHpRatio = _owner.MaxHp > 0 ? (float)_owner.CurrentHp / _owner.MaxHp : 1f;
            if (ownerHpRatio < 0.20f)
            {
                // Sous 20% HP : restauration complète.
                ally.BuffReceiver.RemoveBuffsById(ShieldBuffId);
                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = ShieldBuffId,
                    Source = _owner,
                    StatType = BuffStatType.Shield,
                    Value = maxShield,
                    IsPercent = false,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
                return;
            }

            // Recharge partielle +5% HP max (cap max initial).
            float rechargeAmount = Mathf.RoundToInt(ally.MaxHp * 0.05f);
            var buffs = ally.BuffReceiver.ActiveBuffs;
            for (int i = 0; i < buffs.Count; i++)
            {
                BuffData b = buffs[i];
                if (b == null || b.BuffId != ShieldBuffId || b.StatType != BuffStatType.Shield) continue;
                b.Value = Mathf.Min(b.Value + rechargeAmount, maxShield);
                return;
            }

            // Si plus de buff shield actif, recrée un shield partiel.
            ally.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = ShieldBuffId,
                Source = _owner,
                StatType = BuffStatType.Shield,
                Value = Mathf.Min(rechargeAmount, maxShield),
                IsPercent = false,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null) return;
            if (!ReferenceEquals(participant, _owner)) return;

            _rechargedThisTurn.Clear();
            CheckShieldStatus();
        }

        private void CheckShieldStatus()
        {
            if (_owner == null || _turnManager == null || _owner.BuffReceiver == null) return;
            var allies = _turnManager.GetAllies();
            if (allies == null) return;

            bool anyShieldActive = false;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;
                if (ally.BuffReceiver.HasBuff(ShieldBuffId))
                {
                    anyShieldActive = true;
                    break;
                }
            }

            if (!anyShieldActive)
            {
                _owner.BuffReceiver.RemoveBuffsById(DefBonusBuffId);
                return;
            }

            EnsureOwnerDefBonus();
        }

        private void EnsureOwnerDefBonus()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            if (!_owner.BuffReceiver.HasBuff(DefBonusBuffId))
            {
                _owner.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = DefBonusBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DEF,
                    Value = 0.50f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }
        }

        private void OnDestroy()
        {
            if (_subscribedToTurnChanged && _turnManager != null)
                _turnManager.OnTurnChanged -= OnTurnChanged;
        }
    }
}

