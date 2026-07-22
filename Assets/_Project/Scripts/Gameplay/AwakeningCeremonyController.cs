using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Audio;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.UI;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Orchestre la cérémonie d'éveil SSR v4 (couverture écran, lumière carte, audio autonome).
    /// Singleton de scène. Temps unscaled. Tap ignoré avant la consécration.
    /// </summary>
    public class AwakeningCeremonyController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string WhiteoutProperty = "_Whiteout";
        private const string WhiteoutColorProperty = "_WhiteoutColor";
        private const string DissolveAmountProperty = "_DissolveAmount";
        private const string PrefSfxVolume = "AudioManager_SfxVolume";
        private const float MusicUnduckDuration = 1f;
        private const float HintFadeDuration = 0.45f;
        private const float WhiteoutCap = 0.95f;
        private const float CoverOverscan = 1.02f;
        private const float DriftDuration = 3.5f;
        private const float DriftFactor = 0.985f;
        private const float ScreenShakeAmp = 5f;
        private const float ScreenShakeDuration = 0.2f;
        private const float PresenceRimMax = 0.18f;
        private const float PresenceRimPulseSpeed = 0.9f;
        private const float RimBloomMontéeMax = 0.85f;
        private const float RaysMontéeMax = 0.4f;
        private const float AmbientMax = 0.22f;
        private const float RaysRotMontéeDegPerSec = 10f;
        private const float RaysRotRevealDegPerSec = 3f;
        private const float RaysRevealRest = 0.12f;
        private const float RimRevealRest = 0.14f;
        private const float RimFlashBurst = 1f;
        private const float RaysFlashBurst = 0.7f;
        private const float BannerFlashBurstDuration = 0.4f;
        private const float BannerFlashPeakAlpha = 0.85f;
        private const float AmbienceVolumeFactor = 0.75f;
        private const int MaxMotes = 14;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private AwakeningCeremonyView overlayPrefab;
        [SerializeField] private Material dissolveMaterial;

        [Header("Audio (sources créées par code)")]
        [SerializeField] private AudioClip riserClip;
        [SerializeField] private AudioClip flashClip;
        [SerializeField] private AudioClip fanfareClip;
        [SerializeField] private AudioClip ambienceLoop;

        [Header("Timings")]
        [SerializeField] private float musicDuckDuration = 0.8f;
        [SerializeField] private float isolementDuration = 1.4f;
        [SerializeField] private float presenceFadeDuration = 1.4f;
        [SerializeField] private float presenceHoldDuration = 2.8f;
        [SerializeField] private float monteeDuration = 5.5f;
        [SerializeField] private float flashAttack = 0.15f;
        [SerializeField] private float flashHold = 0.45f;
        [SerializeField] private float flashDecay = 1.1f;
        [SerializeField] private float revealDuration = 2.8f;
        [SerializeField] private float consecrationBeat = 1.2f;
        [SerializeField] private float hintDelay = 1.8f;
        [SerializeField] private float fadeDuration = 0.55f;

        [Header("Échelle / FX")]
        [SerializeField] private float dechuScale = 0.72f;
        [SerializeField] private float bannerSlamScale = 1.8f;
        [SerializeField] private float bannerSlamDuration = 0.45f;
        [SerializeField] private float glowFrontMaxAlpha = 0.22f;

        [Header("Courbe montée (_Whiteout)")]
        [SerializeField] private AnimationCurve monteePulses;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static AwakeningCeremonyController _instance;

        private AwakeningCeremonyView _overlayInstance;
        private Material _runtimeDechuMat;
        private Material _runtimePrimeMat;
        private Coroutine _playRoutine;
        private bool _skipRequested;
        private bool _tapArmed;
        private float _lastDuckFactor = 1f;
        private float _coverScale = 1f;
        private float _ceremonySfxVolume = 1f;

        private AudioSource _ambienceSource;
        private AudioSource _oneshotSource;

        private readonly Image[] _motes = new Image[MaxMotes];
        private readonly float[] _moteSpeeds = new float[MaxMotes];
        private readonly float[] _motePhases = new float[MaxMotes];
        private readonly Vector2[] _moteBasePos = new Vector2[MaxMotes];
        private readonly Color[] _moteColors = new Color[MaxMotes];
        private int _moteCount;

        private RectTransform _raysRt;
        private float _raysAngle;
        private Color _scratchCeremony;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static AwakeningCeremonyController Instance => _instance;

        public bool IsPlaying { get; private set; }

        public bool HasPendingCeremonies
        {
            get
            {
                if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                    return false;

                IReadOnlyList<OwnedCharacter> ownedList =
                    PersistentManager.Instance.Characters.GetOwnedCharacters();
                if (ownedList == null)
                    return false;

                for (int i = 0; i < ownedList.Count; i++)
                {
                    if (TryGetPendingCeremony(ownedList[i], out _, out _))
                        return true;
                }

                return false;
            }
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            EnsureDefaultMonteePulses();
            EnsureAudioSources();
            _scratchCeremony = UiTheme.CeremonyLight;
        }

        private void OnDestroy()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            StopAmbienceImmediate();
            DestroyRuntimeMaterials();

            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void PlayCeremonies(Action onComplete)
        {
            if (IsPlaying)
                return;

            if (!HasPendingCeremonies)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureOverlay();
            if (_overlayInstance == null)
            {
                Debug.LogError("[AwakeningCeremonyController] Overlay introuvable.");
                onComplete?.Invoke();
                return;
            }

            IsPlaying = true;
            _playRoutine = StartCoroutine(PlayCeremoniesRoutine(onComplete));
        }

        public void PlayPreview(CharacterData data, Action onComplete = null)
        {
            if (IsPlaying)
                return;

            if (data == null
                || data.AnimatedPortraitPrime == null
                || data.AnimatedPortraitDechu == null)
            {
                Debug.LogWarning("[AwakeningCeremonyController] PlayPreview : data ou portraits manquants.");
                onComplete?.Invoke();
                return;
            }

            EnsureOverlay();
            if (_overlayInstance == null)
            {
                Debug.LogError("[AwakeningCeremonyController] Overlay introuvable.");
                onComplete?.Invoke();
                return;
            }

            IsPlaying = true;
            _playRoutine = StartCoroutine(PlayPreviewRoutine(data, onComplete));
        }

        public void Configure(
            AwakeningCeremonyView prefab,
            Material dissolveMat,
            AudioClip riser = null,
            AudioClip flash = null,
            AudioClip fanfare = null,
            AudioClip ambience = null)
        {
            overlayPrefab = prefab;
            dissolveMaterial = dissolveMat;
            if (riser != null)
                riserClip = riser;
            if (flash != null)
                flashClip = flash;
            if (fanfare != null)
                fanfareClip = fanfare;
            if (ambience != null)
                ambienceLoop = ambience;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — orchestration
        // ═══════════════════════════════════════════

        private void EnsureOverlay()
        {
            if (_overlayInstance != null)
                return;

            if (overlayPrefab == null)
            {
                Debug.LogError("[AwakeningCeremonyController] overlayPrefab non assigné.");
                return;
            }

            _overlayInstance = Instantiate(overlayPrefab);
            _overlayInstance.gameObject.SetActive(false);

            if (_overlayInstance.TapButton != null)
            {
                _overlayInstance.TapButton.onClick.RemoveAllListeners();
                _overlayInstance.TapButton.onClick.AddListener(OnTapRequested);
            }

            CacheMotes(_overlayInstance);
            _raysRt = _overlayInstance.RaysRoot;
            _raysAngle = 0f;
        }

        private void CacheMotes(AwakeningCeremonyView view)
        {
            _moteCount = 0;
            IReadOnlyList<Image> list = view.MoteImages;
            if (list == null)
                return;

            int count = list.Count;
            if (count > MaxMotes)
                count = MaxMotes;

            for (int i = 0; i < count; i++)
            {
                Image img = list[i];
                if (img == null)
                    continue;

                _motes[_moteCount] = img;
                _moteSpeeds[_moteCount] = 0.6f + (i % 5) * 0.25f;
                _motePhases[_moteCount] = i * 0.73f;
                _moteBasePos[_moteCount] = img.rectTransform.anchoredPosition;
                _moteColors[_moteCount] = UiTheme.Gold;
                _moteColors[_moteCount].a = 0f;
                img.color = _moteColors[_moteCount];
                _moteCount++;
            }
        }

        private void OnTapRequested()
        {
            if (IsPlaying && _tapArmed)
                _skipRequested = true;
        }

        private IEnumerator PlayCeremoniesRoutine(Action onComplete)
        {
            List<(OwnedCharacter owned, CharacterData data)> pending = CollectPending();

            _overlayInstance.gameObject.SetActive(true);
            if (_overlayInstance.CanvasGroup != null)
                _overlayInstance.CanvasGroup.alpha = 0f;

            for (int i = 0; i < pending.Count; i++)
            {
                HideBannerAndHint();
                yield return PlayOneCeremony(pending[i].data, persistCeremonySeen: true);
                if (i < pending.Count - 1)
                    HideBannerAndHint();
            }

            yield return ExitOverlayAndUnduck();
            IsPlaying = false;
            _playRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator PlayPreviewRoutine(CharacterData data, Action onComplete)
        {
            _overlayInstance.gameObject.SetActive(true);
            if (_overlayInstance.CanvasGroup != null)
                _overlayInstance.CanvasGroup.alpha = 0f;

            HideBannerAndHint();
            yield return PlayOneCeremony(data, persistCeremonySeen: false);
            yield return ExitOverlayAndUnduck();

            IsPlaying = false;
            _playRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator ExitOverlayAndUnduck()
        {
            yield return FadeAmbience(0f, fadeDuration);
            StopAmbienceImmediate();

            yield return FadeCanvas(1f, 0f);
            ReleaseViewsAndMaterials();
            ResetFxVisuals(_overlayInstance);

            if (_overlayInstance != null)
                _overlayInstance.gameObject.SetActive(false);

            yield return AnimateMusicDuck(1f, MusicUnduckDuration);
        }

        private List<(OwnedCharacter owned, CharacterData data)> CollectPending()
        {
            var list = new List<(OwnedCharacter, CharacterData)>();
            IReadOnlyList<OwnedCharacter> ownedList =
                PersistentManager.Instance.Characters.GetOwnedCharacters();

            for (int i = 0; i < ownedList.Count; i++)
            {
                if (TryGetPendingCeremony(ownedList[i], out OwnedCharacter owned, out CharacterData data))
                    list.Add((owned, data));
            }

            return list;
        }

        private static bool TryGetPendingCeremony(
            OwnedCharacter owned,
            out OwnedCharacter persisted,
            out CharacterData data)
        {
            persisted = null;
            data = null;

            if (owned == null || !owned.isAwakened || owned.awakeningCeremonySeen)
                return false;

            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return false;

            persisted = PersistentManager.Instance.Characters.GetOwnedCharacter(owned.characterId);
            if (persisted == null)
                return false;

            var pair = PersistentManager.Instance.Characters.GetCharacterWithData(owned.characterId);
            data = pair.data;
            if (data == null
                || data.AnimatedPortraitPrime == null
                || data.AnimatedPortraitDechu == null)
            {
                return false;
            }

            return true;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — séquence
        // ═══════════════════════════════════════════

        private IEnumerator PlayOneCeremony(CharacterData data, bool persistCeremonySeen)
        {
            AwakeningCeremonyView view = _overlayInstance;
            _tapArmed = false;
            _skipRequested = false;
            RefreshCeremonyVolume();
            _coverScale = ComputeCoverScale(view);

            yield return AnimateMusicDuck(0f, musicDuckDuration);

            if (view.CanvasGroup != null && view.CanvasGroup.alpha < 0.99f)
                yield return FadeCanvas(view.CanvasGroup.alpha, 1f);

            ResetPortraitState(view);
            ResetFxVisuals(view);

            float ambienceTarget = _ceremonySfxVolume * AmbienceVolumeFactor;
            yield return ParallelIsolementAndAmbience(ambienceTarget);

            // 1 — PRÉSENCE
            EnsureDechuMaterial();
            view.DechuView?.ShowState(data, data.AnimatedPortraitDechu);
            if (view.DechuRawImage != null)
            {
                if (_runtimeDechuMat != null)
                {
                    _runtimeDechuMat.SetFloat(WhiteoutProperty, 0f);
                    _runtimeDechuMat.SetColor(WhiteoutColorProperty, UiTheme.CeremonyLight);
                    view.DechuRawImage.material = _runtimeDechuMat;
                }

                view.DechuRawImage.enabled = true;
            }

            SetContainerScale(view, dechuScale);
            SetContainerAnchoredPos(view, Vector2.zero);

            CanvasGroup portraitGroup = GetPortraitCanvasGroup(view);
            if (portraitGroup != null)
                portraitGroup.alpha = 0f;

            yield return PresencePhase(view, portraitGroup);

            // 2 — MONTÉE → coverScale
            PlayOneShot(riserClip);
            yield return AnimateMontee(view);
            SetContainerAnchoredPos(view, Vector2.zero);
            SetContainerScale(view, _coverScale);

            // 3 — FLASH (zoom atteint coverScale)
            PlayOneShot(flashClip);
            yield return AnimateFlashAttack(view);

            if (view.DechuRawImage != null)
                view.DechuRawImage.enabled = false;
            view.DechuView?.Release();
            if (view.DechuRawImage != null)
                view.DechuRawImage.material = null;

            EnsurePrimeMaterial();
            view.PrimeView?.ShowState(data, data.AnimatedPortraitPrime);
            if (view.PrimeRawImage != null)
            {
                if (_runtimePrimeMat != null)
                {
                    _runtimePrimeMat.SetFloat(WhiteoutProperty, 1f);
                    _runtimePrimeMat.SetColor(WhiteoutColorProperty, UiTheme.CeremonyLight);
                    view.PrimeRawImage.material = _runtimePrimeMat;
                }

                view.PrimeRawImage.enabled = true;
            }

            SetContainerScale(view, _coverScale);
            SetContainerAnchoredPos(view, Vector2.zero);

            yield return WaitUnscaled(flashHold);
            yield return AnimateFlashDecayAndShake(view);

            // 4 — RÉVÉLATION (+ dérive lente démarre ici et continue pendant consécration)
            yield return AnimateRevealAndDrift(view);

            // 5 — CONSÉCRATION (dérive déjà en cours / poursuivie)
            yield return WaitUnscaled(consecrationBeat);

            if (view.BannerText != null)
                view.BannerText.text = data.CharacterName + " Prime débloqué !";

            if (view.BannerRoot != null)
            {
                view.BannerRoot.SetActive(true);
                view.BannerRoot.transform.localScale = new Vector3(bannerSlamScale, bannerSlamScale, 1f);
                yield return SlamBanner(view);
            }

            yield return WaitUnscaled(hintDelay);

            if (view.HintText != null)
            {
                view.HintText.gameObject.SetActive(true);
                yield return FadeTmpAlpha(view.HintText, 0f, 1f, HintFadeDuration);
            }

            if (persistCeremonySeen
                && PersistentManager.Instance != null
                && PersistentManager.Instance.Characters != null)
            {
                OwnedCharacter persisted =
                    PersistentManager.Instance.Characters.GetOwnedCharacter(data.Id);
                if (persisted != null)
                {
                    persisted.awakeningCeremonySeen = true;
                    PersistentManager.Instance.SaveGame();
                }
            }

            _skipRequested = false;
            _tapArmed = true;
            while (!_skipRequested)
                yield return null;

            _tapArmed = false;
            _skipRequested = false;
            HideBannerAndHint();
        }

        private float ComputeCoverScale(AwakeningCeremonyView view)
        {
            if (view == null || view.PortraitContainer == null || view.CanvasGroup == null)
                return 1f;

            RectTransform canvasRt = view.CanvasGroup.GetComponent<RectTransform>();
            RectTransform cardRt = view.PortraitContainer;
            if (canvasRt == null)
                return 1f;

            // Force scale 1 pour mesurer la carte
            Vector3 saved = cardRt.localScale;
            cardRt.localScale = Vector3.one;

            Rect screenRect = canvasRt.rect;
            Rect cardRect = cardRt.rect;
            float w = cardRect.width;
            float h = cardRect.height;
            if (w < 1f)
                w = 1f;
            if (h < 1f)
                h = 1f;

            float cover = Mathf.Max(screenRect.width / w, screenRect.height / h) * CoverOverscan;
            cardRt.localScale = saved;
            return cover;
        }

        private IEnumerator ParallelIsolementAndAmbience(float ambianceTarget)
        {
            float elapsed = 0f;
            float duration = isolementDuration;
            EnsureAudioSources();
            if (_ambienceSource != null && ambienceLoop != null)
            {
                _ambienceSource.clip = ambienceLoop;
                _ambienceSource.volume = 0f;
                if (!_ambienceSource.isPlaying)
                    _ambienceSource.Play();
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                if (_ambienceSource != null)
                    _ambienceSource.volume = Mathf.Lerp(0f, ambianceTarget, t);
                yield return null;
            }

            if (_ambienceSource != null)
                _ambienceSource.volume = ambianceTarget;
        }

        private IEnumerator PresencePhase(AwakeningCeremonyView view, CanvasGroup portraitGroup)
        {
            float elapsed = 0f;
            float total = presenceFadeDuration + presenceHoldDuration;

            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                float fadeT = presenceFadeDuration > 0f
                    ? Mathf.Clamp01(elapsed / presenceFadeDuration)
                    : 1f;

                if (portraitGroup != null)
                    portraitGroup.alpha = fadeT;

                float breath = PresenceRimMax * (0.5f + 0.5f * Mathf.Sin(elapsed * PresenceRimPulseSpeed));
                SetGlowColorAlpha(view.RimBloom, UiTheme.CeremonyLight, Mathf.Lerp(0f, breath, fadeT));
                SetGlowColorAlpha(view.RaysImage, UiTheme.CeremonyLight, 0f);

                yield return null;
            }

            if (portraitGroup != null)
                portraitGroup.alpha = 1f;
        }

        private IEnumerator AnimateMontee(AwakeningCeremonyView view)
        {
            EnsureDefaultMonteePulses();
            float elapsed = 0f;

            while (elapsed < monteeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t01 = monteeDuration > 0f ? Mathf.Clamp01(elapsed / monteeDuration) : 1f;
                float eased = t01 * t01;
                float pulse = monteePulses.Evaluate(t01);
                float pulse01 = Mathf.Clamp01(pulse);

                // Whiteout plus agressif en fin de montée (masque le déchu avant le flash)
                float whiteout = Mathf.Min(pulse, WhiteoutCap);
                if (t01 > 0.55f)
                {
                    float late = (t01 - 0.55f) / 0.45f;
                    whiteout = Mathf.Max(whiteout, Mathf.Lerp(0.55f, WhiteoutCap, late));
                }

                if (_runtimeDechuMat != null)
                    _runtimeDechuMat.SetFloat(WhiteoutProperty, whiteout);

                float scale = Mathf.Lerp(dechuScale, _coverScale, eased);
                SetContainerScale(view, scale);

                float shake = Mathf.Sin(elapsed * 40f * Mathf.PI * 2f) * 3f * t01;
                SetContainerAnchoredPos(view, new Vector2(shake, shake * 0.6f));

                // RimBloom pulse synchrone (carte)
                SetGlowColorAlpha(view.RimBloom, UiTheme.CeremonyLight, pulse01 * RimBloomMontéeMax);

                // Rays + ambient centrés écran
                SetGlowColorAlpha(view.RaysImage, UiTheme.CeremonyLight, t01 * RaysMontéeMax);
                SetGlowColorAlpha(view.AmbientGlow, UiTheme.CeremonyLight, t01 * AmbientMax);
                SetGlowColorAlpha(view.GlowFront, UiTheme.CeremonyLight, t01 * glowFrontMaxAlpha);

                _raysAngle += RaysRotMontéeDegPerSec * Time.unscaledDeltaTime;
                if (_raysRt != null)
                    _raysRt.localRotation = Quaternion.Euler(0f, 0f, _raysAngle);

                UpdateMotes(elapsed, t01, speedScale: 1f);

                yield return null;
            }

            if (_runtimeDechuMat != null)
                _runtimeDechuMat.SetFloat(WhiteoutProperty, WhiteoutCap);
            SetContainerScale(view, _coverScale);
            SetContainerAnchoredPos(view, Vector2.zero);
        }

        private IEnumerator AnimateFlashAttack(AwakeningCeremonyView view)
        {
            Image flash = view.FlashOverlay;
            if (flash == null)
                yield break;

            flash.gameObject.SetActive(true);
            _scratchCeremony = UiTheme.CeremonyLight;
            _scratchCeremony.a = 0f;
            flash.color = _scratchCeremony;

            SetGlowColorAlpha(view.RimBloom, UiTheme.CeremonyLight, RimFlashBurst);
            SetGlowColorAlpha(view.RaysImage, UiTheme.CeremonyLight, RaysFlashBurst);

            float elapsed = 0f;
            while (elapsed < flashAttack)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = flashAttack > 0f ? Mathf.Clamp01(elapsed / flashAttack) : 1f;
                _scratchCeremony = UiTheme.CeremonyLight;
                _scratchCeremony.a = t;
                flash.color = _scratchCeremony;
                yield return null;
            }

            _scratchCeremony = UiTheme.CeremonyLight;
            _scratchCeremony.a = 1f;
            flash.color = _scratchCeremony;
        }

        private IEnumerator AnimateFlashDecayAndShake(AwakeningCeremonyView view)
        {
            Image flash = view.FlashOverlay;
            float elapsed = 0f;
            float duration = Mathf.Max(flashDecay, ScreenShakeDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float tDecay = flashDecay > 0f ? Mathf.Clamp01(elapsed / flashDecay) : 1f;
                float eased = 1f - (1f - tDecay) * (1f - tDecay);

                if (flash != null && elapsed <= flashDecay)
                {
                    _scratchCeremony = UiTheme.CeremonyLight;
                    _scratchCeremony.a = 1f - eased;
                    flash.color = _scratchCeremony;
                }

                SetGlowColorAlpha(view.RimBloom, UiTheme.CeremonyLight, Mathf.Lerp(RimFlashBurst, RimRevealRest, eased));
                SetGlowColorAlpha(view.RaysImage, UiTheme.CeremonyLight, Mathf.Lerp(RaysFlashBurst, RaysRevealRest, eased));

                // Micro screen-shake amorti 0.15s
                if (elapsed < ScreenShakeDuration)
                {
                    float shakeT = elapsed / ScreenShakeDuration;
                    float amp = ScreenShakeAmp * (1f - shakeT);
                    float sx = Mathf.Sin(elapsed * 55f * Mathf.PI * 2f) * amp;
                    float sy = Mathf.Cos(elapsed * 47f * Mathf.PI * 2f) * amp * 0.7f;
                    SetContainerAnchoredPos(view, new Vector2(sx, sy));
                }
                else
                {
                    SetContainerAnchoredPos(view, Vector2.zero);
                }

                yield return null;
            }

            SetContainerAnchoredPos(view, Vector2.zero);
            if (flash != null)
            {
                _scratchCeremony = UiTheme.CeremonyLight;
                _scratchCeremony.a = 0f;
                flash.color = _scratchCeremony;
                flash.gameObject.SetActive(false);
            }
        }

        private IEnumerator AnimateRevealAndDrift(AwakeningCeremonyView view)
        {
            float elapsed = 0f;
            // Révélation whiteout sur revealDuration ; dérive scale sur DriftDuration (overlap OK)
            float total = Mathf.Max(revealDuration, DriftDuration);

            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                float tReveal = revealDuration > 0f ? Mathf.Clamp01(elapsed / revealDuration) : 1f;
                float tDrift = DriftDuration > 0f ? Mathf.Clamp01(elapsed / DriftDuration) : 1f;

                if (_runtimePrimeMat != null && elapsed <= revealDuration)
                    _runtimePrimeMat.SetFloat(WhiteoutProperty, Mathf.Lerp(1f, 0f, tReveal));

                float scale = Mathf.Lerp(_coverScale, _coverScale * DriftFactor, tDrift);
                SetContainerScale(view, scale);

                SetGlowColorAlpha(view.RimBloom, UiTheme.CeremonyLight, RimRevealRest);
                SetGlowColorAlpha(view.RaysImage, UiTheme.CeremonyLight, RaysRevealRest);
                SetGlowColorAlpha(view.GlowFront, UiTheme.CeremonyLight, (1f - tReveal) * glowFrontMaxAlpha);
                SetGlowColorAlpha(view.AmbientGlow, UiTheme.CeremonyLight, (1f - tReveal) * AmbientMax);

                _raysAngle += RaysRotRevealDegPerSec * Time.unscaledDeltaTime;
                if (_raysRt != null)
                    _raysRt.localRotation = Quaternion.Euler(0f, 0f, _raysAngle);

                UpdateMotes(elapsed, 1f - tReveal, speedScale: 1f - tReveal * 0.7f);

                yield return null;
            }

            if (_runtimePrimeMat != null)
                _runtimePrimeMat.SetFloat(WhiteoutProperty, 0f);

            SetAllMotesAlpha(0f);
            SetGlowColorAlpha(view.GlowFront, UiTheme.CeremonyLight, 0f);
            SetContainerScale(view, _coverScale * DriftFactor);
        }

        private IEnumerator SlamBanner(AwakeningCeremonyView view)
        {
            Transform banner = view.BannerRoot != null ? view.BannerRoot.transform : null;
            Image bannerFlash = view.BannerFlash;

            PlayOneShot(fanfareClip);

            float elapsed = 0f;
            while (elapsed < bannerSlamDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = bannerSlamDuration > 0f ? Mathf.Clamp01(elapsed / bannerSlamDuration) : 1f;

                float scale;
                if (t < 0.65f)
                {
                    float u = t / 0.65f;
                    scale = Mathf.Lerp(bannerSlamScale, 0.95f, EaseOutCubic(u));
                }
                else
                {
                    float u = (t - 0.65f) / 0.35f;
                    scale = Mathf.Lerp(0.95f, 1f, u);
                }

                if (banner != null)
                    banner.localScale = new Vector3(scale, scale, 1f);

                if (bannerFlash != null)
                {
                    float flashT = BannerFlashBurstDuration > 0f
                        ? Mathf.Clamp01(elapsed / BannerFlashBurstDuration)
                        : 1f;
                    SetGlowColorAlpha(bannerFlash, UiTheme.CeremonyLight, BannerFlashPeakAlpha * (1f - flashT));
                }

                yield return null;
            }

            if (banner != null)
                banner.localScale = Vector3.one;
            SetGlowColorAlpha(bannerFlash, UiTheme.CeremonyLight, 0f);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — FX / audio helpers
        // ═══════════════════════════════════════════

        private void UpdateMotes(float elapsed, float globalAlpha, float speedScale)
        {
            for (int i = 0; i < _moteCount; i++)
            {
                Image img = _motes[i];
                if (img == null)
                    continue;

                float speed = _moteSpeeds[i] * speedScale;
                float phase = _motePhases[i];
                float ox = Mathf.Sin(elapsed * speed + phase) * 18f;
                float oy = Mathf.Cos(elapsed * speed * 0.7f + phase) * 24f;
                img.rectTransform.anchoredPosition = _moteBasePos[i] + new Vector2(ox, oy);

                _moteColors[i] = UiTheme.Gold;
                _moteColors[i].a = globalAlpha;
                img.color = _moteColors[i];
            }
        }

        private void SetAllMotesAlpha(float alpha)
        {
            for (int i = 0; i < _moteCount; i++)
            {
                if (_motes[i] == null)
                    continue;
                _moteColors[i] = UiTheme.Gold;
                _moteColors[i].a = alpha;
                _motes[i].color = _moteColors[i];
            }
        }

        private static void SetGlowColorAlpha(Image img, Color rgbSource, float alpha)
        {
            if (img == null)
                return;
            Color c = rgbSource;
            c.a = alpha;
            img.color = c;
        }

        private void ResetFxVisuals(AwakeningCeremonyView view)
        {
            if (view == null)
                return;

            SetGlowColorAlpha(view.AmbientGlow, UiTheme.CeremonyLight, 0f);
            SetGlowColorAlpha(view.RaysImage, UiTheme.CeremonyLight, 0f);
            SetGlowColorAlpha(view.RimBloom, UiTheme.CeremonyLight, 0f);
            SetGlowColorAlpha(view.GlowFront, UiTheme.CeremonyLight, 0f);
            SetGlowColorAlpha(view.BannerFlash, UiTheme.CeremonyLight, 0f);

            if (_raysRt != null)
            {
                _raysAngle = 0f;
                _raysRt.localRotation = Quaternion.identity;
            }

            SetAllMotesAlpha(0f);
            for (int i = 0; i < _moteCount; i++)
            {
                if (_motes[i] != null)
                    _motes[i].rectTransform.anchoredPosition = _moteBasePos[i];
            }
        }

        private void EnsureAudioSources()
        {
            if (_ambienceSource == null)
            {
                _ambienceSource = gameObject.AddComponent<AudioSource>();
                _ambienceSource.playOnAwake = false;
                _ambienceSource.loop = true;
                _ambienceSource.spatialBlend = 0f;
                _ambienceSource.volume = 0f;
            }

            if (_oneshotSource == null)
            {
                _oneshotSource = gameObject.AddComponent<AudioSource>();
                _oneshotSource.playOnAwake = false;
                _oneshotSource.loop = false;
                _oneshotSource.spatialBlend = 0f;
                _oneshotSource.volume = 1f;
                _oneshotSource.ignoreListenerPause = true;
            }

            if (_ambienceSource != null)
                _ambienceSource.ignoreListenerPause = true;
        }

        private void RefreshCeremonyVolume()
        {
            if (SfxManager.Instance != null)
                _ceremonySfxVolume = SfxManager.Instance.CurrentVolume;
            else
                _ceremonySfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxVolume, 1f));

            // Évite un mute total silencieux (réglage à 0 = volontairement bas, pas zéro hard)
            if (_ceremonySfxVolume < 0.05f)
                _ceremonySfxVolume = 0.05f;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning(
                    "[AwakeningCeremonyController] Clip SFX manquant — " +
                    "assigne riser/flash/fanfare sur le controller (Game) " +
                    "ou sur le bouton Preview (Hub).");
                return;
            }

            EnsureAudioSources();
            if (_oneshotSource == null)
                return;

            _oneshotSource.PlayOneShot(clip, _ceremonySfxVolume);
        }

        private IEnumerator FadeAmbience(float target, float duration)
        {
            if (_ambienceSource == null)
                yield break;

            float from = _ambienceSource.volume;
            float elapsed = 0f;
            if (duration <= 0f)
            {
                _ambienceSource.volume = target;
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _ambienceSource.volume = Mathf.Lerp(from, target, t);
                yield return null;
            }

            _ambienceSource.volume = target;
        }

        private void StopAmbienceImmediate()
        {
            if (_ambienceSource == null)
                return;
            _ambienceSource.Stop();
            _ambienceSource.volume = 0f;
        }

        private IEnumerator FadeTmpAlpha(
            TMPro.TextMeshProUGUI tmp, float from, float to, float duration)
        {
            if (tmp == null)
                yield break;

            Color c = tmp.color;
            c.a = from;
            tmp.color = c;

            if (duration <= 0f)
            {
                c.a = to;
                tmp.color = c;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c.a = Mathf.Lerp(from, to, t);
                tmp.color = c;
                yield return null;
            }

            c.a = to;
            tmp.color = c;
        }

        private IEnumerator FadeCanvas(float from, float to)
        {
            CanvasGroup group = _overlayInstance != null ? _overlayInstance.CanvasGroup : null;
            if (group == null || fadeDuration <= 0f)
            {
                if (group != null)
                    group.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            group.alpha = to;
        }

        private IEnumerator AnimateMusicDuck(float targetFactor, float duration)
        {
            AudioManager audio = AudioManager.Instance;
            if (audio == null)
                yield break;

            float from = _lastDuckFactor;
            float elapsed = 0f;
            if (duration <= 0f)
            {
                audio.SetMusicDuck(targetFactor);
                _lastDuckFactor = targetFactor;
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                audio.SetMusicDuck(Mathf.Lerp(from, targetFactor, t));
                yield return null;
            }

            audio.SetMusicDuck(targetFactor);
            _lastDuckFactor = targetFactor;
        }

        private IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — état / matériaux
        // ═══════════════════════════════════════════

        private void ResetPortraitState(AwakeningCeremonyView view)
        {
            if (view.PrimeRawImage != null)
            {
                view.PrimeRawImage.enabled = false;
                view.PrimeRawImage.material = null;
            }

            if (view.DechuRawImage != null)
            {
                view.DechuRawImage.enabled = false;
                view.DechuRawImage.material = null;
            }

            view.PrimeView?.Release();
            view.DechuView?.Release();

            CanvasGroup portraitGroup = GetPortraitCanvasGroup(view);
            if (portraitGroup != null)
                portraitGroup.alpha = 0f;

            SetContainerScale(view, dechuScale);
            SetContainerAnchoredPos(view, Vector2.zero);

            if (view.FlashOverlay != null)
            {
                Color c = UiTheme.CeremonyLight;
                c.a = 0f;
                view.FlashOverlay.color = c;
                view.FlashOverlay.gameObject.SetActive(false);
            }
        }

        private void HideBannerAndHint()
        {
            AwakeningCeremonyView view = _overlayInstance;
            if (view == null)
                return;

            if (view.BannerRoot != null)
            {
                view.BannerRoot.SetActive(false);
                view.BannerRoot.transform.localScale = Vector3.one;
            }

            SetGlowColorAlpha(view.BannerFlash, UiTheme.CeremonyLight, 0f);

            if (view.HintText != null)
            {
                Color c = view.HintText.color;
                c.a = 1f;
                view.HintText.color = c;
                view.HintText.gameObject.SetActive(false);
            }
        }

        private void ReleaseViewsAndMaterials()
        {
            AwakeningCeremonyView view = _overlayInstance;
            if (view != null)
            {
                if (view.PrimeRawImage != null)
                {
                    view.PrimeRawImage.enabled = false;
                    view.PrimeRawImage.material = null;
                }

                if (view.DechuRawImage != null)
                {
                    view.DechuRawImage.enabled = false;
                    view.DechuRawImage.material = null;
                }

                view.PrimeView?.Release();
                view.DechuView?.Release();
            }

            DestroyRuntimeMaterials();
        }

        private void EnsureDechuMaterial()
        {
            if (_runtimeDechuMat != null || dissolveMaterial == null)
                return;
            _runtimeDechuMat = new Material(dissolveMaterial);
            _runtimeDechuMat.SetFloat(DissolveAmountProperty, 0f);
            _runtimeDechuMat.SetFloat(WhiteoutProperty, 0f);
            _runtimeDechuMat.SetColor(WhiteoutColorProperty, UiTheme.CeremonyLight);
        }

        private void EnsurePrimeMaterial()
        {
            if (_runtimePrimeMat != null || dissolveMaterial == null)
                return;
            _runtimePrimeMat = new Material(dissolveMaterial);
            _runtimePrimeMat.SetFloat(DissolveAmountProperty, 0f);
            _runtimePrimeMat.SetFloat(WhiteoutProperty, 0f);
            _runtimePrimeMat.SetColor(WhiteoutColorProperty, UiTheme.CeremonyLight);
        }

        private void DestroyRuntimeMaterials()
        {
            if (_runtimeDechuMat != null)
            {
                Destroy(_runtimeDechuMat);
                _runtimeDechuMat = null;
            }

            if (_runtimePrimeMat != null)
            {
                Destroy(_runtimePrimeMat);
                _runtimePrimeMat = null;
            }
        }

        private static CanvasGroup GetPortraitCanvasGroup(AwakeningCeremonyView view)
        {
            if (view == null || view.PortraitContainer == null)
                return null;
            return view.PortraitContainer.GetComponent<CanvasGroup>();
        }

        private static void SetContainerScale(AwakeningCeremonyView view, float scale)
        {
            if (view == null || view.PortraitContainer == null)
                return;
            view.PortraitContainer.localScale = new Vector3(scale, scale, 1f);
        }

        private static void SetContainerAnchoredPos(AwakeningCeremonyView view, Vector2 pos)
        {
            if (view == null || view.PortraitContainer == null)
                return;
            view.PortraitContainer.anchoredPosition = pos;
        }

        private void EnsureDefaultMonteePulses()
        {
            if (monteePulses != null && monteePulses.length > 0)
                return;

            monteePulses = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.2f, 0.5f),
                new Keyframe(0.35f, 0.15f),
                new Keyframe(0.5f, 0.7f),
                new Keyframe(0.62f, 0.3f),
                new Keyframe(0.75f, 0.85f),
                new Keyframe(0.85f, 0.5f),
                new Keyframe(1f, 1f));
        }

        private static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }
    }
}
