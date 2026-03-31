using UnityEngine;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système runtime de Spenda :
    /// - maintient le marqueur Téléporteur sur l'allié le plus faible,
    /// - intercepte un impact ennemi pour swap avec l'allié le plus tanky,
    /// - expose l'échange VIP (niveau 10) pour l'UI future.
    /// </summary>
    public class SpendaTeleportSystem : MonoBehaviour
    {
        private const string TeleportMarkerBuffId = "spenda_teleport_marker";

        private static SpendaTeleportSystem _instance;
        public static SpendaTeleportSystem Instance => _instance;

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private bool _subscribedToTurnChanged;

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (_instance != null && _instance != this)
                _instance = this;
            else if (_instance == null)
                _instance = this;

            if (_subscribedToTurnChanged && _turnManager != null && _turnManager != turnManager)
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
                _subscribedToTurnChanged = false;
            }

            _owner = owner;
            _turnManager = turnManager;
            SubscribeToTurnChanged();
        }

        /// <summary>
        /// Recalcule quel allié vivant est "Téléporteur" (plus faible %HP).
        /// Spenda lui-même est ignoré.
        /// </summary>
        public void RefreshTeleportMarker()
        {
            if (_turnManager == null || _owner == null) return;

            var allies = _turnManager.GetAllies();
            if (allies == null) return;

            // Nettoyage de tous les marqueurs existants.
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                BuffReceiver br = ally.BuffReceiver;
                if (br != null)
                    br.RemoveBuffsById(TeleportMarkerBuffId);
            }

            CharacterBall weakest = null;
            float weakestRatio = float.MaxValue;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally == _owner) continue;
                if (ally.MaxHp <= 0) continue;

                float ratio = (float)ally.CurrentHp / ally.MaxHp;
                if (ratio < weakestRatio)
                {
                    weakestRatio = ratio;
                    weakest = ally;
                }
            }

            if (weakest == null || weakest.BuffReceiver == null) return;

            weakest.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = TeleportMarkerBuffId,
                Source = _owner,
                StatType = BuffStatType.HP, // marqueur visuel/logique
                Value = 0f,
                IsPercent = false,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        /// <summary>
        /// Si la cible a le marqueur, échange sa position avec l'allié vivant au plus haut HP,
        /// soigne la cible initiale (5%), et retourne la nouvelle cible des dégâts.
        /// </summary>
        public CharacterBall TryTeleportSwap(CharacterBall targetAlly)
        {
            if (_turnManager == null || targetAlly == null || targetAlly.IsDead) return null;
            if (targetAlly.BuffReceiver == null || !targetAlly.BuffReceiver.HasBuff(TeleportMarkerBuffId))
                return null;

            var allies = _turnManager.GetAllies();
            if (allies == null) return null;

            CharacterBall tankAlly = null;
            int bestHp = int.MinValue;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally == targetAlly) continue;

                if (ally.CurrentHp > bestHp)
                {
                    bestHp = ally.CurrentHp;
                    tankAlly = ally;
                }
            }

            if (tankAlly == null) return null;

            Vector3 posTarget = targetAlly.transform.position;
            Vector3 posTank = tankAlly.transform.position;
            targetAlly.transform.position = posTank;
            tankAlly.transform.position = posTarget;

            int heal = Mathf.RoundToInt(targetAlly.MaxHp * 0.05f);
            if (heal > 0)
                targetAlly.Heal(heal);

            return tankAlly;
        }

        /// <summary>
        /// Échange VIP manuel (UI future) : swap Spenda avec l'allié ciblé + soin 5% sur l'allié.
        /// </summary>
        public void PerformVIPSwap(CharacterBall targetAlly)
        {
            if (_owner == null || targetAlly == null || targetAlly == _owner || targetAlly.IsDead) return;

            Vector3 ownerPos = _owner.transform.position;
            Vector3 targetPos = targetAlly.transform.position;
            _owner.transform.position = targetPos;
            targetAlly.transform.position = ownerPos;

            int heal = Mathf.RoundToInt(targetAlly.MaxHp * 0.05f);
            if (heal > 0)
                targetAlly.Heal(heal);
        }

        private void SubscribeToTurnChanged()
        {
            if (_turnManager == null || _subscribedToTurnChanged) return;
            _turnManager.OnTurnChanged += OnTurnChanged;
            _subscribedToTurnChanged = true;
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null) return;

            // Approximation début de cycle : recalcul à chaque début de tour de Spenda.
            if (ReferenceEquals(participant, _owner))
                RefreshTeleportMarker();
        }

        private void OnDestroy()
        {
            if (_turnManager != null && _subscribedToTurnChanged)
                _turnManager.OnTurnChanged -= OnTurnChanged;
            _subscribedToTurnChanged = false;

            if (_instance == this)
                _instance = null;
        }
    }
}

