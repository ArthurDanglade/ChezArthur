using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Chip récompense Tals (devise unique).
    /// </summary>
    [DisallowMultipleComponent]
    public class RewardChipUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Image background;

        public void Bind(Image icon, TextMeshProUGUI amount, Image bg)
        {
            iconImage = icon;
            amountText = amount;
            background = bg;
        }

        public void SetAmount(int amount)
        {
            if (amountText != null)
                amountText.text = amount.ToString();
        }

        public void SetIcon(Sprite sprite)
        {
            if (iconImage == null)
                return;
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }
    }
}
