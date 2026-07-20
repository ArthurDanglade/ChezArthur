using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Affiche une entrée de passif dans la carte détaillée.
    /// </summary>
    public class PassiveEntryUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("UI Éléments")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI levelLabelText;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Image unlockIcon;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image accentBorder;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image levelChipBackground;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Color _roleAccent = UiTheme.RoleNeutral;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Mémorise l'accent de rôle (liseré gauche). </summary>
        public void SetRoleAccent(Color accent)
        {
            _roleAccent = accent;
        }

        /// <summary>
        /// Configure l'entrée pour un passif normal.
        /// </summary>
        public void Setup(PassiveData passive, string levelLabel, bool unlocked)
        {
            if (passive == null) return;

            if (nameText != null)
                nameText.text = passive.PassiveName;

            if (descriptionText != null)
                descriptionText.text = passive.GetFormattedDescription();

            if (levelLabelText != null)
                levelLabelText.text = levelLabel;

            ApplyEntryBackground();
            SetUnlockedState(unlocked);
        }

        /// <summary>
        /// Configure l'entrée pour un groupe de passifs partageant le même niveau de déblocage.
        /// </summary>
        public void Setup(List<PassiveData> passives, string levelLabel, bool unlocked)
        {
            if (passives == null || passives.Count == 0)
                return;

            if (nameText != null)
                nameText.text = passives[0].PassiveName;

            if (descriptionText != null)
            {
                if (passives.Count == 1)
                {
                    descriptionText.text = passives[0].GetFormattedDescription();
                }
                else
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < passives.Count; i++)
                    {
                        if (i > 0)
                            sb.Append('\n');
                        sb.Append(passives[i].GetFormattedDescription());
                    }
                    descriptionText.text = sb.ToString();
                }
            }

            if (levelLabelText != null)
                levelLabelText.text = levelLabel;

            ApplyEntryBackground();
            SetUnlockedState(unlocked);
        }

        /// <summary>
        /// Configure l'entrée pour la spécialisation.
        /// </summary>
        public void SetupAsSpecialization(int requiredLevel, bool available, string options)
        {
            if (nameText != null)
                nameText.text = "Spécialisation";

            if (descriptionText != null)
                descriptionText.text = available ? "Choisir : " + options : "Déblocage au Nv. " + requiredLevel.ToString();

            if (levelLabelText != null)
                levelLabelText.text = "Nv. " + requiredLevel.ToString();

            ApplyEntryBackground();
            SetUnlockedState(available);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyEntryBackground()
        {
            if (backgroundImage != null)
                backgroundImage.color = UiTheme.CardPanelEntry;
        }

        private void SetUnlockedState(bool unlocked)
        {
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(!unlocked);

            if (unlockIcon != null)
                unlockIcon.gameObject.SetActive(unlocked);

            if (canvasGroup != null)
                canvasGroup.alpha = unlocked ? 1f : 0.75f;

            if (accentBorder != null)
                accentBorder.color = unlocked ? _roleAccent : UiTheme.CardBorderMuted;

            if (levelChipBackground != null)
            {
                Color chip = unlocked ? _roleAccent : UiTheme.CardBorderMuted;
                chip.a = unlocked ? 0.35f : 0.5f;
                levelChipBackground.color = chip;
            }

            if (unlockIcon != null)
            {
                unlockIcon.color = unlocked ? _roleAccent : UiTheme.TextMuted;
            }

            if (lockIcon != null)
                lockIcon.color = UiTheme.TextMuted;

            if (nameText != null)
                nameText.color = unlocked ? UiTheme.TextPrimary : UiTheme.TextMuted;

            if (descriptionText != null)
                descriptionText.color = unlocked ? UiTheme.TextSecondary : UiTheme.TextMuted;

            if (levelLabelText != null)
                levelLabelText.color = unlocked ? _roleAccent : UiTheme.TextMuted;
        }
    }
}
