using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système central Morre Voeux : immortalité, transfert, sacrifice d'aura, rage et délabrement.
    /// </summary>
    public class MorreVoeuxSystem : MonoBehaviour
    {
        private const string HpBonusBuffId = "morre_rez_hp_flat";
        private const string AtkFlatBuffId = "morre_rez_atk_flat";
        private const string RageAtkBuffId = "morre_rage_atk";
        private const string SacrificeAtkBuffId = "morre_sacrifice_atk";
        private const string SacrificeDefBuffId = "morre_sacrifice_def";
        private const string SacrificeLfBuffId = "morre_sacrifice_lf";
        private const string SacrificeSwitchAtkBuffId = "morre_sacrifice_switch_atk";
        private const string SacrificeSwitchDefBuffId = "morre_sacrifice_switch_def";
        private const string DecayDmgAmpBuffId = "morre_decay_amp";
        private const string DecaySwitchAtkBuffId = "morre_decay_switch_atk";

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private CharacterPassiveRuntime _runtime;

        private int _resurrectionCount;
        private int _bonusMaxHp;
        private int _permanentAtkFlat;

        private int _rageCounter;
        private bool _rageBoosted;

        private bool _transferUsedThisCycle;
        private bool _freeTransferActive;

        private bool _decayActive;

        private bool _subscribedToTurnChanged;
        private bool _subscribedToOwnerStopped;

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (owner == null) return;

            if (_subscribedToTurnChanged && _turnManager != null && _turnManager != turnManager)
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
                _subscribedToTurnChanged = false;
            }
            if (_subscribedToOwnerStopped && _owner != null && !ReferenceEquals(_owner, owner))
            {
                _owner.OnStopped -= OnOwnerStopped;
                _subscribedToOwnerStopped = false;
            }

            _owner = owner;
            _turnManager = turnManager;
            _runtime = _owner.GetComponent<CharacterPassiveRuntime>();

            if (!_subscribedToTurnChanged && _turnManager != null)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
                _subscribedToTurnChanged = true;
            }
            if (!_subscribedToOwnerStopped)
            {
                _owner.OnStopped += OnOwnerStopped;
                _subscribedToOwnerStopped = true;
            }

            SyncPermanentBuffs();
            SyncDecayBuff();
            RefreshSacrificeAuras();
        }

        public bool TryResurrect()
        {
            if (_owner == null || _turnManager == null) return false;
            if (IsLastAliveAlly()) return false;

            _resurrectionCount++;
            _bonusMaxHp++;

            if (IsAtkSpecActive())
                _permanentAtkFlat += _decayActive ? 10 : 5;

            SyncPermanentBuffs();
            _owner.Revive(1.0f);
            return true;
        }

        public float GetHpMaxBonus() => _bonusMaxHp;
        public float GetAtkFlatBonus() => _permanentAtkFlat;

        public void AddRage(int damage)
        {
            if (!IsAtkSpecActive()) return;
            if (damage <= 0) return;
            _rageCounter += damage;
        }

        public void EnableDecay(bool enabled)
        {
            _decayActive = enabled;
            SyncDecayBuff();
        }

        public void ApplyRageSwitchBonus()
        {
            if (!IsAtkSpecActive()) return;
            _rageBoosted = true;
        }

        public void ApplyTransferSwitchBonus()
        {
            if (!IsSupSpecActive()) return;
            _freeTransferActive = true;
        }

        public void ApplySacrificeSwitchBonus()
        {
            if (!IsSupSpecActive() || _owner == null || _owner.BuffReceiver == null) return;

            IReadOnlyList<CharacterBall> allies = _turnManager != null ? _turnManager.GetAllies() : null;
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null || ReferenceEquals(ally, _owner)) continue;
                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = SacrificeSwitchAtkBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = 0.05f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = 1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = SacrificeSwitchDefBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DEF,
                    Value = 0.05f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = 1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
        }

        public void ApplyDecaySwitchBonus()
        {
            if (!IsAtkSpecActive() || _owner == null || _owner.BuffReceiver == null) return;
            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = DecaySwitchAtkBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = 0.10f,
                IsPercent = true,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null || _turnManager == null) return;

            // Vérifie le transfert sur chaque changement de tour.
            TryTransferToLowHealthAlly();
            RefreshSacrificeAuras();
            SyncDecayBuff();

            if (!ReferenceEquals(participant, _owner)) return;

            ApplyRageAtTurnStart();
            _transferUsedThisCycle = false;
        }

        private void OnOwnerStopped()
        {
            _rageBoosted = false;
            _freeTransferActive = false;
        }

        private void TryTransferToLowHealthAlly()
        {
            if (!IsSupSpecActive()) return;
            if (_transferUsedThisCycle) return;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ReferenceEquals(ally, _owner)) continue;
                if (ally.MaxHp <= 0) continue;

                float ratio = (float)ally.CurrentHp / ally.MaxHp;
                if (ratio >= 0.50f) continue;

                int transferAmount = Mathf.Max(0, _owner.CurrentHp - 1);
                if (_freeTransferActive)
                {
                    _freeTransferActive = false;
                }
                else
                {
                    if (transferAmount <= 0) return;
                    _owner.TakePureDamage(transferAmount);
                }

                if (transferAmount <= 0) return;

                ally.Heal(transferAmount);
                _transferUsedThisCycle = true;
                return;
            }
        }

        private void ApplyRageAtTurnStart()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.RemoveBuffsById(RageAtkBuffId);
            if (!IsAtkSpecActive() || _rageCounter <= 0)
            {
                _rageCounter = 0;
                return;
            }

            float coeff = _rageBoosted ? 0.75f : 0.50f;
            int flatAtk = Mathf.Max(0, Mathf.RoundToInt(_rageCounter * coeff));
            _rageCounter = 0;

            if (flatAtk <= 0) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = RageAtkBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = flatAtk,
                IsPercent = false,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        private void RefreshSacrificeAuras()
        {
            if (_owner == null || _turnManager == null) return;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return;

            bool criticalHp = _owner.CurrentHp <= 1;
            float atkDef = criticalHp ? 0.15f : 0.05f;
            float launch = criticalHp ? 0f : 0.10f;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ReferenceEquals(ally, _owner) || ally.BuffReceiver == null) continue;

                BuffReceiver br = ally.BuffReceiver;
                br.RemoveBuffsById(SacrificeAtkBuffId);
                br.RemoveBuffsById(SacrificeDefBuffId);
                br.RemoveBuffsById(SacrificeLfBuffId);

                br.AddBuff(new BuffData
                {
                    BuffId = SacrificeAtkBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = atkDef,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
                br.AddBuff(new BuffData
                {
                    BuffId = SacrificeDefBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DEF,
                    Value = atkDef,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });

                if (launch > 0f)
                {
                    br.AddBuff(new BuffData
                    {
                        BuffId = SacrificeLfBuffId,
                        Source = _owner,
                        StatType = BuffStatType.LaunchForce,
                        Value = launch,
                        IsPercent = true,
                        RemainingTurns = -1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });
                }
            }
        }

        private void SyncPermanentBuffs()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            BuffReceiver br = _owner.BuffReceiver;
            br.RemoveBuffsById(HpBonusBuffId);
            br.RemoveBuffsById(AtkFlatBuffId);

            if (_bonusMaxHp > 0)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = HpBonusBuffId,
                    Source = _owner,
                    StatType = BuffStatType.HP,
                    Value = _bonusMaxHp,
                    IsPercent = false,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }

            if (_permanentAtkFlat > 0)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = AtkFlatBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = _permanentAtkFlat,
                    IsPercent = false,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
        }

        private void SyncDecayBuff()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.RemoveBuffsById(DecayDmgAmpBuffId);
            if (!_decayActive || !IsAtkSpecActive()) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = DecayDmgAmpBuffId,
                Source = _owner,
                StatType = BuffStatType.DamageAmplification,
                Value = 0.15f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        private bool IsLastAliveAlly()
        {
            if (_turnManager == null) return true;

            int alive = 0;
            var participants = _turnManager.Participants;
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    ITurnParticipant p = participants[i];
                    if (p == null || !p.IsAlly || p.IsDead) continue;
                    alive++;
                }
            }
            return alive <= 1;
        }

        private bool IsSupSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == -1;
        }

        private bool IsAtkSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == 0;
        }

        private void OnDestroy()
        {
            if (_subscribedToTurnChanged && _turnManager != null)
                _turnManager.OnTurnChanged -= OnTurnChanged;
            if (_subscribedToOwnerStopped && _owner != null)
                _owner.OnStopped -= OnOwnerStopped;
        }
    }
}

