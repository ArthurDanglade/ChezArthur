using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Feedback;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Segment de la traînée de feu : brûlure / porteur.
    /// Attribution capturée au StartTrail → callout sur la source passif (Kram), pas sur le porteur.
    /// </summary>
    public class FireTrailSegment : MonoBehaviour
    {
        private const string BurnBuffId = "kram_burn";
        private const string CarrierBuffId = "kram_fire_carrier";
        private const string AtkBuffId = "kram_fire_atk";

        private CharacterBall _source;
        private bool _enhanced;
        private BuffOriginScope.Frame _attribution;

        public void Initialize(CharacterBall source, bool enhanced, BuffOriginScope.Frame attribution)
        {
            _source = source;
            _enhanced = enhanced;
            _attribution = attribution;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || _source == null) return;

            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                BuffReceiver enemyBr = enemy.BuffReceiver;
                if (enemyBr != null && !enemyBr.HasBuff(BurnBuffId))
                {
                    var burn = new BuffData
                    {
                        BuffId = BurnBuffId,
                        Source = _source,
                        StatType = BuffStatType.DamageAmplification,
                        Value = _enhanced ? 0.10f : 0f,
                        IsPercent = true,
                        RemainingTurns = -1,
                        RemainingCycles = -1,
                        UniquePerSource = false,
                        UniqueGlobal = true
                    };
                    BuffOriginScope.ApplyAttribution(burn, _attribution);
                    enemyBr.AddBuff(burn);
                }
                return;
            }

            CharacterBall ally = other.GetComponent<CharacterBall>();
            if (ally == null || ally == _source || ally.IsDead) return;
            if (!ally.IsMoving) return;

            BuffReceiver allyBr = ally.BuffReceiver;
            if (allyBr == null) return;

            if (!allyBr.HasBuff(CarrierBuffId))
            {
                var carrier = new BuffData
                {
                    BuffId = CarrierBuffId,
                    Source = _source,
                    StatType = BuffStatType.ATK,
                    Value = 0f,
                    IsPercent = false,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                };
                BuffOriginScope.ApplyAttribution(carrier, _attribution);
                allyBr.AddBuff(carrier);
            }

            if (_enhanced)
            {
                var atk = new BuffData
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
                };
                BuffOriginScope.ApplyAttribution(atk, _attribution);
                allyBr.AddBuff(atk);
            }
        }
    }
}
