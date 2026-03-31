using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système central de L'Ancien n°1 Mondial.
    /// Gère les compteurs partagés ATK/DEF, la mémorisation d'ennemis,
    /// la robustesse, et les effets "dernier survivant".
    /// </summary>
    public class AncienSystem : MonoBehaviour
    {
        private const string MemorizeAtkBuffId = "ancien_memorize_atk";
        private const string DefLaststandReductionBuffId = "ancien_def_laststand_dr";
        private const string AtkLaststandLfBuffId = "ancien_atk_laststand_lf";
        private const string AtkSwitchBonusBuffId = "ancien_atk_switch_bonus";
        private const string DefSwitchBonusBuffId = "ancien_def_switch_bonus";

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private CharacterPassiveRuntime _runtime;

        private int _switchRewardStacks;   // 0..35 (=> 0..70%)
        private int _lastTurnSpecIndex = -2;
        private readonly HashSet<Enemy> _memorizedEnemies = new HashSet<Enemy>();
        private int _toughnessStacks;      // 0..30
        private bool _isLastSurvivor;

        private bool _subscribedToTurnChanged;
        private bool _subscribedToOwnerStopped;

        public void Initialize(CharacterBall owner, TurnManager tm)
        {
            if (owner == null) return;

            if (_subscribedToTurnChanged && _turnManager != null && _turnManager != tm)
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
            _turnManager = tm;
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

            SyncMemorizeBuff();
            RefreshLastSurvivorState();
            SyncLastSurvivorBuffs();
        }

        public float GetSwitchRewardBonus()
        {
            return _switchRewardStacks * 0.02f;
        }

        public float GetMemorizeAtkBonus()
        {
            return Mathf.Min(_memorizedEnemies.Count, 4) * 0.15f;
        }

        public void TryMemorizeEnemy(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead) return;
            if (_memorizedEnemies.Contains(enemy)) return;
            if (_memorizedEnemies.Count >= 4) return;

            _memorizedEnemies.Add(enemy);
            SyncMemorizeBuff();
        }

        public float GetToughnessBonus()
        {
            return _toughnessStacks * 0.01f;
        }

        public void AddToughnessStack()
        {
            _toughnessStacks = Mathf.Clamp(_toughnessStacks + 1, 0, 30);
        }

        public void ResetForStage()
        {
            _memorizedEnemies.Clear();
            if (_owner != null && _owner.BuffReceiver != null)
                _owner.BuffReceiver.RemoveBuffsById(MemorizeAtkBuffId);

            RefreshLastSurvivorState();
            SyncLastSurvivorBuffs();
        }

        public void ApplyAtkSwitchBonus()
        {
            if (!IsAtkSpecActive()) return;
            ApplyTurnBuff(AtkSwitchBonusBuffId, BuffStatType.ATK, 0.20f);
        }

        public void ApplyDefSwitchBonus()
        {
            if (!IsDefSpecActive()) return;
            ApplyCycleBuff(DefSwitchBonusBuffId, BuffStatType.DEF, 0.20f, 1);
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null || _runtime == null) return;
            RefreshLastSurvivorState();
            SyncLastSurvivorBuffs();

            if (!ReferenceEquals(participant, _owner))
                return;

            int currentSpec = _runtime.CurrentSpecIndex;
            if (_lastTurnSpecIndex != -2 && currentSpec != _lastTurnSpecIndex && _switchRewardStacks < 35)
                _switchRewardStacks++;

            _lastTurnSpecIndex = currentSpec;
        }

        private void OnOwnerStopped()
        {
            if (_owner == null) return;
            if (!IsDefSpecActive()) return;

            RefreshLastSurvivorState();
            SyncLastSurvivorBuffs();

            if (!_isLastSurvivor) return;

            int heal = Mathf.RoundToInt(_owner.MaxHp * 0.05f);
            if (heal > 0)
                _owner.Heal(heal);
        }

        private void SyncMemorizeBuff()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            BuffReceiver br = _owner.BuffReceiver;
            br.RemoveBuffsById(MemorizeAtkBuffId);

            float value = GetMemorizeAtkBonus();
            if (value <= 0f) return;

            br.AddBuff(new BuffData
            {
                BuffId = MemorizeAtkBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = value,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        private void RefreshLastSurvivorState()
        {
            if (_turnManager == null || _owner == null)
            {
                _isLastSurvivor = false;
                return;
            }

            int aliveAllies = 0;
            var participants = _turnManager.Participants;
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    ITurnParticipant p = participants[i];
                    if (p == null || !p.IsAlly || p.IsDead) continue;
                    aliveAllies++;
                }
            }

            _isLastSurvivor = aliveAllies <= 1;
        }

        private void SyncLastSurvivorBuffs()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;
            BuffReceiver br = _owner.BuffReceiver;

            br.RemoveBuffsById(DefLaststandReductionBuffId);
            br.RemoveBuffsById(AtkLaststandLfBuffId);

            if (_isLastSurvivor && IsDefSpecActive())
            {
                br.AddBuff(new BuffData
                {
                    BuffId = DefLaststandReductionBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DamageReduction,
                    Value = 0.50f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }

            if (_isLastSurvivor && IsAtkSpecActive())
            {
                br.AddBuff(new BuffData
                {
                    BuffId = AtkLaststandLfBuffId,
                    Source = _owner,
                    StatType = BuffStatType.LaunchForce,
                    Value = 0.20f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
        }

        private bool IsAtkSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == -1;
        }

        private bool IsDefSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == 0;
        }

        private void ApplyTurnBuff(string buffId, BuffStatType statType, float value)
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = buffId,
                Source = _owner,
                StatType = statType,
                Value = value,
                IsPercent = true,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        private void ApplyCycleBuff(string buffId, BuffStatType statType, float value, int cycles)
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = buffId,
                Source = _owner,
                StatType = statType,
                Value = value,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = cycles,
                UniquePerSource = true,
                UniqueGlobal = false
            });
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

