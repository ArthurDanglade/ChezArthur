using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système central Don Costardo :
    /// collègues mafieux, corruption, exécution, riposte des collègues et salve.
    /// </summary>
    public class DonCostardoSystem : MonoBehaviour
    {
        private const string MemberBuffId = "don_member";
        private const string CorruptBuffId = "don_corrupt";
        private const string SalvoSwitchAtkBuffId = "don_salvo_switch_atk";

        private static DonCostardoSystem _instance;
        public static DonCostardoSystem Instance => _instance;

        private class Henchman
        {
            public int Hp = 150;
            public int MaxHp = 150;
            public int Def = 30;
            public bool IsAlive => Hp > 0;
        }

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private CharacterPassiveRuntime _runtime;

        private readonly Dictionary<CharacterBall, Henchman> _henchmen = new Dictionary<CharacterBall, Henchman>();

        private bool _extendedMemberDuration;

        private Enemy _corruptedEnemy;
        private int _corruptionCyclesRemaining;
        private bool _corruptionUsedThisStage;

        private bool _executionThresholdBoosted;
        private bool _henchmenDamageBoosted;

        private readonly HashSet<CharacterBall> _alliesDamagedThisStage = new HashSet<CharacterBall>();
        private bool _salvoTriggered;

        private bool _subscribedToTurnChanged;
        private bool _subscribedToOwnerStopped;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

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

            EnsureHenchmenEntries();

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
        }

        public int AbsorbDamageWithHenchman(CharacterBall ally, int rawDamage)
        {
            if (ally == null || rawDamage <= 0) return rawDamage;
            EnsureHenchmenEntries();

            if (!_henchmen.TryGetValue(ally, out Henchman h) || h == null || !h.IsAlive)
                return rawDamage;

            int reduced = Mathf.Max(1, rawDamage - h.Def);
            if (h.Hp >= reduced)
            {
                h.Hp -= reduced;
                return 0;
            }

            int overflow = reduced - h.Hp;
            h.Hp = 0;
            return Mathf.Max(1, overflow);
        }

        public void NotifyAllyDamaged(CharacterBall ally)
        {
            if (ally == null || ally.IsDead) return;
            if (_salvoTriggered) return;

            _alliesDamagedThisStage.Add(ally);
            if (_alliesDamagedThisStage.Count < 3) return;

            _salvoTriggered = true;
            TriggerSalvo();
        }

        public void ApplyMemberOnAllyHit(CharacterBall ally)
        {
            if (!IsSupSpecActive()) return;
            if (ally == null || ally.BuffReceiver == null || ally.IsDead) return;

            int turns = _extendedMemberDuration ? 4 : 2;
            ally.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = MemberBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = 0.30f,
                IsPercent = true,
                RemainingTurns = turns,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            ally.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = MemberBuffId,
                Source = _owner,
                StatType = BuffStatType.DEF,
                Value = 0.30f,
                IsPercent = true,
                RemainingTurns = turns,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public void TryCorruptOnTurnStart()
        {
            if (!IsSupSpecActive()) return;
            if (_corruptionUsedThisStage) return;

            Enemy target = GetHighestHpEnemy();
            if (target == null) return;

            _corruptionUsedThisStage = true;
            _corruptedEnemy = target;
            _corruptionCyclesRemaining = 2;

            BuffReceiver br = target.BuffReceiver;
            if (br != null)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = CorruptBuffId,
                    Source = _owner,
                    StatType = BuffStatType.Speed,
                    Value = 0f,
                    IsPercent = false,
                    RemainingTurns = -1,
                    RemainingCycles = 2,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
        }

        public void TryExecuteEnemy(Enemy enemy)
        {
            if (!IsAtkSpecActive()) return;
            if (enemy == null || enemy.IsDead || enemy.Data == null) return;
            if (enemy.Data.EnemyType == EnemyType.Boss) return;

            float threshold = _executionThresholdBoosted ? 0.10f : 0.05f;
            if (enemy.MaxHp <= 0) return;

            float ratio = (float)enemy.CurrentHp / enemy.MaxHp;
            if (ratio > threshold) return;

            enemy.Die();
        }

        public void TriggerHenchmenAttack(Enemy enemy)
        {
            if (!IsAtkSpecActive()) return;
            if (enemy == null || enemy.IsDead || _owner == null) return;
            EnsureHenchmenEntries();

            float min = _henchmenDamageBoosted ? 0.15f : 0.05f;
            float max = _henchmenDamageBoosted ? 0.40f : 0.30f;

            foreach (var kvp in _henchmen)
            {
                Henchman h = kvp.Value;
                if (h == null || !h.IsAlive) continue;

                float coeff = Random.Range(min, max);
                int damage = Mathf.Max(1, Mathf.RoundToInt(_owner.EffectiveAtk * coeff));
                enemy.TakeDamage(damage);
                if (enemy.IsDead) break;
            }
        }

        public void ApplyFamigliaSwitchBonus()
        {
            if (!IsSupSpecActive()) return;
            EnsureHenchmenEntries();

            foreach (var kvp in _henchmen)
            {
                Henchman h = kvp.Value;
                if (h == null || !h.IsAlive) continue;
                h.Hp = Mathf.Min(h.MaxHp, h.Hp + 30);
            }
        }

        public void ApplyMemberSwitchBonus()
        {
            if (!IsSupSpecActive()) return;
            _extendedMemberDuration = true;
        }

        public void ApplyExecutionSwitchBonus()
        {
            if (!IsAtkSpecActive()) return;
            _executionThresholdBoosted = true;
        }

        public void ApplyHenchmenSwitchBonus()
        {
            if (!IsAtkSpecActive()) return;
            _henchmenDamageBoosted = true;
        }

        public void ApplySalvoSwitchBonus()
        {
            if (!IsAtkSpecActive() || _owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = SalvoSwitchAtkBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = 0.20f,
                IsPercent = true,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public void ResetForStage()
        {
            _extendedMemberDuration = false;
            _executionThresholdBoosted = false;
            _henchmenDamageBoosted = false;
            _alliesDamagedThisStage.Clear();
            _salvoTriggered = false;
            _corruptionUsedThisStage = false;
            _corruptionCyclesRemaining = 0;
            ClearCorruption();
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_turnManager == null) return;

            // Si tous les autres ennemis sont morts, annule la corruption.
            if (_corruptedEnemy != null && !HasAnyOtherAliveEnemy(_corruptedEnemy))
            {
                ClearCorruption();
            }

            if (_corruptedEnemy == null || _corruptionCyclesRemaining <= 0) return;
            if (participant == null) return;
            if (!ReferenceEquals(participant, _corruptedEnemy)) return;

            _corruptionCyclesRemaining--;
            if (_corruptionCyclesRemaining <= 0)
                ClearCorruption();

            _turnManager.SkipCurrentTurn();
        }

        private void OnOwnerStopped()
        {
            _extendedMemberDuration = false;
            _executionThresholdBoosted = false;
            _henchmenDamageBoosted = false;
        }

        private void TriggerSalvo()
        {
            if (_turnManager == null || _owner == null) return;

            int damage = Mathf.Max(1, _owner.EffectiveAtk);
            var participants = _turnManager.Participants;
            if (participants == null) return;

            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant p = participants[i];
                if (p == null || p.IsAlly || p.IsDead) continue;

                Enemy enemy = p as Enemy;
                if (enemy == null || enemy.IsDead) continue;
                enemy.TakeDamage(damage);
            }
        }

        private void EnsureHenchmenEntries()
        {
            if (_turnManager == null) return;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null) continue;
                if (_henchmen.ContainsKey(ally)) continue;
                _henchmen[ally] = new Henchman();
            }
        }

        private Enemy GetHighestHpEnemy()
        {
            if (_turnManager == null) return null;
            var participants = _turnManager.Participants;
            if (participants == null) return null;

            Enemy best = null;
            int bestHp = -1;

            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant p = participants[i];
                if (p == null || p.IsAlly || p.IsDead) continue;

                Enemy e = p as Enemy;
                if (e == null || e.IsDead) continue;

                if (e.CurrentHp > bestHp)
                {
                    bestHp = e.CurrentHp;
                    best = e;
                }
            }

            return best;
        }

        private bool HasAnyOtherAliveEnemy(Enemy excluded)
        {
            if (_turnManager == null) return false;
            var participants = _turnManager.Participants;
            if (participants == null) return false;

            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant p = participants[i];
                if (p == null || p.IsAlly || p.IsDead) continue;
                if (ReferenceEquals(p, excluded)) continue;
                return true;
            }
            return false;
        }

        private void ClearCorruption()
        {
            if (_corruptedEnemy != null && _corruptedEnemy.BuffReceiver != null)
                _corruptedEnemy.BuffReceiver.RemoveBuffsById(CorruptBuffId);

            _corruptedEnemy = null;
            _corruptionCyclesRemaining = 0;
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
            if (_instance == this)
                _instance = null;

            if (_subscribedToTurnChanged && _turnManager != null)
                _turnManager.OnTurnChanged -= OnTurnChanged;
            if (_subscribedToOwnerStopped && _owner != null)
                _owner.OnStopped -= OnOwnerStopped;
        }
    }
}

