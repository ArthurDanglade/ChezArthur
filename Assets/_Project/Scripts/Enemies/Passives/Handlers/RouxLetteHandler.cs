using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de Roux Lette : alterne buffs rouge/noir selon un tirage aléatoire.
    /// </summary>
    public class RouxLetteHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_DEF_NOIR = "roux_lette_def_noir";
        private const string BUFF_ATK_ROUGE = "roux_lette_atk_rouge";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _noirActive;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "roux_lette";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnCycleStart()
        {
            if (!IsReady) return;

            if (_noirActive)
            {
                RemoveBuff(BUFF_DEF_NOIR);
                _noirActive = false;
            }
        }

        public override void OnTurnStart()
        {
            if (!IsReady) return;

            RemoveBuff(BUFF_ATK_ROUGE);

            if (Random.value < 0.5f)
            {
                ApplyBuff(BUFF_DEF_NOIR, BuffStatType.DEF, 0.30f, true, -1, -1, true);
                _noirActive = true;
            }
            else
            {
                ApplyBuff(BUFF_ATK_ROUGE, BuffStatType.ATK, 0.30f, true,
                    durationTurns: 1, durationCycles: -1, uniqueGlobal: true);
            }
        }

        public override void ResetForNewStage()
        {
            _noirActive = false;
            RemoveBuff(BUFF_DEF_NOIR);
            RemoveBuff(BUFF_ATK_ROUGE);
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
