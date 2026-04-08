using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Handler de Grilhor : choque un allié aléatoire et renvoie des dégâts si cet allié l'attaque.
    /// </summary>
    public class GrilhorHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string BUFF_REFLECT = "grilhor_reflect";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private CharacterBall _shockedAlly;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════
        public override string HandlerId => "grilhor";

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public override void OnTurnStart()
        {
            if (!IsReady) return;

            IReadOnlyList<CharacterBall> allies = _turnManager?.GetAllies();
            if (allies == null || allies.Count == 0) return;

            List<CharacterBall> living = new List<CharacterBall>();
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] != null && !allies[i].IsDead)
                    living.Add(allies[i]);
            }

            if (living.Count == 0) return;

            CharacterBall target = living[Random.Range(0, living.Count)];
            _shockedAlly = target;

            if (target?.BuffReceiver != null)
            {
                target.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = "grilhor_shock_" + target.GetInstanceID(),
                    Source = null,
                    StatType = BuffStatType.Speed,
                    Value = -0.10f,
                    IsPercent = true,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniqueGlobal = false,
                    UniquePerSource = false
                });
            }
        }

        public override void OnHitByAlly(CharacterBall attacker)
        {
            if (!IsReady) return;
            if (attacker == null) return;
            if (attacker != _shockedAlly) return;

            int reflected = Mathf.RoundToInt(attacker.EffectiveAtk * 0.20f);
            if (reflected > 0)
                attacker.TakeDamage(reflected);
        }

        public override void ResetForNewStage()
        {
            _shockedAlly = null;
        }
    }
}
