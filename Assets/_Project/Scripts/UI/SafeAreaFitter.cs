using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Cale un RectTransform sur la safe area de l'appareil (encoches, barre gestuelle).
    /// À poser sur un conteneur plein écran : les enfants ancrés dedans héritent de la zone sûre.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Bords à conformer (décocher pour ignorer un bord)")]
        [SerializeField] private bool conformTop = true;
        [SerializeField] private bool conformBottom = true;
        [SerializeField] private bool conformLeft = true;
        [SerializeField] private bool conformRight = true;

        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreen.x ||
                Screen.height != _lastScreen.y)
                Apply();
        }

        private void Apply()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();

            int w = Screen.width;
            int h = Screen.height;
            if (w <= 0 || h <= 0) return;

            Rect safe = Screen.safeArea;
            _lastSafeArea = safe;
            _lastScreen = new Vector2Int(w, h);

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;

            if (!conformLeft) min.x = 0f;
            if (!conformBottom) min.y = 0f;
            if (!conformRight) max.x = w;
            if (!conformTop) max.y = h;

            min.x /= w; min.y /= h;
            max.x /= w; max.y /= h;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
