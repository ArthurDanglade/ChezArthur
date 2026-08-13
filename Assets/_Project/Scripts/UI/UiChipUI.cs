using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Chip générique (type / état / synergie).
    /// </summary>
    [DisallowMultipleComponent]
    public class UiChipUI : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI labelText;

        public void Bind(Image bg, TextMeshProUGUI label)
        {
            background = bg;
            labelText = label;
        }

        public void Set(string text, Color bg, Color fg)
        {
            if (labelText != null)
            {
                labelText.text = text ?? string.Empty;
                labelText.color = fg;
            }

            if (background != null)
                background.color = bg;
        }
    }
}
