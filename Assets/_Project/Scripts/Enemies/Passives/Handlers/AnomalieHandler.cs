using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler d'Anomalie : mode A (copie de stats) ou mode B (inversion ATK/DEF des alliés).
    /// </summary>
    public class AnomalieHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_ATK_COPY = "anomalie_atk_copy";
        private const string BUFF_DEF_COPY = "anomalie_def_copy";
        private const string BUFF_INVERT_ATK = "anomalie_inv_atk";
        private const string BUFF_INVERT_DEF = "anomalie_inv_def";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _modeA;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "anomalie";

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════
        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _modeA = (_data != null && _data.SpecialValue1 < 0.5f);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnTurnStart()
        {
            if (!IsReady) return;

            if (_modeA)
                ApplyModeA();
            else
                ApplyModeB();
        }

        public override void ResetForNewStage()
        {
            RemoveBuff(BUFF_ATK_COPY);
            RemoveBuff(BUFF_DEF_COPY);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void ApplyModeA()
        {
            IReadOnlyList<CharacterBall> allies = _turnManager?.GetAllies();
            Enemy[] enemies = UnityEngine.Object.FindObjectsOfType<Enemy>();

            var candidates = new List<(int atk, int def)>();

            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                {
                    CharacterBall ally = allies[i];
                    if (ally != null && !ally.IsDead)
                        candidates.Add((ally.EffectiveAtk, ally.EffectiveDef));
                }
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                Enemy e = enemies[i];
                if (e != null && !e.IsDead && !ReferenceEquals(e, _owner))
                    candidates.Add((e.EffectiveAtk, e.EffectiveDef));
            }

            if (candidates.Count == 0) return;

            int idx = UnityEngine.Random.Range(0, candidates.Count);
            (int atk, int def) chosen = candidates[idx];

            float atkRatio = _owner.Atk > 0 ? (float)chosen.atk / _owner.Atk - 1f : 0f;
            float defRatio = _owner.Def > 0 ? (float)chosen.def / _owner.Def - 1f : 0f;

            RemoveBuff(BUFF_ATK_COPY);
            RemoveBuff(BUFF_DEF_COPY);

            if (Mathf.Abs(atkRatio) > 0.01f)
            {
                ApplyBuff(BUFF_ATK_COPY, BuffStatType.ATK, atkRatio, true,
                    durationTurns: 1, durationCycles: -1, uniqueGlobal: true);
            }

            if (Mathf.Abs(defRatio) > 0.01f)
            {
                ApplyBuff(BUFF_DEF_COPY, BuffStatType.DEF, defRatio, true,
                    durationTurns: 1, durationCycles: -1, uniqueGlobal: true);
            }
        }

        private void ApplyModeB()
        {
            IReadOnlyList<CharacterBall> allies = _turnManager?.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                if (ally.BuffReceiver == null) continue;

                float atkPenalty = ally.EffectiveDef > 0
                    ? -(float)ally.EffectiveDef / Mathf.Max(1, ally.EffectiveAtk)
                    : 0f;
                float defPenalty = ally.EffectiveAtk > 0
                    ? -(float)ally.EffectiveAtk / Mathf.Max(1, ally.EffectiveDef)
                    : 0f;

                string atkId = BUFF_INVERT_ATK + "_" + ally.GetInstanceID();
                string defId = BUFF_INVERT_DEF + "_" + ally.GetInstanceID();

                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = atkId,
                    Source = null,
                    StatType = BuffStatType.ATK,
                    Value = atkPenalty,
                    IsPercent = true,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniqueGlobal = false,
                    UniquePerSource = false
                });

                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = defId,
                    Source = null,
                    StatType = BuffStatType.DEF,
                    Value = defPenalty,
                    IsPercent = true,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniqueGlobal = false,
                    UniquePerSource = false
                });
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
