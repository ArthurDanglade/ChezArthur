using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// DOT « Brûlure » de Kram Hoisi : 1% des PV max / tour sur les ennemis brûlés.
    /// </summary>
    public class BurnTickSystem : MonoBehaviour
    {
        private const string BurnBuffId = "kram_burn";

        private static BurnTickSystem _instance;
        public static BurnTickSystem Instance => _instance;

        private TurnManager _turnManager;

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

        /// <summary>
        /// Branche le système sur le TurnManager (appelé au début de la run).
        /// </summary>
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

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (participant == null || participant.IsDead || participant.IsAlly) return;

            Enemy enemy = participant as Enemy;
            if (enemy == null) return;

            if (enemy.BuffReceiver == null) return;
            if (!enemy.BuffReceiver.HasBuff(BurnBuffId)) return;

            // 1% des PV max.
            int burnDamage = Mathf.Max(1, Mathf.RoundToInt(enemy.MaxHp * 0.01f));
            enemy.TakeDamage(burnDamage);
        }
    }
}

