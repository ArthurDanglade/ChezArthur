using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Characters;

namespace ChezArthur.UI.InvocationFlow
{
    /// <summary>
    /// Harness de test ContextMenu (dormant) — spawn sous le premier Canvas au moment du menu uniquement.
    /// </summary>
    public class InvocationFlowDevHarness : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private static readonly int[] PixelStepLevels = { 6, 12, 22, 40, 72, 140, 4096 };
        private static readonly int PixelStepsId = Shader.PropertyToID("_PixelSteps");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private const int ArtW = 96;
        private const int ArtH = 128;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Prefabs (assignés par le builder ou manuellement)")]
        [SerializeField] private GameObject veilPrefab;
        [SerializeField] private GameObject rarityLayerPrefab;
        [SerializeField] private GameObject bannerPrefab;

        [Header("Config")]
        [SerializeField] private InvocationFlowConfig config;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private PixelVeilController _veil;
        private RevealRarityLayer _rarity;
        private RevealBannerUI _banner;
        private RawImage _testArt;
        private Material _pixelMat;
        private Texture2D _procTex;
        private GameObject _host;
        private Coroutine _appearRoutine;

        // ═══════════════════════════════════════════
        // CONTEXT MENUS
        // ═══════════════════════════════════════════

        [ContextMenu("Voile : couvrir/découvrir")]
        private void CtxVeilCoverUncover()
        {
            EnsureHost();
            EnsureVeil();
            if (_veil == null)
                return;

            _veil.Cover(
                onPeak: () => Debug.Log("[INV1 Harness] Voile onPeak"),
                onDone: () =>
                {
                    _veil.Uncover(() => Debug.Log("[INV1 Harness] Voile découvert"));
                });
        }

        [ContextMenu("Apparition SR (texture test)")]
        private void CtxAppearSR() => StartAppear(CharacterRarity.SR);

        [ContextMenu("Apparition SSR (texture test)")]
        private void CtxAppearSSR() => StartAppear(CharacterRarity.SSR);

        [ContextMenu("Apparition LR (texture test)")]
        private void CtxAppearLR() => StartAppear(CharacterRarity.LR);

        [ContextMenu("Bandeau plein")]
        private void CtxBannerFull()
        {
            EnsureHost();
            EnsureBanner();
            if (_banner == null)
                return;

            _banner.PlayFull(
                "Kael",
                CharacterRarity.SSR,
                3,
                new[] { 580, 164, 103, 89 },
                isNew: true,
                xp01: 0.65f);
        }

        [ContextMenu("Bandeau compact")]
        private void CtxBannerCompact()
        {
            EnsureHost();
            EnsureBanner();
            if (_banner == null)
                return;

            _banner.PlayCompact(
                "Nyra",
                CharacterRarity.SSR,
                niveauAvant: 4,
                niveauAprès: 5,
                xp01: 0.85f);
        }

        // ═══════════════════════════════════════════
        // SETUP
        // ═══════════════════════════════════════════

        private void StartAppear(CharacterRarity rarity)
        {
            EnsureHost();
            EnsureVeil();
            EnsureRarity();
            EnsureTestArt();

            if (_appearRoutine != null)
                StopCoroutine(_appearRoutine);
            _appearRoutine = StartCoroutine(AppearRoutine(rarity));
        }

        private IEnumerator AppearRoutine(CharacterRarity rarity)
        {
            if (_rarity != null)
                _rarity.ResetVisuals();

            SetPixelStep(PixelStepLevels[0]);
            ApplyArtBrightness(0.55f);

            // Voile couvre → swap art au peak → découvre
            bool peakReached = false;
            bool coverDone = false;
            if (_veil != null)
            {
                _veil.Cover(
                    onPeak: () =>
                    {
                        peakReached = true;
                        SetPixelStep(PixelStepLevels[0]);
                        ApplyArtBrightness(0.55f);
                    },
                    onDone: () => { coverDone = true; });

                while (!coverDone)
                    yield return null;

                bool uncoverDone = false;
                _veil.Uncover(() => uncoverDone = true);
                while (!uncoverDone)
                    yield return null;
            }
            else
            {
                peakReached = true;
            }

            if (!peakReached)
                yield break;

            float montee = config != null ? config.monteeDuration : 0.35f;
            if (_rarity != null)
                _rarity.PlayMontee(rarity, montee);
            yield return new WaitForSecondsRealtime(montee);

            float resolve = config != null
                ? config.GetResolveDuration(rarity)
                : (rarity == CharacterRarity.SR ? 1.6f : 2.4f);

            float t = 0f;
            while (t < resolve)
            {
                t += Time.unscaledDeltaTime;
                float u = resolve > 0f ? Mathf.Clamp01(t / resolve) : 1f;
                int idx = Mathf.Min(PixelStepLevels.Length - 1,
                    Mathf.FloorToInt(u * PixelStepLevels.Length));
                SetPixelStep(PixelStepLevels[idx]);
                if (_rarity != null)
                {
                    _rarity.ApplyResolve(rarity, u);
                    ApplyArtBrightness(_rarity.ResolveBrightness);
                }

                yield return null;
            }

            SetPixelStep(4096);
            if (_rarity != null)
            {
                _rarity.ApplyResolve(rarity, 1f);
                _rarity.Punch(rarity);
            }

            yield return new WaitForSecondsRealtime(0.22f);
            _appearRoutine = null;
            Debug.Log($"[INV1 Harness] Apparition {rarity} terminée");
        }

        private void EnsureHost()
        {
            if (_host != null)
                return;

            Canvas canvas = FindFirstCanvas();
            if (canvas == null)
            {
                Debug.LogError("[InvocationFlowDevHarness] Aucun Canvas trouvé.");
                return;
            }

            _host = new GameObject("InvocationFlow_DEV", typeof(RectTransform));
            RectTransform rt = _host.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            StretchFull(rt);
        }

        private void EnsureVeil()
        {
            if (_veil != null || _host == null)
                return;

            if (veilPrefab != null)
            {
                GameObject go = Instantiate(veilPrefab, _host.transform, false);
                go.name = "PixelVeilOverlay_DEV";
                go.SetActive(true);
                _veil = go.GetComponent<PixelVeilController>();
                if (_veil != null && config != null)
                    WireConfig(_veil, "config", config);
            }
            else
            {
                Debug.LogWarning("[InvocationFlowDevHarness] veilPrefab non assigné.");
            }
        }

        private void EnsureRarity()
        {
            if (_rarity != null || _host == null)
                return;

            if (rarityLayerPrefab != null)
            {
                GameObject go = Instantiate(rarityLayerPrefab, _host.transform, false);
                go.name = "RevealRarityLayer_DEV";
                go.SetActive(true);
                _rarity = go.GetComponent<RevealRarityLayer>();
                if (_rarity != null && config != null)
                    WireConfig(_rarity, "config", config);
            }
            else
            {
                Debug.LogWarning("[InvocationFlowDevHarness] rarityLayerPrefab non assigné.");
            }
        }

        private void EnsureBanner()
        {
            if (_banner != null || _host == null)
                return;

            if (bannerPrefab != null)
            {
                GameObject go = Instantiate(bannerPrefab, _host.transform, false);
                go.name = "RevealBanner_DEV";
                go.SetActive(true);
                _banner = go.GetComponent<RevealBannerUI>();
                if (_banner != null && config != null)
                    WireConfig(_banner, "config", config);
            }
            else
            {
                Debug.LogWarning("[InvocationFlowDevHarness] bannerPrefab non assigné.");
            }
        }

        private void EnsureTestArt()
        {
            if (_testArt != null || _host == null)
                return;

            EnsureProceduralTexture();

            RectTransform artRt = CreateChild(_host.transform as RectTransform, "TestArt");
            artRt.anchorMin = new Vector2(0.5f, 0.5f);
            artRt.anchorMax = new Vector2(0.5f, 0.5f);
            artRt.pivot = new Vector2(0.5f, 0.5f);
            artRt.sizeDelta = new Vector2(297.6f, 396.8f);
            artRt.SetAsFirstSibling();

            _testArt = artRt.gameObject.AddComponent<RawImage>();
            _testArt.texture = _procTex;
            _testArt.raycastTarget = false;

            Shader px = Shader.Find("ChezArthur/UI/GachaRevealPixelate");
            if (px != null)
            {
                _pixelMat = new Material(px);
                _pixelMat.SetFloat(PixelStepsId, PixelStepLevels[0]);
                _pixelMat.SetFloat(SaturationId, 1f);
                _testArt.material = _pixelMat;
            }
        }

        private void EnsureProceduralTexture()
        {
            if (_procTex != null)
                return;

            _procTex = new Texture2D(ArtW, ArtH, TextureFormat.RGBA32, false);
            _procTex.filterMode = FilterMode.Point;
            _procTex.wrapMode = TextureWrapMode.Clamp;
            _procTex.name = "INV1_ProcPortrait";

            for (int y = 0; y < ArtH; y++)
            {
                float v = y / (float)(ArtH - 1);
                for (int x = 0; x < ArtW; x++)
                {
                    float u = x / (float)(ArtW - 1);
                    Color c = Color.Lerp(
                        new Color(0.12f, 0.08f, 0.22f),
                        new Color(0.35f, 0.22f, 0.45f),
                        v);
                    // Losange central
                    float dx = Mathf.Abs(u - 0.5f) * 2f;
                    float dy = Mathf.Abs(v - 0.5f) * 2f;
                    if (dx + dy < 0.55f)
                        c = new Color(0.85f, 0.70f, 0.35f);
                    // Cadre 2 px
                    if (x < 2 || y < 2 || x >= ArtW - 2 || y >= ArtH - 2)
                        c = new Color(0.90f, 0.78f, 0.40f);
                    _procTex.SetPixel(x, y, c);
                }
            }

            _procTex.Apply(false, false);
        }

        private void SetPixelStep(int steps)
        {
            if (_pixelMat != null)
                _pixelMat.SetFloat(PixelStepsId, steps);
        }

        private void ApplyArtBrightness(float bright)
        {
            if (_testArt == null)
                return;
            Color c = _testArt.color;
            c.r = c.g = c.b = Mathf.Clamp01(bright);
            c.a = 1f;
            _testArt.color = c;
        }

        private void OnDestroy()
        {
            if (_pixelMat != null)
            {
                if (Application.isPlaying)
                    Destroy(_pixelMat);
                else
                    DestroyImmediate(_pixelMat);
            }

            if (_procTex != null)
            {
                if (Application.isPlaying)
                    Destroy(_procTex);
                else
                    DestroyImmediate(_procTex);
            }
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static Canvas FindFirstCanvas()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            if (canvases == null || canvases.Length == 0)
                return null;

            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].isActiveAndEnabled)
                    return canvases[i];
            }

            return canvases[0];
        }

        private static RectTransform CreateChild(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void WireConfig(Object target, string field, Object value)
        {
            // Reflection légère hors hot-path (ContextMenu uniquement)
            var soType = typeof(UnityEngine.Object);
            var flags = System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Public;
            var fi = target.GetType().GetField(field, flags);
            if (fi != null && soType.IsAssignableFrom(fi.FieldType))
                fi.SetValue(target, value);
        }
    }
}
