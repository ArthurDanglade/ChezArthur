using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Enemies;
using System.Collections.Generic;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système central Goat (tranche 1 - spé ATK) :
    /// - P3: suivi rebonds mur avant 1er ennemi + bonus ATK de lancer
    /// - P4: compteur de kills persistant run
    /// - buffs temporaires au switch de spé ATK
    /// </summary>
    public class GoatSystem : MonoBehaviour
    {
        private const string SimulatorAtkBuffId = "goat_simulator_atk";
        private const string TempAtkBuffId = "goat_atk_switch_tmp";
        private const string TempDefBuffId = "goat_def_switch_tmp";
        private const string TempLfBuffId = "goat_lf_switch_tmp";
        private const string DefTauntCycleBuffId = "goat_def_taunt_cycle_bonus";
        private const string DefArmorCycleBuffId = "goat_def_armor_cycle_bonus";
        private const string SupBlessBuffId = "goat_bless_animal";
        private const string SupBlessAtkBuffId = "goat_bless_supreme_atk";
        private const string SupSwitchAtkBuffId = "goat_sup_switch_team_atk";
        private const string SupSwitchDefBuffId = "goat_sup_switch_team_def";

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private CharacterPassiveRuntime _runtime;

        private int _simulatorWallCount;
        private bool _beforeFirstEnemy = true;
        private bool _simulatorVelocityBoostActive;
        private int _brutalKills;
        private int _grazeCountThisStage;
        private readonly HashSet<CharacterBall> _scratchedAlliesThisTurn = new HashSet<CharacterBall>();
        private bool _supremeBlessActive;
        private bool _doubleTurnArmedThisTurn;
        private bool _doubleTurnUsedThisStage;

        private bool _subscribedHitEnemy;
        private bool _subscribedStopped;
        private bool _subscribedTurnChanged;

        public int BrutalKills => _brutalKills;

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (owner == null) return;

            if (_subscribedHitEnemy && _owner != null && _owner != owner)
                _owner.OnHitEnemy -= HandleOwnerHitEnemy;
            if (_subscribedStopped && _owner != null && _owner != owner)
                _owner.OnStopped -= HandleOwnerStopped;
            if (_subscribedTurnChanged && _turnManager != null && _turnManager != turnManager)
                _turnManager.OnTurnChanged -= HandleTurnChanged;

            _owner = owner;
            _turnManager = turnManager;
            _runtime = _owner.GetComponent<CharacterPassiveRuntime>();

            if (!_subscribedHitEnemy)
            {
                _owner.OnHitEnemy += HandleOwnerHitEnemy;
                _subscribedHitEnemy = true;
            }
            if (!_subscribedStopped)
            {
                _owner.OnStopped += HandleOwnerStopped;
                _subscribedStopped = true;
            }
            if (!_subscribedTurnChanged && _turnManager != null)
            {
                _turnManager.OnTurnChanged += HandleTurnChanged;
                _subscribedTurnChanged = true;
            }
        }

        public void AddBrutalKill()
        {
            _brutalKills = Mathf.Clamp(_brutalKills + 1, 0, 5);
        }

        public void RegisterWallBounce()
        {
            if (!IsAtkSpecActive()) return;
            if (!_beforeFirstEnemy) return;

            _simulatorWallCount = Mathf.Clamp(_simulatorWallCount + 1, 0, 99);
            SyncSimulatorAtkBuff();
        }

        /// <summary>
        /// Hook appelé après le decay mur dans CharacterBall (pour P3 switch bonus vélocité).
        /// </summary>
        public void TryApplyPostWallVelocityBoost()
        {
            if (!IsAtkSpecActive()) return;
            if (!_simulatorVelocityBoostActive || !_beforeFirstEnemy) return;

            Rigidbody2D rb = _owner != null ? _owner.GetComponent<Rigidbody2D>() : null;
            if (rb == null) return;

            rb.velocity *= 1.15f;
        }

        public void ActivateSimulatorSwitchBoost()
        {
            if (!IsAtkSpecActive()) return;
            _simulatorVelocityBoostActive = true;
        }

        public void NotifyEnemyHit()
        {
            HandleOwnerHitEnemy();
        }

        public void ApplyBerserkSwitchBonus()
        {
            ApplyTempBuff(TempLfBuffId, BuffStatType.LaunchForce, 0.05f, true);
        }

        public void ApplyHerdSwitchBonus()
        {
            ApplyTempBuff(TempAtkBuffId, BuffStatType.ATK, 0.05f, true);
        }

        public void ApplyBrutalSwitchBonus()
        {
            ApplyTempBuff(TempAtkBuffId, BuffStatType.ATK, 0.10f, true);
            ApplyTempBuff(TempLfBuffId, BuffStatType.LaunchForce, 0.10f, true);
        }

        public void ApplyTauntSwitchBonus()
        {
            if (!IsDefSpecActive()) return;
            ApplyCycleBuff(DefTauntCycleBuffId, BuffStatType.DamageReduction, 0f, true, 1);
        }

        public void ApplyArmorSwitchBonus()
        {
            if (!IsDefSpecActive()) return;
            ApplyCycleBuff(DefArmorCycleBuffId, BuffStatType.DEF, 0.05f, true, 1);
        }

        public int ModifyIncomingCollisionDamageFromEnemy(int rawDamage, Enemy enemy)
        {
            _ = enemy; // TODO V2: taunt ciblage prioritaire côté EnemyAI.

            if (!IsDefSpecActive()) return rawDamage;
            if (rawDamage <= 0) return rawDamage;

            float reduction = 0.20f;
            if (_owner != null && _owner.BuffReceiver != null && _owner.BuffReceiver.HasBuff(DefTauntCycleBuffId))
                reduction = 0.35f;

            return Mathf.Max(1, Mathf.RoundToInt(rawDamage * (1f - reduction)));
        }

        public void TryScratchFromAlly(CharacterBall ally)
        {
            if (!IsDefSpecActive()) return;
            if (_owner == null || ally == null || ally == _owner || ally.IsDead) return;
            if (_turnManager == null || !ReferenceEquals(_turnManager.CurrentParticipant, ally)) return;
            if (_scratchedAlliesThisTurn.Contains(ally)) return;

            _scratchedAlliesThisTurn.Add(ally);
            int heal = Mathf.RoundToInt(_owner.MaxHp * 0.10f);
            if (heal > 0)
                _owner.Heal(heal);
        }

        public void TryGrazeTurnStart()
        {
            if (!IsDefSpecActive()) return;
            if (_owner == null || _turnManager == null) return;
            if (!ReferenceEquals(_turnManager.CurrentParticipant, _owner)) return;
            if (_grazeCountThisStage >= 3) return;

            _grazeCountThisStage++;
            int heal = Mathf.RoundToInt(_owner.MaxHp * 0.15f);
            if (heal > 0)
                _owner.Heal(heal);
        }

        public void ResetDefStageState()
        {
            _grazeCountThisStage = 0;
            _scratchedAlliesThisTurn.Clear();
        }

        public void SetSupremeBlessActive(bool active)
        {
            _supremeBlessActive = active;
        }

        public void ApplyBlessOnAllyHit(CharacterBall ally)
        {
            if (!IsSupSpecActive()) return;
            if (_owner == null || ally == null || ally.IsDead || ally == _owner) return;

            BuffReceiver allyBr = ally.BuffReceiver;
            if (allyBr == null) return;

            int cycles = _supremeBlessActive ? 3 : 2;
            allyBr.AddBuff(new BuffData
            {
                BuffId = SupBlessBuffId,
                Source = _owner,
                StatType = BuffStatType.DamageReduction,
                Value = 0.15f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = cycles,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            if (_supremeBlessActive)
            {
                allyBr.AddBuff(new BuffData
                {
                    BuffId = SupBlessAtkBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = 0.10f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = cycles,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }

            int blessedCount = 0;
            if (_turnManager != null)
            {
                IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
                if (allies != null)
                {
                    for (int i = 0; i < allies.Count; i++)
                    {
                        CharacterBall member = allies[i];
                        if (member == null || member.IsDead || member.BuffReceiver == null) continue;
                        if (member.BuffReceiver.HasBuff(SupBlessBuffId))
                            blessedCount++;
                    }
                }
            }

            if (blessedCount >= 2 && _owner.BuffReceiver != null)
            {
                _owner.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = SupBlessBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DamageReduction,
                    Value = 0.15f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = cycles,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });

                if (_supremeBlessActive)
                {
                    _owner.BuffReceiver.AddBuff(new BuffData
                    {
                        BuffId = SupBlessAtkBuffId,
                        Source = _owner,
                        StatType = BuffStatType.ATK,
                        Value = 0.10f,
                        IsPercent = true,
                        RemainingTurns = -1,
                        RemainingCycles = cycles,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });
                }
            }

            TryGrantDoubleTurn(ally);
        }

        public void ApplySupSwitchTeamBonus()
        {
            if (!IsSupSpecActive()) return;
            if (_turnManager == null || _owner == null) return;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;

                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = SupSwitchAtkBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = 0.10f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = 1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });

                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = SupSwitchDefBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DEF,
                    Value = 0.10f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = 1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
        }

        public void StartMilkTrail()
        {
            if (!IsSupSpecActive() || _owner == null) return;

            GoatMilkTrailSystem milk = _owner.GetComponent<GoatMilkTrailSystem>();
            if (milk == null)
                milk = _owner.gameObject.AddComponent<GoatMilkTrailSystem>();

            milk.Initialize(_owner);
            milk.ClearTrail();
            milk.StartTrail();
        }

        public void ClearMilkTrail()
        {
            if (_owner == null) return;
            GoatMilkTrailSystem milk = _owner.GetComponent<GoatMilkTrailSystem>();
            if (milk != null)
                milk.ClearTrail();
        }

        public void ArmDoubleTurnOnSupSwitch()
        {
            if (!IsSupSpecActive()) return;
            if (_doubleTurnUsedThisStage) return;
            _doubleTurnArmedThisTurn = true;
        }

        public void ResetSupStageState()
        {
            _doubleTurnArmedThisTurn = false;
            _doubleTurnUsedThisStage = false;
            ClearMilkTrail();
        }

        public void ApplyBerserkFullTeamBonus(bool shouldApply)
        {
            if (!IsAtkSpecActive()) return;

            if (!shouldApply)
            {
                if (_owner?.BuffReceiver != null)
                {
                    _owner.BuffReceiver.RemoveBuffsById(TempAtkBuffId);
                    _owner.BuffReceiver.RemoveBuffsById(TempDefBuffId);
                }
                return;
            }

            ApplyTempBuff(TempAtkBuffId, BuffStatType.ATK, 0.10f, true);
            ApplyTempBuff(TempDefBuffId, BuffStatType.DEF, 0.10f, true);
        }

        private void HandleOwnerHitEnemy()
        {
            _beforeFirstEnemy = false;
            _simulatorVelocityBoostActive = false;
            _simulatorWallCount = 0;
            SyncSimulatorAtkBuff();
        }

        private void HandleOwnerStopped()
        {
            _beforeFirstEnemy = true;
            _simulatorVelocityBoostActive = false;
            _simulatorWallCount = 0;
            SyncSimulatorAtkBuff();
        }

        private void SyncSimulatorAtkBuff()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.RemoveBuffsById(SimulatorAtkBuffId);
            if (_simulatorWallCount <= 0) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = SimulatorAtkBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = _simulatorWallCount * 0.10f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private void ApplyTempBuff(string buffId, BuffStatType statType, float value, bool isPercent)
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = buffId,
                Source = _owner,
                StatType = statType,
                Value = value,
                IsPercent = isPercent,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private void ApplyCycleBuff(string buffId, BuffStatType statType, float value, bool isPercent, int cycles)
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = buffId,
                Source = _owner,
                StatType = statType,
                Value = value,
                IsPercent = isPercent,
                RemainingTurns = -1,
                RemainingCycles = cycles,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private bool IsAtkSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == -1;
        }

        private bool IsDefSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == 0;
        }

        private bool IsSupSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == 1;
        }

        private void TryGrantDoubleTurn(CharacterBall ally)
        {
            if (!_doubleTurnArmedThisTurn || _doubleTurnUsedThisStage) return;
            if (ally == null || ally == _owner || ally.IsDead) return;

            ally.QueueExtraTurn(1);
            _doubleTurnArmedThisTurn = false;
            _doubleTurnUsedThisStage = true;
        }

        private void HandleTurnChanged(ITurnParticipant participant)
        {
            if (participant == null) return;
            if (participant.IsAlly)
                _scratchedAlliesThisTurn.Clear();
            if (ReferenceEquals(participant, _owner))
                _doubleTurnArmedThisTurn = false;
        }

        private void OnDestroy()
        {
            if (_owner != null && _subscribedHitEnemy)
                _owner.OnHitEnemy -= HandleOwnerHitEnemy;
            if (_owner != null && _subscribedStopped)
                _owner.OnStopped -= HandleOwnerStopped;
            if (_turnManager != null && _subscribedTurnChanged)
                _turnManager.OnTurnChanged -= HandleTurnChanged;
        }
    }
}

