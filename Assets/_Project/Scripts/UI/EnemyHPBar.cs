using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Barre de vie world space pour ennemi.
    /// </summary>
    public class EnemyHPBar : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public void SetFill(float ratio)
        {
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(ratio);
        }

        public void SetWidth(float width)
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null) return;

            Vector2 size = rt.sizeDelta;
            size.x = width;
            rt.sizeDelta = size;

            // Pas de changement de couleur — on garde le sprite
            // rouge de l'artiste tel quel
        }
    }
}
