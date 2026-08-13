using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Chip générique — bordure accent + fill translucide (F5).
    /// </summary>
    [DisallowMultipleComponent]
    public class UiChipUI : MonoBehaviour
    {
        [SerializeField] private Image borderImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI labelText;

        public void Bind(Image border, TextMeshProUGUI label)
        {
            borderImage = border;
            labelText = label;
            // Fill optionnel (enfant "Fill") — résolu à la volée si non câblé.
            if (fillImage == null && border != null)
            {
                Transform t = border.transform.Find("Fill");
                if (t != null)
                    fillImage = t.GetComponent<Image>();
            }
        }

        public void Bind(Image border, Image fill, TextMeshProUGUI label)
        {
            borderImage = border;
            fillImage = fill;
            labelText = label;
        }

        public void Set(string text, Color accent, Color fg)
        {
            if (labelText != null)
            {
                labelText.text = text ?? string.Empty;
                labelText.color = fg;
                labelText.enableWordWrapping = false;
            }

            if (borderImage != null)
                borderImage.color = accent;

            if (fillImage != null)
            {
                Color fill = accent;
                fill.a = 0.28f;
                fillImage.color = fill;
            }
        }
    }
}
