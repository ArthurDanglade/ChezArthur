using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler d'Anubis : gain de puissance sur morts, mode jugement et ultime de dernier survivant ennemi.
    /// </summary>
    public class AnubisHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_ATK = "anubis_atk";
        private const string BUFF_SPD = "anubis_spd";
        private const string BUFF_IMMUNE = "anubis_immune";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _deathCount;
        private bool _judgementActive;
        private bool _alliesDeadImmunityUsed;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "anubis";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnAllyKilled(CharacterBall ally)
        {
            if (!IsReady) return;

            _deathCount++;
            float atkBonus = _deathCount * 0.15f;
            float spdBonus = _deathCount * 0.10f;
            ApplyBuff(BUFF_ATK, BuffStatType.ATK, atkBonus, true, -1, -1, true);
            ApplyBuff(BUFF_SPD, BuffStatType.Speed, spdBonus, true, -1, -1, true);

            if (_deathCount >= 2)
                _judgementActive = true;

            if (_alliesDeadImmunityUsed) return;

            IReadOnlyList<CharacterBall> allies = _turnManager?.GetAllies();
            if (allies == null) return;

            bool anyAlive = false;
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null && !allies[i].IsDead)
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
            {
                _alliesDeadImmunityUsed = true;
                ApplyBuff("anubis_atk_double", BuffStatType.ATK, 1.0f, true, -1, -1, true);
                ApplyBuff(BUFF_IMMUNE, BuffStatType.DamageReduction, 1.0f, true,
                    durationTurns: 1, durationCycles: -1, uniqueGlobal: true);
                _owner.GrantDamageImmunityForOneEnemyTurn();
            }
        }

        public override void OnMateKilled(Enemy mate)
        {
            if (!IsReady) return;

            _deathCount++;
            float atkBonus = _deathCount * 0.15f;
            float spdBonus = _deathCount * 0.10f;
            ApplyBuff(BUFF_ATK, BuffStatType.ATK, atkBonus, true, -1, -1, true);
            ApplyBuff(BUFF_SPD, BuffStatType.Speed, spdBonus, true, -1, -1, true);

            if (_deathCount >= 2)
                _judgementActive = true;
        }

        public override void OnHitAlly(CharacterBall ally)
        {
            if (!IsReady) return;
            if (!_judgementActive) return;

            if (UnityEngine.Random.value < 0.25f && ally != null)
            {
                int bonusDmg = _owner.EffectiveAtk;
                ally.TakeDamage(bonusDmg);
            }
        }

        public override void ResetForNewStage()
        {
            _deathCount = 0;
            _judgementActive = false;
            _alliesDeadImmunityUsed = false;
            RemoveBuff(BUFF_ATK);
            RemoveBuff(BUFF_SPD);
            RemoveBuff(BUFF_IMMUNE);
            RemoveBuff("anubis_atk_double");
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
