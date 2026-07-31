using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Flèches + dots branchés sur PortalSnapScroller (Gate 6.a).
    /// </summary>
    public class PortalSnapChrome : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private PortalSnapScroller scroller;
        [SerializeField] private Button arrowUp;
        [SerializeField] private Button arrowDown;
        [SerializeField] private Transform dotContainer;
        [SerializeField] private bool logSnaps = true;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Image[] _dots;
        private Color _dotOn;
        private Color _dotOff;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _dotOn = new Color(1f, 1f, 1f, 0.95f);
            _dotOff = new Color(1f, 1f, 1f, 0.28f);
            CacheDots();
        }

        private void OnEnable()
        {
            if (scroller != null)
                scroller.OnSnapped += HandleSnapped;

            if (arrowUp != null)
            {
                arrowUp.onClick.RemoveListener(OnArrowUp);
                arrowUp.onClick.AddListener(OnArrowUp);
            }

            if (arrowDown != null)
            {
                arrowDown.onClick.RemoveListener(OnArrowDown);
                arrowDown.onClick.AddListener(OnArrowDown);
            }

            RefreshChrome(scroller != null ? scroller.CurrentIndex : 0);
        }

        private void OnDisable()
        {
            if (scroller != null)
                scroller.OnSnapped -= HandleSnapped;

            if (arrowUp != null)
                arrowUp.onClick.RemoveListener(OnArrowUp);
            if (arrowDown != null)
                arrowDown.onClick.RemoveListener(OnArrowDown);
        }

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        public void Bind(
            PortalSnapScroller snap,
            Button up,
            Button down,
            Transform dots)
        {
            scroller = snap;
            arrowUp = up;
            arrowDown = down;
            dotContainer = dots;
            CacheDots();
            RefreshChrome(scroller != null ? scroller.CurrentIndex : 0);
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private void HandleSnapped(int index)
        {
            if (logSnaps)
                Debug.Log($"[PortalSnap] OnSnapped → index {index}", this);
            RefreshChrome(index);
        }

        private void OnArrowUp()
        {
            if (scroller == null)
                return;
            scroller.GoTo(scroller.CurrentIndex - 1);
        }

        private void OnArrowDown()
        {
            if (scroller == null)
                return;
            scroller.GoTo(scroller.CurrentIndex + 1);
        }

        private void CacheDots()
        {
            if (dotContainer == null)
            {
                _dots = System.Array.Empty<Image>();
                return;
            }

            int n = dotContainer.childCount;
            _dots = new Image[n];
            for (int i = 0; i < n; i++)
                _dots[i] = dotContainer.GetChild(i).GetComponent<Image>();
        }

        private void RefreshChrome(int index)
        {
            CacheDots();
            for (int i = 0; i < _dots.Length; i++)
            {
                if (_dots[i] == null)
                    continue;
                _dots[i].color = i == index ? _dotOn : _dotOff;
            }

            int count = scroller != null ? scroller.CellCount : _dots.Length;
            if (arrowUp != null)
                arrowUp.interactable = index > 0;
            if (arrowDown != null)
                arrowDown.interactable = count <= 0 || index < count - 1;
        }
    }
}
