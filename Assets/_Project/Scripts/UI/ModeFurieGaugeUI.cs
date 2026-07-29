using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// HUD Mode Furie — canvas dédié, très visible (debug feel en attendant polish).
    /// </summary>
    public class ModeFurieGaugeUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float BAR_WIDTH = 48f;
        private const float BAR_HEIGHT = 320f;
        private const float RIGHT_MARGIN = 28f;
        private const int CANVAS_SORT_ORDER = 500;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static ModeFurieGaugeUI _instance;
        private Image _fillImage;
        private Image _readyGlow;
        private TextMeshProUGUI _label;
        private TextMeshProUGUI _valueText;
        private RectTransform _root;
        private ModeFurieHandler _handler;
        private bool _visible;
        private bool _loggedMissingHandler;

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
                {
                    Debug.Log("[ModeFurieUI] Jauge affichée (valise active)");
                    _handler?.NotifyUi();
                }
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Crée (ou récupère) le HUD sur un canvas Overlay dédié, sorting élevé.
        /// </summary>
        public static ModeFurieGaugeUI EnsureExists()
        {
            if (_instance != null)
                return _instance;

            GameObject canvasGo = new GameObject("ModeFurieCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CANVAS_SORT_ORDER;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject root = new GameObject("ModeFurieGaugeUI");
            root.transform.SetParent(canvasGo.transform, false);
            ModeFurieGaugeUI ui = root.AddComponent<ModeFurieGaugeUI>();
            ui.BuildVisuals();
            Debug.Log("[ModeFurieUI] HUD créé (canvas dédié sortingOrder=" + CANVAS_SORT_ORDER + ")");
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
            _root.sizeDelta = new Vector2(140f, BAR_HEIGHT + 100f);
            _root.anchoredPosition = new Vector2(-RIGHT_MARGIN, 0f);

            // Fond flashy
            Image panel = CreateChildImage("Panel", _root, new Color(0f, 0f, 0f, 0.75f));
            RectTransform panelRt = panel.rectTransform;
            Stretch(panelRt);

            Image bg = CreateChildImage("Track", _root, new Color(0.15f, 0.05f, 0.02f, 1f));
            RectTransform bgRt = bg.rectTransform;
            bgRt.anchorMin = new Vector2(0.5f, 0.12f);
            bgRt.anchorMax = new Vector2(0.5f, 0.82f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(BAR_WIDTH + 12f, 0f);
            bgRt.anchoredPosition = Vector2.zero;

            _fillImage = CreateChildImage("Fill", bgRt, new Color(1f, 0.25f, 0.05f, 1f));
            RectTransform fillRt = _fillImage.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 0f);
            fillRt.pivot = new Vector2(0.5f, 0f);
            fillRt.offsetMin = new Vector2(4f, 4f);
            fillRt.offsetMax = new Vector2(-4f, 4f);
            fillRt.sizeDelta = new Vector2(0f, 0f);

            _readyGlow = CreateChildImage("Ready", bgRt, new Color(1f, 1f, 0.2f, 0f));
            RectTransform glowRt = _readyGlow.rectTransform;
            Stretch(glowRt);
            glowRt.offsetMin = new Vector2(-8f, -8f);
            glowRt.offsetMax = new Vector2(8f, 8f);

            _label = CreateTmp("Label", _root, 28f, FontStyles.Bold);
            RectTransform labelRt = _label.rectTransform;
            labelRt.anchorMin = new Vector2(0.5f, 0.84f);
            labelRt.anchorMax = new Vector2(0.5f, 0.98f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.sizeDelta = new Vector2(130f, 0f);
            _label.text = "FURIE";
            _label.color = new Color(1f, 0.45f, 0.1f, 1f);
            _label.alignment = TextAlignmentOptions.Center;
            _label.enableAutoSizing = true;
            _label.fontSizeMin = 18f;
            _label.fontSizeMax = 32f;

            _valueText = CreateTmp("Value", _root, 22f, FontStyles.Bold);
            RectTransform valueRt = _valueText.rectTransform;
            valueRt.anchorMin = new Vector2(0.5f, 0.02f);
            valueRt.anchorMax = new Vector2(0.5f, 0.12f);
            valueRt.pivot = new Vector2(0.5f, 0.5f);
            valueRt.sizeDelta = new Vector2(120f, 0f);
            _valueText.text = "0/8";
            _valueText.color = Color.white;
            _valueText.alignment = TextAlignmentOptions.Center;

            _root.gameObject.SetActive(false);
            _visible = false;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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

        private static TextMeshProUGUI CreateTmp(string name, Transform parent, float size, FontStyles style)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        private void EnsureHandlerSubscription()
        {
            if (_handler != null) return;
            if (ValiseEffectRegistry.Instance == null)
            {
                if (!_loggedMissingHandler)
                {
                    Debug.LogWarning("[ModeFurieUI] ValiseEffectRegistry absent");
                    _loggedMissingHandler = true;
                }
                return;
            }

            if (ValiseEffectRegistry.Instance.GetHandler("mode_furie") is ModeFurieHandler handler)
            {
                _handler = handler;
                _handler.OnGaugeChanged += OnGaugeChanged;
                _handler.NotifyUi();
                Debug.Log("[ModeFurieUI] Branché sur ModeFurieHandler");
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
            float parentH = ((RectTransform)_fillImage.transform.parent).rect.height;
            float maxHeight = Mathf.Max(8f, parentH - 8f);
            fillRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight * fill);

            if (_valueText != null)
                _valueText.text = ready ? "MAX !" : $"{gauge}/{threshold}";

            if (_readyGlow != null)
            {
                Color c = _readyGlow.color;
                c.a = ready ? 0.85f : 0f;
                _readyGlow.color = c;
            }

            _fillImage.color = ready
                ? new Color(1f, 0.9f, 0.15f, 1f)
                : new Color(1f, 0.25f, 0.05f, 1f);
        }
    }
}
