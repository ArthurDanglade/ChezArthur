using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Panneau UI unique du design system Hub : bordure (Image racine) + Fill enfant.
    /// Aucune couleur libre — tout passe par UiTheme via les enums.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class PanelSurface : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // ENUMS
        // ═══════════════════════════════════════════
        public enum SurfaceVariant
        {
            Panel = 0,
            Card = 1,
            Pill = 2
        }

        public enum SurfaceBorder
        {
            Subtle = 0,
            AccentAmber = 1,
            AccentGold = 2
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Style")]
        [SerializeField] private SurfaceVariant variant = SurfaceVariant.Panel;
        [SerializeField] private SurfaceBorder borderStyle = SurfaceBorder.Subtle;

        [Header("Sprites 9-slice (assignés par la sandbox / setup)")]
        [SerializeField] private Sprite roundedSpriteS;
        [SerializeField] private Sprite roundedSpriteM;
        [SerializeField] private Sprite roundedSpriteL;

        [Header("Interaction")]
        [Tooltip("Si vrai : bordure + Fill interceptent les raycasts (ex. scrim).")]
        [SerializeField] private bool blocksRaycasts;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Image _borderImage;
        private Image _fillImage;
        private bool _isApplying;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public SurfaceVariant Variant
        {
            get => variant;
            set
            {
                variant = value;
                ApplyStyle();
            }
        }

        public SurfaceBorder BorderStyle
        {
            get => borderStyle;
            set
            {
                borderStyle = value;
                ApplyStyle();
            }
        }

        public bool BlocksRaycasts
        {
            get => blocksRaycasts;
            set
            {
                blocksRaycasts = value;
                ApplyStyle();
            }
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            CacheBorder();
            EnsureFill();
            ApplyStyle();
        }

        private void OnEnable()
        {
            ApplyStyle();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Évite les erreurs pendant le reimport / destroy
            if (_isApplying)
                return;

            UnityEditor.EditorApplication.delayCall += OnValidateDelayed;
        }

        private void OnValidateDelayed()
        {
            if (this == null)
                return;

            CacheBorder();
            EnsureFill();
            ApplyStyle();
        }
#endif

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Assigne les 3 sprites 9-slice (appelé par l'outil sandbox).
        /// </summary>
        public void SetRoundedSprites(Sprite spriteS, Sprite spriteM, Sprite spriteL)
        {
            roundedSpriteS = spriteS;
            roundedSpriteM = spriteM;
            roundedSpriteL = spriteL;
            ApplyStyle();
        }

        /// <summary>
        /// Applique variante + bordure depuis UiTheme. Idempotent.
        /// </summary>
        public void ApplyStyle()
        {
            if (_isApplying)
                return;

            _isApplying = true;
            try
            {
                CacheBorder();
                EnsureFill();
                if (_borderImage == null || _fillImage == null)
                    return;

                Sprite sprite = ResolveSprite();
                Color fillColor = ResolveFillColor();
                Color borderColor = ResolveBorderColor();

                _borderImage.sprite = sprite;
                _borderImage.type = Image.Type.Sliced;
                _borderImage.color = borderColor;
                _borderImage.raycastTarget = blocksRaycasts;

                _fillImage.sprite = sprite;
                _fillImage.type = Image.Type.Sliced;
                _fillImage.color = fillColor;
                _fillImage.raycastTarget = blocksRaycasts;

                ApplyFillInset();
            }
            finally
            {
                _isApplying = false;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void CacheBorder()
        {
            if (_borderImage == null)
                _borderImage = GetComponent<Image>();
        }

        /// <summary>
        /// Trouve ou crée l'enfant "Fill" (jamais de doublon).
        /// </summary>
        private void EnsureFill()
        {
            if (_fillImage != null)
                return;

            Transform fillTx = transform.Find("Fill");
            if (fillTx != null)
            {
                _fillImage = fillTx.GetComponent<Image>();
                if (_fillImage == null)
                    _fillImage = fillTx.gameObject.AddComponent<Image>();
                return;
            }

            // Cherche un Fill orphelin éventuel (rename)
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "Fill")
                {
                    _fillImage = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
                    return;
                }
            }

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RegisterCreatedObjectUndo(fillGo, "Create PanelSurface Fill");
#endif
            fillGo.transform.SetParent(transform, false);
            fillGo.transform.SetAsFirstSibling();

            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.localScale = Vector3.one;

            _fillImage = fillGo.GetComponent<Image>();
        }

        private void ApplyFillInset()
        {
            var fillRt = _fillImage.transform as RectTransform;
            if (fillRt == null)
                return;

            float inset = UiTheme.BorderThin;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(inset, inset);
            fillRt.offsetMax = new Vector2(-inset, -inset);
        }

        private Sprite ResolveSprite()
        {
            switch (variant)
            {
                case SurfaceVariant.Pill:
                    return roundedSpriteS;
                case SurfaceVariant.Card:
                    return roundedSpriteM;
                default:
                    return roundedSpriteL;
            }
        }

        private Color ResolveFillColor()
        {
            return variant == SurfaceVariant.Pill
                ? UiTheme.BgElevated
                : UiTheme.BgPanel;
        }

        private Color ResolveBorderColor()
        {
            switch (borderStyle)
            {
                case SurfaceBorder.AccentAmber:
                    return UiTheme.AccentAmber;
                case SurfaceBorder.AccentGold:
                    return UiTheme.AccentGold;
                default:
                    return UiTheme.BorderSubtle;
            }
        }
    }
}
