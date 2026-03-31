using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime Shado :
    /// - active l'invisibilité si 2+ ennemis touchés pendant un lancer,
    /// - applique le buff ATK à la sortie d'invisibilité,
    /// - gère le vol d'ATK cumulé (passif niv 10).
    /// </summary>
    public class ShadoStealthSystem : MonoBehaviour
    {
        private const string StealthAtkBuffId = "shado_stealth_atk";
        private const string StolenAtkBuffId = "shado_stolen_atk";
        private const string EnemyAtkDebuffId = "shado_enemy_atk_debuff";

        private CharacterBall _owner;
        private TurnManager _turnManager;

        private bool _isInvisible;
        private int _enemiesHitThisLaunch;
        private int _enemiesHitAtActivation;
        private bool _enhanced;

        private bool _subscribedToTurnChanged;
        private bool _subscribedToStopped;

        private int _totalStolenAtk;
        private readonly List<StolenEntry> _stolenFromEnemies = new List<StolenEntry>(8);

        public bool IsInvisible => _isInvisible;

        private class StolenEntry
        {
            public Enemy Enemy;
            public int StolenAmount;
            public Action DeathHandler;
        }

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (owner == null) return;

            if (_subscribedToTurnChanged && _turnManager != null && _turnManager != turnManager)
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
                _subscribedToTurnChanged = false;
            }
            if (_subscribedToStopped && _owner != null && _owner != owner)
            {
                _owner.OnStopped -= OnOwnerStopped;
                _subscribedToStopped = false;
            }

            _owner = owner;
            _turnManager = turnManager;
            SubscribeEvents();
        }

        public void SetEnhanced(bool value)
        {
            _enhanced = value;
        }

        public void IncrementEnemyHitCount()
        {
            _enemiesHitThisLaunch++;
        }

        /// <summary>
        /// Tentative de vol d'ATK à la sortie d'invisibilité (pendant la fenêtre du buff ATK de sortie).
        /// </summary>
        public void TryStealAtk(Enemy enemy)
        {
            if (!_enhanced || enemy == null || enemy.IsDead) return;
            if (_owner == null || _owner.BuffReceiver == null) return;
            if (!_owner.BuffReceiver.HasBuff(StealthAtkBuffId)) return;

            // Évite de voler plusieurs fois le même ennemi avant sa mort.
            for (int i = 0; i < _stolenFromEnemies.Count; i++)
            {
                if (ReferenceEquals(_stolenFromEnemies[i].Enemy, enemy))
                    return;
            }

            int stolenFlat = Mathf.Max(1, Mathf.RoundToInt(enemy.EffectiveAtk * 0.05f));

            BuffReceiver enemyBr = enemy.BuffReceiver;
            if (enemyBr != null)
            {
                enemyBr.AddBuff(new BuffData
                {
                    BuffId = EnemyAtkDebuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = -stolenFlat,
                    IsPercent = false,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                });
            }

            _totalStolenAtk += stolenFlat;
            SyncStolenBuff();

            var entry = new StolenEntry
            {
                Enemy = enemy,
                StolenAmount = stolenFlat
            };
            entry.DeathHandler = () => HandleStolenEnemyDeath(entry);
            enemy.OnDeath += entry.DeathHandler;
            _stolenFromEnemies.Add(entry);
        }

        public void ResetForStage()
        {
            _isInvisible = false;
            _enemiesHitThisLaunch = 0;
            _enemiesHitAtActivation = 0;

            if (_owner != null && _owner.BuffReceiver != null)
            {
                _owner.BuffReceiver.RemoveBuffsById(StealthAtkBuffId);
                _owner.BuffReceiver.RemoveBuffsById(StolenAtkBuffId);
            }

            for (int i = 0; i < _stolenFromEnemies.Count; i++)
            {
                StolenEntry e = _stolenFromEnemies[i];
                if (e?.Enemy != null && e.DeathHandler != null)
                    e.Enemy.OnDeath -= e.DeathHandler;
            }
            _stolenFromEnemies.Clear();
            _totalStolenAtk = 0;
        }

        private void SubscribeEvents()
        {
            if (_owner != null && !_subscribedToStopped)
            {
                _owner.OnStopped += OnOwnerStopped;
                _subscribedToStopped = true;
            }

            if (_turnManager != null && !_subscribedToTurnChanged)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
                _subscribedToTurnChanged = true;
            }
        }

        private void OnOwnerStopped()
        {
            if (_enemiesHitThisLaunch >= 2)
            {
                _isInvisible = true;
                _enemiesHitAtActivation = _enemiesHitThisLaunch;
            }

            _enemiesHitThisLaunch = 0;
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null) return;
            if (!ReferenceEquals(participant, _owner)) return;

            if (_isInvisible)
            {
                _isInvisible = false;

                float atkBonus = (_enhanced && _enemiesHitAtActivation >= 3) ? 0.50f : 0.30f;
                BuffReceiver br = _owner.BuffReceiver;
                if (br != null)
                {
                    br.AddBuff(new BuffData
                    {
                        BuffId = StealthAtkBuffId,
                        Source = _owner,
                        StatType = BuffStatType.ATK,
                        Value = atkBonus,
                        IsPercent = true,
                        RemainingTurns = 1,
                        RemainingCycles = -1,
                        UniquePerSource = false,
                        UniqueGlobal = true
                    });
                }
            }

            _enemiesHitThisLaunch = 0;
            _enemiesHitAtActivation = 0;
        }

        private void HandleStolenEnemyDeath(StolenEntry entry)
        {
            if (entry == null) return;

            if (entry.Enemy != null && entry.DeathHandler != null)
                entry.Enemy.OnDeath -= entry.DeathHandler;

            _totalStolenAtk = Mathf.Max(0, _totalStolenAtk - Mathf.Max(0, entry.StolenAmount));
            _stolenFromEnemies.Remove(entry);
            SyncStolenBuff();
        }

        private void SyncStolenBuff()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            BuffReceiver br = _owner.BuffReceiver;
            br.RemoveBuffsById(StolenAtkBuffId);

            if (_totalStolenAtk <= 0) return;

            br.AddBuff(new BuffData
            {
                BuffId = StolenAtkBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = _totalStolenAtk,
                IsPercent = false,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private void OnDestroy()
        {
            if (_owner != null && _subscribedToStopped)
                _owner.OnStopped -= OnOwnerStopped;
            if (_turnManager != null && _subscribedToTurnChanged)
                _turnManager.OnTurnChanged -= OnTurnChanged;

            for (int i = 0; i < _stolenFromEnemies.Count; i++)
            {
                StolenEntry e = _stolenFromEnemies[i];
                if (e?.Enemy != null && e.DeathHandler != null)
                    e.Enemy.OnDeath -= e.DeathHandler;
            }

            _stolenFromEnemies.Clear();
        }
    }
}

