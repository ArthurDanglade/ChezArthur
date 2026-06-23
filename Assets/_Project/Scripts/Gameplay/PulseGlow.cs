using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Fait « respirer » un halo (alpha + échelle) avec une oscillation douce et un léger
    /// scintillement. Pensé pour les lueurs de torches : la phase est aléatoire pour que
    /// plusieurs halos ne pulsent pas en synchro.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PulseGlow : MonoBehaviour
    {
        [Header("Pulsation")]
        [Tooltip("Alpha moyen du halo.")]
        [SerializeField] private float baseAlpha = 0.55f;

        [Tooltip("Amplitude de variation de l'alpha.")]
        [SerializeField] private float alphaAmplitude = 0.2f;

        [Tooltip("Amplitude de variation de l'échelle (0.08 = ±8%).")]
        [SerializeField] private float scaleAmplitude = 0.08f;

        [Tooltip("Vitesse de la pulsation.")]
        [SerializeField] private float speed = 2.5f;

        [Tooltip("Petit scintillement organique en plus de la pulsation (0 = pulsation pure).")]
        [SerializeField] private float flicker = 0.15f;

        [Tooltip("Décale la phase au démarrage (désynchronise plusieurs torches).")]
        [SerializeField] private bool randomizePhase = true;

        private SpriteRenderer _renderer;
        private Vector3 _baseScale;
        private float _phase;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            if (randomizePhase)
                _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            // Oscillation douce (sinus) + scintillement organique (Perlin) → un feu vivant.
            float wave = Mathf.Sin(Time.time * speed + _phase);
            float noise = (Mathf.PerlinNoise(Time.time * speed * 1.7f + _phase, 0f) - 0.5f) * 2f;
            float t = wave + noise * flicker;

            Color c = _renderer.color;
            c.a = Mathf.Clamp01(baseAlpha + alphaAmplitude * t);
            _renderer.color = c;

            transform.localScale = _baseScale * (1f + scaleAmplitude * t);
        }
    }
}
