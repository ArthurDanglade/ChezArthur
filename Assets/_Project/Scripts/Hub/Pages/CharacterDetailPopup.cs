using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Characters;

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
                typeText.text = _currentData.Rarity.ToString() + " • " + _currentData.Role.ToString();

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

            // Supprime les anciens
            foreach (Transform child in passivesContainer)
            {
                Destroy(child.gameObject);
            }

            if (_currentData.Passives == null) return;

            int level = _currentOwned.level;
            CharacterPassiveSet passives = _currentData.Passives;

            // Passif de base (toujours débloqué)
            if (passives.BasePassive != null)
            {
                CreatePassiveEntry(passives.BasePassive, "Nv. 1", true);
            }

            // Passif niveau 5
            if (passives.Level5Passive != null)
            {
                bool unlocked = level >= 5;
                CreatePassiveEntry(passives.Level5Passive, "Nv. 5", unlocked);
            }

            // Passif niveau 15 (LR uniquement)
            if (passives.Level15Passive != null && _currentData.Rarity == CharacterRarity.LR)
            {
                bool unlocked = level >= 15;
                CreatePassiveEntry(passives.Level15Passive, "Nv. 15", unlocked);
            }

            // Spécialisation
            int specLevel = _currentData.GetSpecializationLevel();
            if (specLevel > 0)
            {
                bool canSpecialize = level >= specLevel;
                bool hasSpecialized = _currentOwned.specialization != SpecializationType.None;

                if (hasSpecialized)
                {
                    // Affiche la spé choisie
                    PassiveData specPassive = passives.GetSpecializationPassive(_currentOwned.specialization);
                    if (specPassive != null)
                    {
                        CreatePassiveEntry(specPassive, "Spé " + _currentOwned.specialization.ToString(), true);
                    }
                }
                else
                {
                    // Affiche que la spé est verrouillée ou disponible
                    CreateSpecializationEntry(specLevel, canSpecialize);
                }
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
            if (_currentData.Rarity == CharacterRarity.SSR)
            {
                // SSR : 2 options
                var opt1 = _currentData.Passives.SsrSpecOption1Type;
                var opt2 = _currentData.Passives.SsrSpecOption2Type;
                return opt1.ToString() + " / " + opt2.ToString();
            }
            else if (_currentData.Rarity == CharacterRarity.LR)
            {
                // LR : 3 options
                return "ATK / DEF / SUP";
            }
            return string.Empty;
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
