using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Zoom aléatoire de la caméra orthographique (Close / Normal / Far).
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

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyMode(CameraMode mode)
        {
            if (targetCamera == null) return;
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
        }
    }
}
