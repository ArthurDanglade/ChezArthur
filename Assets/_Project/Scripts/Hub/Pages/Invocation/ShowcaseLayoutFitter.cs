using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Dimensions responsives du showcase Personnages (Gate 6.c.4).
    /// Empile via LayoutElement ; adapte largeur pages + chrome selon le device.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShowcaseLayoutFitter : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float DEFAULT_FEATURED_FLEX = 1.35f;
        private const float SHORT_FEATURED_FLEX = 1.15f;
        private const float TALL_FEATURED_FLEX = 1.45f;
        private const float DEFAULT_POOL_FLEX = 1f;
        private const float SHORT_PANEL_H = 700f;
        private const float TALL_PANEL_H = 1100f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Zones")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private LayoutElement featuredZoneLe;
        [SerializeField] private LayoutElement poolZoneLe;
        [SerializeField] private LayoutElement dotsRowLe;

        [Header("Carousel")]
        [SerializeField] private RectTransform featuredViewport;
        [SerializeField] private RectTransform featuredContent;
        [SerializeField] private PortalSnapScroller featuredSnap;
        [SerializeField] private Button arrowPrev;
        [SerializeField] private Button arrowNext;

        [Header("Clamps")]
        [SerializeField] private float featuredMinHeight = 220f;
        [SerializeField] private float poolMinHeight = 140f;
        [SerializeField] private float dotsRowHeight = 16f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _lastW = float.MinValue;
        private float _lastH = float.MinValue;
        private int _pageCount;
        private bool _dirty = true;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            Apply(force: true);
        }

        private void LateUpdate()
        {
            Apply(force: false);
        }

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        /// <summary>
        /// Nombre de pages vedettes (dots + fleches collapsibles si &lt;= 1).
        /// </summary>
        public void SetPageCount(int count)
        {
            _pageCount = Mathf.Max(0, count);
            _dirty = true;
            Apply(force: true);
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private void Apply(bool force)
        {
            if (panel == null)
                return;

            float w = panel.rect.width;
            float h = panel.rect.height;
            if (!force
                && !_dirty
                && Mathf.Approximately(w, _lastW)
                && Mathf.Approximately(h, _lastH))
                return;

            if (w < 1f || h < 1f)
                return;

            _lastW = w;
            _lastH = h;
            _dirty = false;

            ApplyFlexWeights(h);
            ApplyChromeVisibility();
            ApplyFeaturedPageWidths();

            if (featuredContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(featuredContent);

            if (featuredSnap != null)
            {
                featuredSnap.RecalculateMetrics();
                featuredSnap.SnapImmediate(featuredSnap.CurrentIndex);
            }
        }

        private void ApplyFlexWeights(float panelH)
        {
            float featuredFlex = DEFAULT_FEATURED_FLEX;
            if (panelH < SHORT_PANEL_H)
                featuredFlex = SHORT_FEATURED_FLEX;
            else if (panelH > TALL_PANEL_H)
                featuredFlex = TALL_FEATURED_FLEX;

            if (featuredZoneLe != null)
            {
                featuredZoneLe.flexibleHeight = featuredFlex;
                featuredZoneLe.minHeight = featuredMinHeight;
                featuredZoneLe.preferredHeight = -1f;
            }

            if (poolZoneLe != null)
            {
                poolZoneLe.flexibleHeight = DEFAULT_POOL_FLEX;
                poolZoneLe.minHeight = poolMinHeight;
                poolZoneLe.preferredHeight = -1f;
            }
        }

        private void ApplyChromeVisibility()
        {
            bool multi = _pageCount > 1;

            if (dotsRowLe != null)
            {
                dotsRowLe.preferredHeight = multi ? dotsRowHeight : 0f;
                dotsRowLe.minHeight = multi ? dotsRowHeight : 0f;
                dotsRowLe.flexibleHeight = 0f;
                if (dotsRowLe.gameObject.activeSelf != multi)
                    dotsRowLe.gameObject.SetActive(multi);
            }

            if (arrowPrev != null && arrowPrev.gameObject.activeSelf != multi)
                arrowPrev.gameObject.SetActive(multi);
            if (arrowNext != null && arrowNext.gameObject.activeSelf != multi)
                arrowNext.gameObject.SetActive(multi);
        }

        private void ApplyFeaturedPageWidths()
        {
            if (featuredViewport == null || featuredContent == null)
                return;

            float pageW = featuredViewport.rect.width;
            if (pageW < 1f)
                return;

            for (int i = 0; i < featuredContent.childCount; i++)
            {
                RectTransform child = featuredContent.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                    continue;

                child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pageW);

                LayoutElement le = child.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.preferredWidth = pageW;
                    le.minWidth = pageW;
                    le.flexibleWidth = 0f;
                }

                ShowcaseFeaturedPage page = child.GetComponent<ShowcaseFeaturedPage>();
                if (page != null)
                    page.NotifyLayoutChanged();
            }
        }
    }
}
