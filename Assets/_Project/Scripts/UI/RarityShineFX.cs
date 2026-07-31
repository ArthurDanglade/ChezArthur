using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Shimmer diagonal SSR + micro-sparkles LR (Gate 5.c).
    /// Endormi hors viewport (check 4 Hz). SR : noop.
    /// </summary>
    public class RarityShineFX : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float ShimmerPeriod = 5f;
        private const float ShimmerAlpha = 0.15f;
        private const float VisibilityHz = 4f;
        private const float SparkleMinInterval = 2f;
        private const float SparkleMaxInterval = 6f;
        private const float SparkleDuration = 0.4f;
        private const int SparkleCount = 4;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private RectTransform viewport;
        [SerializeField] private Image shimmerBand;
        [SerializeField] private Image[] sparkles;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private CharacterRarity _rarity = CharacterRarity.SR;
        private float _phase;
        private float _visibilityTimer;
        private bool _visible = true;
        private bool _sleeping;
        private float _nextSparkleAt;
        private float _sparkleT = -1f;
        private int _activeSparkle;
        private RectTransform _rt;
        private Vector3[] _worldCorners = new Vector3[4];
        private Vector3[] _viewCorners = new Vector3[4];

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        public void Configure(CharacterRarity rarity, RectTransform scrollViewport)
        {
            _rarity = rarity;
            viewport = scrollViewport;
            _phase = Random.value * ShimmerPeriod;
            _nextSparkleAt = Time.unscaledTime
                             + Mathf.Lerp(SparkleMinInterval, SparkleMaxInterval, Random.value);
            _sparkleT = -1f;
            EnsureVisuals();
            ApplyRarityMode();
        }

        public void SetViewport(RectTransform scrollViewport)
        {
            viewport = scrollViewport;
        }

        // ═══════════════════════════════════════════
        // UNITY
        // ═══════════════════════════════════════════

        private void Awake()
        {
            _rt = (RectTransform)transform;
            EnsureVisuals();
        }

        private void OnEnable()
        {
            _visibilityTimer = 0f;
        }

        private void Update()
        {
            if (_rarity == CharacterRarity.SR)
                return;

            _visibilityTimer += Time.unscaledDeltaTime;
            if (_visibilityTimer >= 1f / VisibilityHz)
            {
                _visibilityTimer = 0f;
                UpdateVisibility();
            }

            if (_sleeping)
                return;

            float t = Time.unscaledTime + _phase;
            UpdateShimmer(t);

            if (_rarity == CharacterRarity.LR)
                UpdateSparkles();
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private void EnsureVisuals()
        {
            if (_rt == null)
                _rt = (RectTransform)transform;

            if (shimmerBand == null)
            {
                Transform existing = transform.Find("ShineBand");
                GameObject bandGo;
                if (existing != null)
                    bandGo = existing.gameObject;
                else
                {
                    bandGo = new GameObject(
                        "ShineBand",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));
                    bandGo.transform.SetParent(transform, false);
                }

                shimmerBand = bandGo.GetComponent<Image>();
                RectTransform brt = (RectTransform)bandGo.transform;
                brt.anchorMin = new Vector2(0f, 0f);
                brt.anchorMax = new Vector2(0f, 1f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.sizeDelta = new Vector2(28f, 0f);
                brt.anchoredPosition = Vector2.zero;
                brt.localEulerAngles = new Vector3(0f, 0f, -28f);
                shimmerBand.raycastTarget = false;
                shimmerBand.color = new Color(1f, 1f, 1f, ShimmerAlpha);
            }

            if (sparkles == null || sparkles.Length != SparkleCount)
            {
                sparkles = new Image[SparkleCount];
                for (int i = 0; i < SparkleCount; i++)
                {
                    string name = "Sparkle" + i;
                    Transform ex = transform.Find(name);
                    GameObject go;
                    if (ex != null)
                        go = ex.gameObject;
                    else
                    {
                        go = new GameObject(
                            name,
                            typeof(RectTransform),
                            typeof(CanvasRenderer),
                            typeof(Image));
                        go.transform.SetParent(transform, false);
                    }

                    sparkles[i] = go.GetComponent<Image>();
                    RectTransform srt = (RectTransform)go.transform;
                    srt.anchorMin = new Vector2(0.5f, 0.5f);
                    srt.anchorMax = new Vector2(0.5f, 0.5f);
                    srt.sizeDelta = new Vector2(8f, 8f);
                    sparkles[i].raycastTarget = false;
                    sparkles[i].color = UiTheme.AccentGold;
                    go.SetActive(false);
                }
            }
        }

        private void ApplyRarityMode()
        {
            if (shimmerBand != null)
                shimmerBand.gameObject.SetActive(_rarity == CharacterRarity.SSR || _rarity == CharacterRarity.LR);

            if (sparkles != null)
            {
                for (int i = 0; i < sparkles.Length; i++)
                {
                    if (sparkles[i] != null)
                        sparkles[i].gameObject.SetActive(false);
                }
            }

            enabled = _rarity != CharacterRarity.SR;
            if (_rarity == CharacterRarity.SR && shimmerBand != null)
                shimmerBand.gameObject.SetActive(false);
        }

        private void UpdateVisibility()
        {
            if (viewport == null || _rt == null)
            {
                _visible = true;
                _sleeping = false;
                return;
            }

            _rt.GetWorldCorners(_worldCorners);
            viewport.GetWorldCorners(_viewCorners);

            // AABB overlap simple
            float ax0 = _worldCorners[0].x;
            float ay0 = _worldCorners[0].y;
            float ax1 = _worldCorners[2].x;
            float ay1 = _worldCorners[2].y;
            float bx0 = _viewCorners[0].x;
            float by0 = _viewCorners[0].y;
            float bx1 = _viewCorners[2].x;
            float by1 = _viewCorners[2].y;

            _visible = ax1 >= bx0 && ax0 <= bx1 && ay1 >= by0 && ay0 <= by1;
            _sleeping = !_visible;

            if (shimmerBand != null && (_rarity == CharacterRarity.SSR || _rarity == CharacterRarity.LR))
                shimmerBand.enabled = _visible;
        }

        private void UpdateShimmer(float time)
        {
            if (shimmerBand == null || !_visible)
                return;

            RectTransform brt = (RectTransform)shimmerBand.transform;
            float w = _rt.rect.width;
            if (w < 1f)
                w = 100f;
            float u = Mathf.Repeat(time / ShimmerPeriod, 1f);
            float x = Mathf.Lerp(-w * 0.2f, w * 1.2f, u);
            brt.anchoredPosition = new Vector2(x - w * 0.5f, 0f);

            Color c = Color.white;
            c.a = ShimmerAlpha * (0.65f + 0.35f * Mathf.Sin(u * Mathf.PI));
            shimmerBand.color = c;
        }

        private void UpdateSparkles()
        {
            if (sparkles == null)
                return;

            if (_sparkleT < 0f)
            {
                if (Time.unscaledTime < _nextSparkleAt)
                    return;

                _activeSparkle = Random.Range(0, sparkles.Length);
                Image sp = sparkles[_activeSparkle];
                if (sp == null)
                    return;

                RectTransform srt = (RectTransform)sp.transform;
                float hw = _rt.rect.width * 0.35f;
                float hh = _rt.rect.height * 0.35f;
                srt.anchoredPosition = new Vector2(
                    Random.Range(-hw, hw),
                    Random.Range(-hh, hh));
                srt.localScale = Vector3.zero;
                sp.gameObject.SetActive(true);
                _sparkleT = 0f;
            }

            _sparkleT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_sparkleT / SparkleDuration);
            float scale = k < 0.5f
                ? Mathf.Lerp(0f, 1f, k * 2f)
                : Mathf.Lerp(1f, 0f, (k - 0.5f) * 2f);

            Image active = sparkles[_activeSparkle];
            if (active != null)
            {
                active.transform.localScale = new Vector3(scale, scale, 1f);
                Color c = UiTheme.AccentGold;
                c.a = scale;
                active.color = c;
            }

            if (k >= 1f)
            {
                if (active != null)
                    active.gameObject.SetActive(false);
                _sparkleT = -1f;
                _nextSparkleAt = Time.unscaledTime
                                 + Mathf.Lerp(SparkleMinInterval, SparkleMaxInterval, Random.value);
            }
        }
    }
}
