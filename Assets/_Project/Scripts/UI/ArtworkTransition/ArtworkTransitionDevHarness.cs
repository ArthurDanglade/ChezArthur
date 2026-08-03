using UnityEngine;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>
    /// Harness de test ContextMenu (dormant) — textures procédurales 96×128.
    /// Aucune scène committée ; Find Canvas uniquement au moment du ContextMenu.
    /// </summary>
    public class ArtworkTransitionDevHarness : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int ArtW = 96;
        private const int ArtH = 128;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Prefab stage (ArtworkTransitionStage)")]
        [SerializeField] private GameObject stagePrefab;

        [Header("Driver (auto si stage instancié)")]
        [SerializeField] private ArtworkTransitionDriver driver;

        [Header("Portraits optionnels (sinon procéduraux)")]
        [SerializeField] private Texture2D overridePrime;
        [SerializeField] private Texture2D overrideDechu;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private GameObject _stageInstance;
        private Texture2D _procPrime;
        private Texture2D _procDechu;
        private Texture2D _procPrimeDim;
        private Sprite _flipA;
        private Sprite _flipB;
        private Sprite _flipDechu;

        // ═══════════════════════════════════════════
        // CONTEXT MENUS
        // ═══════════════════════════════════════════

        [ContextMenu("Jouer Déchéance (test)")]
        private void CtxPlayDecheance()
        {
            EnsureStage();
            EnsureProceduralTextures();
            var prime = MakeStatic(overridePrime != null ? overridePrime : _procPrime);
            var dechu = MakeStatic(overrideDechu != null ? overrideDechu : _procDechu);
            driver.PlayDecheance(prime, dechu, null);
        }

        [ContextMenu("Jouer Ascension (test)")]
        private void CtxPlayAscension()
        {
            EnsureStage();
            EnsureProceduralTextures();
            var prime = MakeStatic(overridePrime != null ? overridePrime : _procPrime);
            var dechu = MakeStatic(overrideDechu != null ? overrideDechu : _procDechu);
            driver.PlayAscension(prime, dechu, null);
        }

        [ContextMenu("Jouer Déchéance (flipbook test)")]
        private void CtxPlayDecheanceFlipbook()
        {
            EnsureStage();
            EnsureProceduralTextures();
            EnsureFlipbookSprites();

            var primeFlip = new SimpleFlipbookSource(new[] { _flipA, _flipB }, 2f);
            var dechu = new StaticPortraitSource(_flipDechu);
            driver.PlayDecheance(primeFlip, dechu, null);
        }

        [ContextMenu("Set t = 1.2 s")]
        private void CtxSetTime()
        {
            EnsureStage();
            if (driver != null)
                driver.SetTime(1.2f);
        }

        [ContextMenu("Skip")]
        private void CtxSkip()
        {
            EnsureStage();
            if (driver != null)
                driver.SkipToEnd();
        }

        // ═══════════════════════════════════════════
        // SETUP
        // ═══════════════════════════════════════════

        private void EnsureStage()
        {
            if (driver != null)
                return;

            if (_stageInstance == null)
            {
                if (stagePrefab == null)
                {
                    Debug.LogError("[ArtworkTransitionDevHarness] stagePrefab non assigné.");
                    return;
                }

                Canvas canvas = FindFirstCanvas();
                if (canvas == null)
                {
                    Debug.LogError("[ArtworkTransitionDevHarness] Aucun Canvas trouvé.");
                    return;
                }

                _stageInstance = Instantiate(stagePrefab, canvas.transform, false);
                _stageInstance.name = "ArtworkTransitionStage_DEV";
            }

            driver = _stageInstance.GetComponent<ArtworkTransitionDriver>();
            if (driver == null)
                driver = _stageInstance.GetComponentInChildren<ArtworkTransitionDriver>(true);

            if (driver == null)
                Debug.LogError("[ArtworkTransitionDevHarness] Driver introuvable sur le stage.");
        }

        private static Canvas FindFirstCanvas()
        {
            // Uniquement au ContextMenu — jamais dans Update
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

        private static StaticPortraitSource MakeStatic(Texture2D tex) =>
            new StaticPortraitSource(tex);

        // ═══════════════════════════════════════════
        // TEXTURES PROCÉDURALES
        // ═══════════════════════════════════════════

        private void EnsureProceduralTextures()
        {
            if (_procPrime == null)
                _procPrime = BuildPortrait(vibrant: true, diamondLit: true);
            if (_procDechu == null)
                _procDechu = BuildPortrait(vibrant: false, diamondLit: true);
            if (_procPrimeDim == null)
                _procPrimeDim = BuildPortrait(vibrant: true, diamondLit: false);
        }

        private void EnsureFlipbookSprites()
        {
            EnsureProceduralTextures();
            if (_flipA == null)
                _flipA = Sprite.Create(_procPrime, new Rect(0, 0, ArtW, ArtH), new Vector2(0.5f, 0.5f), 100f);
            if (_flipB == null)
                _flipB = Sprite.Create(_procPrimeDim, new Rect(0, 0, ArtW, ArtH), new Vector2(0.5f, 0.5f), 100f);
            if (_flipDechu == null)
                _flipDechu = Sprite.Create(_procDechu, new Rect(0, 0, ArtW, ArtH), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// 96×128 Point : fond dégradé sombre + cadre 2 px + losange central
        /// (or vive / désaturé sombre).
        /// </summary>
        private static Texture2D BuildPortrait(bool vibrant, bool diamondLit)
        {
            var tex = new Texture2D(ArtW, ArtH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = vibrant
                ? (diamondLit ? "AW1_ProcPrime" : "AW1_ProcPrimeDim")
                : "AW1_ProcDechu";

            Color top = vibrant
                ? new Color(0.18f, 0.10f, 0.28f)
                : new Color(0.08f, 0.06f, 0.10f);
            Color bot = vibrant
                ? new Color(0.06f, 0.04f, 0.12f)
                : new Color(0.03f, 0.02f, 0.05f);

            Color frame = vibrant
                ? AwPalette.RimGold
                : new Color(0.35f, 0.28f, 0.40f);

            Color diamond = diamondLit
                ? (vibrant ? AwPalette.Gold : new Color(0.40f, 0.32f, 0.45f))
                : (vibrant ? new Color(0.45f, 0.30f, 0.15f) : new Color(0.18f, 0.14f, 0.20f));

            var pixels = new Color32[ArtW * ArtH];
            for (int y = 0; y < ArtH; y++)
            {
                float v = y / (float)(ArtH - 1);
                Color bg = Color.Lerp(bot, top, v);
                for (int x = 0; x < ArtW; x++)
                {
                    Color c = bg;

                    // Cadre 2 px
                    if (x < 2 || y < 2 || x >= ArtW - 2 || y >= ArtH - 2)
                        c = frame;

                    // Losange central
                    float cx = (x + 0.5f) / ArtW - 0.5f;
                    float cy = (y + 0.5f) / ArtH - 0.5f;
                    float d = Mathf.Abs(cx) * ArtW / 28f + Mathf.Abs(cy) * ArtH / 36f;
                    if (d < 1f)
                        c = Color.Lerp(diamond, c, d * d);

                    pixels[y * ArtW + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false); // readable CPU pour cache Driver
            return tex;
        }

        // ═══════════════════════════════════════════
        // CLEANUP
        // ═══════════════════════════════════════════

        private void OnDestroy()
        {
            DestroyTex(ref _procPrime);
            DestroyTex(ref _procDechu);
            DestroyTex(ref _procPrimeDim);
        }

        private static void DestroyTex(ref Texture2D tex)
        {
            if (tex == null) return;
            if (Application.isPlaying) Destroy(tex);
            else DestroyImmediate(tex);
            tex = null;
        }
    }
}
