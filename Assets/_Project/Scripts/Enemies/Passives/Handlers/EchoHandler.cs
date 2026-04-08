using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler d'Écho : mode A (écho de dégâts) ou mode B (écho de soins).
    /// </summary>
    public class EchoHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _modeA;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "echo";

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
        public override void OnAllyDamaged(CharacterBall ally, int damage)
        {
            if (!IsReady) return;
            if (!_modeA) return;
            if (ally == null || damage <= 0) return;

            int echoDmg = Mathf.RoundToInt(damage * 0.15f);
            if (echoDmg > 0)
                ally.TakeDamageUnreducible(echoDmg);
        }

        public override void OnAllyHealed(CharacterBall ally, int healAmount)
        {
            if (!IsReady) return;
            if (_modeA) return;
            if (healAmount <= 0) return;

            _owner.Heal(healAmount);
        }

        public override void ResetForNewStage()
        {
            // Le pool A/B est re-résolu par EnemyPassiveRuntime
            // _modeA sera réinitialisé au prochain Initialize()
        }
    }
}
