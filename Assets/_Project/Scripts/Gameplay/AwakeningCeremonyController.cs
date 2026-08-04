using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Audio;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.UI;
using ChezArthur.UI.ArtworkTransition;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Orchestre la cérémonie d'éveil SSR (coque + cœur Ascension AW3).
    /// Singleton de scène. Temps unscaled. Tap pendant l'ascension = SkipToEnd ; après banner = sortie.
    /// </summary>
    public class AwakeningCeremonyController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string PrefSfxVolume = "AudioManager_SfxVolume";
        private const string AscensionStageName = "ArtworkTransitionStage";
        private const float MusicUnduckDuration = 1f;
        private const float HintFadeDuration = 0.45f;
        private const float EdgeWashHeight = 1100f;
        private const float BannerFlashBurstDuration = 0.4f;
        private const float BannerFlashPeakAlpha = 0.85f;
        private const float AmbienceVolumeFactor = 0.75f;
        private const float AscensionSkipGrace = 0.55f;
        private const float FallbackPrimeHold = 0.35f;
        private const int MaxMotes = 14;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private AwakeningCeremonyView overlayPrefab;
        // Conservé pour Configure / scènes (dissolve legacy) — plus utilisé par le cœur AW3.
        [SerializeField] private Material dissolveMaterial;

        [Header("Audio (sources créées par code)")]
        // riser/flash : conservés pour Configure / Hub preview / scènes (audio cœur → Driver AW3).
        [SerializeField] private AudioClip riserClip;
        [SerializeField] private AudioClip flashClip;
        [SerializeField] private AudioClip fanfareClip;
        [SerializeField] private AudioClip ambienceLoop;

        [Header("Timings")]
        [SerializeField] private float musicDuckDuration = 0.8f;
        [SerializeField] private float isolementDuration = 1.4f;
        [SerializeField] private float hintDelay = 1.8f;
        [SerializeField] private float fadeDuration = 0.55f;

        [Header("Échelle / FX")]
        [SerializeField] private float dechuScale = 0.72f;
        [SerializeField] private float bannerSlamScale = 1.8f;
        [SerializeField] private float bannerSlamDuration = 0.45f;

        [Header("Ascension artwork (AW3)")]
        [SerializeField] private ArtworkTransitionDriver artworkDriver;
        [SerializeField] private GameObject artworkStageRoot;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static AwakeningCeremonyController _instance;

        private AwakeningCeremonyView _overlayInstance;
        private Coroutine _playRoutine;
        private bool _skipRequested;
        private bool _tapArmed;
        private bool _ascensionPlaying;
        private float _ascensionSkipArmedAt;
        private bool _warnedMissingAscensionPair;
        private bool _ceremonyAudioActive;
        private bool _musicWasPlayingBeforeCeremony;
        private float _lastDuckFactor = 1f;
        private float _ceremonySfxVolume = 1f;

        private AudioSource _ambienceSource;
        private AudioSource _oneshotSource;

        private readonly Image[] _motes = new Image[MaxMotes];
        private readonly Vector2[] _moteBasePos = new Vector2[MaxMotes];
        private readonly Color[] _moteColors = new Color[MaxMotes];
        private int _moteCount;

        private RectTransform _raysRt;
        private float _raysAngle;

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
            EnsureAudioSources();
        }

        private void OnDisable()
        {
            // Coupure mid-beat (SetActive false) : pas de boucle orpheline, audio jeu restauré.
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            IsPlaying = false;
            _ascensionPlaying = false;
            _tapArmed = false;
            StopAmbienceImmediate();
            if (artworkDriver != null && artworkDriver.IsPlaying)
                artworkDriver.SkipToEnd();
            ApplyCeremonyAudio(false);
        }

        private void OnDestroy()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            StopAmbienceImmediate();
            ApplyCeremonyAudio(false);

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
            {
                BindAscensionStageFromOverlayInstance();
                return;
            }

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
            BindAscensionStageFromOverlayInstance();
        }

        /// <summary>
        /// Relie le stage AW3 depuis l'instance overlay (prefab runtime) — les refs
        /// sérialisées pointent sinon vers l'asset, pas le clone.
        /// </summary>
        private void BindAscensionStageFromOverlayInstance()
        {
            if (_overlayInstance == null)
                return;

            Transform stageTf = FindNamedChild(_overlayInstance.transform, AscensionStageName);
            if (stageTf == null)
                return;

            artworkStageRoot = stageTf.gameObject;
            artworkDriver = stageTf.GetComponent<ArtworkTransitionDriver>();
            if (artworkDriver == null)
                artworkDriver = stageTf.GetComponentInChildren<ArtworkTransitionDriver>(true);

            if (artworkStageRoot.activeSelf)
                artworkStageRoot.SetActive(false);
        }

        private static Transform FindNamedChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            if (root.name == childName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamedChild(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
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
                _moteBasePos[_moteCount] = img.rectTransform.anchoredPosition;
                _moteColors[_moteCount] = UiTheme.Gold;
                _moteColors[_moteCount].a = 0f;
                img.color = _moteColors[_moteCount];
                _moteCount++;
            }
        }

        private void OnTapRequested()
        {
            if (!IsPlaying)
                return;

            // Pendant l'ascension : skip le beat (pas la sortie cérémonie).
            if (_ascensionPlaying && artworkDriver != null)
            {
                if (Time.unscaledTime < _ascensionSkipArmedAt)
                    return;
                artworkDriver.SkipToEnd();
                return;
            }

            if (_tapArmed)
                _skipRequested = true;
        }

        private IEnumerator PlayCeremoniesRoutine(Action onComplete)
        {
            List<(OwnedCharacter owned, CharacterData data)> pending = CollectPending();

            ApplyCeremonyAudio(true);

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
            ApplyCeremonyAudio(true);

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
            TeardownAscensionStage(_overlayInstance);
            ReleaseViewsAndMaterials();
            ResetFxVisuals(_overlayInstance);

            if (_overlayInstance != null)
                _overlayInstance.gameObject.SetActive(false);

            yield return AnimateMusicDuck(1f, MusicUnduckDuration);
            ApplyCeremonyAudio(false);
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
            _ascensionPlaying = false;
            RefreshCeremonyVolume();

            yield return AnimateMusicDuck(0f, musicDuckDuration);

            if (view.CanvasGroup != null && view.CanvasGroup.alpha < 0.99f)
                yield return FadeCanvas(view.CanvasGroup.alpha, 1f);

            ResetPortraitState(view);
            EnsureEdgeWashes(view);
            ResetFxVisuals(view);
            TeardownAscensionStage(view);

            float ambienceTarget = _ceremonySfxVolume * AmbienceVolumeFactor;
            yield return ParallelIsolementAndAmbience(ambienceTarget);

            // ── CŒUR AW3 : Ascension (remplace présence / montée / flash / révélation) ──
            yield return PlayAscensionHeart(view, data);

            // ── COQUE : banner slam + fanfare, hint, persist, tap-to-continue ──
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
            TeardownAscensionStage(view);
            HideBannerAndHint();
        }

        /// <summary>
        /// Cœur AW3 : PlayAscension si couple + driver, sinon fallback prime legacy.
        /// </summary>
        private IEnumerator PlayAscensionHeart(AwakeningCeremonyView view, CharacterData data)
        {
            if (HasPrimeDechuPair(data) && artworkDriver != null)
            {
                HideLegacyPortraitSpectacle(view);

                if (artworkStageRoot != null)
                    artworkStageRoot.SetActive(true);

                // Prefab AW1 ~62 % — trop petit pour la cérémonie ; override instance (comme AW2 gacha).
                LayoutAscensionCardToFillStage();

                bool done = false;
                _ascensionPlaying = true;
                // Empêche un tap résiduel (fade / isolement) de skipper le beat.
                _ascensionSkipArmedAt = Time.unscaledTime + AscensionSkipGrace;

                var prime = new AnimatedPortraitFrameSource(data.AnimatedPortraitPrime);
                var dechu = new AnimatedPortraitFrameSource(data.AnimatedPortraitDechu);
                // API Driver : (prime, dechu) — SetPortraits interne pose déchu devant, prime derrière.
                artworkDriver.PlayAscension(prime, dechu, () => { done = true; });

                while (!done)
                    yield return null;

                _ascensionPlaying = false;
                yield break;
            }

            if (!_warnedMissingAscensionPair)
            {
                _warnedMissingAscensionPair = true;
                string id = data != null ? data.Id : "?";
                Debug.LogWarning(
                    "[AwakeningCeremonyController] Ascension indisponible " +
                    "(driver ou couple prime/déchu manquant) — fallback prime : " + id,
                    this);
            }

            yield return ShowPrimeFallback(view, data);
        }

        private static bool HasPrimeDechuPair(CharacterData data)
        {
            return data != null
                && data.AnimatedPortraitPrime != null
                && data.AnimatedPortraitDechu != null;
        }

        /// <summary>
        /// Masque le spectacle legacy (RawImages / motes / rays) pendant le beat Ascension.
        /// </summary>
        private void HideLegacyPortraitSpectacle(AwakeningCeremonyView view)
        {
            if (view == null)
                return;

            if (view.DechuRawImage != null)
            {
                view.DechuRawImage.enabled = false;
                view.DechuRawImage.material = null;
            }

            if (view.PrimeRawImage != null)
            {
                view.PrimeRawImage.enabled = false;
                view.PrimeRawImage.material = null;
            }

            view.DechuView?.Release();
            view.PrimeView?.Release();

            if (view.PortraitContainer != null)
                view.PortraitContainer.gameObject.SetActive(false);

            ResetFxVisuals(view);
        }

        /// <summary>
        /// Fallback si Ascension non jouable : affiche le prime via les vues existantes.
        /// </summary>
        private IEnumerator ShowPrimeFallback(AwakeningCeremonyView view, CharacterData data)
        {
            if (view == null || data == null)
                yield break;

            if (view.PortraitContainer != null)
                view.PortraitContainer.gameObject.SetActive(true);

            view.PrimeView?.ShowState(data, data.AnimatedPortraitPrime);
            if (view.PrimeRawImage != null)
                view.PrimeRawImage.enabled = true;

            SetContainerScale(view, dechuScale);
            SetContainerAnchoredPos(view, Vector2.zero);

            CanvasGroup portraitGroup = GetPortraitCanvasGroup(view);
            if (portraitGroup != null)
                portraitGroup.alpha = 1f;

            yield return WaitUnscaled(FallbackPrimeHold);
        }

        /// <summary>
        /// Aligne la Card du stage sur la zone portrait (stretch plein parent).
        /// Override d'instance uniquement — prefab AW1 intouché.
        /// </summary>
        private void LayoutAscensionCardToFillStage()
        {
            if (artworkStageRoot == null)
                return;

            ArtworkTransitionView stageView =
                artworkStageRoot.GetComponent<ArtworkTransitionView>();
            if (stageView == null)
                stageView = artworkStageRoot.GetComponentInChildren<ArtworkTransitionView>(true);

            RectTransform cardRt = stageView != null ? stageView.CardRect : null;
            if (cardRt == null)
                return;

            cardRt.anchorMin = Vector2.zero;
            cardRt.anchorMax = Vector2.one;
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;
            cardRt.sizeDelta = Vector2.zero;
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.localScale = Vector3.one;
            cardRt.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Silence audio jeu pendant la cérémonie (pattern gacha ApplyCeremonyAudio).
        /// L'ambiance propre de la cérémonie (_ambienceSource) n'est pas touchée.
        /// </summary>
        private void ApplyCeremonyAudio(bool ceremony)
        {
            if (AudioManager.Instance == null)
                return;

            if (ceremony)
            {
                if (_ceremonyAudioActive)
                    return;

                _ceremonyAudioActive = true;
                _musicWasPlayingBeforeCeremony = AudioManager.Instance.IsMusicPlaying;
                AudioManager.Instance.StopAmbiance();
                AudioManager.Instance.SetMusicDuck(0f);
                _lastDuckFactor = 0f;
                if (_musicWasPlayingBeforeCeremony)
                    AudioManager.Instance.PauseMusic();
            }
            else
            {
                if (!_ceremonyAudioActive)
                {
                    AudioManager.Instance.SetMusicDuck(1f);
                    _lastDuckFactor = 1f;
                    return;
                }

                _ceremonyAudioActive = false;
                AudioManager.Instance.SetMusicDuck(1f);
                _lastDuckFactor = 1f;
                if (_musicWasPlayingBeforeCeremony)
                {
                    AudioManager.Instance.ResumeMusic();
                    _musicWasPlayingBeforeCeremony = false;
                }

                AudioManager.Instance.PlayAmbiance();
            }
        }

        /// <summary>
        /// Désactive le stage Ascension et rétablit le PortraitContainer (comme AW2 gacha).
        /// </summary>
        private void TeardownAscensionStage(AwakeningCeremonyView view)
        {
            _ascensionPlaying = false;

            if (artworkDriver != null && artworkDriver.IsPlaying)
                artworkDriver.SkipToEnd();

            if (artworkStageRoot != null)
            {
                ArtworkTransitionView stageView =
                    artworkStageRoot.GetComponent<ArtworkTransitionView>();
                if (stageView == null)
                    stageView = artworkStageRoot.GetComponentInChildren<ArtworkTransitionView>(true);

                if (stageView != null)
                {
                    stageView.StopAllAudio();
                    stageView.ResetVisuals();
                }

                artworkStageRoot.SetActive(false);
            }

            if (view != null && view.PortraitContainer != null)
                view.PortraitContainer.gameObject.SetActive(true);
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

        private static void SetEdgeWashesAlpha(AwakeningCeremonyView view, float alpha)
        {
            if (view == null)
                return;
            SetGlowColorAlpha(view.EdgeWashTop, UiTheme.CeremonyLight, alpha);
            SetGlowColorAlpha(view.EdgeWashBottom, UiTheme.CeremonyLight, alpha);
        }

        /// <summary>
        /// Crée EdgeWashTop/Bottom full-width si absents du prefab (preview sans rebuild).
        /// Sibling juste au-dessus du Background pour saigner sous le portrait.
        /// </summary>
        private void EnsureEdgeWashes(AwakeningCeremonyView view)
        {
            if (view == null)
                return;
            if (view.EdgeWashTop != null && view.EdgeWashBottom != null)
                return;

            Sprite sprite = view.AmbientGlow != null ? view.AmbientGlow.sprite : null;
            Material mat = view.AmbientGlow != null ? view.AmbientGlow.material : null;
            Transform root = view.transform;

            Image top = view.EdgeWashTop;
            if (top == null)
            {
                top = CreateEdgeWash(root, "EdgeWashTop", sprite, mat, top: true);
                // Derrière AmbientGlow / portrait : juste après Background
                int bgIndex = FindChildIndex(root, "Background");
                if (bgIndex >= 0)
                    top.transform.SetSiblingIndex(bgIndex + 1);
            }

            Image bottom = view.EdgeWashBottom;
            if (bottom == null)
            {
                bottom = CreateEdgeWash(root, "EdgeWashBottom", sprite, mat, top: false);
                int insertAt = top != null ? top.transform.GetSiblingIndex() + 1 : FindChildIndex(root, "Background") + 1;
                if (insertAt > 0)
                    bottom.transform.SetSiblingIndex(insertAt);
            }

            view.BindRuntimeEdgeWashes(top, bottom);
        }

        private static Image CreateEdgeWash(
            Transform parent, string name, Sprite sprite, Material mat, bool top)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Image existingImg = existing.GetComponent<Image>();
                if (existingImg != null)
                    return existingImg;
            }

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            if (top)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
            }

            rt.anchoredPosition = Vector2.zero;
            // Largeur stretch (sizeDelta.x=0) ; hauteur généreuse pour ovale soft full-width
            rt.sizeDelta = new Vector2(0f, EdgeWashHeight);

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            if (mat != null)
                img.material = mat;
            img.raycastTarget = false;
            img.maskable = false;
            Color c = UiTheme.CeremonyLight;
            c.a = 0f;
            img.color = c;
            return img;
        }

        private static int FindChildIndex(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == childName)
                    return i;
            }

            return -1;
        }

        private void ResetFxVisuals(AwakeningCeremonyView view)
        {
            if (view == null)
                return;

            SetGlowColorAlpha(view.AmbientGlow, UiTheme.CeremonyLight, 0f);
            SetEdgeWashesAlpha(view, 0f);
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
            if (view == null)
                return;

            if (view.PortraitContainer != null)
                view.PortraitContainer.gameObject.SetActive(true);

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

        private static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }
    }
}
