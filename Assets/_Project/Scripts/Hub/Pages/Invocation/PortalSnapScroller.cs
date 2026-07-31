using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Snap aimanté sur un ScrollRect (Gate 6.a). Contenu-agnostique.
    /// Vertical (6.a) ou horizontal. Max ±1 cellule par geste.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class PortalSnapScroller : MonoBehaviour,
        IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        // ═══════════════════════════════════════════
        // ENUMS
        // ═══════════════════════════════════════════
        public enum SnapAxis
        {
            Vertical = 0,
            Horizontal = 1
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Orientation")]
        [SerializeField] private SnapAxis axis = SnapAxis.Vertical;

        [Header("Seuils")]
        [Tooltip("Fraction de la taille cellule pour basculer ±1.")]
        [SerializeField] private float pageThreshold = 0.2f;
        [SerializeField] private float velocityThreshold = 800f;

        [Header("Snap")]
        [SerializeField] private float snapDuration = 0.25f;
        [SerializeField] private float overshootPx = 7f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private ScrollRect _scroll;
        private RectTransform _content;
        private RectTransform _viewport;
        private bool _dragging;
        private bool _snapping;
        private int _currentIndex;
        private int _cellCount;
        private float _cellSize;
        private float _snapFrom;
        private float _snapTo;
        private float _snapElapsed;
        private float _overshootSign;
        private int _indexAtDragStart;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS / EVENTS
        // ═══════════════════════════════════════════
        public int CurrentIndex => _currentIndex;
        public int CellCount => _cellCount;

        public event Action<int> OnSnapped;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _scroll = GetComponent<ScrollRect>();
            _content = _scroll.content;
            _viewport = _scroll.viewport != null
                ? _scroll.viewport
                : (RectTransform)_scroll.transform;

            _scroll.inertia = false;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            if (axis == SnapAxis.Vertical)
            {
                _scroll.horizontal = false;
                _scroll.vertical = true;
            }
            else
            {
                _scroll.horizontal = true;
                _scroll.vertical = false;
            }
        }

        private void OnEnable()
        {
            RecalculateMetrics();
            SnapImmediate(_currentIndex);
        }

        private void LateUpdate()
        {
            if (_dragging || !_snapping)
                return;

            _snapElapsed += Time.unscaledDeltaTime;
            float t = snapDuration > 0.0001f
                ? Mathf.Clamp01(_snapElapsed / snapDuration)
                : 1f;

            // Ease-out cubic + vague d'overshoot.
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float overshoot = 0f;
            if (overshootPx > 0f && t < 1f)
            {
                float wave = Mathf.Sin(t * Mathf.PI);
                overshoot = _overshootSign * overshootPx * wave * (1f - t);
            }

            float pos = Mathf.LerpUnclamped(_snapFrom, _snapTo, eased) + overshoot;
            SetContentPos(pos, softClamp: true);

            if (t >= 1f)
            {
                SetContentPos(_snapTo, softClamp: false);
                _snapping = false;
                OnSnapped?.Invoke(_currentIndex);
            }
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary> Recalcule taille cellule / count depuis les enfants actifs du Content. </summary>
        public void RecalculateMetrics()
        {
            if (_content == null)
                return;

            _cellCount = 0;
            RectTransform first = null;
            for (int i = 0; i < _content.childCount; i++)
            {
                Transform ch = _content.GetChild(i);
                if (!ch.gameObject.activeSelf)
                    continue;
                _cellCount++;
                if (first == null)
                    first = ch as RectTransform;
            }

            if (_cellCount <= 0)
            {
                _cellSize = 1f;
                return;
            }

            float spacing = 0f;
            if (axis == SnapAxis.Vertical)
            {
                VerticalLayoutGroup vlg = _content.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                    spacing = vlg.spacing;
            }
            else
            {
                HorizontalLayoutGroup hlg = _content.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                    spacing = hlg.spacing;
            }

            if (first != null)
            {
                _cellSize = axis == SnapAxis.Vertical
                    ? first.rect.height
                    : first.rect.width;
            }

            if (_cellSize < 1f && _viewport != null)
            {
                _cellSize = axis == SnapAxis.Vertical
                    ? Mathf.Max(1f, _viewport.rect.height * 0.62f)
                    : Mathf.Max(1f, _viewport.rect.width * 0.62f);
            }

            // Pas de spacing VLG dans le pas : le padding centre est hors step.
            // Le spacing entre cartes fait partie du déplacement index → index+1.
            _cellSize += spacing;
            if (_cellSize < 1f)
                _cellSize = 1f;
        }

        /// <summary> Va à l'index (clamp), avec animation snap. </summary>
        public void GoTo(int index)
        {
            RecalculateMetrics();
            int from = _currentIndex;
            int clamped = ClampIndex(index);
            _overshootSign = clamped >= from ? 1f : -1f;
            BeginSnap(clamped);
        }

        public void SnapImmediate(int index)
        {
            RecalculateMetrics();
            _currentIndex = ClampIndex(index);
            _snapping = false;
            SetContentPos(IndexToPos(_currentIndex), softClamp: false);
        }

        // ═══════════════════════════════════════════
        // DRAG
        // ═══════════════════════════════════════════

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _snapping = false;
            RecalculateMetrics();
            _indexAtDragStart = PosToIndex(GetContentPos());
        }

        public void OnDrag(PointerEventData eventData)
        {
            // ScrollRect déplace le contenu.
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            RecalculateMetrics();

            float pos = GetContentPos();
            // Vertical Unity : y augmente quand le contenu monte (index suivant).
            float signedVel = axis == SnapAxis.Vertical
                ? _scroll.velocity.y
                : -_scroll.velocity.x;

            int nearest = PosToIndex(pos);
            float cellOrigin = IndexToPos(nearest);
            float delta = pos - cellOrigin;
            float normalized = _cellSize > 0.0001f ? delta / _cellSize : 0f;

            int target = nearest;
            bool byVelocity = Mathf.Abs(signedVel) > velocityThreshold;
            bool byDistance = Mathf.Abs(normalized) > pageThreshold;

            if (byVelocity)
                target = signedVel > 0f ? nearest + 1 : nearest - 1;
            else if (byDistance)
            {
                // Vertical : pos+ → index+ ; Horizontal : pos- → index+.
                float towardNext = axis == SnapAxis.Horizontal ? -normalized : normalized;
                target = towardNext > 0f ? nearest + 1 : nearest - 1;
            }

            // Jamais plus de ±1 depuis l'index au début du geste.
            target = Mathf.Clamp(target, _indexAtDragStart - 1, _indexAtDragStart + 1);
            target = ClampIndex(target);

            _overshootSign = target >= nearest ? 1f : -1f;
            BeginSnap(target);
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private void BeginSnap(int index)
        {
            _currentIndex = ClampIndex(index);
            _snapFrom = GetContentPos();
            _snapTo = IndexToPos(_currentIndex);
            _snapElapsed = 0f;
            _snapping = true;
            _scroll.StopMovement();
            _scroll.velocity = Vector2.zero;
        }

        private int ClampIndex(int index)
        {
            if (_cellCount <= 0)
                return 0;
            return Mathf.Clamp(index, 0, _cellCount - 1);
        }

        private float IndexToPos(int index)
        {
            // Vertical : y positif vers le bas du contenu.
            // Horizontal : x negatif vers la droite du contenu (ancrage gauche Unity).
            float signed = index * _cellSize;
            return axis == SnapAxis.Horizontal ? -signed : signed;
        }

        private int PosToIndex(float pos)
        {
            if (_cellSize < 0.0001f || _cellCount <= 0)
                return 0;
            float raw = axis == SnapAxis.Horizontal ? -pos / _cellSize : pos / _cellSize;
            return ClampIndex(Mathf.RoundToInt(raw));
        }

        private float GetContentPos()
        {
            if (_content == null)
                return 0f;
            return axis == SnapAxis.Vertical
                ? _content.anchoredPosition.y
                : _content.anchoredPosition.x;
        }

        private void SetContentPos(float pos, bool softClamp)
        {
            if (_content == null)
                return;

            float a = IndexToPos(0);
            float b = IndexToPos(_cellCount > 0 ? _cellCount - 1 : 0);
            float min = Mathf.Min(a, b);
            float max = Mathf.Max(a, b);
            if (softClamp && overshootPx > 0f)
                pos = Mathf.Clamp(pos, min - overshootPx, max + overshootPx);
            else
                pos = Mathf.Clamp(pos, min, max);

            Vector2 p = _content.anchoredPosition;
            if (axis == SnapAxis.Vertical)
                p.y = pos;
            else
                p.x = pos;
            _content.anchoredPosition = p;
        }
    }
}
