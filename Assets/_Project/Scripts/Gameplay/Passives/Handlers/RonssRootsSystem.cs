using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système runtime de « Racines profondes » pour Ronss.
    /// Quand l'enracinement est actif, soigne les alliés proches au début du tour de Ronss.
    /// </summary>
    public class RonssRootsSystem : MonoBehaviour
    {
        private const string RootsBuffId = "ronss_roots_dr";
        private const float HealRadius = 3f;

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private bool _rootsActive;
        private bool _subscribed;

        public void Initialize(CharacterBall owner, TurnManager tm)
        {
            _owner = owner;
            _turnManager = tm;
        }

        public void SetRootsActive(bool active)
        {
            _rootsActive = active;
            UpdateSubscription();
        }

        private void UpdateSubscription()
        {
            if (_turnManager == null) return;

            if (_rootsActive && !_subscribed)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
                _subscribed = true;
            }
            else if (!_rootsActive && _subscribed)
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
                _subscribed = false;
            }
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (!_rootsActive || _owner == null || _turnManager == null) return;
            if (!ReferenceEquals(participant, _owner)) return;

            // Si le buff principal n'est plus actif, coupe le système.
            BuffReceiver ownerBr = _owner.BuffReceiver;
            if (ownerBr == null || !ownerBr.HasBuff(RootsBuffId))
            {
                _rootsActive = false;
                UpdateSubscription();
                return;
            }

            var allies = _turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally == _owner) continue;

                float dist = Vector2.Distance(ally.transform.position, _owner.transform.position);
                if (dist > HealRadius) continue;

                int heal = Mathf.Max(1, Mathf.RoundToInt(ally.MaxHp * 0.03f));
                ally.Heal(heal);
            }
        }

        private void OnDestroy()
        {
            if (_turnManager != null && _subscribed)
                _turnManager.OnTurnChanged -= OnTurnChanged;
            _subscribed = false;
        }
    }
}

