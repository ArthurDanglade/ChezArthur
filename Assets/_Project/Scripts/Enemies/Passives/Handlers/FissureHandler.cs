using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de Fissure : mode A (SPD cumulable) ou mode B (ATK cumulable puis reset).
    /// </summary>
    public class FissureHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_SPD = "fissure_spd";
        private const string BUFF_ATK = "fissure_atk";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _modeA;
        private float _spdBonus;
        private float _atkBonus;
        private int _turnsNotAttacked;
        private bool _doubleAttackActive;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "fissure";

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════
        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _modeA = (_data != null && _data.SpecialValue1 < 0.5f);
            _spdBonus = 0f;
            _atkBonus = 0f;
            _turnsNotAttacked = 0;
            _doubleAttackActive = false;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnTakeDamage(int damage)
        {
            if (!IsReady || damage <= 0) return;

            if (_modeA)
            {
                _spdBonus += 0.15f;
                ApplyBuff(BUFF_SPD, BuffStatType.Speed, _spdBonus, true, -1, -1, true);
                if (_spdBonus >= 0.75f && !_doubleAttackActive)
                {
                    _doubleAttackActive = true;
                    ApplyBuff("fissure_double_atk", BuffStatType.ATK, 1.0f, true, -1, -1, true);
                }
            }
            else
            {
                _turnsNotAttacked = 0;
                _atkBonus += 0.20f;
                _atkBonus = Mathf.Min(_atkBonus, 1.0f);
                ApplyBuff(BUFF_ATK, BuffStatType.ATK, _atkBonus, true, -1, -1, true);
            }
        }

        public override void OnTurnStart()
        {
            if (!IsReady) return;
            if (_modeA) return;

            _turnsNotAttacked++;
            if (_turnsNotAttacked >= 2)
            {
                _atkBonus = 0f;
                _turnsNotAttacked = 0;
                RemoveBuff(BUFF_ATK);
            }
        }

        public override void ResetForNewStage()
        {
            _spdBonus = 0f;
            _atkBonus = 0f;
            _turnsNotAttacked = 0;
            _doubleAttackActive = false;
            RemoveBuff(BUFF_SPD);
            RemoveBuff(BUFF_ATK);
            RemoveBuff("fissure_double_atk");
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
    }
}
