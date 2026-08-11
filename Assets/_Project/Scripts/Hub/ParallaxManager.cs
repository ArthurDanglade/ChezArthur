using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ChezArthur.Hub
{
    /// <summary>
    /// Gere le defilement parallaxe de plusieurs couches de paysage.
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
            [System.NonSerialized] public LayerFrameSet frameSet;
            [System.NonSerialized] public float frameTimer;
            [System.NonSerialized] public int frameIndex;
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Couches de parallaxe (arriere vers avant)")]
        [SerializeField] private ParallaxLayer[] layers;

        [Header("Effet de tremblement (train)")]
        [SerializeField] private RectTransform wagonTransform;
        [SerializeField] private float shakeIntensity = 2f;
        [SerializeField] private float shakeSpeed = 15f;

        [Header("Controles")]
        [SerializeField] private bool isScrolling = true;
        [SerializeField] private bool isShaking = true;

        [Header("Fond de monde")]
        [SerializeField] private WorldBackgroundDefinition defaultDefinition;

        [Header("Fenetre wagon (espace art natif)")]
        [SerializeField] private Vector2Int wagonArtSize = new Vector2Int(232, 532);

        [Header("Cibles premier plan")]
        [SerializeField] private Image wagonImage;
        [SerializeField] private Image characterImage;
        [SerializeField] private Image windowGlareImage;
        [SerializeField] private Image lightOverlayImage;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVEES
        // ═══════════════════════════════════════════
        private Vector2 _wagonOriginalPosition;
        private bool _hasWagonTransform;
        private Vector2 _characterOriginalPosition;
        private bool _hasCharacterTransform;
        private float _speedMultiplier = 1f;
        private bool _layingOut;
        private float _lastLaidOutParentW = float.MinValue;
        private float _lastLaidOutParentH = float.MinValue;

        // ═══════════════════════════════════════════
        // PROPRIETES PUBLIQUES
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
            {
                UpdateParallax();
                UpdateFlipbooks();
            }

            if (isShaking && _hasWagonTransform)
                UpdateShake();
        }

        private void LateUpdate()
        {
            // Filet : si le framing a change la taille sans DimensionsChange.
            RelayoutIfParentSizeChanged();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_layingOut || CurrentDefinition == null)
                return;
            LayoutLayers(CurrentDefinition);
        }

        /// <summary>
        /// Recalcule le layout sur le rect actuel (appele apres framing).
        /// Force d'abord le HomeIllustrationRig en pleine largeur page.
        /// </summary>
        public void RelayoutToCurrentRect()
        {
            if (CurrentDefinition == null)
                return;

            ForceIllustrationRigFullWidth();
            Canvas.ForceUpdateCanvases();
            LayoutLayers(CurrentDefinition);
        }

        /// <summary>
        /// HomeIllustrationRig : stretch X = largeur exacte de PageAccueil.
        /// </summary>
        private void ForceIllustrationRigFullWidth()
        {
            RectTransform landscape = transform as RectTransform;
            if (landscape == null)
                return;

            RectTransform rig = landscape.parent as RectTransform;
            RectTransform page = rig != null ? rig.parent as RectTransform : null;
            if (rig == null || page == null)
                return;

            float pageW = page.rect.width;
            float pageH = page.rect.height;
            if (pageW < 1f || pageH < 1f)
                return;

            float scale = pageW / wagonArtSize.x;
            float rigH = wagonArtSize.y * scale;

            rig.anchorMin = new Vector2(0f, 0.5f);
            rig.anchorMax = new Vector2(1f, 0.5f);
            rig.pivot = new Vector2(0.5f, 0.5f);
            rig.localScale = Vector3.one;
            rig.sizeDelta = new Vector2(0f, rigH);

            float posY = 0f;
            if (rigH > pageH + 0.5f)
            {
                float focusY = 0.28f;
                float zoneCenterY = 0f;
                float focusLocalY = (0.5f - focusY) * rigH;
                posY = zoneCenterY - focusLocalY;
                float minY = pageH * 0.5f - rigH * 0.5f;
                float maxY = -pageH * 0.5f + rigH * 0.5f;
                if (minY > maxY)
                {
                    float mid = (minY + maxY) * 0.5f;
                    minY = mid;
                    maxY = mid;
                }

                posY = Mathf.Clamp(posY, minY, maxY);
            }

            rig.anchoredPosition = new Vector2(0f, posY);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(rig);
#endif
        }

        private void RelayoutIfParentSizeChanged()
        {
            if (CurrentDefinition == null || _layingOut)
                return;

            RectTransform root = transform as RectTransform;
            if (root == null)
                return;

            float w = root.rect.width;
            float h = root.rect.height;
            if (w < 1f || h < 1f)
                return;

            if (Mathf.Approximately(w, _lastLaidOutParentW)
                && Mathf.Approximately(h, _lastLaidOutParentH))
                return;

            RelayoutToCurrentRect();
        }

        // ═══════════════════════════════════════════
        // METHODES PRIVEES
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

        private void UpdateFlipbooks()
        {
            if (layers == null)
                return;
            for (int i = 0; i < layers.Length; i++)
            {
                LayerFrameSet set = layers[i].frameSet;
                if (set == null || !set.IsValid || layers[i].image == null)
                    continue;
                layers[i].frameTimer += Time.unscaledDeltaTime;
                float interval = 1f / set.FramesPerSecond;
                if (layers[i].frameTimer < interval)
                    continue;
                layers[i].frameTimer = Mathf.Min(
                    layers[i].frameTimer - interval, interval);
                layers[i].frameIndex++;
                if (layers[i].frameIndex >= set.Frames.Length)
                    layers[i].frameIndex = 0;
                Texture2D next = set.Frames[layers[i].frameIndex];
                if (next != null)
                    layers[i].image.texture = next;
            }
        }

        private void UpdateShake()
        {
            float offsetY = Mathf.Sin(Time.unscaledTime * shakeSpeed) * shakeIntensity;
            float offsetX = Mathf.Sin(Time.unscaledTime * shakeSpeed * 0.7f)
                * (shakeIntensity * 0.3f);
            Vector2 shake = new Vector2(offsetX, offsetY);

            wagonTransform.anchoredPosition = _wagonOriginalPosition + shake;

            // Meme offset que le wagon : le perso est dessine dessous, pas enfant.
            if (_hasCharacterTransform && characterImage != null)
            {
                characterImage.rectTransform.anchoredPosition =
                    _characterOriginalPosition + shake;
            }
        }

        // ═══════════════════════════════════════════
        // METHODES PUBLIQUES
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
                    layers[i].frameSet = definition.Layers[i].FrameSet;
                    layers[i].frameTimer = 0f;
                    layers[i].frameIndex = 0;

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
                    layers[i].frameSet = null;

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
            EnsureForegroundDrawOrder();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        /// <summary>
        /// Ordre de dessin : Landscape (this) → vitre → wagon → perso → light.
        /// Perso au-dessus du wagon (ordre Aseprite) ; les montants fins restent
        /// visibles car le sprite ne les recouvre pas entierement.
        /// </summary>
        private void EnsureForegroundDrawOrder()
        {
            int i = transform.GetSiblingIndex() + 1;

            if (windowGlareImage != null)
                windowGlareImage.transform.SetSiblingIndex(i++);

            if (wagonTransform != null)
                wagonTransform.SetSiblingIndex(i++);

            if (characterImage != null)
                characterImage.transform.SetSiblingIndex(i++);

            if (lightOverlayImage != null)
                lightOverlayImage.transform.SetSiblingIndex(i++);
        }

        /// <summary>
        /// Pose calques en pleine largeur du LandscapeLayer + premier plan.
        /// </summary>
        private void LayoutLayers(WorldBackgroundDefinition definition)
        {
            if (_layingOut)
                return;

            _layingOut = true;
            try
            {
                RectTransform root = transform as RectTransform;
                if (root == null || layers == null)
                    return;

                if (wagonArtSize.x < 1 || wagonArtSize.y < 1)
                {
                    Debug.LogWarning(
                        "[ParallaxManager] wagonArtSize non configure "
                        + "(renseigner 232x532). Layout ignore.",
                        this);
                    return;
                }

                float parentW = root.rect.width;
                float parentH = root.rect.height;
                if (parentW <= 1f || parentH <= 1f)
                {
                    parentW = Mathf.Abs(root.sizeDelta.x) > 1f
                        ? Mathf.Abs(root.sizeDelta.x) : wagonArtSize.x;
                    parentH = Mathf.Abs(root.sizeDelta.y) > 1f
                        ? Mathf.Abs(root.sizeDelta.y) : wagonArtSize.y;
                }

                float ax = parentW / wagonArtSize.x;
                float ay = parentH / wagonArtSize.y;

                // Preferer l'argument ; fallback CurrentDefinition.
                WorldBackgroundDefinition.LayerEntry[] defLayers = null;
                if (definition != null && definition.Layers != null)
                    defLayers = definition.Layers;
                else if (CurrentDefinition != null)
                    defLayers = CurrentDefinition.Layers;

                if (defLayers != null)
                {
                    for (int i = 0; i < layers.Length; i++)
                    {
                        if (layers[i].image == null)
                            continue;
                        if (i >= defLayers.Length || defLayers[i].Texture == null)
                            continue;

                        RectTransform rt = layers[i].image.rectTransform;
                        Texture2D tex = defLayers[i].Texture;

                        // Pleine largeur LandscapeLayer (= ecran) : plus de trous L/R.
                        rt.anchorMin = new Vector2(0f, 1f);
                        rt.anchorMax = new Vector2(1f, 1f);
                        rt.pivot = new Vector2(0.5f, 1f);
                        rt.localScale = Vector3.one;
                        rt.sizeDelta = new Vector2(0f, tex.height * ay);
                        rt.anchoredPosition = new Vector2(
                            0f,
                            -defLayers[i].NativeOffsetY * ay);

                        // UV plein ; le scroll continue de deplacer uvRect.x.
                        Rect uv = layers[i].uvRect;
                        if (uv.width <= 0f)
                            uv.width = 1f;
                        if (uv.height <= 0f)
                            uv.height = 1f;
                        uv.y = 0f;
                        uv.height = 1f;
                        layers[i].uvRect = uv;
                        layers[i].image.uvRect = uv;

#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            EditorUtility.SetDirty(rt);
                            EditorUtility.SetDirty(layers[i].image);
                        }
#endif
                    }
                }

                // Desactive les RawImage au-dela de la definition (ex. Hills_far).
                for (int i = defLayers != null ? defLayers.Length : 0;
                     i < layers.Length;
                     i++)
                {
                    if (layers[i].image == null)
                        continue;
                    layers[i].image.texture = null;
                    layers[i].image.gameObject.SetActive(false);
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        EditorUtility.SetDirty(layers[i].image);
#endif
                }

                LayoutForeground(parentW, parentH, ax, ay);
                _lastLaidOutParentW = parentW;
                _lastLaidOutParentH = parentH;

#if UNITY_EDITOR
                if (!Application.isPlaying
                    && layers != null
                    && layers.Length > 0
                    && layers[0].image != null)
                {
                    RectTransform skyRt = layers[0].image.rectTransform;
                    Debug.Log(
                        "[ParallaxManager] LayoutLayers stretch parent="
                        + parentW.ToString("0.#") + "x" + parentH.ToString("0.#")
                        + " Sky anchors=(" + skyRt.anchorMin.x + "-" + skyRt.anchorMax.x
                        + ") sizeDelta=" + skyRt.sizeDelta
                        + " rect=" + skyRt.rect.width.ToString("0.#")
                        + "x" + skyRt.rect.height.ToString("0.#"),
                        this);
                }
#endif
            }
            finally
            {
                _layingOut = false;
            }
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

            if (lightOverlayImage != null)
            {
                if (definition.LightOverlaySprite != null)
                {
                    lightOverlayImage.sprite = definition.LightOverlaySprite;
                    lightOverlayImage.gameObject.SetActive(true);
                }
                else
                {
                    lightOverlayImage.gameObject.SetActive(false);
                }
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    EditorUtility.SetDirty(lightOverlayImage);
#endif
            }
        }

        private void LayoutForeground(
            float parentW, float parentH, float ax, float ay)
        {
            if (CurrentDefinition == null)
                return;

            // Wagon = plein parent + leger overflow : les cols transparentes du
            // PNG (0-1, 231) sortent sous le RectMask2D → montants opaques au bord.
            if (wagonTransform != null)
            {
                const float WAGON_EDGE_BLEED_ART_PX = 3f;
                float bleed = WAGON_EDGE_BLEED_ART_PX * ax;
                RectTransform wrt = wagonTransform;
                wrt.anchorMin = Vector2.zero;
                wrt.anchorMax = Vector2.one;
                wrt.pivot = new Vector2(0.5f, 0.5f);
                wrt.anchoredPosition = Vector2.zero;
                // Positif = plus large que le parent (crop des bords transparents).
                wrt.sizeDelta = new Vector2(2f * bleed, 0f);
                wrt.localScale = Vector3.one;
                _hasWagonTransform = true;
                SetWagonRestPosition(Vector2.zero);

                // Evite le noir dans le trou de vitre (cull mesh transparent).
                CanvasRenderer wagonCr = wrt.GetComponent<CanvasRenderer>();
                if (wagonCr != null)
                    wagonCr.cullTransparentMesh = false;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(wrt);
                    if (wagonCr != null)
                        EditorUtility.SetDirty(wagonCr);
                }
#endif
            }

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
                // characterArtPosition deja en espace wagon.
                Vector2Int p = CurrentDefinition.CharacterArtPosition;
                Vector2 pos = new Vector2(
                    -parentW * 0.5f + p.x * ax,
                    parentH * 0.5f - p.y * ay);
                rt.anchoredPosition = pos;
                _characterOriginalPosition = pos;
                _hasCharacterTransform = true;
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
            if (!value && _hasCharacterTransform && characterImage != null)
            {
                characterImage.rectTransform.anchoredPosition =
                    _characterOriginalPosition;
            }
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
