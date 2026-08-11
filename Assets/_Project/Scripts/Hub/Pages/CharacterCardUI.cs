using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Carte de personnage dans la collection (Gate 5.a/5.b).
    /// </summary>
    public class CharacterCardUI : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float BannerHeight = 44f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Fond & portrait")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityBorder;

        [Header("Badge rareté (Dokkan)")]
        [SerializeField] private RarityBadgeView rarityBadge;

        [Header("Éveil")]
        [SerializeField] private Image awakenDot;

        [Header("Bandeau bas")]
        [SerializeField] private Image bottomBanner;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI roleLabel;

        [Header("En équipe")]
        [SerializeField] private GameObject inTeamIndicator;
        [SerializeField] private Image inTeamStrip;
        [SerializeField] private TextMeshProUGUI inTeamCheck;

        [Header("Interaction")]
        [SerializeField] private Button cardButton;
        [SerializeField] private CanvasGroup canvasGroup;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private string _characterId;
        private CharacterData _currentData;
        private OwnedCharacter _currentOwned;
        private Action<CharacterData, OwnedCharacter> _onClickCallback;
        private TeamDragController _dragController;
        private bool _suppressClick;
        private bool _pressTracking;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string CharacterId => _characterId;
        public CharacterData CurrentData => _currentData;
        public OwnedCharacter CurrentOwned => _currentOwned;
        public Image RarityBorderImage => rarityBorder;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (cardButton != null)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.transition = Selectable.Transition.None;
                // Empêche Button.OnPointerClick d'ouvrir le détail après un scroll.
                cardButton.enabled = false;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void BindDragController(TeamDragController controller)
        {
            _dragController = controller;
        }

        public void Setup(
            CharacterData data,
            OwnedCharacter owned,
            Action<CharacterData, OwnedCharacter> onClickCallback)
        {
            if (data == null || owned == null)
                return;

            _characterId = owned.characterId;
            _currentData = data;
            _currentOwned = owned;
            _onClickCallback = onClickCallback;
            _suppressClick = false;

            if (cardBackground != null)
            {
                cardBackground.color = UiTheme.BgElevated;
                cardBackground.enabled = true;
            }

            ApplyIcon(data);
            LayoutPortraitArea();

            Color rarityColor = CharacterRarityPalette.GetColor(data.Rarity);
            if (rarityBorder != null)
            {
                rarityBorder.color = rarityColor;
                rarityBorder.enabled = true;
            }

            if (rarityBadge != null)
                rarityBadge.Bind(data.Rarity);

            if (awakenDot != null)
            {
                awakenDot.color = UiTheme.AccentGold;
                awakenDot.gameObject.SetActive(owned.isAwakened);
            }

            if (bottomBanner != null)
            {
                Color banner = UiTheme.BgElevated;
                banner.a = 0.85f;
                bottomBanner.color = banner;
            }

            if (nameText != null)
                nameText.gameObject.SetActive(false);

            if (levelText != null)
            {
                levelText.gameObject.SetActive(true);
                levelText.text = "Nv." + owned.level.ToString();
                levelText.fontSize = UiTypography.Caption;
                levelText.color = UiTheme.TextMuted;
                levelText.alignment = TextAlignmentOptions.MidlineLeft;
            }

            ApplyRoleLabel(data, owned);
            ApplyInTeamChrome(false);
            SetSourceDimmed(false);
        }

        /// <summary> Legacy no-op (shine off Gate 5.c.1). </summary>
        public void SetShineViewport(RectTransform viewport)
        {
        }

        public void SetInTeam(bool inTeam)
        {
            ApplyInTeamChrome(inTeam);
        }

        public void BeginPotentialDrag()
        {
            _pressTracking = true;
            _suppressClick = false;
        }

        public void EndPotentialDrag()
        {
            _pressTracking = false;
        }

        public void SetSourceDimmed(bool dimmed)
        {
            if (canvasGroup == null)
                return;
            canvasGroup.alpha = dimmed ? 0.45f : 1f;
        }

        public void MarkLifted()
        {
            _suppressClick = true;
        }

        public void NotifyShortTap()
        {
            if (_suppressClick)
                return;
            _onClickCallback?.Invoke(_currentData, _currentOwned);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _dragController?.NotifyCardPointerDown(this, eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragController?.NotifyPointerUp(eventData);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyIcon(CharacterData data)
        {
            if (iconImage == null)
                return;

            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
            iconImage.preserveAspect = false;
            iconImage.type = Image.Type.Simple;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
        }

        private void LayoutPortraitArea()
        {
            float border = UiTheme.BorderFocus;
            float bannerH = BannerHeight;
            if (bottomBanner != null)
            {
                RectTransform brt = bottomBanner.rectTransform;
                if (brt.sizeDelta.y > 1f)
                    bannerH = brt.sizeDelta.y;
            }

            if (cardBackground != null)
            {
                RectTransform bg = cardBackground.rectTransform;
                bg.anchorMin = Vector2.zero;
                bg.anchorMax = Vector2.one;
                bg.offsetMin = new Vector2(border, border);
                bg.offsetMax = new Vector2(-border, -border);
            }

            if (iconImage != null)
            {
                RectTransform irt = iconImage.rectTransform;
                irt.anchorMin = Vector2.zero;
                irt.anchorMax = Vector2.one;
                irt.offsetMin = new Vector2(border, bannerH);
                irt.offsetMax = new Vector2(-border, -border);
            }
        }

        private void ApplyRoleLabel(CharacterData data, OwnedCharacter owned)
        {
            if (roleLabel == null)
                return;

            SpecializationData spec = data.GetSpecialization(owned.GetSpecialization());
            CharacterRole role = spec != null ? spec.Role : data.Role;

            roleLabel.gameObject.SetActive(true);
            roleLabel.text = RoleShortCode(role);
            roleLabel.fontSize = UiTypography.Caption;
            roleLabel.fontStyle = FontStyles.Bold;
            roleLabel.color = RolePalette.GetColor(role);
            roleLabel.alignment = TextAlignmentOptions.MidlineRight;
        }

        private void ApplyInTeamChrome(bool inTeam)
        {
            if (inTeamIndicator != null)
                inTeamIndicator.SetActive(inTeam);

            if (inTeamStrip != null)
            {
                inTeamStrip.color = UiTheme.AccentAmber;
                inTeamStrip.enabled = inTeam;
            }

            if (inTeamCheck != null)
            {
                inTeamCheck.color = UiTheme.AccentAmber;
                inTeamCheck.gameObject.SetActive(inTeam);
            }
        }

        private static string RoleShortCode(CharacterRole role) => role switch
        {
            CharacterRole.Attacker => "ATK",
            CharacterRole.Defender => "DEF",
            CharacterRole.Support => "SUP",
            _ => "—"
        };
    }
}
