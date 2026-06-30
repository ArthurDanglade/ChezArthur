using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChezArthur.UI
{
    /// <summary>
    /// Une pastille de comparaison : [icône optionnelle] libellé + valeur (mode stat),
    /// ou texte plein largeur (mode effet). La couleur est pilotée par l'appelant.
    /// L'icône est optionnelle et reste désactivée tant qu'aucune n'est fournie (icon-ready).
    /// </summary>
    public class StatLineUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image iconImage; // optionnel : icône de stat (attaque up, def up…)

        /// <summary> Pastille de stat : libellé + valeur, même couleur. Icône optionnelle. </summary>
        public void ShowStat(string label, string value, Color color, Sprite icon = null)
        {
            gameObject.SetActive(true);
            if (labelText != null) { labelText.text = label; labelText.color = color; }
            if (valueText != null) { valueText.gameObject.SetActive(true); valueText.text = value; valueText.color = color; }
            ApplyIcon(icon, color);
        }

        /// <summary> Pastille d'effet : phrase plein largeur, valeur masquée. </summary>
        public void ShowEffect(string text, Color color)
        {
            gameObject.SetActive(true);
            if (labelText != null) { labelText.text = text; labelText.color = color; }
            if (valueText != null) valueText.gameObject.SetActive(false);
            ApplyIcon(null, color);
        }

        /// <summary> Masque la pastille. </summary>
        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Affiche l'icône si fournie (teintée comme le texte), sinon désactive son GameObject
        /// pour qu'elle n'occupe aucune place dans le layout de la pastille.
        /// </summary>
        private void ApplyIcon(Sprite icon, Color color)
        {
            if (iconImage == null) return;
            bool has = icon != null;
            iconImage.gameObject.SetActive(has);
            if (has) { iconImage.sprite = icon; iconImage.color = color; }
        }
    }
}
