using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Ajuste un RectTransform à la zone sûre de l'écran (encoches, home indicator).
    /// À placer sur un enfant plein écran ; cale le contenu dans Screen.safeArea.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _rt;
        private Rect _lastSafeArea;
        private Vector2Int _lastResolution;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            // Réapplique seulement si l'écran change (rotation, resize éditeur).
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastResolution.x ||
                Screen.height != _lastResolution.y)
            {
                Apply();
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void Apply()
        {
            if (_rt == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safe = Screen.safeArea;
            _lastSafeArea = safe;
            _lastResolution = new Vector2Int(Screen.width, Screen.height);

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Garde-fou contre des valeurs aberrantes.
            if (anchorMin.x < 0f || anchorMin.y < 0f || anchorMax.x > 1f || anchorMax.y > 1f)
                return;

            _rt.anchorMin = anchorMin;
            _rt.anchorMax = anchorMax;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
