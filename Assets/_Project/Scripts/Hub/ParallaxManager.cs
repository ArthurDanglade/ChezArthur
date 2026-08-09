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

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Vector2 _wagonOriginalPosition;
        private bool _hasWagonTransform;
        private float _speedMultiplier = 1f;

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
        }

        private void Update()
        {
            if (isScrolling)
                UpdateParallax();

            if (isShaking && _hasWagonTransform)
                UpdateShake();
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
        /// Applique une definition de fond (textures + vitesses UV).
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
                        if (layers[i].image != null)
                            Undo.RecordObject(layers[i].image, "Apply World Background");
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
            {
                EditorUtility.SetDirty(this);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
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
