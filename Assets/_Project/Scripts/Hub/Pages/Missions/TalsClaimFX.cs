using System.Collections.Generic;
using ChezArthur.Audio;
using ChezArthur.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub.Pages.Missions
{
    /// <summary>
    /// FX claim Missions : pluie d'icônes Tals (max 20) qui explosent puis
    /// s'aspirent vers le compteur header — jouissif, pitch escaladé.
    /// </summary>
    [DisallowMultipleComponent]
    public class TalsClaimFX : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int MaxCoins = 20;
        private const int MinCoins = 4;
        private const float CoinUiSize = 56f;
        private const string OverlayName = "TalsClaimFXOverlay";

        // ═══════════════════════════════════════════
        // SINGLETON SCÈNE
        // ═══════════════════════════════════════════
        public static TalsClaimFX Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Sprites Tals (aléatoire au pop)")]
        [SerializeField] private Sprite[] coinSprites;

        [Header("Cible header")]
        [SerializeField] private RectTransform counterTarget;

        [Header("Audio")]
        [SerializeField] private AudioClip claimSuccessClip;
        [SerializeField] private AudioClip[] pickupClips;
        [SerializeField, Range(0f, 1f)] private float claimVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.7f;
        [SerializeField] private float pitchStart = 1f;
        [SerializeField] private float pitchStep = 0.07f;
        [SerializeField] private float pitchMax = 2.1f;
        [SerializeField] private float pitchResetDelay = 0.4f;

        [Header("Éclatement")]
        [SerializeField] private float scatterRadiusMin = 60f;
        [SerializeField] private float scatterRadiusMax = 180f;
        [SerializeField] private float scatterDuration = 0.38f;
        [SerializeField] private float popArcHeight = 90f;
        [SerializeField] private float popOvershootScale = 1.35f;

        [Header("Aspiration")]
        [SerializeField] private float attractDelayMin = 0.28f;
        [SerializeField] private float attractDelayMax = 0.55f;
        [SerializeField] private float attractMaxSpeed = 3200f;
        [SerializeField] private float attractAcceleration = 9000f;
        [SerializeField] private float arrivalDistance = 28f;

        // ═══════════════════════════════════════════
        // STRUCTURE
        // ═══════════════════════════════════════════
        private sealed class CoinState
        {
            public RectTransform Rt;
            public Image Img;
            public float Age;
            public Vector2 Origin;
            public Vector2 ScatterOffset;
            public float AttractStart;
            public float Speed;
            public float BaseScale;
            public float Spin;
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<CoinState> _active = new List<CoinState>(MaxCoins);
        private readonly Stack<CoinState> _pool = new Stack<CoinState>(MaxCoins);
        private RectTransform _overlay;
        private Canvas _canvas;
        private int _pickupStreak;
        private float _lastSfxTime = -999f;
        private int _spriteIndex;
        private AudioSource[] _voices;
        private int _voiceNext;
        private Vector3 _punchBaseScale = Vector3.one;
        private float _punchUntil;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureOverlay();
            EnsureVoices();
            CachePunchBase();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            UpdatePunch();

            if (_active.Count == 0)
                return;

            Vector2 target = ResolveTargetLocal();

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                CoinState coin = _active[i];
                coin.Age += Time.unscaledDeltaTime;

                if (coin.Age < scatterDuration)
                {
                    ApplyScatter(coin);
                }
                else if (coin.Age >= coin.AttractStart)
                {
                    coin.Speed = Mathf.Min(
                        attractMaxSpeed,
                        coin.Speed + attractAcceleration * Time.unscaledDeltaTime);

                    Vector2 pos = coin.Rt.anchoredPosition;
                    Vector2 next = Vector2.MoveTowards(pos, target, coin.Speed * Time.unscaledDeltaTime);
                    coin.Rt.anchoredPosition = next;

                    // Légère rotation + shrink en approche
                    float dist = Vector2.Distance(next, target);
                    float approach = Mathf.Clamp01(1f - dist / 400f);
                    float scale = Mathf.Lerp(coin.BaseScale, coin.BaseScale * 0.55f, approach);
                    coin.Rt.localScale = Vector3.one * scale;
                    coin.Rt.localEulerAngles = new Vector3(0f, 0f, coin.Spin * coin.Age * 180f);

                    if (dist <= arrivalDistance)
                        Collect(i);
                }
                else
                {
                    // Hold après scatter : idle bounce léger
                    float idle = (coin.Age - scatterDuration) / Mathf.Max(0.01f, coin.AttractStart - scatterDuration);
                    float bob = Mathf.Sin(idle * Mathf.PI) * 6f;
                    coin.Rt.anchoredPosition = coin.Origin + coin.ScatterOffset + new Vector2(0f, bob);
                    coin.Rt.localScale = Vector3.one * coin.BaseScale;
                }
            }
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Lance le FX depuis une carte mission. amount = Tals crédités.
        /// </summary>
        public void Play(RectTransform fromCard, int amount)
        {
            if (fromCard == null || amount <= 0)
                return;

            EnsureOverlay();
            ResolveTargetIfNeeded();

            int count = Mathf.Clamp(Mathf.RoundToInt(amount / 12f), MinCoins, MaxCoins);
            if (amount <= 50)
                count = Mathf.Clamp(Mathf.Max(MinCoins, amount / 20), MinCoins, 8);
            count = Mathf.Min(count, MaxCoins);

            Vector2 origin = WorldToOverlay(fromCard.TransformPoint(fromCard.rect.center));

            PlayClaimSfx();

            for (int i = 0; i < count; i++)
                SpawnCoin(origin);
        }

        public void Bind(
            Sprite[] sprites,
            RectTransform target,
            AudioClip successClip,
            AudioClip[] pickups)
        {
            coinSprites = sprites;
            counterTarget = target;
            claimSuccessClip = successClip;
            pickupClips = pickups;
            CachePunchBase();
        }

        // ═══════════════════════════════════════════
        // PRIVÉ — FX
        // ═══════════════════════════════════════════

        private void SpawnCoin(Vector2 origin)
        {
            CoinState coin = Acquire();
            coin.Age = 0f;
            coin.Origin = origin;
            coin.Speed = 0f;
            coin.BaseScale = UnityEngine.Random.Range(0.85f, 1.15f);
            coin.Spin = UnityEngine.Random.Range(-1.2f, 1.2f);
            coin.AttractStart = scatterDuration
                                + UnityEngine.Random.Range(attractDelayMin, attractDelayMax);

            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector2.up;
            float radius = UnityEngine.Random.Range(scatterRadiusMin, scatterRadiusMax);
            // Bias légèrement vers le haut pour un « pop » plus jouissif
            coin.ScatterOffset = dir * radius + Vector2.up * UnityEngine.Random.Range(20f, 70f);

            if (coin.Img != null)
            {
                Sprite s = SelectSprite();
                if (s != null)
                    coin.Img.sprite = s;
                coin.Img.color = Color.white;
            }

            coin.Rt.anchoredPosition = origin;
            coin.Rt.localScale = Vector3.zero;
            coin.Rt.localEulerAngles = Vector3.zero;
            coin.Rt.gameObject.SetActive(true);
            _active.Add(coin);
        }

        private void ApplyScatter(CoinState coin)
        {
            float t = Mathf.Clamp01(coin.Age / scatterDuration);
            float easeOut = 1f - (1f - t) * (1f - t);
            Vector2 horizontal = coin.ScatterOffset * easeOut;
            float arc = popArcHeight * 4f * t * (1f - t);

            coin.Rt.anchoredPosition = coin.Origin + horizontal + new Vector2(0f, arc);

            // Scale pop : 0 → overshoot → base
            float scaleT;
            if (t < 0.35f)
                scaleT = Mathf.Lerp(0f, popOvershootScale, t / 0.35f);
            else
                scaleT = Mathf.Lerp(popOvershootScale, 1f, (t - 0.35f) / 0.65f);

            coin.Rt.localScale = Vector3.one * (scaleT * coin.BaseScale);
            coin.Rt.localEulerAngles = new Vector3(0f, 0f, coin.Spin * t * 90f);
        }

        private void Collect(int index)
        {
            CoinState coin = _active[index];
            PlayPickupSfx();
            PunchTarget();
            Release(coin, index);
        }

        private void PunchTarget()
        {
            if (counterTarget == null)
                return;

            _punchUntil = Time.unscaledTime + 0.18f;
            counterTarget.localScale = _punchBaseScale * 1.18f;
        }

        private void UpdatePunch()
        {
            if (counterTarget == null || Time.unscaledTime >= _punchUntil)
            {
                if (counterTarget != null && _punchUntil > 0f && Time.unscaledTime >= _punchUntil)
                {
                    counterTarget.localScale = Vector3.Lerp(
                        counterTarget.localScale, _punchBaseScale, Time.unscaledDeltaTime * 14f);
                }

                return;
            }

            float k = 1f - ((_punchUntil - Time.unscaledTime) / 0.18f);
            float s = Mathf.Lerp(1.18f, 1f, k * k);
            counterTarget.localScale = _punchBaseScale * s;
        }

        private void CachePunchBase()
        {
            if (counterTarget != null)
                _punchBaseScale = counterTarget.localScale;
            if (_punchBaseScale.sqrMagnitude < 0.01f)
                _punchBaseScale = Vector3.one;
        }

        // ═══════════════════════════════════════════
        // PRIVÉ — AUDIO / SPRITES
        // ═══════════════════════════════════════════

        private void PlayClaimSfx()
        {
            if (claimSuccessClip == null)
                return;

            if (SfxManager.Instance != null)
                SfxManager.Instance.PlaySfx(claimSuccessClip, claimVolume);
            else if (_voices != null && _voices.Length > 0)
                _voices[0].PlayOneShot(claimSuccessClip, claimVolume);
        }

        private void PlayPickupSfx()
        {
            AudioClip clip = SelectPickupClip();
            if (clip == null || _voices == null || _voices.Length == 0)
                return;

            float now = Time.unscaledTime;
            if (now - _lastSfxTime > pitchResetDelay)
                _pickupStreak = 0;

            _pickupStreak++;
            float pitch = Mathf.Min(pitchMax, pitchStart + (_pickupStreak - 1) * pitchStep);
            float vol = Mathf.Min(1f, pickupVolume + (_pickupStreak - 1) * 0.03f);

            AudioSource src = _voices[_voiceNext];
            _voiceNext = (_voiceNext + 1) % _voices.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, vol);
            _lastSfxTime = now;
        }

        private AudioClip SelectPickupClip()
        {
            if (pickupClips == null || pickupClips.Length == 0)
                return null;
            int i = UnityEngine.Random.Range(0, pickupClips.Length);
            return pickupClips[i];
        }

        private Sprite SelectSprite()
        {
            if (coinSprites == null || coinSprites.Length == 0)
                return null;

            for (int attempt = 0; attempt < coinSprites.Length; attempt++)
            {
                Sprite s = coinSprites[_spriteIndex];
                _spriteIndex = (_spriteIndex + 1) % coinSprites.Length;
                // Aléa : parfois saute pour mélanger Tals1/2/3
                if (UnityEngine.Random.value < 0.35f)
                    _spriteIndex = UnityEngine.Random.Range(0, coinSprites.Length);
                if (s != null)
                    return s;
            }

            return null;
        }

        // ═══════════════════════════════════════════
        // PRIVÉ — POOL / OVERLAY
        // ═══════════════════════════════════════════

        private void EnsureOverlay()
        {
            if (_overlay != null)
                return;

            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
                _canvas = FindObjectOfType<Canvas>();

            Transform parent = _canvas != null ? _canvas.transform : transform;
            Transform existing = parent.Find(OverlayName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasRenderer));
                go.transform.SetParent(parent, false);
            }

            _overlay = (RectTransform)go.transform;
            _overlay.SetAsLastSibling();
            StretchFull(_overlay);
            // Ne bloque pas les clics
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        private void EnsureVoices()
        {
            if (_voices != null && _voices.Length >= 8)
                return;

            _voices = new AudioSource[8];
            for (int i = 0; i < 8; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.loop = false;
                _voices[i] = src;
            }
        }

        private void ResolveTargetIfNeeded()
        {
            if (counterTarget != null)
                return;

            Transform pill = FindDeep(transform.root, "PillTals");
            if (pill == null)
            {
                GameObject found = GameObject.Find("PillTals");
                if (found != null)
                    pill = found.transform;
            }

            if (pill != null)
                counterTarget = pill as RectTransform;

            CachePunchBase();
        }

        private Vector2 ResolveTargetLocal()
        {
            ResolveTargetIfNeeded();
            if (counterTarget == null || _overlay == null)
                return Vector2.zero;

            return WorldToOverlay(counterTarget.TransformPoint(counterTarget.rect.center));
        }

        private Vector2 WorldToOverlay(Vector3 world)
        {
            if (_overlay == null)
                return Vector2.zero;

            Camera cam = null;
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = _canvas.worldCamera;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _overlay, screen, cam, out Vector2 local);
            return local;
        }

        private CoinState Acquire()
        {
            if (_pool.Count > 0)
                return _pool.Pop();

            GameObject go = new GameObject(
                "ClaimCoin",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            go.transform.SetParent(_overlay, false);

            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(CoinUiSize, CoinUiSize);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;

            return new CoinState { Rt = rt, Img = img };
        }

        private void Release(CoinState coin, int index)
        {
            _active.RemoveAt(index);
            coin.Rt.gameObject.SetActive(false);
            coin.Rt.localScale = Vector3.one;
            _pool.Push(coin);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform f = FindDeep(root.GetChild(i), name);
                if (f != null)
                    return f;
            }

            return null;
        }
    }
}
