using System.Collections;
using System;
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
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private TextMeshProUGUI backstoryPreviewText;
        [SerializeField] private int backstoryPreviewMaxChars = 80;
        [SerializeField] private Image statsPanelBackground;
        [SerializeField] private float colorTransitionDuration = 0.2f;
        [Header("Tab Bar")]
        [SerializeField] private GameObject tabBar;
        [SerializeField] private SpecTabButton specTabButtonPrefab;

        [Header("Expanded Zone")]
        [SerializeField] private GameObject expandedZone;
        [SerializeField] private RectTransform expandedZoneRect;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private TextMeshProUGUI backstoryTextInContainer;
        [SerializeField] private float maxExpandedHeightRatio = 0.6f;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI atkText;
        [SerializeField] private TextMeshProUGUI defText;
        [SerializeField] private TextMeshProUGUI speedText;

        [Header("Prefabs")]
        [SerializeField] private PassiveEntryUI passiveEntryPrefab;
        [SerializeField] private SeparatorUI separatorPrefab;

        [Header("Bouton Dépliant")]
        [SerializeField] private Button expandButton;
        [SerializeField] private Image expandArrowIcon;

        [Header("Sprites flèches")]
        [SerializeField] private Sprite arrowExpandDown;
        [SerializeField] private Sprite arrowExpandUp;

        [Header("Footer")]
        [SerializeField] private Button addToTeamButton;
        [SerializeField] private TextMeshProUGUI addToTeamButtonText;
        [SerializeField] private Button closeButton;

        [Header("Composants")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TeamPageUI teamPageUI;

        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private static readonly Color BG_ATTACKER = new Color(0.35f, 0.10f, 0.10f, 1f);
        private static readonly Color BG_DEFENDER = new Color(0.10f, 0.28f, 0.14f, 1f);
        private static readonly Color BG_SUPPORT = new Color(0.10f, 0.18f, 0.35f, 1f);
        private static readonly Color BG_DEFAULT = new Color(0.15f, 0.15f, 0.15f, 1f);

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private string _currentCharacterId;
        private CharacterData _currentData;
        private OwnedCharacter _currentOwned;
        private bool _isExpanded = false;
        private Coroutine _animationCoroutine;
        private int _selectedSpecIndex = -1;
        private readonly List<SpecTabButton> _tabButtons = new List<SpecTabButton>();
        private Coroutine _colorCoroutine;

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
            if (data == null || owned == null)
                return;

            _currentCharacterId = owned.characterId;
            _currentData = data;
            _currentOwned = owned;
            _selectedSpecIndex = _currentOwned.GetSpecialization();

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            // Reset de l'état du panneau avant affichage.
            _isExpanded = false;
            ClearExpandedContent();

            if (expandedZone != null)
                expandedZone.SetActive(false);

            if (statsPanel != null)
                statsPanel.sizeDelta = new Vector2(statsPanel.sizeDelta.x, panelClosedHeight);

            BuildTabBar();
            ApplyStatsPanelColor(_selectedSpecIndex, animate: false);
            RefreshBackstoryPreview();
            if (backstoryPreviewText != null)
                backstoryPreviewText.gameObject.SetActive(true);

            RefreshDisplay();
            UpdateExpandArrow();

            ShowPopup();
        }

        /// <summary>
        /// Ferme le popup.
        /// </summary>
        public void Close()
        {
            HidePopup();
            CleanupTabBar();
            if (_colorCoroutine != null)
            {
                StopCoroutine(_colorCoroutine);
                _colorCoroutine = null;
            }
            if (statsPanelBackground != null)
                statsPanelBackground.color = BG_DEFAULT;
            _currentCharacterId = null;
            _currentData = null;
            _currentOwned = null;
            _selectedSpecIndex = -1;
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

            RefreshStatsDisplay();

            // Bouton équipe
            UpdateTeamButton();
        }

        private void PopulateExpandedContent()
        {
            if (contentContainer == null || backstoryTextInContainer == null)
                return;

            // Nettoyage immédiat du contenu instancié (on conserve le bloc backstory).
            ClearExpandedContent();

            if (_currentData == null || _currentOwned == null) return;

            // Backstory complète en mode déplié.
            backstoryTextInContainer.text = _currentData.Backstory ?? string.Empty;

            SpecializationData activeSpec = _currentData.GetSpecialization(_selectedSpecIndex);
            if (activeSpec == null) return;

            IReadOnlyList<PassiveSlot> slots = activeSpec.GetPassiveSlots();
            InstantiatePassiveEntries(slots);
        }

        /// <summary>
        /// Instancie les entrées de passifs dans le contentContainer en regroupant les slots par UnlockLevel.
        /// Un SeparatorUI est ajouté avant chaque groupe.
        /// </summary>
        private void InstantiatePassiveEntries(IReadOnlyList<PassiveSlot> slots)
        {
            if (contentContainer == null || slots == null || slots.Count == 0)
                return;

            // Grouper par niveau de déblocage en conservant l'ordre d'apparition des niveaux.
            var groups = new List<(int unlockLevel, List<PassiveData> passives)>();

            for (int i = 0; i < slots.Count; i++)
            {
                PassiveSlot slot = slots[i];
                if (slot == null || slot.PassiveData == null)
                    continue;

                int level = slot.UnlockLevel;
                bool found = false;

                for (int g = 0; g < groups.Count; g++)
                {
                    if (groups[g].unlockLevel == level)
                    {
                        groups[g].passives.Add(slot.PassiveData);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var newGroup = new List<PassiveData> { slot.PassiveData };
                    groups.Add((level, newGroup));
                }
            }

            for (int g = 0; g < groups.Count; g++)
            {
                var group = groups[g];

                if (separatorPrefab != null)
                    Instantiate(separatorPrefab, contentContainer);

                if (passiveEntryPrefab != null)
                {
                    PassiveEntryUI entry = Instantiate(passiveEntryPrefab, contentContainer);
                    bool unlocked = _currentOwned != null && _currentOwned.level >= group.unlockLevel;
                    entry.Setup(group.passives, "Nv. " + group.unlockLevel, unlocked);
                }
            }
        }

        private void ClearExpandedContent()
        {
            if (contentContainer == null)
                return;

            for (int i = contentContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = contentContainer.GetChild(i);
                if (backstoryTextInContainer != null && child == backstoryTextInContainer.transform)
                    continue;

                DestroyImmediate(child.gameObject);
            }
        }

        private void BuildTabBar()
        {
            foreach (SpecTabButton btn in _tabButtons)
            {
                if (btn != null)
                    btn.Cleanup();
            }
            _tabButtons.Clear();

            if (tabBar == null || specTabButtonPrefab == null)
                return;

            Transform tabBarTransform = tabBar.transform;
            for (int i = tabBarTransform.childCount - 1; i >= 0; i--)
                DestroyImmediate(tabBarTransform.GetChild(i).gameObject);

            if (_currentData == null)
                return;

            var specIndices = new List<int> { -1 };
            int altCount = _currentData.GetSpecializationCount();
            for (int i = 0; i < altCount; i++)
                specIndices.Add(i);

            for (int i = 0; i < specIndices.Count; i++)
            {
                int specIndex = specIndices[i];
                SpecializationData spec = _currentData.GetSpecialization(specIndex);
                if (spec == null)
                    continue;

                string label = GetRoleLabel(spec.Role);
                SpecTabButton tab = Instantiate(specTabButtonPrefab, tabBarTransform);
                tab.Setup(label, spec.Role, specIndex, OnTabClicked);
                _tabButtons.Add(tab);
            }

            RefreshTabVisuals();
            tabBar.SetActive(true);
        }

        private void CleanupTabBar()
        {
            foreach (SpecTabButton btn in _tabButtons)
            {
                if (btn != null)
                    btn.Cleanup();
            }
            _tabButtons.Clear();
        }

        /// <summary>
        /// Couleur de fond du panneau stats selon le rôle de la spécialisation affichée.
        /// </summary>
        private Color GetBgColorForSpec(int specIndex)
        {
            if (_currentData == null)
                return BG_DEFAULT;
            SpecializationData spec = _currentData.GetSpecialization(specIndex);
            if (spec == null)
                return BG_DEFAULT;

            return spec.Role switch
            {
                CharacterRole.Attacker => BG_ATTACKER,
                CharacterRole.Defender => BG_DEFENDER,
                CharacterRole.Support => BG_SUPPORT,
                _ => BG_DEFAULT
            };
        }

        /// <summary>
        /// Interpolation douce vers la couleur cible (temps de jeu non mis en pause).
        /// </summary>
        private IEnumerator AnimateStatsPanelColor(Color targetColor)
        {
            if (statsPanelBackground == null)
                yield break;

            Color startColor = statsPanelBackground.color;
            float elapsed = 0f;
            float duration = Mathf.Max(0.0001f, colorTransitionDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                statsPanelBackground.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            statsPanelBackground.color = targetColor;
            _colorCoroutine = null;
        }

        /// <summary>
        /// Applique la couleur de fond du StatsPanel pour l'index de spé donné (immédiat ou animé).
        /// </summary>
        private void ApplyStatsPanelColor(int specIndex, bool animate)
        {
            if (statsPanelBackground == null)
                return;

            Color target = GetBgColorForSpec(specIndex);

            if (!animate)
            {
                // Application immédiate à l'ouverture
                if (_colorCoroutine != null)
                {
                    StopCoroutine(_colorCoroutine);
                    _colorCoroutine = null;
                }
                statsPanelBackground.color = target;
                return;
            }

            if (_colorCoroutine != null)
                StopCoroutine(_colorCoroutine);
            _colorCoroutine = StartCoroutine(AnimateStatsPanelColor(target));
        }

        private string GetRoleLabel(CharacterRole role)
        {
            return role switch
            {
                CharacterRole.Attacker => "Spé Attaque",
                CharacterRole.Defender => "Spé Défense",
                CharacterRole.Support => "Spé Soutien",
                _ => "Spé"
            };
        }

        private void OnTabClicked(int specIndex)
        {
            if (_selectedSpecIndex == specIndex)
                return;

            _selectedSpecIndex = specIndex;
            RefreshTabVisuals();
            RefreshStatsDisplay();
            ApplyStatsPanelColor(_selectedSpecIndex, animate: true);

            if (_isExpanded)
                RefreshPassivesOnly();
        }

        private void RefreshTabVisuals()
        {
            int altCount = _currentData != null ? _currentData.GetSpecializationCount() : 0;
            var specIndices = new List<int> { -1 };
            for (int i = 0; i < altCount; i++)
                specIndices.Add(i);

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (_tabButtons[i] == null)
                    continue;

                bool isActive = (i < specIndices.Count) && (specIndices[i] == _selectedSpecIndex);
                _tabButtons[i].SetActive(isActive);
            }
        }

        private void RefreshStatsDisplay()
        {
            if (_currentData == null || _currentOwned == null)
                return;

            if (typeText != null)
            {
                SpecializationData activeSpec = _currentData.GetSpecialization(_selectedSpecIndex);
                if (activeSpec != null)
                    typeText.text = _currentData.Rarity + " • " + activeSpec.Role;
                else
                    typeText.text = _currentData.Rarity + " • " + _currentData.Role;
            }

            SpecializationData spec = _currentData.GetSpecialization(_selectedSpecIndex);
            int level = _currentOwned.level;

            if (hpText != null)
                hpText.text = spec != null ? spec.GetHpAtLevel(level).ToString() : "—";
            if (atkText != null)
                atkText.text = spec != null ? spec.GetAtkAtLevel(level).ToString() : "—";
            if (defText != null)
                defText.text = spec != null ? spec.GetDefAtLevel(level).ToString() : "—";
            if (speedText != null)
                speedText.text = spec != null ? spec.GetSpeedAtLevel(level).ToString() : "—";
        }

        private void RefreshPassivesOnly()
        {
            if (contentContainer == null || _currentData == null || _currentOwned == null)
                return;

            for (int i = contentContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = contentContainer.GetChild(i);
                if (backstoryTextInContainer != null && child == backstoryTextInContainer.transform)
                    continue;

                DestroyImmediate(child.gameObject);
            }

            SpecializationData spec = _currentData.GetSpecialization(_selectedSpecIndex);
            if (spec == null)
                return;

            IReadOnlyList<PassiveSlot> slots = spec.GetPassiveSlots();
            InstantiatePassiveEntries(slots);

            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(RecalculateExpandedHeight());
        }

        /// <summary>
        /// Met à jour le texte de preview de backstory pour le mode replié.
        /// </summary>
        private void RefreshBackstoryPreview()
        {
            if (backstoryPreviewText == null || _currentData == null)
                return;

            string full = _currentData.Backstory ?? string.Empty;

            int maxChars = Mathf.Max(0, backstoryPreviewMaxChars);
            if (full.Length <= maxChars)
                backstoryPreviewText.text = full;
            else
                backstoryPreviewText.text = full.Substring(0, maxChars) + "...";
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — INTERACTIONS
        // ═══════════════════════════════════════════

        private void ToggleExpand()
        {
            _isExpanded = !_isExpanded;

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            _animationCoroutine = StartCoroutine(_isExpanded ? ExpandRoutine() : CollapseRoutine());

            UpdateExpandArrow();
        }

        private IEnumerator ExpandRoutine()
        {
            if (expandedZone != null)
                expandedZone.SetActive(true);

            if (backstoryPreviewText != null)
                backstoryPreviewText.gameObject.SetActive(false);

            PopulateExpandedContent();
            yield return RecalculateExpandedHeight();

            _animationCoroutine = null;
        }

        private IEnumerator RecalculateExpandedHeight()
        {
            yield return null;
            yield return null;

            float contentHeight = 0f;
            if (contentContainer is RectTransform contentRect)
                contentHeight = LayoutUtility.GetPreferredHeight(contentRect);

            float maxHeight = Screen.height * maxExpandedHeightRatio;
            float targetExpandedHeight = Mathf.Min(contentHeight, maxHeight);

            if (expandedZoneRect != null)
                expandedZoneRect.sizeDelta = new Vector2(expandedZoneRect.sizeDelta.x, targetExpandedHeight);

            float targetPanelHeight = panelClosedHeight + targetExpandedHeight;
            yield return AnimatePanelHeight(targetPanelHeight);
        }

        private IEnumerator CollapseRoutine()
        {
            yield return AnimatePanelHeight(panelClosedHeight, () =>
            {
                if (expandedZone != null)
                    expandedZone.SetActive(false);

                if (backstoryPreviewText != null)
                    backstoryPreviewText.gameObject.SetActive(true);

                ClearExpandedContent();
                RefreshBackstoryPreview();
            });

            _animationCoroutine = null;
        }

        private IEnumerator AnimatePanelHeight(float targetHeight, Action onComplete = null)
        {
            if (statsPanel == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float duration = Mathf.Max(0.0001f, animationDuration);
            float startHeight = statsPanel.sizeDelta.y;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float newHeight = Mathf.Lerp(startHeight, targetHeight, t);
                statsPanel.sizeDelta = new Vector2(statsPanel.sizeDelta.x, newHeight);
                yield return null;
            }

            statsPanel.sizeDelta = new Vector2(statsPanel.sizeDelta.x, targetHeight);
            onComplete?.Invoke();
        }

        private void UpdateExpandArrow()
        {
            if (expandArrowIcon != null)
            {
                expandArrowIcon.sprite = _isExpanded ? arrowExpandUp : arrowExpandDown;
            }
        }

        private void OnAddToTeamClicked()
        {
            if (string.IsNullOrEmpty(_currentCharacterId)) return;
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null) return;

            var manager = PersistentManager.Instance.Characters;
            int presetAvant = manager.ActivePresetIndex;
            bool étaitDansÉquipe = manager.IsInTeam(_currentCharacterId);
            Debug.Log($"[CharacterDetailPopup] OnAddToTeamClicked | id='{_currentCharacterId}' | preset={presetAvant} | " +
                      $"déjà dans équipe={étaitDansÉquipe} | action={(étaitDansÉquipe ? "RemoveFromTeam" : "AddToTeam")}");

            if (manager.IsInTeam(_currentCharacterId))
            {
                manager.RemoveFromTeam(_currentCharacterId);
            }
            else
            {
                manager.AddToTeam(_currentCharacterId);
            }

            Debug.Log($"[CharacterDetailPopup] Après modif équipe | preset={manager.ActivePresetIndex} | " +
                      $"IsInTeam maintenant={manager.IsInTeam(_currentCharacterId)}");

            PersistentManager.Instance.SaveGame();

            // Forcer le refresh de TeamPageUI indépendamment
            // des events — corrige le cas où TeamPageUI est
            // désabonné pendant l'ouverture du popup
            if (teamPageUI != null)
                teamPageUI.RefreshDisplay();

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
