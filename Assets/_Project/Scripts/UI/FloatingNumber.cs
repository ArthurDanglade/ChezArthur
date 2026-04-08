using System.Collections;
using TMPro;
using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Anime un nombre flottant en espace monde, puis détruit l'objet.
    /// </summary>
    public class FloatingNumber : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private TMP_Text label;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float fadeDuration = 0.6f;
        [SerializeField] private float lifetime = 0.8f;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Initialise le texte flottant puis lance son animation.
        /// </summary>
        public void Initialize(string text, Color color, float scale = 1f)
        {
            if (label == null) return;

            label.text = text;
            label.color = color;
            transform.localScale = Vector3.one * scale;

            StartCoroutine(AnimateAndDestroy());
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private IEnumerator AnimateAndDestroy()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Color startColor = label.color;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = lifetime > 0f ? elapsed / lifetime : 1f;

                // Déplacement vertical
                transform.position = startPos + Vector3.up * floatSpeed * t;

                // Fade out dans le dernier tiers de la durée
                float fadeStart = 1f - fadeDuration / lifetime;
                if (t > fadeStart)
                {
                    float fadeT = (t - fadeStart) / (fadeDuration / lifetime);
                    label.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
