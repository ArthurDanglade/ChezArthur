using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Pose TopUtilityRow juste sous le bas réel du Header (bleed inclus),
    /// avec une marge (gap) pour ne pas coller aux pills.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [DefaultExecutionOrder(50)] // Après HubHeaderSafeBleed.
    public class TopUtilityHeaderClearance : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private RectTransform header;

        [Header("Marge sous header")]
        [SerializeField] private float gap = UiTheme.Space3;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _rowRt;
        private float _lastInset = float.MinValue;
        private float _lastGap = float.MinValue;

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

        public void BindHeader(RectTransform headerRt)
        {
            header = headerRt;
            Apply(force: true);
        }

        public void Refresh()
        {
            Apply(force: true);
        }

        /// <summary>
        /// Inset depuis le bord haut du parent jusqu'au bas du Header.
        /// (Le bleed agrandit height et remonte posY → l'inset utile reste ≈ HeaderHeight.)
        /// </summary>
        public static float ResolveHeaderBottomInset(RectTransform header)
        {
            if (header == null)
                return UiTheme.HeaderHeight;

            float height = header.rect.height;
            if (height < 1f)
                height = header.sizeDelta.y;
            if (height < 1f)
                return UiTheme.HeaderHeight;

            // Ancres haut + pivot haut : posY positif = déborde au-dessus du parent.
            float topEdge = header.anchoredPosition.y;
            float inset = height - topEdge;
            if (inset < UiTheme.HeaderHeight * 0.5f)
                inset = Mathf.Max(height, UiTheme.HeaderHeight);
            return inset;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Cache()
        {
            if (_rowRt == null)
                _rowRt = (RectTransform)transform;
        }

        private void Apply(bool force)
        {
            Cache();
            if (_rowRt == null)
                return;

            float inset = ResolveHeaderBottomInset(header);
            if (!force
                && Mathf.Approximately(inset, _lastInset)
                && Mathf.Approximately(gap, _lastGap))
                return;

            _lastInset = inset;
            _lastGap = gap;

            _rowRt.anchorMin = new Vector2(0f, 1f);
            _rowRt.anchorMax = new Vector2(1f, 1f);
            _rowRt.pivot = new Vector2(0.5f, 1f);
            _rowRt.anchoredPosition = new Vector2(0f, -(inset + Mathf.Max(0f, gap)));
        }
    }
}
