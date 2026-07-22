using System;
using System.Collections;
using ChezArthur.Audio;
using ChezArthur.Characters;
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
        private const float DOOR_HEIGHT_RATIO = 0.72f;
        private const float DOOR_ASPECT = 228f / 342f;
        private const float REF_HEIGHT = 1920f;
        private const float DOOR_SMOKE_DURATION = 0.55f;

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

        [Header("Assets")]
        [SerializeField] private TrainCurveData departCurve;
        [SerializeField] private AnimatedPortraitData doorFlipbook;

        [Header("Fumée (overlay premium)")]
        [SerializeField] private Image smokeTransition;
        [SerializeField] private float smokeCoverDuration = 1.05f;

        [Header("Audio")]
        [SerializeField] private AudioClip trainArriveClip;
        [SerializeField] private AudioClip doorClip;
        [SerializeField] private AudioClip smokeClip;

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
        private bool _layoutPrepared;
        private Sprite _runtimeSmokeSprite;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public GameObject TrainScene => trainScene;
        public GameObject DoorScene => doorScene;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Joue la séquence train complète. Skip → saute à la fumée (scalée rareté).
        /// </summary>
        public IEnumerator PlaySequence(CharacterRarity bestRarity, Action onSkipToReveal)
        {
            _skipRequested = false;
            PreparePremiumLayout();
            EnsureSmokeDrawable();
            SetTapListeners(true);

            yield return PlayArrival();

            if (_skipRequested)
            {
                yield return PlaySmokeCover(bestRarity, onSkipToReveal, true);
                SetTapListeners(false);
                yield break;
            }

            yield return PlayDoorOpen(bestRarity);

            if (_skipRequested)
            {
                yield return PlaySmokeCover(bestRarity, onSkipToReveal, true);
                SetTapListeners(false);
                yield break;
            }

            yield return PlayRarityLight(bestRarity);

            if (_skipRequested)
            {
                yield return PlaySmokeCover(bestRarity, onSkipToReveal, true);
                SetTapListeners(false);
                yield break;
            }

            yield return PlaySmokeCover(bestRarity, onSkipToReveal, true);
            SetTapListeners(false);
        }

        public void ReleaseDoorSheet()
        {
            if (_doorSheetTexture != null)
            {
                PortraitLoader.Release(_doorSheetTexture);
                _doorSheetTexture = null;
            }

            if (doorRawImage != null)
            {
                doorRawImage.texture = null;
                doorRawImage.enabled = false;
            }

            if (doorView != null)
                doorView.PlayStatic();
        }

        public void HideSequenceScenes()
        {
            if (trainScene != null)
                trainScene.SetActive(false);
            if (doorScene != null)
                doorScene.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // LAYOUT PREMIUM
        // ═══════════════════════════════════════════

        private void PreparePremiumLayout()
        {
            if (_layoutPrepared)
                return;

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

            // Porte héros : ~72 % hauteur, ancrée un peu bas (sol wagon).
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
                glowRt.sizeDelta = doorRt.sizeDelta * 1.45f;
            }

            if (trainMask == null && trainSprite != null && trainSprite.parent != null)
                trainMask = trainSprite.parent as RectTransform;

            _layoutPrepared = true;
        }

        private void EnsureSmokeDrawable()
        {
            if (smokeTransition == null)
                return;

            if (smokeTransition.sprite == null)
            {
                if (_runtimeSmokeSprite == null)
                {
                    Texture2D tex = Texture2D.whiteTexture;
                    _runtimeSmokeSprite = Sprite.Create(
                        tex,
                        new Rect(0f, 0f, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }

                smokeTransition.sprite = _runtimeSmokeSprite;
            }

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
            if (trainScene != null)
                trainScene.SetActive(true);
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

            // Courbe inversée Duration→0 : freinage. Scale UI → hors écran à droite.
            float t = duration;
            while (t > 0f)
            {
                if (_skipRequested)
                    yield break;

                t -= Time.unscaledDeltaTime;
                if (t < 0f)
                    t = 0f;

                ApplyTrainX(departCurve.EvaluateOffset(t));
                yield return null;
            }

            ApplyTrainX(0f);
        }

        private void ComputeTrainMotionScale()
        {
            if (trainSprite == null || departCurve == null)
                return;

            _trainRestY = trainSprite.anchoredPosition.y;
            _trainRestX = 0f;

            float authorMax = Mathf.Max(1f, departCurve.MaxOffset);
            float maskW = 1080f;
            if (trainMask != null)
                maskW = Mathf.Max(maskW, trainMask.rect.width);

            float trainW = trainSprite.rect.width * Mathf.Abs(trainSprite.localScale.x);
            if (trainW < 1f)
                trainW = departCurve.SpriteWidthPx;

            // Trajet : entièrement hors écran à droite → repos centré.
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

            if (rarityGlow != null)
            {
                Color c = rarityGlow.color;
                c.a = 0f;
                rarityGlow.color = c;
                rarityGlow.gameObject.SetActive(true);
                rarityGlow.transform.localScale = Vector3.one;
            }

            if (doorRawImage != null)
                doorRawImage.enabled = true;

            if (doorView != null && doorFlipbook != null)
            {
                if (doorRawImage != null && _doorSheetTexture != null)
                    doorRawImage.texture = _doorSheetTexture;

                doorView.Initialize(doorRawImage);
                doorView.PlayAnimatedOnce(doorFlipbook);
            }

            PlaySfx(doorClip);

            // Fumée courte à l'ouverture (signal premium).
            StartCoroutine(PlayDoorSmokePuff(bestRarity));

            if (doorView == null)
                yield break;

            while (!doorView.HasFinishedOneShot)
            {
                if (_skipRequested)
                    yield break;
                yield return null;
            }
        }

        private IEnumerator PlayDoorSmokePuff(CharacterRarity rarity)
        {
            if (smokeTransition == null)
                yield break;

            EnsureSmokeDrawable();
            CaptureSmokeBaseScale();

            float intensity = GetSmokeIntensity(rarity) * 0.65f;
            Color baseCol = UiTheme.GachaStageCharcoal;
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

            if (playSfx)
                PlaySfx(smokeClip);

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
            if (clip == null)
                return;
            if (SfxManager.Instance != null)
                SfxManager.Instance.PlaySfx(clip);
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
