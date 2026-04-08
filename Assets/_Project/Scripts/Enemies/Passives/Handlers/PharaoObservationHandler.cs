using System;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Pharaon : tours d'observation immunisés, puis buffs ; bonus ATK par allié tué ; résurrection à bas PV.
    /// </summary>
    public class PharaoObservationHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const int IMMUNE_TURNS = 2;
        private const string BUFF_ATK = "pharao_atk_observation";
        private const string BUFF_DEF = "pharao_def_observation";
        private const string BUFF_ATK_DEATH = "pharao_atk_death";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private int _observationTurns;
        private bool _observationEnded;
        private bool _resurrectionUsed;
        private int _allyDeathBuffCounter;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════

        public override string HandlerId => "pharao_observation";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public override void OnTurnStart()
        {
            if (!IsReady)
                return;

            if (!_observationEnded)
            {
                if (_observationTurns < IMMUNE_TURNS)
                {
                    _owner.GrantDamageImmunityForOneEnemyTurn();
                    _observationTurns++;
                }
                else
                {
                    _observationEnded = true;
                    float bonus = _observationTurns * 0.15f;
                    if (bonus < 0.30f)
                        bonus = 0.30f;

                    ApplyBuff(BUFF_ATK, BuffStatType.ATK, bonus, true, -1, -1, true);
                    ApplyBuff(BUFF_DEF, BuffStatType.DEF, bonus, true, -1, -1, true);
                }
            }
        }

        public override void OnAllyKilled(CharacterBall ally)
        {
            if (!IsReady)
                return;

            _allyDeathBuffCounter++;
            string id = BUFF_ATK_DEATH + "_" + _allyDeathBuffCounter;
            ApplyBuff(id, BuffStatType.ATK, 0.2f, true, -1, -1, false);
        }

        public override void OnHpChanged(int currentHp, int maxHp)
        {
            if (!IsReady)
                return;

            if (_resurrectionUsed)
                return;

            float ratio = maxHp > 0 ? (float)currentHp / maxHp : 0f;
            if (ratio <= 0.20f && !_resurrectionUsed)
            {
                _resurrectionUsed = true;
                int targetHp = Mathf.Max(1, Mathf.RoundToInt(maxHp * 0.40f));
                int delta = targetHp - currentHp;
                if (delta > 0)
                    _owner.Heal(delta);
            }
        }

        public override void ResetForNewStage()
        {
            _observationTurns = 0;
            _observationEnded = false;
            _resurrectionUsed = false;
            _allyDeathBuffCounter = 0;

            if (_owner?.BuffReceiver == null)
                return;

            BuffReceiver br = _owner.BuffReceiver;
            br.RemoveBuffsById(BUFF_ATK);
            br.RemoveBuffsById(BUFF_DEF);

            if (br.ActiveBuffs != null)
            {
                string prefix = BUFF_ATK_DEATH + "_";
                for (int safety = 0; safety < 64; safety++)
                {
                    bool removed = false;
                    for (int i = 0; i < br.ActiveBuffs.Count; i++)
                    {
                        BuffData b = br.ActiveBuffs[i];
                        if (b?.BuffId != null && b.BuffId.StartsWith(prefix, StringComparison.Ordinal))
                        {
                            br.RemoveBuffsById(b.BuffId);
                            removed = true;
                            break;
                        }
                    }

                    if (!removed)
                        break;
                }
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
    }
}
