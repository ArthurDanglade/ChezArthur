using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Zone électrifiée posée sur le dernier mur touché par Voltrain.
    /// Les ennemis qui la traversent reçoivent la paralysie.
    /// </summary>
    public class ElectricZoneSegment : MonoBehaviour
    {
        private const string ParalysisBuffSpdId = "voltrain_paralysis_spd";
        private const string ParalysisBuffLfId = "voltrain_paralysis_lf";

        private CharacterBall _source;

        public void Initialize(CharacterBall source)
        {
            _source = source;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Enemy enemy = other != null ? other.GetComponent<Enemy>() : null;
            if (enemy == null || enemy.IsDead) return;

            BuffReceiver br = enemy.BuffReceiver;
            if (br == null) return;

            if (br.HasBuff(ParalysisBuffSpdId)) return;

            br.AddBuff(new BuffData
            {
                BuffId = ParalysisBuffSpdId,
                Source = _source,
                StatType = BuffStatType.Speed,
                Value = -0.20f,
                IsPercent = true,
                RemainingTurns = 2,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            br.AddBuff(new BuffData
            {
                BuffId = ParalysisBuffLfId,
                Source = _source,
                StatType = BuffStatType.LaunchForce,
                Value = -0.15f,
                IsPercent = true,
                RemainingTurns = 2,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }
    }
}

