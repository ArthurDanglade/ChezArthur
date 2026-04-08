using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de Cha Teuh : chance offensive/défensive et buff de survie.
    /// </summary>
    public class ChateuhHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_ATK = "chateuh_atk";
        private const string BUFF_DEF_LUCKY = "chateuh_def_lucky";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _luckyDefUsed;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "cha_teuh";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnTurnStart()
        {
            if (!IsReady) return;

            RemoveBuff(BUFF_ATK);
            if (Random.value < 0.5f)
            {
                ApplyBuff(BUFF_ATK, BuffStatType.ATK, 0.50f, true,
                    durationTurns: 1, durationCycles: -1, uniqueGlobal: true);
            }
        }

        public override void OnTakeDamage(int damage)
        {
            if (!IsReady) return;
            if (damage <= 0) return;

            if (Random.value < 0.30f)
            {
                int heal = Mathf.RoundToInt(damage * 0.50f);
                if (heal > 0)
                    _owner.Heal(heal);
            }
        }

        public override void OnHpChanged(int currentHp, int maxHp)
        {
            if (!IsReady) return;
            if (_luckyDefUsed) return;

            if (currentHp <= 1 && maxHp > 1)
            {
                _luckyDefUsed = true;
                ApplyBuff(BUFF_DEF_LUCKY, BuffStatType.DEF, 0.20f, true, -1, -1, true);
            }
        }

        public override void ResetForNewStage()
        {
            _luckyDefUsed = false;
            RemoveBuff(BUFF_ATK);
            RemoveBuff(BUFF_DEF_LUCKY);
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
