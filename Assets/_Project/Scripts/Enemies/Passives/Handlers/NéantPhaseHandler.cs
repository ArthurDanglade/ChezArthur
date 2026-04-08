using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler du Néant : 3 phases HP avec annulation de buffs, miss chance et exécution du plus faible.
    /// </summary>
    public class NéantPhaseHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // TYPES PRIVÉS
        // ═══════════════════════════════════════════

        private enum NeantPhase
        {
            Phase1 = 1,
            Phase2 = 2,
            Phase3 = 3
        }

        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const string BUFF_ATK = "neant_atk_cancel";
        private const string BUFF_DEF = "neant_def_cancel";
        private const string BUFF_SPD = "neant_spd_miss";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private int _currentPhase;
        private int _buffsCancelledTotal;
        private int _missAppliedThisCycle;
        private float _spdBonusPhase2;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════

        public override string HandlerId => "neant_phase";

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _currentPhase = (int)NeantPhase.Phase1;
            _buffsCancelledTotal = 0;
            _missAppliedThisCycle = 0;
            _spdBonusPhase2 = 0f;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public override void OnHpChanged(int currentHp, int maxHp)
        {
            if (!IsReady)
                return;

            float ratio = maxHp > 0 ? (float)currentHp / maxHp : 0f;
            if (ratio > 0.60f)
                _currentPhase = (int)NeantPhase.Phase1;
            else if (ratio > 0.30f)
                _currentPhase = (int)NeantPhase.Phase2;
            else
                _currentPhase = (int)NeantPhase.Phase3;
        }

        public override void OnCycleStart()
        {
            if (!IsReady)
                return;

            IReadOnlyList<CharacterBall> allies = _turnManager != null ? _turnManager.GetAllies() : null;
            if (allies == null)
                return;

            // Phase 1 : annulation de buffs alliés + scaling ATK/DEF.
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;

                BuffReceiver br = ally.BuffReceiver;
                if (br == null) continue;

                int countBefore = br.ActiveBuffs != null ? br.ActiveBuffs.Count : 0;
                br.ClearAll();
                int countAfter = br.ActiveBuffs != null ? br.ActiveBuffs.Count : 0;
                int cancelled = countBefore - countAfter;
                if (cancelled > 0)
                    _buffsCancelledTotal += cancelled;
            }

            if (_buffsCancelledTotal > 0)
            {
                float bonus = _buffsCancelledTotal * 0.05f;
                ApplyBuff(BUFF_ATK, BuffStatType.ATK, bonus, true, -1, -1, true);
                ApplyBuff(BUFF_DEF, BuffStatType.DEF, bonus, true, -1, -1, true);
            }

            // Phase 2+ : chance de rater + gain SPD.
            if (_currentPhase >= (int)NeantPhase.Phase2)
            {
                for (int i = 0; i < allies.Count; i++)
                {
                    CharacterBall ally = allies[i];
                    if (ally == null || ally.IsDead) continue;

                    if (Random.value < 0.30f)
                    {
                        ApplyBuffToAlly(ally, "neant_miss_" + ally.GetInstanceID(), BuffStatType.MissChance, 1f, false, 1, -1, false);
                        _missAppliedThisCycle++;
                    }
                }

                if (_missAppliedThisCycle > 0)
                {
                    float spdGain = _missAppliedThisCycle * 0.10f;
                    _spdBonusPhase2 += spdGain;
                    ApplyBuff(BUFF_SPD, BuffStatType.Speed, _spdBonusPhase2, true, -1, -1, true);
                    _missAppliedThisCycle = 0;
                }
            }
        }

        public override void OnTurnStart()
        {
            if (!IsReady)
                return;

            if (_currentPhase < (int)NeantPhase.Phase3)
                return;

            IReadOnlyList<CharacterBall> allies = _turnManager != null ? _turnManager.GetAllies() : null;
            if (allies == null)
                return;

            CharacterBall weakest = null;
            int minHp = int.MaxValue;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                if (ally.CurrentHp < minHp)
                {
                    minHp = ally.CurrentHp;
                    weakest = ally;
                }
            }

            if (weakest != null)
            {
                int dmg = Mathf.RoundToInt(weakest.MaxHp * 0.15f);
                if (dmg > 0)
                    weakest.TakeDamageUnreducible(dmg);
            }
        }

        public override void OnAllyKilled(CharacterBall ally)
        {
            if (!IsReady)
                return;

            if (_currentPhase < (int)NeantPhase.Phase3)
                return;

            int heal = Mathf.RoundToInt(_owner.MaxHp * 0.10f);
            if (heal > 0)
                _owner.Heal(heal);
        }

        public override void ResetForNewStage()
        {
            _currentPhase = (int)NeantPhase.Phase1;
            _buffsCancelledTotal = 0;
            _missAppliedThisCycle = 0;
            _spdBonusPhase2 = 0f;

            if (_owner?.BuffReceiver != null)
            {
                _owner.BuffReceiver.RemoveBuffsById(BUFF_ATK);
                _owner.BuffReceiver.RemoveBuffsById(BUFF_DEF);
                _owner.BuffReceiver.RemoveBuffsById(BUFF_SPD);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyBuff(string buffId, BuffStatType stat, float value, bool isPercent,
            int durationTurns = -1, int durationCycles = -1, bool uniqueGlobal = true)
        {
            if (_owner?.BuffReceiver == null) return;
            var buff = new BuffData
            {
                BuffId = buffId,
                Source = null,
                StatType = stat,
                Value = value,
                IsPercent = isPercent,
                RemainingTurns = durationTurns,
                RemainingCycles = durationCycles,
                UniqueGlobal = uniqueGlobal,
                UniquePerSource = false
            };
            _owner.BuffReceiver.AddBuff(buff);
        }

        private void RemoveBuff(string buffId)
        {
            _owner?.BuffReceiver?.RemoveBuffsById(buffId);
        }

        private void ApplyBuffToAlly(CharacterBall ally, string buffId, BuffStatType stat, float value, bool isPercent,
            int durationTurns = -1, int durationCycles = -1, bool uniqueGlobal = true)
        {
            if (ally?.BuffReceiver == null) return;
            var buff = new BuffData
            {
                BuffId = buffId,
                Source = null,
                StatType = stat,
                Value = value,
                IsPercent = isPercent,
                RemainingTurns = durationTurns,
                RemainingCycles = durationCycles,
                UniqueGlobal = uniqueGlobal,
                UniquePerSource = false
            };
            ally.BuffReceiver.AddBuff(buff);
        }
    }
}

