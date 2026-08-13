using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Cellule de stat (label + valeur, accent couleur).
    /// </summary>
    [DisallowMultipleComponent]
    public class StatCellUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image background;

        public void Bind(TextMeshProUGUI label, TextMeshProUGUI value, Image bg)
        {
            labelText = label;
            valueText = value;
            background = bg;
        }

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

            if (background != null)
            {
                Color fill = accent;
                fill.a = 0.22f;
                background.color = fill;
            }
        }
    }
}
