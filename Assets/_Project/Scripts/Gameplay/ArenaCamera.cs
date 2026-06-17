using System.Collections;
using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Zoom aléatoire de la caméra orthographique (Close / Normal / Far) et finisher cinématique de fin d'étage.
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
        [SerializeField] private CameraShake _cameraShake;

        [Header("Tailles caméra")]
        [SerializeField] private float sizeClose = 6f;
        [SerializeField] private float sizeNormal = 9f;
        [SerializeField] private float sizeFar = 13f;

        [Header("Probabilités")]
        [SerializeField] [Range(0f, 1f)] private float chanceClose = 0.25f;
        [SerializeField] [Range(0f, 1f)] private float chanceFar = 0.25f;

        [Header("Zoom finisher")]
        [SerializeField] private float _finisherZoomFactor = 0.82f;
        [SerializeField] private float _finisherPanAmount = 0.5f;
        [SerializeField] private float _finisherLateralNudge = 0.5f;
        [SerializeField] private float _finisherTiltDegrees = 4f;
        [SerializeField] private float _finisherInDuration = 0.5f;
        [SerializeField] private float _finisherHoldDuration = 0.15f;
        [SerializeField] private float _finisherOutDuration = 0.5f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _appliedOrthoSize;
        private Coroutine _finisherZoomRoutine;
        private System.Action _finisherZoomComplete;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (_cameraShake == null)
                _cameraShake = GetComponent<CameraShake>();
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
        /// Glissé lent vers le kill : zoom, pan décentré et dutch tilt (temps réel).
        /// </summary>
        public void PlayFinisherZoom(Vector3 killWorldPos, System.Action onComplete = null)
        {
            if (targetCamera == null)
            {
                onComplete?.Invoke();
                return;
            }
            if (_finisherZoomRoutine != null) StopCoroutine(_finisherZoomRoutine);
            _finisherZoomComplete = onComplete;
            _finisherZoomRoutine = StartCoroutine(FinisherZoomRoutine(killWorldPos));
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
                _cameraShake?.SetCinematic(Vector2.zero, 0f);
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

        private IEnumerator FinisherZoomRoutine(Vector3 killWorldPos)
        {
            float baseOrtho = _appliedOrthoSize > 0f ? _appliedOrthoSize : targetCamera.orthographicSize;
            float zoomedOrtho = baseOrtho * _finisherZoomFactor;

            Vector3 center = _cameraShake != null ? _cameraShake.BasePosition : transform.localPosition;
            Vector2 toKill = new Vector2(killWorldPos.x - center.x, killWorldPos.y - center.y);
            Vector2 panTarget = toKill * _finisherPanAmount;
            Vector2 perp = toKill.sqrMagnitude > 0.001f
                ? new Vector2(-toKill.y, toKill.x).normalized
                : Vector2.right;
            panTarget += perp * (_finisherLateralNudge * (Random.value < 0.5f ? -1f : 1f));
            float tiltTarget = _finisherTiltDegrees * (Random.value < 0.5f ? -1f : 1f);

            float t = 0f;
            while (t < _finisherInDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / _finisherInDuration);
                k = k * k * (3f - 2f * k);
                targetCamera.orthographicSize = Mathf.Lerp(baseOrtho, zoomedOrtho, k);
                _cameraShake?.SetCinematic(panTarget * k, tiltTarget * k);
                yield return null;
            }

            float h = 0f;
            while (h < _finisherHoldDuration)
            {
                h += Time.unscaledDeltaTime;
                targetCamera.orthographicSize = zoomedOrtho;
                _cameraShake?.SetCinematic(panTarget, tiltTarget);
                yield return null;
            }

            t = 0f;
            while (t < _finisherOutDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / _finisherOutDuration);
                k = k * k * (3f - 2f * k);
                targetCamera.orthographicSize = Mathf.Lerp(zoomedOrtho, baseOrtho, k);
                _cameraShake?.SetCinematic(Vector2.Lerp(panTarget, Vector2.zero, k), Mathf.Lerp(tiltTarget, 0f, k));
                yield return null;
            }

            targetCamera.orthographicSize = baseOrtho;
            _cameraShake?.SetCinematic(Vector2.zero, 0f);
            _finisherZoomRoutine = null;
            _finisherZoomComplete?.Invoke();
            _finisherZoomComplete = null;
        }
    }
}
