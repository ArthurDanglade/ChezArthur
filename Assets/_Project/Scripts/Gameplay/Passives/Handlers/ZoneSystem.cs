using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Zone de protection circulaire de Zoneur : alliés à l'arrêt dans le rayon subissent moins de dégâts.
    /// Niv. 10 : zone élargie + buff de passage pour les alliés qui traversent en mouvement.
    /// Composant ajouté sur le CharacterBall de Zoneur (pas singleton global).
    /// </summary>
    public class ZoneSystem : MonoBehaviour
    {
        private const string PassThroughBuffId = "zoneur_passthrough";

        private float _baseRadius = 2f;
        private float _currentRadius;
        private bool _expanded;
        private CircleCollider2D _zoneCollider;
        private CharacterBall _owner;

        /// <summary> Alliés déjà buffés « passage » pour ce cycle (reset par étage). </summary>
        private HashSet<CharacterBall> _passThroughThisCycle;

        /// <summary>
        /// Initialise la zone (collider trigger). Peut être rappelé si le TurnManager change.
        /// </summary>
        public void Initialize(CharacterBall owner, TurnManager turnManager, float baseRadius = 2f)
        {
            _owner = owner;
            _ = turnManager; // Référence future (reset par cycle combat / sync tour).
            _baseRadius = baseRadius;
            _currentRadius = _expanded ? _baseRadius * 1.5f : _baseRadius;
            if (_passThroughThisCycle == null)
                _passThroughThisCycle = new HashSet<CharacterBall>();

            if (_zoneCollider == null)
            {
                _zoneCollider = gameObject.AddComponent<CircleCollider2D>();
                _zoneCollider.isTrigger = true;
            }

            _zoneCollider.radius = _currentRadius;
        }

        /// <summary> Active l'agrandissement de zone (passif niv. 10) : rayon × 1,5. </summary>
        public void ExpandZone()
        {
            _expanded = true;
            _currentRadius = _baseRadius * 1.5f;
            if (_zoneCollider != null)
                _zoneCollider.radius = _currentRadius;
        }

        /// <summary> Réinitialise le suivi des passages (nouvel étage ou début de cycle futur). </summary>
        public void ResetCycleTracking()
        {
            _passThroughThisCycle?.Clear();
        }

        /// <summary>
        /// Fraction de réduction des dégâts (0–1) si l'allié est dans la zone et immobile. Zoneur exclu.
        /// </summary>
        public float GetDamageReductionForAlly(CharacterBall ally)
        {
            if (ally == null || _owner == null) return 0f;
            if (ally == _owner) return 0f;

            float distance = Vector2.Distance(ally.transform.position, _owner.transform.position);

            if (distance <= _currentRadius && !ally.IsMoving)
                return 0.40f;

            return 0f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_expanded) return;

            CharacterBall ally = other.GetComponent<CharacterBall>();
            if (ally == null || ally == _owner || ally.IsDead) return;

            // Traversée : l'allié doit être en mouvement au moment de l'entrée dans le trigger.
            if (!ally.IsMoving) return;

            if (_passThroughThisCycle != null && _passThroughThisCycle.Contains(ally)) return;

            BuffReceiver br = ally.BuffReceiver;
            if (br != null)
            {
                var passThrough = new BuffData
                {
                    BuffId = PassThroughBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DamageReduction,
                    Value = 0.20f,
                    IsPercent = true,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniquePerSource = true,
                    UniqueGlobal = false
                };
                br.AddBuff(passThrough);
            }

            _passThroughThisCycle?.Add(ally);
        }
    }
}
