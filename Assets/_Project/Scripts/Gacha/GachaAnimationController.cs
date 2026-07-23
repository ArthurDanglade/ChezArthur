using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.UI;
using ChezArthur.Core;
using ChezArthur.Hub;
using ChezArthur.Hub.Pages;
using ChezArthur.Hub.Pages.Invocation;
using ChezArthur.Audio;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Contrôle la séquence d'animation complète du gacha.
    /// </summary>
    public class GachaAnimationController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private static readonly int PixelStepsId = Shader.PropertyToID("_PixelSteps");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int UvRectId = Shader.PropertyToID("_UvRect");

        /// <summary> Espace bas du récap pour laisser la nav Hub cliquable. </summary>
        private const float NAV_CLEARANCE = 280f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Scènes")]
        [SerializeField] private GameObject crankScene;
        [SerializeField] private GameObject doorScene;
        [SerializeField] private GameObject revealScene;

        [Header("Séquence train (Gate 3)")]
        [SerializeField] private TrainSequenceController trainSequence;

        [Header("Manivelle")]
        [SerializeField] private LeverController crankController;

        [Header("Porte (OBSOLÈTE — Gate 3 train ; retrait au gate de nettoyage)")]
        [SerializeField] private RectTransform doorPanel;
        [SerializeField] private float doorOpenDuration = 3f;
        [SerializeField] private float doorSlideDistance = 400f; // Pixels vers la droite

        [Header("Artwork (pipeline portraits unifié)")]
        [SerializeField] private CharacterArtworkView artworkView;
        [SerializeField] private RawImage artworkRawImage;
        // artworkRawImage sert UNIQUEMENT aux manipulations de couleur/visibilité
        // que faisait l'ancien code sur l'Image ; l'affichage passe par la view.

        [Header("Révélation")]
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI characterRarityText;
        [SerializeField] private TextMeshProUGUI statusText; // "NOUVEAU !" ou "Nv.X → Nv.Y"
        [SerializeField] private GameObject ssrEffects; // Effets spéciaux pour SSR
        [SerializeField] private Image smokeTransition; // Image de fumée pour transition
        [SerializeField] private float revealDuration = 2f;
        [SerializeField] private float transitionDuration = 0.5f;

        [Header("Reveal — résolution pixel (Gate 4)")]
        [SerializeField] private Material revealPixelateMaterial;
        [SerializeField] private AudioClip revealClip;
        [SerializeField] private float revealResolveDuration = 2.2f;
        [SerializeField] private int[] pixelStepLevels = { 6, 12, 22, 40, 72, 140, 4096 };
        [SerializeField] private float revealArtworkHeightRatio = 0.95f;

        [Header("Reveal — bandeau statut")]
        [SerializeField] private GachaRevealStatusUI revealStatusUi;
        [Tooltip("Epidemic — laisser vide pour l’instant.")]
        [SerializeField] private AudioClip revealXpProgressClip;
        [SerializeField] private AudioClip revealLevelUpClip;
        [SerializeField] private AudioClip revealStatTickClip;
        [SerializeField] private AudioClip revealMaxConfirmClip;

        [Header("Parallax")]
        [SerializeField] private ParallaxManager parallaxManager;
        [SerializeField] private float parallaxSlowdownDuration = 2f;

        [Header("Tap to Continue")]
        [SerializeField] private GameObject tapToContinueText;
        [SerializeField] private Button tapArea; // Bouton invisible plein écran

        [Header("Éléments à cacher / stage")]
        [SerializeField] private GameObject invocationPageBackground;
        [Tooltip("InfoBar + NavigationBar (SafeArea).")]
        [SerializeField] private GameObject hubChrome;
        [Tooltip("Pages Hub à masquer (Accueil, Équipe…). Auto-rempli si vide.")]
        [SerializeField] private GameObject[] hubPagesToHide;
        [Tooltip("Bouton Preview éveil / debug hors SafeArea.")]
        [SerializeField] private GameObject debugPreviewRoot;
        [SerializeField] private Image stageBackdrop;
        [SerializeField] private float musicDuckFactor = 0.12f;

        [Header("Récapitulatif")]
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

        [Header("Références")]
        [SerializeField] private CharacterDatabase characterDatabase;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action OnAnimationComplete;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<PulledCharacter> _charactersToReveal;
        private int _currentRevealIndex;
        private Vector2 _doorClosedPosition;
        private bool _isAnimating = false;
        private bool _waitingForTap = false;
        private readonly List<PullResultEntryUI> _gridPool = new List<PullResultEntryUI>();
        private readonly List<PullResultEntryUI> _singlePool = new List<PullResultEntryUI>();
        private Material _runtimePixelateMat;
        private Sprite _runtimeSmokeSprite;
        private bool[] _hubPageWasActive;
        private bool _debugWasActive = true;
        private bool _firstRevealPreparedUnderVeil;
        private const float INTER_REVEAL_SMOKE = 0.85f;

        private GachaPullResult _currentResult;
        private BannerData _currentBanner;
        private bool _wasMulti;
        private bool _watchingSummaryPageChanges;
        private int _detailPopupSiblingIndex = -1;
        private Vector2 _stageBackdropOffsetMin;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            EnsureSfxManagerExists();

            // Sauvegarder la position fermée de la porte
            if (doorPanel != null)
                _doorClosedPosition = doorPanel.anchoredPosition;

            if (crankController == null)
            {
                Debug.LogError(
                    "[Gacha] crankController non câblé — la séquence " +
                    "restera bloquée après la manivelle",
                    this);
            }
            else
            {
                crankController.OnCrankComplete += OnCrankComplete;
            }

            // Cacher tout au départ
            HideAllScenes();

            // S'abonner au tap
            if (tapArea != null)
                tapArea.onClick.AddListener(OnTapToContinue);

            // S'abonner au bouton fermer
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);

            if (repullButton != null)
                repullButton.onClick.AddListener(OnRepullClicked);
        }

        private void OnDestroy()
        {
            UnsubscribeSummaryPageWatch();

            // Désabonnement obligatoire — évite les leaks d'event si le GO est détruit.
            if (crankController != null)
                crankController.OnCrankComplete -= OnCrankComplete;

            if (tapArea != null)
                tapArea.onClick.RemoveListener(OnTapToContinue);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);

            if (repullButton != null)
                repullButton.onClick.RemoveListener(OnRepullClicked);

            if (_runtimePixelateMat != null)
            {
                Destroy(_runtimePixelateMat);
                _runtimePixelateMat = null;
            }

            if (_runtimeSmokeSprite != null)
            {
                Destroy(_runtimeSmokeSprite);
                _runtimeSmokeSprite = null;
            }
        }

        private void OnDisable()
        {
            // Fermeture / interruption de l'écran gacha : libère le portrait chargé.
            if (artworkView != null)
                artworkView.Release();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Lance l'animation de gacha (surcharge legacy — sans bannière / re-pull).
        /// </summary>
        public bool StartAnimation(GachaPullResult result)
        {
            return StartAnimation(result, null, false);
        }

        /// <summary>
        /// Lance l'animation de gacha avec les personnages à révéler.
        /// </summary>
        /// <returns>false si une garde a refusé le démarrage (fallback ShowResultDirect).</returns>
        public bool StartAnimation(GachaPullResult result, BannerData banner, bool isMulti)
        {
            if (_isAnimating)
            {
                Debug.LogWarning(
                    "[Gacha] StartAnimation refusé — une animation est déjà en cours.",
                    this);
                return false;
            }

            if (result == null)
            {
                Debug.LogError("[Gacha] StartAnimation refusé — result null.", this);
                return false;
            }

            if (result.characters == null || result.characters.Count == 0)
            {
                Debug.LogError(
                    "[Gacha] StartAnimation refusé — aucun personnage dans le résultat.",
                    this);
                return false;
            }

            _isAnimating = true;
            _currentResult = result;
            _currentBanner = banner;
            _wasMulti = isMulti;
            _charactersToReveal = result.characters;
            _currentRevealIndex = 0;

            EnsurePremiumStage();
            EnsureSfxManagerExists();

            // Parent d'abord : Awake peut appeler HideAllScenes — les scènes sont
            // réactivées ensuite, sinon CrankScene est immédiatement re-masquée.
            gameObject.SetActive(true);
            SetExclusiveMode(true);
            ApplyMusicDuck(true);

            HideAllScenes();
            if (crankScene != null)
                crankScene.SetActive(true);
            return true;
        }

        /// <summary>
        /// Repli legacy : récap direct sans bannière.
        /// </summary>
        public void ShowResultDirect(GachaPullResult result)
        {
            ShowResultDirect(result, null, false);
        }

        /// <summary>
        /// Repli : affiche directement le récapitulatif (sans crank / porte / reveals).
        /// Garantit que le joueur voit toujours ce qu'il a payé.
        /// </summary>
        public void ShowResultDirect(GachaPullResult result, BannerData banner, bool isMulti)
        {
            if (result == null || result.characters == null || result.characters.Count == 0)
            {
                Debug.LogError(
                    "[Gacha] ShowResultDirect refusé — résultat null ou vide.",
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
            ApplyMusicDuck(true);

            HideAllScenes();
            ShowSummary();
        }

        /// <summary>
        /// Force l'arrêt de l'animation (si besoin).
        /// </summary>
        public void StopAnimation()
        {
            StopAllCoroutines();
            _waitingForTap = false;
            _isAnimating = false;

            UnsubscribeSummaryPageWatch();
            RestoreDetailPopupSibling();
            ApplySummaryBackdropClearance(false);

            FinishPixelResolve();

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
            ApplyMusicDuck(false);
            gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // CALLBACKS
        // ═══════════════════════════════════════════

        private void OnCrankComplete()
        {
            StartCoroutine(RunTrainThenReveal());
        }

        private void OnTapToContinue()
        {
            if (_waitingForTap)
            {
                _waitingForTap = false;
            }
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
                Debug.LogWarning("[Gacha] Re-pull impossible — Gacha null.");
                RefreshRepullButton();
                return;
            }

            GachaManager gacha = PersistentManager.Instance.Gacha;
            if (!gacha.CanPull(_currentBanner, _wasMulti))
            {
                Debug.LogWarning("[Gacha] Re-pull refusé — CanPull false.");
                RefreshRepullButton();
                return;
            }

            GachaPullResult result = _wasMulti
                ? gacha.PullMulti(_currentBanner)
                : gacha.PullSingle(_currentBanner);

            if (result == null)
            {
                Debug.LogWarning("[Gacha] Re-pull échoué — résultat null.");
                RefreshRepullButton();
                return;
            }

            BannerData banner = _currentBanner;
            bool isMulti = _wasMulti;
            ResetForNewRun();
            StartAnimation(result, banner, isMulti);
        }

        // ═══════════════════════════════════════════
        // COROUTINES — SÉQUENCE D'ANIMATION
        // ═══════════════════════════════════════════

        /// <summary>
        /// Après manivelle : séquence train → reveals.
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
                    "[Gacha] TrainSequenceController absent — passage direct au reveal.",
                    this);
                yield return PrepareFirstRevealUnderVeil();
            }

            yield return RevealSequence();
        }

        /// <summary>
        /// Sous le voile opaque : active Reveal + charge le 1er artwork (résolu).
        /// Le fondu du voile révèle ensuite le perso déjà prêt.
        /// </summary>
        private IEnumerator PrepareFirstRevealUnderVeil()
        {
            PrepareRevealAfterSmoke();

            if (_charactersToReveal == null || _charactersToReveal.Count == 0)
                yield break;

            PulledCharacter first = _charactersToReveal[0];
            CharacterData data = characterDatabase?.GetById(first.characterId);

            ClearRevealOverlayTexts();
            if (tapToContinueText != null)
                tapToContinueText.SetActive(false);
            if (ssrEffects != null)
                ssrEffects.SetActive(false);
            if (revealStatusUi != null)
                revealStatusUi.HideImmediate();

            if (artworkView != null && data != null)
            {
                if (!first.isNew
                    && PersistentManager.Instance != null
                    && PersistentManager.Instance.Characters != null)
                {
                    OwnedCharacter owned =
                        PersistentManager.Instance.Characters.GetOwnedCharacter(data.Id);
                    if (owned != null)
                        artworkView.Show(data, owned);
                    else
                        artworkView.Show(data);
                }
                else
                {
                    artworkView.Show(data);
                }

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

            // Résolution pixel sous le blanc — invisible, puis le fondu montre l'art net.
            yield return PlayPixelResolve();

            _firstRevealPreparedUnderVeil = true;
        }

        /// <summary>
        /// Appelé au pic de la fumée : Door/Train off, Reveal prêt.
        /// </summary>
        private void PrepareRevealAfterSmoke()
        {
            if (trainSequence != null)
                trainSequence.HideSequenceScenes();
            else if (doorScene != null)
                doorScene.SetActive(false);

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

        /// <summary>
        /// OBSOLÈTE — remplacé par RunTrainThenReveal (Gate 3).
        /// Conservé pour référence ; retrait au gate de nettoyage.
        /// </summary>
        private IEnumerator TransitionToDoor()
        {
            yield return new WaitForSeconds(0.5f);

            crankScene.SetActive(false);
            doorScene.SetActive(true);

            if (doorPanel != null)
                doorPanel.anchoredPosition = _doorClosedPosition;

            // Réinitialiser le parallax à vitesse normale
            if (parallaxManager != null)
                parallaxManager.SetSpeedMultiplier(1f);

            // Attendre un peu avec le parallax qui tourne normalement
            yield return new WaitForSeconds(1f);

            // Ralentir le parallax jusqu'à l'arrêt
            yield return StartCoroutine(SlowdownParallax());

            // Petit délai puis ouvrir la porte
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(OpenDoor());

            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(RevealSequence());
        }

        private IEnumerator SlowdownParallax()
        {
            if (parallaxManager == null) yield break;

            float elapsed = 0f;

            while (elapsed < parallaxSlowdownDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / parallaxSlowdownDuration;

                // Easing out : ralentir de 1 vers 0
                float multiplier = Mathf.Lerp(1f, 0f, t * t);
                parallaxManager.SetSpeedMultiplier(multiplier);

                yield return null;
            }

            parallaxManager.SetSpeedMultiplier(0f);
        }

        /// <summary>
        /// OBSOLÈTE — porte coulissante remplacée par flipbook (Gate 3).
        /// Conservé pour référence ; retrait au gate de nettoyage.
        /// </summary>
        private IEnumerator OpenDoor()
        {
            if (doorPanel == null) yield break;

            Vector2 startPos = _doorClosedPosition;
            Vector2 endPos = new Vector2(_doorClosedPosition.x + doorSlideDistance, _doorClosedPosition.y);
            float elapsed = 0f;

            while (elapsed < doorOpenDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / doorOpenDuration;

                // Easing out pour un effet plus naturel
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                doorPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);
                yield return null;
            }

            doorPanel.anchoredPosition = endPos;
        }

        private IEnumerator RevealSequence()
        {
            // S'assurer que reveal est actif (fumée a pu déjà l'activer)
            if (trainSequence != null)
                trainSequence.HideSequenceScenes();
            if (doorScene != null)
                doorScene.SetActive(false);
            if (revealScene != null)
                revealScene.SetActive(true);

            // Révéler chaque personnage
            for (int i = 0; i < _charactersToReveal.Count; i++)
            {
                _currentRevealIndex = i;

                if (i > 0)
                    yield return InterRevealSmokeCover();

                yield return StartCoroutine(RevealCharacter(_charactersToReveal[i]));
            }

            // Afficher le récapitulatif
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

            ClearRevealOverlayTexts();
            if (tapToContinueText != null)
                tapToContinueText.SetActive(false);
            if (ssrEffects != null)
                ssrEffects.SetActive(false);

            if (!artAlreadyReady)
            {
                if (artworkView != null && data != null)
                {
                    if (!pulled.isNew
                        && PersistentManager.Instance != null
                        && PersistentManager.Instance.Characters != null)
                    {
                        OwnedCharacter owned =
                            PersistentManager.Instance.Characters.GetOwnedCharacter(data.Id);
                        if (owned != null)
                            artworkView.Show(data, owned);
                        else
                            artworkView.Show(data);
                    }
                    else
                    {
                        artworkView.Show(data);
                    }

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

                yield return PlayPixelResolve();
            }

            // Bandeau premium (XP / stats / MAX).
            EnsureRevealStatusUi();
            if (revealStatusUi != null)
            {
                ClearRevealOverlayTexts();
                yield return revealStatusUi.PlayStatus(data, pulled);
            }
            else
            {
                if (characterNameText != null && data != null)
                    characterNameText.text = data.CharacterName;

                if (characterRarityText != null && data != null)
                {
                    characterRarityText.text = data.Rarity.ToString();
                    characterRarityText.color = CharacterRarityPalette.GetColor(data.Rarity);
                }

                if (statusText != null)
                {
                    statusText.text = pulled.FormatStatusText();
                    statusText.color = pulled.FormatStatusColor();
                }
            }

            if (ssrEffects != null)
                ssrEffects.SetActive(pulled.rarity == CharacterRarity.SSR || pulled.rarity == CharacterRarity.LR);

            if (tapToContinueText != null)
                tapToContinueText.SetActive(true);

            _waitingForTap = true;
            while (_waitingForTap)
            {
                yield return null;
            }

            if (tapToContinueText != null)
                tapToContinueText.SetActive(false);

            if (revealStatusUi != null)
                revealStatusUi.HideImmediate();
        }

        private void EnsureRevealStatusUi()
        {
            if (revealStatusUi != null)
            {
                HideLegacyRevealLabels();
                revealStatusUi.ConfigureAudio(
                    revealXpProgressClip,
                    revealLevelUpClip,
                    revealStatTickClip,
                    revealMaxConfirmClip);
                return;
            }

            if (revealScene == null)
                return;

            revealStatusUi = GachaRevealStatusUI.EnsureUnder(revealScene.transform);
            revealStatusUi.ConfigureAudio(
                revealXpProgressClip,
                revealLevelUpClip,
                revealStatTickClip,
                revealMaxConfirmClip);
            HideLegacyRevealLabels();
        }

        private void HideLegacyRevealLabels()
        {
            if (characterNameText != null)
                characterNameText.gameObject.SetActive(false);
            if (characterRarityText != null)
                characterRarityText.gameObject.SetActive(false);
            if (statusText != null)
                statusText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Résolution pixel : paliers francs + saturation 0→1. Tap = skip immédiat.
        /// </summary>
        private IEnumerator PlayPixelResolve()
        {
            if (artworkRawImage == null || revealPixelateMaterial == null)
                yield break;

            Material mat = EnsurePixelateInstance();
            if (mat == null)
                yield break;

            if (artworkView != null)
                artworkView.SetAnimationPaused(true);

            Rect uv = artworkRawImage.uvRect;
            mat.SetVector(UvRectId, new Vector4(uv.x, uv.y, uv.width, uv.height));
            artworkRawImage.material = mat;

            PlayRevealSfx(revealClip);

            int levelCount = pixelStepLevels != null ? pixelStepLevels.Length : 0;
            if (levelCount < 1)
            {
                FinishPixelResolve();
                yield break;
            }

            float duration = Mathf.Max(0.01f, revealResolveDuration);
            float stepDur = duration / levelCount;

            // Premier palier immédiat ; tap armé = skip résolution.
            mat.SetFloat(PixelStepsId, pixelStepLevels[0]);
            mat.SetFloat(SaturationId, 0f);
            _waitingForTap = true;

            float elapsed = 0f;
            int currentStep = 0;

            while (elapsed < duration)
            {
                if (!_waitingForTap)
                {
                    FinishPixelResolve();
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                mat.SetFloat(SaturationId, t);

                int stepIndex = Mathf.Min(
                    levelCount - 1,
                    Mathf.FloorToInt(elapsed / stepDur));
                if (stepIndex != currentStep)
                {
                    currentStep = stepIndex;
                    mat.SetFloat(PixelStepsId, pixelStepLevels[currentStep]);
                }

                yield return null;
            }

            FinishPixelResolve();
        }

        private void FinishPixelResolve()
        {
            if (_runtimePixelateMat != null
                && pixelStepLevels != null
                && pixelStepLevels.Length > 0)
            {
                _runtimePixelateMat.SetFloat(
                    PixelStepsId,
                    pixelStepLevels[pixelStepLevels.Length - 1]);
                _runtimePixelateMat.SetFloat(SaturationId, 1f);
            }

            if (artworkRawImage != null)
                artworkRawImage.material = null;

            if (artworkView != null)
                artworkView.SetAnimationPaused(false);

            _waitingForTap = false;
        }

        private Material EnsurePixelateInstance()
        {
            if (_runtimePixelateMat != null)
                return _runtimePixelateMat;

            if (revealPixelateMaterial == null)
                return null;

            _runtimePixelateMat = new Material(revealPixelateMaterial);
            return _runtimePixelateMat;
        }

        private void ClearRevealOverlayTexts()
        {
            if (characterNameText != null)
                characterNameText.text = string.Empty;
            if (characterRarityText != null)
                characterRarityText.text = string.Empty;
            if (statusText != null)
                statusText.text = string.Empty;
        }

        private static void PlayRevealSfx(AudioClip clip)
        {
            PlayGachaSfx(clip);
        }

        /// <summary>
        /// Couverture charbon entre deux reveals (pas de flash blanc).
        /// </summary>
        private IEnumerator InterRevealSmokeCover()
        {
            EnsureSmokeDrawable();
            if (smokeTransition == null)
                yield break;

            Color c = UiTheme.GachaStageCharcoal;
            c.a = 0f;
            smokeTransition.gameObject.SetActive(true);
            smokeTransition.color = c;
            smokeTransition.rectTransform.localScale = Vector3.one;
            smokeTransition.transform.SetAsLastSibling();

            float half = INTER_REVEAL_SMOKE * 0.5f;
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / half);
                float eased = u * u * (3f - 2f * u);
                c.a = Mathf.Lerp(0f, 1f, eased);
                smokeTransition.color = c;
                yield return null;
            }

            // Pic opaque : nettoyer l'overlay texte (le prochain RevealCharacter charge l'art).
            ClearRevealOverlayTexts();
            FinishPixelResolve();
            if (artworkRawImage != null)
                artworkRawImage.enabled = false;

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / half);
                float eased = u * u * (3f - 2f * u);
                c.a = Mathf.Lerp(1f, 0f, eased);
                smokeTransition.color = c;
                yield return null;
            }

            c.a = 0f;
            smokeTransition.color = c;
            smokeTransition.gameObject.SetActive(false);

            if (artworkRawImage != null)
            {
                artworkRawImage.enabled = true;
                LayoutRevealArtwork();
            }
        }

        private void ShowSummary()
        {
            // Récap : plus d'artwork plein écran — libérer la texture reveal.
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
                // GridPanel (parent) : masqué en mode x1 pour ne pas laisser un cadre vide.
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
        /// Ajuste les cellules 5×2 à la largeur réelle (marges PadCard).
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

            // Rect parfois 0 le frame d'activation — refit au frame suivant.
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
                    "[Gacha] Fiche impossible — CharacterData introuvable : " + pulled.characterId);
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
                    "[Gacha] Fiche impossible — OwnedCharacter introuvable : " + pulled.characterId);
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
            string countLabel = _wasMulti ? "×10" : "×1";

            if (repullLabelText != null)
                repullLabelText.text = "Invoquer à nouveau " + countLabel;

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
            // Exclusive mode + duck conservés pour la séquence suivante.
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

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — STAGE / AUDIO
        // ═══════════════════════════════════════════

        private void HideAllScenes()
        {
            if (trainSequence != null)
                trainSequence.HideSequenceScenes();
            if (crankScene != null) crankScene.SetActive(false);
            if (doorScene != null) doorScene.SetActive(false);
            if (revealScene != null) revealScene.SetActive(false);
            if (summaryScene != null) summaryScene.SetActive(false);
        }

        /// <summary>
        /// Mode exclusif : backdrop + pages Hub + chrome + debug.
        /// Restaure l'état actif précédent des pages (sauf si restoreHubPages=false).
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

                // invocationPageBackground peut être PageInvocation déjà dans la liste.
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
        /// Masque le bouton debug « Preview éveil » créé sous le Canvas (hors SafeArea).
        /// </summary>
        private void HidePreviewEveilButtons(bool hide)
        {
            Transform canvas = transform.parent;
            if (canvas == null)
                return;

            Transform btn = canvas.Find("BtnPreviewEveil");
            if (btn != null)
                btn.gameObject.SetActive(!hide);

            // Au cas où plusieurs instances runtime existent.
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
            EnsureSmokeDrawable();
        }

        private void AutoFindHubPagesIfNeeded()
        {
            if (hubPagesToHide != null && hubPagesToHide.Length > 0)
                return;

            Transform canvas = transform.parent;
            if (canvas == null)
                return;

            string[] names =
            {
                "PageAccueil", "PageEquipe", "PageMusique", "PageInvocation"
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

                // Bouton créé runtime : chercher par nom dans les enfants
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
                stageBackdrop.raycastTarget = true;
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
            stageBackdrop.raycastTarget = true;
        }

        private void LayoutRevealArtwork()
        {
            if (artworkRawImage == null)
                return;

            // Artwork plein écran ; le bandeau scrim flotte par-dessus en bas.
            // Désactive tout AspectRatioFitter qui aurait réduit le cadre (SR Fit).
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

            RectTransform rt = smokeTransition.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            smokeTransition.raycastTarget = false;
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
        /// Lecture SFX fiable (crée le manager si besoin + log si clip manquant).
        /// </summary>
        public static void PlayGachaSfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
                return;

            EnsureSfxManagerExists();
            if (SfxManager.Instance == null)
            {
                Debug.LogWarning("[Gacha] SfxManager introuvable — SFX ignoré : " + clip.name);
                return;
            }

            if (UnityEngine.Object.FindObjectOfType<AudioListener>() == null)
            {
                Debug.LogWarning(
                    "[Gacha] Aucun AudioListener actif — les SFX sont inaudibles.");
            }

            SfxManager.Instance.PlaySfx(clip, volumeScale);
        }

        private void ApplyMusicDuck(bool duck)
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetMusicDuck(duck ? musicDuckFactor : 1f);
        }

        private void CompleteAnimation(bool restoreHubPage = true)
        {
            _isAnimating = false;

            UnsubscribeSummaryPageWatch();
            RestoreDetailPopupSibling();
            ApplySummaryBackdropClearance(false);

            FinishPixelResolve();

            // Nettoyer les entrées du récap (pooling — désactive, ne détruit pas)
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
            ApplyMusicDuck(false);
            gameObject.SetActive(false);
            OnAnimationComplete?.Invoke();
        }
    }
}
