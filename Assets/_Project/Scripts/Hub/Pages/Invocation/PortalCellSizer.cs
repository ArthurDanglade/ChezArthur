using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Dimensionne les PortalCard (~88 % viewport) + padding pour centrer la carte active.
    /// </summary>
    [DisallowMultipleComponent]
    public class PortalCellSizer : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private float heightFraction = 0.62f;
        [SerializeField] private float spacingFraction = 0.06f;
        [SerializeField] private bool centerInViewport = true;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _lastH = float.MinValue;

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

        public void Bind(RectTransform vp, RectTransform ct)
        {
            viewport = vp;
            content = ct;
            Apply(force: true);
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private void Apply(bool force)
        {
            if (viewport == null || content == null)
                return;

            float h = viewport.rect.height;
            if (!force && Mathf.Approximately(h, _lastH))
                return;
            if (h < 1f)
                return;

            _lastH = h;
            float cellH = h * Mathf.Clamp(heightFraction, 0.4f, 0.95f);
            float spacing = h * Mathf.Clamp01(spacingFraction);
            float pad = 0f;
            if (centerInViewport)
                pad = Mathf.Max(0f, (h - cellH) * 0.5f);

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = spacing;
                int padI = Mathf.RoundToInt(pad);
                vlg.padding = new RectOffset(0, 0, padI, padI);
            }

            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform child = content.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                    continue;

                LayoutElement le = child.GetComponent<LayoutElement>();
                if (le == null)
                    le = child.gameObject.AddComponent<LayoutElement>();

                le.preferredHeight = cellH;
                le.minHeight = cellH;
                le.flexibleHeight = 0f;

                child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cellH);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            PortalSnapScroller snap = GetComponent<PortalSnapScroller>();
            if (snap != null)
            {
                snap.RecalculateMetrics();
                snap.SnapImmediate(snap.CurrentIndex);
            }
        }
    }
}
