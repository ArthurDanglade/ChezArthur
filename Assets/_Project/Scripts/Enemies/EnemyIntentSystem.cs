using System.Collections.Generic;
using ChezArthur.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Télégraphe d'intention ennemi (R6) — singleton scène, événementiel, visuels poolés.
    /// Intention visible dès que l'ennemi est le prochain (courant inclus), y compris
    /// sur deux tours alliés consécutifs. Aucun contact avec GroundZone : l'intensification
    /// passe exclusivement par IEnemyIntentProvider.OnTelegraphStateChanged.
    /// </summary>
    public class EnemyIntentSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        /// <summary> Rouge ennemi semi-transparent — lisible sur fond arène sans crier. </summary>
        private static readonly Color TELEGRAPH_COLOR = new Color(0.92f, 0.28f, 0.22f, 0.78f);

        private const float LINE_WIDTH = 0.06f;
        private const float ICON_OFFSET_Y = 0.95f;
        private const float ICON_FONT_SIZE = 5.5f;
        private const float ICON_PULSE_AMP = 0.1f;
        private const float ICON_PULSE_SPEED = 3.5f;
        private const float RING_WORLD_SIZE = 1.15f;
        private const float RING_ALPHA_BASE = 0.55f;
        private const float RING_ALPHA_PULSE = 0.35f;
        private const float RING_PULSE_SPEED = 3.2f;
        private const int PEEK_COUNT = 8;

        // Tri : zones P1 à -20 ; anneau au-dessus des zones, sous ombres/persos (8/10) ;
        // ligne + icône au-dessus des balles pour lecture claire du télégraphe.
        private const int RING_SORTING_ORDER = -10;
        private const int LINE_SORTING_ORDER = 12;
        private const int ICON_SORTING_ORDER = 13;

        // ═══════════════════════════════════════════
        // SINGLETON / AUTO-BOOTSTRAP
        // ═══════════════════════════════════════════

        public static EnemyIntentSystem Instance { get; private set; }

        private static bool _sceneHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!_sceneHooked)
            {
                _sceneHooked = true;
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            TrySpawnForActiveScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TrySpawnForActiveScene();
        }

        private static void TrySpawnForActiveScene()
        {
            if (Instance != null)
                return;

            // Vit seul : aucun contenu G6 ne l'appelle encore — spawn si combat présent.
            TurnManager tm = Object.FindObjectOfType<TurnManager>();
            if (tm == null)
                return;

            var go = new GameObject("EnemyIntentSystem");
            go.AddComponent<EnemyIntentSystem>();
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private readonly Dictionary<Enemy, IEnemyIntentProvider> _providers =
            new Dictionary<Enemy, IEnemyIntentProvider>(8);
        private readonly Dictionary<Enemy, EnemyAI> _aiCache = new Dictionary<Enemy, EnemyAI>(8);
        private readonly List<Enemy> _purgeKeys = new List<Enemy>(8);
        private readonly List<ITurnParticipant> _peekBuffer = new List<ITurnParticipant>(8);

        private TurnManager _turnManager;
        private Enemy _telegraphedEnemy;
        private IEnemyIntentProvider _telegraphedProvider;
        private CharacterBall _currentTarget;
        private EnemyIntentKind _currentKind;
        private bool _visualsActive;
        private bool _lineVisible;
        private bool _iconVisible;
        private bool _ringVisible;
        private bool _subscribedStatic;

        private LineRenderer _line;
        private TextMeshPro _icon;
        private SpriteRenderer _ring;
        private Material _lineMaterial;
        private Texture2D _dashTexture;
        private Sprite _ringSprite;

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
            EnsureVisuals();
            EnsureTurnManagerSubscription();
            EnsureStaticSubscription();
            Refresh();
        }

        private void OnDestroy()
        {
            UnsubscribeTurnManager();
            UnsubscribeStatic();
            // Notifie le provider pour éteindre ses zones avant destruction scène.
            HideAll(notifyProvider: true);

            if (_lineMaterial != null)
                Destroy(_lineMaterial);
            if (_dashTexture != null)
                Destroy(_dashTexture);
            if (_ringSprite != null)
            {
                if (_ringSprite.texture != null)
                    Destroy(_ringSprite.texture);
                Destroy(_ringSprite);
            }

            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            if (!_visualsActive)
                return;

            float t = Time.time;

            if (_lineVisible && _line != null && _telegraphedEnemy != null && _currentTarget != null)
            {
                Vector3 from = _telegraphedEnemy.transform.position;
                Vector3 to = _currentTarget.transform.position;
                from.z = 0f;
                to.z = 0f;
                _line.SetPosition(0, from);
                _line.SetPosition(1, to);
            }

            if (_iconVisible && _icon != null && _telegraphedEnemy != null)
            {
                Vector3 p = _telegraphedEnemy.transform.position;
                p.y += ICON_OFFSET_Y;
                p.z = 0f;
                _icon.transform.position = p;
                float s = 1f + ICON_PULSE_AMP * Mathf.Sin(t * ICON_PULSE_SPEED);
                _icon.transform.localScale = new Vector3(s, s, 1f);
            }

            if (_ringVisible && _ring != null && _currentTarget != null)
            {
                Vector3 p = _currentTarget.transform.position;
                p.z = 0f;
                _ring.transform.position = p;
                Color c = TELEGRAPH_COLOR;
                c.a = RING_ALPHA_BASE + RING_ALPHA_PULSE * (0.5f + 0.5f * Mathf.Sin(t * RING_PULSE_SPEED));
                _ring.color = c;
            }
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE (gelée pour G6)
        // ═══════════════════════════════════════════

        public static void RegisterProvider(Enemy enemy, IEnemyIntentProvider provider)
        {
            if (Instance == null || enemy == null || provider == null)
                return;

            Instance._providers[enemy] = provider;
            Instance.Refresh();
        }

        public static void UnregisterProvider(Enemy enemy)
        {
            if (Instance == null || enemy == null)
                return;

            Instance._providers.Remove(enemy);
            if (ReferenceEquals(Instance._telegraphedEnemy, enemy))
                Instance.Refresh();
        }

        /// <summary>
        /// Rafraîchissement à la demande (ex. bascule d'archétype hors event standard).
        /// Filet pour tout futur cas non couvert par les events TurnManager /
        /// OnAnySpecSwitched — ex. SetArchetype sans mort associée.
        /// </summary>
        public static void RequestRefresh()
        {
            Instance?.Refresh();
        }

        // ═══════════════════════════════════════════
        // REFRESH ÉVÉNEMENTIEL
        // ═══════════════════════════════════════════

        private void Refresh()
        {
            PurgeStaleEntries();

            if (_turnManager == null)
                EnsureTurnManagerSubscription();

            if (_turnManager == null || !_turnManager.HasCurrentParticipant)
            {
                ClearTelegraph();
                return;
            }

            Enemy nextEnemy = FindNextEnemy();
            if (nextEnemy == null)
            {
                ClearTelegraph();
                return;
            }

            ApplyTelegraphEnemy(nextEnemy);
            ResolveAndShow(nextEnemy);
        }

        private Enemy FindNextEnemy()
        {
            int written = _turnManager.PeekUpcoming(PEEK_COUNT, _peekBuffer, out _);
            for (int i = 0; i < written; i++)
            {
                ITurnParticipant p = _peekBuffer[i];
                if (p == null || p.IsDead || p.IsAlly)
                    continue;

                return p as Enemy;
            }

            return null;
        }

        private void ApplyTelegraphEnemy(Enemy nextEnemy)
        {
            IEnemyIntentProvider newProvider = null;
            if (nextEnemy != null && _providers.TryGetValue(nextEnemy, out IEnemyIntentProvider provider))
                newProvider = provider;

            // Même ennemi : le provider peut s'être enregistré / changé après le premier telegraph.
            if (ReferenceEquals(_telegraphedEnemy, nextEnemy))
            {
                if (ReferenceEquals(_telegraphedProvider, newProvider))
                    return;

                if (_telegraphedProvider != null)
                    _telegraphedProvider.OnTelegraphStateChanged(false);

                _telegraphedProvider = newProvider;
                if (_telegraphedProvider != null)
                    _telegraphedProvider.OnTelegraphStateChanged(true);
                return;
            }

            if (_telegraphedProvider != null)
                _telegraphedProvider.OnTelegraphStateChanged(false);

            _telegraphedEnemy = nextEnemy;
            _telegraphedProvider = newProvider;

            if (_telegraphedProvider != null)
                _telegraphedProvider.OnTelegraphStateChanged(true);
        }

        private void ResolveAndShow(Enemy enemy)
        {
            EnemyIntent intent = default;
            bool hasIntent = false;

            if (_providers.TryGetValue(enemy, out IEnemyIntentProvider provider) && provider != null)
            {
                hasIntent = provider.TryGetIntent(out intent);
            }
            else if (enemy.Archetype == EnemyArchetype.Mobile)
            {
                // Intention générique intégrée (Mobile sans provider G6).
                EnemyAI ai = GetCachedAI(enemy);
                intent.Kind = EnemyIntentKind.Charge;
                intent.Target = ai != null ? ai.ResolveCurrentTarget() : null;
                intent.IconText = "»";
                hasIntent = true;
            }
            else
            {
                // Fixe sans provider (état G3) : AUCUN visuel.
                // Un Fixe est muet tant que son pattern n'existe pas — un télégraphe ne ment jamais.
                hasIntent = false;
            }

            if (!hasIntent || intent.Kind == EnemyIntentKind.None)
            {
                HideVisualsOnly();
                return;
            }

            ShowIntent(enemy, intent);
        }

        private void ClearTelegraph()
        {
            if (_telegraphedProvider != null)
                _telegraphedProvider.OnTelegraphStateChanged(false);

            _telegraphedEnemy = null;
            _telegraphedProvider = null;
            HideVisualsOnly();
        }

        private void HideAll(bool notifyProvider)
        {
            if (notifyProvider && _telegraphedProvider != null)
                _telegraphedProvider.OnTelegraphStateChanged(false);

            _telegraphedEnemy = null;
            _telegraphedProvider = null;
            HideVisualsOnly();
        }

        // ═══════════════════════════════════════════
        // VISUELS
        // ═══════════════════════════════════════════

        private void ShowIntent(Enemy enemy, EnemyIntent intent)
        {
            _currentKind = intent.Kind;
            _currentTarget = intent.Target != null && !intent.Target.IsDead ? intent.Target : null;

            bool showLine = intent.Kind != EnemyIntentKind.Zone && _currentTarget != null;
            bool showIcon = !string.IsNullOrEmpty(intent.IconText);
            bool showRing = _currentTarget != null;

            if (_line != null)
            {
                _line.enabled = showLine;
                if (showLine)
                {
                    Vector3 from = enemy.transform.position;
                    Vector3 to = _currentTarget.transform.position;
                    from.z = 0f;
                    to.z = 0f;
                    _line.SetPosition(0, from);
                    _line.SetPosition(1, to);
                    _line.startColor = TELEGRAPH_COLOR;
                    _line.endColor = TELEGRAPH_COLOR;
                }
            }

            if (_icon != null)
            {
                if (showIcon)
                {
                    _icon.gameObject.SetActive(true);
                    _icon.text = intent.IconText;
                    _icon.color = TELEGRAPH_COLOR;
                    Vector3 p = enemy.transform.position;
                    p.y += ICON_OFFSET_Y;
                    p.z = 0f;
                    _icon.transform.position = p;
                }
                else
                {
                    _icon.gameObject.SetActive(false);
                }
            }

            if (_ring != null)
            {
                if (showRing)
                {
                    _ring.gameObject.SetActive(true);
                    Vector3 p = _currentTarget.transform.position;
                    p.z = 0f;
                    _ring.transform.position = p;
                    Color c = TELEGRAPH_COLOR;
                    c.a = RING_ALPHA_BASE;
                    _ring.color = c;
                }
                else
                {
                    _ring.gameObject.SetActive(false);
                }
            }

            _lineVisible = showLine;
            _iconVisible = showIcon;
            _ringVisible = showRing;
            _visualsActive = showLine || showIcon || showRing;
        }

        private void HideVisualsOnly()
        {
            _currentTarget = null;
            _currentKind = EnemyIntentKind.None;
            _lineVisible = false;
            _iconVisible = false;
            _ringVisible = false;
            _visualsActive = false;

            if (_line != null)
                _line.enabled = false;
            if (_icon != null)
                _icon.gameObject.SetActive(false);
            if (_ring != null)
                _ring.gameObject.SetActive(false);
        }

        private void EnsureVisuals()
        {
            if (_line != null)
                return;

            EnsureRingSprite();
            EnsureLineMaterial();

            // Ligne d'aggro pointillée
            var lineGo = new GameObject("IntentLine");
            lineGo.transform.SetParent(transform, false);
            _line = lineGo.AddComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.startWidth = LINE_WIDTH;
            _line.endWidth = LINE_WIDTH;
            _line.numCapVertices = 0;
            _line.numCornerVertices = 0;
            _line.textureMode = LineTextureMode.Tile;
            _line.sharedMaterial = _lineMaterial;
            _line.sortingOrder = LINE_SORTING_ORDER;
            _line.enabled = false;

            // Icône d'action (TMP world-space placeholder)
            var iconGo = new GameObject("IntentIcon");
            iconGo.transform.SetParent(transform, false);
            _icon = iconGo.AddComponent<TextMeshPro>();
            _icon.alignment = TextAlignmentOptions.Center;
            _icon.fontSize = ICON_FONT_SIZE;
            _icon.color = TELEGRAPH_COLOR;
            _icon.enableWordWrapping = false;
            _icon.overflowMode = TextOverflowModes.Overflow;
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            if (iconRect != null)
                iconRect.sizeDelta = new Vector2(2f, 1.2f);
            MeshRenderer iconRenderer = iconGo.GetComponent<MeshRenderer>();
            if (iconRenderer != null)
                iconRenderer.sortingOrder = ICON_SORTING_ORDER;
            iconGo.SetActive(false);

            // Anneau de cible
            var ringGo = new GameObject("IntentRing");
            ringGo.transform.SetParent(transform, false);
            _ring = ringGo.AddComponent<SpriteRenderer>();
            _ring.sprite = _ringSprite;
            _ring.color = TELEGRAPH_COLOR;
            _ring.sortingOrder = RING_SORTING_ORDER;
            float scale = RING_WORLD_SIZE;
            ringGo.transform.localScale = new Vector3(scale, scale, 1f);
            ringGo.SetActive(false);
        }

        private void EnsureLineMaterial()
        {
            if (_lineMaterial != null)
                return;

            _dashTexture = new Texture2D(8, 1, TextureFormat.RGBA32, false);
            _dashTexture.filterMode = FilterMode.Point;
            _dashTexture.wrapMode = TextureWrapMode.Repeat;
            for (int x = 0; x < 8; x++)
                _dashTexture.SetPixel(x, 0, x < 4 ? Color.white : Color.clear);
            _dashTexture.Apply(false, true);

            Shader shader = Shader.Find("Sprites/Default");
            _lineMaterial = new Material(shader);
            _lineMaterial.mainTexture = _dashTexture;
            // Tile densifié : ~2 unités monde par motif → lisible sans clutter.
            _lineMaterial.mainTextureScale = new Vector2(0.5f, 1f);
        }

        private void EnsureRingSprite()
        {
            if (_ringSprite != null)
                return;

            const int size = 96;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float outer = size * 0.48f;
            float inner = size * 0.38f;
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
            _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // ═══════════════════════════════════════════
        // CACHE / PURGE
        // ═══════════════════════════════════════════

        private EnemyAI GetCachedAI(Enemy enemy)
        {
            if (enemy == null)
                return null;

            if (_aiCache.TryGetValue(enemy, out EnemyAI ai) && ai != null)
                return ai;

            ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
                _aiCache[enemy] = ai;
            return ai;
        }

        private void PurgeStaleEntries()
        {
            _purgeKeys.Clear();
            foreach (KeyValuePair<Enemy, IEnemyIntentProvider> kv in _providers)
            {
                if (kv.Key == null || kv.Key.IsDead || kv.Value == null)
                    _purgeKeys.Add(kv.Key);
            }

            for (int i = 0; i < _purgeKeys.Count; i++)
                _providers.Remove(_purgeKeys[i]);

            _purgeKeys.Clear();
            foreach (KeyValuePair<Enemy, EnemyAI> kv in _aiCache)
            {
                if (kv.Key == null || kv.Key.IsDead)
                    _purgeKeys.Add(kv.Key);
            }

            for (int i = 0; i < _purgeKeys.Count; i++)
                _aiCache.Remove(_purgeKeys[i]);
        }

        // ═══════════════════════════════════════════
        // ABONNEMENTS
        // ═══════════════════════════════════════════

        private void EnsureTurnManagerSubscription()
        {
            if (_turnManager == null)
                _turnManager = Object.FindObjectOfType<TurnManager>();

            if (_turnManager == null)
                return;

            _turnManager.OnTurnChanged -= OnTurnSignal;
            _turnManager.OnTurnChanged += OnTurnSignal;
            _turnManager.OnParticipantTurnEnded -= OnTurnSignal;
            _turnManager.OnParticipantTurnEnded += OnTurnSignal;
            _turnManager.OnParticipantDeath -= OnParticipantDeath;
            _turnManager.OnParticipantDeath += OnParticipantDeath;
            _turnManager.OnCycleStarted -= OnCycleStarted;
            _turnManager.OnCycleStarted += OnCycleStarted;
            _turnManager.OnEnemyAddedMidCombat -= OnEnemyAddedMidCombat;
            _turnManager.OnEnemyAddedMidCombat += OnEnemyAddedMidCombat;
        }

        private void UnsubscribeTurnManager()
        {
            if (_turnManager == null)
                return;

            _turnManager.OnTurnChanged -= OnTurnSignal;
            _turnManager.OnParticipantTurnEnded -= OnTurnSignal;
            _turnManager.OnParticipantDeath -= OnParticipantDeath;
            _turnManager.OnCycleStarted -= OnCycleStarted;
            _turnManager.OnEnemyAddedMidCombat -= OnEnemyAddedMidCombat;
            _turnManager = null;
        }

        private void EnsureStaticSubscription()
        {
            if (_subscribedStatic)
                return;

            CharacterBall.OnAnySpecSwitchedInCombat += OnAnySpecSwitchedInCombat;
            _subscribedStatic = true;
        }

        private void UnsubscribeStatic()
        {
            if (!_subscribedStatic)
                return;

            CharacterBall.OnAnySpecSwitchedInCombat -= OnAnySpecSwitchedInCombat;
            _subscribedStatic = false;
        }

        private void OnTurnSignal(ITurnParticipant _)
        {
            Refresh();
        }

        private void OnParticipantDeath(ITurnParticipant _)
        {
            // Couvre aussi Alucadra : mort de l'Épée → rebuild file → nouvel ennemi prochain.
            Refresh();
        }

        private void OnCycleStarted()
        {
            if (_turnManager == null)
                EnsureTurnManagerSubscription();
            Refresh();
        }

        private void OnEnemyAddedMidCombat(Enemy _)
        {
            Refresh();
        }

        private void OnAnySpecSwitchedInCombat(CharacterBall _)
        {
            // Cible Mobile peut changer (sélecteurs G4) — état vivant.
            Refresh();
        }

#if UNITY_EDITOR
        // ═══════════════════════════════════════════
        // OUTILLAGE DEV
        // ═══════════════════════════════════════════

        [ContextMenu("DEV — Log intention courante")]
        private void DevLogCurrentIntent()
        {
            Refresh();
            if (_telegraphedEnemy == null)
            {
                Debug.Log("[EnemyIntentSystem] Aucun ennemi télégraphié.");
                return;
            }

            bool hasProvider = _providers.ContainsKey(_telegraphedEnemy);
            string targetName = _currentTarget != null ? _currentTarget.Name : "(null)";
            Debug.Log(
                $"[EnemyIntentSystem] Ennemi={_telegraphedEnemy.name} Kind={_currentKind} " +
                $"Target={targetName} Provider={(hasProvider ? "oui" : "générique Mobile / muet Fixe")} " +
                $"Visuels={_visualsActive}");
        }
#endif
    }
}
