using UnityEngine;
using ChezArthur.Enemies;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime Voltrain : mémorise le dernier mur touché, y pose une zone électrifiée
    /// et gère la décharge niveau 10 au début du tour de Voltrain.
    /// </summary>
    public class ElectricWallSystem : MonoBehaviour
    {
        private const string ParalysisBuffSpdId = "voltrain_paralysis_spd";
        private const float ElectricZoneRadius = 1.5f;

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private GameObject _electricZone;
        private bool _enhanced;
        private bool _subscribedToTurnChanged;

        public void Initialize(CharacterBall owner, TurnManager tm)
        {
            _owner = owner;
            _turnManager = tm;
            EnsureTurnChangedSubscription();
        }

        public void SetEnhanced(bool value)
        {
            _enhanced = value;
        }

        /// <summary>
        /// Appelé quand Voltrain touche un mur : ne garde qu'une seule zone (la plus récente).
        /// </summary>
        public void RecordWallHit(Vector2 contactPoint, Vector2 wallNormal)
        {
            _ = wallNormal;

            ClearElectricZone();

            GameObject go = new GameObject("ElectricZone");
            go.transform.position = contactPoint;

            CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = ElectricZoneRadius;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            ElectricZoneSegment segment = go.AddComponent<ElectricZoneSegment>();
            segment.Initialize(_owner);

            _electricZone = go;
        }

        public void ClearElectricZone()
        {
            if (_electricZone != null)
                Object.Destroy(_electricZone);
            _electricZone = null;
        }

        private void EnsureTurnChangedSubscription()
        {
            if (_subscribedToTurnChanged || _turnManager == null) return;
            _turnManager.OnTurnChanged += OnTurnChanged;
            _subscribedToTurnChanged = true;
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null || _turnManager == null) return;
            if (!ReferenceEquals(participant, _owner)) return;

            if (_enhanced)
                TryTriggerDischarge();

            // La zone dure jusqu'au prochain tour de Voltrain.
            ClearElectricZone();
        }

        private void TryTriggerDischarge()
        {
            var participants = _turnManager.Participants;
            if (participants == null) return;

            int paralyzedCount = 0;
            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] is Enemy enemy && !enemy.IsDead &&
                    enemy.BuffReceiver != null && enemy.BuffReceiver.HasBuff(ParalysisBuffSpdId))
                {
                    paralyzedCount++;
                }
            }

            if (paralyzedCount < 2) return;

            float damagePercent = paralyzedCount >= 4 ? 0.75f : 0.50f;
            int dischargeDamage = Mathf.Max(1, Mathf.RoundToInt(_owner.EffectiveAtk * damagePercent));

            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] is Enemy enemy && !enemy.IsDead &&
                    enemy.BuffReceiver != null && enemy.BuffReceiver.HasBuff(ParalysisBuffSpdId))
                {
                    enemy.TakeDamage(dischargeDamage);
                }
            }
        }

        private void OnDestroy()
        {
            if (_turnManager != null && _subscribedToTurnChanged)
                _turnManager.OnTurnChanged -= OnTurnChanged;
            _subscribedToTurnChanged = false;
            ClearElectricZone();
        }
    }
}

