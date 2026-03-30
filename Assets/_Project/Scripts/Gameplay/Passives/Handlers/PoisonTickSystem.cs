using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Gère le DOT du poison Pusam Air et l'empoisonnement via les porteurs.
    /// À placer en scène (souvent sur le même GameObject que <see cref="SpecialPassiveRegistry"/>).
    /// </summary>
    public class PoisonTickSystem : MonoBehaviour
    {
        private const string PoisonBuffId = "pusamair_poison";
        private const string CarrierBuffId = "pusamair_carrier";

        private static PoisonTickSystem _instance;
        public static PoisonTickSystem Instance => _instance;

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
        /// Branche le système sur le TurnManager (appeler au début de la run, ex. RunManager).
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

        /// <summary>
        /// À chaque changement de tour : DOT sur l'ennemi empoisonné dont c'est le tour.
        /// </summary>
        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (participant == null || participant.IsAlly || participant.IsDead) return;

            Enemy enemy = participant as Enemy;
            if (enemy == null) return;

            BuffReceiver br = enemy.BuffReceiver;
            if (br == null || !br.HasBuff(PoisonBuffId)) return;

            int poisonedCount = CountPoisonedEnemies();
            int pusamAirAtk = GetPusamAirAtk(br);
            float damagePercent = 0.10f * poisonedCount;
            int poisonDamage = Mathf.Max(1, Mathf.RoundToInt(pusamAirAtk * damagePercent));

            enemy.TakeDamage(poisonDamage);
        }

        /// <summary>
        /// Compte les ennemis vivants marqués empoisonnés.
        /// </summary>
        private int CountPoisonedEnemies()
        {
            if (_turnManager == null) return 0;

            IReadOnlyList<ITurnParticipant> participants = _turnManager.Participants;
            int count = 0;
            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant p = participants[i];
                if (p == null || p.IsAlly || p.IsDead) continue;

                Enemy enemy = p as Enemy;
                if (enemy == null) continue;

                BuffReceiver br = enemy.BuffReceiver;
                if (br != null && br.HasBuff(PoisonBuffId))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// ATK effective de la source du poison (Pusam Air) depuis le buff sur l'ennemi.
        /// </summary>
        private int GetPusamAirAtk(BuffReceiver enemyBuffReceiver)
        {
            IReadOnlyList<BuffData> buffs = enemyBuffReceiver.ActiveBuffs;
            for (int i = 0; i < buffs.Count; i++)
            {
                BuffData b = buffs[i];
                if (b != null && b.BuffId == PoisonBuffId && b.Source != null)
                    return b.Source.EffectiveAtk;
            }
            return 0;
        }

        /// <summary>
        /// Si l'allié est porteur, applique le poison à l'ennemi touché puis retire le porteur.
        /// </summary>
        public void TryApplyCarrierPoison(CharacterBall carrier, Enemy enemy)
        {
            if (carrier == null || enemy == null) return;

            BuffReceiver carrierBr = carrier.BuffReceiver;
            if (carrierBr == null || !carrierBr.HasBuff(CarrierBuffId)) return;

            BuffReceiver enemyBr = enemy.BuffReceiver;
            if (enemyBr == null) return;

            CharacterBall pusamAir = null;
            IReadOnlyList<BuffData> carrierBuffs = carrierBr.ActiveBuffs;
            for (int i = 0; i < carrierBuffs.Count; i++)
            {
                BuffData b = carrierBuffs[i];
                if (b != null && b.BuffId == CarrierBuffId && b.Source != null)
                {
                    pusamAir = b.Source;
                    break;
                }
            }

            if (pusamAir == null) return;

            enemyBr.RemoveBuffsById(PoisonBuffId);
            var poisonBuff = new BuffData
            {
                BuffId = PoisonBuffId,
                Source = pusamAir,
                StatType = BuffStatType.DamageAmplification,
                Value = 0f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            };
            enemyBr.AddBuff(poisonBuff);

            carrierBr.RemoveBuffsById(CarrierBuffId);
        }
    }
}
