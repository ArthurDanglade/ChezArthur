using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Characters;
using System.Collections.Generic;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Popup affichant les détails d'un personnage (artwork, stats, passifs).
    /// </summary>
    public class CharacterDetailPopup : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Header (sur artwork)")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI typeText; // "SR • Attacker"

        [Header("Artwork")]
        [SerializeField] private Image artworkImage;

        [Header("Encadré Stats/Passifs")]
        [SerializeField] private RectTransform statsPanel;
        [SerializeField] private float panelClosedHeight = 200f;
        [SerializeField] private float panelOpenHeight = 400f;
        [SerializeField] private float animationDuration = 0.3f;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI atkText;
        [SerializeField] private TextMeshProUGUI defText;
        [SerializeField] private TextMeshProUGUI speedText;

        [Header("Passifs")]
        [SerializeField] private Transform passivesContainer;
        [SerializeField] private PassiveEntryUI passiveEntryPrefab;

        [Header("Bouton Dépliant")]
        [SerializeField] private Button expandButton;
        [SerializeField] private Image expandArrowIcon;

        [Header("Footer")]
        [SerializeField] private Button addToTeamButton;
        [SerializeField] private TextMeshProUGUI addToTeamButtonText;
        [SerializeField] private Button closeButton;

        [Header("Composants")]
        [SerializeField] private CanvasGroup canvasGroup;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private string _currentCharacterId;
        private CharacterData _currentData;
        private OwnedCharacter _currentOwned;
        private bool _isExpanded = false;
        private Coroutine _animationCoroutine;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (expandButton != null)
                expandButton.onClick.AddListener(ToggleExpand);

            if (addToTeamButton != null)
                addToTeamButton.onClick.AddListener(OnAddToTeamClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            // Masquer au démarrage via CanvasGroup (pas SetActive)
            HidePopup();
        }

        private void OnDestroy()
        {
            if (expandButton != null)
                expandButton.onClick.RemoveListener(ToggleExpand);

            if (addToTeamButton != null)
                addToTeamButton.onClick.RemoveListener(OnAddToTeamClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Ouvre le popup avec les données du personnage.
        /// </summary>
        public void Open(CharacterData data, OwnedCharacter owned)
        {
            Debug.Log($"[CharacterDetailPopup] Open appelé - data null? {data == null}, owned null? {owned == null}");

            if (data == null || owned == null)
            {
                Debug.LogWarning("[CharacterDetailPopup] Open annulé - data ou owned est null");
                return;
            }

            _currentCharacterId = owned.characterId;
            _currentData = data;
            _currentOwned = owned;

            Debug.Log($"[CharacterDetailPopup] Données assignées pour {data.CharacterName}");

            // Reset l'état
            _isExpanded = false;
            if (statsPanel != null)
            {
                statsPanel.sizeDelta = new Vector2(statsPanel.sizeDelta.x, panelClosedHeight);
            }
            UpdateExpandArrow();

            // Remplir les données
            RefreshDisplay();

            Debug.Log($"[CharacterDetailPopup] Avant ShowPopup(), gameObject.activeSelf = {gameObject.activeSelf}");
            ShowPopup();
            Debug.Log($"[CharacterDetailPopup] Après ShowPopup(), gameObject.activeSelf = {gameObject.activeSelf}");
        }

        /// <summary>
        /// Ferme le popup.
        /// </summary>
        public void Close()
        {
            HidePopup();
            _currentCharacterId = null;
            _currentData = null;
            _currentOwned = null;
        }

        /// <summary>
        /// Affiche le popup via le CanvasGroup (GameObject reste actif).
        /// </summary>
        private void ShowPopup()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Masque le popup via le CanvasGroup (GameObject reste actif).
        /// </summary>
        private void HidePopup()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — AFFICHAGE
        // ═══════════════════════════════════════════

        private void RefreshDisplay()
        {
            if (_currentData == null || _currentOwned == null) return;

            // Header
            if (nameText != null)
                nameText.text = _currentData.CharacterName;

            if (levelText != null)
                levelText.text = "Nv. " + _currentOwned.level.ToString();

            if (typeText != null)
            {
                SpecializationData activeSpec = _currentData.GetSpecialization(_currentOwned.GetSpecialization());
                if (activeSpec != null)
                    typeText.text = _currentData.Rarity.ToString() + " • " + activeSpec.Role.ToString();
                else
                    typeText.text = _currentData.Rarity.ToString() + " • " + _currentData.Role.ToString();
            }

            // Artwork
            if (artworkImage != null && _currentData.Portrait != null)
            {
                artworkImage.sprite = _currentData.Portrait;
            }
            else if (artworkImage != null && _currentData.Icon != null)
            {
                // Fallback sur l'icône si pas de portrait
                artworkImage.sprite = _currentData.Icon;
            }

            // Stats (au niveau actuel)
            int level = _currentOwned.level;
            if (hpText != null)
                hpText.text = _currentData.GetHpAtLevel(level).ToString();
            if (atkText != null)
                atkText.text = _currentData.GetAtkAtLevel(level).ToString();
            if (defText != null)
                defText.text = _currentData.GetDefAtLevel(level).ToString();
            if (speedText != null)
                speedText.text = _currentData.GetSpeedAtLevel(level).ToString();

            // Passifs
            RefreshPassives();

            // Bouton équipe
            UpdateTeamButton();
        }

        private void RefreshPassives()
        {
            if (passivesContainer == null || passiveEntryPrefab == null) return;

            foreach (Transform child in passivesContainer)
            {
                Destroy(child.gameObject);
            }

            if (_currentData == null || _currentOwned == null) return;

            int specIndex = _currentOwned.GetSpecialization();
            SpecializationData activeSpec = _currentData.GetSpecialization(specIndex);
            if (activeSpec == null) return;

            int level = _currentOwned.level;
            IReadOnlyList<PassiveSlot> slots = activeSpec.GetPassiveSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                PassiveSlot slot = slots[i];
                if (slot == null || slot.PassiveData == null) continue;

                bool unlocked = level >= slot.UnlockLevel;
                string levelLabel = "Nv. " + slot.UnlockLevel.ToString();
                CreatePassiveEntry(slot.PassiveData, levelLabel, unlocked);
            }

            var availableSpecs = _currentData.GetAvailableSpecializations(level);
            if (availableSpecs.Count > 1)
            {
                // Indicateur : plusieurs spés disponibles (affichage optionnel)
            }
        }

        private void CreatePassiveEntry(PassiveData passive, string levelLabel, bool unlocked)
        {
            PassiveEntryUI entry = Instantiate(passiveEntryPrefab, passivesContainer);
            entry.Setup(passive, levelLabel, unlocked);
        }

        private void CreateSpecializationEntry(int requiredLevel, bool available)
        {
            PassiveEntryUI entry = Instantiate(passiveEntryPrefab, passivesContainer);
            entry.SetupAsSpecialization(requiredLevel, available, GetAvailableSpecializations());
        }

        private string GetAvailableSpecializations()
        {
            if (_currentData == null) return string.Empty;

            var specs = _currentData.GetAvailableSpecializations(_currentOwned != null ? _currentOwned.level : 1);
            if (specs.Count <= 1) return string.Empty;

            var names = new StringBuilder();
            for (int i = 1; i < specs.Count; i++)
            {
                if (i > 1) names.Append(" / ");
                names.Append(specs[i].spec.SpecName);
            }
            return names.ToString();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — INTERACTIONS
        // ═══════════════════════════════════════════

        private void ToggleExpand()
        {
            _isExpanded = !_isExpanded;

            float targetHeight = _isExpanded ? panelOpenHeight : panelClosedHeight;

            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);

            _animationCoroutine = StartCoroutine(AnimatePanelHeight(targetHeight));

            UpdateExpandArrow();
        }

        private IEnumerator AnimatePanelHeight(float targetHeight)
        {
            if (statsPanel == null)
            {
                _animationCoroutine = null;
                yield break;
            }

            float startHeight = statsPanel.sizeDelta.y;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animationDuration;
                float newHeight = Mathf.Lerp(startHeight, targetHeight, t);
                statsPanel.sizeDelta = new Vector2(statsPanel.sizeDelta.x, newHeight);
                yield return null;
            }

            statsPanel.sizeDelta = new Vector2(statsPanel.sizeDelta.x, targetHeight);
            _animationCoroutine = null;
        }

        private void UpdateExpandArrow()
        {
            if (expandArrowIcon != null)
            {
                // Rotation de la flèche
                float rotation = _isExpanded ? 180f : 0f;
                expandArrowIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            }
        }

        private void OnAddToTeamClicked()
        {
            if (string.IsNullOrEmpty(_currentCharacterId)) return;
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null) return;

            var manager = PersistentManager.Instance.Characters;

            if (manager.IsInTeam(_currentCharacterId))
            {
                manager.RemoveFromTeam(_currentCharacterId);
            }
            else
            {
                manager.AddToTeam(_currentCharacterId);
            }

            PersistentManager.Instance.SaveGame();
            UpdateTeamButton();
        }

        private void UpdateTeamButton()
        {
            if (addToTeamButtonText == null) return;
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null) return;

            bool isInTeam = PersistentManager.Instance.Characters.IsInTeam(_currentCharacterId);
            addToTeamButtonText.text = isInTeam ? "Retirer de l'équipe" : "Ajouter à l'équipe";
        }
    }
}
