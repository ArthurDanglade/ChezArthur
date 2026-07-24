using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Pose BottomZone juste au-dessus de la NavigationBar réelle
    /// (NavHeight + bleed HubNavSafeBleed), pour ne jamais chevaucher le footer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [DefaultExecutionOrder(50)] // Après HubNavSafeBleed (0) dans LateUpdate.
    public class BottomZoneNavClearance : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private RectTransform navigationBar;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _zoneRt;
        private float _lastNavH = float.MinValue;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Cache();
            Apply(force: true);
        }

        private void OnEnable()
        {
            Apply(force: true);
        }

        private void LateUpdate()
        {
            Apply(force: false);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Bind / rebind NavigationBar (builder). </summary>
        public void BindNavigationBar(RectTransform nav)
        {
            navigationBar = nav;
            Apply(force: true);
        }

        public void Refresh()
        {
            Apply(force: true);
        }

        /// <summary>
        /// Hauteur à réserver sous BottomZone (= hauteur rect nav, sinon token).
        /// </summary>
        public static float ResolveNavHeight(RectTransform navigationBar)
        {
            if (navigationBar != null)
            {
                float h = navigationBar.rect.height;
                if (h < 1f)
                    h = navigationBar.sizeDelta.y;
                if (h >= 1f)
                    return h;
            }

            return UiTheme.NavHeight;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Cache()
        {
            if (_zoneRt == null)
                _zoneRt = (RectTransform)transform;
        }

        private void Apply(bool force)
        {
            Cache();
            if (_zoneRt == null)
                return;

            float navH = ResolveNavHeight(navigationBar);
            if (!force && Mathf.Approximately(navH, _lastNavH))
                return;

            _lastNavH = navH;
            _zoneRt.anchorMin = new Vector2(0f, 0f);
            _zoneRt.anchorMax = new Vector2(1f, 0f);
            _zoneRt.pivot = new Vector2(0.5f, 0f);
            _zoneRt.anchoredPosition = new Vector2(_zoneRt.anchoredPosition.x, navH);
        }
    }
}
