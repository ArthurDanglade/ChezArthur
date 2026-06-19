using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Caméra responsive "fit-contain" : calcule l'orthographicSize pour que la zone jouable
    /// (Arena) soit toujours entièrement visible, quel que soit le ratio d'écran. La largeur est
    /// remplie en priorité ; l'espace vertical en plus (écrans hauts) est laissé au décor.
    /// Ne touche QUE l'orthographicSize — la position reste au Camera Shake.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ResponsiveArenaCamera : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Arena arena;

        [Header("Marge (optionnel)")]
        [Tooltip("Marge autour de la zone jouable, en unités monde (0 = la zone touche les bords).")]
        [SerializeField] private float padding = 0f;

        private Camera _camera;
        private CameraShake _cameraShake;
        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _cameraShake = GetComponent<CameraShake>();
        }

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            // Recalcule seulement si la résolution/aspect a changé (test éditeur, rotation).
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
                Apply();
        }

        /// <summary> Recalcule et applique l'orthographicSize pour contenir toute la zone jouable. </summary>
        public void Apply()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (arena == null || _camera == null) return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            // Référence verticale = base caméra (hors tremblement).
            float camY = _cameraShake != null ? _cameraShake.BasePosition.y : transform.position.y;

            Bounds b = arena.Bounds;
            float aspect = (float)Screen.width / Screen.height;

            // Hauteur : montrer le bord le plus éloigné du centre caméra (arène décalable).
            float topExtent = (b.max.y + padding) - camY;
            float bottomExtent = camY - (b.min.y - padding);
            float sizeForHeight = Mathf.Max(topExtent, bottomExtent);

            // Largeur : remplie en priorité (arène centrée en x).
            float sizeForWidth = (b.size.x * 0.5f + padding) / aspect;

            _camera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                Apply();
        }
#endif
    }
}
