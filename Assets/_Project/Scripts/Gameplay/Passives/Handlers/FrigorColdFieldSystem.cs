using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime Frigor passif 2 : aura SPD −15 % sur tous les ennemis tant que Frigor est vivant,
    /// et éclats de glace quand un ennemi gelé meurt.
    /// </summary>
    public class FrigorColdFieldSystem : MonoBehaviour
    {
        private const string ColdFieldBuffId = "frigor_cold_field";
        private const string FreezeBuffId = "frigor_freeze";
        private const float ColdFieldSpdDebuff = -0.15f;
        private const float IceShardAtkRatio = 0.30f;

        private static FrigorColdFieldSystem _instance;
        public static FrigorColdFieldSystem Instance => _instance;

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private bool _subscribedOwnerDeath;

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (owner == null) return;

            if (_subscribedOwnerDeath && _owner != null && !ReferenceEquals(_owner, owner))
            {
                _owner.OnDeath -= OnOwnerDeath;
                _subscribedOwnerDeath = false;
            }

            _owner = owner;
            _turnManager = turnManager;
            _instance = this;

            if (!_subscribedOwnerDeath)
            {
                _owner.OnDeath += OnOwnerDeath;
                _subscribedOwnerDeath = true;
            }
        }

        public void ApplyColdFieldToAllEnemies()
        {
            if (!IsAuraActive() || _turnManager == null) return;

            var participants = _turnManager.Participants;
            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] is Enemy enemy)
                    ApplyColdFieldToEnemy(enemy);
            }
        }

        /// <summary>
        /// Appelé à chaque spawn ennemi (début d'étage ou ajout en cours de combat).
        /// </summary>
        public void TryApplyColdFieldToEnemy(Enemy enemy)
        {
            if (!IsAuraActive()) return;
            ApplyColdFieldToEnemy(enemy);
        }

        /// <summary>
        /// Hook mort ennemi : explosion si l'ennemi portait encore frigor_freeze (avant Die).
        /// </summary>
        public static void TryIceShardsOnFrozenDeath(Enemy dyingEnemy)
        {
            if (dyingEnemy == null || _instance == null || !_instance.IsAuraActive()) return;
            if (!_instance.HasFreezeBuff(dyingEnemy)) return;

            _instance.TriggerIceShards(dyingEnemy);
            FreezeSystem.Instance?.OnFrozenEnemyDied(dyingEnemy);
        }

        private bool IsAuraActive()
        {
            return _owner != null && !_owner.IsDead;
        }

        private bool HasFreezeBuff(Enemy enemy)
        {
            if (enemy == null) return false;
            BuffReceiver br = enemy.BuffReceiver;
            return br != null && br.HasBuff(FreezeBuffId);
        }

        private void TriggerIceShards(Enemy dyingEnemy)
        {
            if (_turnManager == null || _owner == null) return;

            int damage = Mathf.Max(1, Mathf.RoundToInt(_owner.EffectiveAtk * IceShardAtkRatio));
            int hitCount = 0;

            var participants = _turnManager.Participants;
            for (int i = 0; i < participants.Count; i++)
            {
                Enemy enemy = participants[i] as Enemy;
                if (enemy == null || enemy.IsDead || enemy == dyingEnemy) continue;

                enemy.TakePureDamage(damage);
                hitCount++;
            }

            Debug.Log($"[Passif] Frigor : éclats de glace ({hitCount} ennemis touchés)");
        }

        private void ApplyColdFieldToEnemy(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead || _owner == null) return;

            BuffReceiver br = enemy.BuffReceiver;
            if (br == null) return;

            br.AddBuff(new BuffData
            {
                BuffId = ColdFieldBuffId,
                Source = _owner,
                StatType = BuffStatType.Speed,
                Value = ColdFieldSpdDebuff,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private void RemoveColdFieldFromAllEnemies()
        {
            if (_turnManager == null) return;

            var participants = _turnManager.Participants;
            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] is Enemy enemy && enemy.BuffReceiver != null)
                    enemy.BuffReceiver.RemoveBuffsById(ColdFieldBuffId);
            }
        }

        private void OnOwnerDeath()
        {
            RemoveColdFieldFromAllEnemies();
        }

        private void OnDestroy()
        {
            if (_owner != null && _subscribedOwnerDeath)
                _owner.OnDeath -= OnOwnerDeath;

            if (_instance == this)
                _instance = null;
        }
    }
}
