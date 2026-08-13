using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Ligne de liste (avatar / nom / meta) — prefab runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public class ListRowUI : MonoBehaviour
    {
        [SerializeField] private Image avatarFrame;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI metaText;
        [SerializeField] private Slider hpBar;
        [SerializeField] private TextMeshProUGUI hpText;

        public void Bind(
            Image frame,
            Image avatar,
            TextMeshProUGUI name,
            TextMeshProUGUI meta,
            Slider hp,
            TextMeshProUGUI hpLabel)
        {
            avatarFrame = frame;
            avatarImage = avatar;
            nameText = name;
            metaText = meta;
            hpBar = hp;
            hpText = hpLabel;
        }

        public void SetName(string value)
        {
            if (nameText != null)
                nameText.text = value ?? string.Empty;
        }

        public void SetMeta(string value)
        {
            if (metaText == null)
                return;
            bool has = !string.IsNullOrEmpty(value);
            metaText.gameObject.SetActive(has);
            if (has)
                metaText.text = value;
        }

        public void SetFrameColor(Color color)
        {
            if (avatarFrame != null)
                avatarFrame.color = color;
        }

        public void SetHp(float normalized, string label)
        {
            if (hpBar != null)
            {
                hpBar.gameObject.SetActive(true);
                hpBar.value = Mathf.Clamp01(normalized);
            }

            if (hpText != null)
            {
                hpText.gameObject.SetActive(!string.IsNullOrEmpty(label));
                hpText.text = label ?? string.Empty;
            }
        }
    }
}
