using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;

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

        [Header("Couleurs")]
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

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

            SetUnlockedState(unlocked);
        }

        /// <summary>
        /// Configure l'entrée pour un groupe de passifs partageant le même niveau de déblocage.
        /// </summary>
        public void Setup(List<PassiveData> passives, string levelLabel, bool unlocked)
        {
            if (passives == null || passives.Count == 0)
                return;

            // Nom : un seul passif ou plusieurs → nom du premier.
            if (nameText != null)
                nameText.text = passives[0].PassiveName;

            // Description : toutes les descriptions séparées par un saut de ligne.
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

            SetUnlockedState(available);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void SetUnlockedState(bool unlocked)
        {
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(!unlocked);

            if (unlockIcon != null)
                unlockIcon.gameObject.SetActive(unlocked);

            if (canvasGroup != null)
                canvasGroup.alpha = unlocked ? 1f : 0.6f;

            if (nameText != null)
                nameText.color = unlocked ? unlockedColor : lockedColor;

            if (descriptionText != null)
                descriptionText.color = unlocked ? unlockedColor : lockedColor;
        }
    }
}
