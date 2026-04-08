using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler du Prototype : tire un mode aléatoire à chaque tour (ATK / DEF / Chaos).
    /// </summary>
    public class PrototypeHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // TYPES PRIVÉS
        // ═══════════════════════════════════════════

        private enum PrototypeMode
        {
            ATK,
            DEF,
            Chaos
        }

        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const string BUFF_ATK = "prototype_atk";
        private const string BUFF_DEF = "prototype_def";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private PrototypeMode _currentMode;
        private bool _chaosActive;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════

        public override string HandlerId => "prototype";

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// True si le Prototype est en mode Chaos ce tour.
        /// Lu par EnemyPassiveRuntime pour ReflectDamageToAttacker.
        /// </summary>
        public bool ChaosActive => _chaosActive;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public override void OnTurnStart()
        {
            if (!IsReady)
                return;

            BuffReceiver br = _owner.BuffReceiver;
            if (br != null)
            {
                br.RemoveBuffsById(BUFF_ATK);
                br.RemoveBuffsById(BUFF_DEF);
            }

            _chaosActive = false;

            _currentMode = (PrototypeMode)Random.Range(0, 3);

            switch (_currentMode)
            {
                case PrototypeMode.ATK:
                    ApplyBuff(BUFF_ATK, BuffStatType.ATK, 0.5f, true, durationTurns: 1);
                    ApplyBuff(BUFF_DEF, BuffStatType.DEF, -0.3f, true, durationTurns: 1);
                    break;

                case PrototypeMode.DEF:
                    ApplyBuff(BUFF_DEF, BuffStatType.DEF, 0.5f, true, durationTurns: 1);
                    ApplyBuff(BUFF_ATK, BuffStatType.ATK, -0.3f, true, durationTurns: 1);
                    break;

                case PrototypeMode.Chaos:
                    _chaosActive = true;
                    break;
            }
        }

        public override void OnHitByAlly(CharacterBall attacker)
        {
            if (!IsReady)
                return;

            if (!_chaosActive)
                return;

            if (attacker == null)
                return;

            // Approximation acceptable : renvoie des dégâts flats basés sur les HP max.
            float fraction = (_data != null && _data.SpecialValue1 > 0f) ? _data.SpecialValue1 : 0.05f;
            int reflected = Mathf.RoundToInt(_owner.MaxHp * fraction);
            if (reflected > 0)
                attacker.TakeDamage(reflected);
        }

        public override void ResetForNewStage()
        {
            _chaosActive = false;

            if (_owner?.BuffReceiver != null)
            {
                _owner.BuffReceiver.RemoveBuffsById(BUFF_ATK);
                _owner.BuffReceiver.RemoveBuffsById(BUFF_DEF);
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

        private void RemoveBuff(string buffId)
        {
            _owner?.BuffReceiver?.RemoveBuffsById(buffId);
        }
    }
}

