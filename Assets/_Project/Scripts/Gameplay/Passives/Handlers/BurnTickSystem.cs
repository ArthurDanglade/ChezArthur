using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.UI;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// DOT brûlure : Kram Hoisi (kram_burn) et item Boule de Feu (boule_de_feu_burn).
    /// </summary>
    public class BurnTickSystem : MonoBehaviour
    {
        private const string KramBurnBuffId = "kram_burn";
        private const string BouleDeFeuBurnBuffId = "boule_de_feu_burn";
        private const int BouleDeFeuBurnDamage = 10;

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

            if (enemy.BuffReceiver.HasBuff(KramBurnBuffId))
            {
                // 1% des PV max (comportement Kram inchangé).
                int burnDamage = Mathf.Max(1, Mathf.RoundToInt(enemy.MaxHp * 0.01f));
                enemy.SuppressNextDamagePopup();
                enemy.TakeDamage(burnDamage);
                if (FloatingNumberSpawner.Instance != null)
                    FloatingNumberSpawner.Instance.ShowBurn(burnDamage, enemy.transform.position);
            }

            if (enemy.BuffReceiver.HasBuff(BouleDeFeuBurnBuffId))
            {
                int remainingTurns = GetBuffRemainingTurns(enemy.BuffReceiver, BouleDeFeuBurnBuffId);
                enemy.SuppressNextDamagePopup();
                enemy.TakePureDamage(BouleDeFeuBurnDamage);
                if (FloatingNumberSpawner.Instance != null)
                    FloatingNumberSpawner.Instance.ShowBurn(BouleDeFeuBurnDamage, enemy.transform.position);
                Debug.Log($"[Item] Boule de Feu : tick {BouleDeFeuBurnDamage} ({remainingTurns} tours restants)");
            }
        }

        private static int GetBuffRemainingTurns(BuffReceiver buffReceiver, string buffId)
        {
            IReadOnlyList<BuffData> buffs = buffReceiver.ActiveBuffs;
            if (buffs == null) return 0;

            for (int i = 0; i < buffs.Count; i++)
            {
                BuffData buff = buffs[i];
                if (buff == null || buff.BuffId != buffId) continue;
                if (buff.RemainingTurns > 0)
                    return buff.RemainingTurns;
            }

            return 0;
        }
    }
}
