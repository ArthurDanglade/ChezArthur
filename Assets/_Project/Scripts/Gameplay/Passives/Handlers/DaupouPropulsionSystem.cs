using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime Daupou :
    /// propulsion hors tour quand touché par un ennemi (seuil de vélocité, force max drag),
    /// buff ATK +100 % le temps du déplacement, limite 1 fois par tour ennemi.
    /// </summary>
    public class DaupouPropulsionSystem : MonoBehaviour
    {
        private const string PropulsionAtkBuffId = "daupou_propulsion_atk";
        // Valeurs alignées sur DragDropController (drag à pleine puissance).
        private const float LaunchForceMultiplier = 50f;
        private const float MaxDragDistance = 3f;

        [Header("Équilibrage")]
        [SerializeField] private float minImpactVelocity = 6f;

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

            float impactSpeed = enemyVelocity.magnitude;
            if (impactSpeed < minImpactVelocity)
            {
                Debug.Log($"[Passif] Daupou : impact trop faible ({impactSpeed:0.0} < {minImpactVelocity})");
                return;
            }

            _propulsionUsedThisTurn = true;
            _isPassiveMovement = true;

            if (_owner.BuffReceiver != null)
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

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.velocity = Vector2.zero;

            Vector2 propulsionDir = -enemyVelocity.normalized;
            float lfMult = _owner.EffectiveLaunchForceMultiplier;
            float fullDragBaseForce = MaxDragDistance * lfMult * LaunchForceMultiplier;
            _owner.Launch(propulsionDir, fullDragBaseForce);
            Debug.Log("[Passif] Daupou : propulsé (force max)");
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

