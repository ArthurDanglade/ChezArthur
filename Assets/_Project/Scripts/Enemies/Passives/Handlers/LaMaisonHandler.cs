using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de La Maison : dégâts cycliques sur alliés et interception partielle des soins.
    /// </summary>
    public class LaMaisonHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_ATK = "maison_atk";
        private const string BUFF_DEF = "maison_def";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _interceptedHealCount;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "la_maison";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnCycleStart()
        {
            if (!IsReady) return;

            IReadOnlyList<CharacterBall> allies = _turnManager?.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                int dmg = Mathf.RoundToInt(ally.MaxHp * 0.05f);
                if (dmg > 0)
                    ally.TakeDamageUnreducible(dmg);
            }
        }

        public override void OnAllyHealed(CharacterBall ally, int healAmount)
        {
            if (!IsReady) return;
            if (ally == null || healAmount <= 0) return;

            int intercepted = Mathf.RoundToInt(healAmount * 0.50f);
            if (intercepted <= 0) return;

            ally.TakeDamageUnreducible(intercepted);

            _interceptedHealCount++;
            float bonus = _interceptedHealCount * 0.05f;
            ApplyBuff(BUFF_ATK, BuffStatType.ATK, bonus, true, -1, -1, true);
            ApplyBuff(BUFF_DEF, BuffStatType.DEF, bonus, true, -1, -1, true);
        }

        public override void ResetForNewStage()
        {
            _interceptedHealCount = 0;
            RemoveBuff(BUFF_ATK);
            RemoveBuff(BUFF_DEF);
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
