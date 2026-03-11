using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Hub;
using ChezArthur.Hub.Pages.Invocation;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Contrôle la séquence d'animation complète du gacha.
    /// </summary>
    public class GachaAnimationController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Scènes")]
        [SerializeField] private GameObject crankScene;
        [SerializeField] private GameObject doorScene;
        [SerializeField] private GameObject revealScene;

        [Header("Manivelle")]
        [SerializeField] private CrankController crankController;

        [Header("Porte")]
        [SerializeField] private RectTransform doorPanel;
        [SerializeField] private float doorOpenDuration = 3f;
        [SerializeField] private float doorSlideDistance = 400f; // Pixels vers la droite

        [Header("Révélation")]
        [SerializeField] private Image characterArtwork;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI characterRarityText;
        [SerializeField] private TextMeshProUGUI statusText; // "NOUVEAU !" ou "Nv.X → Nv.Y"
        [SerializeField] private GameObject ssrEffects; // Effets spéciaux pour SSR
        [SerializeField] private Image smokeTransition; // Image de fumée pour transition
        [SerializeField] private float revealDuration = 2f;
        [SerializeField] private float transitionDuration = 0.5f;

        [Header("Parallax")]
        [SerializeField] private ParallaxManager parallaxManager;
        [SerializeField] private float parallaxSlowdownDuration = 2f;

        [Header("Tap to Continue")]
        [SerializeField] private GameObject tapToContinueText;
        [SerializeField] private Button tapArea; // Bouton invisible plein écran

        [Header("Éléments à cacher")]
        [SerializeField] private GameObject invocationPageBackground;

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

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            // Sauvegarder la position fermée de la porte
            if (doorPanel != null)
                _doorClosedPosition = doorPanel.anchoredPosition;

            // S'abonner à la manivelle
            if (crankController != null)
                crankController.OnCrankComplete += OnCrankComplete;

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
            if (crankController != null)
                crankController.OnCrankComplete -= OnCrankComplete;

            if (tapArea != null)
                tapArea.onClick.RemoveListener(OnTapToContinue);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Lance l'animation de gacha avec les personnages à révéler.
        /// </summary>
        public void StartAnimation(GachaPullResult result)
        {
            if (_isAnimating || result == null || result.characters.Count == 0) return;

            _isAnimating = true;
            _charactersToReveal = result.characters;
            _currentRevealIndex = 0;

            // Cacher le fond de l'invocation
            if (invocationPageBackground != null)
                invocationPageBackground.SetActive(false);

            // Afficher la scène de la manivelle
            HideAllScenes();
            crankScene.SetActive(true);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Force l'arrêt de l'animation (si besoin).
        /// </summary>
        public void StopAnimation()
        {
            StopAllCoroutines();
            _waitingForTap = false;
            _isAnimating = false;
            HideAllScenes();

            // Restaurer la vitesse du parallax
            if (parallaxManager != null)
                parallaxManager.SetSpeedMultiplier(1f);

            if (invocationPageBackground != null)
                invocationPageBackground.SetActive(true);
            gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // CALLBACKS
        // ═══════════════════════════════════════════

        private void OnCrankComplete()
        {
            // La manivelle est terminée, passer à la porte
            StartCoroutine(TransitionToDoor());
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
            // Cacher la porte, afficher la révélation
            doorScene.SetActive(false);
            revealScene.SetActive(true);

            // Révéler chaque personnage
            for (int i = 0; i < _charactersToReveal.Count; i++)
            {
                _currentRevealIndex = i;
                yield return StartCoroutine(RevealCharacter(_charactersToReveal[i]));

                // Si ce n'est pas le dernier, faire une transition
                if (i < _charactersToReveal.Count - 1)
                {
                    yield return StartCoroutine(SmokeTransition());
                }
            }

            // Afficher le récapitulatif
            yield return new WaitForSeconds(0.3f);
            ShowSummary();
        }

        private IEnumerator RevealCharacter(PulledCharacter pulled)
        {
            // Récupérer les données du personnage
            CharacterData data = characterDatabase?.GetById(pulled.characterId);

            // Afficher l'artwork
            if (characterArtwork != null && data != null)
            {
                // Utiliser le portrait si disponible, sinon l'icône
                characterArtwork.sprite = data.Portrait != null ? data.Portrait : data.Icon;
                characterArtwork.color = Color.white;
            }

            // Afficher le nom
            if (characterNameText != null && data != null)
                characterNameText.text = data.CharacterName;

            // Afficher la rareté
            if (characterRarityText != null && data != null)
            {
                characterRarityText.text = data.Rarity.ToString();
                characterRarityText.color = GetRarityColor(data.Rarity);
            }

            // Afficher le statut (nouveau ou level up)
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

            // Effets SSR
            if (ssrEffects != null)
                ssrEffects.SetActive(pulled.rarity == CharacterRarity.SSR || pulled.rarity == CharacterRarity.LR);

            // Afficher "Tap pour continuer"
            if (tapToContinueText != null)
                tapToContinueText.SetActive(true);

            // Attendre le tap
            _waitingForTap = true;
            while (_waitingForTap)
            {
                yield return null;
            }

            // Cacher "Tap pour continuer"
            if (tapToContinueText != null)
                tapToContinueText.SetActive(false);
        }

        private IEnumerator SmokeTransition()
        {
            if (smokeTransition == null) yield break;

            // Fade in de la fumée
            smokeTransition.gameObject.SetActive(true);
            float elapsed = 0f;
            float halfDuration = transitionDuration / 2f;

            // Fade in
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = elapsed / halfDuration;
                smokeTransition.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / halfDuration);
                smokeTransition.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            smokeTransition.gameObject.SetActive(false);
        }

        private void ShowSummary()
        {
            // Cacher la révélation, afficher le récap
            revealScene.SetActive(false);
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
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void HideAllScenes()
        {
            if (crankScene != null) crankScene.SetActive(false);
            if (doorScene != null) doorScene.SetActive(false);
            if (revealScene != null) revealScene.SetActive(false);
            if (summaryScene != null) summaryScene.SetActive(false);
        }

        private void CompleteAnimation()
        {
            _isAnimating = false;

            // Nettoyer les entrées du récap
            ClearSummaryEntries();

            HideAllScenes();

            // Restaurer la vitesse du parallax
            if (parallaxManager != null)
                parallaxManager.SetSpeedMultiplier(1f);

            // Réafficher le fond de l'invocation
            if (invocationPageBackground != null)
                invocationPageBackground.SetActive(true);

            gameObject.SetActive(false);
            OnAnimationComplete?.Invoke();
        }

        private Color GetRarityColor(CharacterRarity rarity)
        {
            return rarity switch
            {
                CharacterRarity.SR => new Color(0.6f, 0.8f, 1f),   // Bleu clair
                CharacterRarity.SSR => new Color(1f, 0.84f, 0f),  // Or
                CharacterRarity.LR => new Color(0.8f, 0.5f, 1f),  // Violet
                _ => Color.white
            };
        }
    }
}
