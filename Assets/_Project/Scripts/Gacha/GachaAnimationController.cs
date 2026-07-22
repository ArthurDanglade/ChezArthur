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
        [SerializeField] private float revealArtworkHeightRatio = 0.82f;

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
        [SerializeField] private Transform summaryContainer; // Contient la grille des personnages
        [SerializeField] private PullResultEntryUI summaryEntryPrefab; // Prefab pour chaque perso
        [SerializeField] private Button closeButton;

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
        private List<PullResultEntryUI> _spawnedSummaryEntries = new List<PullResultEntryUI>();
        private Material _runtimePixelateMat;
        private Sprite _runtimeSmokeSprite;
        private bool _stagePrepared;
        private bool[] _hubPageWasActive;
        private bool _debugWasActive = true;
        private const float INTER_REVEAL_SMOKE = 0.85f;

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
        }

        private void OnDestroy()
        {
            // Désabonnement obligatoire — évite les leaks d'event si le GO est détruit.
            if (crankController != null)
                crankController.OnCrankComplete -= OnCrankComplete;

            if (tapArea != null)
                tapArea.onClick.RemoveListener(OnTapToContinue);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);

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
        /// Lance l'animation de gacha avec les personnages à révéler.
        /// </summary>
        /// <returns>false si une garde a refusé le démarrage (fallback ShowResultDirect).</returns>
        public bool StartAnimation(GachaPullResult result)
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
        /// Repli : affiche directement le récapitulatif (sans crank / porte / reveals).
        /// Garantit que le joueur voit toujours ce qu'il a payé.
        /// </summary>
        public void ShowResultDirect(GachaPullResult result)
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

            FinishPixelResolve();

            if (trainSequence != null)
            {
                trainSequence.ReleaseDoorSheet();
                trainSequence.HideSequenceScenes();
            }

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

            if (trainSequence != null)
            {
                yield return trainSequence.PlaySequence(
                    bestRarity,
                    PrepareRevealAfterSmoke);
            }
            else
            {
                Debug.LogError(
                    "[Gacha] TrainSequenceController absent — passage direct au reveal.",
                    this);
                PrepareRevealAfterSmoke();
            }

            yield return RevealSequence();
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
            // Récupérer les données du personnage
            CharacterData data = characterDatabase?.GetById(pulled.characterId);

            // Textes / statut / tap : masqués pendant la résolution pixel.
            ClearRevealOverlayTexts();
            if (tapToContinueText != null)
                tapToContinueText.SetActive(false);
            if (ssrEffects != null)
                ssrEffects.SetActive(false);

            // Afficher l'artwork (pipeline portraits unifié : SSR animé / SR Resources / fallback icône).
            if (artworkView != null && data != null)
            {
                // Doublon : respecter l'état d'éveil persisté. Nouveau : toujours déchu (legacy Show).
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
            }

            if (artworkRawImage != null)
                artworkRawImage.color = Color.white;

            // Résolution pixel (tap = skip résolution uniquement).
            yield return PlayPixelResolve();

            // Textes / statut APRÈS la résolution.
            if (characterNameText != null && data != null)
                characterNameText.text = data.CharacterName;

            if (characterRarityText != null && data != null)
            {
                characterRarityText.text = data.Rarity.ToString();
                characterRarityText.color = CharacterRarityPalette.GetColor(data.Rarity);
            }

            if (statusText != null)
            {
                if (pulled.isNew)
                {
                    statusText.text = "NOUVEAU !";
                    statusText.color = Color.green;
                }
                else
                {
                    statusText.text = $"Nv.{pulled.previousLevel} → Nv.{pulled.newLevel}";
                    statusText.color = Color.yellow;
                }
            }

            if (ssrEffects != null)
                ssrEffects.SetActive(pulled.rarity == CharacterRarity.SSR || pulled.rarity == CharacterRarity.LR);

            // Tap "perso suivant" — armé seulement après l'étape d.
            if (tapToContinueText != null)
                tapToContinueText.SetActive(true);

            _waitingForTap = true;
            while (_waitingForTap)
            {
                yield return null;
            }

            if (tapToContinueText != null)
                tapToContinueText.SetActive(false);
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
            if (clip == null)
                return;
            if (SfxManager.Instance != null)
                SfxManager.Instance.PlaySfx(clip);
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
                artworkRawImage.enabled = true;
        }

        private void ShowSummary()
        {
            // Récap : plus d'artwork plein écran — libérer la texture reveal.
            if (artworkView != null)
                artworkView.Release();

            // Cacher la révélation, afficher le récap
            if (revealScene != null)
                revealScene.SetActive(false);
            if (summaryScene != null)
                summaryScene.SetActive(true);

            // Nettoyer les anciennes entrées
            ClearSummaryEntries();

            // Créer une entrée pour chaque personnage
            foreach (var pulled in _charactersToReveal)
            {
                if (summaryEntryPrefab == null || summaryContainer == null) continue;

                CharacterData data = characterDatabase?.GetById(pulled.characterId);
                if (data == null) continue;

                PullResultEntryUI entry = Instantiate(summaryEntryPrefab, summaryContainer);
                entry.Setup(data, pulled);
                _spawnedSummaryEntries.Add(entry);
            }
        }

        private void ClearSummaryEntries()
        {
            foreach (var entry in _spawnedSummaryEntries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }
            _spawnedSummaryEntries.Clear();
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
        /// Restaure l'état actif précédent des pages (pas un SetActive(true) aveugle).
        /// </summary>
        private void SetExclusiveMode(bool exclusive)
        {
            if (stageBackdrop != null)
                stageBackdrop.gameObject.SetActive(exclusive);

            if (hubChrome != null)
                hubChrome.SetActive(!exclusive);

            if (exclusive)
            {
                if (debugPreviewRoot != null)
                {
                    _debugWasActive = debugPreviewRoot.activeSelf;
                    debugPreviewRoot.SetActive(false);
                }

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
                if (hubPagesToHide != null && _hubPageWasActive != null)
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
            }
        }

        private void EnsurePremiumStage()
        {
            if (_stagePrepared)
            {
                EnsureSmokeDrawable();
                return;
            }

            AutoFindHubPagesIfNeeded();
            EnsureStageBackdrop();
            LayoutRevealArtwork();
            EnsureSmokeDrawable();
            _stagePrepared = true;
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

            RectTransform rt = artworkRawImage.rectTransform;
            float h = 1920f * Mathf.Clamp(revealArtworkHeightRatio, 0.5f, 0.95f);
            float w = h * (228f / 342f);
            rt.anchorMin = new Vector2(0.5f, 0.52f);
            rt.anchorMax = new Vector2(0.5f, 0.52f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
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
                return;

            GameObject go = new GameObject("SfxManager");
            go.AddComponent<SfxManager>();
        }

        private void ApplyMusicDuck(bool duck)
        {
            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.SetMusicDuck(duck ? musicDuckFactor : 1f);
        }

        private void CompleteAnimation()
        {
            _isAnimating = false;

            FinishPixelResolve();

            // Nettoyer les entrées du récap
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

            SetExclusiveMode(false);
            ApplyMusicDuck(false);
            gameObject.SetActive(false);
            OnAnimationComplete?.Invoke();
        }
    }
}
