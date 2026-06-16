using System.Collections;
using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Zoom aléatoire de la caméra orthographique (Close / Normal / Far) et zoom finisher de fin d'étage.
    /// L'arène physique reste à taille fixe.
    /// </summary>
    public class ArenaCamera : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // TYPES PRIVÉS
        // ═══════════════════════════════════════════
        private enum CameraMode
        {
            Close,
            Normal,
            Far
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private Camera targetCamera;

        [Header("Tailles caméra")]
        [SerializeField] private float sizeClose = 6f;
        [SerializeField] private float sizeNormal = 9f;
        [SerializeField] private float sizeFar = 13f;

        [Header("Probabilités")]
        [SerializeField] [Range(0f, 1f)] private float chanceClose = 0.25f;
        [SerializeField] [Range(0f, 1f)] private float chanceFar = 0.25f;

        [Header("Zoom finisher")]
        [SerializeField] private float _finisherZoomFactor = 0.7f;
        [SerializeField] private float _finisherZoomInDuration = 0.25f;
        [SerializeField] private float _finisherZoomHold = 0.2f;
        [SerializeField] private float _finisherZoomOutDuration = 0.45f;
        [SerializeField] private float _finisherZoomOvershoot = 1.7f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _appliedOrthoSize;
        private Coroutine _finisherZoomRoutine;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Tire aléatoirement un mode caméra et l'applique.
        /// </summary>
        public void ApplyRandomSize()
        {
            float roll = Random.value;
            CameraMode mode;
            if (roll < chanceClose)
                mode = CameraMode.Close;
            else if (roll < chanceClose + chanceFar)
                mode = CameraMode.Far;
            else
                mode = CameraMode.Normal;

            ApplyMode(mode);
        }

        /// <summary>
        /// Zoom IN/OUT sur l'ortho size (temps réel) pour la fin d'étage.
        /// </summary>
        public void PlayFinisherZoom()
        {
            if (targetCamera == null) return;
            if (_finisherZoomRoutine != null) StopCoroutine(_finisherZoomRoutine);
            _finisherZoomRoutine = StartCoroutine(FinisherZoomRoutine());
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyMode(CameraMode mode)
        {
            if (targetCamera == null) return;
            if (_finisherZoomRoutine != null)
            {
                StopCoroutine(_finisherZoomRoutine);
                _finisherZoomRoutine = null;
            }

            switch (mode)
            {
                case CameraMode.Close:
                    targetCamera.orthographicSize = sizeClose;
                    break;
                case CameraMode.Far:
                    targetCamera.orthographicSize = sizeFar;
                    break;
                default:
                    targetCamera.orthographicSize = sizeNormal;
                    break;
            }

            _appliedOrthoSize = targetCamera.orthographicSize;
        }

        private IEnumerator FinisherZoomRoutine()
        {
            float baseOrtho = _appliedOrthoSize > 0f ? _appliedOrthoSize : targetCamera.orthographicSize;
            float zoomedOrtho = baseOrtho * (_finisherZoomFactor * Random.Range(0.92f, 1.08f));
            float inDur = _finisherZoomInDuration * Random.Range(0.9f, 1.1f);
            float hold = _finisherZoomHold * Random.Range(0.9f, 1.1f);
            float outDur = _finisherZoomOutDuration * Random.Range(0.9f, 1.1f);

            float t = 0f;
            while (t < inDur)
            {
                t += Time.unscaledDeltaTime;
                float k = EaseOutBack(Mathf.Clamp01(t / inDur), _finisherZoomOvershoot);
                targetCamera.orthographicSize = Mathf.LerpUnclamped(baseOrtho, zoomedOrtho, k);
                yield return null;
            }

            targetCamera.orthographicSize = zoomedOrtho;
            yield return new WaitForSecondsRealtime(hold);

            t = 0f;
            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / outDur);
                k = k * k * (3f - 2f * k);
                targetCamera.orthographicSize = Mathf.Lerp(zoomedOrtho, baseOrtho, k);
                yield return null;
            }

            targetCamera.orthographicSize = baseOrtho;
            _finisherZoomRoutine = null;
        }

        private static float EaseOutBack(float t, float overshoot)
        {
            float c3 = overshoot + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
        }
    }
}
