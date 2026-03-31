using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using System.Collections.Generic;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système central Brooke Heune (tranche DEF) :
    /// armure, panique, survie létale et stacks High Five.
    /// </summary>
    public class BrookeSystem : MonoBehaviour
    {
        private const string ArmorDRBuffId = "brooke_armor_dr";
        private const string ArmorDRBoostBuffId = "brooke_armor_dr_boost";
        private const string PanicSwitchDefBuffId = "brooke_panic_switch_def";
        private const string HighFiveDefBuffId = "brooke_highfive_def";
        private const string BleedBuffId = "brooke_bleed";
        private const string BleedSwitchAtkBuffId = "brooke_bleed_switch_atk";
        private const string SoloSwitchDmgAmpBuffId = "brooke_solo_switch_damp";
        private const string LadiesAtkBuffId = "brooke_ladies_atk";
        private const string LadiesSwitchTeamAtkBuffId = "brooke_ladies_switch_team_atk";
        private const string SoloFlatAtkBuffId = "brooke_solo_flat_atk";
        private const string GiftBuffId = "brooke_gift";
        private const string AuraDRBuffId = "brooke_aura_dr";
        private const string AuraAtkBuffId = "brooke_aura_atk";

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private CharacterPassiveRuntime _runtime;

        private int _armorStacks; // 0..6
        private bool _surviveUsedThisStage;

        private int _highFiveStacks; // 0..30, permanent run

        private readonly HashSet<Enemy> _enemiesBleedByAtk = new HashSet<Enemy>();

        private enum FirstContact
        {
            None,
            Ally,
            Enemy
        }

        private FirstContact _ladiesFirstContact;
        private bool _ladiesBuffApplied;
        private readonly HashSet<CharacterBall> _trampolinedAlliesThisTurn = new HashSet<CharacterBall>();
        private bool _giftExtendedDuration;
        private bool _balmBoostActive;
        private int _auraStacks; // 0..20, persistant tant que pas de switch hors SUP

        private bool _subscribedToTurnChanged;
        private bool _subscribedToOwnerStopped;
        private bool _subscribedToHitEnemy;
        private bool _subscribedToHitAlly;

        public void Initialize(CharacterBall owner, TurnManager tm)
        {
            if (owner == null) return;

            if (_subscribedToTurnChanged && _turnManager != null && _turnManager != tm)
            {
                _turnManager.OnTurnChanged -= HandleTurnChanged;
                _subscribedToTurnChanged = false;
            }
            if (_subscribedToOwnerStopped && _owner != null && !ReferenceEquals(_owner, owner))
            {
                _owner.OnStopped -= OnOwnerStopped;
                _subscribedToOwnerStopped = false;
            }
            if (_subscribedToHitEnemy && _owner != null && !ReferenceEquals(_owner, owner))
            {
                _owner.OnHitEnemy -= OnEnemyHitForLadies;
                _subscribedToHitEnemy = false;
            }
            if (_subscribedToHitAlly && _owner != null && !ReferenceEquals(_owner, owner))
            {
                _owner.OnHitAllyEvent -= OnAllyHitForLadies;
                _subscribedToHitAlly = false;
            }

            _owner = owner;
            _turnManager = tm;
            _runtime = _owner.GetComponent<CharacterPassiveRuntime>();

            if (!_subscribedToTurnChanged && _turnManager != null)
            {
                _turnManager.OnTurnChanged += HandleTurnChanged;
                _subscribedToTurnChanged = true;
            }
            if (!_subscribedToOwnerStopped)
            {
                _owner.OnStopped += OnOwnerStopped;
                _subscribedToOwnerStopped = true;
            }
            if (!_subscribedToHitEnemy)
            {
                _owner.OnHitEnemy += OnEnemyHitForLadies;
                _subscribedToHitEnemy = true;
            }
            if (!_subscribedToHitAlly)
            {
                _owner.OnHitAllyEvent += OnAllyHitForLadies;
                _subscribedToHitAlly = true;
            }
        }

        public void OnOwnerTookDamage()
        {
            if (!IsDefSpecActive()) return;
            AddArmorStack();
        }

        public void AddArmorStack()
        {
            _armorStacks = Mathf.Clamp(_armorStacks + 1, 0, 6);
        }

        public float GetArmorDefBonus()
        {
            return _armorStacks * 0.05f;
        }

        public void ApplyArmorDR()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;
            _owner.BuffReceiver.RemoveBuffsById(ArmorDRBuffId);
            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = ArmorDRBuffId,
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

        public void ApplyArmorSwitchBonus()
        {
            if (!IsDefSpecActive()) return;
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = ArmorDRBoostBuffId,
                Source = _owner,
                StatType = BuffStatType.DamageReduction,
                Value = 0.10f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = 1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public void ApplyPanicSwitchBonus()
        {
            if (!IsDefSpecActive()) return;
            if (_owner == null || _owner.BuffReceiver == null) return;
            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = PanicSwitchDefBuffId,
                Source = _owner,
                StatType = BuffStatType.DEF,
                Value = 0.20f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = 1,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            _enemiesBleedByAtk.Clear();
        }

        public bool TrySurviveLethal()
        {
            if (_surviveUsedThisStage) return false;
            _surviveUsedThisStage = true;
            return true;
        }

        public void AddHighFiveStack()
        {
            _highFiveStacks = Mathf.Clamp(_highFiveStacks + 1, 0, 30);
            SyncHighFiveBuff();
        }

        public void OnAllyHitBrooke(CharacterBall ally)
        {
            _ = ally;
            AddHighFiveStack();
        }

        public void ApplyBleed(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead) return;

            BuffReceiver br = enemy.BuffReceiver;
            if (br == null) return;

            br.AddBuff(new BuffData
            {
                BuffId = BleedBuffId,
                Source = _owner,
                StatType = BuffStatType.DamageAmplification,
                Value = 0.20f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = 3,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            if (IsAtkSpecActive())
                _enemiesBleedByAtk.Add(enemy);
        }

        public void TryEncoreBonusDamage(Enemy enemy)
        {
            if (!IsAtkSpecActive()) return;
            if (enemy == null || enemy.IsDead) return;
            if (enemy.BuffReceiver == null || !enemy.BuffReceiver.HasBuff(BleedBuffId)) return;
            if (!_enemiesBleedByAtk.Contains(enemy)) return;

            int bonusDamage = Mathf.Max(1, Mathf.RoundToInt(_owner.EffectiveAtk * 0.70f));
            enemy.TakeDamage(bonusDamage);
        }

        public int CountBleedingEnemies()
        {
            if (_turnManager == null) return 0;
            int count = 0;
            var participants = _turnManager.Participants;
            if (participants == null) return 0;

            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant p = participants[i];
                if (p == null || p.IsAlly || p.IsDead) continue;
                Enemy enemy = p as Enemy;
                if (enemy == null || enemy.BuffReceiver == null) continue;
                if (enemy.BuffReceiver.HasBuff(BleedBuffId))
                    count++;
            }
            return count;
        }

        public int CountAttackersInTeam()
        {
            if (_turnManager == null) return 0;
            int count = 0;
            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return 0;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.ActiveSpec == null) continue;
                if (ally.ActiveSpec.Role == CharacterRole.Attacker)
                    count++;
            }
            return count;
        }

        public bool IsBrookeOnlyAttacker(int attackerCount)
        {
            if (_owner == null || _owner.ActiveSpec == null) return false;
            if (_owner.ActiveSpec.Role != CharacterRole.Attacker) return false;
            return attackerCount == 1;
        }

        public void SyncSoloFlatBuff(bool isOnlyAttacker)
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.RemoveBuffsById(SoloFlatAtkBuffId);
            if (!isOnlyAttacker) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = SoloFlatAtkBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = 50f,
                IsPercent = false,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public void ApplyBleedSwitchBonus()
        {
            if (!IsAtkSpecActive() || _owner == null || _owner.BuffReceiver == null) return;
            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = BleedSwitchAtkBuffId,
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

        public void ApplySoloSwitchBonus()
        {
            if (!IsAtkSpecActive() || _owner == null || _owner.BuffReceiver == null) return;
            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = SoloSwitchDmgAmpBuffId,
                Source = _owner,
                StatType = BuffStatType.DamageAmplification,
                Value = 0.10f,
                IsPercent = true,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public void ApplyLadiesSwitchBonus()
        {
            if (!IsAtkSpecActive() || _turnManager == null || _owner == null) return;
            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;
                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = LadiesSwitchTeamAtkBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = 0.05f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = 1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
        }

        public void ResetAtkLaunchState()
        {
            _ladiesFirstContact = FirstContact.None;
            _ladiesBuffApplied = false;
            if (_owner != null && _owner.BuffReceiver != null)
                _owner.BuffReceiver.RemoveBuffsById(LadiesAtkBuffId);
        }

        public void ResetAtkStageState()
        {
            _enemiesBleedByAtk.Clear();
            ResetAtkLaunchState();
        }

        public void ApplyGiftOnAllyHit(CharacterBall ally)
        {
            if (!IsSupSpecActive()) return;
            if (ally == null || ally.IsDead || ally.BuffReceiver == null || ally.ActiveSpec == null) return;

            int cycles = _giftExtendedDuration ? 3 : 2;
            BuffReceiver br = ally.BuffReceiver;
            br.RemoveBuffsById(GiftBuffId);

            if (ally.ActiveSpec.Role == CharacterRole.Defender)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = GiftBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DEF,
                    Value = 0.20f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = cycles,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
            else if (ally.ActiveSpec.Role == CharacterRole.Attacker)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = GiftBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = 0.20f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = cycles,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
            else
            {
                br.AddBuff(new BuffData
                {
                    BuffId = GiftBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = 0.10f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = cycles,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
                br.AddBuff(new BuffData
                {
                    BuffId = GiftBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DEF,
                    Value = 0.10f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = cycles,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
        }

        public void TryTrampolineAlly(CharacterBall ally)
        {
            if (!IsSupSpecActive()) return;
            if (ally == null || ally.IsDead) return;
            if (_trampolinedAlliesThisTurn.Contains(ally)) return;

            _trampolinedAlliesThisTurn.Add(ally);
            Rigidbody2D allyRb = ally.GetComponent<Rigidbody2D>();
            if (allyRb != null)
                allyRb.velocity *= 1.50f;
        }

        public void ApplyGiftSwitchBonus()
        {
            if (!IsSupSpecActive()) return;
            _giftExtendedDuration = true;
        }

        public void ApplyBalmSwitchBonus()
        {
            if (!IsSupSpecActive()) return;
            _balmBoostActive = true;
        }

        public void HealAllAlliesBalmEndOfTurn()
        {
            if (!IsSupSpecActive()) return;
            if (_turnManager == null) return;

            float healPercent = _balmBoostActive ? 0.05f : 0.03f;
            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                int heal = Mathf.RoundToInt(ally.MaxHp * healPercent);
                if (heal > 0) ally.Heal(heal);
            }

            _balmBoostActive = false;
        }

        public void IncrementAuraStack()
        {
            if (!IsSupSpecActive()) return;
            if (_auraStacks >= 20) return;
            _auraStacks++;
            SyncAuraBuffs();
        }

        public void SyncAuraBuffs()
        {
            if (_turnManager == null || _owner == null) return;
            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return;

            float dr = _auraStacks * 0.01f;
            bool atkBonus = _auraStacks >= 20;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;

                BuffReceiver br = ally.BuffReceiver;
                br.RemoveBuffsById(AuraDRBuffId);
                br.RemoveBuffsById(AuraAtkBuffId);

                if (_auraStacks > 0)
                {
                    br.AddBuff(new BuffData
                    {
                        BuffId = AuraDRBuffId,
                        Source = _owner,
                        StatType = BuffStatType.DamageReduction,
                        Value = dr,
                        IsPercent = true,
                        RemainingTurns = -1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });
                }

                if (atkBonus)
                {
                    br.AddBuff(new BuffData
                    {
                        BuffId = AuraAtkBuffId,
                        Source = _owner,
                        StatType = BuffStatType.ATK,
                        Value = 0.05f,
                        IsPercent = true,
                        RemainingTurns = -1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });
                }
            }
        }

        public void HandleSpecSwitchState()
        {
            if (IsSupSpecActive()) return;
            if (_auraStacks <= 0) return;
            _auraStacks = 0;
            SyncAuraBuffs();
        }

        public void ResetSupStageState()
        {
            _trampolinedAlliesThisTurn.Clear();
            _balmBoostActive = false;
            _giftExtendedDuration = false;
        }

        public void SyncHighFiveBuff()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.RemoveBuffsById(HighFiveDefBuffId);
            if (_highFiveStacks <= 0) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = HighFiveDefBuffId,
                Source = _owner,
                StatType = BuffStatType.DEF,
                Value = _highFiveStacks,
                IsPercent = false,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public void ResetDefStageState()
        {
            _armorStacks = 0;
            _surviveUsedThisStage = false;
            ApplyArmorDR();
        }

        public bool IsDefSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == -1;
        }

        public bool IsAtkSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == 0;
        }

        public bool IsSupSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == 1;
        }

        private void HandleTurnChanged(ITurnParticipant participant)
        {
            _ = participant;
            if (_owner == null || _owner.BuffReceiver == null) return;

            if (IsDefSpecActive() && !_owner.BuffReceiver.HasBuff(ArmorDRBuffId))
                ApplyArmorDR();
            else if (!IsDefSpecActive())
            {
                _owner.BuffReceiver.RemoveBuffsById(ArmorDRBuffId);
                _owner.BuffReceiver.RemoveBuffsById(ArmorDRBoostBuffId);
            }

            if (!IsAtkSpecActive())
            {
                _enemiesBleedByAtk.Clear();
                ResetAtkLaunchState();
            }

            if (ReferenceEquals(participant, _owner))
            {
                _trampolinedAlliesThisTurn.Clear();
                if (IsSupSpecActive())
                    IncrementAuraStack();
            }
        }

        private void OnOwnerStopped()
        {
            ResetAtkLaunchState();
            if (IsSupSpecActive())
                HealAllAlliesBalmEndOfTurn();
            _giftExtendedDuration = false;
        }

        private void OnAllyHitForLadies(CharacterBall ally)
        {
            if (_ladiesBuffApplied) return;
            if (ally == null || ally.IsDead) return;

            if (_ladiesFirstContact == FirstContact.None)
                _ladiesFirstContact = FirstContact.Ally;

            if (_ladiesFirstContact != FirstContact.Ally) return;

            if (_owner != null && _owner.BuffReceiver != null)
            {
                _owner.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = LadiesAtkBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = 0.30f,
                    IsPercent = true,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
            _ladiesBuffApplied = true;
        }

        private void OnEnemyHitForLadies()
        {
            if (_ladiesBuffApplied) return;

            if (_ladiesFirstContact == FirstContact.None)
                _ladiesFirstContact = FirstContact.Enemy;

            if (_ladiesFirstContact != FirstContact.Enemy) return;

            if (_owner != null && _owner.BuffReceiver != null)
            {
                _owner.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = LadiesAtkBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = -0.10f,
                    IsPercent = true,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
            _ladiesBuffApplied = true;
        }

        private void OnDestroy()
        {
            if (_subscribedToTurnChanged && _turnManager != null)
                _turnManager.OnTurnChanged -= HandleTurnChanged;
            if (_subscribedToOwnerStopped && _owner != null)
                _owner.OnStopped -= OnOwnerStopped;
            if (_subscribedToHitEnemy && _owner != null)
                _owner.OnHitEnemy -= OnEnemyHitForLadies;
            if (_subscribedToHitAlly && _owner != null)
                _owner.OnHitAllyEvent -= OnAllyHitForLadies;
        }
    }
}

