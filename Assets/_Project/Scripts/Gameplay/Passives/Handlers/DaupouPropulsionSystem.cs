using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime Daupou :
    /// propulsion hors tour quand touché par un ennemi, avec limite 1 fois par tour.
    /// </summary>
    public class DaupouPropulsionSystem : MonoBehaviour
    {
        private const string PropulsionAtkBuffId = "daupou_propulsion_atk";

        private CharacterBall _owner;
        private TurnManager _turnManager;

        private bool _enhanced;
        private bool _isPassiveMovement;
        private bool _propulsionUsedThisTurn;

        private bool _subscribedToOwnerStopped;
        private bool _subscribedToTurnChanged;

        public bool IsPassiveMovement => _isPassiveMovement;

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (owner == null) return;

            if (_subscribedToOwnerStopped && _owner != null && _owner != owner)
            {
                _owner.OnStopped -= OnDaupouStoppedAfterPropulsion;
                _subscribedToOwnerStopped = false;
            }
            if (_subscribedToTurnChanged && _turnManager != null && _turnManager != turnManager)
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
                _subscribedToTurnChanged = false;
            }

            _owner = owner;
            _turnManager = turnManager;

            if (!_subscribedToOwnerStopped && _owner != null)
            {
                _owner.OnStopped += OnDaupouStoppedAfterPropulsion;
                _subscribedToOwnerStopped = true;
            }

            if (!_subscribedToTurnChanged && _turnManager != null)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
                _subscribedToTurnChanged = true;
            }
        }

        public void SetEnhanced(bool value)
        {
            _enhanced = value;
        }

        public void ResetState()
        {
            _isPassiveMovement = false;
            _propulsionUsedThisTurn = false;

            if (_owner != null && _owner.BuffReceiver != null)
                _owner.BuffReceiver.RemoveBuffsById(PropulsionAtkBuffId);
        }

        /// <summary>
        /// Tente de propulser Daupou dans la direction opposée à la vélocité ennemie.
        /// </summary>
        public void TryPropulse(Vector2 enemyVelocity)
        {
            if (_owner == null) return;
            if (_propulsionUsedThisTurn) return;
            if (enemyVelocity.sqrMagnitude <= 0.0001f) return;

            _propulsionUsedThisTurn = true;
            _isPassiveMovement = true;

            if (_enhanced && _owner.BuffReceiver != null)
            {
                _owner.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = PropulsionAtkBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = 1.00f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }

            Rigidbody2D rb = _owner.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            Vector2 propulsionDir = -enemyVelocity.normalized;
            float propulsionForce = enemyVelocity.magnitude * 0.80f;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.velocity = Vector2.zero;
            rb.AddForce(propulsionDir * propulsionForce, ForceMode2D.Impulse);
        }

        private void OnDaupouStoppedAfterPropulsion()
        {
            if (!_isPassiveMovement || _owner == null) return;

            _isPassiveMovement = false;

            if (_owner.BuffReceiver != null)
                _owner.BuffReceiver.RemoveBuffsById(PropulsionAtkBuffId);

            Rigidbody2D rb = _owner.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Si ce n'est pas son tour actif, on le remet en kinematic.
                bool isOwnersTurn = _turnManager != null && ReferenceEquals(_turnManager.CurrentParticipant, _owner);
                if (!isOwnersTurn)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.velocity = Vector2.zero;
                }
            }

            _turnManager?.RefreshMovableStates();
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            _propulsionUsedThisTurn = false;
        }

        private void OnDestroy()
        {
            if (_subscribedToOwnerStopped && _owner != null)
                _owner.OnStopped -= OnDaupouStoppedAfterPropulsion;
            if (_subscribedToTurnChanged && _turnManager != null)
                _turnManager.OnTurnChanged -= OnTurnChanged;
        }
    }
}

