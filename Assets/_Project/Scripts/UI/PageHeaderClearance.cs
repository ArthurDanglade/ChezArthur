using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Décale un contenu de page sous le bas réel du Header (+ marge),
    /// et optionnellement au-dessus de la NavigationBar.
    /// Réutilisable (Missions d'abord ; Équipe / Invocation plus tard).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [DefaultExecutionOrder(60)] // Après HubHeaderSafeBleed / HubNavSafeBleed.
    public class PageHeaderClearance : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform navigationBar;

        [Header("Marges (aération)")]
        [SerializeField] private float topGap = UiTheme.Space5;
        [SerializeField] private bool clearNavigation = true;
        [SerializeField] private float bottomGap = UiTheme.Space3;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _rt;
        private float _lastTop = float.MinValue;
        private float _lastBottom = float.MinValue;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Cache();
            ResolveRefsIfNeeded();
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

        public void Bind(RectTransform headerRt, RectTransform navRt)
        {
            header = headerRt;
            navigationBar = navRt;
            Apply(force: true);
        }

        public void Refresh()
        {
            Apply(force: true);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Cache()
        {
            if (_rt == null)
                _rt = (RectTransform)transform;
        }

        private void ResolveRefsIfNeeded()
        {
            Transform root = transform.root;
            if (header == null)
            {
                Transform h = FindDeep(root, "Header");
                if (h != null)
                    header = h as RectTransform;
            }

            if (navigationBar == null)
            {
                Transform n = FindDeep(root, "NavigationBar");
                if (n != null)
                    navigationBar = n as RectTransform;
            }
        }

        private void Apply(bool force)
        {
            Cache();
            if (_rt == null)
                return;

            ResolveRefsIfNeeded();

            float topInset = TopUtilityHeaderClearance.ResolveHeaderBottomInset(header)
                             + Mathf.Max(0f, topGap);
            float bottomInset = 0f;
            if (clearNavigation)
            {
                bottomInset = BottomZoneNavClearance.ResolveNavHeight(navigationBar)
                              + Mathf.Max(0f, bottomGap);
            }

            if (!force
                && Mathf.Approximately(topInset, _lastTop)
                && Mathf.Approximately(bottomInset, _lastBottom))
                return;

            _lastTop = topInset;
            _lastBottom = bottomInset;

            // Contenu stretch dans la page, rogné sous header / au-dessus nav.
            _rt.anchorMin = Vector2.zero;
            _rt.anchorMax = Vector2.one;
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.offsetMin = new Vector2(_rt.offsetMin.x, bottomInset);
            _rt.offsetMax = new Vector2(_rt.offsetMax.x, -topInset);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform f = FindDeep(root.GetChild(i), name);
                if (f != null)
                    return f;
            }

            return null;
        }
    }
}
