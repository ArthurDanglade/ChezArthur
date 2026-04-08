using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de Robot Blindé : charges sur dégâts reçus et burst ATK périodique.
    /// </summary>
    public class RobotBlindeHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int MAX_CHARGES = 5;
        private const string BUFF_ATK = "robot_blinde_atk";
        private const string BUFF_DEF = "robot_blinde_def_perm";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _charges;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "robot_blinde";

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════
        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            ApplyBuff(BUFF_DEF, BuffStatType.DEF, 0.25f, true, -1, -1, true);
            _charges = 0;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnTakeDamage(int damage)
        {
            if (!IsReady) return;
            if (damage <= 0) return;

            _charges = Mathf.Min(_charges + 1, MAX_CHARGES);
            if (_charges >= MAX_CHARGES)
            {
                ApplyBuff(BUFF_ATK, BuffStatType.ATK, 1.0f, true,
                    durationTurns: 1, durationCycles: -1, uniqueGlobal: true);
                _charges = 0;
            }
        }

        public override void ResetForNewStage()
        {
            _charges = 0;
            RemoveBuff(BUFF_ATK);
            // Ne reset PAS BUFF_DEF — permanent
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
