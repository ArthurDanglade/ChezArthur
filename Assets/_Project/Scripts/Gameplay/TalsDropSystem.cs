using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.UI;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Anime la pluie de pièces Tals à la mort d'un ennemi (couche d'affichage uniquement).
    /// Le crédit logique reste immédiat dans Enemy.Die → RunManager.AddTals.
    /// </summary>
    public class TalsDropSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        public const string PrefPickupVolume = "TalsDrop_PickupVolume";

        // ═══════════════════════════════════════════
        // SINGLETON (scène)
        // ═══════════════════════════════════════════
        public static TalsDropSystem Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private Sprite[] coinSprites;
        [SerializeField] private RectTransform counterTarget;
        [SerializeField] private GameUI gameUI;
        [SerializeField] private Camera worldCamera;

        [Header("Éclatement")]
        [SerializeField] private float scatterRadiusMin = 0.3f;
        [SerializeField] private float scatterRadiusMax = 0.8f;
        [SerializeField] private float scatterDuration = 0.45f;
        [SerializeField] private float popArcHeight = 0.4f;
        [SerializeField] private float popLandBounceHeight = 0.12f;
        [SerializeField] private float popOvershootScale = 1.3f;
        [SerializeField] private float popUndershootScale = 0.9f;

        [Header("Aspiration")]
        [SerializeField] private float attractDelayMin = 0.5f;
        [SerializeField] private float attractDelayMax = 1f;
        [SerializeField] private float attractMaxSpeed = 18f;
        [SerializeField] private float attractAcceleration = 60f;
        [SerializeField] private float arrivalDistance = 0.2f;

        [Header("Règles")]
        [SerializeField] private int maxCoinsPerKill = 15;

        [Header("Audio")]
        [SerializeField] private AudioClip[] pickupClips;
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 1f;
        [SerializeField] private int pickupVoiceCount = 16;
        [SerializeField] private float pitchStart = 1f;
        [SerializeField] private float pitchStep = 0.08f;
        [SerializeField] private float pitchMax = 2.2f;
        [SerializeField] private float pitchResetDelay = 0.45f;
        [SerializeField] private float pickupVolumeStep = 0.04f;

        [Header("Rendu")]
        [SerializeField] private int coinSortingOrder = 20;

        // ═══════════════════════════════════════════
        // STRUCTURE INTERNE
        // ═══════════════════════════════════════════
        private sealed class CoinState
        {
            public Transform T;
            public SpriteRenderer Sr;
            public int Value;
            public float Age;
            public Vector3 Origin;
            public Vector3 ScatterOffset;
            public float AttractStart;
            public float Speed;
            public float BaseScale;
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<CoinState> _active = new List<CoinState>(16);
        private readonly Stack<CoinState> _statePool = new Stack<CoinState>(16);
        private readonly Stack<Transform> _coinPool = new Stack<Transform>(16);

        private Transform _coinContainer;
        private int _inFlight;
        private float _lastSfxTime = -999f;
        private int _pickupStreak;
        private int _pickupClipIndex;
        private int _coinSpriteIndex;

        private AudioSource[] _pickupVoices;
        private int _pickupVoiceNext;

        private Vector3 _cachedTargetWorld;
        private bool _targetCached;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public int InFlightAmount => _inFlight;

        /// <summary> True tant que des pièces Tals cosmétiques sont encore actives ou en vol. </summary>
        public bool HasPendingDrops => _inFlight > 0 || _active.Count > 0;

        /// <summary> Volume des SFX de ramassage Tals (0–1). </summary>
        public float PickupVolume => pickupVolume;

        /// <summary>
        /// Charge le volume sauvegardé (utilisable avant que l'instance soit prête).
        /// </summary>
        public static float LoadSavedPickupVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(PrefPickupVolume, 1f));
        }

        /// <summary>
        /// Définit le volume des SFX de ramassage et le persiste.
        /// </summary>
        public void SetPickupVolume(float normalized)
        {
            pickupVolume = Mathf.Clamp01(normalized);
            PlayerPrefs.SetFloat(PrefPickupVolume, pickupVolume);
            PlayerPrefs.Save();
        }

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

            Transform container = transform.Find("CoinContainer");
            if (container == null)
            {
                GameObject go = new GameObject("CoinContainer");
                go.transform.SetParent(transform, false);
                container = go.transform;
            }

            _coinContainer = container;

            pickupVolume = LoadSavedPickupVolume();

            EnsurePickupVoices();
            PrewarmPools();
        }

        private void EnsurePickupVoices()
        {
            int count = Mathf.Max(1, pickupVoiceCount);
            _pickupVoices = GetComponents<AudioSource>();

            if (_pickupVoices == null || _pickupVoices.Length < count)
            {
                int existing = _pickupVoices != null ? _pickupVoices.Length : 0;
                var voices = new AudioSource[count];
                for (int i = 0; i < count; i++)
                {
                    if (i < existing)
                    {
                        voices[i] = _pickupVoices[i];
                    }
                    else
                    {
                        voices[i] = gameObject.AddComponent<AudioSource>();
                    }

                    voices[i].playOnAwake = false;
                    voices[i].spatialBlend = 0f;
                    voices[i].loop = false;
                }

                _pickupVoices = voices;
            }
        }

        private void Start()
        {
            Enemy.OnTalsDropped += HandleDrop;

            if (RunManager.Instance != null)
                RunManager.Instance.OnRunEnded += OnRunEndedFlush;
        }

        private void OnDestroy()
        {
            Enemy.OnTalsDropped -= HandleDrop;

            if (RunManager.Instance != null)
                RunManager.Instance.OnRunEnded -= OnRunEndedFlush;

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_active.Count == 0)
                return;

            _targetCached = false;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                CoinState coin = _active[i];
                coin.Age += Time.deltaTime;

                if (coin.Age < scatterDuration)
                {
                    float t = coin.Age / scatterDuration;
                    ApplyScatterVisual(coin, t);
                }
                else if (coin.Age >= coin.AttractStart)
                {
                    ApplyBaseScale(coin);

                    if (!_targetCached)
                    {
                        _cachedTargetWorld = ResolveCounterWorldPosition();
                        _targetCached = true;
                    }

                    coin.Speed = Mathf.Min(attractMaxSpeed,
                        coin.Speed + attractAcceleration * Time.deltaTime);

                    Vector3 pos = coin.T.position;
                    Vector3 next = Vector3.MoveTowards(pos, _cachedTargetWorld, coin.Speed * Time.deltaTime);
                    coin.T.position = next;

                    if ((next - _cachedTargetWorld).sqrMagnitude <= arrivalDistance * arrivalDistance)
                        Collect(coin, i);
                }
            }
        }

        // ═══════════════════════════════════════════
        // GESTION DES DROPS
        // ═══════════════════════════════════════════

        private void HandleDrop(Vector3 pos, int amount)
        {
            if (amount <= 0 || coinPrefab == null)
                return;

            int count = Mathf.Min(amount, maxCoinsPerKill);
            int baseValue = amount / count;
            int remainder = amount % count;

            _inFlight += amount;

            if (gameUI != null)
                gameUI.RefreshTalsDisplay(punch: false);

            Debug.Log($"[TalsDrop] {amount} tals → {count} pièces");

            for (int i = 0; i < count; i++)
            {
                int value = baseValue + (i < remainder ? 1 : 0);
                SpawnCoin(pos, value);
            }
        }

        private void SpawnCoin(Vector3 origin, int value)
        {
            Transform coinTransform = AcquireCoinTransform();
            if (coinTransform == null)
                return;

            SpriteRenderer sr = coinTransform.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = coinTransform.gameObject.AddComponent<SpriteRenderer>();

            if (coinSprites != null && coinSprites.Length > 0)
            {
                Sprite sprite = SelectCoinSprite();
                if (sprite != null)
                    sr.sprite = sprite;
            }

            sr.sortingOrder = coinSortingOrder;

            float baseScale = Mathf.Abs(coinTransform.localScale.x);
            if (baseScale < 0.001f)
                baseScale = 0.35f;

            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector2.right;

            float radius = UnityEngine.Random.Range(scatterRadiusMin, scatterRadiusMax);

            CoinState state = AcquireCoinState();
            state.T = coinTransform;
            state.Sr = sr;
            state.Value = value;
            state.Age = 0f;
            state.Origin = origin;
            state.ScatterOffset = (Vector3)(dir * radius);
            state.AttractStart = UnityEngine.Random.Range(attractDelayMin, attractDelayMax);
            state.Speed = 0f;
            state.BaseScale = baseScale;

            coinTransform.position = origin;
            coinTransform.localScale = Vector3.zero;
            coinTransform.gameObject.SetActive(true);

            _active.Add(state);
        }

        private void Collect(CoinState coin, int index)
        {
            _inFlight = Mathf.Max(0, _inFlight - coin.Value);

            if (gameUI != null)
                gameUI.RefreshTalsDisplay(punch: true);

            PlayPickupSfx();
            ReleaseCoin(coin, index);
        }

        private void FlushAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                ReleaseCoin(_active[i], i);

            _inFlight = 0;

            if (gameUI != null)
                gameUI.RefreshTalsDisplay(punch: true);

            PlayPickupSfx();
        }

        private void OnRunEndedFlush(bool victory)
        {
            FlushAll();
        }

        // ═══════════════════════════════════════════
        // AUDIO
        // ═══════════════════════════════════════════

        private void PlayPickupSfx()
        {
            AudioClip clip = SelectPickupClip();
            if (clip == null || _pickupVoices == null || _pickupVoices.Length == 0)
                return;

            float now = Time.time;
            if (now - _lastSfxTime > pitchResetDelay)
                _pickupStreak = 0;

            _pickupStreak++;

            float pitch = Mathf.Min(pitchMax, pitchStart + (_pickupStreak - 1) * pitchStep);
            float volume = Mathf.Min(1f, pickupVolume + (_pickupStreak - 1) * pickupVolumeStep);

            AudioSource src = _pickupVoices[_pickupVoiceNext];
            _pickupVoiceNext = (_pickupVoiceNext + 1) % _pickupVoices.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);

            _lastSfxTime = now;
        }

        private Sprite SelectCoinSprite()
        {
            int count = coinSprites.Length;
            for (int attempt = 0; attempt < count; attempt++)
            {
                Sprite candidate = coinSprites[_coinSpriteIndex];
                _coinSpriteIndex = (_coinSpriteIndex + 1) % count;
                if (candidate != null)
                    return candidate;
            }

            return null;
        }

        private void ApplyScatterVisual(CoinState coin, float t)
        {
            float easeOut = 1f - (1f - t) * (1f - t);
            Vector3 horizontal = coin.ScatterOffset * easeOut;
            float arc = popArcHeight * 4f * t * (1f - t);

            float landBounce = 0f;
            if (t > 0.8f)
            {
                float bounceT = (t - 0.8f) / 0.2f;
                landBounce = popLandBounceHeight * Mathf.Sin(bounceT * Mathf.PI);
            }

            coin.T.position = coin.Origin + horizontal + Vector3.up * (arc + landBounce);

            float scaleFactor = EvaluatePopScale(t) * coin.BaseScale;
            coin.T.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        }

        private float EvaluatePopScale(float t)
        {
            if (t < 0.3f)
            {
                float p = t / 0.3f;
                float eased = 1f - (1f - p) * (1f - p);
                return Mathf.Lerp(0f, popOvershootScale, eased);
            }

            if (t < 0.55f)
            {
                float p = (t - 0.3f) / 0.25f;
                return Mathf.Lerp(popOvershootScale, popUndershootScale, p);
            }

            float settle = (t - 0.55f) / 0.45f;
            return Mathf.Lerp(popUndershootScale, 1f, settle);
        }

        private void ApplyBaseScale(CoinState coin)
        {
            float s = coin.BaseScale;
            coin.T.localScale = new Vector3(s, s, 1f);
        }

        private AudioClip SelectPickupClip()
        {
            if (pickupClips == null || pickupClips.Length == 0)
                return null;

            int count = pickupClips.Length;
            for (int attempt = 0; attempt < count; attempt++)
            {
                AudioClip candidate = pickupClips[_pickupClipIndex];
                _pickupClipIndex = (_pickupClipIndex + 1) % count;
                if (candidate != null)
                    return candidate;
            }

            return null;
        }

        // ═══════════════════════════════════════════
        // POOLING
        // ═══════════════════════════════════════════

        private void PrewarmPools()
        {
            for (int i = 0; i < maxCoinsPerKill; i++)
            {
                _statePool.Push(new CoinState());

                if (coinPrefab != null)
                {
                    GameObject go = Instantiate(coinPrefab, _coinContainer);
                    go.SetActive(false);
                    _coinPool.Push(go.transform);
                }
            }
        }

        private CoinState AcquireCoinState()
        {
            if (_statePool.Count > 0)
                return _statePool.Pop();

            return new CoinState();
        }

        private Transform AcquireCoinTransform()
        {
            if (_coinPool.Count > 0)
                return _coinPool.Pop();

            if (coinPrefab == null)
                return null;

            GameObject go = Instantiate(coinPrefab, _coinContainer);
            return go.transform;
        }

        private void ReleaseCoin(CoinState coin, int activeIndex)
        {
            if (coin.T != null)
            {
                coin.T.gameObject.SetActive(false);
                _coinPool.Push(coin.T);
            }

            coin.T = null;
            coin.Sr = null;
            coin.Value = 0;
            coin.BaseScale = 0f;
            _statePool.Push(coin);

            int last = _active.Count - 1;
            if (activeIndex != last)
                _active[activeIndex] = _active[last];

            _active.RemoveAt(last);
        }

        // ═══════════════════════════════════════════
        // CIBLE HUD
        // ═══════════════════════════════════════════

        private Vector3 ResolveCounterWorldPosition()
        {
            if (counterTarget == null || worldCamera == null)
                return Vector3.zero;

            Vector3 screen = RectTransformUtility.WorldToScreenPoint(null, counterTarget.position);
            float depth = -worldCamera.transform.position.z;
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            world.z = 0f;
            return world;
        }
    }
}
