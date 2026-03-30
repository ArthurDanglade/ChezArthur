using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Gère les stuns (skip du prochain tour ennemi). Réutilisable (Elfert, Lanssé, etc.).
    /// </summary>
    public class StunSystem : MonoBehaviour
    {
        /// <summary> Identifiant commun des buffs de stun (un par ennemi via son BuffReceiver). </summary>
        public const string StunBuffId = "stun";

        private static StunSystem _instance;
        public static StunSystem Instance => _instance;

        private TurnManager _turnManager;
        private readonly HashSet<Enemy> _stunnedEnemies = new HashSet<Enemy>();

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

        /// <summary> Indique si l'ennemi doit skip son tour (bloque mouvement / IA jusqu'au skip). </summary>
        public bool IsStunned(Enemy enemy)
        {
            return enemy != null && _stunnedEnemies.Contains(enemy);
        }

        /// <summary> Retire le stun sans passer par le skip de tour (ex. changement de cible Lumino). </summary>
        public void RemoveStunFromEnemy(Enemy enemy)
        {
            if (enemy == null) return;
            if (!_stunnedEnemies.Remove(enemy)) return;
            RemoveStunBuff(enemy);
        }

        /// <summary> Marque l'ennemi pour un tour sauté au prochain tour ennemi + buff de suivi. </summary>
        public void StunEnemy(Enemy enemy, CharacterBall source = null)
        {
            if (enemy == null || enemy.IsDead) return;

            _stunnedEnemies.Add(enemy);

            BuffReceiver br = enemy.BuffReceiver;
            if (br != null)
            {
                br.RemoveBuffsById(StunBuffId);
                var stunBuff = new BuffData
                {
                    BuffId = StunBuffId,
                    Source = source,
                    StatType = BuffStatType.Speed,
                    Value = 0f,
                    IsPercent = false,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                };
                br.AddBuff(stunBuff);
            }
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            PruneDeadStunned();

            if (_turnManager == null) return;

            if (participant is Enemy e && _stunnedEnemies.Contains(e))
            {
                _stunnedEnemies.Remove(e);
                RemoveStunBuff(e);
                _turnManager.SkipCurrentTurn();
            }
        }

        private void PruneDeadStunned()
        {
            if (_stunnedEnemies.Count == 0) return;

            List<Enemy> toRemove = null;
            foreach (Enemy en in _stunnedEnemies)
            {
                if (en == null || en.IsDead)
                {
                    if (toRemove == null)
                        toRemove = new List<Enemy>(_stunnedEnemies.Count);
                    toRemove.Add(en);
                }
            }

            if (toRemove == null) return;
            for (int i = 0; i < toRemove.Count; i++)
                _stunnedEnemies.Remove(toRemove[i]);
        }

        private static void RemoveStunBuff(Enemy enemy)
        {
            if (enemy == null) return;
            BuffReceiver br = enemy.BuffReceiver;
            if (br != null)
                br.RemoveBuffsById(StunBuffId);
        }
    }
}
