using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Characters;
using ChezArthur.Gameplay;
using ChezArthur.UI;
using System.Collections.Generic;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Popup affichant les détails d'un personnage (artwork, stats, passifs).
    /// Compatible ancien prefab (champs refonte null-guardés) jusqu'au builder Gate 3.
    /// </summary>
    public class CharacterDetailPopup : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Header (sur artwork)")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI typeText;

        [Header("Artwork")]
        [SerializeField] private CharacterArtworkView artworkView;

        [Header("Encadré Stats/Passifs")]
        [SerializeField] private RectTransform statsPanel;
        [SerializeField] private Image statsPanelBackground;
        [SerializeField] private float panelClosedHeight = 440f;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private TextMeshProUGUI backstoryPreviewText;

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

        [Header("Refonte")]
        [SerializeField] private Image panelTopBorder;
        [SerializeField] private Image loreAccentBorder;
        [SerializeField] private TextMeshProUGUI rarityChipText;
        [SerializeField] private Image rarityChipFrame;
        [SerializeField] private Image primaryButtonFrame;
        [SerializeField] private Button switchArtworkButton;
        [SerializeField] private Image artworkDimOverlay;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private string _currentCharacterId;
        private CharacterData _currentData;
        private OwnedCharacter _currentOwned;
        private CharacterBall _liveBall;
        private bool _isExpanded;
        private Coroutine _animationCoroutine;
        private int _selectedSpecIndex = -1;
        private readonly List<SpecTabButton> _tabButtons = new List<SpecTabButton>();
        private readonly List<int> _tabSpecIndices = new List<int>();
        private readonly List<PassiveEntryUI> _passivePool = new List<PassiveEntryUI>();
        private readonly List<SeparatorUI> _separatorPool = new List<SeparatorUI>();
        private int _passivePoolUsed;
        private int _separatorPoolUsed;

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

            if (switchArtworkButton != null)
                switchArtworkButton.onClick.AddListener(OnSwitchArtworkClicked);

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

            if (switchArtworkButton != null)
                switchArtworkButton.onClick.RemoveListener(OnSwitchArtworkClicked);
        }

        private void OnDisable()
        {
            if (artworkView != null)
                artworkView.Release();
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

            _isExpanded = false;
            ClearExpandedContent();

            if (expandedZone != null)
                expandedZone.SetActive(false);

            if (artworkDimOverlay != null)
                artworkDimOverlay.gameObject.SetActive(false);

            if (statsPanel != null)
                statsPanel.sizeDelta = new Vector2(statsPanel.sizeDelta.x, panelClosedHeight);

            ApplyRarityChrome();
            ApplyRoleLiseré();
            ApplyPanelSurface(expanded: false);
            BuildTabBar();
            // Plié = stats + spé uniquement (pas de teaser lore).
            if (backstoryPreviewText != null)
                backstoryPreviewText.gameObject.SetActive(false);

            ShowPopup();
            RefreshDisplay();
            UpdateExpandArrow();
        }

        /// <summary>
        /// Ouvre le popup en mode in-run : stats live depuis le CharacterBall.
        /// </summary>
        public void OpenLive(CharacterBall ball)
        {
            if (ball == null || ball.Data == null || ball.OwnedCharacter == null)
                return;

            _liveBall = ball;
            Open(ball.Data, ball.OwnedCharacter);

            if (addToTeamButton != null)
                addToTeamButton.gameObject.SetActive(false);

            if (levelText != null)
                levelText.text = "Nv. " + ball.CharacterLevel;
        }

        /// <summary>
        /// Ferme le popup.
        /// </summary>
        public void Close()
        {
            if (artworkView != null)
                artworkView.Release();

            HidePopup();
            CleanupTabBar();
            ClearExpandedContent();

            if (artworkDimOverlay != null)
                artworkDimOverlay.gameObject.SetActive(false);

            _liveBall = null;
            if (addToTeamButton != null)
                addToTeamButton.gameObject.SetActive(true);

            _currentCharacterId = null;
            _currentData = null;
            _currentOwned = null;
            _selectedSpecIndex = -1;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — AFFICHAGE
        // ═══════════════════════════════════════════

        private void ShowPopup()
        {
            // Au-dessus des onglets pause / contenu équipe.
            transform.SetAsLastSibling();

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

        private void ApplyRarityChrome()
        {
            if (_currentData == null)
                return;

            Color rarityColor = CharacterRarityPalette.GetColor(_currentData.Rarity);

            if (rarityChipFrame != null)
                rarityChipFrame.color = rarityColor;

            if (rarityChipText != null)
            {
                // Texte clair sur fill sombre du chip (contraste) — la rareté colore le cadre.
                rarityChipText.color = UiTheme.TextPrimary;
                rarityChipText.text = _currentData.Rarity.ToString();
            }

            if (primaryButtonFrame != null)
                primaryButtonFrame.color = rarityColor;

            if (switchArtworkButton != null)
            {
                switchArtworkButton.interactable =
                    PortraitStateResolver.CanSwitchArtwork(_currentData, _currentOwned);
            }
        }

        /// <summary> Tous les accents rouge/bleu/vert suivent la spé active. </summary>
        private void ApplyRoleLiseré()
        {
            if (_currentData == null)
                return;

            SpecializationData spec = _currentData.GetSpecialization(_selectedSpecIndex);
            CharacterRole role = spec != null ? spec.Role : _currentData.Role;
            Color accent = RolePalette.GetColor(role);
            accent.a = 1f;

            if (panelTopBorder != null)
                panelTopBorder.color = accent;

            if (loreAccentBorder != null)
                loreAccentBorder.color = accent;
        }

        private void ApplyPanelSurface(bool expanded)
        {
            if (statsPanelBackground == null)
                return;

            statsPanelBackground.color = expanded
                ? UiTheme.CardPanel
                : UiTheme.CardPanelCollapsed;
        }

        private void RefreshDisplay()
        {
            if (_currentData == null || _currentOwned == null)
                return;

            if (nameText != null)
                nameText.text = _currentData.CharacterName;

            if (levelText != null)
                levelText.text = "Nv. " + _currentOwned.level.ToString();

            if (artworkView != null)
                artworkView.Show(_currentData, _currentOwned);

            RefreshStatsDisplay();
            UpdateTeamButton();
        }

        private void PopulateExpandedContent()
        {
            if (contentContainer == null || backstoryTextInContainer == null)
                return;

            ClearExpandedContent();

            if (_currentData == null || _currentOwned == null)
                return;

            backstoryTextInContainer.text = FormatLoreText(_currentData.Backstory);
            if (backstoryTextInContainer.transform.parent != null)
            {
                // Remonte au bloc lore si le TMP est enfant de LoreBlock.
                Transform loreRoot = backstoryTextInContainer.transform.parent;
                if (loreRoot.parent == contentContainer)
                    loreRoot.SetSiblingIndex(0);
                else if (backstoryTextInContainer.transform.parent == contentContainer)
                    backstoryTextInContainer.transform.SetSiblingIndex(0);
            }

            SpecializationData activeSpec = _currentData.GetSpecialization(_selectedSpecIndex);
            if (activeSpec == null)
                return;

            FillPassiveEntries(activeSpec.GetPassiveSlots(), activeSpec.Role);
        }

        private void FillPassiveEntries(IReadOnlyList<PassiveSlot> slots, CharacterRole role)
        {
            if (contentContainer == null || slots == null || slots.Count == 0)
            {
                DeactivateUnusedPoolItems();
                return;
            }

            List<(int unlockLevel, List<PassiveData> passives)> groups =
                GroupPassiveSlots(slots);

            Color roleAccent = RolePalette.GetColor(role);
            int siblingIndex = 1;

            for (int g = 0; g < groups.Count; g++)
            {
                (int unlockLevel, List<PassiveData> passives) group = groups[g];

                SeparatorUI separator = AcquireSeparator();
                if (separator != null)
                {
                    separator.transform.SetSiblingIndex(siblingIndex);
                    siblingIndex++;
                }

                PassiveEntryUI entry = AcquirePassiveEntry();
                if (entry != null)
                {
                    bool unlocked = _currentOwned != null && _currentOwned.level >= group.unlockLevel;
                    entry.SetRoleAccent(roleAccent);
                    entry.Setup(group.passives, "Nv. " + group.unlockLevel, unlocked);
                    entry.transform.SetSiblingIndex(siblingIndex);
                    siblingIndex++;
                }
            }

            DeactivateUnusedPoolItems();
        }

        private static List<(int unlockLevel, List<PassiveData> passives)> GroupPassiveSlots(
            IReadOnlyList<PassiveSlot> slots)
        {
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

            return groups;
        }

        private SeparatorUI AcquireSeparator()
        {
            if (separatorPrefab == null && _separatorPool.Count == 0)
                return null;

            SeparatorUI sep;
            if (_separatorPoolUsed < _separatorPool.Count)
            {
                sep = _separatorPool[_separatorPoolUsed];
            }
            else
            {
                if (separatorPrefab == null)
                    return null;
                sep = Instantiate(separatorPrefab, contentContainer);
                _separatorPool.Add(sep);
            }

            _separatorPoolUsed++;
            if (sep != null)
                sep.gameObject.SetActive(true);
            return sep;
        }

        private PassiveEntryUI AcquirePassiveEntry()
        {
            if (passiveEntryPrefab == null && _passivePool.Count == 0)
                return null;

            PassiveEntryUI entry;
            if (_passivePoolUsed < _passivePool.Count)
            {
                entry = _passivePool[_passivePoolUsed];
            }
            else
            {
                if (passiveEntryPrefab == null)
                    return null;
                entry = Instantiate(passiveEntryPrefab, contentContainer);
                _passivePool.Add(entry);
            }

            _passivePoolUsed++;
            if (entry != null)
                entry.gameObject.SetActive(true);
            return entry;
        }

        private void DeactivateUnusedPoolItems()
        {
            for (int i = _separatorPoolUsed; i < _separatorPool.Count; i++)
            {
                if (_separatorPool[i] != null)
                    _separatorPool[i].gameObject.SetActive(false);
            }

            for (int i = _passivePoolUsed; i < _passivePool.Count; i++)
            {
                if (_passivePool[i] != null)
                    _passivePool[i].gameObject.SetActive(false);
            }
        }

        private void ClearExpandedContent()
        {
            _passivePoolUsed = 0;
            _separatorPoolUsed = 0;
            DeactivateUnusedPoolItems();
        }

        private void BuildTabBar()
        {
            if (tabBar == null || specTabButtonPrefab == null)
                return;

            if (_currentData == null)
            {
                CleanupTabBar();
                return;
            }

            var specIndices = new List<int> { -1 };
            int altCount = _currentData.GetSpecializationCount();
            for (int i = 0; i < altCount; i++)
                specIndices.Add(i);

            Transform tabBarTransform = tabBar.transform;
            int needed = 0;
            _tabSpecIndices.Clear();

            for (int i = 0; i < specIndices.Count; i++)
            {
                int specIndex = specIndices[i];
                SpecializationData spec = _currentData.GetSpecialization(specIndex);
                if (spec == null)
                    continue;

                SpecTabButton tab;
                if (needed < _tabButtons.Count && _tabButtons[needed] != null)
                {
                    tab = _tabButtons[needed];
                }
                else
                {
                    tab = Instantiate(specTabButtonPrefab, tabBarTransform);
                    if (needed < _tabButtons.Count)
                        _tabButtons[needed] = tab;
                    else
                        _tabButtons.Add(tab);
                }

                tab.gameObject.SetActive(true);
                tab.Setup(GetRoleLabel(spec.Role), spec.Role, specIndex, OnTabClicked);
                _tabSpecIndices.Add(specIndex);
                needed++;
            }

            for (int i = needed; i < _tabButtons.Count; i++)
            {
                if (_tabButtons[i] == null)
                    continue;
                _tabButtons[i].Cleanup();
                _tabButtons[i].gameObject.SetActive(false);
            }

            RefreshTabVisuals();
            tabBar.SetActive(true);
        }

        private void CleanupTabBar()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (_tabButtons[i] == null)
                    continue;
                _tabButtons[i].Cleanup();
                _tabButtons[i].gameObject.SetActive(false);
            }
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

        private static string GetRoleShortLabel(CharacterRole role)
        {
            return role switch
            {
                CharacterRole.Attacker => "Attaque",
                CharacterRole.Defender => "Défense",
                CharacterRole.Support => "Soutien",
                _ => "—"
            };
        }

        private void OnTabClicked(int specIndex)
        {
            if (_selectedSpecIndex == specIndex)
                return;

            _selectedSpecIndex = specIndex;
            RefreshTabVisuals();
            RefreshStatsDisplay();
            ApplyRoleLiseré();

            if (_isExpanded)
                RefreshPassivesOnly();
        }

        private void RefreshTabVisuals()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (_tabButtons[i] == null || !_tabButtons[i].gameObject.activeSelf)
                    continue;

                bool isActive = i < _tabSpecIndices.Count && _tabSpecIndices[i] == _selectedSpecIndex;
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
                CharacterRole role = activeSpec != null ? activeSpec.Role : _currentData.Role;
                typeText.text = GetRoleShortLabel(role);
                typeText.color = RolePalette.GetColor(role);
            }

            SpecializationData spec = _currentData.GetSpecialization(_selectedSpecIndex);
            int level = _currentOwned.level;

            if (_liveBall != null)
            {
                if (hpText != null) hpText.text = _liveBall.CurrentHp + " / " + _liveBall.EffectiveMaxHp;
                if (atkText != null) atkText.text = _liveBall.EffectiveAtk.ToString();
                if (defText != null) defText.text = _liveBall.EffectiveDef.ToString();
                if (speedText != null) speedText.text = _liveBall.EffectiveSpeed.ToString();
            }
            else
            {
                if (hpText != null)
                    hpText.text = spec != null ? spec.GetHpAtLevel(level).ToString() : "—";
                if (atkText != null)
                    atkText.text = spec != null ? spec.GetAtkAtLevel(level).ToString() : "—";
                if (defText != null)
                    defText.text = spec != null ? spec.GetDefAtLevel(level).ToString() : "—";
                if (speedText != null)
                    speedText.text = spec != null ? spec.GetSpeedAtLevel(level).ToString() : "—";
            }
        }

        private void RefreshPassivesOnly()
        {
            if (contentContainer == null || _currentData == null || _currentOwned == null)
                return;

            ClearExpandedContent();

            if (backstoryTextInContainer != null)
            {
                backstoryTextInContainer.text = FormatLoreText(_currentData.Backstory);
                Transform loreRoot = backstoryTextInContainer.transform.parent;
                if (loreRoot != null && loreRoot.parent == contentContainer)
                    loreRoot.SetSiblingIndex(0);
                else if (backstoryTextInContainer.transform.parent == contentContainer)
                    backstoryTextInContainer.transform.SetSiblingIndex(0);
            }

            SpecializationData spec = _currentData.GetSpecialization(_selectedSpecIndex);
            if (spec == null)
                return;

            FillPassiveEntries(spec.GetPassiveSlots(), spec.Role);

            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(RecalculateExpandedHeight());
        }

        private void RefreshBackstoryPreview()
        {
            // Teaser plié retiré — la lore n'apparaît qu'en déplié.
            if (backstoryPreviewText != null)
                backstoryPreviewText.gameObject.SetActive(false);
        }

        private static string FormatLoreText(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;
            return "« " + raw.Trim() + " »";
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
            if (artworkDimOverlay != null)
                artworkDimOverlay.gameObject.SetActive(true);

            ApplyPanelSurface(expanded: true);

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

            // La zone dépliée est en stretch (au-dessus du footer) : on grossit
            // seulement le panneau. Le ScrollRect gère le surplus de contenu.
            float contentHeight = 0f;
            if (contentContainer is RectTransform contentRect)
                contentHeight = LayoutUtility.GetPreferredHeight(contentRect);

            float maxPanel = Screen.height * maxExpandedHeightRatio;
            float extra = Mathf.Clamp(contentHeight, 280f, 520f);
            float targetPanelHeight = Mathf.Min(panelClosedHeight + extra, maxPanel);
            if (targetPanelHeight < panelClosedHeight + 280f)
                targetPanelHeight = panelClosedHeight + 280f;

            yield return AnimatePanelHeight(targetPanelHeight);
        }

        private IEnumerator CollapseRoutine()
        {
            yield return AnimatePanelHeight(panelClosedHeight, () =>
            {
                if (expandedZone != null)
                    expandedZone.SetActive(false);

                if (backstoryPreviewText != null)
                    backstoryPreviewText.gameObject.SetActive(false);

                if (artworkDimOverlay != null)
                    artworkDimOverlay.gameObject.SetActive(false);

                ApplyPanelSurface(expanded: false);
                ClearExpandedContent();
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

        private void OnSwitchArtworkClicked()
        {
            if (string.IsNullOrEmpty(_currentCharacterId))
                return;

            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return;

            OwnedCharacter persisted =
                PersistentManager.Instance.Characters.GetOwnedCharacter(_currentCharacterId);
            if (persisted == null)
                return;

            persisted.prefersDechuArtwork = !persisted.prefersDechuArtwork;

            // Mode live : _currentOwned peut être la même ref, sinon on synchronise.
            if (_currentOwned != null && !ReferenceEquals(_currentOwned, persisted))
                _currentOwned.prefersDechuArtwork = persisted.prefersDechuArtwork;

            PersistentManager.Instance.SaveGame();

            if (artworkView != null)
                artworkView.Show(_currentData, _currentOwned);
        }

        private void UpdateExpandArrow()
        {
            if (expandArrowIcon != null)
                expandArrowIcon.sprite = _isExpanded ? arrowExpandUp : arrowExpandDown;
        }

        private void OnAddToTeamClicked()
        {
            if (string.IsNullOrEmpty(_currentCharacterId))
                return;
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return;

            CharacterManager manager = PersistentManager.Instance.Characters;

            if (manager.IsInTeam(_currentCharacterId))
                manager.RemoveFromTeam(_currentCharacterId);
            else
                manager.AddToTeam(_currentCharacterId);

            PersistentManager.Instance.SaveGame();

            if (teamPageUI != null)
                teamPageUI.RefreshDisplay();

            UpdateTeamButton();
        }

        private void UpdateTeamButton()
        {
            if (addToTeamButtonText == null)
                return;
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return;

            bool isInTeam = PersistentManager.Instance.Characters.IsInTeam(_currentCharacterId);
            addToTeamButtonText.text = isInTeam ? "Retirer de l'équipe" : "Ajouter à l'équipe";
        }
    }
}
