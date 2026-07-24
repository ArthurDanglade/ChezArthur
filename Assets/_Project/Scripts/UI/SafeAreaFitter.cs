using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Cale un RectTransform enfant direct du Canvas sur la safe area appareil
    /// (encoche, Dynamic Island, barre gestuelle Android/iOS).
    /// Les enfants ancrés dedans héritent automatiquement de la zone sûre.
    /// Coût nul en régime permanent : réapplique uniquement si safeArea ou résolution change.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Bords à conformer")]
        [Tooltip("Décoche un bord pour l’ignorer (ex. barre nav collée au bas physique).")]
        [SerializeField] private bool conformTop = true;
        [SerializeField] private bool conformBottom = true;
        [SerializeField] private bool conformLeft = true;
        [SerializeField] private bool conformRight = true;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private bool _hasApplied;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            CacheRect();
        }

        private void OnEnable()
        {
            Apply(force: true);
        }

        private void Update()
        {
            // Comparaison Rect / ints — aucune allocation.
            Rect safe = Screen.safeArea;
            int w = Screen.width;
            int h = Screen.height;
            if (_hasApplied
                && safe == _lastSafeArea
                && w == _lastScreenWidth
                && h == _lastScreenHeight)
                return;

            Apply(force: false, safe, w, h);
        }

        /// <summary>
        /// Certains appareils Android recalculent la safe area au retour foreground.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                Apply(force: true);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Force un recalcul immédiat (outils éditeur / tests).
        /// </summary>
        public void Refresh()
        {
            Apply(force: true);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void CacheRect()
        {
            if (_rect == null)
                _rect = (RectTransform)transform;
        }

        private void Apply(bool force)
        {
            Apply(force, Screen.safeArea, Screen.width, Screen.height);
        }

        private void Apply(bool force, Rect safe, int screenW, int screenH)
        {
            CacheRect();
            if (_rect == null)
                return;

            if (screenW <= 0 || screenH <= 0)
                return;

            if (!force
                && _hasApplied
                && safe == _lastSafeArea
                && screenW == _lastScreenWidth
                && screenH == _lastScreenHeight)
                return;

            _lastSafeArea = safe;
            _lastScreenWidth = screenW;
            _lastScreenHeight = screenH;
            _hasApplied = true;

            // Coordonnées écran → ancres normalisées [0,1] relatives au parent (Canvas plein écran).
            float xMin = safe.xMin;
            float yMin = safe.yMin;
            float xMax = safe.xMax;
            float yMax = safe.yMax;

            if (!conformLeft) xMin = 0f;
            if (!conformBottom) yMin = 0f;
            if (!conformRight) xMax = screenW;
            if (!conformTop) yMax = screenH;

            // Clamp défensif (drivers OEM parfois hors bornes).
            xMin = Mathf.Clamp(xMin, 0f, screenW);
            yMin = Mathf.Clamp(yMin, 0f, screenH);
            xMax = Mathf.Clamp(xMax, 0f, screenW);
            yMax = Mathf.Clamp(yMax, 0f, screenH);
            if (xMax < xMin) xMax = xMin;
            if (yMax < yMin) yMax = yMin;

            _rect.anchorMin = new Vector2(xMin / screenW, yMin / screenH);
            _rect.anchorMax = new Vector2(xMax / screenW, yMax / screenH);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
