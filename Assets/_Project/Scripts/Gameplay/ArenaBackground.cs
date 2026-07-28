using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Fond d'arène : SpriteRenderer + couleur placeholder par univers.
    /// </summary>
    public class ArenaBackground : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private SpriteRenderer backgroundRenderer;

        [Header("Couleurs placeholder par univers")]
        [SerializeField] private Color colorUnivers1 =
            new Color(0.102f, 0.082f, 0.180f); // Ardacula — gothique
        [SerializeField] private Color colorUnivers2 =
            new Color(0.196f, 0.165f, 0.125f); // L'Ancien — arts martiaux
        [SerializeField] private Color colorUnivers3 =
            new Color(0.094f, 0.110f, 0.133f); // Don Costardo — super-héros absurdes
        [SerializeField] private Color colorUnivers4 =
            new Color(0.157f, 0.137f, 0.094f); // Faille — laboratoire
        [SerializeField] private Color colorUnivers5 =
            new Color(0.176f, 0.063f, 0.051f); // Troplin — dragon / gobelin

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Met à jour la couleur du fond selon le numéro d'univers (1 à 5).
        /// </summary>
        public void SetUnivers(int universNumber)
        {
            if (backgroundRenderer == null)
            {
                Debug.LogWarning("[ArenaBackground] backgroundRenderer non assigné.", this);
                return;
            }

            Color c = universNumber switch
            {
                1 => colorUnivers1,
                2 => colorUnivers2,
                3 => colorUnivers3,
                4 => colorUnivers4,
                5 => colorUnivers5,
                _ => colorUnivers1
            };
            backgroundRenderer.color = c;
        }

        /// <summary>
        /// Adapte la taille du SpriteRenderer aux bounds de l'arène.
        /// Le sprite remplit exactement la zone jouable.
        /// </summary>
        public void FitToBounds(Bounds bounds)
        {
            if (backgroundRenderer == null)
                return;

            backgroundRenderer.transform.position =
                new Vector3(bounds.center.x, bounds.center.y, 1f);

            if (backgroundRenderer.sprite != null)
            {
                Vector2 spriteSize = backgroundRenderer.sprite.bounds.size;
                float sx = spriteSize.x > 0f ? spriteSize.x : 1f;
                float sy = spriteSize.y > 0f ? spriteSize.y : 1f;
                float scaleX = bounds.size.x / sx;
                float scaleY = bounds.size.y / sy;
                backgroundRenderer.transform.localScale =
                    new Vector3(scaleX, scaleY, 1f);
            }
            else
            {
                // Quad / sprite par défaut : une unité monde ≈ 1 côté
                backgroundRenderer.transform.localScale =
                    new Vector3(bounds.size.x, bounds.size.y, 1f);
            }
        }
    }
}
