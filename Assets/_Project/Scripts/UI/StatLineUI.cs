using UnityEngine;
using TMPro;

namespace ChezArthur.UI
{
    /// <summary>
    /// Une ligne de l'encart de comparaison : libellé à gauche + valeur à droite (mode stat),
    /// ou texte plein largeur (mode effet). La couleur est pilotée par l'appelant.
    /// </summary>
    public class StatLineUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI valueText;

        /// <summary> Ligne de stat : libellé + valeur, même couleur. </summary>
        public void ShowStat(string label, string value, Color color)
        {
            gameObject.SetActive(true);
            if (labelText != null) { labelText.text = label; labelText.color = color; }
            if (valueText != null) { valueText.gameObject.SetActive(true); valueText.text = value; valueText.color = color; }
        }

        /// <summary> Ligne d'effet : phrase plein largeur, valeur masquée. </summary>
        public void ShowEffect(string text, Color color)
        {
            gameObject.SetActive(true);
            if (labelText != null) { labelText.text = text; labelText.color = color; }
            if (valueText != null) valueText.gameObject.SetActive(false);
        }

        /// <summary> Masque la ligne. </summary>
        public void Hide() => gameObject.SetActive(false);
    }
}
