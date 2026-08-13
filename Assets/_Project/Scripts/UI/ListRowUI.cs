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
            float n = Mathf.Clamp01(normalized);
            if (hpBar != null)
            {
                hpBar.gameObject.SetActive(false);
                hpBar.value = n;
            }

            // Fill sous Mid/HpBar (factory RUI1).
            Transform mid = transform.Find("Mid");
            Transform hpTrack = mid != null ? mid.Find("HpBar") : null;
            Transform fill = hpTrack != null ? hpTrack.Find("Fill") : null;
            if (fill is RectTransform fillRt)
            {
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = new Vector2(n, 1f);
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
                if (hpTrack != null)
                    hpTrack.gameObject.SetActive(true);
            }

            if (hpText != null)
            {
                hpText.gameObject.SetActive(!string.IsNullOrEmpty(label));
                hpText.text = label ?? string.Empty;
            }
        }
    }
}
