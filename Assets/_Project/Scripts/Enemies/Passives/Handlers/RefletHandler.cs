using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Reflet : cible l'allié avec le plus d'ATK, reflète une partie des dégâts et intercepte des soins.
    /// </summary>
    public class RefletHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private CharacterBall _targetAlly;
        private bool _targetInitialized;
        private readonly List<CharacterBall> _scratch = new List<CharacterBall>(4);

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ ABSTRAITE
        // ═══════════════════════════════════════════

        public override string HandlerId => "reflet";

        // ═══════════════════════════════════════════
        // INITIALIZE
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _targetAlly = null;
            _targetInitialized = false;
            _scratch.Clear();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public override void OnTurnStart()
        {
            if (!IsReady)
                return;

            if (!_targetInitialized)
            {
                SelectTarget();
                _targetInitialized = true;
            }
        }

        public override void OnAllyDamaged(CharacterBall ally, int damage)
        {
            if (!IsReady)
                return;

            if (!ReferenceEquals(ally, _targetAlly))
                return;

            if (_targetAlly == null || _targetAlly.IsDead)
            {
                HandleTargetDead();
                return;
            }

            int reflected = Mathf.RoundToInt(damage * 0.40f);
            if (reflected > 0)
                _targetAlly.TakeDamage(reflected);
        }

        public override void OnAllyHealed(CharacterBall ally, int healAmount)
        {
            if (!IsReady)
                return;

            if (!ReferenceEquals(ally, _targetAlly))
                return;

            if (_targetAlly == null || _targetAlly.IsDead)
            {
                HandleTargetDead();
                return;
            }

            int selfHeal = Mathf.RoundToInt(healAmount * 0.50f);
            if (selfHeal > 0)
                _owner.Heal(selfHeal);
        }

        public override void OnAllyKilled(CharacterBall ally)
        {
            if (!IsReady)
                return;

            if (!ReferenceEquals(ally, _targetAlly))
                return;

            HandleTargetDead();
        }

        public override void ResetForNewStage()
        {
            _targetAlly = null;
            _targetInitialized = false;
            _scratch.Clear();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Sélectionne l'allié avec le plus d'ATK parmi les alliés vivants.
        /// </summary>
        private void SelectTarget()
        {
            FillScratch();

            CharacterBall best = null;
            int bestAtk = int.MinValue;

            for (int i = 0; i < _scratch.Count; i++)
            {
                CharacterBall a = _scratch[i];
                if (a == null || a.IsDead) continue;

                int atk = a.EffectiveAtk;
                if (atk > bestAtk)
                {
                    bestAtk = atk;
                    best = a;
                }
            }

            _targetAlly = best;
        }

        private void HandleTargetDead()
        {
            int heal = Mathf.RoundToInt(_owner.MaxHp * 0.30f);
            _owner.Heal(heal);

            SelectTarget();
        }

        private void FillScratch()
        {
            _scratch.Clear();

            if (_turnManager == null)
                return;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally != null && !ally.IsDead)
                    _scratch.Add(ally);
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

