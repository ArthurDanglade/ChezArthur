using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelBattleText;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche les nombres de combat via Pixel Battle Text (dégâts, crit, soin, KO…).
    /// Anti-chevauchement : fan-out des positions normalisées canvas.
    /// Conserve l'API historique FloatingNumberSpawner pour les call sites existants.
    /// </summary>
    public class FloatingNumberSpawner : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float OCCUPANCY_LIFETIME = 0.85f;
        private const float MIN_SEPARATION = 0.1f;
        private const int MAX_OFFSET_ATTEMPTS = 10;
        private const string CritLabel = "CRIT !";
        private const string KoLabel = "KO";
        private const int MIN_COMBAT_TEXT_SIZE = 48;
        private const int MAX_COMBAT_TEXT_SIZE = 84;

        // F5-L1 — labels d'état (budget séparé des chiffres).
        private const int MAX_ACTIVE_STATE_LABELS = 3;
        private const float STATE_LABEL_DISPLAY_S = 0.8f;
        private const float STATE_LABEL_DEDUP_S = 0.6f;
        private const float STATE_LABEL_LANE_PURGE_S = 5f;
        // Tuning L3 possible — « Étourdissement » lit trop large à scale 1.
        private const int STATE_LABEL_LONG_CHARS = 10;
        private const float STATE_LABEL_LONG_SCALE = 0.9f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Pixel Battle Text")]
        [SerializeField] private RectTransform battleTextCanvas;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private bool preferPixelBattleText = true;

        [Header("Animations (presets pack)")]
        [SerializeField] private TextAnimation damageAnim;
        [SerializeField] private TextAnimation critNumberAnim;
        [SerializeField] private TextAnimation critLabelAnim;
        [SerializeField] private TextAnimation healAnim;
        [SerializeField] private TextAnimation koAnim;
        [SerializeField] private TextAnimation allyDamageAnim;
        [SerializeField] private TextAnimation burnAnim;
        [SerializeField] private TextAnimation poisonAnim;
        [SerializeField] private TextAnimation labelAnim;

        [Header("Lisibilité combat (grossit / ralentit les presets pack)")]
        [Tooltip("Multiplicateur de taille TMP (16 pack → ~64).")]
        [SerializeField] private float textSizeMul = 4f;
        [Tooltip("Multiplicateur de durée d'anim + délai lettres.")]
        [SerializeField] private float durationMul = 1.85f;
        [Tooltip("Écarte les lettres (espacement canvas).")]
        [SerializeField] private float spacingMul = 3.2f;
        [Tooltip("Amplifie le mouvement — garder modéré pour éviter les traits étirés.")]
        [SerializeField] private float motionMul = 1.6f;

        [Header("Clarté (anti-spam)")]
        [Tooltip("Ignore les micro-dégâts (rebonds / ticks 1 PV) qui polluent l'écran.")]
        [SerializeField] private int minDamageToShow = 5;
        [Tooltip("Ignore les micro-soins (+1/+2) trop fréquents.")]
        [SerializeField] private int minHealToShow = 8;
        [Tooltip("Sur un coup fatal : seulement KO, pas le chiffre en plus.")]
        [SerializeField] private bool skipDamagePopupOnKill = true;
        [Tooltip("Plafond de popups simultanés (au-delà = on ignore les non-crit / non-KO).")]
        [SerializeField] private int maxSimultaneousPopups = 5;
        [Tooltip("Désactiver le vieux FloatingNumber monde (source de petits chiffres parasites).")]
        [SerializeField] private bool useLegacyFallback = false;

        [Header("Placement")]
        [SerializeField] private float worldOffsetY = 0.65f;
        [SerializeField] private float critLabelExtraY = 0.09f;
        [SerializeField] private float koExtraY = 0.1f;
        [SerializeField] private float baseJitterX = 0.012f;
        [SerializeField] private float baseJitterY = 0.01f;
        [Tooltip("Offset Y monde des labels d'état (au-dessus de la barre PV).")]
        [SerializeField] private float stateLabelOffsetY = 1.1f;

        [Header("Fallback legacy (si Pixel Battle Text indispo)")]
        [SerializeField] private GameObject floatingNumberPrefab;
        [SerializeField] private float spawnOffsetY = 0.5f;
        [SerializeField] private float randomOffsetX = 0.4f;
        [SerializeField] private Color _enemyDamageColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color _critColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private float _critScaleMul = 1.5f;
        [SerializeField] private int _damageForMinScale = 20;
        [SerializeField] private int _damageForMaxScale = 300;
        [SerializeField] private float _minMagnitudeScale = 0.9f;
        [SerializeField] private float _maxMagnitudeScale = 1.4f;
        [SerializeField] private Color colorDamageAlly = new Color(1f, 0.6f, 0.2f);
        [SerializeField] private Color colorHeal = new Color(0.3f, 1f, 0.4f);
        [SerializeField] private Color colorPoison = new Color(0.5f, 0.9f, 0.2f);
        [SerializeField] private Color colorBurn = new Color(1f, 0.5f, 0f);

        // ═══════════════════════════════════════════
        // TYPES
        // ═══════════════════════════════════════════
        private struct OccupiedSlot
        {
            public Vector2 Normalized;
            public float ExpireAt;
        }

        /// <summary> Lane par unité : 1 label affiché + 1 en file (F5-L1). </summary>
        private class StateLabelLane
        {
            public bool HasActive;
            public string ActiveText;
            public float LastShownUnscaledTime;
            public bool HasQueued;
            public string QueuedText;
            public Color QueuedColor;
            public Vector3 QueuedPos;
            public float LastActivityUnscaledTime;
            public Coroutine Timer;
        }

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static FloatingNumberSpawner Instance { get; private set; }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<OccupiedSlot> _occupied = new List<OccupiedSlot>(32);
        /// <summary> Occupation visuelle labels d'état — séparée du budget chiffres. </summary>
        private readonly List<OccupiedSlot> _stateOccupied = new List<OccupiedSlot>(8);
        private TurnManager _turnManager;

        private readonly Dictionary<CharacterBall, Action<int>> _allyDamagedHandlers =
            new Dictionary<CharacterBall, Action<int>>(8);
        private readonly Dictionary<CharacterBall, Action<int>> _allyHealedHandlers =
            new Dictionary<CharacterBall, Action<int>>(8);
        private readonly Dictionary<Enemy, Action<int>> _enemyDamagedHandlers =
            new Dictionary<Enemy, Action<int>>(16);
        private readonly Dictionary<Enemy, Action> _enemyDeathHandlers =
            new Dictionary<Enemy, Action>(16);
        private readonly Dictionary<TextAnimation, TextAnimation> _scaledAnimCache =
            new Dictionary<TextAnimation, TextAnimation>(16);
        /// <summary>
        /// Clones teintés runtime — borné par la palette (~12). Presets assets jamais modifiés.
        /// </summary>
        private Dictionary<(TextAnimation, Color32), TextAnimation> _tintedAnimCache;

        private readonly Dictionary<int, StateLabelLane> _stateLanes =
            new Dictionary<int, StateLabelLane>(16);
        private int _activeStateLabelCount;

        // Dedup-frame chiffres (puits ShowDamage*) — Hook mort côté prefab, Bind* est vivant.
        private int _damageDedupUnitId;
        private int _damageDedupAmount = int.MinValue;
        private int _damageDedupFrame = -1;

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

            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        private void OnDestroy()
        {
            UnbindCombatEvents();
            ClearScaledAnimCache();
            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // BINDING COMBAT
        // ═══════════════════════════════════════════

        /// <summary>
        /// Abonne dégâts / soins / KO sur l'équipe et les ennemis du TurnManager.
        /// </summary>
        public void Initialize(TurnManager turnManager)
        {
            UnbindCombatEvents();
            _turnManager = turnManager;
            if (_turnManager == null)
                return;

            _turnManager.OnEnemyAddedMidCombat += HandleEnemyAddedMidCombat;
            BindAllParticipants();
        }

        /// <summary> Coupe les abonnements (fin de run / cleanup). </summary>
        public void Cleanup()
        {
            UnbindCombatEvents();
            _turnManager = null;
            _occupied.Clear();
            ClearStateLabelLanes();
        }

        /// <summary>
        /// Purge visuelle immédiate : popups Pixel Battle Text + legacy.
        /// </summary>
        /// <param name="unbindEvents">
        /// True = coupe aussi les abonnements (fin de run).
        /// False = garde les hooks pour l'étage suivant (bonus / gare / transition).
        /// </param>
        public void ClearAllVisuals(bool unbindEvents = true)
        {
            if (unbindEvents)
                Cleanup();
            else
            {
                _occupied.Clear();
                ClearStateLabelLanes();
            }

            PixelBattleTextController.ClearAllActive();

            FloatingNumber[] legacy = UnityEngine.Object.FindObjectsOfType<FloatingNumber>(true);
            for (int i = 0; i < legacy.Length; i++)
            {
                if (legacy[i] != null)
                    Destroy(legacy[i].gameObject);
            }

            ResetBattleTextCanvasAlpha();
        }

        /// <summary>
        /// Fade doux du canvas de combat text, puis purge (inter-étage / bonus / gare).
        /// </summary>
        public System.Collections.IEnumerator FadeOutAndClearVisuals(float duration = 0.28f)
        {
            CanvasGroup cg = GetOrCreateBattleTextCanvasGroup();
            if (cg == null)
            {
                ClearAllVisuals(unbindEvents: false);
                yield break;
            }

            float start = cg.alpha;
            float elapsed = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.Lerp(start, 0f, t);
                yield return null;
            }

            cg.alpha = 0f;
            ClearAllVisuals(unbindEvents: false);
            cg.alpha = 1f;
        }

        private CanvasGroup GetOrCreateBattleTextCanvasGroup()
        {
            RectTransform canvasRt = battleTextCanvas;
            if (canvasRt == null && PixelBattleTextController.singleton != null)
                canvasRt = PixelBattleTextController.singleton.canvas;
            if (canvasRt == null)
                return null;

            CanvasGroup cg = canvasRt.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = canvasRt.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }

        private void ResetBattleTextCanvasAlpha()
        {
            CanvasGroup cg = GetOrCreateBattleTextCanvasGroup();
            if (cg != null)
                cg.alpha = 1f;
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary> Dégâts ennemi ; crit = chiffre + label CRIT. </summary>
        public void ShowDamageEnemy(int amount, Vector3 worldPos, bool isCrit = false)
        {
            ShowDamageEnemy(amount, worldPos, isCrit, skipFrameDedup: false);
        }

        private void ShowDamageEnemy(int amount, Vector3 worldPos, bool isCrit, bool skipFrameDedup)
        {
            if (amount <= 0)
                return;

            if (!isCrit && amount < minDamageToShow)
                return;

            if (!skipFrameDedup && IsDuplicateDamagePopup(HashWorldUnit(worldPos), amount))
                return;

            if (!CanSpawnPopup(priority: isCrit))
                return;

            Vector2 basePos = ResolveFreePosition(WorldToNormalized(worldPos + Vector3.up * worldOffsetY));

            if (TryDisplayPixel(amount.ToString(), isCrit ? critNumberAnim : damageAnim, basePos))
            {
                if (isCrit && critLabelAnim != null && CanSpawnPopup(priority: true))
                {
                    Vector2 critPos = ResolveFreePosition(basePos + new Vector2(0f, critLabelExtraY));
                    TryDisplayPixel(CritLabel, critLabelAnim, critPos);
                }

                return;
            }

            if (useLegacyFallback)
                FallbackDamageEnemy(amount, worldPos, isCrit);
        }

        /// <summary> Dégâts subis par un allié. </summary>
        public void ShowDamageAlly(int amount, Vector3 worldPos)
        {
            ShowDamageAlly(amount, worldPos, skipFrameDedup: false);
        }

        private void ShowDamageAlly(int amount, Vector3 worldPos, bool skipFrameDedup)
        {
            if (amount <= 0 || amount < minDamageToShow)
                return;

            if (!skipFrameDedup && IsDuplicateDamagePopup(HashWorldUnit(worldPos), amount))
                return;

            if (!CanSpawnPopup(priority: false))
                return;

            Vector2 pos = ResolveFreePosition(WorldToNormalized(worldPos + Vector3.up * worldOffsetY));
            TextAnimation anim = allyDamageAnim != null ? allyDamageAnim : damageAnim;
            if (TryDisplayPixel(amount.ToString(), anim, pos))
                return;

            if (useLegacyFallback)
                FallbackSpawn(amount.ToString(), colorDamageAlly, worldPos, 1f, false);
        }

        /// <summary> Soin reçu. </summary>
        public void ShowHeal(int amount, Vector3 worldPos)
        {
            if (amount <= 0 || amount < minHealToShow)
                return;

            if (!CanSpawnPopup(priority: false))
                return;

            Vector2 pos = ResolveFreePosition(WorldToNormalized(worldPos + Vector3.up * worldOffsetY));
            if (TryDisplayPixel("+" + amount, healAnim, pos))
                return;

            if (useLegacyFallback)
                FallbackSpawn("+" + amount, colorHeal, worldPos, 0.85f, false);
        }

        /// <summary> KO au-dessus d'un ennemi vaincu. </summary>
        public void ShowKO(Vector3 worldPos)
        {
            // KO prioritaire : on force un slot même si le plafond est atteint.
            Vector2 pos = ResolveFreePosition(
                WorldToNormalized(worldPos + Vector3.up * (worldOffsetY + koExtraY)));
            if (TryDisplayPixel(KoLabel, koAnim, pos))
                return;

            if (useLegacyFallback)
                FallbackSpawn(KoLabel, _critColor, worldPos + Vector3.up * 0.35f, 1.35f, true);
        }

        /// <summary> Dégâts poison. </summary>
        public void ShowPoison(int amount, Vector3 worldPos)
        {
            if (amount <= 0 || amount < minDamageToShow)
                return;

            if (!CanSpawnPopup(priority: false))
                return;

            Vector2 pos = ResolveFreePosition(WorldToNormalized(worldPos + Vector3.up * worldOffsetY));
            TextAnimation anim = poisonAnim != null ? poisonAnim : damageAnim;
            if (TryDisplayPixel(amount.ToString(), anim, pos))
                return;

            if (useLegacyFallback)
                FallbackSpawn(amount.ToString(), colorPoison, worldPos, 0.8f, false);
        }

        /// <summary> Dégâts brûlure. </summary>
        public void ShowBurn(int amount, Vector3 worldPos)
        {
            if (amount <= 0 || amount < minDamageToShow)
                return;

            if (!CanSpawnPopup(priority: false))
                return;

            Vector2 pos = ResolveFreePosition(WorldToNormalized(worldPos + Vector3.up * worldOffsetY));
            TextAnimation anim = burnAnim != null ? burnAnim : damageAnim;
            if (TryDisplayPixel(amount.ToString(), anim, pos))
                return;

            if (useLegacyFallback)
                FallbackSpawn(amount.ToString(), colorBurn, worldPos, 0.8f, false);
        }

        /// <summary>
        /// Label libre (ex. MÉGACRIT !).
        /// Amendement charte §5.6 — API intacte, bugfix : le paramètre couleur était ignoré
        /// sur le chemin PixelBattleText (labels D12 écrasés en orange crit).
        /// </summary>
        public void ShowLabel(string text, Color color, Vector3 worldPos, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (!CanSpawnPopup(priority: true))
                return;

            Vector2 pos = ResolveFreePosition(WorldToNormalized(worldPos + Vector3.up * worldOffsetY));
            TextAnimation anim = labelAnim != null ? labelAnim : critLabelAnim;
            if (TryDisplayPixel(text, GetTintedAnim(anim, color), pos))
                return;

            if (useLegacyFallback)
                FallbackSpawn(text, color, worldPos, scale, false);
        }

        /// <summary>
        /// Label d'état (F5-L1). Budget dédié (3) — ne consomme jamais les slots chiffres.
        /// Lane 1+1 par unité, affichage 0,8 s unscaled, dedup texte 0,6 s.
        /// </summary>
        public void ShowStateLabel(int unitId, string text, Color color, Vector3 unitPos)
        {
            if (string.IsNullOrEmpty(text))
                return;

            PruneInactiveStateLanes();

            float now = Time.unscaledTime;
            if (!_stateLanes.TryGetValue(unitId, out StateLabelLane lane))
            {
                lane = new StateLabelLane();
                _stateLanes[unitId] = lane;
            }

            lane.LastActivityUnscaledTime = now;

            // Dedup : même texte + même unité < 0,6 s.
            if (lane.HasActive
                && string.Equals(lane.ActiveText, text, StringComparison.Ordinal)
                && now - lane.LastShownUnscaledTime < STATE_LABEL_DEDUP_S)
                return;

            if (lane.HasQueued
                && string.Equals(lane.QueuedText, text, StringComparison.Ordinal)
                && now - lane.LastShownUnscaledTime < STATE_LABEL_DEDUP_S)
                return;

            if (lane.HasActive)
            {
                // 1 en file : un 3e remplace la file (le plus récent gagne).
                lane.HasQueued = true;
                lane.QueuedText = text;
                lane.QueuedColor = color;
                lane.QueuedPos = unitPos;
                return;
            }

            if (_activeStateLabelCount >= MAX_ACTIVE_STATE_LABELS)
                return;

            BeginStateLabel(unitId, lane, text, color, unitPos);
        }

        private void BeginStateLabel(
            int unitId, StateLabelLane lane, string text, Color color, Vector3 unitPos)
        {
            float scale = text.Length > STATE_LABEL_LONG_CHARS
                ? STATE_LABEL_LONG_SCALE
                : 1f;

            Vector2 pos = ResolveFreeStateLabelPosition(
                WorldToNormalized(unitPos + Vector3.up * stateLabelOffsetY));
            TextAnimation anim = labelAnim != null ? labelAnim : critLabelAnim;
            TextAnimation tinted = GetTintedAnim(anim, color);

            bool shown = TryDisplayStateLabelPixel(text, tinted, pos, scale);
            if (!shown && useLegacyFallback)
                FallbackSpawn(text, color, unitPos + Vector3.up * (stateLabelOffsetY - worldOffsetY), scale, false);
            else if (!shown)
                return;

            lane.HasActive = true;
            lane.ActiveText = text;
            lane.LastShownUnscaledTime = Time.unscaledTime;
            lane.LastActivityUnscaledTime = lane.LastShownUnscaledTime;
            _activeStateLabelCount++;

            if (lane.Timer != null)
                StopCoroutine(lane.Timer);
            lane.Timer = StartCoroutine(StateLabelTimer(unitId));
        }

        private IEnumerator StateLabelTimer(int unitId)
        {
            yield return new WaitForSecondsRealtime(STATE_LABEL_DISPLAY_S);

            if (!_stateLanes.TryGetValue(unitId, out StateLabelLane lane))
                yield break;

            if (lane.HasActive)
            {
                lane.HasActive = false;
                lane.ActiveText = null;
                _activeStateLabelCount = Mathf.Max(0, _activeStateLabelCount - 1);
            }

            lane.Timer = null;

            if (lane.HasQueued && _activeStateLabelCount < MAX_ACTIVE_STATE_LABELS)
            {
                string qText = lane.QueuedText;
                Color qColor = lane.QueuedColor;
                Vector3 qPos = lane.QueuedPos;
                lane.HasQueued = false;
                lane.QueuedText = null;
                BeginStateLabel(unitId, lane, qText, qColor, qPos);
            }
        }

        private void ClearStateLabelLanes()
        {
            foreach (KeyValuePair<int, StateLabelLane> pair in _stateLanes)
            {
                if (pair.Value.Timer != null)
                    StopCoroutine(pair.Value.Timer);
            }

            _stateLanes.Clear();
            _stateOccupied.Clear();
            _activeStateLabelCount = 0;
        }

        private void PruneInactiveStateLanes()
        {
            if (_stateLanes.Count == 0)
                return;

            float now = Time.unscaledTime;
            List<int> toRemove = null;
            foreach (KeyValuePair<int, StateLabelLane> pair in _stateLanes)
            {
                StateLabelLane lane = pair.Value;
                if (lane.HasActive || lane.HasQueued || lane.Timer != null)
                    continue;
                if (now - lane.LastActivityUnscaledTime <= STATE_LABEL_LANE_PURGE_S)
                    continue;
                if (toRemove == null)
                    toRemove = new List<int>(4);
                toRemove.Add(pair.Key);
            }

            if (toRemove == null)
                return;

            for (int i = 0; i < toRemove.Count; i++)
                _stateLanes.Remove(toRemove[i]);
        }

        private bool TryDisplayStateLabelPixel(
            string word, TextAnimation animation, Vector2 normalizedPos, float scale)
        {
            if (!preferPixelBattleText || animation == null)
                return false;

            if (PixelBattleTextController.singleton == null)
                return false;

            TextAnimation combatAnim = GetCombatScaledAnim(animation);
            if (scale < 0.999f)
                combatAnim = GetStateLabelScaledAnim(combatAnim, scale);

            PixelBattleTextController.DisplayText(word, combatAnim, normalizedPos);
            return true;
        }

        /// <summary> Clone léger pour scale 0,9 sur mots longs (cache borné). </summary>
        private TextAnimation GetStateLabelScaledAnim(TextAnimation source, float scale)
        {
            if (source == null || scale >= 0.999f)
                return source;

            // Réutilise le cache teinté/scaled via clé distincte : Instantiate une fois.
            var key = (source, new Color32(255, 255, 255, (byte)Mathf.RoundToInt(scale * 255f)));
            if (_tintedAnimCache == null)
                _tintedAnimCache = new Dictionary<(TextAnimation, Color32), TextAnimation>(16);

            if (_tintedAnimCache.TryGetValue(key, out TextAnimation cached) && cached != null)
                return cached;

            TextAnimation clone = Instantiate(source);
            clone.name = source.name + "_StateScale";
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.textSize = Mathf.Max(8, Mathf.RoundToInt(source.textSize * scale));
            _tintedAnimCache[key] = clone;
            return clone;
        }

        /// <summary>
        /// Dedup-frame au puits des chiffres (F5-L1). Log DEV si doublon.
        /// </summary>
        private bool IsDuplicateDamagePopup(int unitKey, int amount)
        {
            int frame = Time.frameCount;
            if (_damageDedupFrame == frame
                && _damageDedupAmount == amount
                && _damageDedupUnitId == unitKey)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Popup] doublon unit:{unitKey} {amount} f{frame}");
#endif
                return true;
            }

            _damageDedupFrame = frame;
            _damageDedupAmount = amount;
            _damageDedupUnitId = unitKey;
            return false;
        }

        private static int HashWorldUnit(Vector3 worldPos)
        {
            // Grille grossière (~0,25 u) pour coller « même unité » sans Transform.
            int x = Mathf.RoundToInt(worldPos.x * 4f);
            int y = Mathf.RoundToInt(worldPos.y * 4f);
            return (x * 73856093) ^ (y * 19349663);
        }

        /// <summary>
        /// Clone runtime du preset avec fill teinté ; alphaKeys et border inchangés.
        /// Cache borné par nature (couleurs = palette D12).
        /// </summary>
        private TextAnimation GetTintedAnim(TextAnimation baseAnim, Color color)
        {
            if (baseAnim == null)
                return null;

            if (_tintedAnimCache == null)
                _tintedAnimCache = new Dictionary<(TextAnimation, Color32), TextAnimation>(16);

            Color32 keyColor = color;
            var key = (baseAnim, keyColor);
            if (_tintedAnimCache.TryGetValue(key, out TextAnimation cached) && cached != null)
                return cached;

            TextAnimation clone = Instantiate(baseAnim);
            clone.name = baseAnim.name + "_Tinted";
            clone.hideFlags = HideFlags.HideAndDontSave;

            Gradient source = baseAnim.fillColorInTime;
            GradientColorKey[] srcColors = source.colorKeys;
            GradientAlphaKey[] srcAlphas = source.alphaKeys;
            GradientColorKey[] newColors = new GradientColorKey[srcColors.Length];
            for (int i = 0; i < srcColors.Length; i++)
                newColors[i] = new GradientColorKey(color, srcColors[i].time);

            Gradient tinted = new Gradient();
            tinted.SetKeys(newColors, srcAlphas);
            clone.fillColorInTime = tinted;

            _tintedAnimCache[key] = clone;
            return clone;
        }

        private bool CanSpawnPopup(bool priority)
        {
            PruneOccupied();
            if (priority)
                return true;
            return _occupied.Count < Mathf.Max(1, maxSimultaneousPopups);
        }

        // ═══════════════════════════════════════════
        // PIXEL BATTLE TEXT
        // ═══════════════════════════════════════════

        private bool TryDisplayPixel(string word, TextAnimation animation, Vector2 normalizedPos)
        {
            if (!preferPixelBattleText || animation == null)
                return false;

            if (PixelBattleTextController.singleton == null)
                return false;

            TextAnimation combatAnim = GetCombatScaledAnim(animation);
            PixelBattleTextController.DisplayText(word, combatAnim, normalizedPos);
            return true;
        }

        /// <summary>
        /// Clone runtime du preset pack : beaucoup plus gros et plus lent pour la lisibilité mobile.
        /// </summary>
        private TextAnimation GetCombatScaledAnim(TextAnimation source)
        {
            if (source == null)
                return null;

            if (_scaledAnimCache.TryGetValue(source, out TextAnimation cached) && cached != null)
                return cached;

            TextAnimation scaled = ScriptableObject.Instantiate(source);
            scaled.name = source.name + "_CombatScaled";
            scaled.hideFlags = HideFlags.HideAndDontSave;

            float sizeMul = Mathf.Max(1f, textSizeMul);
            float durMul = Mathf.Max(1f, durationMul);
            float spaceMul = Mathf.Max(1f, spacingMul);
            float moveMul = Mathf.Max(1f, motionMul);

            scaled.textSize = Mathf.Clamp(
                Mathf.RoundToInt(source.textSize * sizeMul),
                MIN_COMBAT_TEXT_SIZE,
                MAX_COMBAT_TEXT_SIZE);
            scaled.transitionDuration = source.transitionDuration * durMul;
            // Délai lettres court : le nombre se lit d'un bloc, pas lettre par lettre.
            scaled.perLetterDelay = Mathf.Min(0.04f, source.perLetterDelay * 0.35f);
            scaled.initialSpacing = source.initialSpacing * spaceMul;
            scaled.endSpacing = source.endSpacing * spaceMul;
            scaled.initialOffset = source.initialOffset * moveMul;
            scaled.endOffset = source.endOffset * moveMul;

            _scaledAnimCache[source] = scaled;
            return scaled;
        }

        private void ClearScaledAnimCache()
        {
            foreach (KeyValuePair<TextAnimation, TextAnimation> pair in _scaledAnimCache)
            {
                if (pair.Value != null)
                    Destroy(pair.Value);
            }

            _scaledAnimCache.Clear();
        }

        private Vector2 WorldToNormalized(Vector3 worldPos)
        {
            // Overlay plein écran : le viewport caméra = coords 0–1 attendues par PixelBattleText.
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null)
                return new Vector2(0.5f, 0.55f);

            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            if (vp.z < 0f)
                return new Vector2(0.5f, 0.55f);

            return new Vector2(Mathf.Clamp01(vp.x), Mathf.Clamp01(vp.y));
        }

        private Vector2 ResolveFreePosition(Vector2 desired)
        {
            PruneList(_occupied);

            float jx = UnityEngine.Random.Range(-baseJitterX, baseJitterX);
            float jy = UnityEngine.Random.Range(-baseJitterY, baseJitterY);
            Vector2 candidate = ClampNorm(desired + new Vector2(jx, jy));

            for (int attempt = 0; attempt < MAX_OFFSET_ATTEMPTS; attempt++)
            {
                if (!IsTooClose(candidate, _occupied))
                {
                    RegisterOccupied(_occupied, candidate);
                    return candidate;
                }

                float angle = attempt * 2.399963f;
                float radius = MIN_SEPARATION * (1.15f + attempt * 0.55f);
                candidate = ClampNorm(desired + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Abs(Mathf.Sin(angle)) * radius + 0.02f * attempt));
            }

            RegisterOccupied(_occupied, candidate);
            return candidate;
        }

        /// <summary>
        /// Stagger labels d'état sans toucher le budget chiffres (_occupied).
        /// </summary>
        private Vector2 ResolveFreeStateLabelPosition(Vector2 desired)
        {
            PruneList(_stateOccupied);

            float jx = UnityEngine.Random.Range(-baseJitterX, baseJitterX);
            float jy = UnityEngine.Random.Range(-baseJitterY, baseJitterY);
            Vector2 candidate = ClampNorm(desired + new Vector2(jx, jy));

            for (int attempt = 0; attempt < MAX_OFFSET_ATTEMPTS; attempt++)
            {
                if (!IsTooClose(candidate, _stateOccupied))
                {
                    RegisterOccupied(_stateOccupied, candidate);
                    return candidate;
                }

                float angle = attempt * 2.399963f;
                float radius = MIN_SEPARATION * (1.15f + attempt * 0.55f);
                candidate = ClampNorm(desired + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Abs(Mathf.Sin(angle)) * radius + 0.02f * attempt));
            }

            RegisterOccupied(_stateOccupied, candidate);
            return candidate;
        }

        private static Vector2 ClampNorm(Vector2 p)
        {
            // Garde les textes dans la zone jouable (pas dans le HUD haut / barres bas).
            return new Vector2(Mathf.Clamp(p.x, 0.12f, 0.88f), Mathf.Clamp(p.y, 0.22f, 0.72f));
        }

        private void PruneOccupied() => PruneList(_occupied);

        private static void PruneList(List<OccupiedSlot> list)
        {
            float now = Time.unscaledTime;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].ExpireAt <= now)
                    list.RemoveAt(i);
            }
        }

        private static bool IsTooClose(Vector2 pos, List<OccupiedSlot> list)
        {
            float minSq = MIN_SEPARATION * MIN_SEPARATION;
            for (int i = 0; i < list.Count; i++)
            {
                if ((list[i].Normalized - pos).sqrMagnitude < minSq)
                    return true;
            }

            return false;
        }

        private static void RegisterOccupied(List<OccupiedSlot> list, Vector2 pos)
        {
            list.Add(new OccupiedSlot
            {
                Normalized = pos,
                ExpireAt = Time.unscaledTime + OCCUPANCY_LIFETIME
            });
        }

        // ═══════════════════════════════════════════
        // ABONNEMENTS COMBAT
        // ═══════════════════════════════════════════

        /// <summary> Abonne un ennemi fraîchement spawné (étage / mid-combat). </summary>
        public void NotifyEnemySpawned(Enemy enemy)
        {
            BindEnemy(enemy);
        }

        /// <summary> Désabonne tous les ennemis (ClearEnemies / changement d'étage). </summary>
        public void NotifyEnemiesCleared()
        {
            foreach (KeyValuePair<Enemy, Action<int>> pair in _enemyDamagedHandlers)
            {
                if (pair.Key == null)
                    continue;
                pair.Key.OnDamaged -= pair.Value;
                if (_enemyDeathHandlers.TryGetValue(pair.Key, out Action death))
                    pair.Key.OnDeath -= death;
            }

            _enemyDamagedHandlers.Clear();
            _enemyDeathHandlers.Clear();
        }

        private void BindAllParticipants()
        {
            if (_turnManager == null)
                return;

            IReadOnlyList<ITurnParticipant> participants = _turnManager.Participants;
            if (participants == null)
                return;

            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] is CharacterBall ally)
                    BindAlly(ally);
                else if (participants[i] is Enemy enemy)
                    BindEnemy(enemy);
            }
        }

        private void HandleEnemyAddedMidCombat(Enemy enemy)
        {
            BindEnemy(enemy);
        }

        private void BindAlly(CharacterBall ally)
        {
            if (ally == null || _allyDamagedHandlers.ContainsKey(ally))
                return;

            Action<int> onDamaged = amount =>
            {
                if (ally.ConsumeSuppressDamagePopup())
                    return;

                if (IsDuplicateDamagePopup(ally.GetInstanceID(), amount))
                    return;

                ShowDamageAlly(amount, ally.transform.position, skipFrameDedup: true);
            };
            Action<int> onHealed = amount => ShowHeal(amount, ally.transform.position);

            ally.OnDamaged += onDamaged;
            ally.OnHealed += onHealed;
            _allyDamagedHandlers[ally] = onDamaged;
            _allyHealedHandlers[ally] = onHealed;
        }

        private void BindEnemy(Enemy enemy)
        {
            if (enemy == null || _enemyDamagedHandlers.ContainsKey(enemy))
                return;

            Action<int> onDamaged = amount =>
            {
                if (enemy.ConsumeSuppressDamagePopup())
                    return;

                // Coup fatal : uniquement le KO (évite chiffre + KO empilés).
                if (skipDamagePopupOnKill && enemy.CurrentHp <= 0)
                    return;

                if (IsDuplicateDamagePopup(enemy.GetInstanceID(), amount))
                    return;

                bool isCrit = enemy.LastDamageWasCrit;
                ShowDamageEnemy(amount, enemy.transform.position, isCrit, skipFrameDedup: true);
            };
            Action onDeath = () => ShowKO(enemy.transform.position);

            enemy.OnDamaged += onDamaged;
            enemy.OnDeath += onDeath;
            _enemyDamagedHandlers[enemy] = onDamaged;
            _enemyDeathHandlers[enemy] = onDeath;
        }

        private void UnbindCombatEvents()
        {
            if (_turnManager != null)
                _turnManager.OnEnemyAddedMidCombat -= HandleEnemyAddedMidCombat;

            foreach (KeyValuePair<CharacterBall, Action<int>> pair in _allyDamagedHandlers)
            {
                if (pair.Key == null)
                    continue;
                pair.Key.OnDamaged -= pair.Value;
                if (_allyHealedHandlers.TryGetValue(pair.Key, out Action<int> healed))
                    pair.Key.OnHealed -= healed;
            }

            _allyDamagedHandlers.Clear();
            _allyHealedHandlers.Clear();

            foreach (KeyValuePair<Enemy, Action<int>> pair in _enemyDamagedHandlers)
            {
                if (pair.Key == null)
                    continue;
                pair.Key.OnDamaged -= pair.Value;
                if (_enemyDeathHandlers.TryGetValue(pair.Key, out Action death))
                    pair.Key.OnDeath -= death;
            }

            _enemyDamagedHandlers.Clear();
            _enemyDeathHandlers.Clear();
        }

        // ═══════════════════════════════════════════
        // FALLBACK LEGACY
        // ═══════════════════════════════════════════

        private void FallbackDamageEnemy(int amount, Vector3 worldPos, bool isCrit)
        {
            Color color = isCrit ? _critColor : _enemyDamageColor;
            float magScale = Mathf.Lerp(_minMagnitudeScale, _maxMagnitudeScale,
                Mathf.Clamp01((float)(amount - _damageForMinScale)
                    / Mathf.Max(1, _damageForMaxScale - _damageForMinScale)));
            FallbackSpawn(amount.ToString(), color, worldPos,
                magScale * (isCrit ? _critScaleMul : 1f), isCrit);
        }

        private void FallbackSpawn(string text, Color color, Vector3 worldPos, float scale, bool isCrit)
        {
            if (floatingNumberPrefab == null)
                return;

            float offsetX = UnityEngine.Random.Range(-randomOffsetX, randomOffsetX);
            Vector3 spawnPos = worldPos + new Vector3(offsetX, spawnOffsetY, 0f);
            GameObject go = Instantiate(floatingNumberPrefab, spawnPos, Quaternion.identity);
            FloatingNumber fn = go.GetComponent<FloatingNumber>();
            if (fn != null)
                fn.Initialize(text, color, scale, isCrit);
        }
    }
}
