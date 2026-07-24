using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Remonte le Header au bord haut physique de l'écran.
    /// Prérequis : SafeAreaFitter.conformTop = false (SafeRoot jusqu'en haut).
    /// Visuels (backdrop + circuit) remplissent tout le header y compris l'encoche.
    /// Pills uniquement dans la bande safe — aucun clic dans la zone unsafe.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HubHeaderSafeBleed : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Pills — zone safe uniquement")]
        [Tooltip("PillIdentity / PillStage / PillTals (pas le circuit).")]
        [SerializeField] private RectTransform[] safeBandContent;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _headerRt;
        private Canvas _canvas;
        private SafeAreaFitter _safeAreaFitter;
        private float _lastBleed = float.MinValue;
        private int _lastScreenW;
        private int _lastScreenH;
        private Rect _lastSafeArea;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Cache();
            EnsureSafeRootReachesPhysicalTop();
            EnsureVisualsNonClickable();
        }

        private void OnEnable()
        {
            EnsureSafeRootReachesPhysicalTop();
            Apply(force: true);
        }

        private void LateUpdate()
        {
            EnsureSafeRootReachesPhysicalTop();

            Rect safe = ScreenSafeArea.SafeArea;
            int w = ScreenSafeArea.Width;
            int h = ScreenSafeArea.Height;
            if (safe == _lastSafeArea && w == _lastScreenW && h == _lastScreenH)
                return;

            Apply(force: false, safe, w, h);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                Apply(force: true);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Recalcule immédiatement (éditeur / builder). </summary>
        public void Refresh()
        {
            EnsureSafeRootReachesPhysicalTop();
            Apply(force: true);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Cache()
        {
            if (_headerRt == null)
                _headerRt = (RectTransform)transform;
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();
            if (_safeAreaFitter == null && _headerRt != null && _headerRt.parent != null)
                _safeAreaFitter = _headerRt.parent.GetComponent<SafeAreaFitter>();
        }

        /// <summary>
        /// SafeRoot doit aller jusqu'au bord haut physique — sinon rien ne peut
        /// « remonter » dans l'encoche (bande caméra / fond).
        /// </summary>
        private void EnsureSafeRootReachesPhysicalTop()
        {
            Cache();
            if (_safeAreaFitter == null)
                return;
            if (_safeAreaFitter.ConformTop)
                _safeAreaFitter.ConformTop = false;
        }

        private void Apply(bool force)
        {
            Apply(force, ScreenSafeArea.SafeArea, ScreenSafeArea.Width, ScreenSafeArea.Height);
        }

        private void Apply(bool force, Rect safe, int screenW, int screenH)
        {
            Cache();
            if (_headerRt == null || screenW <= 0 || screenH <= 0)
                return;

            float bleed = ComputeTopBleedCanvasUnits(safe, screenW, screenH);
            if (!force
                && Mathf.Approximately(bleed, _lastBleed)
                && safe == _lastSafeArea
                && screenW == _lastScreenW
                && screenH == _lastScreenH)
                return;

            _lastBleed = bleed;
            _lastSafeArea = safe;
            _lastScreenW = screenW;
            _lastScreenH = screenH;

            // Collé au haut physique de SafeRoot (= haut écran si conformTop=false).
            _headerRt.anchorMin = new Vector2(0f, 1f);
            _headerRt.anchorMax = new Vector2(1f, 1f);
            _headerRt.pivot = new Vector2(0.5f, 1f);
            _headerRt.anchoredPosition = Vector2.zero;
            _headerRt.sizeDelta = new Vector2(0f, UiTheme.HeaderHeight + bleed);

            // Backdrop + circuit : stretch plein header (visuel dans l'encoche OK).
            StretchVisualsFullHeader();

            // Pills : centrées dans la bande safe (sous l'encoche), pas dans l'unsafe.
            float contentY = -bleed * 0.5f;
            if (safeBandContent == null)
                return;

            for (int i = 0; i < safeBandContent.Length; i++)
            {
                RectTransform rt = safeBandContent[i];
                if (rt == null)
                    continue;
                if (rt.name != "PillIdentity"
                    && rt.name != "PillStage"
                    && rt.name != "PillTals")
                    continue;

                Vector2 pos = rt.anchoredPosition;
                pos.y = contentY;
                rt.anchoredPosition = pos;
            }
        }

        private void StretchVisualsFullHeader()
        {
            for (int i = 0; i < _headerRt.childCount; i++)
            {
                Transform child = _headerRt.GetChild(i);
                if (child.name != "HeaderBackdrop" && child.name != "CircuitBackdrop")
                    continue;

                RectTransform rt = child as RectTransform;
                if (rt == null)
                    continue;

                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;

                Image img = child.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = false;
                    if (child.name == "CircuitBackdrop")
                    {
                        img.type = Image.Type.Simple;
                        img.preserveAspect = false;
                    }
                }
            }
        }

        private void EnsureVisualsNonClickable()
        {
            Cache();
            if (_headerRt == null)
                return;

            for (int i = 0; i < _headerRt.childCount; i++)
            {
                Transform child = _headerRt.GetChild(i);
                if (child.name != "HeaderBackdrop"
                    && child.name != "CircuitBackdrop"
                    && child.name != "BottomHairline")
                    continue;

                Image img = child.GetComponent<Image>();
                if (img != null)
                    img.raycastTarget = false;
            }
        }

        private float ComputeTopBleedCanvasUnits(Rect safe, int screenW, int screenH)
        {
            float unsafeTopPx = Mathf.Max(0f, screenH - safe.yMax);
            if (unsafeTopPx <= 0.5f)
                return 0f;

            float scale = 1f;
            if (_canvas != null && _canvas.scaleFactor > 0.0001f)
                scale = _canvas.scaleFactor;

            return unsafeTopPx / scale;
        }
    }
}
