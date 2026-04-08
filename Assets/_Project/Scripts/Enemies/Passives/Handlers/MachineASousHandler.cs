using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Machine à sous : tirage chaque cycle, buffs ou malus selon les symboles.
    /// </summary>
    public class MachineASousHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // TYPES PRIVÉS
        // ═══════════════════════════════════════════

        private enum SlotSymbol
        {
            Bar,
            Cherry,
            Seven,
            Diamond
        }

        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const string BUFF_ATK = "machine_atk";
        private const string BUFF_DEF = "machine_def";
        private const string DEBUFF_DEF = "machine_debuff_def";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private int _lastResultHash;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════

        public override string HandlerId => "machine_a_sous";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public override void OnCycleStart()
        {
            if (!IsReady)
                return;

            SlotSymbol s1 = (SlotSymbol)Random.Range(0, 4);
            SlotSymbol s2 = (SlotSymbol)Random.Range(0, 4);
            SlotSymbol s3 = (SlotSymbol)Random.Range(0, 4);

            _lastResultHash = ((int)s1 << 8) | ((int)s2 << 4) | (int)s3;

            BuffReceiver br = _owner.BuffReceiver;
            if (br == null)
                return;

            br.RemoveBuffsById(BUFF_ATK);
            br.RemoveBuffsById(BUFF_DEF);
            br.RemoveBuffsById(DEBUFF_DEF);

            if (s1 == s2 && s2 == s3)
            {
                ApplyBuff(BUFF_ATK, BuffStatType.ATK, 0.5f, true, -1, 1, true);
                ApplyBuff(BUFF_DEF, BuffStatType.DEF, 0.5f, true, -1, 1, true);
                Debug.Log("[MachineASousHandler] JACKPOT !");
            }
            else if (s1 == s2 || s2 == s3 || s1 == s3)
                ApplyBuff(BUFF_ATK, BuffStatType.ATK, 0.25f, true, -1, 1, true);
            else
                ApplyBuff(DEBUFF_DEF, BuffStatType.DEF, -0.2f, true, -1, 1, true);
        }

        public override void ResetForNewStage()
        {
            _lastResultHash = 0;
            if (_owner?.BuffReceiver == null)
                return;

            BuffReceiver br = _owner.BuffReceiver;
            br.RemoveBuffsById(BUFF_ATK);
            br.RemoveBuffsById(BUFF_DEF);
            br.RemoveBuffsById(DEBUFF_DEF);
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
    }
}
