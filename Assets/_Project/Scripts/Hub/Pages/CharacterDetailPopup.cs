using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Characters;
using ChezArthur.Gameplay;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Popup détails personnage (Gate 5.c) : artwork magnifié, back flèche,
    /// hold-to-equip sur artwork, badge équipe. Signatures Open/OpenLive intactes.
    /// </summary>
    public class CharacterDetailPopup : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float MoveCancelPx = 8f;
        /// <summary> Dégagement header (Back / nom) hors zone hold. </summary>
        private const float HeaderHoldClearance = 120f;
        /// <summary> Inset haut ExpandedZone : Title + TabBar + StatsRow. </summary>
        private const float ExpandedTopInset = 280f;
        private const float ExpandedBottomInset = 4f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Header (sur artwork)")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject inTeamBadge;
        [SerializeField] private TextMeshProUGUI inTeamBadgeText;

        [Header("Artwork")]
        [SerializeField] private CharacterArtworkView artworkView;
        [SerializeField] private RectTransform artworkHoldArea;
        [SerializeField] private Sprite holdRingSprite;
        [SerializeField] private RarityBadgeView rarityBadge;

        [Header("Encadré Stats/Passifs")]
        [SerializeField] private RectTransform statsPanel;
        [SerializeField] private Image statsPanelBackground;
        [SerializeField] private float panelClosedHeight = 270f;
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
        [SerializeField] private float maxExpandedHeightRatio = 0.78f;

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

        [Header("Composants")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TeamPageUI teamPageUI;
        [SerializeField] private TeamDragController teamDragController;

        [Header("Refonte")]
        [SerializeField] private Image panelTopBorder;
        [SerializeField] private Image loreAccentBorder;
        [SerializeField] private Button switchArtworkButton;
        [SerializeField] private Image artworkDimOverlay;
        [SerializeField] private RarityShineFX artworkShine;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private string _currentCharacterId;
        private CharacterData _currentData;
        private OwnedCharacter _currentOwned;
        private CharacterBall _liveBall;
        private bool _liveMode;
        private bool _isExpanded;
        private Coroutine _animationCoroutine;
        private int _selectedSpecIndex = -1;
        private readonly List<SpecTabButton> _tabButtons = new List<SpecTabButton>();
        private readonly List<int> _tabSpecIndices = new List<int>();
        private readonly List<PassiveEntryUI> _passivePool = new List<PassiveEntryUI>();
        private readonly List<SeparatorUI> _separatorPool = new List<SeparatorUI>();
        private int _passivePoolUsed;
        private int _separatorPoolUsed;

        private int _holdPointerId = int.MinValue;
        private bool _holdActive;
        private bool _holdConsumed;
        private bool _holdMoved;
        private float _holdPressTime;
        private Vector2 _holdPressPos;
        private HoldProgressFX _holdFx;
        private ArtworkHoldRelay _holdRelay;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (expandButton != null)
                expandButton.onClick.AddListener(ToggleExpand);

            if (backButton != null)
                backButton.onClick.AddListener(Close);

            if (switchArtworkButton != null)
                switchArtworkButton.onClick.AddListener(OnSwitchArtworkClicked);

            EnsureHoldRelay();
            LayoutHoldArea();
            LayoutExpandedZone();
            HidePopup();
        }

        private void OnDestroy()
        {
            if (expandButton != null)
                expandButton.onClick.RemoveListener(ToggleExpand);

            if (backButton != null)
                backButton.onClick.RemoveListener(Close);

            if (switchArtworkButton != null)
                switchArtworkButton.onClick.RemoveListener(OnSwitchArtworkClicked);
        }

        private void OnDisable()
        {
            if (artworkView != null)
                artworkView.Release();
            CancelHold();
        }

        private void Update()
        {
            if (!_holdActive || _holdConsumed || _holdMoved || _liveMode)
                return;

            if (!TryGetPointerScreenPos(_holdPointerId, out Vector2 screenPos))
            {
                CancelHold();
                return;
            }

            if ((screenPos - _holdPressPos).sqrMagnitude > MoveCancelPx * MoveCancelPx)
            {
                _holdMoved = true;
                HideHoldFx();
                return;
            }

            float progress = (Time.unscaledTime - _holdPressTime) / TeamDragController.LongPressSeconds;
            if (_holdFx != null)
                _holdFx.SetProgress(progress);

            if (progress >= 1f)
                ExecuteArtworkHold();
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

            _liveMode = false;
            _liveBall = null;
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

            LayoutHoldArea();
            LayoutExpandedZone();

            ApplyRarityChrome();
            ApplyRoleLiseré();
            ApplyPanelSurface(expanded: false);
            BuildTabBar();
            if (backstoryPreviewText != null)
                backstoryPreviewText.gameObject.SetActive(false);

            ShowPopup();
            RefreshDisplay();
            UpdateExpandArrow();
            UpdateInTeamBadge();
            ConfigureArtworkShine();
            SetHoldEnabled(true);
            WireBackButton();
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

            _liveMode = true;
            SetHoldEnabled(false);
            if (inTeamBadge != null)
                inTeamBadge.SetActive(false);

            if (levelText != null)
                levelText.text = "Nv." + ball.CharacterLevel;
        }

        /// <summary>
        /// Ferme le popup.
        /// </summary>
        public void Close()
        {
            CancelHold();

            if (artworkView != null)
                artworkView.Release();

            HidePopup();
            CleanupTabBar();
            ClearExpandedContent();

            if (artworkDimOverlay != null)
                artworkDimOverlay.gameObject.SetActive(false);

            _liveBall = null;
            _liveMode = false;

            _currentCharacterId = null;
            _currentData = null;
            _currentOwned = null;
            _selectedSpecIndex = -1;
        }

        public void NotifyArtworkPointerDown(PointerEventData eventData)
        {
            if (_liveMode || eventData == null)
                return;
            if (_holdActive || _holdConsumed)
                return;

            _holdPointerId = eventData.pointerId;
            _holdActive = true;
            _holdConsumed = false;
            _holdMoved = false;
            _holdPressTime = Time.unscaledTime;
            _holdPressPos = eventData.position;

            RectTransform host = artworkHoldArea != null
                ? artworkHoldArea
                : (RectTransform)transform;
            _holdFx = HoldProgressFX.Ensure(host, holdRingSprite);
            if (_holdFx != null)
                _holdFx.ShowAt(Vector2.zero);
        }

        public void NotifyArtworkPointerUp(PointerEventData eventData)
        {
            if (eventData == null)
                return;
            if (_holdPointerId != int.MinValue && eventData.pointerId != _holdPointerId)
                return;

            HideHoldFx();
            _holdActive = false;
            _holdConsumed = false;
            _holdMoved = false;
            _holdPointerId = int.MinValue;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — AFFICHAGE
        // ═══════════════════════════════════════════

        private void ShowPopup()
        {
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

            if (rarityBadge != null)
                rarityBadge.SetPlaying(true);
        }

        private void HidePopup()
        {
            if (rarityBadge != null)
                rarityBadge.SetPlaying(false);

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

            // Chip rareté retiré (Gate 5.c.1) — ne plus colorer de cadre header.

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
            {
                nameText.text = _currentData.CharacterName;
                nameText.fontSize = UiTypography.Title;
                nameText.fontStyle = FontStyles.Bold;
                nameText.color = UiTheme.TextPrimary;
                nameText.raycastTarget = false;
            }

            if (levelText != null)
                levelText.text = "Nv." + _currentOwned.level.ToString();

            if (artworkView != null)
                artworkView.Show(_currentData, _currentOwned);

            if (rarityBadge != null)
                rarityBadge.Bind(_currentData.Rarity);

            RefreshStatsDisplay();
            UpdateInTeamBadge();
            // Shine désactivé (Gate 5.c.1)
        }

        private void ConfigureArtworkShine()
        {
            // Gate 5.c.1 : aucun shine sur le popup.
            if (artworkShine != null)
                artworkShine.enabled = false;
        }

        private void UpdateInTeamBadge()
        {
            // BR1 polish : pastille « OK En équipe » retirée — le badge de rareté
            // garde le coin artwork ; l'état équipe reste visible via le dock.
            if (inTeamBadge != null)
                inTeamBadge.SetActive(false);
        }

        private void SetHoldEnabled(bool enabled)
        {
            bool on = enabled && !_liveMode;
            if (_holdRelay != null)
                _holdRelay.SetRelayEnabled(on);
            if (artworkHoldArea != null)
            {
                Image img = artworkHoldArea.GetComponent<Image>();
                if (img != null)
                    img.raycastTarget = on;
            }
        }

        private void EnsureHoldRelay()
        {
            if (artworkHoldArea == null)
                return;

            _holdRelay = artworkHoldArea.GetComponent<ArtworkHoldRelay>();
            if (_holdRelay == null)
                _holdRelay = artworkHoldArea.gameObject.AddComponent<ArtworkHoldRelay>();
            _holdRelay.Bind(this);

            Image img = artworkHoldArea.GetComponent<Image>();
            if (img == null)
                img = artworkHoldArea.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;

            // Back doit rester cliquable (PanelSurface coupe les raycasts par défaut).
            WireBackButton();
        }

        private void Start()
        {
            // Après tous les Awake/OnEnable PanelSurface.
            WireBackButton();
            LayoutHoldArea();
        }

        private void WireBackButton()
        {
            if (backButton == null)
            {
                Transform backTx = FindDeepChild(transform, "BackButton");
                if (backTx != null)
                    backButton = backTx.GetComponent<Button>();
            }

            if (backButton == null)
                return;

            // Au-dessus du StatsPanel (qui raycast sur tout le bas) — sinon le clic est mangé.
            float panelH = panelClosedHeight;
            if (statsPanel != null && statsPanel.gameObject.activeInHierarchy)
                panelH = Mathf.Max(panelClosedHeight, statsPanel.sizeDelta.y);

            RectTransform brt = backButton.transform as RectTransform;
            if (brt != null)
            {
                if (backButton.transform.parent != transform)
                    backButton.transform.SetParent(transform, false);

                brt.anchorMin = new Vector2(0f, 0f);
                brt.anchorMax = new Vector2(0f, 0f);
                brt.pivot = new Vector2(0f, 0f);
                brt.sizeDelta = new Vector2(72f, 72f);
                brt.anchoredPosition = new Vector2(16f, panelH + 12f);
                brt.localEulerAngles = Vector3.zero;
            }

            backButton.onClick.RemoveListener(Close);
            backButton.onClick.AddListener(Close);
            backButton.transition = Selectable.Transition.None;
            backButton.interactable = true;

            PanelSurface surface = backButton.GetComponent<PanelSurface>();
            if (surface != null)
                surface.BlocksRaycasts = true;

            Image rootImg = backButton.GetComponent<Image>();
            if (rootImg != null)
            {
                Color c = rootImg.color;
                c.a = 0f;
                rootImg.color = c;
                rootImg.enabled = true;
                rootImg.raycastTarget = true;
                backButton.targetGraphic = rootImg;
            }

            Transform fill = backButton.transform.Find("Fill");
            if (fill != null)
                fill.gameObject.SetActive(false);

            Transform icon = backButton.transform.Find("Icon");
            if (icon != null)
            {
                Image iconImg = icon.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.raycastTarget = false;
                    iconImg.preserveAspect = true;
                }

                RectTransform irt = icon as RectTransform;
                if (irt != null)
                {
                    irt.anchorMin = new Vector2(0.5f, 0.5f);
                    irt.anchorMax = new Vector2(0.5f, 0.5f);
                    irt.pivot = new Vector2(0.5f, 0.5f);
                    irt.anchoredPosition = Vector2.zero;
                    irt.sizeDelta = new Vector2(44f, 44f);
                }
            }

            backButton.transform.SetAsLastSibling();
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c.name == name)
                    return c;
                Transform nested = FindDeepChild(c, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        /// <summary>
        /// Hold uniquement sur l'artwork visible : hors header (Back) et hors StatsPanel.
        /// </summary>
        private void LayoutHoldArea()
        {
            if (artworkHoldArea == null)
                return;

            float bottom = panelClosedHeight;
            if (statsPanel != null && statsPanel.gameObject.activeInHierarchy)
                bottom = Mathf.Max(panelClosedHeight, statsPanel.sizeDelta.y);

            RectTransform rt = artworkHoldArea;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(0f, bottom);
            rt.offsetMax = new Vector2(0f, -HeaderHoldClearance);

            Transform header = transform.Find("Header");
            if (header != null)
            {
                int hi = header.GetSiblingIndex();
                artworkHoldArea.SetSiblingIndex(Mathf.Max(0, hi));
            }

            // Recaler le Back au-dessus du panneau (hauteur variable à l'expand).
            LayoutBackAboveStats(bottom);
        }

        private void LayoutBackAboveStats(float panelH)
        {
            if (backButton == null)
                return;

            RectTransform brt = backButton.transform as RectTransform;
            if (brt == null)
                return;

            if (backButton.transform.parent != transform)
                backButton.transform.SetParent(transform, false);

            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.zero;
            brt.pivot = Vector2.zero;
            brt.sizeDelta = new Vector2(72f, 72f);
            brt.anchoredPosition = new Vector2(16f, panelH + 12f);
            backButton.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Zone lore/passifs : sous tabs+stats, collée en bas du panneau (plus d'écart footer).
        /// </summary>
        private void LayoutExpandedZone()
        {
            if (expandedZoneRect == null && expandedZone != null)
                expandedZoneRect = expandedZone.transform as RectTransform;
            if (expandedZoneRect == null)
                return;

            expandedZoneRect.anchorMin = Vector2.zero;
            expandedZoneRect.anchorMax = Vector2.one;
            expandedZoneRect.pivot = new Vector2(0.5f, 0.5f);
            expandedZoneRect.anchoredPosition = Vector2.zero;
            expandedZoneRect.sizeDelta = Vector2.zero;
            expandedZoneRect.offsetMin = new Vector2(12f, ExpandedBottomInset);
            expandedZoneRect.offsetMax = new Vector2(-12f, -ExpandedTopInset);

            Transform scroll = expandedZoneRect.Find("ContentScrollView");
            if (scroll == null && expandedZoneRect.childCount > 0)
                scroll = expandedZoneRect.GetChild(0);
            if (scroll is RectTransform scrollRt)
            {
                scrollRt.anchorMin = Vector2.zero;
                scrollRt.anchorMax = Vector2.one;
                scrollRt.offsetMin = Vector2.zero;
                scrollRt.offsetMax = Vector2.zero;
                scrollRt.anchoredPosition = Vector2.zero;
                scrollRt.sizeDelta = Vector2.zero;
            }
        }

        private void ExecuteArtworkHold()
        {
            _holdConsumed = true;
            HideHoldFx();

            if (string.IsNullOrEmpty(_currentCharacterId))
                return;
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return;

            CharacterManager manager = PersistentManager.Instance.Characters;
            Transform punchTarget = artworkHoldArea != null
                ? (Transform)artworkHoldArea
                : transform;

            if (manager.IsInTeam(_currentCharacterId))
            {
                if (manager.RemoveFromTeam(_currentCharacterId))
                {
                    PersistentManager.Instance.SaveGame();
                    HoldFeedback.PlaySuccess(this, punchTarget, null);
                    if (teamPageUI != null)
                        teamPageUI.RefreshDisplay();
                    UpdateInTeamBadge();
                }

                return;
            }

            if (manager.GetSelectedTeamIds().Count >= CharacterManager.MAX_TEAM_SIZE)
            {
                HoldFeedback.PlayFailShake(this, punchTarget);
                if (teamDragController != null)
                    teamDragController.PulseDockDanger();
                return;
            }

            if (manager.AddToTeam(_currentCharacterId))
            {
                PersistentManager.Instance.SaveGame();
                HoldFeedback.PlaySuccess(this, punchTarget, null);
                if (teamPageUI != null)
                    teamPageUI.RefreshDisplay();
                if (teamDragController != null)
                    teamDragController.MarkHintSeenAndHide();
                UpdateInTeamBadge();
            }
        }

        private void CancelHold()
        {
            HideHoldFx();
            _holdActive = false;
            _holdConsumed = false;
            _holdMoved = false;
            _holdPointerId = int.MinValue;
        }

        private void HideHoldFx()
        {
            if (_holdFx != null)
                _holdFx.Hide();
            _holdFx = null;
        }

        private static bool TryGetPointerScreenPos(int pointerId, out Vector2 pos)
        {
            if (pointerId == -1)
            {
                pos = Input.mousePosition;
                return Input.GetMouseButton(0);
            }

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.fingerId == pointerId)
                    {
                        pos = t.position;
                        return t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                    }
                }
            }

            pos = Input.mousePosition;
            return Input.GetMouseButton(0);
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

            SpecializationData spec = _currentData.GetSpecialization(_selectedSpecIndex);
            int level = _currentOwned.level;

            if (levelText != null && _liveBall == null)
                levelText.text = "Nv." + level;

            if (_liveBall != null)
            {
                if (levelText != null)
                    levelText.text = "Nv." + _liveBall.CharacterLevel;
                if (hpText != null) hpText.text = _liveBall.CurrentHp + "/" + _liveBall.EffectiveMaxHp;
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
            SetHoldEnabled(false);

            if (expandedZone != null)
                expandedZone.SetActive(true);

            LayoutExpandedZone();

            if (backstoryPreviewText != null)
                backstoryPreviewText.gameObject.SetActive(false);

            PopulateExpandedContent();
            yield return RecalculateExpandedHeight();
            LayoutHoldArea();

            _animationCoroutine = null;
        }

        private IEnumerator RecalculateExpandedHeight()
        {
            yield return null;
            yield return null;

            LayoutExpandedZone();

            float contentHeight = 0f;
            if (contentContainer is RectTransform contentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                contentHeight = LayoutUtility.GetPreferredHeight(contentRect);
            }

            // Remplir l'écran : le ScrollRect gère le surplus ; plus d'écart bas.
            float maxPanel = Screen.height * maxExpandedHeightRatio;
            float needed = panelClosedHeight + Mathf.Max(200f, contentHeight + 24f);
            float targetPanelHeight = Mathf.Clamp(needed, panelClosedHeight + 200f, maxPanel);

            yield return AnimatePanelHeight(targetPanelHeight);
            LayoutExpandedZone();
            LayoutHoldArea();
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
                LayoutHoldArea();
                if (!_liveMode)
                    SetHoldEnabled(true);
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
    }
}
