using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche une valise active (icône placeholder, nom, niveau, description).
    /// </summary>
    public class ValiseEntryUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI descText;

        public void Setup(ValiseInstance valise)
        {
            if (valise == null || valise.Data == null) return;
            var data = valise.Data;

            if (nameText != null)  nameText.text  = data.ValiseName;
            if (levelText != null) levelText.text = "Niv. " + valise.CurrentLevel;
            if (descText != null)  descText.text  = data.GetFormattedDescription();
            // iconImage : placeholder pour l'instant (vraies icônes plus tard).
        }
    }
}
