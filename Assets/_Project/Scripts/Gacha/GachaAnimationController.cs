using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.UI;
using ChezArthur.UI.ArtworkTransition;
using ChezArthur.Core;
using ChezArthur.Hub;
using ChezArthur.Hub.Pages;
using ChezArthur.Hub.Pages.Invocation;
using ChezArthur.Audio;
using ChezArthur.UI.RevealStage;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// ContrÃ´le la sÃ©quence d'animation complÃ¨te du gacha.
    /// </summary>
    public class GachaAnimationController : MonoBehaviour
    {
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // CONSTANTES
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        /// <summary> Espace bas du rÃ©cap pour laisser la nav Hub cliquable. </summary>
        private const float NAV_CLEARANCE = 280f;

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // SERIALIZED FIELDS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        [Header("ScÃ¨nes")]
        [SerializeField] private GameObject crankScene;
        [SerializeField] private GameObject revealScene;

        [Header("SÃ©quence train (Gate 3)")]
        [SerializeField] private TrainSequenceController trainSequence;

        [Header("Manivelle")]
        [SerializeField] private LeverController crankController;

        [Header("Artwork (pipeline portraits unifiÃ©)")]
        [SerializeField] private CharacterArtworkView artworkView;
        [SerializeField] private RawImage artworkRawImage;
        // artworkRawImage sert UNIQUEMENT aux manipulations de couleur/visibilitÃ©
        // que faisait l'ancien code sur l'Image ; l'affichage passe par la view.
        [SerializeField] private float revealDuration = 2f;

        [Header("Déchéance artwork (AW2)")]
        [SerializeField] private ArtworkTransitionDriver artworkDriver;
        [SerializeField] private GameObject artworkStageRoot;

        [Header("Reveal — Entrée en scène (INVR2)")]
        [SerializeField] private RevealStageDirector revealDirector;
        [SerializeField] private RevealStageConfig revealConfig;
        [SerializeField] private Button skipAllButton;

        [Header("Parallax")]
        [SerializeField] private ParallaxManager parallaxManager;

        [Header("Tap to Continue")]
        [SerializeField] private GameObject tapToContinueText;
        [SerializeField] private Button tapArea; // Bouton invisible plein Ã©cran

        [Header("Ã‰lÃ©ments Ã  cacher / stage")]
        [SerializeField] private GameObject invocationPageBackground;
        [Tooltip("InfoBar + NavigationBar (SafeArea).")]
        [SerializeField] private GameObject hubChrome;
        [Tooltip("Pages Hub Ã  masquer (Accueil, Ã‰quipeâ€¦). Auto-rempli si vide.")]
        [SerializeField] private GameObject[] hubPagesToHide;
        [Tooltip("Bouton Preview Ã©veil / debug hors SafeArea.")]
        [SerializeField] private GameObject debugPreviewRoot;
        [SerializeField] private Image stageBackdrop;
        [SerializeField] private float musicDuckFactor = 0f;

        [Header("RÃ©capitulatif")]
        [SerializeField] private GameObject summaryScene;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private Transform singleContainer;
        [SerializeField] private PullResultEntryUI summaryEntryPrefab;
        [SerializeField] private PullResultEntryUI singleCardPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button repullButton;
        [SerializeField] private TextMeshProUGUI repullLabelText;
        [SerializeField] private TextMeshProUGUI repullCostText;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private HubManager hubManager;
        [SerializeField] private CharacterDetailPopup characterDetailPopup;

        [Header("RÃ©fÃ©rences")]
        [SerializeField] private CharacterDatabase characterDatabase;

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // EVENTS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        public event Action OnAnimationComplete;

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // VARIABLES PRIVÃ‰ES
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private List<PulledCharacter> _charactersToReveal;
        private int _currentRevealIndex;
        private bool _isAnimating = false;
        private bool _waitingForTap = false;
        private bool _decheancePlaying;
        private float _decheanceSkipArmedAt;
        private bool _warnedMissingPrimeDechuPair;
        private readonly List<PullResultEntryUI> _gridPool = new List<PullResultEntryUI>();
        private readonly List<PullResultEntryUI> _singlePool = new List<PullResultEntryUI>();
        private Sprite _runtimeSmokeSprite;
        private bool[] _hubPageWasActive;
        private bool _debugWasActive = true;
        private bool _firstRevealPreparedUnderVeil;
        private RevealInfoPanel _infoPanel;
        private Coroutine _infoRoutine;
        private bool _fakeoutUsedThisPull;
        private bool _skipAllRequested;
        private float _tapAdvanceArmedAt;

        private GachaPullResult _currentResult;
        private BannerData _currentBanner;
        private bool _wasMulti;
        private bool _watchingSummaryPageChanges;
        private int _detailPopupSiblingIndex = -1;
        private Vector2 _stageBackdropOffsetMin;
        private bool _musicWasPlayingBeforeCeremony;
        private bool _ceremonyAudioActive;

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // UNITY LIFECYCLE
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private void Awake()
        {
            EnsureSfxManagerExists();

            if (crankController == null)
            {
                Debug.LogError(
                    "[Gacha] crankController non cÃ¢blÃ© â€” la sÃ©quence " +
                    "restera bloquÃ©e aprÃ¨s la manivelle",
                    this);
            }
            else
            {
                crankController.OnCrankComplete += OnCrankComplete;
            }

            // Cacher tout au dÃ©part
            HideAllScenes();

            // S'abonner au tap
            if (tapArea != null)
                tapArea.onClick.AddListener(OnTapToContinue);

            // S'abonner au bouton fermer
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);

            if (repullButton != null)
                repullButton.onClick.AddListener(OnRepullClicked);

            if (skipAllButton != null)
                skipAllButton.onClick.AddListener(OnSkipAllClicked);
        }

        private void OnDestroy()
        {
            UnsubscribeSummaryPageWatch();

            // DÃ©sabonnement obligatoire â€” Ã©vite les leaks d'event si le GO est dÃ©truit.
            if (crankController != null)
                crankController.OnCrankComplete -= OnCrankComplete;

            if (tapArea != null)
                tapArea.onClick.RemoveListener(OnTapToContinue);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);

            if (repullButton != null)
                repullButton.onClick.RemoveListener(OnRepullClicked);

            if (skipAllButton != null)
                skipAllButton.onClick.RemoveListener(OnSkipAllClicked);

            if (_runtimeSmokeSprite != null)
            {
                Destroy(_runtimeSmokeSprite);
                _runtimeSmokeSprite = null;
            }
        }

        private void OnDisable()
        {
            // Fermeture / interruption de l'Ã©cran gacha : libÃ¨re le portrait chargÃ©.
            if (artworkView != null)
                artworkView.Release();
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // MÃ‰THODES PUBLIQUES
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <summary>
        /// Lance l'animation de gacha (surcharge legacy â€” sans banniÃ¨re / re-pull).
        /// </summary>
        public bool StartAnimation(GachaPullResult result)
        {
            return StartAnimation(result, null, false);
        }

        /// <summary>
        /// Lance l'animation de gacha avec les personnages Ã  rÃ©vÃ©ler.
        /// </summary>
        /// <returns>false si une garde a refusÃ© le dÃ©marrage (fallback ShowResultDirect).</returns>
        public bool StartAnimation(GachaPullResult result, BannerData banner, bool isMulti)
        {
            if (_isAnimating)
            {
                Debug.LogWarning(
                    "[Gacha] StartAnimation refusÃ© â€” une animation est dÃ©jÃ  en cours.",
                    this);
                return false;
            }

            if (result == null)
            {
                Debug.LogError("[Gacha] StartAnimation refusÃ© â€” result null.", this);
                return false;
            }

            if (result.characters == null || result.characters.Count == 0)
            {
                Debug.LogError(
                    "[Gacha] StartAnimation refusÃ© â€” aucun personnage dans le rÃ©sultat.",
                    this);
                return false;
            }

            _isAnimating = true;
            _currentResult = result;
            _currentBanner = banner;
            _wasMulti = isMulti;
            _charactersToReveal = result.characters;
            _currentRevealIndex = 0;
            _fakeoutUsedThisPull = false;
            _skipAllRequested = false;
            if (skipAllButton != null)
                skipAllButton.gameObject.SetActive(false);

            EnsurePremiumStage();
            EnsureSfxManagerExists();

            // Parent d'abord : Awake peut appeler HideAllScenes â€” les scÃ¨nes sont
            // rÃ©activÃ©es ensuite, sinon CrankScene est immÃ©diatement re-masquÃ©e.
            gameObject.SetActive(true);
            SetExclusiveMode(true);
            ApplyCeremonyAudio(true);

            HideAllScenes();
            if (crankScene != null)
                crankScene.SetActive(true);
            EnsureCrankInputReceivable();
            return true;
        }

        /// <summary>
        /// Repli legacy : rÃ©cap direct sans banniÃ¨re.
        /// </summary>
        public void ShowResultDirect(GachaPullResult result)
        {
            ShowResultDirect(result, null, false);
        }

        /// <summary>
        /// Repli : affiche directement le rÃ©capitulatif (sans crank / porte / reveals).
        /// Garantit que le joueur voit toujours ce qu'il a payÃ©.
        /// </summary>
        public void ShowResultDirect(GachaPullResult result, BannerData banner, bool isMulti)
        {
            if (result == null || result.characters == null || result.characters.Count == 0)
            {
                Debug.LogError(
                    "[Gacha] ShowResultDirect refusÃ© â€” rÃ©sultat null ou vide.",
                    this);
                return;
            }

            StopAllCoroutines();
            _waitingForTap = false;
            _isAnimating = true;
            _currentResult = result;
            _currentBanner = banner;
            _wasMulti = isMulti;
            _charactersToReveal = result.characters;
            _currentRevealIndex = 0;

            EnsurePremiumStage();
            EnsureSfxManagerExists();

            gameObject.SetActive(true);
            SetExclusiveMode(true);
            ApplyCeremonyAudio(true);

            HideAllScenes();
            ShowSummary();
        }

        /// <summary>
        /// Force l'arrÃªt de l'animation (si besoin).
        /// </summary>
        public void StopAnimation()
        {
            StopAllCoroutines();
            _waitingForTap = false;
            _isAnimating = false;

            UnsubscribeSummaryPageWatch();
            RestoreDetailPopupSibling();
            ApplySummaryBackdropClearance(false);

            revealDirector?.ResetVisuals();
            StopInfo();

            if (trainSequence != null)
            {
                trainSequence.ReleaseDoorSheet();
                trainSequence.HideSequenceScenes();
            }

            ClearSummaryEntries();
            HideAllScenes();

            // Restaurer la vitesse du parallax
            if (parallaxManager != null)
                parallaxManager.SetSpeedMultiplier(1f);

            SetExclusiveMode(false);
            ApplyCeremonyAudio(false);
            gameObject.SetActive(false);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // CALLBACKS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private void OnCrankComplete()
        {
            StartCoroutine(RunTrainThenReveal());
        }

        private void OnTapToContinue()
        {
            // Pendant la déchéance : skip propre (pas d'avance du reveal).
            if (_decheancePlaying && artworkDriver != null)
            {
                if (Time.unscaledTime < _decheanceSkipArmedAt)
                    return;
                artworkDriver.SkipToEnd();
                return;
            }

            if (revealDirector != null && revealDirector.IsPlaying)
            {
                revealDirector.SkipToSnap();
                return;
            }

            if (_waitingForTap && Time.unscaledTime >= _tapAdvanceArmedAt)
                _waitingForTap = false;
        }

        private void OnSkipAllClicked()
        {
            _skipAllRequested = true;
            if (skipAllButton != null)
                skipAllButton.gameObject.SetActive(false);
            if (revealDirector != null && revealDirector.IsPlaying)
                revealDirector.SkipToSnap();
            _waitingForTap = false;
        }

        private void OnCloseButtonClicked()
        {
            CompleteAnimation();
        }

        private void OnRepullClicked()
        {
            if (_currentBanner == null)
                return;

            if (PersistentManager.Instance == null || PersistentManager.Instance.Gacha == null)
            {
                Debug.LogWarning("[Gacha] Re-pull impossible â€” Gacha null.");
                RefreshRepullButton();
                return;
            }

            GachaManager gacha = PersistentManager.Instance.Gacha;
            if (!gacha.CanPull(_currentBanner, _wasMulti))
            {
                Debug.LogWarning("[Gacha] Re-pull refusÃ© â€” CanPull false.");
                RefreshRepullButton();
                return;
            }

            GachaPullResult result = _wasMulti
                ? gacha.PullMulti(_currentBanner)
                : gacha.PullSingle(_currentBanner);

            if (result == null)
            {
                Debug.LogWarning("[Gacha] Re-pull Ã©chouÃ© â€” rÃ©sultat null.");
                RefreshRepullButton();
                return;
            }

            BannerData banner = _currentBanner;
            bool isMulti = _wasMulti;
            ResetForNewRun();
            StartAnimation(result, banner, isMulti);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // COROUTINES â€” SÃ‰QUENCE D'ANIMATION
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        /// <summary>
        /// AprÃ¨s manivelle : sÃ©quence train â†’ reveals.
        /// </summary>
        private IEnumerator RunTrainThenReveal()
        {
            if (crankScene != null)
                crankScene.SetActive(false);

            CharacterRarity bestRarity = ComputeBestRarity();
            _firstRevealPreparedUnderVeil = false;

            if (trainSequence != null)
            {
                yield return trainSequence.PlaySequence(
                    bestRarity,
                    PrepareFirstRevealUnderVeil);
            }
            else
            {
                Debug.LogError(
                    "[Gacha] TrainSequenceController absent â€” passage direct au reveal.",
                    this);
                yield return PrepareFirstRevealUnderVeil();
            }

            yield return RevealSequence();
        }

        /// <summary>
        /// Sous le voile opaque : active Reveal + charge le 1er artwork en mode pixel (palier 0).
        /// La rÃ©solution + SFX se jouent APRÃˆS le fondu, visibles.
        /// </summary>
        private IEnumerator PrepareFirstRevealUnderVeil()
        {
            PrepareRevealAfterSmoke();

            if (_charactersToReveal == null || _charactersToReveal.Count == 0)
                yield break;

            PulledCharacter first = _charactersToReveal[0];
            CharacterData data = characterDatabase?.GetById(first.characterId);

            if (tapToContinueText != null)
                tapToContinueText.SetActive(false);

            if (artworkView != null && data != null)
            {
                ShowRevealArtwork(data, first);
                LayoutRevealArtwork();
                Canvas.ForceUpdateCanvases();
                artworkView.ForceCoverMode();
            }

            if (artworkRawImage != null)
            {
                artworkRawImage.enabled = true;
                artworkRawImage.color = Color.white;
                LayoutRevealArtwork();
            }

            // Arme le palier 0 sous le voile â€” pas de SFX / pas de resolve ici.
            if (revealDirector != null)
            {
                revealDirector.Bind(artworkRawImage, artworkView);
                if (artworkView != null)
                    artworkView.SetAnimationPaused(true);
                EnsureInfoPanel();
                if (_infoPanel != null)
                    _infoPanel.HideImmediate();
            }
            else
            {
                Debug.LogError("[Gacha] revealDirector non câblé — INVR2.", this);
            }

            yield return null;

            _firstRevealPreparedUnderVeil = true;
        }

        /// <summary>
        /// AppelÃ© au pic de la fumÃ©e : Door/Train off, Reveal prÃªt.
        /// </summary>
        private void PrepareRevealAfterSmoke()
        {
            if (trainSequence != null)
                trainSequence.HideSequenceScenes();

            if (revealScene != null)
                revealScene.SetActive(true);
        }

        private CharacterRarity ComputeBestRarity()
        {
            CharacterRarity best = CharacterRarity.SR;
            if (_charactersToReveal == null)
                return best;

            for (int i = 0; i < _charactersToReveal.Count; i++)
            {
                CharacterRarity r = _charactersToReveal[i].rarity;
                if ((int)r > (int)best)
                    best = r;
            }

            return best;
        }

        private IEnumerator RevealSequence()
        {
            if (trainSequence != null)
                trainSequence.HideSequenceScenes();
            if (revealScene != null)
                revealScene.SetActive(true);

            for (int i = 0; i < _charactersToReveal.Count; i++)
            {
                _currentRevealIndex = i;

                if (_wasMulti && i >= 1 && !_skipAllRequested && skipAllButton != null)
                    skipAllButton.gameObject.SetActive(true);

                if (i > 0 && revealDirector != null && revealConfig != null)
                {
                    StartCoroutine(revealDirector.CoPlayExit());
                    float wait = Mathf.Max(0f, revealConfig.exitDim - revealConfig.entryOverlap);
                    float t = 0f;
                    while (t < wait)
                    {
                        t += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }

                yield return StartCoroutine(RevealCharacter(_charactersToReveal[i]));
            }

            if (revealDirector != null)
            {
                yield return revealDirector.CoPlayExit();
                revealDirector.ResetVisuals();
            }

            yield return new WaitForSecondsRealtime(0.3f);
            ShowSummary();
        }

        private IEnumerator RevealCharacter(PulledCharacter pulled)
        {
            CharacterData data = characterDatabase?.GetById(pulled.characterId);

            bool artAlreadyReady =
                _firstRevealPreparedUnderVeil && _currentRevealIndex == 0;
            if (artAlreadyReady)
                _firstRevealPreparedUnderVeil = false;

            if (tapToContinueText != null)
                tapToContinueText.SetActive(false);

            if (!artAlreadyReady)
            {
                if (artworkView != null && data != null)
                {
                    ShowRevealArtwork(data, pulled);
                    LayoutRevealArtwork();
                    Canvas.ForceUpdateCanvases();
                    artworkView.ForceCoverMode();
                }

                if (artworkRawImage != null)
                {
                    artworkRawImage.enabled = true;
                    artworkRawImage.color = Color.white;
                    LayoutRevealArtwork();
                    Canvas.ForceUpdateCanvases();
                    if (artworkView != null)
                        artworkView.ForceCoverMode();
                }
            }
            else if (artworkRawImage != null)
            {
                // 1ère carte : artwork déjà sous le voile — forcer cover avant l'entrée en scène.
                artworkRawImage.enabled = true;
                LayoutRevealArtwork();
                Canvas.ForceUpdateCanvases();
                if (artworkView != null)
                    artworkView.ForceCoverMode();
            }

            artworkView?.SetAnimationPaused(true);
            bool playBeat = ShouldPlayDecheance(data, pulled);
            bool fakeout = ComputeFakeout(pulled);
            Vector2 focal = data != null ? data.portraitFocalPoint : new Vector2(0.5f, 0.55f);

            if (revealDirector == null)
            {
                Debug.LogError("[Gacha] revealDirector null in RevealCharacter", this);
                yield break;
            }

            Coroutine arrival = StartCoroutine(revealDirector.CoPlayArrival(
                pulled.rarity, fakeout, focal,
                onSnap: () =>
                {
                    _tapAdvanceArmedAt = Time.unscaledTime + 0.55f;
                    if (!playBeat && artworkView != null)
                        artworkView.SetAnimationPaused(false);
                },
                suppressSnapSfx: playBeat,
                skipSettle: true));
            if (_skipAllRequested)
                revealDirector.SkipToSnap();
            yield return arrival;

            if (playBeat)
            {
                yield return PlayDecheanceBeat(data);
                if (artworkView != null && data != null)
                {
                    artworkView.Show(data);
                    artworkView.ForceCoverMode();
                    artworkView.SetAnimationPaused(false);
                }
                TeardownDecheanceStage();
            }

            // Artwork brut pendant carte-titre / stamp (plus de pénombre Bayer résiduelle).
            revealDirector.PresentCleanArtwork();

            if (!_skipAllRequested)
            {
                EnsureInfoPanel();
                if (_infoPanel != null)
                {
                    _infoRoutine = StartCoroutine(_infoPanel.CoPlay(BuildPayload(data, pulled)));
                }
                if (tapToContinueText != null)
                    tapToContinueText.SetActive(true);
                _waitingForTap = true;
                while (_waitingForTap)
                    yield return null;
                if (tapToContinueText != null)
                    tapToContinueText.SetActive(false);
                StopInfo();
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.35f);
            }

            TeardownDecheanceStage(); // safety no-op
        }

        private void EnsureInfoPanel()
        {
            if (revealScene == null) return;
            _infoPanel = RevealInfoPanel.EnsureUnder(revealScene.transform);
            if (_infoPanel == null) return;
            _infoPanel.Configure(revealConfig);
            if (revealDirector != null)
                _infoPanel.BindFx(revealDirector.Fx);
        }

        private void StopInfo()
        {
            if (_infoRoutine != null)
            {
                StopCoroutine(_infoRoutine);
                _infoRoutine = null;
            }
            if (_infoPanel != null)
                _infoPanel.HideImmediate();
        }

        private bool ComputeFakeout(PulledCharacter pulled)
        {
            if (_fakeoutUsedThisPull || pulled == null) return false;
            bool eligible = pulled.rarity == CharacterRarity.LR
                || (pulled.rarity == CharacterRarity.SSR && pulled.isPity);
            if (!eligible) return false;
            _fakeoutUsedThisPull = true;
            return true;
        }

        private RevealInfoPanel.Payload BuildPayload(CharacterData data, PulledCharacter pulled)
        {
            var payload = new RevealInfoPanel.Payload
            {
                name = data != null ? data.CharacterName : string.Empty,
                rarity = pulled.rarity,
                isNew = pulled.isNew,
                prevLevel = pulled.previousLevel,
                newLevel = pulled.newLevel,
                isMax = !pulled.isNew && (pulled.previousLevel >= CharacterData.MAX_LEVEL
                    || pulled.newLevel <= pulled.previousLevel),
                statDeltas = null
            };

            if (!pulled.isNew && !payload.isMax && data != null)
            {
                var list = new System.Collections.Generic.List<(string, int)>(4);
                int dHp = data.GetHpAtLevel(pulled.newLevel) - data.GetHpAtLevel(pulled.previousLevel);
                int dAtk = data.GetAtkAtLevel(pulled.newLevel) - data.GetAtkAtLevel(pulled.previousLevel);
                int dDef = data.GetDefAtLevel(pulled.newLevel) - data.GetDefAtLevel(pulled.previousLevel);
                int dSpd = data.GetSpeedAtLevel(pulled.newLevel) - data.GetSpeedAtLevel(pulled.previousLevel);
                if (dHp != 0) list.Add(("HP", dHp));
                if (dAtk != 0) list.Add(("ATK", dAtk));
                if (dDef != 0) list.Add(("DEF", dDef));
                if (dSpd != 0) list.Add(("SPD", dSpd));
                if (list.Count > 0)
                    payload.statDeltas = list.ToArray();
            }
            return payload;
        }

        /// <summary>
        /// Charge l'artwork de reveal : Prime si beat déchéance, sinon résolution éveil / déchu.
        /// </summary>
        private void ShowRevealArtwork(CharacterData data, PulledCharacter pulled)
        {
            if (artworkView == null || data == null || pulled == null)
                return;

            if (ShouldPlayDecheance(data, pulled))
            {
                artworkView.ShowState(data, data.AnimatedPortraitPrime);
                return;
            }

            if (!pulled.isNew
                && PersistentManager.Instance != null
                && PersistentManager.Instance.Characters != null)
            {
                OwnedCharacter owned =
                    PersistentManager.Instance.Characters.GetOwnedCharacter(data.Id);
                if (owned != null)
                {
                    artworkView.Show(data, owned);
                    return;
                }
            }

            artworkView.Show(data);
        }

        /// <summary>
        /// True si ce pull doit jouer le beat Déchéance AW2 (nouveau + couple data + driver câblé).
        /// </summary>
        private bool ShouldPlayDecheance(CharacterData data, PulledCharacter pulled)
        {
            if (pulled == null || !pulled.isNew || data == null)
                return false;

            if (!HasPrimeDechuPair(data))
            {
                // Driver câblé mais perso sans couple → fallback classique + warning unique/session.
                if (artworkDriver != null && !_warnedMissingPrimeDechuPair)
                {
                    _warnedMissingPrimeDechuPair = true;
                    Debug.LogWarning(
                        "[Gacha] Nouveau perso sans couple prime/déchu — reveal classique : "
                        + data.Id,
                        this);
                }

                return false;
            }

            return artworkDriver != null;
        }

        private static bool HasPrimeDechuPair(CharacterData data)
        {
            return data != null
                && data.AnimatedPortraitPrime != null
                && data.AnimatedPortraitDechu != null;
        }

        /// <summary>
        /// Joue la déchéance sur le stage AW1 ; tap = SkipToEnd. Attend la fin.
        /// </summary>
        private IEnumerator PlayDecheanceBeat(CharacterData data)
        {
            if (artworkDriver == null || data == null)
                yield break;

            if (artworkRawImage != null)
                artworkRawImage.enabled = false;

            if (artworkStageRoot != null)
                artworkStageRoot.SetActive(true);

            // Carte plein cadre reveal (le prefab AW1 est en ~62 % — trop petit pour le gacha).
            LayoutDecheanceCardToMatchReveal();

            bool done = false;
            _decheancePlaying = true;
            // Empêche le tap résiduel de la résolution pixel de skipper le beat.
            _decheanceSkipArmedAt = Time.unscaledTime + 0.55f;

            var prime = new AnimatedPortraitFrameSource(data.AnimatedPortraitPrime);
            var dechu = new AnimatedPortraitFrameSource(data.AnimatedPortraitDechu);
            artworkDriver.PlayDecheance(prime, dechu, () => { done = true; });

            while (!done)
                yield return null;

            _decheancePlaying = false;
        }

        /// <summary>
        /// Aligne la Card du stage sur le RawImage reveal (plein stretch).
        /// </summary>
        private void LayoutDecheanceCardToMatchReveal()
        {
            if (artworkStageRoot == null)
                return;

            ArtworkTransitionView stageView =
                artworkStageRoot.GetComponent<ArtworkTransitionView>();
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
        /// Désactive le stage et rétablit l'artwork reveal classique.
        /// </summary>
        private void TeardownDecheanceStage()
        {
            _decheancePlaying = false;

            if (artworkDriver != null && artworkDriver.IsPlaying)
                artworkDriver.SkipToEnd();

            if (artworkStageRoot != null)
            {
                ArtworkTransitionView stageView =
                    artworkStageRoot.GetComponent<ArtworkTransitionView>();
                if (stageView != null)
                {
                    stageView.StopAllAudio();
                    stageView.ResetVisuals();
                }

                artworkStageRoot.SetActive(false);
            }

            if (artworkRawImage != null)
                artworkRawImage.enabled = true;
        }

        private void ShowSummary()
        {
            // RÃ©cap : plus d'artwork plein Ã©cran â€” libÃ©rer la texture reveal.
            if (artworkView != null)
                artworkView.Release();

            if (revealScene != null)
                revealScene.SetActive(false);

            // Nav Hub visible / cliquable sous le récap (clearance bas).
            if (hubChrome != null)
                hubChrome.SetActive(true);
            ApplySummaryBackdropClearance(true);
            SubscribeSummaryPageWatch();

            if (summaryScene != null)
                summaryScene.SetActive(true);

            ClearSummaryEntries();

            int count = _charactersToReveal != null ? _charactersToReveal.Count : 0;
            bool singleMode = count == 1;

            if (gridContainer != null)
            {
                // GridPanel (parent) : masquÃ© en mode x1 pour ne pas laisser un cadre vide.
                Transform panel = gridContainer.parent;
                if (panel != null && panel.name == "GridPanel")
                    panel.gameObject.SetActive(!singleMode);
                else
                    gridContainer.gameObject.SetActive(!singleMode);
            }
            if (singleContainer != null)
                singleContainer.gameObject.SetActive(singleMode);

            if (hintText != null)
            {
                hintText.gameObject.SetActive(true);
                hintText.text = singleMode
                    ? "Toucher pour ouvrir la fiche"
                    : "Touchez un personnage pour ouvrir sa fiche";
            }

            if (singleMode)
                PopulateSingleCard(_charactersToReveal[0]);
            else
            {
                PopulateGrid();
                FitSummaryGrid();
            }

            RefreshRepullButton();
        }

        /// <summary>
        /// Ajuste les cellules 5Ã—2 Ã  la largeur rÃ©elle (marges PadCard).
        /// </summary>
        private void FitSummaryGrid()
        {
            if (gridContainer == null)
                return;

            Canvas.ForceUpdateCanvases();

            GachaSummaryGridFitter fitter =
                gridContainer.GetComponent<GachaSummaryGridFitter>();
            if (fitter == null)
                fitter = gridContainer.gameObject.AddComponent<GachaSummaryGridFitter>();

            fitter.Fit();

            // Rect parfois 0 le frame d'activation â€” refit au frame suivant.
            RectTransform rt = gridContainer as RectTransform;
            if (rt != null && rt.rect.width < 8f)
                StartCoroutine(FitSummaryGridNextFrame());
        }

        private IEnumerator FitSummaryGridNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            GachaSummaryGridFitter fitter =
                gridContainer != null
                    ? gridContainer.GetComponent<GachaSummaryGridFitter>()
                    : null;
            fitter?.Fit();
        }

        private void PopulateGrid()
        {
            if (summaryEntryPrefab == null || gridContainer == null || _charactersToReveal == null)
                return;

            for (int i = 0; i < _charactersToReveal.Count; i++)
            {
                PulledCharacter pulled = _charactersToReveal[i];
                CharacterData data = characterDatabase?.GetById(pulled.characterId);
                if (data == null)
                    continue;

                PullResultEntryUI entry = RentFromPool(
                    _gridPool, summaryEntryPrefab, gridContainer);
                entry.Setup(data, pulled, OpenCharacterCard);
            }
        }

        private void PopulateSingleCard(PulledCharacter pulled)
        {
            if (singleCardPrefab == null || singleContainer == null || pulled == null)
                return;

            CharacterData data = characterDatabase?.GetById(pulled.characterId);
            if (data == null)
                return;

            PullResultEntryUI entry = RentFromPool(
                _singlePool, singleCardPrefab, singleContainer);
            entry.Setup(data, pulled, OpenCharacterCard);
        }

        private PullResultEntryUI RentFromPool(
            List<PullResultEntryUI> pool,
            PullResultEntryUI prefab,
            Transform parent)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                PullResultEntryUI existing = pool[i];
                if (existing == null)
                    continue;
                if (existing.gameObject.activeSelf)
                    continue;

                existing.transform.SetParent(parent, false);
                existing.gameObject.SetActive(true);
                return existing;
            }

            PullResultEntryUI created = Instantiate(prefab, parent);
            pool.Add(created);
            return created;
        }

        private void ClearSummaryEntries()
        {
            DeactivatePool(_gridPool);
            DeactivatePool(_singlePool);
        }

        private static void DeactivatePool(List<PullResultEntryUI> pool)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                PullResultEntryUI entry = pool[i];
                if (entry == null)
                    continue;

                entry.Cleanup();
                entry.gameObject.SetActive(false);
            }
        }

        private void OpenCharacterCard(PulledCharacter pulled)
        {
            if (pulled == null || characterDetailPopup == null)
                return;

            CharacterData data = characterDatabase?.GetById(pulled.characterId);
            if (data == null)
            {
                Debug.LogWarning(
                    "[Gacha] Fiche impossible â€” CharacterData introuvable : " + pulled.characterId);
                return;
            }

            OwnedCharacter owned = null;
            if (PersistentManager.Instance != null
                && PersistentManager.Instance.Characters != null)
            {
                owned = PersistentManager.Instance.Characters.GetOwnedCharacter(
                    pulled.characterId);
            }

            if (owned == null)
            {
                Debug.LogWarning(
                    "[Gacha] Fiche impossible â€” OwnedCharacter introuvable : " + pulled.characterId);
                return;
            }

            Transform popupTf = characterDetailPopup.transform;
            if (_detailPopupSiblingIndex < 0)
                _detailPopupSiblingIndex = popupTf.GetSiblingIndex();

            popupTf.SetAsLastSibling();
            characterDetailPopup.Open(data, owned);
        }

        private void RestoreDetailPopupSibling()
        {
            if (_detailPopupSiblingIndex < 0 || characterDetailPopup == null)
                return;

            characterDetailPopup.transform.SetSiblingIndex(_detailPopupSiblingIndex);
            _detailPopupSiblingIndex = -1;
        }

        private void RefreshRepullButton()
        {
            if (repullButton == null)
                return;

            if (_currentBanner == null)
            {
                repullButton.gameObject.SetActive(false);
                return;
            }

            repullButton.gameObject.SetActive(true);

            int cost = _wasMulti ? _currentBanner.CostMulti : _currentBanner.CostSingle;
            string countLabel = _wasMulti ? "Ã—10" : "Ã—1";

            if (repullLabelText != null)
                repullLabelText.text = "Invoquer Ã  nouveau " + countLabel;

            if (repullCostText != null)
                repullCostText.text = cost.ToString() + " Tals";

            bool canPay = PersistentManager.Instance != null
                && PersistentManager.Instance.Gacha != null
                && PersistentManager.Instance.Gacha.CanPull(_currentBanner, _wasMulti);

            repullButton.interactable = canPay;
        }

        private void ResetForNewRun()
        {
            UnsubscribeSummaryPageWatch();
            ClearSummaryEntries();
            RestoreDetailPopupSibling();
            ApplySummaryBackdropClearance(false);

            if (gridContainer != null)
            {
                Transform panel = gridContainer.parent;
                if (panel != null && panel.name == "GridPanel")
                    panel.gameObject.SetActive(false);
                else
                    gridContainer.gameObject.SetActive(false);
            }
            if (singleContainer != null)
                singleContainer.gameObject.SetActive(false);

            HideAllScenes();
            _isAnimating = false;
            // Exclusive mode + duck conservÃ©s pour la sÃ©quence suivante.
        }

        private void SubscribeSummaryPageWatch()
        {
            if (_watchingSummaryPageChanges || hubManager == null)
                return;

            hubManager.OnPageChanged += HandlePageChangedDuringSummary;
            _watchingSummaryPageChanges = true;
        }

        private void UnsubscribeSummaryPageWatch()
        {
            if (!_watchingSummaryPageChanges || hubManager == null)
                return;

            hubManager.OnPageChanged -= HandlePageChangedDuringSummary;
            _watchingSummaryPageChanges = false;
        }

        private void HandlePageChangedDuringSummary(int _)
        {
            CompleteAnimation(restoreHubPage: false);
        }

        private void ApplySummaryBackdropClearance(bool clearNav)
        {
            if (stageBackdrop == null)
                return;

            RectTransform rt = stageBackdrop.rectTransform;
            if (clearNav)
            {
                _stageBackdropOffsetMin = rt.offsetMin;
                rt.offsetMin = new Vector2(rt.offsetMin.x, NAV_CLEARANCE);
            }
            else
            {
                rt.offsetMin = new Vector2(rt.offsetMin.x, 0f);
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // MÃ‰THODES PRIVÃ‰ES â€” STAGE / AUDIO
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private void HideAllScenes()
        {
            if (trainSequence != null)
                trainSequence.HideSequenceScenes();
            if (crankScene != null) crankScene.SetActive(false);
            if (revealScene != null) revealScene.SetActive(false);
            if (summaryScene != null) summaryScene.SetActive(false);
        }

        /// <summary>
        /// Mode exclusif : backdrop + pages Hub + chrome + debug.
        /// Restaure l'Ã©tat actif prÃ©cÃ©dent des pages (sauf si restoreHubPages=false).
        /// </summary>
        private void SetExclusiveMode(bool exclusive, bool restoreHubPages = true)
        {
            if (stageBackdrop != null)
                stageBackdrop.gameObject.SetActive(exclusive);

            if (hubChrome != null)
                hubChrome.SetActive(!exclusive);

            if (exclusive)
            {
                ResolveDebugPreviewRoot();
                if (debugPreviewRoot != null)
                {
                    _debugWasActive = debugPreviewRoot.activeSelf;
                    debugPreviewRoot.SetActive(false);
                }

                // Bouton runtime BtnPreviewEveil (souvent hors debugPreviewRoot).
                HidePreviewEveilButtons(true);

                if (hubPagesToHide != null)
                {
                    if (_hubPageWasActive == null
                        || _hubPageWasActive.Length != hubPagesToHide.Length)
                    {
                        _hubPageWasActive = new bool[hubPagesToHide.Length];
                    }

                    for (int i = 0; i < hubPagesToHide.Length; i++)
                    {
                        if (hubPagesToHide[i] == null)
                            continue;
                        _hubPageWasActive[i] = hubPagesToHide[i].activeSelf;
                        hubPagesToHide[i].SetActive(false);
                    }
                }

                // invocationPageBackground peut Ãªtre PageInvocation dÃ©jÃ  dans la liste.
                if (invocationPageBackground != null)
                    invocationPageBackground.SetActive(false);
            }
            else
            {
                if (restoreHubPages
                    && hubPagesToHide != null
                    && _hubPageWasActive != null)
                {
                    int n = Mathf.Min(hubPagesToHide.Length, _hubPageWasActive.Length);
                    for (int i = 0; i < n; i++)
                    {
                        if (hubPagesToHide[i] != null)
                            hubPagesToHide[i].SetActive(_hubPageWasActive[i]);
                    }
                }

                if (debugPreviewRoot != null)
                    debugPreviewRoot.SetActive(_debugWasActive);

                HidePreviewEveilButtons(false);
            }
        }

        /// <summary>
        /// Masque le bouton debug Â« Preview Ã©veil Â» crÃ©Ã© sous le Canvas (hors SafeArea).
        /// </summary>
        private void HidePreviewEveilButtons(bool hide)
        {
            Transform canvas = transform.parent;
            if (canvas == null)
                return;

            Transform btn = canvas.Find("BtnPreviewEveil");
            if (btn != null)
                btn.gameObject.SetActive(!hide);

            // Au cas oÃ¹ plusieurs instances runtime existent.
            Transform[] all = canvas.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].name != "BtnPreviewEveil")
                    continue;
                all[i].gameObject.SetActive(!hide);
            }
        }

        private void ResolveDebugPreviewRoot()
        {
            if (debugPreviewRoot != null)
                return;

            Transform canvas = transform.parent;
            if (canvas == null)
                return;

            Transform dbg = canvas.Find("AwakeningCeremonyDebugPreview");
            if (dbg == null)
                dbg = canvas.Find("BtnPreviewEveil");
            if (dbg != null)
                debugPreviewRoot = dbg.gameObject;
        }

        private void EnsurePremiumStage()
        {
            AutoFindHubPagesIfNeeded();
            EnsureStageBackdrop();
            LayoutRevealArtwork();
        }

        private void AutoFindHubPagesIfNeeded()
        {
            if (hubPagesToHide != null && hubPagesToHide.Length > 0)
                return;

            // Bug préexistant post-1.2 : canvas.Find("PageXxx") est mort
            // (pages sous PageContainer). Même remède que TrainSequenceController.
            HubManager hub = FindObjectOfType<HubManager>();
            if (hub != null && hub.AllPages != null && hub.AllPages.Length > 0)
            {
                hubPagesToHide = hub.AllPages;
                return;
            }

            Transform canvas = transform.parent;
            if (canvas == null)
                return;

            string[] names =
            {
                "PageAccueil", "PageEquipe", "PageMissions", "PageInvocation"
            };
            List<GameObject> found = new List<GameObject>(4);
            for (int i = 0; i < names.Length; i++)
            {
                Transform t = canvas.Find(names[i]);
                if (t != null)
                    found.Add(t.gameObject);
            }

            hubPagesToHide = found.ToArray();

            if (debugPreviewRoot == null)
            {
                Transform dbg = canvas.Find("AwakeningCeremonyDebugPreview");
                if (dbg == null)
                    dbg = canvas.Find("BtnPreviewEveil");
                if (dbg != null)
                    debugPreviewRoot = dbg.gameObject;

                // Bouton crÃ©Ã© runtime : chercher par nom dans les enfants
                if (debugPreviewRoot == null)
                {
                    Transform[] all = canvas.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i].name.Contains("Preview") && all[i].name.Contains("veil"))
                        {
                            debugPreviewRoot = all[i].gameObject;
                            break;
                        }
                    }
                }
            }
        }

        private void EnsureStageBackdrop()
        {
            if (stageBackdrop != null)
            {
                stageBackdrop.color = UiTheme.GachaStageCharcoal;
                // Visuel only — un raycast plein écran gèle le levier si CrankHandle
                // a perdu son raycastTarget (ex. purge Hub trop agressive).
                stageBackdrop.raycastTarget = false;
                stageBackdrop.transform.SetAsFirstSibling();
                return;
            }

            Transform existing = transform.Find("StageBackdrop");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(
                    "StageBackdrop",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                go.transform.SetParent(transform, false);
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();

            stageBackdrop = go.GetComponent<Image>();
            if (stageBackdrop.sprite == null && _runtimeSmokeSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                _runtimeSmokeSprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            if (stageBackdrop.sprite == null)
                stageBackdrop.sprite = _runtimeSmokeSprite;

            stageBackdrop.color = UiTheme.GachaStageCharcoal;
            stageBackdrop.raycastTarget = false;
        }

        /// <summary>
        /// Garantit que le levier reçoit les drags (raycast + sibling au-dessus).
        /// </summary>
        private void EnsureCrankInputReceivable()
        {
            if (crankController != null)
                crankController.EnsureRaycastReceivable();

            if (crankScene != null)
                crankScene.transform.SetAsLastSibling();

            // Au-dessus des pages / overlays Hub pendant la cérémonie.
            transform.SetAsLastSibling();
        }

        private void LayoutRevealArtwork()
        {
            if (artworkRawImage == null)
                return;

            // Artwork plein Ã©cran ; le bandeau scrim flotte par-dessus en bas.
            // DÃ©sactive tout AspectRatioFitter qui aurait rÃ©duit le cadre (SR Fit).
            AspectRatioFitter arf = artworkRawImage.GetComponent<AspectRatioFitter>();
            if (arf != null)
                arf.enabled = false;

            RectTransform rt = artworkRawImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void EnsureSfxManagerExists()
        {
            if (SfxManager.Instance != null)
                return;

            SfxManager existing = FindObjectOfType<SfxManager>(true);
            if (existing != null)
            {
                // Awake (Instance) ne tourne pas tant que l'objet est inactif.
                if (!existing.gameObject.activeSelf)
                    existing.gameObject.SetActive(true);
                return;
            }

            GameObject go = new GameObject("SfxManager");
            DontDestroyOnLoad(go);
            go.AddComponent<SfxManager>();
        }

        /// <summary>
        /// Lecture SFX fiable (crÃ©e le manager si besoin + log si clip manquant).
        /// </summary>
        public static void PlayGachaSfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
                return;

            EnsureSfxManagerExists();
            if (SfxManager.Instance == null)
            {
                Debug.LogWarning("[Gacha] SfxManager introuvable â€” SFX ignorÃ© : " + clip.name);
                return;
            }

            if (UnityEngine.Object.FindObjectOfType<AudioListener>() == null)
            {
                Debug.LogWarning(
                    "[Gacha] Aucun AudioListener actif â€” les SFX sont inaudibles.");
            }

            SfxManager.Instance.PlaySfx(clip, volumeScale);
        }

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
                if (_musicWasPlayingBeforeCeremony)
                    AudioManager.Instance.PauseMusic();
            }
            else
            {
                if (!_ceremonyAudioActive)
                {
                    AudioManager.Instance.SetMusicDuck(1f);
                    return;
                }

                _ceremonyAudioActive = false;
                AudioManager.Instance.SetMusicDuck(1f);
                if (_musicWasPlayingBeforeCeremony)
                {
                    AudioManager.Instance.ResumeMusic();
                    _musicWasPlayingBeforeCeremony = false;
                }

                AudioManager.Instance.PlayAmbiance();
            }
        }

        private void ApplyMusicDuck(bool duck)
        {
            // Legacy — redirigé vers silence cérémonie.
            ApplyCeremonyAudio(duck);
        }

        private void CompleteAnimation(bool restoreHubPage = true)
        {
            _isAnimating = false;

            UnsubscribeSummaryPageWatch();
            RestoreDetailPopupSibling();
            ApplySummaryBackdropClearance(false);

            revealDirector?.ResetVisuals();
            StopInfo();

            // Nettoyer les entrÃ©es du rÃ©cap (pooling â€” dÃ©sactive, ne dÃ©truit pas)
            ClearSummaryEntries();

            if (trainSequence != null)
            {
                trainSequence.ReleaseDoorSheet();
                trainSequence.HideSequenceScenes();
            }

            HideAllScenes();

            // Restaurer la vitesse du parallax
            if (parallaxManager != null)
                parallaxManager.SetSpeedMultiplier(1f);

            SetExclusiveMode(false, restoreHubPages: restoreHubPage);
            ApplyCeremonyAudio(false);
            gameObject.SetActive(false);
            OnAnimationComplete?.Invoke();
        }
    }
}
