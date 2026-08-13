using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Feedback;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Traînée de feu Kram Hoisi : spawns des segments le long du déplacement du lanceur,
    /// puis applique brûlure / porteur via <see cref="FireTrailSegment"/>.
    /// Composant attaché dynamiquement sur le <c>CharacterBall</c> de Kram.
    /// </summary>
    public class FireTrailSystem : MonoBehaviour
    {
        private const float TrailSegmentSpacing = 0.5f;
        private const float SegmentRadius = 0.3f;

        private CharacterBall _owner;
        private TurnManager _turnManager;

        private bool _enhanced; // Passif niveau 10 : « Tout brûle, tout flambe »
        private readonly List<GameObject> _trailSegments = new List<GameObject>();

        private bool _isTrailActive;
        private Vector2 _lastSegmentPos;
        private bool _subscribedToOwnerStopped;

        /// <summary> Attribution capturée au StartTrail (sous scope sync du passif). </summary>
        private BuffOriginScope.Frame _attribution;

        /// <summary> Indique si la traînée est en mode « niveau 10 ». </summary>
        public bool IsEnhanced => _enhanced;

        /// <summary> Attribution passif pour segments différés. </summary>
        public BuffOriginScope.Frame Attribution => _attribution;

        /// <summary>
        /// Stocke les références et prépare l'état interne.
        /// </summary>
        public void Initialize(CharacterBall owner, TurnManager tm)
        {
            _owner = owner;
            _turnManager = tm;
            _isTrailActive = false;
            _lastSegmentPos = _owner != null ? (Vector2)_owner.transform.position : Vector2.zero;
        }

        /// <summary> Active / désactive l'état « niveau 10 ». </summary>
        public void SetEnhanced(bool value)
        {
            _enhanced = value;
        }

        /// <summary> Démarre la traînée : commence à spawner les segments pendant que Kram est en mouvement. </summary>
        /// <summary> Démarre la traînée : commence à spawner les segments pendant que Kram est en mouvement. </summary>
        public void StartTrail()
        {
            StartTrail(BuffOriginScope.Current);
        }

        /// <summary> Démarre la traînée en capturant l'attribution passif (règle générale différée). </summary>
        public void StartTrail(BuffOriginScope.Frame attribution)
        {
            if (_owner == null) return;

            _attribution = attribution;
            if (string.IsNullOrEmpty(_attribution.DisplayName) && _owner != null)
            {
                Transform visual = _owner.Visual != null ? _owner.Visual : _owner.transform;
                // Garde au moins l'ancre source ; le nom devrait venir du scope dispatcher.
                _attribution = new BuffOriginScope.Frame(
                    BuffOrigin.Passif,
                    visual,
                    _attribution.DisplayName,
                    _attribution.PassiveId,
                    _attribution.Silent,
                    _attribution.ActivationId);
            }

            _isTrailActive = true;

            if (!_subscribedToOwnerStopped)
            {
                _owner.OnStopped += HandleOwnerStopped;
                _subscribedToOwnerStopped = true;
            }

            _lastSegmentPos = _owner.transform.position;

            SpawnSegmentAt(_owner.transform.position);
        }

        /// <summary> Arrête le spawner (les segments existants restent jusqu'au ClearTrail). </summary>
        public void StopTrail()
        {
            _isTrailActive = false;

            if (_owner != null && _subscribedToOwnerStopped)
            {
                _owner.OnStopped -= HandleOwnerStopped;
                _subscribedToOwnerStopped = false;
            }
        }

        /// <summary> Détruit tous les segments de la traînée courante. </summary>
        public void ClearTrail()
        {
            for (int i = _trailSegments.Count - 1; i >= 0; i--)
            {
                GameObject go = _trailSegments[i];
                if (go != null)
                    Object.Destroy(go);
            }
            _trailSegments.Clear();
        }

        private void HandleOwnerStopped()
        {
            StopTrail();
        }

        private void FixedUpdate()
        {
            if (!_isTrailActive) return;
            if (_owner == null) return;
            if (!_owner.IsMoving) return;

            Vector2 currentPos = _owner.transform.position;
            float dist = Vector2.Distance(currentPos, _lastSegmentPos);
            if (dist < TrailSegmentSpacing) return;

            SpawnSegmentAt(currentPos);
            _lastSegmentPos = currentPos;
        }

        private void SpawnSegmentAt(Vector3 position)
        {
            GameObject segmentGO = new GameObject("FireTrailSegment");
            segmentGO.transform.position = position;

            // Collider trigger pour détecter les alliés/ennemis qui passent dessus.
            CircleCollider2D trigger = segmentGO.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = SegmentRadius;

            // Rigidbody nécessaire pour déclencher OnTriggerEnter2D.
            Rigidbody2D rb = segmentGO.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            FireTrailSegment segment = segmentGO.AddComponent<FireTrailSegment>();
            segment.Initialize(_owner, _enhanced, _attribution);

            _trailSegments.Add(segmentGO);
        }

        private void OnDestroy()
        {
            StopTrail();
            ClearTrail();
        }
    }
}

