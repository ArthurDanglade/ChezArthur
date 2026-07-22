using System;
using UnityEngine;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Courbe de déplacement horizontal du train (gacha).
    /// Le sens de lecture (arrivée = courbe inversée) est décidé par le
    /// TrainSequenceController, pas ici.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TrainCurve",
        menuName = "Chez Arthur/Gacha/Train Curve Data")]
    public class TrainCurveData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Tooltip("Temps (sec) → décalage X (px auteur). Sens = départ du train.")]
        [SerializeField] private AnimationCurve xOffsetByTime = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [Tooltip("Largeur du sprite train en pixels (espace auteur).")]
        [SerializeField] private int spriteWidthPx;

        [Tooltip("Largeur canvas auteur (ex. 1385) — pour conversion UI.")]
        [SerializeField] private int canvasWidthPx = 1385;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        [NonSerialized] private float _cachedDuration = -1f;
        [NonSerialized] private float _cachedMaxOffset = -1f;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Durée totale (temps de la dernière clé), mise en cache. </summary>
        public float Duration
        {
            get
            {
                if (_cachedDuration < 0f)
                    _cachedDuration = ComputeDurationFromLastKey();
                return _cachedDuration;
            }
        }

        /// <summary> Amplitude max |dx| de la courbe (espace auteur). </summary>
        public float MaxOffset
        {
            get
            {
                if (_cachedMaxOffset < 0f)
                    _cachedMaxOffset = ComputeMaxOffset();
                return _cachedMaxOffset;
            }
        }

        public int SpriteWidthPx => spriteWidthPx;
        public int CanvasWidthPx => canvasWidthPx > 0 ? canvasWidthPx : 1385;

        public AnimationCurve XOffsetByTime => xOffsetByTime;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Évalue le décalage X (px auteur) à l'instant t, clampé sur [0, Duration].
        /// </summary>
        public float EvaluateOffset(float t)
        {
            float duration = Duration;
            float clamped = Mathf.Clamp(t, 0f, duration);

            if (xOffsetByTime == null || xOffsetByTime.length == 0)
                return 0f;

            return xOffsetByTime.Evaluate(clamped);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Init éditeur (TrainCurveImporter). Invalide les caches.
        /// </summary>
        public void EditorInitialize(AnimationCurve curve, int widthPx, int canvasWidth = 1385)
        {
            xOffsetByTime = curve != null
                ? new AnimationCurve(curve.keys)
                : AnimationCurve.Linear(0f, 0f, 1f, 0f);
            spriteWidthPx = widthPx;
            canvasWidthPx = canvasWidth > 0 ? canvasWidth : 1385;
            InvalidateCaches();
        }
#endif

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnValidate()
        {
            InvalidateCaches();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void InvalidateCaches()
        {
            _cachedDuration = -1f;
            _cachedMaxOffset = -1f;
        }

        private float ComputeDurationFromLastKey()
        {
            if (xOffsetByTime == null || xOffsetByTime.length == 0)
                return 0f;

            Keyframe[] keys = xOffsetByTime.keys;
            return keys[keys.Length - 1].time;
        }

        private float ComputeMaxOffset()
        {
            if (xOffsetByTime == null || xOffsetByTime.length == 0)
                return 0f;

            float max = 0f;
            Keyframe[] keys = xOffsetByTime.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                float abs = Mathf.Abs(keys[i].value);
                if (abs > max)
                    max = abs;
            }

            return max;
        }
    }
}
