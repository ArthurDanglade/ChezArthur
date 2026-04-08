using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler du Directeur : bouclier cyclique, DEF croissante et ATK croissante quand touché.
    /// </summary>
    public class DirecteurHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_ATK = "directeur_atk";
        private const string BUFF_DEF = "directeur_def";
        private const string SHIELD_BUFF = "directeur_shield";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _shieldBrokenCount;
        private int _shieldRegenCount;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "directeur";

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════
        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _shieldBrokenCount = 0;
            _shieldRegenCount = 0;
            ApplyShield();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnCycleStart()
        {
            if (!IsReady) return;

            ApplyShield();
            _shieldRegenCount++;
            float defBonus = _shieldRegenCount * 0.05f;
            ApplyBuff(BUFF_DEF, BuffStatType.DEF, Mathf.Min(defBonus, 0.50f), true, -1, -1, true);
        }

        public override void OnTakeDamage(int damage)
        {
            if (!IsReady) return;
            if (damage > 0)
            {
                _shieldBrokenCount++;
                float atkBonus = _shieldBrokenCount * 0.05f;
                ApplyBuff(BUFF_ATK, BuffStatType.ATK, Mathf.Min(atkBonus, 0.50f), true, -1, -1, true);
            }
        }

        public override void ResetForNewStage()
        {
            _shieldBrokenCount = 0;
            _shieldRegenCount = 0;
            RemoveBuff(BUFF_ATK);
            RemoveBuff(BUFF_DEF);
            RemoveBuff(SHIELD_BUFF);
            ApplyShield();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void ApplyShield()
        {
            ApplyBuff(SHIELD_BUFF, BuffStatType.DamageReduction,
                0.15f, true, durationTurns: -1, durationCycles: 1, uniqueGlobal: true);
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
