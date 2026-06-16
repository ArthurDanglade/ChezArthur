using System.Collections;
using TMPro;
using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Anime un nombre flottant en espace monde (pop, arc, fondu), puis détruit l'objet.
    /// </summary>
    public class FloatingNumber : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private TMP_Text label;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float lifetime = 0.8f;
        [SerializeField] private float fadeDuration = 0.6f;

        [Header("Pop")]
        [SerializeField] private float popPunch = 1.35f;
        [SerializeField] private float critPopPunch = 1.7f;
        [SerializeField] private float popDuration = 0.1f;
        [SerializeField] private float settleDuration = 0.12f;

        [Header("Arc")]
        [SerializeField] private float horizontalDrift = 0.8f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Vector3 _displayScale = Vector3.one;
        private bool _isCrit;
        private Vector3 _arcVelocity;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Initialise le texte flottant puis lance son animation.
        /// </summary>
        public void Initialize(string text, Color color, float scale = 1f, bool isCrit = false)
        {
            if (label == null) return;

            label.text = text;
            label.color = color;
            // Même référence d'échelle qu'avant le juice (Vector3.one * scale), pas le 0.01 du prefab Canvas.
            _displayScale = Vector3.one * scale;
            _isCrit = isCrit;
            _arcVelocity = new Vector3(Random.Range(-horizontalDrift, horizontalDrift), floatSpeed, 0f);
            transform.localScale = Vector3.zero;
            StartCoroutine(AnimateAndDestroy());
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private IEnumerator AnimateAndDestroy()
        {
            float punch = _isCrit ? critPopPunch : popPunch;
            float peakMul = punch;
            float fadeStart = lifetime - fadeDuration;
            Color baseColor = label.color;
            float t = 0f;

            while (t < lifetime)
            {
                t += Time.deltaTime;
                float mul = t < popDuration
                    ? Mathf.Lerp(0f, peakMul, t / popDuration)
                    : Mathf.Lerp(peakMul, 1f, Mathf.Clamp01((t - popDuration) / settleDuration));
                transform.localScale = _displayScale * mul;

                float decel = 1f - Mathf.Clamp01(t / lifetime) * 0.6f;
                transform.position += _arcVelocity * (Time.deltaTime * decel);

                if (t > fadeStart)
                {
                    float a = 1f - Mathf.Clamp01((t - fadeStart) / fadeDuration);
                    Color c = baseColor;
                    c.a = a;
                    label.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
