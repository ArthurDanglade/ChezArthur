using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système central d'Ardacula (ATK/SUP) :
    /// lifesteal, marque de sang, rebonds no-decay, pacte, masochiste, transfusion.
    /// </summary>
    public class ArdaculaSystem : MonoBehaviour
    {
        private const string BloodMarkBuffId = "ardacula_bloodmark";
        private const string TransfusionPactBuffId = "ardacula_transfusion_def";
        private const string TransfusionMarkerBuffId = "ardacula_transfusion_marker";
        private const string AtkSwitchBonusBuffId = "ardacula_atk_switch_atk";
        private const string NightflightSwitchBonusBuffId = "ardacula_nightflight_switch_atk";

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private CharacterPassiveRuntime _runtime;

        private Enemy _markedEnemy;
        private Action _markedEnemyDeathHandler;
        private bool _switchAtkBonusMark;

        private int _bounceCountThisLaunch;
        private bool _noBounceDecayActive;

        private bool _freeHealActive;
        private int _masochistStacks;

        private int _alliesHitThisLaunch;
        private bool _transfusionThresholdReduced;
        private readonly HashSet<CharacterBall> _alliesHitSet = new HashSet<CharacterBall>();

        private bool _subscribedToOwnerStopped;

        public void Initialize(CharacterBall owner, TurnManager tm)
        {
            if (owner == null) return;

            if (_subscribedToOwnerStopped && _owner != null && !ReferenceEquals(_owner, owner))
            {
                _owner.OnStopped -= OnOwnerStopped;
                _subscribedToOwnerStopped = false;
            }

            _owner = owner;
            _turnManager = tm;
            _runtime = _owner.GetComponent<CharacterPassiveRuntime>();

            _noBounceDecayActive = IsAtkSpecActive();

            if (!_subscribedToOwnerStopped)
            {
                _owner.OnStopped += OnOwnerStopped;
                _subscribedToOwnerStopped = true;
            }
        }

        public void ApplyLifesteal(int baseDamage)
        {
            if (!IsAtkSpecActive()) return;
            if (_owner == null) return;

            int heal = Mathf.RoundToInt(baseDamage * 0.15f);
            if (heal > 0)
                _owner.Heal(heal);
        }

        public void TryApplyBloodMark(Enemy enemy)
        {
            if (!IsAtkSpecActive()) return;
            if (enemy == null || enemy.IsDead) return;

            if (_markedEnemy != null && !_markedEnemy.IsDead)
                return; // Une seule marque active, ne bouge pas tant que la cible vit.

            ApplyMarkOnEnemy(enemy);
        }

        public void RegisterBounce()
        {
            _bounceCountThisLaunch++;
        }

        public bool ShouldBypassDecay()
        {
            return _noBounceDecayActive && _bounceCountThisLaunch < 5;
        }

        public void OnAllyHitForPact(CharacterBall ally)
        {
            if (!IsSupSpecActive()) return;
            if (_owner == null || ally == null || ally.IsDead || ReferenceEquals(ally, _owner)) return;

            int allyHeal = Mathf.RoundToInt(ally.MaxHp * 0.05f);
            if (allyHeal > 0)
                ally.Heal(allyHeal);

            if (_freeHealActive)
            {
                _freeHealActive = false;
                return;
            }

            int sacrifice = Mathf.RoundToInt(_owner.MaxHp * 0.03f);
            if (sacrifice > 0)
            {
                _owner.TakePureDamage(sacrifice);
                AddMasochistStack();
            }
        }

        public void AddMasochistStack()
        {
            if (_masochistStacks >= 10) return;
            _masochistStacks++;
        }

        public float GetMasochistBonus()
        {
            return _masochistStacks * 0.03f;
        }

        public void OnAllyHitForTransfusion(CharacterBall ally)
        {
            if (!IsSupSpecActive()) return;
            if (ally == null || ally.IsDead || ReferenceEquals(ally, _owner)) return;
            if (_alliesHitSet.Contains(ally)) return;

            _alliesHitSet.Add(ally);
            _alliesHitThisLaunch++;
        }

        public void ApplyAtkSwitchBonus()
        {
            if (!IsAtkSpecActive()) return;
            ApplyTurnBuff(AtkSwitchBonusBuffId, BuffStatType.ATK, 0.10f);
            _switchAtkBonusMark = true;
        }

        public void ApplyNightflightSwitchBonus()
        {
            if (!IsAtkSpecActive()) return;
            ApplyTurnBuff(NightflightSwitchBonusBuffId, BuffStatType.ATK, 0.15f);
        }

        public void ApplyBloodpactSwitchBonus()
        {
            if (!IsSupSpecActive()) return;
            _freeHealActive = true;
        }

        public void ApplyMasochistSwitchBonus()
        {
            if (!IsSupSpecActive()) return;
            AddMasochistStack();
            AddMasochistStack();
        }

        public void ApplyTransfusionSwitchBonus()
        {
            if (!IsSupSpecActive()) return;
            _transfusionThresholdReduced = true;
        }

        public void ResetForStage()
        {
            _masochistStacks = 0;
            _alliesHitThisLaunch = 0;
            _alliesHitSet.Clear();
            _freeHealActive = false;
            _transfusionThresholdReduced = false;
            _bounceCountThisLaunch = 0;

            ClearMarkedEnemy();
            RemoveTransfusionBuffsFromAllies();
        }

        private void OnOwnerStopped()
        {
            int threshold = _transfusionThresholdReduced ? 1 : 2;
            if (_alliesHitThisLaunch >= threshold)
                ApplyTransfusionPactToTouchedAllies();

            _alliesHitThisLaunch = 0;
            _alliesHitSet.Clear();
            _bounceCountThisLaunch = 0;
            _transfusionThresholdReduced = false;
            _freeHealActive = false;
            _switchAtkBonusMark = false;
            _noBounceDecayActive = IsAtkSpecActive();
        }

        private void ApplyMarkOnEnemy(Enemy enemy)
        {
            if (_owner == null || enemy == null || enemy.BuffReceiver == null) return;

            float amplification = _switchAtkBonusMark ? 0.25f : 0.15f;
            _switchAtkBonusMark = false;

            enemy.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = BloodMarkBuffId,
                Source = _owner,
                StatType = BuffStatType.DamageAmplification,
                Value = amplification,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            _markedEnemy = enemy;
            _markedEnemyDeathHandler = OnMarkedEnemyDeath;
            _markedEnemy.OnDeath += _markedEnemyDeathHandler;
        }

        private void OnMarkedEnemyDeath()
        {
            Vector3 deadPos = _markedEnemy != null ? _markedEnemy.transform.position : Vector3.zero;
            ClearMarkedEnemy();

            Enemy next = FindClosestAliveEnemy(deadPos);
            if (next != null)
                ApplyMarkOnEnemy(next);
        }

        private Enemy FindClosestAliveEnemy(Vector3 fromPos)
        {
            if (_turnManager == null) return null;

            float bestSqr = float.MaxValue;
            Enemy best = null;

            var participants = _turnManager.Participants;
            if (participants == null) return null;

            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant p = participants[i];
                if (p == null || p.IsAlly || p.IsDead) continue;

                Enemy enemy = p as Enemy;
                if (enemy == null || enemy.IsDead) continue;

                float sqr = (enemy.transform.position - fromPos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = enemy;
                }
            }

            return best;
        }

        private void ApplyTransfusionPactToTouchedAllies()
        {
            if (_owner == null) return;

            foreach (CharacterBall ally in _alliesHitSet)
            {
                if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;

                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = TransfusionMarkerBuffId,
                    Source = _owner,
                    StatType = BuffStatType.HP,
                    Value = 0f,
                    IsPercent = false,
                    RemainingTurns = -1,
                    RemainingCycles = 2,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });

                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = TransfusionPactBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DEF,
                    Value = 0.10f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = 2,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
        }

        private void RemoveTransfusionBuffsFromAllies()
        {
            if (_turnManager == null || _owner == null) return;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.BuffReceiver == null) continue;
                ally.BuffReceiver.RemoveBuffsById(TransfusionMarkerBuffId);
                ally.BuffReceiver.RemoveBuffsById(TransfusionPactBuffId);
            }
        }

        private void ClearMarkedEnemy()
        {
            if (_markedEnemy != null && _markedEnemyDeathHandler != null)
                _markedEnemy.OnDeath -= _markedEnemyDeathHandler;

            _markedEnemy = null;
            _markedEnemyDeathHandler = null;
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

        private bool IsAtkSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == -1;
        }

        private bool IsSupSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == 0;
        }

        private void OnDestroy()
        {
            if (_subscribedToOwnerStopped && _owner != null)
                _owner.OnStopped -= OnOwnerStopped;
            ClearMarkedEnemy();
        }
    }
}

