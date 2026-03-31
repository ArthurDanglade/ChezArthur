using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système central Troplin : spin défensif/offensif, stacks cross-spé, strip DEF et vol d'ATK.
    /// </summary>
    public class TroplinSystem : MonoBehaviour
    {
        private const string SlowBuffId = "troplin_slow";
        private const string StripDefBuffId = "troplin_strip_def";
        private const string AllyDefBuffId = "troplin_ally_def";
        private const string AllyAtkBuffId = "troplin_ally_atk";
        private const string AllyLfBuffId = "troplin_ally_lf";
        private const string DefSwitchBuffId = "troplin_def_switch_def";
        private const string AtkSwitchBuffId = "troplin_atk_switch_atk";
        private const string StolenAtkBuffId = "troplin_stolen_atk";
        private const string EnemyAtkDebuffId = "troplin_enemy_atk_debuff";

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private CharacterPassiveRuntime _runtime;

        private int _hpMaxStacks; // 0..20
        private int _atkStacks;   // 0..20

        private bool _spinDefBoostActive;
        private readonly Dictionary<Enemy, int> _stripCounts = new Dictionary<Enemy, int>();

        private bool _spinAtkBoostActive;

        private int _totalStolenAtk;
        private readonly List<StolenEntry> _stolenFromEnemies = new List<StolenEntry>(8);

        private bool _subscribedToOwnerStopped;

        private class StolenEntry
        {
            public Enemy Enemy;
            public int StolenAmount;
            public Action DeathHandler;
        }

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (owner == null) return;

            if (_subscribedToOwnerStopped && _owner != null && !ReferenceEquals(_owner, owner))
            {
                _owner.OnStopped -= OnOwnerStopped;
                _subscribedToOwnerStopped = false;
            }

            _owner = owner;
            _turnManager = turnManager;
            _runtime = _owner.GetComponent<CharacterPassiveRuntime>();

            if (!_subscribedToOwnerStopped)
            {
                _owner.OnStopped += OnOwnerStopped;
                _subscribedToOwnerStopped = true;
            }
        }

        public float GetHpStackBonus() => _hpMaxStacks * 0.01f;
        public float GetAtkStackBonus() => _atkStacks * 0.01f;

        public int ModifyIncomingDamageFromEnemy(int rawDamage, Enemy enemy)
        {
            if (_owner == null || enemy == null || enemy.IsDead) return rawDamage;
            if (_owner.IsMoving) return rawDamage;

            if (IsDefSpecActive())
            {
                float reduction = _spinDefBoostActive ? 0.40f : 0.20f;
                ApplySlowAndStrip(enemy);
                return Mathf.Max(1, Mathf.RoundToInt(rawDamage * (1f - reduction)));
            }

            if (IsAtkSpecActive())
            {
                float atkMult = _spinAtkBoostActive ? 1.30f : 1.00f;
                int counterDamage = Mathf.Max(1, Mathf.RoundToInt(_owner.EffectiveAtk * atkMult));
                enemy.TakeDamage(counterDamage);
                return rawDamage;
            }

            return rawDamage;
        }

        public void OnAllyHitTroplin(CharacterBall ally)
        {
            if (_owner == null || ally == null || ally.IsDead) return;
            if (_owner.IsMoving) return;

            if (IsDefSpecActive())
            {
                _hpMaxStacks = Mathf.Clamp(_hpMaxStacks + 1, 0, 20);
                if (ally.BuffReceiver != null)
                {
                    ally.BuffReceiver.AddBuff(new BuffData
                    {
                        BuffId = AllyDefBuffId,
                        Source = _owner,
                        StatType = BuffStatType.DEF,
                        Value = 0.10f,
                        IsPercent = true,
                        RemainingTurns = 1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });
                }
            }
            else if (IsAtkSpecActive())
            {
                _atkStacks = Mathf.Clamp(_atkStacks + 1, 0, 20);
                if (ally.BuffReceiver != null)
                {
                    ally.BuffReceiver.AddBuff(new BuffData
                    {
                        BuffId = AllyAtkBuffId,
                        Source = _owner,
                        StatType = BuffStatType.ATK,
                        Value = 0.15f,
                        IsPercent = true,
                        RemainingTurns = 1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });
                    ally.BuffReceiver.AddBuff(new BuffData
                    {
                        BuffId = AllyLfBuffId,
                        Source = _owner,
                        StatType = BuffStatType.LaunchForce,
                        Value = 0.10f,
                        IsPercent = true,
                        RemainingTurns = 1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });
                }
            }

            // Recalcule les HP effectifs si les stacks HP ont bougé.
            _owner.RecalculateHpAfterBonus();
        }

        public void TryStealAtk(Enemy enemy)
        {
            if (!IsAtkSpecActive()) return;
            if (_owner == null || enemy == null || enemy.IsDead) return;

            for (int i = 0; i < _stolenFromEnemies.Count; i++)
            {
                if (ReferenceEquals(_stolenFromEnemies[i].Enemy, enemy))
                    return;
            }

            int stolenFlat = Mathf.Max(1, Mathf.RoundToInt(enemy.EffectiveAtk * 0.05f));

            BuffReceiver enemyBr = enemy.BuffReceiver;
            if (enemyBr != null)
            {
                enemyBr.AddBuff(new BuffData
                {
                    BuffId = EnemyAtkDebuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = -stolenFlat,
                    IsPercent = false,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }

            _totalStolenAtk += stolenFlat;
            SyncStolenBuff();

            var entry = new StolenEntry
            {
                Enemy = enemy,
                StolenAmount = stolenFlat
            };
            entry.DeathHandler = () => HandleStolenEnemyDeath(entry);
            enemy.OnDeath += entry.DeathHandler;
            _stolenFromEnemies.Add(entry);
        }

        public void ApplySpinDefSwitchBonus()
        {
            if (!IsDefSpecActive()) return;
            _spinDefBoostActive = true;
        }

        public void ApplyStripSwitchBonus()
        {
            if (!IsDefSpecActive() || _owner == null || _owner.BuffReceiver == null) return;
            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = DefSwitchBuffId,
                Source = _owner,
                StatType = BuffStatType.DEF,
                Value = 0.20f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = 1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public void ApplySpinAtkSwitchBonus()
        {
            if (!IsAtkSpecActive()) return;
            _spinAtkBoostActive = true;
        }

        public void ApplyStealSwitchBonus()
        {
            if (!IsAtkSpecActive() || _owner == null || _owner.BuffReceiver == null) return;
            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = AtkSwitchBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = 0.15f,
                IsPercent = true,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public void ResetForStage()
        {
            _stripCounts.Clear();
            _spinDefBoostActive = false;
            _spinAtkBoostActive = false;
        }

        private void ApplySlowAndStrip(Enemy enemy)
        {
            BuffReceiver br = enemy.BuffReceiver;
            if (br == null) return;

            br.AddBuff(new BuffData
            {
                BuffId = SlowBuffId,
                Source = _owner,
                StatType = BuffStatType.Speed,
                Value = -0.30f,
                IsPercent = true,
                RemainingTurns = 2,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            _stripCounts.TryGetValue(enemy, out int count);
            if (count >= 2) return;

            _stripCounts[enemy] = count + 1;
            br.AddBuff(new BuffData
            {
                BuffId = StripDefBuffId,
                Source = _owner,
                StatType = BuffStatType.DEF,
                Value = -0.05f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        private void SyncStolenBuff()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            BuffReceiver br = _owner.BuffReceiver;
            br.RemoveBuffsById(StolenAtkBuffId);

            if (_totalStolenAtk <= 0) return;

            br.AddBuff(new BuffData
            {
                BuffId = StolenAtkBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = _totalStolenAtk,
                IsPercent = false,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        private void HandleStolenEnemyDeath(StolenEntry entry)
        {
            if (entry == null) return;

            if (entry.Enemy != null && entry.DeathHandler != null)
                entry.Enemy.OnDeath -= entry.DeathHandler;

            _totalStolenAtk = Mathf.Max(0, _totalStolenAtk - Mathf.Max(0, entry.StolenAmount));
            _stolenFromEnemies.Remove(entry);
            SyncStolenBuff();
        }

        private bool IsDefSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == -1;
        }

        private bool IsAtkSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == 0;
        }

        private void OnOwnerStopped()
        {
            _spinDefBoostActive = false;
            _spinAtkBoostActive = false;
        }

        private void OnDestroy()
        {
            if (_subscribedToOwnerStopped && _owner != null)
                _owner.OnStopped -= OnOwnerStopped;

            for (int i = 0; i < _stolenFromEnemies.Count; i++)
            {
                StolenEntry e = _stolenFromEnemies[i];
                if (e?.Enemy != null && e.DeathHandler != null)
                    e.Enemy.OnDeath -= e.DeathHandler;
            }
            _stolenFromEnemies.Clear();
        }
    }
}

