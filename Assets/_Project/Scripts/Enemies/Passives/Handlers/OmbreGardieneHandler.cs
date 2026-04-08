using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de l'Ombre Gardienne : absorbe une partie des dégâts des alliés, puis passe en mode dernier survivant.
    /// </summary>
    public class OmbreGardieneHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_DEF_LAST = "ombre_def_last";
        private const float ABSORB_FRACTION = 0.20f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _isLastAlive;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "ombre_gardienne";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnAllyDamaged(CharacterBall ally, int damage)
        {
            if (!IsReady) return;
            if (_isLastAlive) return;
            if (ally == null || damage <= 0) return;

            int absorbed = Mathf.RoundToInt(damage * ABSORB_FRACTION);
            if (absorbed <= 0) return;

            ally.Heal(absorbed);
            _owner.TakeDamage(absorbed);
        }

        public override void OnMateKilled(Enemy mate)
        {
            if (!IsReady) return;

            Enemy[] all = UnityEngine.Object.FindObjectsOfType<Enemy>();
            int aliveCount = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && !all[i].IsDead)
                    aliveCount++;
            }

            if (aliveCount <= 1 && !_isLastAlive)
            {
                _isLastAlive = true;
                ApplyBuff(BUFF_DEF_LAST, BuffStatType.DEF, 0.60f, true, -1, -1, true);
                ApplyBuff("ombre_atk_malus", BuffStatType.ATK, -0.30f, true, -1, -1, true);
            }
        }

        public override void ResetForNewStage()
        {
            _isLastAlive = false;
            RemoveBuff(BUFF_DEF_LAST);
            RemoveBuff("ombre_atk_malus");
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
