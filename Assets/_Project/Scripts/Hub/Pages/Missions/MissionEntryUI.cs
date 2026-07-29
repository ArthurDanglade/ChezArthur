using System;
using ChezArthur.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub.Pages.Missions
{
    /// <summary>
    /// Carte mission lisible — lignes horizontales, icône Tals, carte entière cliquable si réclamable.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MissionEntryUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float ClaimedAlpha = 0.55f;
        private const string ProgressFormat = "{0}/{1}";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Surface")]
        [SerializeField] private PanelSurface surface;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button cardButton;

        [Header("Textes")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI stateLabel;
        [SerializeField] private TextMeshProUGUI actionHint;

        [Header("Progression")]
        [SerializeField] private Image progressFill;
        [SerializeField] private RectTransform progressTrack;

        [Header("Reward")]
        [SerializeField] private Image talsIcon;
        [SerializeField] private GameObject rewardRow;

        [Header("États")]
        [SerializeField] private GameObject checkmark;
        [SerializeField] private GameObject lockIcon;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private string _missionId;
        private Action<string> _onClaimClicked;
        private bool _claimable;
        private int _boundRewardTals;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Dernière récompense bindée (pour FX claim). </summary>
        public int BoundRewardTals => _boundRewardTals;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Bind une entrée. Tap carte → claim uniquement si RÉCLAMABLE.
        /// </summary>
        public void Bind(in MissionUiEntry entry, Action<string> onClaim)
        {
            _missionId = entry.Id;
            _onClaimClicked = onClaim;
            _claimable = entry.State == MissionUiState.Claimable;
            _boundRewardTals = entry.RewardTals;

            if (titleText != null)
            {
                titleText.text = entry.Label ?? string.Empty;
                titleText.enableWordWrapping = true;
                titleText.overflowMode = TextOverflowModes.Ellipsis;
            }

            int target = Mathf.Max(1, entry.Target);
            int progress = Mathf.Clamp(entry.Progress, 0, target);

            if (progressText != null)
            {
                progressText.text = string.Format(ProgressFormat, progress, target);
                progressText.enableWordWrapping = false;
            }

            bool hasReward = entry.RewardTals > 0;
            if (rewardRow != null)
                rewardRow.SetActive(hasReward);
            if (rewardText != null)
                rewardText.text = hasReward ? $"+{entry.RewardTals}" : string.Empty;

            EnsureTalsIcon();

            ApplyProgressFill((float)progress / target);
            ApplyState(entry.State, entry.IsLayerBonus);
        }

        /// <summary> Force le sprite Tals2 cohérent (header + missions). </summary>
        public void SetTalsSprite(Sprite sprite)
        {
            if (talsIcon == null || sprite == null)
                return;
            talsIcon.sprite = sprite;
            talsIcon.preserveAspect = true;
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            WireCardButton(true);
        }

        private void OnDestroy()
        {
            WireCardButton(false);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureTalsIcon()
        {
            if (talsIcon == null)
                return;

            // Si déjà un vrai sprite Tals*, on garde ; sinon le builder aura assigné Tals2.
            if (talsIcon.sprite != null && talsIcon.sprite.name.StartsWith("Tals"))
                return;
        }

        private void WireCardButton(bool add)
        {
            if (cardButton == null)
                cardButton = GetComponent<Button>();
            if (cardButton == null)
                return;

            cardButton.onClick.RemoveListener(OnCardClicked);
            if (add)
                cardButton.onClick.AddListener(OnCardClicked);
        }

        private void ApplyProgressFill(float ratio)
        {
            if (progressFill == null)
                return;

            ratio = Mathf.Clamp01(ratio);
            RectTransform fillRt = progressFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(ratio, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.pivot = new Vector2(0f, 0.5f);
        }

        private void ApplyState(MissionUiState state, bool isLayerBonus)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = state == MissionUiState.Claimed ? ClaimedAlpha : 1f;
                canvasGroup.blocksRaycasts = state != MissionUiState.Locked;
            }

            if (surface != null)
            {
                if (isLayerBonus && state != MissionUiState.Claimable)
                    surface.BorderStyle = PanelSurface.SurfaceBorder.AccentAmber;
                else if (state == MissionUiState.Claimable)
                    surface.BorderStyle = PanelSurface.SurfaceBorder.AccentGold;
                else
                    surface.BorderStyle = PanelSurface.SurfaceBorder.Subtle;
                surface.ApplyStyle();
            }

            bool showProgress = state == MissionUiState.InProgress || state == MissionUiState.Claimable;
            if (progressTrack != null)
                progressTrack.gameObject.SetActive(showProgress);
            if (progressText != null)
                progressText.gameObject.SetActive(showProgress);

            if (checkmark != null)
                checkmark.SetActive(state == MissionUiState.Claimed);
            if (lockIcon != null)
                lockIcon.SetActive(state == MissionUiState.Locked);

            if (stateLabel != null)
            {
                stateLabel.enableWordWrapping = false;
                switch (state)
                {
                    case MissionUiState.InProgress:
                        stateLabel.text = "EN COURS";
                        stateLabel.color = UiTheme.TextSecondary;
                        break;
                    case MissionUiState.Claimable:
                        stateLabel.text = "RÉCLAMABLE";
                        stateLabel.color = UiTheme.AccentGold;
                        break;
                    case MissionUiState.Claimed:
                        stateLabel.text = "RÉCLAMÉE";
                        stateLabel.color = UiTheme.Success;
                        break;
                    case MissionUiState.Locked:
                        stateLabel.text = "VERROUILLÉE";
                        stateLabel.color = UiTheme.TextMuted;
                        break;
                }
            }

            if (actionHint != null)
            {
                bool showHint = state == MissionUiState.Claimable;
                actionHint.gameObject.SetActive(showHint);
                if (showHint)
                {
                    actionHint.text = "Réclamer";
                    actionHint.color = UiTheme.AccentGold;
                }
            }

            if (cardButton != null)
            {
                cardButton.interactable = _claimable;
                ColorBlock cb = cardButton.colors;
                cb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
                cb.disabledColor = Color.white;
                cb.normalColor = Color.white;
                cb.highlightedColor = Color.white;
                cb.selectedColor = Color.white;
                cardButton.colors = cb;
            }

            EnsureRootOnlyRaycast();
        }

        /// <summary>
        /// Seul le Graphic racine reçoit les taps — Fill PanelSurface exclus.
        /// </summary>
        private void EnsureRootOnlyRaycast()
        {
            Graphic rootGraphic = GetComponent<Graphic>();
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null)
                    continue;
                graphics[i].raycastTarget = graphics[i] == rootGraphic;
            }
        }

        private void OnCardClicked()
        {
            if (!_claimable || string.IsNullOrEmpty(_missionId) || _onClaimClicked == null)
                return;
            _onClaimClicked.Invoke(_missionId);
        }
    }
}
