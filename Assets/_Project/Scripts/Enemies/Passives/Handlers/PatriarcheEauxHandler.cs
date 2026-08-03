using System.Collections;
using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Passives.Handlers;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Patriarche — Eaux Bénites (patriarche_eaux), fiche 5.7.
    /// Zones Impact télégraphées un tour avant (D11 — aléatoire acceptable car annoncé).
    /// Traversée = Brûlure (R8) ; résolution = gros dégâts. Crescendo N = 2→3→4.
    /// Params : value=N base, sv1=dégâts, sv2/sv3=seuils PV pour +1/+1.
    /// </summary>
    public class PatriarcheEauxHandler : EnemyPassiveHandlerBase, IEnemyIntentProvider
    {
        // ═══════════════════════════════════════════
        // CONSTANTES [G6] — placeholders R8, calibrage G7
        // ═══════════════════════════════════════════

        private const float ZONE_RADIUS = 1.2f;
        private const float BURN_PERCENT = 0.03f;
        private const int BURN_CYCLES = 2;
        private const float ARENA_MARGIN = 1f;
        private const float MIN_ZONE_DISTANCE = 2f;
        private const int MAX_SPAWN_ATTEMPTS = 24;
        private const float ACTION_DURATION = 0.80f;
        private static readonly Color WaterTint = new Color(0.35f, 0.55f, 0.85f, 1f);

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private readonly List<GroundZone> _zones = new List<GroundZone>(4);
        private readonly List<CharacterBall> _insideBuffer = new List<CharacterBall>(8);
        private readonly System.Action<CharacterBall>[] _enteredHandlers = new System.Action<CharacterBall>[4];

        private Arena _arena;
        private int _telegraphN;
        private bool _subscribedDeath;
        private bool _released;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ
        // ═══════════════════════════════════════════

        public override string HandlerId => "patriarche_eaux";

        // ═══════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _released = false;
            _telegraphN = ComputeN();
            _arena = Object.FindObjectOfType<Arena>();

            EnemyFixedTurnActionRegistry.Register(owner, ExecuteEauxAction);
            EnemyIntentSystem.RegisterProvider(owner, this);

            if (owner != null && !_subscribedDeath)
            {
                owner.OnDeath += OnOwnerDeath;
                _subscribedDeath = true;
            }
        }

        public override void Cleanup()
        {
            ReleaseAllResources();
            base.Cleanup();
        }

        public override void ResetForNewStage()
        {
            ReleaseZones();
            _telegraphN = ComputeN();
        }

        // ═══════════════════════════════════════════
        // IEnemyIntentProvider
        // ═══════════════════════════════════════════

        public bool TryGetIntent(out EnemyIntent intent)
        {
            intent = default;
            if (!IsReady || _data == null)
                return false;

            int n = _zones.Count > 0 ? _zones.Count : ComputeN();
            intent.Kind = EnemyIntentKind.Zone;
            intent.Target = null;
            intent.IconText = "◉";
            intent.ShortLabel = "Eaux Bénites ×" + n;
            return true;
        }

        public void OnTelegraphStateChanged(bool isTelegraphing)
        {
            if (!IsReady || _data == null)
                return;

            if (isTelegraphing)
            {
                // Aléatoire ACCEPTABLE — télégraphié un tour avant (D11).
                SpawnTelegraphZones();
                return;
            }

            // false : mort avant d'agir / plus le prochain télégraphié.
            // Si c'est NOTRE tour qui commence, garder les zones pour l'action Fixed.
            if (_owner != null && _turnManager != null
                && ReferenceEquals(_turnManager.CurrentParticipant, _owner))
                return;

            ReleaseZones();
        }

        // ═══════════════════════════════════════════
        // N / ZONES
        // ═══════════════════════════════════════════

        private int ComputeN()
        {
            if (_data == null || _owner == null)
                return 2;

            float ratio = _owner.MaxHp > 0 ? (float)_owner.CurrentHp / _owner.MaxHp : 0f;
            int n = Mathf.RoundToInt(_data.Value);
            if (n < 1)
                n = 2;
            if (ratio < _data.SpecialValue2)
                n++;
            if (ratio < _data.SpecialValue3)
                n++;
            return n;
        }

        private void SpawnTelegraphZones()
        {
            ReleaseZones();
            _telegraphN = ComputeN();

            for (int i = 0; i < _telegraphN; i++)
            {
                if (!TryPickZonePosition(out Vector2 pos))
                    pos = FallbackPosition(i);

                GroundZone zone = GroundZoneSystem.CreateZone(
                    _owner,
                    ZoneKind.Impact,
                    ZoneShape.Circle,
                    new Vector2(ZONE_RADIUS, ZONE_RADIUS),
                    pos,
                    WaterTint);

                if (zone == null)
                    continue;

                int captured = _zones.Count;
                System.Action<CharacterBall> handler = ally => OnAllyEnteredZone(ally);
                _enteredHandlers[captured] = handler;
                zone.OnAllyEntered += handler;
                _zones.Add(zone);
            }
        }

        private void OnAllyEnteredZone(CharacterBall ally)
        {
            // Traverser brûle — briques R7+R8.
            if (ally == null || ally.IsDead || !ally.IsMoving)
                return;
            if (_owner == null || _owner.IsDead)
                return;

            AllyDotSystem.ApplyBurn(ally, BURN_PERCENT, BURN_CYCLES, _owner);
        }

        private bool TryPickZonePosition(out Vector2 position)
        {
            position = Vector2.zero;
            if (_arena == null)
                return false;

            Bounds b = _arena.Bounds;
            float xMin = b.min.x + ARENA_MARGIN;
            float xMax = b.max.x - ARENA_MARGIN;
            float yMin = b.min.y + ARENA_MARGIN;
            float yMax = b.max.y - ARENA_MARGIN;

            for (int attempt = 0; attempt < MAX_SPAWN_ATTEMPTS; attempt++)
            {
                Vector2 p = new Vector2(
                    Random.Range(xMin, xMax),
                    Random.Range(yMin, yMax));

                bool ok = true;
                for (int i = 0; i < _zones.Count; i++)
                {
                    if (_zones[i] == null)
                        continue;
                    if (Vector2.Distance(p, _zones[i].transform.position) < MIN_ZONE_DISTANCE)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    position = p;
                    return true;
                }
            }

            return false;
        }

        private Vector2 FallbackPosition(int index)
        {
            if (_arena == null)
                return _owner != null ? (Vector2)_owner.transform.position : Vector2.zero;

            Bounds b = _arena.Bounds;
            float t = (_telegraphN > 1) ? (float)index / (_telegraphN - 1) : 0.5f;
            return new Vector2(
                Mathf.Lerp(b.min.x + ARENA_MARGIN, b.max.x - ARENA_MARGIN, t),
                b.center.y);
        }

        // ═══════════════════════════════════════════
        // ACTION DE TOUR
        // ═══════════════════════════════════════════

        private IEnumerator ExecuteEauxAction()
        {
            if (!IsReady || _data == null)
                yield break;

            // Pulse final (~0.8 s) puis résolution.
            float elapsed = 0f;
            while (elapsed < ACTION_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            int dmg = Mathf.RoundToInt(_data.SpecialValue1);
            for (int i = 0; i < _zones.Count; i++)
            {
                GroundZone zone = _zones[i];
                if (zone == null)
                    continue;

                zone.GetAlliesInside(_insideBuffer);
                for (int a = 0; a < _insideBuffer.Count; a++)
                {
                    CharacterBall ally = _insideBuffer[a];
                    if (ally == null || ally.IsDead)
                        continue;
                    if (dmg > 0)
                        ally.TakeDamage(dmg);
                }
            }

            ReleaseZones();
        }

        // ═══════════════════════════════════════════
        // RELEASE
        // ═══════════════════════════════════════════

        private void ReleaseZones()
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                GroundZone zone = _zones[i];
                if (zone == null)
                    continue;

                if (i < _enteredHandlers.Length && _enteredHandlers[i] != null)
                {
                    zone.OnAllyEntered -= _enteredHandlers[i];
                    _enteredHandlers[i] = null;
                }

                GroundZoneSystem.ReleaseZone(zone);
            }

            _zones.Clear();
            for (int i = 0; i < _enteredHandlers.Length; i++)
                _enteredHandlers[i] = null;
        }

        private void OnOwnerDeath()
        {
            ReleaseAllResources();
        }

        private void ReleaseAllResources()
        {
            if (_released)
                return;
            _released = true;

            ReleaseZones();

            if (_owner != null)
            {
                EnemyFixedTurnActionRegistry.Unregister(_owner);
                EnemyIntentSystem.UnregisterProvider(_owner);
            }

            if (_owner != null && _subscribedDeath)
            {
                _owner.OnDeath -= OnOwnerDeath;
                _subscribedDeath = false;
            }

            _arena = null;
        }
    }
}
