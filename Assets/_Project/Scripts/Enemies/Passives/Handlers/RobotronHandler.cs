using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de Robotron : buffs permanents, stacks sans dégâts et mode fantôme à bas HP.
    /// </summary>
    public class RobotronHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_ATK_PERM = "robotron_atk_perm";
        private const string BUFF_DEF_PERM = "robotron_def_perm";
        private const string BUFF_ATK_STACK = "robotron_atk_stack";
        private const string BUFF_DEF_STACK = "robotron_def_stack";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _noHitTurns;
        private bool _phantomTriggered;
        private bool _tookDamageThisTurn;
        private int _noHitStacks;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "robotron";

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════
        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _noHitTurns = 0;
            _phantomTriggered = false;
            _tookDamageThisTurn = false;
            _noHitStacks = 0;

            ApplyBuff(BUFF_ATK_PERM, BuffStatType.ATK, 0.10f, true, -1, -1, true);
            ApplyBuff(BUFF_DEF_PERM, BuffStatType.DEF, 0.10f, true, -1, -1, true);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnTurnStart()
        {
            if (!IsReady) return;
            _tookDamageThisTurn = false;
        }

        public override void OnCycleStart()
        {
            if (!IsReady) return;

            if (!_tookDamageThisTurn)
            {
                _noHitTurns++;
                _noHitStacks = Mathf.Min(_noHitStacks + 1, 5);
                float stackBonus = _noHitStacks * 0.10f;
                ApplyBuff(BUFF_ATK_STACK, BuffStatType.ATK, stackBonus, true, -1, -1, true);
                ApplyBuff(BUFF_DEF_STACK, BuffStatType.DEF, stackBonus, true, -1, -1, true);
            }
            else
            {
                _noHitTurns = 0;
                _noHitStacks = 0;
                RemoveBuff(BUFF_ATK_STACK);
                RemoveBuff(BUFF_DEF_STACK);
            }

            _tookDamageThisTurn = false;
        }

        public override void OnTakeDamage(int damage)
        {
            if (!IsReady) return;
            _tookDamageThisTurn = true;

            if (!_phantomTriggered)
            {
                float ratio = _owner.MaxHp > 0 ? (float)_owner.CurrentHp / _owner.MaxHp : 0f;
                if (ratio <= 0.20f)
                {
                    _phantomTriggered = true;

                    int phantomHeal = Mathf.RoundToInt(_owner.MaxHp * 0.20f);
                    _owner.Heal(phantomHeal);

                    ApplyBuff("robotron_phantom_atk", BuffStatType.ATK, 0.50f, true, -1, -1, true);
                    ApplyBuff("robotron_phantom_def", BuffStatType.DEF, 0.50f, true, -1, -1, true);
                }
            }
        }

        public override void ResetForNewStage()
        {
            _noHitTurns = 0;
            _noHitStacks = 0;
            _phantomTriggered = false;
            _tookDamageThisTurn = false;
            RemoveBuff(BUFF_ATK_STACK);
            RemoveBuff(BUFF_DEF_STACK);
            RemoveBuff("robotron_phantom_atk");
            RemoveBuff("robotron_phantom_def");
            // Ne reset PAS BUFF_ATK_PERM et BUFF_DEF_PERM
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
