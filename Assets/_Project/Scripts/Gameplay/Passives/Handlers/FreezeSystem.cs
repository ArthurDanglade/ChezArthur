using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Congélation Frigor : un seul ennemi gelé (DEF -20 %), tours intermédiaires sautés, dégel au tour de Frigor ou au swap.
    /// </summary>
    public class FreezeSystem : MonoBehaviour
    {
        private const string FreezeBuffId = "frigor_freeze";
        private const string ShatterDebuffId = "frigor_shatter";

        private static FreezeSystem _instance;
        public static FreezeSystem Instance => _instance;

        private TurnManager _turnManager;
        private Enemy _frozenEnemy;
        private CharacterBall _freezeSource;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            UnsubscribeFromTurnManager();
        }

        /// <summary> Branche le système sur le TurnManager (ex. RunManager.StartRun). </summary>
        public void Initialize(TurnManager turnManager)
        {
            UnsubscribeFromTurnManager();
            _turnManager = turnManager;
            if (_turnManager != null)
                _turnManager.OnTurnChanged += OnTurnChanged;
        }

        private void UnsubscribeFromTurnManager()
        {
            if (_turnManager != null)
                _turnManager.OnTurnChanged -= OnTurnChanged;
            _turnManager = null;
        }

        /// <summary> Utilisé par TurnManager pour bloquer mouvement / IA tant que l'ennemi reste gelé. </summary>
        public bool IsFrozenEnemy(Enemy enemy)
        {
            return enemy != null && enemy == _frozenEnemy;
        }

        /// <summary> Indique si l'ennemi porte encore le buff de gel (ex. éclats de glace à la mort). </summary>
        public bool HasFreezeBuff(Enemy enemy)
        {
            if (enemy == null) return false;
            BuffReceiver br = enemy.BuffReceiver;
            return br != null && br.HasBuff(FreezeBuffId);
        }

        /// <summary> Nettoie la référence gelée quand l'ennemi meurt (buff retiré par la mort). </summary>
        public void OnFrozenEnemyDied(Enemy enemy)
        {
            if (_frozenEnemy == null || enemy != _frozenEnemy) return;
            ThawAndClearFrozenEnemy();
        }

        /// <summary> Gèle un ennemi ; dégèle l'ancien s'il y en a un autre (swap). </summary>
        public void FreezeEnemy(Enemy enemy, CharacterBall source)
        {
            if (enemy == null || source == null || enemy.IsDead) return;

            // Swap : dégel complet de l'ancien (buff retiré + movable restauré via RefreshMovableStates).
            if (_frozenEnemy != null && _frozenEnemy != enemy)
                ThawAndClearFrozenEnemy();

            _frozenEnemy = enemy;
            _freezeSource = source;

            BuffReceiver br = enemy.BuffReceiver;
            if (br != null)
            {
                br.RemoveBuffsById(FreezeBuffId);
                br.AddBuff(new BuffData
                {
                    BuffId = FreezeBuffId,
                    Source = source,
                    StatType = BuffStatType.DEF,
                    Value = -0.20f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }

            enemy.SetMovable(false);
            _turnManager?.RefreshMovableStates();
        }

        /// <summary> Allié (hors Frigor source du gel) touche l'ennemi gelé → bris. </summary>
        public void TryShatter(CharacterBall attacker, Enemy enemy)
        {
            if (attacker == null || enemy == null) return;
            if (attacker == _freezeSource) return;
            if (enemy != _frozenEnemy) return;

            ShatterEnemy(enemy, attacker);
        }

        /// <summary> Dégâts bonus, debuff lancer sur le frappeur, dégel. </summary>
        public void ShatterEnemy(Enemy enemy, CharacterBall attacker)
        {
            if (enemy != _frozenEnemy || attacker == null) return;

            int shatterDamage = Mathf.Max(1, Mathf.RoundToInt(attacker.EffectiveAtk * 0.30f));
            enemy.TakeDamage(shatterDamage);

            BuffReceiver attackerBr = attacker.BuffReceiver;
            if (attackerBr != null)
            {
                attackerBr.AddBuff(new BuffData
                {
                    BuffId = ShatterDebuffId,
                    Source = _freezeSource,
                    StatType = BuffStatType.LaunchForce,
                    Value = -0.30f,
                    IsPercent = true,
                    RemainingTurns = 2,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }

            ThawAndClearFrozenEnemy();
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_turnManager == null) return;

            if (_frozenEnemy != null && _frozenEnemy.IsDead)
            {
                ThawAndClearFrozenEnemy();
                return;
            }

            // Tour de l'ennemi gelé : skip sans dégel (reste gelé jusqu'au tour de Frigor).
            if (participant != null && _frozenEnemy != null && ReferenceEquals(participant, _frozenEnemy))
            {
                _turnManager.SkipCurrentTurn();
                return;
            }

            // Tour de Frigor : seul point de dégel naturel (buff retiré + movable restauré).
            if (_freezeSource != null && participant != null && ReferenceEquals(participant, _freezeSource) && _frozenEnemy != null)
                ThawAndClearFrozenEnemy();
        }

        /// <summary>
        /// Retire le gel, efface la référence et restaure le movable de l'ex-ennemi via TurnManager.
        /// </summary>
        private void ThawAndClearFrozenEnemy()
        {
            if (_frozenEnemy == null) return;

            RemoveFreezeBuffFrom(_frozenEnemy);
            _frozenEnemy = null;
            _turnManager?.RefreshMovableStates();
        }

        private static void RemoveFreezeBuffFrom(Enemy enemy)
        {
            if (enemy == null) return;
            BuffReceiver br = enemy.BuffReceiver;
            if (br != null)
                br.RemoveBuffsById(FreezeBuffId);
        }
    }
}
