using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Cellule de stat — fond neutre, étiquette colorée, valeur blanche (F4).
    /// </summary>
    [DisallowMultipleComponent]
    public class StatCellUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image borderImage;

        public void Bind(TextMeshProUGUI label, TextMeshProUGUI value, Image fill, Image border)
        {
            labelText = label;
            valueText = value;
            fillImage = fill;
            borderImage = border;
        }

        /// <summary>
        /// accent = couleur d'étiquette uniquement ; fond reste BgElevated.
        /// </summary>
        public void Set(string label, string value, Color accent)
        {
            if (labelText != null)
            {
                labelText.text = label ?? string.Empty;
                labelText.color = accent;
            }

            if (valueText != null)
            {
                valueText.text = value ?? string.Empty;
                valueText.color = UiTheme.TextPrimary;
            }

            if (fillImage != null)
                fillImage.color = UiTheme.BgElevated;

            if (borderImage != null)
                borderImage.color = UiTheme.BorderSubtle;
        }
    }
}
