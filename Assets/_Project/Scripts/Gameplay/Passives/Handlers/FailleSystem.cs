using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système central de Faille : portails persistants, trajets type Portal,
    /// effets ATK/SUP selon la spé active au moment de la traversée.
    /// </summary>
    public class FailleSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string SelfReboostAtkBuffId = "faille_self_reboost_atk";
        private const string AllyReboostAtkBuffId = "faille_ally_reboost_atk";
        private const string TeamAtkBuffId = "faille_team_atk";
        private const string TeamDefBuffId = "faille_team_def";
        private const float ExitOffset = 0.75f;
        private const float TeleportCooldown = 0.18f;
        private const float EdgeInset = 0.35f;
        private const float LaunchForceMultiplier = 50f;
        private const float MaxDragDistance = 3f;
        private const int MaxTraverseStacks = 10;
        private const int MaxFailleTeamStacks = 20;
        private const float EnemyDamageAtkRatio = 0.05f;
        private const float AllyHealHpRatio = 0.05f;
        private const float TraverseStackAtk = 0.10f;
        private const float ReboostAtkBonus = 0.20f;
        private const float TeamAllAlliesBonus = 0.20f;
        private const float TeamFailleTraverseBonus = 0.01f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static FailleSystem _instance;

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private Arena _arena;

        private FaillePortal _portalA;
        private FaillePortal _portalB;
        private bool _portalsPlaced;
        private bool _hasPlacedOnceThisCombat;
        private bool _placementMode;
        private int _placementSlot;

        private bool _atkEnemyDamage;
        private bool _atkStacks;
        private bool _atkReboost;
        private bool _supAllyHeal;
        private bool _supTeamBuff;
        private bool _supReboost;

        private int _traverseStacks;
        private readonly HashSet<int> _alliesTraversedIds = new HashSet<int>();
        private bool _allAlliesBuffGranted;
        private int _failleTeamStacks;

        private readonly Dictionary<int, float> _cooldownUntil = new Dictionary<int, float>(16);
        private readonly List<int> _cooldownKeysBuffer = new List<int>(16);

        private bool _subscribedTurnChanged;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static FailleSystem Instance => _instance;
        public CharacterBall Owner => _owner;
        public bool PortalsPlaced => _portalsPlaced;
        public bool IsPlacementMode => _placementMode;
        public bool RequiresPlacement => !_hasPlacedOnceThisCombat || !_portalsPlaced;
        public int TraverseStacks => _traverseStacks;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            // Une seule instance runtime : si une Faille vivante existe déjà, on ne l'écrase pas.
            if (_instance != null && _instance != this
                && _instance._owner != null && !_instance._owner.IsDead)
                return;

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_subscribedTurnChanged && _turnManager != null)
                _turnManager.OnTurnChanged -= OnTurnChanged;

            if (_instance == this)
                _instance = null;

            // Les portails vivent sous l'arène et doivent survivre à la mort / despawn de Faille
            // pendant le combat. Nettoyage = ResetForStage uniquement.
        }

        private void Update()
        {
            if (_cooldownUntil.Count == 0) return;

            float now = Time.time;
            _cooldownKeysBuffer.Clear();
            foreach (KeyValuePair<int, float> pair in _cooldownUntil)
            {
                if (pair.Value <= now)
                    _cooldownKeysBuffer.Add(pair.Key);
            }

            for (int i = 0; i < _cooldownKeysBuffer.Count; i++)
                _cooldownUntil.Remove(_cooldownKeysBuffer[i]);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (owner == null) return;

            if (_subscribedTurnChanged && _turnManager != null && !ReferenceEquals(_turnManager, turnManager))
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
                _subscribedTurnChanged = false;
            }

            _owner = owner;
            _turnManager = turnManager;
            _instance = this;

            if (_arena == null)
                _arena = Object.FindObjectOfType<Arena>();

            EnsurePortals();

            if (!_subscribedTurnChanged && _turnManager != null)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
                _subscribedTurnChanged = true;
            }

            ChezArthur.UI.FaillePlacementUI.EnsureExists();
        }

        public void EnableAtkEnemyDamage() => _atkEnemyDamage = true;
        public void EnableAtkStacks() => _atkStacks = true;
        public void EnableAtkReboost() => _atkReboost = true;
        public void EnableSupAllyHeal() => _supAllyHeal = true;
        public void EnableSupTeamBuff() => _supTeamBuff = true;
        public void EnableSupReboost() => _supReboost = true;

        /// <summary>
        /// Bonus ATK des stacks de traversée (actif uniquement en spé ATK).
        /// </summary>
        public float GetTraverseStackAtkBonus()
        {
            if (!_atkStacks || !IsAtkSpecActive()) return 0f;
            return _traverseStacks * TraverseStackAtk;
        }

        public void ResetForStage()
        {
            // Les stacks ATK persistent (doc) ; le palier SUP se reset au switch de spé uniquement.
            // Nouvel étage : retire les portails pour forcer un nouveau placement.
            _portalA = null;
            _portalB = null;
            _portalsPlaced = false;
            _hasPlacedOnceThisCombat = false;
            _placementMode = false;
            _placementSlot = 0;
            ExitPlacementMode();
            CleanupOrphanPortalsInScene();
        }

        public void OnSpecSwitched()
        {
            _alliesTraversedIds.Clear();
            _allAlliesBuffGranted = false;
            _failleTeamStacks = 0;
            RemoveTeamBuffs();
        }

        /// <summary>
        /// Démarre le mode placement (obligatoire au 1er tour, optionnel ensuite).
        /// </summary>
        public void BeginPlacement()
        {
            if (_owner == null) return;
            if (_turnManager == null || !ReferenceEquals(_turnManager.CurrentParticipant, _owner))
                return;

            EnsurePortals();
            _placementMode = true;
            _placementSlot = 0;
            Debug.Log("[Faille] Mode placement portails — tapez 2 bordures.");
        }

        public void CancelPlacement()
        {
            if (!_placementMode) return;
            // Annulation seulement si des portails existent déjà.
            if (_portalsPlaced)
                ExitPlacementMode();
        }

        public void ExitPlacementMode()
        {
            _placementMode = false;
            _placementSlot = 0;
        }

        /// <summary>
        /// Place le prochain portail au point monde (snap sur bordure la plus proche).
        /// </summary>
        public bool TryPlaceAtWorld(Vector2 worldPos)
        {
            if (!_placementMode) return false;
            if (_arena == null)
                _arena = Object.FindObjectOfType<Arena>();
            if (_arena == null) return false;

            EnsurePortals();
            ResolveEdgePoint(worldPos, out FaillePortalEdge edge, out Vector2 snapped, out Vector2 normal);

            FaillePortal portal = _placementSlot == 0 ? _portalA : _portalB;
            if (portal == null) return false;

            portal.Place(edge, snapped, normal);
            _placementSlot++;

            if (_placementSlot >= 2)
            {
                _portalsPlaced = true;
                _hasPlacedOnceThisCombat = true;
                ExitPlacementMode();
                Debug.Log("[Faille] Portails placés.");
            }

            return true;
        }

        /// <summary>
        /// Pose par défaut haut/bas (filet de sécurité / debug).
        /// </summary>
        public void PlaceDefaultPortals()
        {
            if (_arena == null)
                _arena = Object.FindObjectOfType<Arena>();
            if (_arena == null) return;

            EnsurePortals();
            Bounds b = _arena.Bounds;
            Vector2 top = new Vector2(b.center.x, b.max.y - EdgeInset);
            Vector2 bottom = new Vector2(b.center.x, b.min.y + EdgeInset);
            _portalA.Place(FaillePortalEdge.Top, top, Vector2.down);
            _portalB.Place(FaillePortalEdge.Bottom, bottom, Vector2.up);
            _portalsPlaced = true;
            _hasPlacedOnceThisCombat = true;
            ExitPlacementMode();
        }

        /// <summary>
        /// True si le drag de combat doit être bloqué (placement obligatoire non terminé).
        /// </summary>
        public bool BlocksNormalLaunch()
        {
            if (_owner == null || _turnManager == null) return false;
            if (!ReferenceEquals(_turnManager.CurrentParticipant, _owner)) return false;
            if (_placementMode) return true;
            return RequiresPlacement;
        }

        /// <summary>
        /// Téléporte une entité d'un portail vers l'autre (logique Portal).
        /// </summary>
        public void TryTeleportThrough(FaillePortal entry, Collider2D other)
        {
            if (!_portalsPlaced || entry == null || other == null) return;
            if (_portalA == null || _portalB == null) return;

            int id = other.GetInstanceID();
            if (_cooldownUntil.TryGetValue(id, out float until) && until > Time.time)
                return;

            FaillePortal exit = ReferenceEquals(entry, _portalA) ? _portalB : _portalA;
            if (exit == null || !exit.gameObject.activeInHierarchy) return;

            CharacterBall ball = other.GetComponent<CharacterBall>();
            if (ball != null)
            {
                TeleportBall(ball, entry, exit);
                return;
            }

            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead)
                TeleportEnemy(enemy, entry, exit);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null) return;

            bool isFailleTurn = ReferenceEquals(participant, _owner);
            if (!isFailleTurn)
            {
                ExitPlacementMode();
                return;
            }

            // Premier tour de combat : placement obligatoire.
            if (RequiresPlacement)
                BeginPlacement();
        }

        private void TeleportBall(CharacterBall ball, FaillePortal entry, FaillePortal exit)
        {
            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            Vector2 entryVel = rb.velocity;
            Vector2 exitVel = RemapVelocity(entryVel, entry.OutwardNormal, exit.OutwardNormal);
            Vector2 exitPos = exit.WorldPosition + exit.OutwardNormal * ExitOffset;

            _cooldownUntil[ball.GetInstanceID()] = Time.time + TeleportCooldown;
            _cooldownUntil[entry.GetInstanceID()] = Time.time + TeleportCooldown;
            _cooldownUntil[exit.GetInstanceID()] = Time.time + TeleportCooldown;

            ball.transform.position = exitPos;
            rb.velocity = exitVel;

            OnEntityTraversed(ball, null, wasSelf: ReferenceEquals(ball, _owner), ball.IsSuperLaunch);
            ApplyBallTraverseEffects(ball, exitVel);
        }

        private void TeleportEnemy(Enemy enemy, FaillePortal entry, FaillePortal exit)
        {
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            Vector2 entryVel = rb.velocity;
            Vector2 exitVel = RemapVelocity(entryVel, entry.OutwardNormal, exit.OutwardNormal);
            Vector2 exitPos = exit.WorldPosition + exit.OutwardNormal * ExitOffset;

            _cooldownUntil[enemy.GetInstanceID()] = Time.time + TeleportCooldown;

            enemy.transform.position = exitPos;
            rb.velocity = exitVel;

            OnEntityTraversed(null, enemy, wasSelf: false, isSuperLaunch: false);
        }

        private static Vector2 RemapVelocity(Vector2 entryVel, Vector2 entryOut, Vector2 exitOut)
        {
            if (entryOut.sqrMagnitude < 0.0001f) entryOut = Vector2.up;
            if (exitOut.sqrMagnitude < 0.0001f) exitOut = Vector2.up;

            // Entrée contre la normale du portail d'entrée → sortie le long de la normale de sortie.
            Quaternion rot = Quaternion.FromToRotation(-entryOut.normalized, exitOut.normalized);
            Vector2 remapped = rot * entryVel;

            // Si la vélocité est quasi nulle, pousse légèrement vers l'extérieur.
            if (remapped.sqrMagnitude < 0.01f)
                remapped = exitOut.normalized * 2f;

            return remapped;
        }

        private void OnEntityTraversed(CharacterBall ball, Enemy enemy, bool wasSelf, bool isSuperLaunch)
        {
            // ATK stacks : toute entité.
            if (_atkStacks && _traverseStacks < MaxTraverseStacks)
                _traverseStacks++;

            // Dégâts ennemi (spé ATK active à la traversée).
            if (enemy != null && _atkEnemyDamage && IsAtkSpecActive() && _owner != null)
            {
                int dmg = Mathf.Max(1, Mathf.RoundToInt(_owner.EffectiveAtk * EnemyDamageAtkRatio));
                enemy.TakeDamage(dmg, false);
            }

            // Heal allié (spé SUP).
            if (ball != null && !ReferenceEquals(ball, _owner) && _supAllyHeal && IsSupSpecActive())
            {
                int heal = Mathf.Max(1, Mathf.RoundToInt(ball.MaxHp * AllyHealHpRatio));
                ball.Heal(heal, _owner);
            }

            // SUP team : tracking alliés + stacks Faille.
            if (_supTeamBuff && IsSupSpecActive())
                HandleSupTeamTraverse(ball, wasSelf);
        }

        private void HandleSupTeamTraverse(CharacterBall ball, bool wasSelf)
        {
            if (ball != null && !wasSelf && !ball.IsDead)
                _alliesTraversedIds.Add(ball.GetInstanceID());

            if (!_allAlliesBuffGranted && HaveAllLivingAlliesTraversed())
            {
                _allAlliesBuffGranted = true;
                ApplyOrRefreshTeamBuffs(TeamAllAlliesBonus + _failleTeamStacks * TeamFailleTraverseBonus);
            }

            if (wasSelf && _failleTeamStacks < MaxFailleTeamStacks)
            {
                _failleTeamStacks++;
                float bonus = (_allAlliesBuffGranted ? TeamAllAlliesBonus : 0f)
                    + _failleTeamStacks * TeamFailleTraverseBonus;
                if (_allAlliesBuffGranted || _failleTeamStacks > 0)
                    ApplyOrRefreshTeamBuffs(bonus);
            }
        }

        private bool HaveAllLivingAlliesTraversed()
        {
            if (_turnManager == null) return false;
            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null || allies.Count == 0) return false;

            int living = 0;
            int traversed = 0;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                if (ReferenceEquals(ally, _owner)) continue; // « tous les alliés » = hors Faille
                living++;
                if (_alliesTraversedIds.Contains(ally.GetInstanceID()))
                    traversed++;
            }

            return living > 0 && traversed >= living;
        }

        private void ApplyBallTraverseEffects(CharacterBall ball, Vector2 exitVel)
        {
            if (ball == null) return;

            bool isFaille = ReferenceEquals(ball, _owner);
            bool isFailleOwnTurn = _turnManager != null && ReferenceEquals(_turnManager.CurrentParticipant, _owner);

            // ATK P15 : Faille se repropulse pendant son tour.
            if (isFaille && _atkReboost && IsAtkSpecActive() && isFailleOwnTurn)
            {
                ApplyTurnAtkBuff(ball, SelfReboostAtkBuffId, ReboostAtkBonus);
                bool wasSuper = ball.IsSuperLaunch;
                Repropulse(ball, exitVel, wasSuper);
                if (wasSuper)
                    ball.SetPortalLaunchDamageMultiplier(2f);
                return;
            }

            // SUP P15 : allié repropulsé.
            if (!isFaille && _supReboost && IsSupSpecActive())
            {
                ApplyTurnAtkBuff(ball, AllyReboostAtkBuffId, ReboostAtkBonus);
                bool wasSuper = ball.IsSuperLaunch;
                Repropulse(ball, exitVel, wasSuper);
                if (wasSuper)
                    ball.SetPortalLaunchDamageMultiplier(2f);
            }
        }

        private void Repropulse(CharacterBall ball, Vector2 preferredDir, bool preserveSuper)
        {
            if (ball == null) return;

            Vector2 dir = preferredDir.sqrMagnitude > 0.0001f
                ? preferredDir.normalized
                : Vector2.up;

            float lfMult = ball.EffectiveLaunchForceMultiplier;
            float fullForce = MaxDragDistance * lfMult * LaunchForceMultiplier;

            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.velocity = Vector2.zero;
            }

            if (preserveSuper)
                ball.SetNextLaunchIsSuper(true);

            ball.Launch(dir, fullForce);
            Debug.Log($"[Faille] Repropulsion {ball.Name}");
        }

        private static void ApplyTurnAtkBuff(CharacterBall ball, string buffId, float value)
        {
            if (ball == null || ball.BuffReceiver == null) return;

            ball.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = buffId,
                Source = ball,
                StatType = BuffStatType.ATK,
                Value = value,
                IsPercent = true,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        private void ApplyOrRefreshTeamBuffs(float percent)
        {
            if (_turnManager == null || _owner == null) return;
            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;

                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = TeamAtkBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = percent,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniqueGlobal = true,
                    UniquePerSource = false
                });

                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = TeamDefBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DEF,
                    Value = percent,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniqueGlobal = true,
                    UniquePerSource = false
                });
            }
        }

        private void RemoveTeamBuffs()
        {
            if (_turnManager == null) return;
            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.BuffReceiver == null) continue;
                ally.BuffReceiver.RemoveBuffsById(TeamAtkBuffId);
                ally.BuffReceiver.RemoveBuffsById(TeamDefBuffId);
            }
        }

        private void EnsurePortals()
        {
            if (_portalA != null && _portalB != null) return;

            Transform parent = _arena != null ? _arena.transform : transform;
            if (_portalA == null)
            {
                var go = new GameObject("FaillePortal_A");
                go.transform.SetParent(parent, true);
                _portalA = go.AddComponent<FaillePortal>();
                _portalA.Initialize(this, 0);
                _portalA.Hide();
            }

            if (_portalB == null)
            {
                var go = new GameObject("FaillePortal_B");
                go.transform.SetParent(parent, true);
                _portalB = go.AddComponent<FaillePortal>();
                _portalB.Initialize(this, 1);
                _portalB.Hide();
            }
        }

        /// <summary>
        /// Nettoie d'éventuels portails orphelins (Faille despawnée sans ResetForStage).
        /// </summary>
        private static void CleanupOrphanPortalsInScene()
        {
            FaillePortal[] portals = Object.FindObjectsOfType<FaillePortal>();
            for (int i = 0; i < portals.Length; i++)
            {
                if (portals[i] != null)
                    Object.Destroy(portals[i].gameObject);
            }
        }

        private void ResolveEdgePoint(Vector2 worldPos, out FaillePortalEdge edge, out Vector2 snapped, out Vector2 normal)
        {
            Bounds b = _arena.Bounds;
            float left = b.min.x + EdgeInset;
            float right = b.max.x - EdgeInset;
            float bottom = b.min.y + EdgeInset;
            float top = b.max.y - EdgeInset;

            float dLeft = Mathf.Abs(worldPos.x - left);
            float dRight = Mathf.Abs(worldPos.x - right);
            float dBottom = Mathf.Abs(worldPos.y - bottom);
            float dTop = Mathf.Abs(worldPos.y - top);

            float min = dTop;
            edge = FaillePortalEdge.Top;
            if (dBottom < min) { min = dBottom; edge = FaillePortalEdge.Bottom; }
            if (dLeft < min) { min = dLeft; edge = FaillePortalEdge.Left; }
            if (dRight < min) { edge = FaillePortalEdge.Right; }

            switch (edge)
            {
                case FaillePortalEdge.Top:
                    snapped = new Vector2(Mathf.Clamp(worldPos.x, left, right), top);
                    normal = Vector2.down;
                    break;
                case FaillePortalEdge.Bottom:
                    snapped = new Vector2(Mathf.Clamp(worldPos.x, left, right), bottom);
                    normal = Vector2.up;
                    break;
                case FaillePortalEdge.Left:
                    snapped = new Vector2(left, Mathf.Clamp(worldPos.y, bottom, top));
                    normal = Vector2.right;
                    break;
                default:
                    snapped = new Vector2(right, Mathf.Clamp(worldPos.y, bottom, top));
                    normal = Vector2.left;
                    break;
            }
        }

        private bool IsAtkSpecActive()
        {
            return _owner != null
                && _owner.ActiveSpec != null
                && _owner.ActiveSpec.Role == CharacterRole.Attacker;
        }

        private bool IsSupSpecActive()
        {
            return _owner != null
                && _owner.ActiveSpec != null
                && _owner.ActiveSpec.Role == CharacterRole.Support;
        }
    }
}
