using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de Lonbou : soigne les alliés ennemis faibles et passe en berserk en dernier survivant.
    /// </summary>
    public class LonbouHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _berserkActive;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "lonbou";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnCycleStart()
        {
            if (!IsReady) return;
            if (_berserkActive) return;

            Enemy[] enemies = UnityEngine.Object.FindObjectsOfType<Enemy>();
            for (int i = 0; i < enemies.Length; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || enemy.IsDead || enemy == _owner) continue;
                if (enemy.CurrentHp >= enemy.MaxHp * 0.5f) continue;

                int amount = Mathf.RoundToInt(enemy.MaxHp * 0.05f);
                if (amount > 0)
                    enemy.Heal(amount);
            }
        }

        public override void OnMateKilled(Enemy mate)
        {
            if (!IsReady) return;
            if (_berserkActive) return;

            Enemy[] enemies = UnityEngine.Object.FindObjectsOfType<Enemy>();
            int aliveCount = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null || enemies[i].IsDead) continue;
                aliveCount++;
            }

            if (aliveCount == 1 && !_owner.IsDead)
            {
                _berserkActive = true;
                ApplyBuff("lonbou_berserk_atk", BuffStatType.ATK, 0.30f, true, -1, -1, true);
                ApplyBuff("lonbou_berserk_spd", BuffStatType.Speed, 0.20f, true, -1, -1, true);
            }
        }

        public override void ResetForNewStage()
        {
            _berserkActive = false;
            RemoveBuff("lonbou_berserk_atk");
            RemoveBuff("lonbou_berserk_spd");
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
