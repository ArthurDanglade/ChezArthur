using System;
using ChezArthur.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub.Pages.Missions
{
    /// <summary>
    /// Carte mission — 4 états visuels (EN COURS / RÉCLAMABLE / RÉCLAMÉE / VERROUILLÉE).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MissionEntryUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float ClaimedAlpha = 0.45f;
        private const string ProgressFormat = "{0}/{1}";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Surface")]
        [SerializeField] private PanelSurface surface;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Textes")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI stateLabel;

        [Header("Progression")]
        [SerializeField] private Image progressFill;
        [SerializeField] private RectTransform progressTrack;

        [Header("Actions / états")]
        [SerializeField] private Button claimButton;
        [SerializeField] private TextMeshProUGUI claimButtonLabel;
        [SerializeField] private GameObject checkmark;
        [SerializeField] private GameObject lockIcon;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private string _missionId;
        private Action<string> _onClaimClicked;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Bind une entrée. <paramref name="onClaim"/> reçoit l'id au tap Réclamer.
        /// </summary>
        public void Bind(in MissionUiEntry entry, Action<string> onClaim)
        {
            _missionId = entry.Id;
            _onClaimClicked = onClaim;

            if (titleText != null)
                titleText.text = entry.Label ?? string.Empty;

            if (rewardText != null)
                rewardText.text = entry.RewardTals > 0 ? $"+{entry.RewardTals} Tals" : string.Empty;

            int target = Mathf.Max(1, entry.Target);
            int progress = Mathf.Clamp(entry.Progress, 0, target);

            if (progressText != null)
                progressText.text = string.Format(ProgressFormat, progress, target);

            if (progressFill != null && progressTrack != null)
            {
                float ratio = (float)progress / target;
                Vector2 size = progressTrack.rect.size;
                progressFill.rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    size.x * ratio);
            }

            ApplyState(entry.State);
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(OnClaimClicked);
                claimButton.onClick.AddListener(OnClaimClicked);
            }
        }

        private void OnDestroy()
        {
            if (claimButton != null)
                claimButton.onClick.RemoveListener(OnClaimClicked);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyState(MissionUiState state)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = state == MissionUiState.Claimed ? ClaimedAlpha : 1f;
                canvasGroup.interactable = state != MissionUiState.Locked;
                canvasGroup.blocksRaycasts = state != MissionUiState.Locked;
            }

            if (surface != null)
            {
                surface.BorderStyle = state == MissionUiState.Claimable
                    ? PanelSurface.SurfaceBorder.AccentGold
                    : PanelSurface.SurfaceBorder.Subtle;
                surface.ApplyStyle();
            }

            bool showClaim = state == MissionUiState.Claimable;
            bool showCheck = state == MissionUiState.Claimed;
            bool showLock = state == MissionUiState.Locked;
            bool showProgress = state == MissionUiState.InProgress || state == MissionUiState.Claimable;

            if (claimButton != null)
                claimButton.gameObject.SetActive(showClaim);
            if (checkmark != null)
                checkmark.SetActive(showCheck);
            if (lockIcon != null)
                lockIcon.SetActive(showLock);
            if (progressTrack != null)
                progressTrack.gameObject.SetActive(showProgress);
            if (progressText != null)
                progressText.gameObject.SetActive(showProgress);

            if (stateLabel != null)
            {
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

            if (claimButtonLabel != null)
                claimButtonLabel.text = "Réclamer";
        }

        private void OnClaimClicked()
        {
            if (string.IsNullOrEmpty(_missionId) || _onClaimClicked == null)
                return;
            _onClaimClicked.Invoke(_missionId);
        }
    }
}
