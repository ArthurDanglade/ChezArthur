using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Barre de navigation Hub définitive (Gate 2.2). Data-driven depuis TabTemplate.
    /// Remplace HubNavigationUI. Aucun appel direct à HubManager — via OnTabTapped.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HubNavBarUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string TemplateName = "TabTemplate";
        private const string TabsRowName = "TabsRow";
        private const string IconSlotName = "IconSlot";
        private const string IconName = "Icon";
        private const string LabelName = "Label";
        private const string ActiveLineName = "ActiveTopLine";
        private const string BadgeName = "Badge";
        private const float IconSlotSize = 64f;
        private const float IconScale = 4f;

        // ═══════════════════════════════════════════
        // TYPES
        // ═══════════════════════════════════════════
        [Serializable]
        public class TabDefinition
        {
            public string id;
            public string label;
            public Sprite icon;
            public int pageIndex;
        }

        private struct TabView
        {
            public string Id;
            public int PageIndex;
            public RectTransform Root;
            public Image Icon;
            public TextMeshProUGUI Label;
            public Image ActiveLine;
            public GameObject Badge;
            public Button Button;
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Données onglets")]
        [SerializeField] private List<TabDefinition> tabs = new List<TabDefinition>(4);

        [Header("Références (auto si vides)")]
        [SerializeField] private RectTransform tabsRow;
        [SerializeField] private GameObject tabTemplate;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<TabView> _views = new List<TabView>(4);
        private int _selectedPageIndex = -1;
        private bool _built;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Index de page demandé par l'utilisateur. </summary>
        public event Action<int> OnTabTapped;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            EnsureBuilt();
        }

        private void OnEnable()
        {
            EnsureBuilt();
            if (_selectedPageIndex >= 0)
                ApplyVisualState(_selectedPageIndex);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Sélection visuelle (sync après fade / init). </summary>
        public void Select(int pageIndex)
        {
            EnsureBuilt();
            _selectedPageIndex = pageIndex;
            ApplyVisualState(pageIndex);
        }

        /// <summary>
        /// Pastille AccentGold (mock Phase 6) — API définitive, non branchée métier.
        /// </summary>
        public void SetBadge(string tabId, bool visible)
        {
            EnsureBuilt();
            if (string.IsNullOrEmpty(tabId))
                return;

            for (int i = 0; i < _views.Count; i++)
            {
                if (_views[i].Id != tabId || _views[i].Badge == null)
                    continue;
                _views[i].Badge.SetActive(visible);
                return;
            }
        }

        /// <summary> Rebuild forcé (éditeur / builder après câblage). </summary>
        public void Rebuild()
        {
            _built = false;
            EnsureBuilt();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureBuilt()
        {
            if (_built)
                return;
            BuildTabs();
            _built = true;
        }

        private void BuildTabs()
        {
            ResolveRefs();
            ClearGeneratedTabs();
            _views.Clear();

            if (tabTemplate == null || tabsRow == null || tabs == null || tabs.Count == 0)
                return;

            tabTemplate.SetActive(false);

            int n = tabs.Count;
            for (int i = 0; i < n; i++)
            {
                TabDefinition def = tabs[i];
                if (def == null)
                    continue;

                GameObject go = Instantiate(tabTemplate, tabsRow);
                go.name = "Tab_" + (string.IsNullOrEmpty(def.id) ? i.ToString() : def.id);
                go.SetActive(true);

                RectTransform rt = go.transform as RectTransform;
                ConfigureTabStretch(rt, i, n);

                TabView view = BindTabView(go, def);
                ApplyPixelPerfectIcon(view.Icon, def.icon);
                if (view.Label != null)
                {
                    view.Label.text = def.label ?? string.Empty;
                    view.Label.fontSize = UiTypography.Caption;
                    view.Label.enableAutoSizing = false;
                    view.Label.overflowMode = TextOverflowModes.Overflow;
                }

                if (view.Badge != null)
                    view.Badge.SetActive(false);

                int pageIndex = def.pageIndex;
                if (view.Button != null)
                {
                    view.Button.onClick.RemoveAllListeners();
                    view.Button.onClick.AddListener(() => HandleTap(pageIndex));
                }

                _views.Add(view);
            }

            HubNavSafeBleed bleed = GetComponent<HubNavSafeBleed>();
            if (bleed != null && tabsRow != null)
                bleed.BindSafeBand(new[] { tabsRow });

            if (_selectedPageIndex >= 0)
                ApplyVisualState(_selectedPageIndex);
        }

        private void HandleTap(int pageIndex)
        {
            OnTabTapped?.Invoke(pageIndex);
        }

        private void ApplyVisualState(int pageIndex)
        {
            for (int i = 0; i < _views.Count; i++)
            {
                TabView v = _views[i];
                bool active = v.PageIndex == pageIndex;
                Color color = active ? UiTheme.TextPrimary : UiTheme.TextMuted;

                if (v.Icon != null)
                    v.Icon.color = color;
                if (v.Label != null)
                    v.Label.color = color;
                if (v.ActiveLine != null)
                {
                    v.ActiveLine.enabled = active;
                    v.ActiveLine.color = UiTheme.AccentAmber;
                }
            }
        }

        private void ResolveRefs()
        {
            if (tabsRow == null)
            {
                Transform row = transform.Find(TabsRowName);
                if (row != null)
                    tabsRow = row as RectTransform;
            }

            if (tabTemplate == null)
            {
                Transform t = transform.Find(TemplateName);
                if (t != null)
                    tabTemplate = t.gameObject;
            }
        }

        private void ClearGeneratedTabs()
        {
            if (tabsRow == null)
                return;

            for (int i = tabsRow.childCount - 1; i >= 0; i--)
            {
                Transform child = tabsRow.GetChild(i);
                if (child == null || child.name == TemplateName)
                    continue;
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private static void ConfigureTabStretch(RectTransform rt, int index, int count)
        {
            if (rt == null || count <= 0)
                return;

            float minX = index / (float)count;
            float maxX = (index + 1) / (float)count;
            rt.anchorMin = new Vector2(minX, 0f);
            rt.anchorMax = new Vector2(maxX, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static TabView BindTabView(GameObject go, TabDefinition def)
        {
            var view = new TabView
            {
                Id = def.id,
                PageIndex = def.pageIndex,
                Root = go.transform as RectTransform,
                Button = go.GetComponent<Button>()
            };

            Transform iconSlot = go.transform.Find(IconSlotName);
            if (iconSlot != null)
            {
                Transform iconTx = iconSlot.Find(IconName);
                if (iconTx != null)
                    view.Icon = iconTx.GetComponent<Image>();

                Transform badgeTx = iconSlot.Find(BadgeName);
                if (badgeTx != null)
                    view.Badge = badgeTx.gameObject;
            }

            Transform labelTx = go.transform.Find(LabelName);
            if (labelTx != null)
                view.Label = labelTx.GetComponent<TextMeshProUGUI>();

            Transform lineTx = go.transform.Find(ActiveLineName);
            if (lineTx != null)
                view.ActiveLine = lineTx.GetComponent<Image>();

            return view;
        }

        private static void ApplyPixelPerfectIcon(Image icon, Sprite sprite)
        {
            if (icon == null)
                return;

            icon.sprite = sprite;
            icon.preserveAspect = false;
            icon.type = Image.Type.Simple;
            icon.raycastTarget = false;

            RectTransform rt = icon.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            if (sprite != null)
            {
                float w = sprite.rect.width * IconScale;
                float h = sprite.rect.height * IconScale;
                rt.sizeDelta = new Vector2(w, h);
            }
            else
            {
                rt.sizeDelta = new Vector2(IconSlotSize, IconSlotSize);
            }
        }
    }
}
