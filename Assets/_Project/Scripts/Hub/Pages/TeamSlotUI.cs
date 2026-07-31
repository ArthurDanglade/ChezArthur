using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Slot d'équipe (Gate 5.a/5.b) — maintien pour retirer ; tap court → popup.
    /// </summary>
    public class TeamSlotUI : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float RoleBorderPx = 4f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Cadre")]
        [SerializeField] private Image roleFrame;
        [SerializeField] private RectTransform innerContent;
        [SerializeField] private PanelSurface panelSurface;

        [Header("UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI emptyPlusText;
        [SerializeField] private Button slotButton;
        [SerializeField] private CanvasGroup canvasGroup;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private string _characterId;
        private bool _isEmpty = true;
        private int _uiSlotIndex = -1;
        private Color _restFrameColor = Color.white;
        private TeamDragController _dragController;
        private CharacterData _currentData;
        private OwnedCharacter _currentOwned;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public bool IsEmpty => _isEmpty;
        public string CharacterId => _characterId;
        public int UiSlotIndex => _uiSlotIndex;
        public CharacterData CurrentData => _currentData;
        public OwnedCharacter CurrentOwned => _currentOwned;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (slotButton != null)
            {
                slotButton.onClick.RemoveAllListeners();
                slotButton.transition = Selectable.Transition.None;
                slotButton.enabled = false;
            }

            ApplyInnerInset();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void BindDragController(TeamDragController controller)
        {
            _dragController = controller;
        }

        public void SetUiSlotIndex(int index)
        {
            _uiSlotIndex = index;
        }

        public void SetCharacter(CharacterData data, OwnedCharacter owned)
        {
            if (data == null || owned == null)
            {
                SetEmpty();
                return;
            }

            _characterId = owned.characterId;
            _currentData = data;
            _currentOwned = owned;
            _isEmpty = false;
            EnsureButtonBlocksRaycasts();

            SpecializationData spec = data.GetSpecialization(owned.GetSpecialization());
            CharacterRole role = spec != null ? spec.Role : data.Role;
            _restFrameColor = RolePalette.GetColor(role);
            if (roleFrame != null)
                roleFrame.color = _restFrameColor;

            if (panelSurface != null)
                panelSurface.ApplyStyle();

            if (iconImage != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = data.Icon != null;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
            }

            if (levelText != null)
            {
                levelText.gameObject.SetActive(true);
                levelText.text = "Nv." + owned.level.ToString();
                levelText.fontSize = UiTypography.Caption;
                levelText.color = UiTheme.TextPrimary;
            }

            if (emptyPlusText != null)
                emptyPlusText.gameObject.SetActive(false);

            SetSourceDimmed(false);
        }

        public void SetEmpty()
        {
            _characterId = null;
            _currentData = null;
            _currentOwned = null;
            _isEmpty = true;
            EnsureButtonBlocksRaycasts();

            _restFrameColor = UiTheme.BorderSubtle;
            if (roleFrame != null)
                roleFrame.color = _restFrameColor;

            if (panelSurface != null)
                panelSurface.ApplyStyle();

            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }

            if (levelText != null)
                levelText.gameObject.SetActive(false);

            if (emptyPlusText != null)
            {
                emptyPlusText.gameObject.SetActive(true);
                emptyPlusText.text = "+";
                emptyPlusText.fontSize = UiTypography.Display;
                Color muted = UiTheme.TextMuted;
                muted.a = 0.5f;
                emptyPlusText.color = muted;
                emptyPlusText.alignment = TextAlignmentOptions.Center;
            }

            SetSourceDimmed(false);
        }

        public Color GetFrameColor()
        {
            return roleFrame != null ? roleFrame.color : _restFrameColor;
        }

        public void BeginPotentialDrag() { }

        public void EndPotentialDrag() { }

        public void SetSourceDimmed(bool dimmed)
        {
            if (canvasGroup == null)
                return;
            canvasGroup.alpha = dimmed ? 0.45f : 1f;
        }

        public void SetDragHover(bool hovered, Vector3 scale)
        {
            transform.localScale = scale;
            if (roleFrame == null)
                return;

            if (hovered)
            {
                Color c = UiTheme.AccentAmber;
                c.a = 0.9f;
                roleFrame.color = c;
            }
            else
            {
                roleFrame.color = _restFrameColor;
            }
        }

        public void SetDragPulseAlpha(float alpha)
        {
            if (roleFrame == null)
                return;
            Color c = UiTheme.AccentAmber;
            c.a = Mathf.Clamp01(alpha);
            roleFrame.color = c;
        }

        public void ClearDragVisuals(Color restColor, Vector3 restScale)
        {
            _restFrameColor = restColor;
            if (roleFrame != null)
                roleFrame.color = restColor;
            transform.localScale = restScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _dragController?.NotifySlotPointerDown(this, eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragController?.NotifyPointerUp(eventData);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyInnerInset()
        {
            if (innerContent == null)
                return;

            float inset = RoleBorderPx;
            innerContent.anchorMin = Vector2.zero;
            innerContent.anchorMax = Vector2.one;
            innerContent.offsetMin = new Vector2(inset, inset);
            innerContent.offsetMax = new Vector2(-inset, -inset);
        }

        private void EnsureButtonBlocksRaycasts()
        {
            if (slotButton == null)
                return;
            slotButton.interactable = true;
            if (slotButton.targetGraphic != null)
                slotButton.targetGraphic.raycastTarget = true;
        }
    }
}
