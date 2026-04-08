using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler du Parieur Endetté : mise croissante et buffs par paliers.
    /// </summary>
    public class ParieurEndetteHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_ATK_ALLIES = "parieur_atk_allies";
        private const string BUFF_ATK_MISE1 = "parieur_atk_mise1";
        private const string BUFF_ATK_MISE2 = "parieur_atk_mise2";
        private const string BUFF_DEF_MISE2 = "parieur_def_mise2";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _mise;
        private int _aliveEnemiesAtStart;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "parieur_endette";

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════
        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _mise = 0;
            _aliveEnemiesAtStart = UnityEngine.Object.FindObjectsOfType<Enemy>().Length;
            _ = _aliveEnemiesAtStart;
            UpdateAllyCountBuff();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnCycleStart()
        {
            if (!IsReady) return;
            _mise += 100;
            CheckMilestones();
        }

        public override void OnMateKilled(Enemy mate)
        {
            if (!IsReady) return;
            UpdateAllyCountBuff();
        }

        public override void ResetForNewStage()
        {
            _mise = 0;
            RemoveBuff(BUFF_ATK_ALLIES);
            RemoveBuff(BUFF_ATK_MISE1);
            RemoveBuff(BUFF_ATK_MISE2);
            RemoveBuff(BUFF_DEF_MISE2);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void UpdateAllyCountBuff()
        {
            Enemy[] allEnemies = UnityEngine.Object.FindObjectsOfType<Enemy>();
            int aliveCount = 0;
            for (int i = 0; i < allEnemies.Length; i++)
            {
                if (allEnemies[i] != null && !allEnemies[i].IsDead)
                    aliveCount++;
            }

            float bonus = aliveCount * 0.05f;
            ApplyBuff(BUFF_ATK_ALLIES, BuffStatType.ATK, bonus, true, -1, -1, true);
        }

        private void CheckMilestones()
        {
            if (_mise >= 2000)
            {
                ApplyBuff(BUFF_ATK_MISE1, BuffStatType.ATK, 0.40f, true, -1, -1, true);
                ApplyBuff(BUFF_ATK_MISE2, BuffStatType.ATK, 0.40f, true, -1, -1, true);
                ApplyBuff(BUFF_DEF_MISE2, BuffStatType.DEF, 0.40f, true, -1, -1, true);
            }
            else if (_mise >= 1000)
            {
                ApplyBuff(BUFF_ATK_MISE1, BuffStatType.ATK, 0.40f, true, -1, -1, true);
            }
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
