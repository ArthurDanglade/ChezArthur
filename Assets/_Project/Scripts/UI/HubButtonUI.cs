using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Bouton standardisé Hub (Primary / Secondary, état locked).
    /// Feedback pressed via ColorTint (pas d'Animator). Couleurs = UiTheme uniquement.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class HubButtonUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // ENUMS
        // ═══════════════════════════════════════════
        public enum ButtonVariant
        {
            Primary = 0,
            Secondary = 1,
            Danger = 2
        }

        private const float PRESSED_MULTIPLIER = 0.88f; // assombrissement 12 %
        private const float LockedAlpha = 0.55f;
        private const float PrimaryLabelTracking = 6f;
        private const string FillChildName = "Fill";
        private const string LabelChildName = "Label";
        private const string SubLabelChildName = "SubLabel";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Style")]
        [SerializeField] private ButtonVariant variant = ButtonVariant.Primary;
        [SerializeField] private bool locked;

        [Tooltip("Si > 0, remplace la hauteur du variant (ex. bouton icône carré).")]
        [SerializeField] private float overrideHeight;

        [Header("Sprites 9-slice")]
        [SerializeField] private Sprite roundedSpriteS;
        [SerializeField] private Sprite roundedSpriteM;
        [SerializeField] private Sprite roundedSpriteL;

        [Header("Textes (auto si absents)")]
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI subLabel;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Button _button;
        private Image _rootImage;
        private Image _fillImage;
        private CanvasGroup _canvasGroup;
        private LayoutElement _layoutElement;
        private bool _isApplying;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public ButtonVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                ApplyStyle();
            }
        }

        public bool Locked
        {
            get => locked;
            set
            {
                locked = value;
                ApplyStyle();
            }
        }

        public Button Button
        {
            get
            {
                if (_button == null) _button = GetComponent<Button>();
                return _button;
            }
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            CacheRefs();
            ApplyStyle();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_isApplying)
                return;

            UnityEditor.EditorApplication.delayCall += OnValidateDelayed;
        }

        private void OnValidateDelayed()
        {
            if (this == null)
                return;

            CacheRefs();
            ApplyStyle();
        }
#endif

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void SetRoundedSprites(Sprite spriteS, Sprite spriteM, Sprite spriteL)
        {
            roundedSpriteS = spriteS;
            roundedSpriteM = spriteM;
            roundedSpriteL = spriteL;
            ApplyStyle();
        }

        public void SetLabel(string text)
        {
            EnsureLabels();
            if (label != null)
                label.text = text ?? string.Empty;
        }

        public void SetSubLabel(string text)
        {
            EnsureLabels();
            if (subLabel == null)
                return;

            bool has = !string.IsNullOrEmpty(text);
            subLabel.gameObject.SetActive(has);
            if (has)
                subLabel.text = text;
        }

        /// <summary>
        /// Applique variante, locked, ColorTint pressed, raycasts. Idempotent.
        /// </summary>
        public void ApplyStyle()
        {
            if (_isApplying)
                return;

            _isApplying = true;
            try
            {
                CacheRefs();
                EnsureStructure();
                EnsureLabels();
                if (_button == null || _rootImage == null)
                    return;

                if (variant == ButtonVariant.Primary)
                    ApplyPrimary();
                else if (variant == ButtonVariant.Danger)
                    ApplyDanger();
                else
                    ApplySecondary();

                ApplyLockedState();
                ApplyColorTint();
                ApplyRaycasts();
            }
            finally
            {
                _isApplying = false;
            }
        }

        // ═══════════════════════════════════════════
        // STYLE
        // ═══════════════════════════════════════════

        private void ApplyPrimary()
        {
            EnsureFillImage();

            // Contour or + fill ambre (placeholder plus soigné qu'un bloc plat).
            _rootImage.sprite = roundedSpriteL;
            _rootImage.type = Image.Type.Sliced;
            _rootImage.fillCenter = true;
            _rootImage.color = UiTheme.AccentGold;

            if (_fillImage != null)
            {
                _fillImage.gameObject.SetActive(true);
                _fillImage.sprite = roundedSpriteL;
                _fillImage.type = Image.Type.Sliced;
                _fillImage.color = UiTheme.AccentAmber;
                ApplyFillInset(_fillImage.rectTransform, UiTheme.BorderFocus);
                _button.targetGraphic = _fillImage;
            }
            else
            {
                _button.targetGraphic = _rootImage;
            }

            if (_layoutElement != null)
            {
                float h = overrideHeight > 0.01f ? overrideHeight : UiTheme.ButtonPrimaryH;
                _layoutElement.minHeight = h;
                _layoutElement.preferredHeight = h;
                if (overrideHeight < 0.01f)
                {
                    _layoutElement.preferredWidth = UiTheme.ButtonMaxWidth;
                    _layoutElement.flexibleWidth = 0f;
                }
            }

            if (label != null)
            {
                label.color = locked ? UiTheme.TextDisabled : UiTheme.BgDeep;
                label.fontSize = UiTypography.Label;
                label.fontStyle = FontStyles.Bold;
                label.characterSpacing = PrimaryLabelTracking;
            }

            if (subLabel != null && subLabel.gameObject.activeSelf)
                subLabel.color = UiTheme.TextDisabled;
        }

        private void ApplySecondary()
        {
            EnsureFillImage();

            _rootImage.sprite = roundedSpriteM;
            _rootImage.type = Image.Type.Sliced;
            _rootImage.fillCenter = true;
            _rootImage.color = UiTheme.BorderStrong;

            if (_fillImage != null)
            {
                _fillImage.gameObject.SetActive(true);
                _fillImage.sprite = roundedSpriteM;
                _fillImage.type = Image.Type.Sliced;
                // Verre sombre semi-transparent — moins cheap qu'un panneau opaque.
                Color fill = UiTheme.BgElevated;
                fill.a = 0.82f;
                _fillImage.color = fill;
                ApplyFillInset(_fillImage.rectTransform, UiTheme.BorderThin);
                _button.targetGraphic = _fillImage;
            }
            else
            {
                _button.targetGraphic = _rootImage;
            }

            if (_layoutElement != null)
            {
                float h = overrideHeight > 0.01f ? overrideHeight : UiTheme.ButtonSecondaryH;
                _layoutElement.minHeight = h;
                _layoutElement.preferredHeight = h;
                if (overrideHeight < 0.01f)
                {
                    _layoutElement.preferredWidth = UiTheme.ButtonMaxWidth;
                    _layoutElement.flexibleWidth = 0f;
                }
            }

            if (label != null)
            {
                label.color = locked ? UiTheme.TextDisabled : UiTheme.TextPrimary;
                label.fontSize = UiTypography.Label;
                label.fontStyle = FontStyles.Bold;
                label.characterSpacing = 2f;
            }

            if (subLabel != null && subLabel.gameObject.activeSelf)
            {
                subLabel.color = UiTheme.TextMuted;
                subLabel.fontStyle = FontStyles.Normal;
            }
        }

        private void ApplyDanger()
        {
            EnsureFillImage();

            _rootImage.sprite = roundedSpriteM;
            _rootImage.type = Image.Type.Sliced;
            _rootImage.fillCenter = true;
            _rootImage.color = UiTheme.Danger;

            if (_fillImage != null)
            {
                _fillImage.gameObject.SetActive(true);
                _fillImage.sprite = roundedSpriteM;
                _fillImage.type = Image.Type.Sliced;
                Color fill = UiTheme.BgDeep;
                fill.a = 0.92f;
                _fillImage.color = fill;
                ApplyFillInset(_fillImage.rectTransform, UiTheme.BorderThin);
                _button.targetGraphic = _fillImage;
            }
            else
            {
                _button.targetGraphic = _rootImage;
            }

            if (_layoutElement != null)
            {
                float h = overrideHeight > 0.01f ? overrideHeight : UiTheme.ButtonSecondaryH;
                _layoutElement.minHeight = h;
                _layoutElement.preferredHeight = h;
                if (overrideHeight < 0.01f)
                {
                    _layoutElement.preferredWidth = UiTheme.ButtonMaxWidth;
                    _layoutElement.flexibleWidth = 0f;
                }
            }

            if (label != null)
            {
                label.color = locked ? UiTheme.TextDisabled : UiTheme.Danger;
                label.fontSize = UiTypography.Label;
                label.fontStyle = FontStyles.Bold;
                label.characterSpacing = 2f;
            }

            if (subLabel != null && subLabel.gameObject.activeSelf)
                subLabel.color = UiTheme.TextMuted;
        }

        private void ApplyLockedState()
        {
            _button.interactable = !locked;

            if (locked)
            {
                // Fond Locked (token RUI1) — lisibilité condition via SubLabel.
                if (_fillImage != null && _fillImage.gameObject.activeSelf)
                    _fillImage.color = UiTheme.Locked;
                if (label != null)
                    label.color = UiTheme.TextMuted;
            }

            if (_canvasGroup != null)
            {
                // Boutons icône (overrideHeight) : rester lisibles même locked.
                float lockedA = overrideHeight > 0.01f ? 0.85f : LockedAlpha;
                _canvasGroup.alpha = locked ? lockedA : 1f;
            }

            if (subLabel != null)
                subLabel.fontSize = UiTypography.Caption;
        }

        private void ApplyColorTint()
        {
            _button.transition = Selectable.Transition.ColorTint;

            ColorBlock block = _button.colors;
            block.colorMultiplier = 1f;
            block.fadeDuration = 0.08f;
            block.normalColor = Color.white;
            block.highlightedColor = Color.white;
            block.selectedColor = Color.white;
            block.pressedColor = new Color(PRESSED_MULTIPLIER, PRESSED_MULTIPLIER, PRESSED_MULTIPLIER, 1f);
            block.disabledColor = Color.white;
            _button.colors = block;
        }

        private void ApplyRaycasts()
        {
            Graphic target = _button.targetGraphic;
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null) continue;
                images[i].raycastTarget = images[i] == target;
            }

            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                    texts[i].raycastTarget = false;
            }
        }

        // ═══════════════════════════════════════════
        // STRUCTURE
        // ═══════════════════════════════════════════

        private void CacheRefs()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_rootImage == null) _rootImage = GetComponent<Image>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_layoutElement == null) _layoutElement = GetComponent<LayoutElement>();

            if (_fillImage == null)
            {
                Transform fillTx = transform.Find(FillChildName);
                if (fillTx != null)
                    _fillImage = fillTx.GetComponent<Image>();
            }

            if (label == null)
            {
                Transform t = transform.Find(LabelChildName);
                if (t != null) label = t.GetComponent<TextMeshProUGUI>();
            }

            if (subLabel == null)
            {
                Transform t = transform.Find(SubLabelChildName);
                if (t != null) subLabel = t.GetComponent<TextMeshProUGUI>();
            }
        }

        private void EnsureStructure()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.RegisterCreatedObjectUndo(_canvasGroup, "HubButton CanvasGroup");
#endif
            }

            if (_layoutElement == null)
            {
                _layoutElement = gameObject.AddComponent<LayoutElement>();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.RegisterCreatedObjectUndo(_layoutElement, "HubButton LayoutElement");
#endif
            }

            EnsureFillImage();
        }

        private void EnsureFillImage()
        {
            if (_fillImage != null)
                return;

            Transform fillTx = transform.Find(FillChildName);
            if (fillTx != null)
            {
                _fillImage = fillTx.GetComponent<Image>();
                if (_fillImage == null)
                    _fillImage = fillTx.gameObject.AddComponent<Image>();
                return;
            }

            var fillGo = new GameObject(FillChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RegisterCreatedObjectUndo(fillGo, "HubButton Fill");
#endif
            fillGo.transform.SetParent(transform, false);
            fillGo.transform.SetAsFirstSibling();
            _fillImage = fillGo.GetComponent<Image>();
        }

        private void EnsureLabels()
        {
            if (label == null)
            {
                Transform existing = transform.Find(LabelChildName);
                if (existing != null)
                    label = existing.GetComponent<TextMeshProUGUI>();
                else
                    label = CreateTmp(LabelChildName, UiTypography.Label);
            }

            if (subLabel == null)
            {
                Transform existing = transform.Find(SubLabelChildName);
                if (existing != null)
                    subLabel = existing.GetComponent<TextMeshProUGUI>();
                else
                {
                    subLabel = CreateTmp(SubLabelChildName, UiTypography.Caption);
                    subLabel.gameObject.SetActive(false);
                }
            }

            // Positionnement texte : centre, SubLabel légèrement sous le Label
            if (label != null)
            {
                var rt = label.rectTransform;
                rt.anchorMin = new Vector2(0.05f, 0.35f);
                rt.anchorMax = new Vector2(0.95f, 0.95f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }

            if (subLabel != null)
            {
                var rt = subLabel.rectTransform;
                rt.anchorMin = new Vector2(0.05f, 0.05f);
                rt.anchorMax = new Vector2(0.95f, 0.4f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                subLabel.alignment = TextAlignmentOptions.Center;
                subLabel.raycastTarget = false;
                subLabel.fontSize = UiTypography.Caption;
            }
        }

        private TextMeshProUGUI CreateTmp(string name, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "HubButton TMP");
#endif
            go.transform.SetParent(transform, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.raycastTarget = false;
            // Police = défaut TMP Settings (gate 7.4)
            return tmp;
        }

        private static void ApplyFillInset(RectTransform fillRt, float inset)
        {
            if (fillRt == null) return;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(inset, inset);
            fillRt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
