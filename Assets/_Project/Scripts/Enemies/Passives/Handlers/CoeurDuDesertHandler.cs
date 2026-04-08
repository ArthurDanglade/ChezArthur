using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler du Cœur du Désert : fragments, réduction de dégâts, fenêtre de vulnérabilité, arrêt de régénération sous 25 % HP.
    /// </summary>
    public class CoeurDuDesertHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const int FRAGMENT_COUNT = 4;
        private const int FRAGMENT_HP = 350;
        private const float DAMAGE_REDUCTION = 0.70f;
        private const string BUFF_DR = "coeur_damage_reduction";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private EnemyShieldSystem _shieldSystem;
        private bool _vulnerabilityActive;
        private bool _fragmentsRegenEnabled;
        private bool _initializedLocal;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════

        public override string HandlerId => "coeur_desert";

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);

            _shieldSystem = _owner.GetComponent<EnemyShieldSystem>();
            if (_shieldSystem == null)
                _shieldSystem = _owner.gameObject.AddComponent<EnemyShieldSystem>();

            _shieldSystem.Initialize(_owner, _turnManager);
            _shieldSystem.SetupFragments(FRAGMENT_COUNT, FRAGMENT_HP);

            _fragmentsRegenEnabled = true;
            _vulnerabilityActive = false;
            _initializedLocal = true;

            ApplyBuff(BUFF_DR, BuffStatType.DamageReduction, DAMAGE_REDUCTION, true, -1, -1, true);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public override void OnCycleStart()
        {
            if (!IsReady)
                return;

            if (!_initializedLocal || _shieldSystem == null)
                return;

            if (_vulnerabilityActive)
            {
                // La vulnérabilité dure 1 cycle, puis retour des fragments/réduction si regen encore active.
                _vulnerabilityActive = false;
                if (_fragmentsRegenEnabled)
                {
                    _shieldSystem.SetupFragments(FRAGMENT_COUNT, FRAGMENT_HP);
                    ApplyBuff(BUFF_DR, BuffStatType.DamageReduction, DAMAGE_REDUCTION, true, -1, -1, true);
                }
                return;
            }

            if (_fragmentsRegenEnabled)
            {
                _shieldSystem.RegenFragments(1f);

                // _vulnerabilityActive est potentiellement déjà true
                // si OnTakeDamage l'a déclenché ce cycle.
                // Dans ce cas, le OnCycleStart suivant gèrera la
                // sortie de vulnérabilité normalement.
                // Pas de double déclenchement car OnCycleStart
                // vérifie _vulnerabilityActive en premier.

                if (_shieldSystem.AliveFragmentCount > 0)
                    ApplyBuff(BUFF_DR, BuffStatType.DamageReduction, DAMAGE_REDUCTION, true, -1, -1, true);
                else
                {
                    _vulnerabilityActive = true;
                    RemoveBuff(BUFF_DR);
                }
            }
        }

        public override void OnHpChanged(int currentHp, int maxHp)
        {
            if (!IsReady)
                return;

            float ratio = maxHp > 0 ? (float)currentHp / maxHp : 0f;
            if (ratio <= 0.25f && _fragmentsRegenEnabled)
            {
                _fragmentsRegenEnabled = false;
                Debug.Log("[CoeurDuDesertHandler] Fragments plus régénérés sous 25% HP");
            }
        }

        public override void OnTakeDamage(int damage)
        {
            if (!IsReady)
                return;

            if (_shieldSystem == null)
                return;

            if (_shieldSystem.AllFragmentsDestroyed && !_vulnerabilityActive)
            {
                _vulnerabilityActive = true;
                RemoveBuff(BUFF_DR);
            }
        }

        public override void ResetForNewStage()
        {
            _vulnerabilityActive = false;
            _fragmentsRegenEnabled = true;
            _initializedLocal = false;
            RemoveBuff(BUFF_DR);

            if (_shieldSystem != null)
                _shieldSystem.ResetForNewStage();
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

