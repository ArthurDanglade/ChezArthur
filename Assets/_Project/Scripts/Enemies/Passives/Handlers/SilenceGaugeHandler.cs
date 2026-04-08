using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Jauge de silence : dégâts remplissent la jauge, puis buffs ATK/DEF ; résistance tant que la jauge n'est pas pleine.
    /// </summary>
    public class SilenceGaugeHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const string BUFF_ID_ATK = "silence_atk";
        private const string BUFF_ID_DEF = "silence_def";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private float _gaugeValue;
        private bool _gaugeFull;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════

        public override string HandlerId => "silence_gauge";

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES (runtime / conditions)
        // ═══════════════════════════════════════════

        /// <summary> Remplissage actuel de la jauge (0–1). </summary>
        public float GaugeValue => _gaugeValue;

        /// <summary> True lorsque la jauge a atteint 100 %. </summary>
        public bool GaugeFull => _gaugeFull;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public override void OnTakeDamage(int damage)
        {
            if (!IsReady)
                return;

            if (_gaugeFull)
                return;

            if (damage <= 0 || _owner.MaxHp <= 0)
                return;

            float fill = damage / (float)_owner.MaxHp;
            _gaugeValue = Mathf.Min(1f, _gaugeValue + fill);

            if (_gaugeValue >= 1f && !_gaugeFull)
            {
                _gaugeFull = true;
                RemoveBuff("silence_resistance");
                ApplyBuff(BUFF_ID_ATK, BuffStatType.ATK, 0.5f, true, -1, -1, true);
                ApplyBuff(BUFF_ID_DEF, BuffStatType.DEF, 0.5f, true, -1, -1, true);
            }
        }

        public override void OnHpChanged(int currentHp, int maxHp)
        {
            if (!IsReady)
                return;

            if (_gaugeFull)
            {
                RemoveBuff("silence_resistance");
                return;
            }

            ApplyBuff("silence_resistance", BuffStatType.DamageReduction, 0.3f, true, -1, -1, true);
        }

        public override void ResetForNewStage()
        {
            _gaugeValue = 0f;
            _gaugeFull = false;
            if (_owner != null && _owner.BuffReceiver != null)
            {
                BuffReceiver br = _owner.BuffReceiver;
                br.RemoveBuffsById(BUFF_ID_ATK);
                br.RemoveBuffsById(BUFF_ID_DEF);
                br.RemoveBuffsById("silence_resistance");
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
    }
}
