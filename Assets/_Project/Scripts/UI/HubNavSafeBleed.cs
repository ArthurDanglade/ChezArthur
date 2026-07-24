using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Miroir bas de HubHeaderSafeBleed : prolonge la NavigationBar jusqu'au bord
    /// physique inférieur. Prérequis : SafeAreaFitter.conformBottom = false.
    /// Visuels (backdrop + motif) dans la zone unsafe ; onglets dans la bande safe.
    /// N'altère jamais ConformTop (LOCK 2.1).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HubNavSafeBleed : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Onglets — zone safe uniquement")]
        [Tooltip("TabsRow et/ou onglets (pas le motif footer).")]
        [SerializeField] private RectTransform[] safeBandContent;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _navRt;
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
            EnsureSafeRootReachesPhysicalBottom();
            EnsureVisualsRaycastPolicy();
        }

        private void OnEnable()
        {
            EnsureSafeRootReachesPhysicalBottom();
            Apply(force: true);
        }

        private void LateUpdate()
        {
            EnsureSafeRootReachesPhysicalBottom();

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

        /// <summary> Recalcule immédiatement (éditeur / builder / après rebuild onglets). </summary>
        public void Refresh()
        {
            EnsureSafeRootReachesPhysicalBottom();
            Apply(force: true);
        }

        /// <summary> Rebind la bande safe (après génération runtime des onglets). </summary>
        public void BindSafeBand(RectTransform[] content)
        {
            safeBandContent = content;
            Apply(force: true);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Cache()
        {
            if (_navRt == null)
                _navRt = (RectTransform)transform;
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();
            if (_safeAreaFitter == null && _navRt != null && _navRt.parent != null)
                _safeAreaFitter = _navRt.parent.GetComponent<SafeAreaFitter>();
        }

        /// <summary>
        /// SafeRoot doit aller jusqu'au bord bas physique — sinon rien ne peut
        /// descendre dans la barre gestuelle.
        /// </summary>
        private void EnsureSafeRootReachesPhysicalBottom()
        {
            Cache();
            if (_safeAreaFitter == null)
                return;
            // LOCK 2.1 : ne jamais toucher ConformTop ici.
            if (_safeAreaFitter.ConformBottom)
                _safeAreaFitter.ConformBottom = false;
        }

        private void Apply(bool force)
        {
            Apply(force, ScreenSafeArea.SafeArea, ScreenSafeArea.Width, ScreenSafeArea.Height);
        }

        private void Apply(bool force, Rect safe, int screenW, int screenH)
        {
            Cache();
            if (_navRt == null || screenW <= 0 || screenH <= 0)
                return;

            float bleed = ComputeBottomBleedCanvasUnits(safe, screenW, screenH);
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

            // Collé au bas physique de SafeRoot (= bas écran si conformBottom=false).
            _navRt.anchorMin = new Vector2(0f, 0f);
            _navRt.anchorMax = new Vector2(1f, 0f);
            _navRt.pivot = new Vector2(0.5f, 0f);
            _navRt.anchoredPosition = Vector2.zero;
            _navRt.sizeDelta = new Vector2(0f, UiTheme.NavHeight + bleed);

            StretchVisualsFullNav();

            // Descendre la bande tabs dans l'encart : marge haut généreuse, labels bas.
            // (Le bleed gestuel reste réservé en bas — pas de clic dans la home indicator.)
            float topPad = 64f;
            float bottomPad = bleed + 8f;
            if (safeBandContent == null)
                return;

            for (int i = 0; i < safeBandContent.Length; i++)
            {
                RectTransform rt = safeBandContent[i];
                if (rt == null)
                    continue;

                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.offsetMin = new Vector2(0f, bottomPad);
                rt.offsetMax = new Vector2(0f, -topPad);
            }
        }

        private void StretchVisualsFullNav()
        {
            for (int i = 0; i < _navRt.childCount; i++)
            {
                Transform child = _navRt.GetChild(i);
                if (child.name == "NavBackdrop")
                {
                    RectTransform rt = child as RectTransform;
                    if (rt == null)
                        continue;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = Vector2.zero;
                    rt.localScale = Vector3.one;
                    continue;
                }

                if (child.name != "FooterMotif")
                    continue;

                // Plein cadre + léger scale : le plateau pixel comble les marges noires
                // du sprite ; RectMask2D sur la barre rogne le débord.
                RectTransform motifRt = child as RectTransform;
                if (motifRt == null)
                    continue;

                motifRt.anchorMin = Vector2.zero;
                motifRt.anchorMax = Vector2.one;
                motifRt.pivot = new Vector2(0.5f, 0.5f);
                motifRt.anchoredPosition = Vector2.zero;
                motifRt.sizeDelta = Vector2.zero;
                motifRt.localScale = new Vector3(1.28f, 1.32f, 1f);

                Image img = child.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = false;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = false;
                }
            }
        }

        private void EnsureVisualsRaycastPolicy()
        {
            Cache();
            if (_navRt == null)
                return;

            for (int i = 0; i < _navRt.childCount; i++)
            {
                Transform child = _navRt.GetChild(i);
                Image img = child.GetComponent<Image>();
                if (img == null)
                    continue;

                if (child.name == "NavBackdrop")
                    img.raycastTarget = true;
                else if (child.name == "FooterMotif" || child.name == "TopHairline")
                    img.raycastTarget = false;
            }
        }

        private float ComputeBottomBleedCanvasUnits(Rect safe, int screenW, int screenH)
        {
            float unsafeBottomPx = Mathf.Max(0f, safe.yMin);
            if (unsafeBottomPx <= 0.5f)
                return 0f;

            float scale = 1f;
            if (_canvas != null && _canvas.scaleFactor > 0.0001f)
                scale = _canvas.scaleFactor;

            return unsafeBottomPx / scale;
        }
    }
}
