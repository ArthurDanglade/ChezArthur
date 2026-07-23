using System;
using System.Collections;
using ChezArthur.Characters;
using ChezArthur.Hub;
using ChezArthur.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Séquence train gacha : arrivée hors-écran, porte, lumière, fumée, skip.
    /// </summary>
    public class TrainSequenceController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float SMOKE_INTENSITY_SR = 1f;
        private const float SMOKE_INTENSITY_SSR = 1.8f;
        private const float SMOKE_INTENSITY_LR = 2.4f;
        private const float SMOKE_PEAK_ALPHA = 0.94f;
        private const float SMOKE_FADE_IN_RATIO = 0.42f;
        private const float GLOW_PULSE_AMPLITUDE = 0.08f;
        private const float GLOW_PULSE_SPEED = 3.2f;
        private const float GLOW_SCALE_SSR_LR = 1.15f;
        private const float DOOR_HEIGHT_RATIO = 0.88f;
        private const float DOOR_ASPECT = 228f / 342f;
        private const float REF_HEIGHT = 1920f;
        private const float DOOR_SMOKE_DURATION = 0.55f;
        private const float TRAIN_HEIGHT_RATIO = 0.68f;
        private const float TRAIN_ANCHOR_Y = 0.22f;
        private const float GLOW_HOLD_BEFORE_DOOR = 0.85f;
        private const float RARITY_GLOW_DURATION = 1.65f;
        private const float CONTRE_JOUR_DURATION = 1.35f;
        private const float CONTRE_JOUR_COVER_RATIO = 0.38f;
        private const float CONTRE_JOUR_HOLD = 0.12f;
        private const float CONTRE_JOUR_REVEAL_FADE = 0.9f;
        /// <summary> Centre des portes sur train_side_sprite (0–1, gauche→droite, asset non flippé). </summary>
        private const float TRAIN_DOOR_CENTER_NORM_X = 0.455f;
        private const float DOOR_OPEN_DURATION_SCALE = 1.85f;
        private static readonly int ContreJourId = Shader.PropertyToID("_ContreJour");
        private static readonly int LightColorId = Shader.PropertyToID("_LightColor");

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Scènes")]
        [SerializeField] private GameObject trainScene;
        [SerializeField] private RectTransform trainSprite;
        [SerializeField] private RectTransform trainMask;
        [SerializeField] private GameObject doorScene;
        [SerializeField] private PortraitAnimator doorView;
        [SerializeField] private RawImage doorRawImage;
        [SerializeField] private RectTransform doorViewRect;
        [SerializeField] private Image rarityGlow;
        [SerializeField] private Image wagonInterior;
        [SerializeField] private Button[] tapButtons;

        [Header("Parallax Hub (fond arrivée train)")]
        [Tooltip("LandscapeLayer de PageAccueil — emprunté seulement pendant l'arrivée.")]
        [SerializeField] private ParallaxManager hubParallax;

        [Header("Assets")]
        [SerializeField] private TrainCurveData departCurve;
        [SerializeField] private AnimatedPortraitData doorFlipbook;

        [Header("Fumée / contre-jour")]
        [SerializeField] private Image smokeTransition;
        [SerializeField] private float smokeCoverDuration = 1.05f;
        [Tooltip("Flash contre-jour plein écran (créé runtime si vide).")]
        [SerializeField] private Image contreJourFlash;
        [Tooltip("Matériau ChezArthur/UI/GachaDoorContreJour (instance runtime).")]
        [SerializeField] private Material doorContreJourMaterial;

        [Header("Audio")]
        [SerializeField] private AudioClip trainArriveClip;
        [Tooltip("Joué quand les portes du flipbook commencent à s'ouvrir.")]
        [SerializeField] private AudioClip doorClip;
        [Tooltip("Joué quand le glow de rareté apparaît autour de la porte.")]
        [SerializeField] private AudioClip rarityGlowClip;

        [Header("Timing")]
        [SerializeField] private float arriveOvershootPx;
        [SerializeField] private float doorLightFadeDuration = 0.8f;
        [SerializeField] private float lightHoldDuration = 0.6f;

        [Header("Glow alpha par rareté")]
        [SerializeField] private float glowAlphaSR = 0.35f;
        [SerializeField] private float glowAlphaSSR = 0.75f;
        [SerializeField] private float glowAlphaLR = 0.9f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _skipRequested;
        private Texture2D _doorSheetTexture;
        private float _trainRestX;
        private float _trainRestY;
        private float _motionScale = 1f;
        private Vector3 _smokeBaseScale = Vector3.one;
        private bool _smokeBaseCaptured;
        private Sprite _runtimeSmokeSprite;
        private Image _trainBackdrop;
        private Material _doorContreJourInstance;
        private float _trainDoorAlignX;
        private Image _doorBloom;
        private Material _doorBloomMatInstance;
        private bool _parallaxBorrowed;
        private Transform _parallaxOriginalParent;
        private int _parallaxOriginalSibling;
        private Vector2 _parallaxOrigAnchorMin;
        private Vector2 _parallaxOrigAnchorMax;
        private Vector2 _parallaxOrigOffsetMin;
        private Vector2 _parallaxOrigOffsetMax;
        private Vector2 _parallaxOrigSizeDelta;
        private Vector2 _parallaxOrigAnchoredPos;
        private Vector3 _parallaxOrigScale;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public GameObject TrainScene => trainScene;
        public GameObject DoorScene => doorScene;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Séquence train. onCoveredPrepare tourne SOUS le voile opaque (artwork prêt avant fondu).
        /// </summary>
        public IEnumerator PlaySequence(
            CharacterRarity bestRarity,
            System.Func<IEnumerator> onCoveredPrepare)
        {
            _skipRequested = false;
            PreparePremiumLayout();
            EnsureSmokeDrawable();
            EnsureContreJourFlash();
            SetTapListeners(true);

            yield return PlayArrival();

            RestoreHubParallax();

            if (!_skipRequested)
                yield return PlayDoorOpen(bestRarity);

            yield return PlayContreJourHandoff(bestRarity, onCoveredPrepare, true);
            SetTapListeners(false);
        }

        /// <summary> Compat : Action simple au pic (sans préparer l'artwork sous voile). </summary>
        public IEnumerator PlaySequence(CharacterRarity bestRarity, Action onSkipToReveal)
        {
            yield return PlaySequence(
                bestRarity,
                () => InvokeCoverAction(onSkipToReveal));
        }

        private static IEnumerator InvokeCoverAction(Action action)
        {
            action?.Invoke();
            yield break;
        }

        public void ReleaseDoorSheet()
        {
            RestoreHubParallax();

            if (_doorSheetTexture != null)
            {
                PortraitLoader.Release(_doorSheetTexture);
                _doorSheetTexture = null;
            }

            if (doorRawImage != null)
            {
                doorRawImage.material = null;
                doorRawImage.texture = null;
                doorRawImage.enabled = false;
            }

            if (doorView != null)
                doorView.PlayStatic();
        }

        public void HideSequenceScenes()
        {
            RestoreHubParallax();

            if (trainScene != null)
                trainScene.SetActive(false);
            if (doorScene != null)
                doorScene.SetActive(false);
        }

        /// <summary>
        /// Remet le LandscapeLayer Hub à sa place (safe à appeler plusieurs fois).
        /// </summary>
        public void RestoreHubParallax()
        {
            if (!_parallaxBorrowed || hubParallax == null)
                return;

            hubParallax.EndGachaBorrow();

            RectTransform rt = hubParallax.RootRect;
            if (rt != null && _parallaxOriginalParent != null)
            {
                rt.SetParent(_parallaxOriginalParent, false);
                rt.SetSiblingIndex(_parallaxOriginalSibling);
                rt.anchorMin = _parallaxOrigAnchorMin;
                rt.anchorMax = _parallaxOrigAnchorMax;
                rt.offsetMin = _parallaxOrigOffsetMin;
                rt.offsetMax = _parallaxOrigOffsetMax;
                rt.sizeDelta = _parallaxOrigSizeDelta;
                rt.anchoredPosition = _parallaxOrigAnchoredPos;
                rt.localScale = _parallaxOrigScale;
            }

            _parallaxBorrowed = false;
        }

        // ═══════════════════════════════════════════
        // LAYOUT PREMIUM
        // ═══════════════════════════════════════════

        private void PreparePremiumLayout()
        {
            // Toujours réappliquer train / porte (scale + flip) — pas de cache figé.
            EnsureTrainSceneBackdrop();
            PrepareTrainSpriteLayout();

            // Wagon : cover plein écran (plus de letterbox blanc).
            if (wagonInterior != null)
            {
                wagonInterior.preserveAspect = false;
                wagonInterior.color = Color.white;
                RectTransform wrt = wagonInterior.rectTransform;
                wrt.anchorMin = Vector2.zero;
                wrt.anchorMax = Vector2.one;
                wrt.offsetMin = Vector2.zero;
                wrt.offsetMax = Vector2.zero;
            }

            // Paysage : plein cadre (évite bandes + bleed Hub).
            if (doorScene != null)
            {
                Transform landscape = doorScene.transform.Find("LandscapeLayer");
                if (landscape != null)
                {
                    RectTransform lrt = landscape as RectTransform;
                    if (lrt != null)
                    {
                        lrt.anchorMin = Vector2.zero;
                        lrt.anchorMax = Vector2.one;
                        lrt.offsetMin = Vector2.zero;
                        lrt.offsetMax = Vector2.zero;
                        lrt.sizeDelta = Vector2.zero;
                    }
                }
            }

            // Porte héros : large, ancrée bas (ouverture wagon).
            RectTransform doorRt = doorViewRect;
            if (doorRt == null && doorRawImage != null)
                doorRt = doorRawImage.rectTransform;

            if (doorRt != null)
            {
                float h = REF_HEIGHT * DOOR_HEIGHT_RATIO;
                float w = h * DOOR_ASPECT;
                doorRt.anchorMin = new Vector2(0.5f, 0.42f);
                doorRt.anchorMax = new Vector2(0.5f, 0.42f);
                doorRt.pivot = new Vector2(0.5f, 0.5f);
                doorRt.sizeDelta = new Vector2(w, h);
                doorRt.anchoredPosition = Vector2.zero;
                doorRt.localScale = Vector3.one;
            }

            if (rarityGlow != null && doorRt != null)
            {
                RectTransform glowRt = rarityGlow.rectTransform;
                glowRt.anchorMin = doorRt.anchorMin;
                glowRt.anchorMax = doorRt.anchorMax;
                glowRt.pivot = doorRt.pivot;
                glowRt.anchoredPosition = doorRt.anchoredPosition;
                glowRt.sizeDelta = doorRt.sizeDelta * 2.1f;
            }

            if (trainMask == null && trainSprite != null && trainSprite.parent != null)
                trainMask = trainSprite.parent as RectTransform;
        }

        /// <summary>
        /// Fond charbon opaque sous le train (évite le blanc Game view / Hub).
        /// </summary>
        private void EnsureTrainSceneBackdrop()
        {
            if (trainScene == null)
                return;

            if (_trainBackdrop == null)
            {
                Transform existing = trainScene.transform.Find("TrainBackdrop");
                GameObject go;
                if (existing != null)
                {
                    go = existing.gameObject;
                }
                else
                {
                    go = new GameObject(
                        "TrainBackdrop",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));
                    go.transform.SetParent(trainScene.transform, false);
                }

                go.transform.SetAsFirstSibling();
                StretchFull(go.GetComponent<RectTransform>());
                _trainBackdrop = go.GetComponent<Image>();
            }

            if (_trainBackdrop.sprite == null)
                _trainBackdrop.sprite = GetOrCreateWhiteSprite();

            _trainBackdrop.color = UiTheme.GachaStageCharcoal;
            _trainBackdrop.raycastTarget = false;
            _trainBackdrop.enabled = true;
        }

        /// <summary>
        /// Agrandit le train + flip X (nez à droite → pointe à gauche) +
        /// offset pour centrer la double porte à l'arrêt.
        /// </summary>
        private void PrepareTrainSpriteLayout()
        {
            if (trainSprite == null)
                return;

            Image img = trainSprite.GetComponent<Image>();
            float nativeW = trainSprite.sizeDelta.x;
            float nativeH = trainSprite.sizeDelta.y;
            if (img != null && img.sprite != null)
            {
                nativeW = img.sprite.rect.width;
                nativeH = img.sprite.rect.height;
            }

            if (nativeH < 1f)
                nativeH = 200f;
            if (nativeW < 1f)
                nativeW = 400f;

            float targetH = REF_HEIGHT * TRAIN_HEIGHT_RATIO;
            float scale = targetH / nativeH;

            // Nez locomotive à droite sur train_side_sprite → flip pour arriver de la droite.
            trainSprite.localScale = new Vector3(-scale, scale, 1f);
            trainSprite.anchorMin = new Vector2(0.5f, TRAIN_ANCHOR_Y);
            trainSprite.anchorMax = new Vector2(0.5f, TRAIN_ANCHOR_Y);
            trainSprite.pivot = new Vector2(0.5f, 0.5f);

            // Après flip autour du pivot centre : décaler pour que la porte soit au milieu.
            _trainDoorAlignX = (TRAIN_DOOR_CENTER_NORM_X - 0.5f) * nativeW * scale;
            trainSprite.anchoredPosition = new Vector2(_trainDoorAlignX, 0f);

            if (img != null)
            {
                img.preserveAspect = true;
                img.raycastTarget = false;
                if (img.sprite != null)
                    img.SetNativeSize();
                trainSprite.localScale = new Vector3(-scale, scale, 1f);
                trainSprite.anchoredPosition = new Vector2(_trainDoorAlignX, 0f);
            }
        }

        private Sprite GetOrCreateWhiteSprite()
        {
            if (_runtimeSmokeSprite != null)
                return _runtimeSmokeSprite;

            Texture2D tex = Texture2D.whiteTexture;
            _runtimeSmokeSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _runtimeSmokeSprite;
        }

        private void EnsureContreJourFlash()
        {
            if (contreJourFlash != null)
            {
                StretchFull(contreJourFlash.rectTransform);
                contreJourFlash.raycastTarget = false;
                contreJourFlash.material = null;
                contreJourFlash.sprite = GetOrCreateWhiteSprite();
                return;
            }

            Transform parent = transform;
            Transform existing = parent.Find("ContreJourFlash");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(
                    "ContreJourFlash",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                go.transform.SetParent(parent, false);
            }

            StretchFull(go.GetComponent<RectTransform>());
            go.transform.SetAsLastSibling();

            contreJourFlash = go.GetComponent<Image>();
            contreJourFlash.material = null;
            contreJourFlash.sprite = GetOrCreateWhiteSprite();
            Color c = UiTheme.CeremonyLight;
            c.a = 0f;
            contreJourFlash.color = c;
            contreJourFlash.raycastTarget = false;
            contreJourFlash.gameObject.SetActive(false);
        }

        private void EnsureSmokeDrawable()
        {
            if (smokeTransition == null)
                return;

            if (smokeTransition.sprite == null)
                smokeTransition.sprite = GetOrCreateWhiteSprite();

            smokeTransition.type = Image.Type.Simple;
            smokeTransition.raycastTarget = false;
            StretchFull(smokeTransition.rectTransform);
            smokeTransition.transform.SetAsLastSibling();
        }

        private static void StretchFull(RectTransform rt)
        {
            if (rt == null)
                return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        // ═══════════════════════════════════════════
        // PHASES
        // ═══════════════════════════════════════════

        private IEnumerator PlayArrival()
        {
            EnsureTrainSceneBackdrop();
            EnsureTrainMaskTransparent();

            // Fond = parallax Hub (pas le charbon / blanc).
            if (_trainBackdrop != null)
                _trainBackdrop.enabled = false;

            BorrowHubParallax();

            if (trainScene != null)
            {
                StretchFull(trainScene.GetComponent<RectTransform>());
                trainScene.transform.localScale = Vector3.one;
                trainScene.SetActive(true);
            }

            if (doorScene != null)
                doorScene.SetActive(false);

            PreloadDoorSheet();
            ComputeTrainMotionScale();

            PlaySfx(trainArriveClip);

            if (departCurve == null || trainSprite == null)
            {
                Debug.LogError(
                    "[TrainSequence] departCurve ou trainSprite manquant — arrivée sautée.",
                    this);
                yield break;
            }

            float duration = departCurve.Duration;
            if (duration <= 0f)
                yield break;

            // Courbe inversée Duration→0 : freinage. Parallax suit la même allure.
            float t = duration;
            while (t > 0f)
            {
                if (_skipRequested)
                    yield break;

                t -= Time.unscaledDeltaTime;
                if (t < 0f)
                    t = 0f;

                float progress = 1f - (t / duration); // 0 départ → 1 arrêt
                float speedMul = 1f - (progress * progress); // freinage doux
                if (hubParallax != null && _parallaxBorrowed)
                    hubParallax.SetSpeedMultiplier(speedMul);

                ApplyTrainX(departCurve.EvaluateOffset(t));
                yield return null;
            }

            ApplyTrainX(0f);
            if (hubParallax != null && _parallaxBorrowed)
            {
                hubParallax.SetSpeedMultiplier(0f);
                hubParallax.SetScrolling(false);
            }
        }

        /// <summary>
        /// TrainMask avait une Image blanche opaque plein écran → masquait le parallax.
        /// </summary>
        private void EnsureTrainMaskTransparent()
        {
            if (trainMask == null)
                return;

            Image maskImage = trainMask.GetComponent<Image>();
            if (maskImage == null)
                return;

            Color c = maskImage.color;
            c.a = 0f;
            maskImage.color = c;
            maskImage.raycastTarget = false;
        }

        private void BorrowHubParallax()
        {
            ResolveHubParallaxIfNeeded();
            if (hubParallax == null || trainScene == null || _parallaxBorrowed)
                return;

            RectTransform rt = hubParallax.RootRect;
            if (rt == null)
                return;

            _parallaxOriginalParent = rt.parent;
            _parallaxOriginalSibling = rt.GetSiblingIndex();
            _parallaxOrigAnchorMin = rt.anchorMin;
            _parallaxOrigAnchorMax = rt.anchorMax;
            _parallaxOrigOffsetMin = rt.offsetMin;
            _parallaxOrigOffsetMax = rt.offsetMax;
            _parallaxOrigSizeDelta = rt.sizeDelta;
            _parallaxOrigAnchoredPos = rt.anchoredPosition;
            _parallaxOrigScale = rt.localScale;

            rt.SetParent(trainScene.transform, false);
            rt.SetAsFirstSibling();
            StretchFull(rt);

            // Force le LandscapeLayer et ses RawImage actifs / visibles.
            EnsureParallaxLayersVisible(hubParallax);

            hubParallax.BeginGachaBorrow();
            _parallaxBorrowed = true;
        }

        private static void EnsureParallaxLayersVisible(ParallaxManager parallax)
        {
            if (parallax == null)
                return;

            parallax.gameObject.SetActive(true);
            RawImage[] images = parallax.GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null)
                    continue;
                images[i].enabled = true;
                images[i].gameObject.SetActive(true);
                Color c = images[i].color;
                if (c.a < 0.05f)
                {
                    c.a = 1f;
                    images[i].color = c;
                }
            }
        }

        private void ResolveHubParallaxIfNeeded()
        {
            if (hubParallax != null)
                return;

            // PageAccueil / LandscapeLayer (pas le LandscapeLayer de DoorScene).
            Transform canvas = transform.parent;
            if (canvas == null)
                return;

            Transform accueil = canvas.Find("PageAccueil");
            if (accueil == null)
                return;

            Transform landscape = accueil.Find("LandscapeLayer");
            if (landscape == null)
                return;

            hubParallax = landscape.GetComponent<ParallaxManager>();
        }

        private void ComputeTrainMotionScale()
        {
            if (trainSprite == null || departCurve == null)
                return;

            _trainRestY = trainSprite.anchoredPosition.y;
            _trainRestX = _trainDoorAlignX;

            float authorMax = Mathf.Max(1f, departCurve.MaxOffset);
            float maskW = 1080f;
            if (trainMask != null)
                maskW = Mathf.Max(maskW, trainMask.rect.width);

            float trainW = trainSprite.rect.width * Mathf.Abs(trainSprite.localScale.x);
            if (trainW < 1f)
                trainW = departCurve.SpriteWidthPx;

            // Trajet : entièrement hors écran à droite → repos (porte centrée).
            float travelUi = maskW * 0.55f + trainW * 0.55f + arriveOvershootPx;
            _motionScale = travelUi / authorMax;

            ApplyTrainX(authorMax);
        }

        private void ApplyTrainX(float authorOffset)
        {
            if (trainSprite == null)
                return;

            Vector2 pos = trainSprite.anchoredPosition;
            pos.x = _trainRestX + authorOffset * _motionScale;
            pos.y = _trainRestY;
            trainSprite.anchoredPosition = pos;
        }

        private IEnumerator PlayDoorOpen(CharacterRarity bestRarity)
        {
            if (trainScene != null)
                trainScene.SetActive(false);
            if (doorScene != null)
                doorScene.SetActive(true);

            EnsureDoorContreJourMaterial();
            EnsureDoorBloom();
            SetContreJourIntensity(0f);

            if (rarityGlow != null)
            {
                Color c = CharacterRarityPalette.GetColor(bestRarity);
                c.a = 0f;
                rarityGlow.color = c;
                rarityGlow.gameObject.SetActive(true);
                rarityGlow.transform.localScale = Vector3.one * 1.15f;
            }

            if (_doorBloom != null)
            {
                Color bc = UiTheme.CeremonyLight;
                bc.a = 0f;
                _doorBloom.color = bc;
                _doorBloom.gameObject.SetActive(true);
                _doorBloom.transform.localScale = Vector3.one * 1.6f;
            }

            // Afficher la porte FERMÉE (frame 0) avant tout SFX / anim.
            if (doorRawImage != null)
            {
                doorRawImage.enabled = true;
                if (_doorContreJourInstance != null)
                    doorRawImage.material = _doorContreJourInstance;
                if (_doorSheetTexture != null)
                    doorRawImage.texture = _doorSheetTexture;
            }

            if (doorView != null && doorFlipbook != null)
            {
                doorView.Initialize(doorRawImage);
                // Pose sur la première frame sans lancer l'ouverture.
                doorView.PlayAnimatedOnce(doorFlipbook, DOOR_OPEN_DURATION_SCALE);
                // Freeze immédiat sur frame 0 en désactivant jusqu'au glow.
                doorView.enabled = false;
            }

            // ── 1) Glow rareté (plus long) + son ──
            PlaySfx(rarityGlowClip);
            yield return RampDoorGlow(bestRarity);

            if (_skipRequested)
                yield break;

            // Petite respiration entre glow et ouverture.
            float hold = GLOW_HOLD_BEFORE_DOOR;
            float held = 0f;
            while (held < hold)
            {
                if (_skipRequested)
                    yield break;
                held += Time.unscaledDeltaTime;
                yield return null;
            }

            // ── 2) Ouverture portes + son ──
            PlaySfx(doorClip);

            if (doorView != null && doorFlipbook != null)
            {
                doorView.enabled = true;
                doorView.PlayAnimatedOnce(doorFlipbook, DOOR_OPEN_DURATION_SCALE);

                while (!doorView.HasFinishedOneShot)
                {
                    if (_skipRequested)
                        yield break;

                    float p = doorView.OneShotProgress;
                    float dazzle = 0f;
                    if (p > 0.2f)
                    {
                        float u = (p - 0.2f) / 0.8f;
                        dazzle = u * u * (3f - 2f * u);
                    }

                    SetContreJourIntensity(dazzle);
                    yield return null;
                }
            }
            else
            {
                float fake = 0f;
                while (fake < 1f)
                {
                    if (_skipRequested)
                        yield break;
                    fake += Time.unscaledDeltaTime / 1.1f;
                    SetContreJourIntensity(Mathf.Clamp01(fake));
                    yield return null;
                }
            }

            SetContreJourIntensity(1f);
        }

        private void EnsureDoorContreJourMaterial()
        {
            if (_doorContreJourInstance != null)
                return;

            if (doorContreJourMaterial != null)
            {
                _doorContreJourInstance = new Material(doorContreJourMaterial);
            }
            else
            {
                Shader shader = Shader.Find("ChezArthur/UI/GachaDoorContreJour");
                if (shader == null)
                {
                    Debug.LogWarning(
                        "[TrainSequence] Shader GachaDoorContreJour introuvable — " +
                        "lance Chez Arthur/Art/Generate Noise + FX Materials.");
                    return;
                }

                _doorContreJourInstance = new Material(shader);
            }

            _doorContreJourInstance.SetColor(LightColorId, UiTheme.CeremonyLight);
            _doorContreJourInstance.SetFloat(ContreJourId, 0f);
        }

        private void SetContreJourIntensity(float intensity01)
        {
            float i = Mathf.Clamp01(intensity01);
            if (_doorContreJourInstance != null)
                _doorContreJourInstance.SetFloat(ContreJourId, i);

            // Couverture OPAQUE de la zone porte (pas d'additif transparent).
            if (_doorBloom != null)
            {
                Color c = UiTheme.CeremonyLight;
                c.a = Mathf.Lerp(0f, 0.92f, i * i);
                _doorBloom.color = c;
                float s = Mathf.Lerp(1.2f, 2.4f, i);
                _doorBloom.transform.localScale = new Vector3(s, s * 1.2f, 1f);
            }

            if (rarityGlow != null && rarityGlow.gameObject.activeSelf)
            {
                Color gc = UiTheme.CeremonyLight;
                gc.a = Mathf.Lerp(0f, 0.55f, i);
                rarityGlow.color = gc;
                float gs = Mathf.Lerp(1.4f, 2.8f, i);
                rarityGlow.transform.localScale = new Vector3(gs, gs, 1f);
            }
        }

        /// <summary>
        /// Plaque lumière OPAQUE sur la zone porte (alpha blend, pas additif).
        /// Cache le vide sans rendre le wagon transparent.
        /// </summary>
        private void EnsureDoorBloom()
        {
            if (_doorBloom != null)
                return;

            Transform parent = doorScene != null ? doorScene.transform : transform;
            Transform existing = parent.Find("DoorBloomSoft");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(
                    "DoorBloomSoft",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            RectTransform doorRt = doorViewRect;
            if (doorRt == null && doorRawImage != null)
                doorRt = doorRawImage.rectTransform;

            if (doorRt != null)
            {
                rt.anchorMin = doorRt.anchorMin;
                rt.anchorMax = doorRt.anchorMax;
                rt.pivot = doorRt.pivot;
                rt.anchoredPosition = doorRt.anchoredPosition;
                rt.sizeDelta = doorRt.sizeDelta * 1.55f;
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, 0.42f);
                rt.anchorMax = new Vector2(0.5f, 0.42f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(900f, 1200f);
            }

            _doorBloom = go.GetComponent<Image>();
            // Blanc plein + alpha : couverture réelle (pas de mat additif = pas de fond qui « transparaît »).
            _doorBloom.material = null;
            _doorBloom.sprite = GetOrCreateWhiteSprite();
            _doorBloom.type = Image.Type.Simple;
            _doorBloom.raycastTarget = false;
            Color c = UiTheme.CeremonyLight;
            c.a = 0f;
            _doorBloom.color = c;

            // DEVANT la porte : on couvre le trou, on ne regarde pas à travers.
            if (doorRawImage != null)
            {
                int doorIdx = doorRawImage.transform.GetSiblingIndex();
                go.transform.SetSiblingIndex(doorIdx + 1);
            }

            go.SetActive(false);
        }

        private IEnumerator RampDoorGlow(CharacterRarity rarity)
        {
            if (rarityGlow == null)
                yield break;

            Color baseRgb = Color.Lerp(
                CharacterRarityPalette.GetColor(rarity),
                UiTheme.CeremonyLight,
                0.55f);
            float targetAlpha = Mathf.Max(0.7f, GetGlowAlpha(rarity));
            float duration = Mathf.Max(0.8f, RARITY_GLOW_DURATION);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_skipRequested)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float eased = u * u * (3f - 2f * u);

                Color c = baseRgb;
                c.a = Mathf.Lerp(0f, targetAlpha, eased);
                rarityGlow.color = c;
                rarityGlow.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1.75f, eased);
                yield return null;
            }

            // Hold léger au pic pour laisser le son / la lecture.
            float holdPic = 0.25f;
            float h = 0f;
            while (h < holdPic)
            {
                if (_skipRequested)
                    yield break;
                h += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Éblouissement = couverture opaque (illusion). Prepare reveal SOUS le voile, puis fondu.
        /// Le wagon n'est jamais rendu transparent.
        /// </summary>
        private IEnumerator PlayContreJourHandoff(
            CharacterRarity rarity,
            System.Func<IEnumerator> onCoveredPrepare,
            bool playSfx)
        {
            EnsureContreJourFlash();
            if (contreJourFlash == null)
            {
                if (onCoveredPrepare != null)
                    yield return onCoveredPrepare();
                yield break;
            }

            // playSfx réservé (steamburst retiré) — silence volontaire sur le voile.
            _ = playSfx;

            Color warm = UiTheme.CeremonyLight;
            warm.a = 0f;
            contreJourFlash.gameObject.SetActive(true);
            contreJourFlash.material = null;
            contreJourFlash.sprite = GetOrCreateWhiteSprite();
            contreJourFlash.color = warm;
            contreJourFlash.transform.SetAsLastSibling();
            StretchFull(contreJourFlash.rectTransform);

            float coverDur = Mathf.Max(0.25f, CONTRE_JOUR_DURATION * CONTRE_JOUR_COVER_RATIO);
            float elapsed = 0f;

            while (elapsed < coverDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / coverDur);
                float eased = u * u * (3f - 2f * u);
                warm.a = Mathf.Lerp(0f, 1f, eased);
                contreJourFlash.color = warm;
                SetContreJourIntensity(Mathf.Lerp(0.7f, 1f, eased));
                yield return null;
            }

            warm.a = 1f;
            contreJourFlash.color = warm;
            SetContreJourIntensity(1f);

            if (rarityGlow != null)
                rarityGlow.gameObject.SetActive(false);
            if (_doorBloom != null)
                _doorBloom.gameObject.SetActive(false);

            if (onCoveredPrepare != null)
                yield return onCoveredPrepare();

            if (CONTRE_JOUR_HOLD > 0f)
                yield return new WaitForSecondsRealtime(CONTRE_JOUR_HOLD);

            float fadeDur = Mathf.Max(0.35f, CONTRE_JOUR_REVEAL_FADE);
            elapsed = 0f;
            while (elapsed < fadeDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / fadeDur);
                float eased = 1f - (1f - u) * (1f - u);
                warm.a = Mathf.Lerp(1f, 0f, eased);
                contreJourFlash.color = warm;
                yield return null;
            }

            warm.a = 0f;
            contreJourFlash.color = warm;
            contreJourFlash.gameObject.SetActive(false);

            if (doorRawImage != null)
            {
                doorRawImage.material = null;
                doorRawImage.enabled = false;
            }

            SetContreJourIntensity(0f);
        }

        private IEnumerator PlayDoorSmokePuff(CharacterRarity rarity)
        {
            if (smokeTransition == null)
                yield break;

            EnsureSmokeDrawable();
            CaptureSmokeBaseScale();

            float intensity = GetSmokeIntensity(rarity) * 0.65f;
            Color baseCol = UiTheme.CeremonyLight;
            smokeTransition.gameObject.SetActive(true);

            float duration = DOOR_SMOKE_DURATION;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float alpha;
                float scale;
                if (u < 0.4f)
                {
                    float tin = u / 0.4f;
                    float eased = tin * tin * (3f - 2f * tin);
                    alpha = Mathf.Lerp(0f, 0.55f * intensity, eased);
                    scale = Mathf.Lerp(1f, 1f + 0.35f * intensity, eased);
                }
                else
                {
                    float tout = (u - 0.4f) / 0.6f;
                    float eased = tout * tout * (3f - 2f * tout);
                    alpha = Mathf.Lerp(0.55f * intensity, 0f, eased);
                    scale = Mathf.Lerp(1f + 0.35f * intensity, 1.15f, eased);
                }

                baseCol.a = alpha;
                smokeTransition.color = baseCol;
                smokeTransition.rectTransform.localScale = _smokeBaseScale * scale;
                yield return null;
            }

            baseCol.a = 0f;
            smokeTransition.color = baseCol;
            smokeTransition.rectTransform.localScale = _smokeBaseScale;
            smokeTransition.gameObject.SetActive(false);
        }

        private IEnumerator PlayRarityLight(CharacterRarity rarity)
        {
            if (rarityGlow == null)
                yield break;

            Color baseRgb = CharacterRarityPalette.GetColor(rarity);
            float targetAlpha = GetGlowAlpha(rarity);
            bool scaleUp = rarity == CharacterRarity.SSR || rarity == CharacterRarity.LR;

            float elapsed = 0f;
            float fadeDur = Mathf.Max(0.01f, doorLightFadeDuration);
            while (elapsed < fadeDur)
            {
                if (_skipRequested)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / fadeDur);
                float eased = u * u * (3f - 2f * u);

                Color c = baseRgb;
                c.a = Mathf.Lerp(0f, targetAlpha, eased);
                rarityGlow.color = c;

                if (scaleUp)
                {
                    float s = Mathf.Lerp(1f, GLOW_SCALE_SSR_LR, eased);
                    rarityGlow.transform.localScale = new Vector3(s, s, 1f);
                }

                yield return null;
            }

            float hold = Mathf.Max(0f, lightHoldDuration);
            elapsed = 0f;
            while (elapsed < hold)
            {
                if (_skipRequested)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float pulse = 1f + Mathf.Sin(elapsed * GLOW_PULSE_SPEED) * GLOW_PULSE_AMPLITUDE;
                Color c = baseRgb;
                c.a = targetAlpha * pulse;
                rarityGlow.color = c;
                yield return null;
            }
        }

        /// <summary>
        /// Fumée plein écran charbon — couvre la sortie de séquence.
        /// </summary>
        private IEnumerator PlaySmokeCover(
            CharacterRarity rarity,
            Action onCoverPeak,
            bool playSfx)
        {
            if (smokeTransition == null)
            {
                onCoverPeak?.Invoke();
                yield break;
            }

            EnsureSmokeDrawable();
            CaptureSmokeBaseScale();

            float intensity = GetSmokeIntensity(rarity);
            Color smokeColor = UiTheme.GachaStageCharcoal;
            smokeColor.a = 0f;
            smokeTransition.gameObject.SetActive(true);
            smokeTransition.color = smokeColor;

            float peakAlpha = Mathf.Clamp01(SMOKE_PEAK_ALPHA);
            float peakScale = 1f + 0.45f * (intensity - 1f);
            float duration = Mathf.Max(0.35f, smokeCoverDuration);
            float fadeInEnd = duration * SMOKE_FADE_IN_RATIO;

            // steamburst retiré — couverture visuelle silencieuse.
            bool peakNotified = false;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha;
                float scale;

                if (elapsed <= fadeInEnd)
                {
                    float tin = Mathf.Clamp01(elapsed / fadeInEnd);
                    float eased = tin * tin * (3f - 2f * tin);
                    alpha = Mathf.Lerp(0f, peakAlpha, eased);
                    scale = Mathf.Lerp(1f, peakScale, eased);
                }
                else
                {
                    float tout = Mathf.Clamp01((elapsed - fadeInEnd) / (duration - fadeInEnd));
                    float eased = tout * tout * (3f - 2f * tout);
                    alpha = Mathf.Lerp(peakAlpha, 0f, eased);
                    scale = Mathf.Lerp(peakScale, 1.08f, eased);

                    if (!peakNotified && tout >= 0.02f)
                    {
                        peakNotified = true;
                        onCoverPeak?.Invoke();
                    }
                }

                smokeColor.a = alpha;
                smokeTransition.color = smokeColor;
                smokeTransition.rectTransform.localScale = _smokeBaseScale * scale;
                yield return null;
            }

            if (!peakNotified)
                onCoverPeak?.Invoke();

            smokeColor.a = 0f;
            smokeTransition.color = smokeColor;
            smokeTransition.rectTransform.localScale = _smokeBaseScale;
            smokeTransition.gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private void PreloadDoorSheet()
        {
            if (doorFlipbook == null || _doorSheetTexture != null)
                return;

            _doorSheetTexture = PortraitLoader.LoadAtPath(doorFlipbook.ResourcesPath);
        }

        private void OnTapSkip()
        {
            _skipRequested = true;
        }

        private void SetTapListeners(bool active)
        {
            if (tapButtons == null)
                return;

            for (int i = 0; i < tapButtons.Length; i++)
            {
                Button btn = tapButtons[i];
                if (btn == null)
                    continue;

                btn.onClick.RemoveListener(OnTapSkip);
                if (active)
                    btn.onClick.AddListener(OnTapSkip);
            }
        }

        private void PlaySfx(AudioClip clip)
        {
            GachaAnimationController.PlayGachaSfx(clip);
        }

        private float GetGlowAlpha(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.LR: return glowAlphaLR;
                case CharacterRarity.SSR: return glowAlphaSSR;
                default: return glowAlphaSR;
            }
        }

        private static float GetSmokeIntensity(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.LR: return SMOKE_INTENSITY_LR;
                case CharacterRarity.SSR: return SMOKE_INTENSITY_SSR;
                default: return SMOKE_INTENSITY_SR;
            }
        }

        private void CaptureSmokeBaseScale()
        {
            if (_smokeBaseCaptured || smokeTransition == null)
                return;

            _smokeBaseScale = smokeTransition.rectTransform.localScale;
            if (_smokeBaseScale.sqrMagnitude < 0.0001f)
                _smokeBaseScale = Vector3.one;
            _smokeBaseCaptured = true;
        }

        private void OnDestroy()
        {
            SetTapListeners(false);
            if (_runtimeSmokeSprite != null)
            {
                Destroy(_runtimeSmokeSprite);
                _runtimeSmokeSprite = null;
            }
        }
    }
}
