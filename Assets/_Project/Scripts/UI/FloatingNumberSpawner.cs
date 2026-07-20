using System;
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

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static FloatingNumberSpawner Instance { get; private set; }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<OccupiedSlot> _occupied = new List<OccupiedSlot>(32);
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
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary> Dégâts ennemi ; crit = chiffre + label CRIT. </summary>
        public void ShowDamageEnemy(int amount, Vector3 worldPos, bool isCrit = false)
        {
            if (amount <= 0)
                return;

            if (!isCrit && amount < minDamageToShow)
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
            if (amount <= 0 || amount < minDamageToShow)
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

        /// <summary> Label libre (ex. MÉGACRIT !). </summary>
        public void ShowLabel(string text, Color color, Vector3 worldPos, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (!CanSpawnPopup(priority: true))
                return;

            Vector2 pos = ResolveFreePosition(WorldToNormalized(worldPos + Vector3.up * worldOffsetY));
            TextAnimation anim = labelAnim != null ? labelAnim : critLabelAnim;
            if (TryDisplayPixel(text, anim, pos))
                return;

            if (useLegacyFallback)
                FallbackSpawn(text, color, worldPos, scale, false);
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
            PruneOccupied();

            float jx = UnityEngine.Random.Range(-baseJitterX, baseJitterX);
            float jy = UnityEngine.Random.Range(-baseJitterY, baseJitterY);
            Vector2 candidate = ClampNorm(desired + new Vector2(jx, jy));

            for (int attempt = 0; attempt < MAX_OFFSET_ATTEMPTS; attempt++)
            {
                if (!IsTooClose(candidate))
                {
                    RegisterOccupied(candidate);
                    return candidate;
                }

                float angle = attempt * 2.399963f;
                float radius = MIN_SEPARATION * (1.15f + attempt * 0.55f);
                candidate = ClampNorm(desired + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Abs(Mathf.Sin(angle)) * radius + 0.02f * attempt));
            }

            RegisterOccupied(candidate);
            return candidate;
        }

        private static Vector2 ClampNorm(Vector2 p)
        {
            // Garde les textes dans la zone jouable (pas dans le HUD haut / barres bas).
            return new Vector2(Mathf.Clamp(p.x, 0.12f, 0.88f), Mathf.Clamp(p.y, 0.22f, 0.72f));
        }

        private void PruneOccupied()
        {
            float now = Time.unscaledTime;
            for (int i = _occupied.Count - 1; i >= 0; i--)
            {
                if (_occupied[i].ExpireAt <= now)
                    _occupied.RemoveAt(i);
            }
        }

        private bool IsTooClose(Vector2 pos)
        {
            float minSq = MIN_SEPARATION * MIN_SEPARATION;
            for (int i = 0; i < _occupied.Count; i++)
            {
                if ((_occupied[i].Normalized - pos).sqrMagnitude < minSq)
                    return true;
            }

            return false;
        }

        private void RegisterOccupied(Vector2 pos)
        {
            _occupied.Add(new OccupiedSlot
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

            Action<int> onDamaged = amount => ShowDamageAlly(amount, ally.transform.position);
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

                bool isCrit = enemy.LastDamageWasCrit;
                ShowDamageEnemy(amount, enemy.transform.position, isCrit);
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
