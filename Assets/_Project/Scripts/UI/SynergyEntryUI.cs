using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche une synergie active ou possible dans le menu pause.
    /// </summary>
    public class SynergyEntryUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image backgroundImage;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Remplit l'entrée selon l'état actif / possible.
        /// </summary>
        public void Setup(SynergyData data, bool isActive, string comboLine)
        {
            if (data == null)
                return;

            if (nameText != null)
            {
                nameText.text = data.DisplayName ?? string.Empty;
                nameText.color = isActive ? UiTheme.Gold : UiTheme.TextSecondary;
            }

            if (comboText != null)
            {
                comboText.text = comboLine ?? string.Empty;
                comboText.color = isActive ? UiTheme.TextSecondary : UiTheme.TextMuted;
            }

            if (descriptionText != null)
            {
                descriptionText.text = data.Description ?? string.Empty;
                descriptionText.color = isActive ? UiTheme.TextPrimary : UiTheme.TextSecondary;
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = isActive ? 1f : 0.75f;
        }
    }
}
