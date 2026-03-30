using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Segment de la traînée de feu de Kram : déclenche brûlure / porteur via trigger.
    /// </summary>
    public class FireTrailSegment : MonoBehaviour
    {
        private const string BurnBuffId = "kram_burn";
        private const string CarrierBuffId = "kram_fire_carrier";
        private const string AtkBuffId = "kram_fire_atk";

        private CharacterBall _source;
        private bool _enhanced;

        /// <summary>
        /// Initialise le segment.
        /// </summary>
        public void Initialize(CharacterBall source, bool enhanced)
        {
            _source = source;
            _enhanced = enhanced;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || _source == null) return;

            // Ennemi : application de Brûlure.
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                BuffReceiver enemyBr = enemy.BuffReceiver;
                if (enemyBr != null && !enemyBr.HasBuff(BurnBuffId))
                {
                    enemyBr.AddBuff(new BuffData
                    {
                        BuffId = BurnBuffId,
                        Source = _source,
                        StatType = BuffStatType.DamageAmplification,
                        Value = _enhanced ? 0.10f : 0f,
                        IsPercent = true,
                        RemainingTurns = -1, // persiste jusqu'au nettoyage global (stage / ennemi)
                        RemainingCycles = -1,
                        UniquePerSource = false,
                        UniqueGlobal = true
                    });
                }
                return;
            }

            // Alliés : application de l'état "Enflammé" (porteur).
            CharacterBall ally = other.GetComponent<CharacterBall>();
            if (ally == null || ally == _source || ally.IsDead) return;

            // On ne buff que si l'allié traverse (en mouvement).
            if (!ally.IsMoving) return;

            BuffReceiver allyBr = ally.BuffReceiver;
            if (allyBr == null) return;

            if (!allyBr.HasBuff(CarrierBuffId))
            {
                allyBr.AddBuff(new BuffData
                {
                    BuffId = CarrierBuffId,
                    Source = _source,
                    StatType = BuffStatType.ATK, // marqueur (Value = 0 => pas d'impact)
                    Value = 0f,
                    IsPercent = false,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }

            // Niveau 10 : l'allié traversant gagne aussi ATK +15% pendant 1 tour.
            if (_enhanced)
            {
                allyBr.AddBuff(new BuffData
                {
                    BuffId = AtkBuffId,
                    Source = _source,
                    StatType = BuffStatType.ATK,
                    Value = 0.15f,
                    IsPercent = true,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }
        }
    }
}

