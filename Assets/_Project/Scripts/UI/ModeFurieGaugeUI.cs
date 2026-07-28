using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// HUD minimal de la jauge Mode Furie — barre fine sur le bord droit.
    /// Se crée automatiquement si absente de la scène.
    /// </summary>
    public class ModeFurieGaugeUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float BAR_WIDTH = 10f;
        private const float BAR_HEIGHT = 160f;
        private const float RIGHT_MARGIN = 18f;
        private const float VERTICAL_OFFSET = 40f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static ModeFurieGaugeUI _instance;
        private Image _fillImage;
        private Image _readyGlow;
        private RectTransform _root;
        private ModeFurieHandler _handler;
        private bool _visible;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            UnsubscribeHandler();
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            EnsureHandlerSubscription();

            bool shouldShow = ValiseManager.Instance != null &&
                              ValiseManager.Instance.IsValiseActive("valise_mode_furie");

            if (shouldShow != _visible)
            {
                _visible = shouldShow;
                if (_root != null)
                    _root.gameObject.SetActive(_visible);
                if (_visible)
                    _handler?.NotifyUi();
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Crée le HUD sous un canvas Overlay s'il n'existe pas encore.
        /// </summary>
        public static ModeFurieGaugeUI EnsureExists()
        {
            if (_instance != null)
                return _instance;

            Canvas canvas = FindOverlayCanvas();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("ModeFurieCanvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 80;
                canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            GameObject root = new GameObject("ModeFurieGaugeUI");
            root.transform.SetParent(canvas.transform, false);
            ModeFurieGaugeUI ui = root.AddComponent<ModeFurieGaugeUI>();
            ui.BuildVisuals();
            return ui;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void BuildVisuals()
        {
            _root = gameObject.GetComponent<RectTransform>();
            if (_root == null)
                _root = gameObject.AddComponent<RectTransform>();

            _root.anchorMin = new Vector2(1f, 0.5f);
            _root.anchorMax = new Vector2(1f, 0.5f);
            _root.pivot = new Vector2(1f, 0.5f);
            _root.sizeDelta = new Vector2(BAR_WIDTH + 4f, BAR_HEIGHT + 4f);
            _root.anchoredPosition = new Vector2(-RIGHT_MARGIN, VERTICAL_OFFSET);

            Image bg = CreateChildImage("Track", _root, new Color(0.12f, 0.12f, 0.14f, 0.75f));
            RectTransform bgRt = bg.rectTransform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            _fillImage = CreateChildImage("Fill", _root, new Color(0.95f, 0.35f, 0.2f, 0.95f));
            RectTransform fillRt = _fillImage.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 0f);
            fillRt.pivot = new Vector2(0.5f, 0f);
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, 2f);
            fillRt.sizeDelta = new Vector2(fillRt.sizeDelta.x, 0f);

            _readyGlow = CreateChildImage("Ready", _root, new Color(1f, 0.85f, 0.2f, 0f));
            RectTransform glowRt = _readyGlow.rectTransform;
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.offsetMin = new Vector2(-2f, -2f);
            glowRt.offsetMax = new Vector2(2f, 2f);

            _root.gameObject.SetActive(false);
            _visible = false;
        }

        private static Image CreateChildImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Canvas FindOverlayCanvas()
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas c = canvases[i];
                if (c != null && c.isActiveAndEnabled && c.renderMode == RenderMode.ScreenSpaceOverlay)
                    return c;
            }
            return null;
        }

        private void EnsureHandlerSubscription()
        {
            if (_handler != null) return;
            if (ValiseEffectRegistry.Instance == null) return;

            if (ValiseEffectRegistry.Instance.GetHandler("mode_furie") is ModeFurieHandler handler)
            {
                _handler = handler;
                _handler.OnGaugeChanged += OnGaugeChanged;
            }
        }

        private void UnsubscribeHandler()
        {
            if (_handler == null) return;
            _handler.OnGaugeChanged -= OnGaugeChanged;
            _handler = null;
        }

        private void OnGaugeChanged(int gauge, int threshold, bool ready)
        {
            if (_fillImage == null) return;

            float fill = threshold > 0 ? Mathf.Clamp01((float)gauge / threshold) : 0f;
            RectTransform fillRt = _fillImage.rectTransform;
            float maxHeight = BAR_HEIGHT - 4f;
            fillRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight * fill);

            if (_readyGlow != null)
            {
                Color c = _readyGlow.color;
                c.a = ready ? 0.55f : 0f;
                _readyGlow.color = c;
            }

            if (ready)
                _fillImage.color = new Color(1f, 0.75f, 0.15f, 0.95f);
            else
                _fillImage.color = new Color(0.95f, 0.35f, 0.2f, 0.95f);
        }
    }
}
