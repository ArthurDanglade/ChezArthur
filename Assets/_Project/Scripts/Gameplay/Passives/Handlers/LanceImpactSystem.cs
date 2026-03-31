using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système runtime de Lans Himmer :
    /// - marque la position de fin de tour (OnStopped),
    /// - fait tomber la lance au début du tour suivant (OnTurnChanged sur Lans).
    /// </summary>
    public class LanceImpactSystem : MonoBehaviour
    {
        private const float BaseImpactRadius = 1.5f;
        private const float ExpandedImpactRadius = 2.0f;

        private CharacterBall _owner;
        private TurnManager _turnManager;

        private Vector2 _markedPosition;
        private bool _hasMarkedPosition;
        private bool _enhanced;

        private bool _subscribedToTurnChanged;
        private bool _subscribedToStopped;

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

            SubscribeToStopped();
            SubscribeToTurnChanged();
        }

        public void SetEnhanced(bool value)
        {
            _enhanced = value;
        }

        /// <summary> Utilisé au début d'étage : pas d'impact reporté entre deux stages. </summary>
        public void ResetMarkedPosition()
        {
            _hasMarkedPosition = false;
        }

        private void SubscribeToStopped()
        {
            if (_owner == null || _subscribedToStopped) return;
            _owner.OnStopped += OnOwnerStopped;
            _subscribedToStopped = true;
        }

        private void SubscribeToTurnChanged()
        {
            if (_turnManager == null || _subscribedToTurnChanged) return;
            _turnManager.OnTurnChanged += OnTurnChanged;
            _subscribedToTurnChanged = true;
        }

        private void OnOwnerStopped()
        {
            if (_owner == null) return;
            _markedPosition = _owner.transform.position;
            _hasMarkedPosition = true;
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null || _turnManager == null) return;
            if (!ReferenceEquals(participant, _owner)) return;
            if (!_hasMarkedPosition) return;

            float impactRadius = _enhanced ? ExpandedImpactRadius : BaseImpactRadius;
            int damage = Mathf.Max(1, Mathf.RoundToInt(_owner.EffectiveAtk * 1.20f));

            var participants = _turnManager.Participants;
            for (int i = 0; i < participants.Count; i++)
            {
                Enemy enemy = participants[i] as Enemy;
                if (enemy == null || enemy.IsDead) continue;

                float dist = Vector2.Distance(enemy.Transform.position, _markedPosition);
                if (dist <= impactRadius)
                    enemy.TakeDamage(damage);
            }

            _hasMarkedPosition = false;
        }

        private void OnDestroy()
        {
            if (_subscribedToTurnChanged && _turnManager != null)
                _turnManager.OnTurnChanged -= OnTurnChanged;
            if (_subscribedToStopped && _owner != null)
                _owner.OnStopped -= OnOwnerStopped;

            _subscribedToTurnChanged = false;
            _subscribedToStopped = false;
        }
    }
}

