using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche un item actif (icône, nom, description).
    /// </summary>
    public class ItemEntryUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;

        public void Setup(ItemInstance item)
        {
            if (item == null || item.Data == null) return;
            var data = item.Data;

            if (nameText != null) nameText.text = data.ItemName;
            if (descText != null) descText.text = data.GetFormattedDescription();

            // Icône réelle si dispo, sinon on garde le placeholder du prefab.
            if (iconImage != null && data.Icon != null)
                iconImage.sprite = data.Icon;
        }
    }
}
