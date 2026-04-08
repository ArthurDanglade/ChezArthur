using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de Contremaître : soutien d'allié puis mode berserk en dernier survivant.
    /// </summary>
    public class ContreMaitreHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_ATK = "contremaitre_atk";
        private const string BUFF_DEF = "contremaitre_def_malus";
        private const string BUFF_SPD = "contremaitre_spd";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _berserkActive;
        private int _berserkAtkStacks;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "contremaitre";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnTurnStart()
        {
            if (!IsReady) return;

            if (!_berserkActive)
            {
                Enemy[] allEnemies = UnityEngine.Object.FindObjectsOfType<Enemy>();
                List<Enemy> livingOthers = new List<Enemy>();
                for (int i = 0; i < allEnemies.Length; i++)
                {
                    Enemy e = allEnemies[i];
                    if (e == null || e.IsDead || e == _owner) continue;
                    livingOthers.Add(e);
                }

                if (livingOthers.Count > 0)
                {
                    Enemy target = livingOthers[Random.Range(0, livingOthers.Count)];
                    int healAmount = Mathf.RoundToInt(target.MaxHp * 0.10f);
                    if (healAmount > 0)
                        target.Heal(healAmount);
                }
            }

            if (_berserkActive)
            {
                _berserkAtkStacks++;
                float totalAtk = 0.60f + (_berserkAtkStacks * 0.05f);
                ApplyBuff(BUFF_ATK, BuffStatType.ATK, totalAtk, true, -1, -1, true);
            }
        }

        public override void OnMateKilled(Enemy mate)
        {
            if (!IsReady) return;
            if (_berserkActive) return;

            Enemy[] allEnemies = UnityEngine.Object.FindObjectsOfType<Enemy>();
            int aliveCount = 0;
            for (int i = 0; i < allEnemies.Length; i++)
            {
                if (allEnemies[i] == null || allEnemies[i].IsDead) continue;
                aliveCount++;
            }

            if (aliveCount == 1 && !_owner.IsDead)
            {
                _berserkActive = true;
                _berserkAtkStacks = 0;
                ApplyBuff(BUFF_ATK, BuffStatType.ATK, 0.60f, true, -1, -1, true);
                ApplyBuff(BUFF_DEF, BuffStatType.DEF, -0.30f, true, -1, -1, true);
                ApplyBuff(BUFF_SPD, BuffStatType.Speed, 0.40f, true, -1, -1, true);
            }
        }

        public override void ResetForNewStage()
        {
            _berserkActive = false;
            _berserkAtkStacks = 0;
            RemoveBuff(BUFF_ATK);
            RemoveBuff(BUFF_DEF);
            RemoveBuff(BUFF_SPD);
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
