using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Dévotion du Skarabé : stacks DEF, puis rite ATK + immunité un tour.
    /// </summary>
    public class SkarabeDevotionHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const int MAX_DEVOTION = 10;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private int _devotionStacks;
        private bool _riteAccomplished;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════

        public override string HandlerId => "skarabe_devotion";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public override void OnTurnStart()
        {
            if (!IsReady)
                return;

            if (_riteAccomplished)
            {
                _riteAccomplished = false;
                _owner.ClearDamageImmunityAtTurnStart();
                // Repart sur un cycle de dévotion (évite de re-déclencher le rite chaque tour tant que stacks == MAX).
                _devotionStacks = 0;
                RemoveBuff("skarabe_def_devotion");
                return; // ne pas accumuler de stack ce même tour
            }

            if (_devotionStacks < MAX_DEVOTION)
            {
                _devotionStacks++;
                float defBonus = _devotionStacks * 0.03f;
                ApplyBuff("skarabe_def_devotion", BuffStatType.DEF, defBonus, true, -1, -1, true);
            }

            if (_devotionStacks == MAX_DEVOTION && !_riteAccomplished)
            {
                ApplyBuff("skarabe_atk_rite", BuffStatType.ATK, 0.5f, true, 1, -1, true);
                _owner.GrantDamageImmunityForOneEnemyTurn();
                _riteAccomplished = true;
            }
        }

        public override void ResetForNewStage()
        {
            _devotionStacks = 0;
            _riteAccomplished = false;
            if (_owner != null && _owner.BuffReceiver != null)
            {
                _owner.BuffReceiver.RemoveBuffsById("skarabe_def_devotion");
                _owner.BuffReceiver.RemoveBuffsById("skarabe_atk_rite");
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
