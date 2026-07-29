using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Barre d'onglets data-driven : clone un TabItemTemplate désactivé.
    /// Supporte label seul ou icône + label (Gate Missions polish).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class TabBarUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string TemplateName = "TabItemTemplate";
        private const string FillChildName = "Fill";
        private const string LabelChildName = "Label";
        private const string IconChildName = "Icon";
        private const string IconGlyphChildName = "IconGlyph";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Sprites 9-slice (pill = S)")]
        [SerializeField] private Sprite roundedSpriteS;

        [Header("Template (enfant désactivé, trouvé par nom si vide)")]
        [SerializeField] private GameObject tabItemTemplate;

        [Header("Hauteur fixe (0 = TouchTargetMin)")]
        [SerializeField] private float fixedItemHeight;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<TabItem> _items = new List<TabItem>(8);
        private HorizontalLayoutGroup _layout;
        private int _selectedIndex = -1;
        private Action<int> _onSelectedCallback;

        private struct TabItem
        {
            public GameObject Root;
            public Image Border;
            public Image Fill;
            public Image Icon;
            public TextMeshProUGUI IconGlyph;
            public TextMeshProUGUI Label;
            public Button Button;
        }

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action<int> OnTabSelected;

        public int SelectedIndex => _selectedIndex;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            EnsureLayout();
            ResolveTemplate();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void SetRoundedSprite(Sprite spriteS)
        {
            roundedSpriteS = spriteS;
        }

        public void SetFixedItemHeight(float height)
        {
            fixedItemHeight = height;
        }

        /// <summary>
        /// (Re)génère les onglets à partir des labels. Idempotent.
        /// </summary>
        public void Init(IReadOnlyList<string> labels, Action<int> onSelected, int defaultIndex = 0)
        {
            Init(labels, null, null, onSelected, defaultIndex);
        }

        /// <summary>
        /// Labels + icônes sprite (null = glyphe texte si fourni).
        /// </summary>
        public void Init(
            IReadOnlyList<string> labels,
            IReadOnlyList<Sprite> icons,
            IReadOnlyList<string> iconGlyphs,
            Action<int> onSelected,
            int defaultIndex = 0)
        {
            EnsureLayout();
            ResolveTemplate();
            if (tabItemTemplate == null)
            {
                Debug.LogError("[TabBarUI] TabItemTemplate introuvable.");
                return;
            }

            _onSelectedCallback = onSelected;
            ClearItems();

            if (labels == null || labels.Count == 0)
                return;

            tabItemTemplate.SetActive(false);

            for (int i = 0; i < labels.Count; i++)
            {
                Sprite icon = icons != null && i < icons.Count ? icons[i] : null;
                string glyph = iconGlyphs != null && i < iconGlyphs.Count ? iconGlyphs[i] : null;
                TabItem item = CreateItem(labels[i], icon, glyph, i);
                _items.Add(item);
            }

            int index = Mathf.Clamp(defaultIndex, 0, _items.Count - 1);
            SetSelected(index, notify: false);
        }

        public void SetSelected(int index)
        {
            SetSelected(index, notify: true);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void SetSelected(int index, bool notify)
        {
            if (_items.Count == 0)
                return;

            index = Mathf.Clamp(index, 0, _items.Count - 1);
            _selectedIndex = index;

            for (int i = 0; i < _items.Count; i++)
                ApplyItemVisual(_items[i], selected: i == index);

            if (notify)
            {
                OnTabSelected?.Invoke(index);
                _onSelectedCallback?.Invoke(index);
            }
        }

        private void ApplyItemVisual(TabItem item, bool selected)
        {
            if (item.Border != null)
            {
                item.Border.sprite = roundedSpriteS;
                item.Border.type = Image.Type.Sliced;
                item.Border.color = selected ? UiTheme.AccentAmber : UiTheme.BorderSubtle;
            }

            if (item.Fill != null)
            {
                item.Fill.sprite = roundedSpriteS;
                item.Fill.type = Image.Type.Sliced;
                float inset = selected ? UiTheme.BorderFocus : UiTheme.BorderThin;
                ApplyInset(item.Fill.rectTransform, inset);
                item.Fill.color = selected ? UiTheme.BgElevated : new Color(0f, 0f, 0f, 0f);
            }

            Color labelColor = selected ? UiTheme.TextPrimary : UiTheme.TextSecondary;
            Color iconColor = selected ? UiTheme.AccentAmber : UiTheme.TextMuted;

            if (item.Label != null)
            {
                item.Label.color = labelColor;
                item.Label.fontSize = UiTypography.Caption;
            }

            if (item.Icon != null)
                item.Icon.color = iconColor;

            if (item.IconGlyph != null)
                item.IconGlyph.color = iconColor;
        }

        private TabItem CreateItem(string labelText, Sprite iconSprite, string glyph, int index)
        {
            GameObject instance = Instantiate(tabItemTemplate, transform);
            instance.name = "Tab_" + index;
            instance.SetActive(true);

            Image border = instance.GetComponent<Image>();
            Transform fillTx = instance.transform.Find(FillChildName);
            Image fill = fillTx != null ? fillTx.GetComponent<Image>() : null;

            Transform content = instance.transform.Find("Content");
            Transform searchRoot = content != null ? content : instance.transform;

            Transform labelTx = searchRoot.Find(LabelChildName);
            if (labelTx == null)
                labelTx = instance.transform.Find(LabelChildName);
            TextMeshProUGUI label = labelTx != null ? labelTx.GetComponent<TextMeshProUGUI>() : null;

            Transform iconTx = searchRoot.Find(IconChildName);
            Image icon = iconTx != null ? iconTx.GetComponent<Image>() : null;

            Transform glyphTx = searchRoot.Find(IconGlyphChildName);
            TextMeshProUGUI iconGlyph = glyphTx != null ? glyphTx.GetComponent<TextMeshProUGUI>() : null;

            Button button = instance.GetComponent<Button>();
            if (button == null)
                button = instance.AddComponent<Button>();

            if (label != null)
            {
                label.text = labelText;
                label.raycastTarget = false;
                label.fontSize = UiTypography.Caption;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (icon != null)
            {
                bool hasSprite = iconSprite != null;
                icon.gameObject.SetActive(hasSprite);
                if (hasSprite)
                {
                    icon.sprite = iconSprite;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                }
            }

            if (iconGlyph != null)
            {
                bool useGlyph = iconSprite == null && !string.IsNullOrEmpty(glyph);
                iconGlyph.gameObject.SetActive(useGlyph);
                if (useGlyph)
                {
                    iconGlyph.text = glyph;
                    iconGlyph.raycastTarget = false;
                }
            }

            float h = fixedItemHeight > 1f ? fixedItemHeight : UiTheme.TouchTargetMin;
            LayoutElement le = instance.GetComponent<LayoutElement>();
            if (le == null)
                le = instance.AddComponent<LayoutElement>();
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            if (fill != null)
                button.targetGraphic = fill;
            else if (border != null)
                button.targetGraphic = border;

            button.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = button.colors;
            cb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.disabledColor = Color.white;
            button.colors = cb;

            ApplyRaycasts(instance, button.targetGraphic);

            int captured = index;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SetSelected(captured, notify: true));

            return new TabItem
            {
                Root = instance,
                Border = border,
                Fill = fill,
                Icon = icon,
                IconGlyph = iconGlyph,
                Label = label,
                Button = button
            };
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Root != null)
                {
                    if (Application.isPlaying)
                        Destroy(_items[i].Root);
                    else
                        DestroyImmediate(_items[i].Root);
                }
            }

            _items.Clear();
            _selectedIndex = -1;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null || child.name == TemplateName)
                    continue;
                if (tabItemTemplate != null && child.gameObject == tabItemTemplate)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private void EnsureLayout()
        {
            if (_layout == null)
                _layout = GetComponent<HorizontalLayoutGroup>();

            if (_layout == null)
                _layout = gameObject.AddComponent<HorizontalLayoutGroup>();

            _layout.spacing = UiTheme.Space2;
            _layout.childAlignment = TextAnchor.MiddleCenter;
            _layout.childControlWidth = true;
            _layout.childControlHeight = true;
            _layout.childForceExpandWidth = true;
            _layout.childForceExpandHeight = true;
            _layout.padding = new RectOffset(0, 0, 0, 0);
        }

        private void ResolveTemplate()
        {
            if (tabItemTemplate != null)
                return;

            Transform found = transform.Find(TemplateName);
            if (found != null)
                tabItemTemplate = found.gameObject;
        }

        private static void ApplyInset(RectTransform rt, float inset)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void ApplyRaycasts(GameObject root, Graphic target)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                    images[i].raycastTarget = images[i] == target;
            }

            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                    texts[i].raycastTarget = false;
            }
        }
    }
}
