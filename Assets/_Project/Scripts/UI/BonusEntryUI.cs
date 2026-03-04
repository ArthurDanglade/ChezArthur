using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche une ligne de bonus (icône, nom, effet, stack).
    /// </summary>
    public class BonusEntryUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI effectText;
        [SerializeField] private TextMeshProUGUI stackText;

        /// <summary>
        /// Remplit l'entrée avec les données du bonus.
        /// </summary>
        public void Setup(BonusData bonus, int stackCount)
        {
            if (bonus == null) return;

            if (iconImage != null && bonus.Icon != null)
                iconImage.sprite = bonus.Icon;

            if (nameText != null)
                nameText.text = bonus.BonusName;

            if (effectText != null)
                effectText.text = bonus.Description;

            if (stackText != null)
            {
                stackText.text = stackCount > 1 ? $"x{stackCount}" : "";
                stackText.gameObject.SetActive(stackCount > 1);
            }
        }
    }
}
