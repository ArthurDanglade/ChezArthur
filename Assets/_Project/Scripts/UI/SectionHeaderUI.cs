using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// En-tête de section (barre ambre + titre + compteur optionnel).
    /// </summary>
    [DisallowMultipleComponent]
    public class SectionHeaderUI : MonoBehaviour
    {
        [SerializeField] private Image accentBar;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI countText;

        public void Bind(Image bar, TextMeshProUGUI title, TextMeshProUGUI count)
        {
            accentBar = bar;
            titleText = title;
            countText = count;
        }

        public void Set(string title, string count = null)
        {
            if (titleText != null)
                titleText.text = title ?? string.Empty;
            if (countText != null)
            {
                bool has = !string.IsNullOrEmpty(count);
                countText.gameObject.SetActive(has);
                if (has)
                    countText.text = count;
            }
        }
    }
}
