using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler du Grand Prêtre : buffs dynamiques, maintien d'un Skarabé et soin cyclique.
    /// </summary>
    public class GrandPretreHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_ATK = "gp_atk_per_skarabe";
        private const string BUFF_DEF = "gp_def_per_mate";
        private const string BUFF_HEAL_STACK = "gp_heal_stack";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Enemy _currentSkarabe;
        private int _skarabeCount;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "grand_pretre";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnCycleStart()
        {
            if (!IsReady) return;

            UpdateDefBuff();
            UpdateAtkBuff();

            bool skarabeAlive = _currentSkarabe != null && !_currentSkarabe.IsDead;
            if (!skarabeAlive)
                SpawnSkarabe();

            if (skarabeAlive)
            {
                int heal = Mathf.RoundToInt(_owner.MaxHp * 0.05f);
                _owner.Heal(heal);
            }
        }

        public override void ResetForNewStage()
        {
            _currentSkarabe = null;
            _skarabeCount = 0;
            RemoveBuff(BUFF_ATK);
            RemoveBuff(BUFF_DEF);
            RemoveBuff(BUFF_HEAL_STACK);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void SpawnSkarabe()
        {
            if (MidCombatSpawner.Instance == null) return;

            EnemyData skarabeData = MidCombatSpawner.Instance.GetEnemyData("Skarabé");
            if (skarabeData == null)
            {
                Debug.LogWarning("[GrandPretreHandler] EnemyData Skarabé introuvable dans MidCombatSpawner.");
                return;
            }

            float hpMult = _owner.MaxHp > 0 ? (float)_owner.MaxHp / skarabeData.BaseHp : 1f;
            hpMult = Mathf.Clamp(hpMult, 0.5f, 5f);

            Vector3 spawnPos = _owner.transform.position
                + new Vector3(UnityEngine.Random.Range(-2f, 2f), 1f, 0f);

            _currentSkarabe = MidCombatSpawner.Instance.SpawnEnemy(skarabeData, spawnPos, hpMult, 1f);
            if (_currentSkarabe != null)
                _skarabeCount++;
        }

        private void UpdateDefBuff()
        {
            Enemy[] all = UnityEngine.Object.FindObjectsOfType<Enemy>();
            int aliveCount = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && !all[i].IsDead && !ReferenceEquals(all[i], _owner))
                    aliveCount++;
            }

            float defBonus = aliveCount * 0.05f;
            ApplyBuff(BUFF_DEF, BuffStatType.DEF, defBonus, true, -1, -1, true);
        }

        private void UpdateAtkBuff()
        {
            Enemy[] all = UnityEngine.Object.FindObjectsOfType<Enemy>();
            int skarabeCount = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].IsDead || all[i].Data == null) continue;
                if (all[i].Data.EnemyName == "Skarabé")
                    skarabeCount++;
            }

            float atkBonus = skarabeCount * 0.05f;
            ApplyBuff(BUFF_ATK, BuffStatType.ATK, atkBonus, true, -1, -1, true);
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
