using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Traînée de lait Goat : segments soignants posés pendant le lancer.
    /// </summary>
    public class GoatMilkTrailSystem : MonoBehaviour
    {
        private const float TrailSegmentSpacing = 0.5f;
        private const float SegmentRadius = 0.3f;

        private CharacterBall _owner;
        private readonly List<GameObject> _trailSegments = new List<GameObject>();
        private bool _isTrailActive;
        private Vector2 _lastSegmentPos;
        private bool _subscribedToOwnerStopped;

        public void Initialize(CharacterBall owner)
        {
            _owner = owner;
            _isTrailActive = false;
            _lastSegmentPos = _owner != null ? (Vector2)_owner.transform.position : Vector2.zero;
        }

        public void StartTrail()
        {
            if (_owner == null) return;
            _isTrailActive = true;

            if (!_subscribedToOwnerStopped)
            {
                _owner.OnStopped += HandleOwnerStopped;
                _subscribedToOwnerStopped = true;
            }

            _lastSegmentPos = _owner.transform.position;
            SpawnSegmentAt(_owner.transform.position);
        }

        public void StopTrail()
        {
            _isTrailActive = false;

            if (_owner != null && _subscribedToOwnerStopped)
            {
                _owner.OnStopped -= HandleOwnerStopped;
                _subscribedToOwnerStopped = false;
            }
        }

        public void ClearTrail()
        {
            StopTrail();

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
            if (!_isTrailActive || _owner == null || !_owner.IsMoving) return;

            Vector2 currentPos = _owner.transform.position;
            float dist = Vector2.Distance(currentPos, _lastSegmentPos);
            if (dist < TrailSegmentSpacing) return;

            SpawnSegmentAt(currentPos);
            _lastSegmentPos = currentPos;
        }

        private void SpawnSegmentAt(Vector3 position)
        {
            GameObject segmentGO = new GameObject("GoatMilkSegment");
            segmentGO.transform.position = position;

            CircleCollider2D trigger = segmentGO.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = SegmentRadius;

            Rigidbody2D rb = segmentGO.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            GoatMilkSegment segment = segmentGO.AddComponent<GoatMilkSegment>();
            segment.Initialize(_owner);

            _trailSegments.Add(segmentGO);
        }

        private void OnDestroy()
        {
            ClearTrail();
        }
    }
}

