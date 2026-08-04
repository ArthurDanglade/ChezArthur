using ChezArthur.Enemies;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives.Handlers;
using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Driver d'état par unité : boucle unique priorisée, teinte gel, publication pastilles.
    /// Écoute uniquement — zéro gameplay. Prefabs via Resources (ajout runtime, pas de SerializeField).
    /// </summary>
    public class UnitStatusFx : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // TYPES INTERNES
        // ═══════════════════════════════════════════
        private enum StatusSlot
        {
            Freeze = 0,
            Stun = 1,
            Burn = 2,
            Poison = 3,
            Shield = 4,
            BuffUp = 5,
            DebuffDown = 6,
            Count = 7
        }

        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        // Déviation .cursorrules : Resources requis (composant AddComponent runtime, pas de ref sérialisée).
        private const string LoopBurnPath = "VFX/Feedback/Loops/LoopBurn";
        private const string LoopPoisonPath = "VFX/Feedback/Loops/LoopPoison";
        private const string LoopShieldPath = "VFX/Feedback/Loops/LoopShield";
        private const string LoopStunPath = "VFX/Feedback/Loops/LoopStun";
        private const string LoopFreezePath = "VFX/Feedback/Loops/LoopFreeze";

        // ═══════════════════════════════════════════
        // CACHE STATIQUE PREFABS
        // ═══════════════════════════════════════════
        private static ParticleSystem _prefabBurn;
        private static ParticleSystem _prefabPoison;
        private static ParticleSystem _prefabShield;
        private static ParticleSystem _prefabStun;
        private static ParticleSystem _prefabFreeze;
        private static bool _prefabsResolved;
        private static bool _warnedMissingLoops;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private CharacterBall _ball;
        private Enemy _enemy;
        private BuffReceiver _buffReceiver;
        private EnemyShieldSystem _shieldSystem;
        private SpriteRenderer _renderer;

        private readonly int[] _counts = new int[(int)StatusSlot.Count];
        private StatusSlot _activeLoop = StatusSlot.Count; // Count = aucune
        private ParticleSystem _loopInstance;

        private bool _freezeTintActive;
        private Color _capturedColor = Color.white;
        private bool _colorCaptured;

        private System.Action<BuffData> _onBuffAdded;
        private System.Action<BuffData, BuffRemovalReason> _onBuffRemoved;
        private System.Action<CharacterBall, bool> _onAllyBurn;
        private System.Action<bool> _onShieldPresence;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event System.Action OnPipsChanged;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Résout les refs, s'abonne, sync initiale.
        /// </summary>
        public void Initialize()
        {
            _ball = GetComponent<CharacterBall>();
            _enemy = GetComponent<Enemy>();
            _buffReceiver = GetComponent<BuffReceiver>();
            _shieldSystem = GetComponent<EnemyShieldSystem>();
            ResolveRenderer();

            _onBuffAdded = OnBuffAdded;
            _onBuffRemoved = OnBuffRemoved;
            _onAllyBurn = OnAllyBurnChanged;
            _onShieldPresence = OnShieldPresenceChanged;

            if (_buffReceiver != null)
            {
                _buffReceiver.OnBuffAdded += _onBuffAdded;
                _buffReceiver.OnBuffRemoved += _onBuffRemoved;
            }

            if (_ball != null)
                AllyDotSystem.OnBurnStateChanged += _onAllyBurn;

            if (_shieldSystem != null)
                _shieldSystem.OnShieldPresenceChanged += _onShieldPresence;

            SyncInitial();
            RefreshPresentation();
        }

        /// <summary>
        /// Remplit buffer avec causes actives hors boucle. Retourne le compte.
        /// </summary>
        public int GetActivePips(FeedbackCause[] buffer)
        {
            if (buffer == null)
                return 0;

            int written = 0;
            // Ordre pastilles : Stun, Freeze, Burn, Poison, Shield, BuffUp, DebuffDown
            StatusSlot[] order =
            {
                StatusSlot.Stun, StatusSlot.Freeze, StatusSlot.Burn, StatusSlot.Poison,
                StatusSlot.Shield, StatusSlot.BuffUp, StatusSlot.DebuffDown
            };

            for (int i = 0; i < order.Length && written < buffer.Length; i++)
            {
                StatusSlot slot = order[i];
                if (_counts[(int)slot] <= 0)
                    continue;
                if (slot == _activeLoop)
                    continue;

                buffer[written++] = SlotToCause(slot);
            }

            return written;
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnDestroy()
        {
            if (_buffReceiver != null)
            {
                if (_onBuffAdded != null)
                    _buffReceiver.OnBuffAdded -= _onBuffAdded;
                if (_onBuffRemoved != null)
                    _buffReceiver.OnBuffRemoved -= _onBuffRemoved;
            }

            if (_ball != null && _onAllyBurn != null)
                AllyDotSystem.OnBurnStateChanged -= _onAllyBurn;

            if (_shieldSystem != null && _onShieldPresence != null)
                _shieldSystem.OnShieldPresenceChanged -= _onShieldPresence;

            ReleaseLoop();
            RestoreFreezeTint();
        }

        // ═══════════════════════════════════════════
        // HANDLERS
        // ═══════════════════════════════════════════

        private void OnBuffAdded(BuffData b)
        {
            StatusSlot slot = ClassifyBuff(b);
            if (slot == StatusSlot.Count)
                return;
            _counts[(int)slot]++;
            RefreshPresentation();
        }

        private void OnBuffRemoved(BuffData b, BuffRemovalReason reason)
        {
            StatusSlot slot = ClassifyBuff(b);
            if (slot == StatusSlot.Count)
                return;

            int idx = (int)slot;
            if (_counts[idx] > 0)
                _counts[idx]--;
#if UNITY_EDITOR
            else
                Debug.LogWarning($"[UnitStatusFx] Décrément {slot} déjà à 0 sur {name}");
#endif
            RefreshPresentation();
        }

        private void OnAllyBurnChanged(CharacterBall target, bool active)
        {
            if (!ReferenceEquals(target, _ball))
                return;

            int idx = (int)StatusSlot.Burn;
            if (active)
                _counts[idx] = Mathf.Max(_counts[idx], 1);
            else
                _counts[idx] = 0;

            RefreshPresentation();
        }

        private void OnShieldPresenceChanged(bool present)
        {
            _counts[(int)StatusSlot.Shield] = present ? 1 : 0;
            RefreshPresentation();
        }

        // ═══════════════════════════════════════════
        // SYNC / CLASSIFY
        // ═══════════════════════════════════════════

        private void SyncInitial()
        {
            for (int i = 0; i < _counts.Length; i++)
                _counts[i] = 0;

            if (_buffReceiver != null && _buffReceiver.ActiveBuffs != null)
            {
                var buffs = _buffReceiver.ActiveBuffs;
                for (int i = 0; i < buffs.Count; i++)
                {
                    StatusSlot slot = ClassifyBuff(buffs[i]);
                    if (slot != StatusSlot.Count)
                        _counts[(int)slot]++;
                }
            }

            if (_enemy != null)
            {
                if (StunSystem.Instance != null && StunSystem.Instance.IsStunned(_enemy))
                    _counts[(int)StatusSlot.Stun] = Mathf.Max(_counts[(int)StatusSlot.Stun], 1);
                if (FreezeSystem.Instance != null && FreezeSystem.Instance.HasFreezeBuff(_enemy))
                    _counts[(int)StatusSlot.Freeze] = Mathf.Max(_counts[(int)StatusSlot.Freeze], 1);
            }

            if (_ball != null && AllyDotSystem.HasBurn(_ball))
                _counts[(int)StatusSlot.Burn] = Mathf.Max(_counts[(int)StatusSlot.Burn], 1);

            if (_shieldSystem != null && _shieldSystem.HasShieldPresence)
                _counts[(int)StatusSlot.Shield] = 1;
        }

        private static StatusSlot ClassifyBuff(BuffData b)
        {
            if (b == null)
                return StatusSlot.Count;

            BuffFeedbackKind kind = FeedbackCauses.Classify(b);
            switch (kind)
            {
                case BuffFeedbackKind.Buff: return StatusSlot.BuffUp;
                case BuffFeedbackKind.Debuff: return StatusSlot.DebuffDown;
                case BuffFeedbackKind.Shield: return StatusSlot.Shield;
                case BuffFeedbackKind.Burn: return StatusSlot.Burn;
                case BuffFeedbackKind.Poison: return StatusSlot.Poison;
                case BuffFeedbackKind.None:
                    if (b.BuffId == StunSystem.StunBuffId)
                        return StatusSlot.Stun;
                    if (b.BuffId == FreezeSystem.FreezeBuffId)
                        return StatusSlot.Freeze;
                    return StatusSlot.Count;
                default:
                    return StatusSlot.Count;
            }
        }

        private static FeedbackCause SlotToCause(StatusSlot slot)
        {
            switch (slot)
            {
                case StatusSlot.Freeze: return FeedbackCause.Freeze;
                case StatusSlot.Stun: return FeedbackCause.Stun;
                case StatusSlot.Burn: return FeedbackCause.Burn;
                case StatusSlot.Poison: return FeedbackCause.Poison;
                case StatusSlot.Shield: return FeedbackCause.Shield;
                case StatusSlot.BuffUp: return FeedbackCause.BuffUp;
                case StatusSlot.DebuffDown: return FeedbackCause.DebuffDown;
                default: return FeedbackCause.None;
            }
        }

        // ═══════════════════════════════════════════
        // PRÉSENTATION
        // ═══════════════════════════════════════════

        private void RefreshPresentation()
        {
            StatusSlot target = PickLoopTarget();
            if (target != _activeLoop)
                SwitchLoop(target);

            // Écrivain unique teinte d'état — vigilance Veuve P3
            bool wantFreeze = _counts[(int)StatusSlot.Freeze] > 0;
            if (wantFreeze && !_freezeTintActive)
                ApplyFreezeTint();
            else if (!wantFreeze && _freezeTintActive)
                RestoreFreezeTint();

            OnPipsChanged?.Invoke();
        }

        private StatusSlot PickLoopTarget()
        {
            // Freeze > Stun > Burn > Poison > Shield
            if (_counts[(int)StatusSlot.Freeze] > 0) return StatusSlot.Freeze;
            if (_counts[(int)StatusSlot.Stun] > 0) return StatusSlot.Stun;
            if (_counts[(int)StatusSlot.Burn] > 0) return StatusSlot.Burn;
            if (_counts[(int)StatusSlot.Poison] > 0) return StatusSlot.Poison;
            if (_counts[(int)StatusSlot.Shield] > 0) return StatusSlot.Shield;
            return StatusSlot.Count;
        }

        private void SwitchLoop(StatusSlot target)
        {
            ReleaseLoop();
            _activeLoop = target;
            if (target == StatusSlot.Count)
                return;

            ParticleSystem prefab = ResolveLoopPrefab(target);
            if (prefab == null)
                return;

            Transform parent = _renderer != null ? _renderer.transform : transform;
            _loopInstance = StatusLoopPool.Shared.Get(prefab, parent);

            // Teinte cause — palette unique (§1.1). Alloc GetComponentsInChildren OK :
            // événementiel (changement d'état), pas hot path.
            FeedbackCause cause = SlotToCause(target);
            Color c = CombatFeedbackPalette.GetColor(cause);
            ParticleSystem[] systems = _loopInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null) continue;
                var main = ps.main;
                Color prev = main.startColor.color;
                c.a = prev.a;
                main.startColor = c;
            }
        }

        private void ReleaseLoop()
        {
            if (_loopInstance != null)
            {
                StatusLoopPool.Shared.Release(_loopInstance);
                _loopInstance = null;
            }
            _activeLoop = StatusSlot.Count;
        }

        private static ParticleSystem ResolveLoopPrefab(StatusSlot slot)
        {
            EnsurePrefabsLoaded();
            switch (slot)
            {
                case StatusSlot.Burn: return _prefabBurn;
                case StatusSlot.Poison: return _prefabPoison;
                case StatusSlot.Shield: return _prefabShield;
                case StatusSlot.Stun: return _prefabStun;
                case StatusSlot.Freeze: return _prefabFreeze;
                default: return null;
            }
        }

        private static void EnsurePrefabsLoaded()
        {
            if (_prefabsResolved)
                return;
            _prefabsResolved = true;

            _prefabBurn = LoadLoop(LoopBurnPath);
            _prefabPoison = LoadLoop(LoopPoisonPath);
            _prefabShield = LoadLoop(LoopShieldPath);
            _prefabStun = LoadLoop(LoopStunPath);
            _prefabFreeze = LoadLoop(LoopFreezePath);

            if ((_prefabBurn == null || _prefabPoison == null || _prefabShield == null
                 || _prefabStun == null || _prefabFreeze == null) && !_warnedMissingLoops)
            {
                _warnedMissingLoops = true;
                Debug.LogWarning(
                    "[UnitStatusFx] Prefabs boucles absents (Resources/VFX/Feedback/Loops/) — " +
                    "pas de boucle, pastilles/teinte OK. Lancer le menu Générer Boucles d'État (P2b).");
            }
        }

        private static ParticleSystem LoadLoop(string path)
        {
            GameObject go = Resources.Load<GameObject>(path);
            return go != null ? go.GetComponent<ParticleSystem>() : null;
        }

        private void ResolveRenderer()
        {
            if (_ball != null && _ball.VisualRenderer != null)
            {
                _renderer = _ball.VisualRenderer;
                return;
            }

            _renderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void ApplyFreezeTint()
        {
            if (_renderer == null)
                return;

            if (!_colorCaptured)
            {
                _capturedColor = _renderer.color;
                _colorCaptured = true;
            }

            _renderer.color = CombatFeedbackPalette.GetColor(FeedbackCause.Freeze);
            _freezeTintActive = true;
        }

        private void RestoreFreezeTint()
        {
            if (_renderer != null && _colorCaptured)
                _renderer.color = _capturedColor;

            _freezeTintActive = false;
            _colorCaptured = false;
        }
    }
}
