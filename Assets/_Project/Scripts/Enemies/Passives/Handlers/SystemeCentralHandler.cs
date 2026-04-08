using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de Système Central : ATK par alliés morts, bouclier cyclique et mode surcharge à bas HP.
    /// </summary>
    public class SystemeCentralHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_ATK_MATES = "sysC_atk_mates";
        private const string BUFF_ATK_SURGE = "sysC_atk_surge";
        private const string BUFF_SPD_SURGE = "sysC_spd_surge";
        private const string BUFF_DEF_SURGE = "sysC_def_malus_surge";
        private const string SHIELD_ID = "sysC_shield";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _matesDeadCount;
        private bool _surchargeActive;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "systeme_central";

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════
        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _matesDeadCount = 0;
            _surchargeActive = false;
            ArmShield();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnCycleStart()
        {
            if (!IsReady) return;
            ArmShield();
        }

        public override void OnMateKilled(Enemy mate)
        {
            if (!IsReady) return;
            _matesDeadCount++;

            float totalBonus = _matesDeadCount * 0.15f;
            ApplyBuff(BUFF_ATK_MATES, BuffStatType.ATK, totalBonus, true, -1, -1, true);
        }

        public override void OnHpChanged(int currentHp, int maxHp)
        {
            if (!IsReady) return;

            float ratio = maxHp > 0 ? (float)currentHp / maxHp : 0f;
            if (ratio <= 0.25f && !_surchargeActive)
            {
                _surchargeActive = true;
                ApplyBuff(BUFF_ATK_SURGE, BuffStatType.ATK, 1.0f, true, -1, -1, true);
                ApplyBuff(BUFF_SPD_SURGE, BuffStatType.Speed, 1.0f, true, -1, -1, true);
                ApplyBuff(BUFF_DEF_SURGE, BuffStatType.DEF, -0.50f, true, -1, -1, true);
            }
        }

        public override void ResetForNewStage()
        {
            _matesDeadCount = 0;
            _surchargeActive = false;
            RemoveBuff(BUFF_ATK_MATES);
            RemoveBuff(BUFF_ATK_SURGE);
            RemoveBuff(BUFF_SPD_SURGE);
            RemoveBuff(BUFF_DEF_SURGE);
            RemoveBuff(SHIELD_ID);
            ArmShield();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void ArmShield()
        {
            if (_owner?.BuffReceiver == null) return;

            int shieldHp = Mathf.RoundToInt(_owner.MaxHp * 0.10f);
            _ = shieldHp;

            ApplyBuff(SHIELD_ID, BuffStatType.DamageReduction,
                0.15f, true, durationTurns: -1, durationCycles: 1, uniqueGlobal: true);
        }

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
