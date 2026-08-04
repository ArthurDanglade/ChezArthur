using System.Collections.Generic;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Feedback;
using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Manager des zones au sol (R7) — singleton paresseux, pool, textures placeholder.
    /// Consommé par G3-P2 (intensification) et handlers G6 (Archère, Eaux Bénites).
    /// </summary>
    public class GroundZoneSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Tri de rendu : au-dessus du sol décor (~0), sous les ombres/persos (8/10).
        /// -20 était SOUS le damier d'arène → zones invisibles en jeu (hotfix G6c-P2).
        /// </summary>
        public const int ZoneSortingOrder = 6;

        private const int POOL_PREWARM = 4;

        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════

        /// <summary> Nullable — null hors scène de jeu / avant premier usage. </summary>
        public static GroundZoneSystem Instance { get; private set; }

        private static GroundZoneSystem EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            // Scène courante uniquement — PAS de DontDestroyOnLoad (pattern AllyDotSystem).
            var go = new GameObject("GroundZoneSystem");
            return go.AddComponent<GroundZoneSystem>();
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<GroundZone> _pool = new List<GroundZone>(8);
        private readonly List<GroundZone> _active = new List<GroundZone>(8);
        private bool _subscribedRun;
        private GameObject _zonePrototype;

        private static Sprite _ringSprite;
        private static Sprite _softDiscSprite;
        private static Sprite _hollowRectSprite;
        private static Sprite _tileSprite;
        private static bool _texturesReady;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureTextures();
            PrewarmPool();
            EnsureRunManagerSubscription();
        }

        private void OnDestroy()
        {
            UnsubscribeRunManager();
            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _active.Count; i++)
            {
                GroundZone zone = _active[i];
                if (zone != null && zone.isActiveAndEnabled)
                    zone.TickVisual(dt);
            }
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Crée (ou recycle) une zone. size : cercle → x = rayon (y ignoré) ; rectangle → largeur × hauteur.
        /// </summary>
        public static GroundZone CreateZone(
            Enemy owner,
            ZoneKind kind,
            ZoneShape shape,
            Vector2 size,
            Vector2 worldPosition,
            Color tint)
        {
            GroundZoneSystem sys = EnsureInstance();
            return sys.CreateZoneInternal(owner, kind, shape, size, worldPosition, tint);
        }

        public static void ReleaseZone(GroundZone zone)
        {
            if (zone == null)
                return;
            if (Instance == null)
            {
                if (zone.gameObject != null)
                    Object.Destroy(zone.gameObject);
                return;
            }

            Instance.ReleaseZoneInternal(zone);
        }

        /// <summary> Une ligne dans les Cleanup de handlers. </summary>
        public static void ReleaseAllForOwner(Enemy owner)
        {
            if (Instance == null || owner == null)
                return;
            Instance.ReleaseAllForOwnerInternal(owner);
        }

        /// <summary> Balayage fin d'étage. </summary>
        public static void ReleaseAll()
        {
            if (Instance == null)
                return;
            Instance.ReleaseAllInternal();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — pool
        // ═══════════════════════════════════════════

        private void PrewarmPool()
        {
            for (int i = _pool.Count; i < POOL_PREWARM; i++)
                _pool.Add(CreatePooledZone());
        }

        private GroundZone CreatePooledZone()
        {
            EnsurePrototype();
            // SEUL Instantiate du fichier (croissance du pool).
            GameObject go = Instantiate(_zonePrototype, transform);
            go.name = "GroundZone";
            go.hideFlags = HideFlags.None;
            go.SetActive(false);
            return go.GetComponent<GroundZone>();
        }

        private void EnsurePrototype()
        {
            if (_zonePrototype != null)
                return;

            _zonePrototype = new GameObject("GroundZonePrototype");
            _zonePrototype.transform.SetParent(transform, false);
            _zonePrototype.SetActive(false);
            _zonePrototype.hideFlags = HideFlags.HideAndDontSave;
            GroundZone zone = _zonePrototype.AddComponent<GroundZone>();
            zone.EnsureComponents();
        }

        private GroundZone CreateZoneInternal(
            Enemy owner,
            ZoneKind kind,
            ZoneShape shape,
            Vector2 size,
            Vector2 worldPosition,
            Color tint)
        {
            EnsureTextures();

            GroundZone zone;
            if (_pool.Count > 0)
            {
                int last = _pool.Count - 1;
                zone = _pool[last];
                _pool.RemoveAt(last);
            }
            else
            {
                zone = CreatePooledZone();
            }

            zone.Activate(
                owner,
                kind,
                shape,
                size,
                worldPosition,
                tint,
                _ringSprite,
                _softDiscSprite,
                _hollowRectSprite,
                _tileSprite);
            _active.Add(zone);
            CombatFeedbackService.PlayEvent(
                FeedbackEventId.ZonePlaced,
                FeedbackContext.At(worldPosition));
            return zone;
        }

        private void ReleaseZoneInternal(GroundZone zone)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_active[i], zone))
                    continue;
                _active.RemoveAt(i);
                break;
            }

            zone.DeactivateForPool();
            zone.transform.SetParent(transform, false);
            _pool.Add(zone);
        }

        private void ReleaseAllForOwnerInternal(Enemy owner)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                GroundZone zone = _active[i];
                if (zone == null || !ReferenceEquals(zone.Owner, owner))
                    continue;
                _active.RemoveAt(i);
                zone.DeactivateForPool();
                zone.transform.SetParent(transform, false);
                _pool.Add(zone);
            }
        }

        private void ReleaseAllInternal()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                GroundZone zone = _active[i];
                _active.RemoveAt(i);
                if (zone == null)
                    continue;
                zone.DeactivateForPool();
                zone.transform.SetParent(transform, false);
                _pool.Add(zone);
            }
        }

        // ═══════════════════════════════════════════
        // RUN MANAGER
        // ═══════════════════════════════════════════

        private void EnsureRunManagerSubscription()
        {
            if (_subscribedRun || RunManager.Instance == null)
                return;

            RunManager.Instance.OnStageCompleted += OnStageCompleted;
            _subscribedRun = true;
        }

        private void UnsubscribeRunManager()
        {
            if (!_subscribedRun)
                return;

            if (RunManager.Instance != null)
                RunManager.Instance.OnStageCompleted -= OnStageCompleted;

            _subscribedRun = false;
        }

        private void OnStageCompleted(int _)
        {
            ReleaseAllInternal();
        }

        // ═══════════════════════════════════════════
        // TEXTURES PLACEHOLDER (générées une fois)
        // ═══════════════════════════════════════════

        private static void EnsureTextures()
        {
            if (_texturesReady)
                return;

            _ringSprite = CreateRingSprite(128);
            _softDiscSprite = CreateSoftDiscSprite(64);
            _hollowRectSprite = CreateHollowRectSprite(64, 2);
            _tileSprite = CreateTileSprite(16);
            _texturesReady = true;
        }

        private static Sprite CreateRingSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float outer = size * 0.48f;
            float inner = size * 0.40f;
            float outerSqr = outer * outer;
            float innerSqr = inner * inner;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dSqr = dx * dx + dy * dy;
                    bool on = dSqr <= outerSqr && dSqr >= innerSqr;
                    tex.SetPixel(x, y, on ? Color.white : Color.clear);
                }
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateSoftDiscSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - (d / radius));
                    a = a * a;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateHollowRectSprite(int size, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < border || y < border || x >= size - border || y >= size - border;
                    tex.SetPixel(x, y, edge ? Color.white : Color.clear);
                }
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateTileSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            int half = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool dark = ((x < half) ^ (y < half));
                    float a = dark ? 0.55f : 0.25f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // ═══════════════════════════════════════════
        // OUTILLAGE DEV
        // ═══════════════════════════════════════════

#if UNITY_EDITOR
        [ContextMenu("DEV — Zone persistante test (cercle r=2,5)")]
        private void DevPersistentCircle()
        {
            GroundZone zone = CreateZone(
                null,
                ZoneKind.Persistent,
                ZoneShape.Circle,
                new Vector2(2.5f, 0f),
                new Vector2(0f, 1f),
                new Color(1f, 0.92f, 0.2f, 1f));
            WireDevLogs(zone, "persistante");
            Debug.Log("[GroundZoneSystem] DEV zone persistante cercle r=2.5 @ (0,1)");
        }

        [ContextMenu("DEV — Zone d'impact test (rect 3×2)")]
        private void DevImpactRect()
        {
            GroundZone zone = CreateZone(
                null,
                ZoneKind.Impact,
                ZoneShape.Rectangle,
                new Vector2(3f, 2f),
                new Vector2(0f, 1f),
                new Color(1f, 0.92f, 0.2f, 1f));
            WireDevLogs(zone, "impact");
            Debug.Log("[GroundZoneSystem] DEV zone impact rect 3×2 @ (0,1)");
        }

        private static void WireDevLogs(GroundZone zone, string label)
        {
            if (zone == null)
                return;

            zone.OnAllyEntered += ally =>
            {
                if (ally == null) return;
                Debug.Log($"[GroundZone/{label}] ENTER {ally.Name} IsMoving={ally.IsMoving}");
            };
            zone.OnAllyExited += ally =>
            {
                if (ally == null) return;
                Debug.Log($"[GroundZone/{label}] EXIT {ally.Name} IsMoving={ally.IsMoving}");
            };
        }
#endif
    }
}
