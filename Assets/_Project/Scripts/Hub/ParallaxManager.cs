using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ChezArthur.Hub
{
    /// <summary>
    /// Gère le défilement parallaxe de plusieurs couches de paysage.
    /// </summary>
    public class ParallaxManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CLASSES INTERNES
        // ═══════════════════════════════════════════

        [System.Serializable]
        public class ParallaxLayer
        {
            public RawImage image;
            public float scrollSpeed;
            [HideInInspector] public Rect uvRect;
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Couches de parallaxe (arrière vers avant)")]
        [SerializeField] private ParallaxLayer[] layers;

        [Header("Effet de tremblement (train)")]
        [SerializeField] private RectTransform wagonTransform;
        [SerializeField] private float shakeIntensity = 2f;
        [SerializeField] private float shakeSpeed = 15f;

        [Header("Contrôles")]
        [SerializeField] private bool isScrolling = true;
        [SerializeField] private bool isShaking = true;

        [Header("Fond de monde")]
        [SerializeField] private WorldBackgroundDefinition defaultDefinition;

        [Header("Fenetre wagon (espace art natif)")]
        [SerializeField] private Vector2Int wagonArtSize = new Vector2Int(232, 532);
        [SerializeField] private RectInt windowHole = new RectInt(0, 61, 232, 305);

        [Header("Cibles premier plan")]
        [SerializeField] private Image wagonImage;
        [SerializeField] private Image characterImage;
        [SerializeField] private Image windowGlareImage;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Vector2 _wagonOriginalPosition;
        private bool _hasWagonTransform;
        private float _speedMultiplier = 1f;
        private bool _layingOut;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public RectTransform RootRect => transform as RectTransform;

        public WorldBackgroundDefinition CurrentDefinition { get; private set; }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            EnsureUvInitialized();

            if (wagonTransform != null)
            {
                _hasWagonTransform = true;
                _wagonOriginalPosition = wagonTransform.anchoredPosition;
            }

            if (CurrentDefinition == null && defaultDefinition != null)
                ApplyDefinition(defaultDefinition);
        }

        private void Update()
        {
            if (isScrolling)
                UpdateParallax();

            if (isShaking && _hasWagonTransform)
                UpdateShake();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_layingOut || CurrentDefinition == null)
                return;
            LayoutLayers(CurrentDefinition);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureUvInitialized()
        {
            if (layers == null)
                return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].image != null
                    && (layers[i].uvRect.width <= 0f || layers[i].uvRect.height <= 0f))
                {
                    layers[i].uvRect = layers[i].image.uvRect;
                }
            }
        }

        private void UpdateParallax()
        {
            if (layers == null)
                return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].image == null)
                    continue;

                float speed = layers[i].scrollSpeed * _speedMultiplier;
                layers[i].uvRect.x += speed * Time.unscaledDeltaTime;

                if (layers[i].uvRect.x > 1f)
                    layers[i].uvRect.x -= 1f;
                if (layers[i].uvRect.x < 0f)
                    layers[i].uvRect.x += 1f;

                layers[i].image.uvRect = layers[i].uvRect;
            }
        }

        private void UpdateShake()
        {
            float offsetY = Mathf.Sin(Time.unscaledTime * shakeSpeed) * shakeIntensity;
            float offsetX = Mathf.Sin(Time.unscaledTime * shakeSpeed * 0.7f)
                * (shakeIntensity * 0.3f);

            wagonTransform.anchoredPosition =
                _wagonOriginalPosition + new Vector2(offsetX, offsetY);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Applique une definition de fond (textures + vitesses UV + cadrage).
        /// Appelable hors Play mode depuis un script editeur.
        /// </summary>
        public void ApplyDefinition(WorldBackgroundDefinition definition)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(this, "Apply World Background");
                if (layers != null)
                {
                    for (int i = 0; i < layers.Length; i++)
                    {
                        if (layers[i].image == null)
                            continue;
                        Undo.RecordObject(layers[i].image, "Apply World Background");
                        RectTransform rt = layers[i].image.rectTransform;
                        if (rt != null)
                            Undo.RecordObject(rt, "Apply World Background");
                    }
                }
            }
#endif

            EnsureUvInitialized();

            if (definition == null || definition.Layers == null)
            {
                Debug.LogWarning("[ParallaxManager] ApplyDefinition: definition ou Layers null.");
                return;
            }

            CurrentDefinition = definition;

            if (layers == null)
                return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (i < definition.Layers.Length && layers[i].image != null)
                {
                    layers[i].image.texture = definition.Layers[i].Texture;
                    layers[i].scrollSpeed = definition.Layers[i].ScrollSpeed;

                    Rect previous = layers[i].uvRect;
                    float w = previous.width  > 0f ? previous.width  : 1f;
                    float h = previous.height > 0f ? previous.height : 1f;
                    layers[i].uvRect = new Rect(0f, previous.y, w, h);
                    layers[i].image.uvRect = layers[i].uvRect;

                    layers[i].image.gameObject.SetActive(
                        definition.Layers[i].Texture != null);

#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        EditorUtility.SetDirty(layers[i].image);
#endif
                }
                else if (layers[i].image != null)
                {
                    layers[i].image.gameObject.SetActive(false);

#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        EditorUtility.SetDirty(layers[i].image);
#endif
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Canvas.ForceUpdateCanvases();
#endif
            ApplyForeground(definition);
            LayoutLayers(definition);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        /// <summary>
        /// Pose les RectTransform selon StretchFullBleed ou NativeStacked.
        /// </summary>
        private void LayoutLayers(WorldBackgroundDefinition definition)
        {
            if (_layingOut)
                return;

            _layingOut = true;
            try
            {
                if (definition.LayoutMode == LayerLayoutMode.StretchFullBleed)
                {
                    LayoutStretchFullBleed();
                    return;
                }

                LayoutNativeStacked(definition);
            }
            finally
            {
                _layingOut = false;
            }
        }

        private void LayoutStretchFullBleed()
        {
            if (layers == null)
                return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].image == null)
                    continue;

                RectTransform rt = layers[i].image.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(rt);
#endif
            }
        }

        private void LayoutNativeStacked(WorldBackgroundDefinition definition)
        {
            RectTransform root = transform as RectTransform;
            if (root == null || layers == null)
                return;

            if (wagonArtSize.x < 1 || wagonArtSize.y < 1
                || windowHole.width < 1 || windowHole.height < 1)
            {
                Debug.LogWarning(
                    "[ParallaxManager] wagonArtSize/windowHole non configures "
                    + "(instance de scene anterieure au champ ? Renseigner "
                    + "232x532 / 0,61,232,305). Layout ignore.",
                    this);
                return;
            }

            Vector2Int canvas = definition.NativeCanvasSize;
            int nw = Mathf.Max(1, canvas.x);
            int nh = Mathf.Max(1, canvas.y);

            float parentW = root.rect.width;
            float parentH = root.rect.height;
            if (parentW <= 1f || parentH <= 1f)
            {
                parentW = Mathf.Abs(root.sizeDelta.x) > 1f ? Mathf.Abs(root.sizeDelta.x) : nw;
                parentH = Mathf.Abs(root.sizeDelta.y) > 1f ? Mathf.Abs(root.sizeDelta.y) : nh;
            }

            // Trou fenetre dans l'espace art wagon, mappe sur LandscapeLayer.
            float artW = wagonArtSize.x;
            float artH = wagonArtSize.y;

            float ax = parentW / artW;
            float ay = parentH / artH;
            float holeW = windowHole.width * ax;
            float holeH = windowHole.height * ay;
            float holeLeft = -parentW * 0.5f + windowHole.x * ax;
            float holeTop = parentH * 0.5f - windowHole.y * ay;
            float holeCenterY = holeTop - holeH * 0.5f;

            float scaleW = holeW / nw;
            float scaleH = holeH / nh;
            float scale = Mathf.Lerp(scaleW, scaleH, definition.NativeFitBias);
            if (scale < 0.01f)
                scale = 1f;

            float canvasW = nw * scale;
            float canvasH = nh * scale;
            float focusX = definition.NativeFocusX;
            float canvasLeft;
            if (focusX < 0f)
                canvasLeft = holeLeft + (holeW - canvasW) * 0.5f;
            else
                canvasLeft = holeLeft + holeW * 0.5f - focusX * scale;

            float focusY = definition.NativeFocusY;
            if (focusY < 0f)
                focusY = 0f;
            if (focusY > nh)
                focusY = nh;

            // Point natif focusY aligne au centre vertical du trou.
            float canvasTop = holeCenterY + focusY * scale;

            WorldBackgroundDefinition.LayerEntry[] defLayers = definition.Layers;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].image == null)
                    continue;

                if (i >= defLayers.Length || defLayers[i].Texture == null)
                    continue;

                RectTransform rt = layers[i].image.rectTransform;
                Texture2D tex = defLayers[i].Texture;
                int offsetY = defLayers[i].NativeOffsetY;

                float layerW = canvasW;
                float layerH = tex.height * scale;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(layerW, layerH);
                rt.anchoredPosition = new Vector2(
                    canvasLeft,
                    canvasTop - (offsetY * scale));

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(rt);
#endif
            }

            LayoutForeground(parentW, parentH, ax, ay);
        }

        private void ApplyForeground(WorldBackgroundDefinition definition)
        {
            if (wagonImage != null && wagonTransform != null
                && wagonImage.rectTransform != wagonTransform)
            {
                Debug.LogWarning(
                    "[ParallaxManager] wagonImage et wagonTransform pointent des "
                    + "objets differents. Le shake et le sprite doivent viser le "
                    + "meme GameObject.", this);
            }

            if (wagonImage != null && definition.WagonSprite != null)
            {
                wagonImage.sprite = definition.WagonSprite;
                wagonImage.gameObject.SetActive(true);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(wagonImage);
#endif
            }

            if (characterImage != null && definition.CharacterSprite != null)
            {
                characterImage.sprite = definition.CharacterSprite;
                characterImage.gameObject.SetActive(true);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(characterImage);
#endif
            }

            if (windowGlareImage != null)
            {
                if (definition.WindowGlareSprite != null)
                {
                    windowGlareImage.sprite = definition.WindowGlareSprite;
                    windowGlareImage.gameObject.SetActive(true);
                }
                else
                {
                    windowGlareImage.gameObject.SetActive(false);
                }
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(windowGlareImage);
#endif
            }
        }

        private void LayoutForeground(
            float parentW, float parentH, float ax, float ay)
        {
            if (CurrentDefinition == null)
                return;

            if (characterImage != null && characterImage.sprite != null)
            {
                Sprite s = characterImage.sprite;
                float w = s.rect.width * ax;
                float h = s.rect.height * ay;
                RectTransform rt = characterImage.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(w, h);
                Vector2Int p = CurrentDefinition.CharacterArtPosition;
                rt.anchoredPosition = new Vector2(
                    -parentW * 0.5f + p.x * ax,
                    parentH * 0.5f - p.y * ay);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(rt);
#endif
            }

            if (windowGlareImage != null && windowGlareImage.sprite != null)
            {
                Sprite s = windowGlareImage.sprite;
                float w = s.rect.width * ax;
                float h = s.rect.height * ay;
                RectTransform rt = windowGlareImage.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(w, h);
                Vector2Int p = CurrentDefinition.GlareArtPosition;
                rt.anchoredPosition = new Vector2(
                    -parentW * 0.5f + p.x * ax,
                    parentH * 0.5f - p.y * ay);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(rt);
#endif
            }

            if (wagonImage != null && _hasWagonTransform)
                SetWagonRestPosition(wagonImage.rectTransform.anchoredPosition);
        }

        public void SetScrolling(bool value)
        {
            isScrolling = value;
        }

        public void SetShaking(bool value)
        {
            isShaking = value;
            if (!value && _hasWagonTransform)
                wagonTransform.anchoredPosition = _wagonOriginalPosition;
        }

        /// <summary>
        /// Met a jour la position de repos du wagon (apres layout).
        /// </summary>
        public void SetWagonRestPosition(Vector2 anchoredPosition)
        {
            _wagonOriginalPosition = anchoredPosition;
            if (!isShaking && _hasWagonTransform)
                wagonTransform.anchoredPosition = anchoredPosition;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Mode invocation : scroll sans shake wagon Hub.
        /// </summary>
        public void BeginGachaBorrow()
        {
            EnsureUvInitialized();
            SetShaking(false);
            SetScrolling(true);
            SetSpeedMultiplier(1f);
            gameObject.SetActive(true);
        }

        public void EndGachaBorrow()
        {
            SetSpeedMultiplier(1f);
            SetScrolling(true);
            SetShaking(true);
        }
    }
}
