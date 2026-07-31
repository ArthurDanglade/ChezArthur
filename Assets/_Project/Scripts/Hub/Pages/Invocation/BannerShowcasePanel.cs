using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.Gacha;
using ChezArthur.Hub.Pages;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Showcase Personnages d'un portail (Gate 6.c).
    /// </summary>
    public class BannerShowcasePanel : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Racine")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image scrim;
        [SerializeField] private Button backButton;

        [Header("Etage 1 — vedettes")]
        [SerializeField] private RectTransform featuredZone;
        [SerializeField] private ScrollRect featuredScroll;
        [SerializeField] private PortalSnapScroller featuredSnap;
        [SerializeField] private PortalSnapChrome featuredChrome;
        [SerializeField] private RectTransform featuredContent;
        [SerializeField] private ShowcaseFeaturedPage featuredPagePrefab;

        [Header("Etage 2 — pool")]
        [SerializeField] private RectTransform poolContent;
        [SerializeField] private TextMeshProUGUI poolEmptyLabel;
        [SerializeField] private TextMeshProUGUI featuredSectionTitle;
        [SerializeField] private TextMeshProUGUI poolSectionTitle;

        [Header("Layout")]
        [SerializeField] private ShowcaseLayoutFitter layoutFitter;

        [Header("Refs externes")]
        [SerializeField] private CharacterDetailPopup detailPopup;
        [SerializeField] private ScrollRect portalScrollToBlock;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<CharacterData> _etage1 = new List<CharacterData>(8);
        private readonly List<CharacterData> _etage2 = new List<CharacterData>(64);
        private readonly List<ShowcaseFeaturedPage> _pages = new List<ShowcaseFeaturedPage>(8);
        private readonly List<GameObject> _poolRows = new List<GameObject>(64);
        private bool _portalScrollWasEnabled = true;
        private Sprite _rowSprite;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            WireBackButton();
            if (scrim != null)
            {
                scrim.raycastTarget = true;
                Button b = scrim.GetComponent<Button>();
                if (b != null)
                    Destroy(b);
            }

            HideImmediate();
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(Close);
        }

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        public void BindDetailPopup(CharacterDetailPopup popup)
        {
            detailPopup = popup;
        }

        public void BindPortalScroll(ScrollRect scroll)
        {
            portalScrollToBlock = scroll;
        }

        public void SetRowSprite(Sprite sprite)
        {
            _rowSprite = sprite;
        }

        public void Open(BannerData banner)
        {
            if (banner == null)
                return;

            BannerRoster.SplitForShowcase(banner, _etage1, _etage2);
            if (featuredSectionTitle != null)
                featuredSectionTitle.text = "SSR du portail";
            if (poolSectionTitle != null)
                poolSectionTitle.text = "Liste des personnages obtenables";
            RebuildFeaturedPages();
            RebuildPoolList();
            SyncFeaturedDots(_etage1.Count);
            if (layoutFitter != null)
                layoutFitter.SetPageCount(_etage1.Count);
            SetPortalScrollBlocked(true);
            WireBackButton();
            SetVisible(true);

            if (featuredSnap != null)
            {
                featuredSnap.RecalculateMetrics();
                featuredSnap.SnapImmediate(0);
            }
        }

        public void Close()
        {
            ClearFeaturedPages();
            ClearPoolRows();
            SetPortalScrollBlocked(false);
            SetVisible(false);
        }

        // ═══════════════════════════════════════════
        // PRIVÉ — build
        // ═══════════════════════════════════════════

        private void RebuildFeaturedPages()
        {
            ClearFeaturedPages();
            if (featuredContent == null || featuredPagePrefab == null)
                return;

            float pageW = featuredScroll != null && featuredScroll.viewport != null
                ? featuredScroll.viewport.rect.width
                : 900f;
            if (pageW < 1f)
                pageW = 900f;

            for (int i = 0; i < _etage1.Count; i++)
            {
                ShowcaseFeaturedPage page = Instantiate(featuredPagePrefab, featuredContent);
                page.gameObject.SetActive(true);
                RectTransform rt = page.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.sizeDelta = new Vector2(pageW, 0f);
                }

                float originX = -i * pageW;
                page.Bind(_etage1[i], featuredContent, originX, OnOpenOwnedDetail);
                page.SetChipSprite(_rowSprite);
                LayoutElement le = page.GetComponent<LayoutElement>();
                if (le != null)
                    le.preferredWidth = pageW;
                _pages.Add(page);
            }

            if (layoutFitter != null)
                layoutFitter.MarkDirty();

            if (featuredSnap != null)
                featuredSnap.RecalculateMetrics();
        }

        private void SyncFeaturedDots(int count)
        {
            if (featuredZone == null)
                return;
            Transform dots = featuredZone.Find("DotIndicator");
            if (dots == null)
                return;
            for (int i = 0; i < dots.childCount; i++)
                dots.GetChild(i).gameObject.SetActive(i < count);
        }

        private void RebuildPoolList()
        {
            ClearPoolRows();
            if (poolContent == null)
                return;

            if (_etage2.Count == 0)
            {
                if (poolEmptyLabel != null)
                {
                    poolEmptyLabel.gameObject.SetActive(true);
                    poolEmptyLabel.text = _etage1.Count == 0
                        ? "Aucun personnage"
                        : "Pool vide";
                }

                return;
            }

            if (poolEmptyLabel != null)
                poolEmptyLabel.gameObject.SetActive(false);

            // Sections LR / SSR / SR.
            AppendSection(CharacterRarity.LR);
            AppendSection(CharacterRarity.SSR);
            AppendSection(CharacterRarity.SR);
        }

        private void AppendSection(CharacterRarity rarity)
        {
            bool any = false;
            for (int i = 0; i < _etage2.Count; i++)
            {
                if (_etage2[i] != null && _etage2[i].Rarity == rarity)
                {
                    any = true;
                    break;
                }
            }

            if (!any)
                return;

            CreateHeaderRow(rarity.ToString(), CharacterRarityPalette.GetColor(rarity));
            for (int i = 0; i < _etage2.Count; i++)
            {
                CharacterData c = _etage2[i];
                if (c == null || c.Rarity != rarity)
                    continue;
                CreatePoolRow(c);
            }
        }

        private void CreateHeaderRow(string title, Color color)
        {
            GameObject go = new GameObject("Section_" + title, typeof(RectTransform));
            go.transform.SetParent(poolContent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = UiTypography.Caption;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            _poolRows.Add(go);
        }

        private void CreatePoolRow(CharacterData data)
        {
            GameObject go = new GameObject("Row_" + data.Id, typeof(RectTransform));
            go.transform.SetParent(poolContent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 56f;

            if (_rowSprite != null)
            {
                Image bg = go.AddComponent<Image>();
                bg.sprite = _rowSprite;
                bg.type = Image.Type.Sliced;
                Color c = UiTheme.SurfaceBar;
                c.a = 0.55f;
                bg.color = c;
                bg.raycastTarget = false;
            }

            HorizontalLayoutGroup h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 10, 6, 6);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            h.spacing = 8f;

            TextMeshProUGUI name = CreateTmp(go.transform, "Name", data.CharacterName,
                UiTheme.TextPrimary, TextAlignmentOptions.Left);
            LayoutElement nLe = name.gameObject.AddComponent<LayoutElement>();
            nLe.flexibleWidth = 1f;
            nLe.minWidth = 80f;

            GameObject chips = new GameObject("SpecChips", typeof(RectTransform));
            chips.transform.SetParent(go.transform, false);
            LayoutElement chipsLe = chips.AddComponent<LayoutElement>();
            chipsLe.flexibleWidth = 0f;
            chipsLe.preferredWidth = -1f;
            HorizontalLayoutGroup chipsH = chips.AddComponent<HorizontalLayoutGroup>();
            chipsH.spacing = 6f;
            chipsH.childAlignment = TextAnchor.MiddleRight;
            chipsH.childControlWidth = true;
            chipsH.childControlHeight = true;
            chipsH.childForceExpandWidth = false;
            chipsH.childForceExpandHeight = true;
            ContentSizeFitter chipsCsf = chips.AddComponent<ContentSizeFitter>();
            chipsCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            chipsCsf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            ShowcaseSpecChips.Rebuild(
                chips.transform,
                data,
                _rowSprite,
                selectedSpecIndex: -1,
                interactive: false,
                onSelect: null);

            TextMeshProUGUI rarity = CreateTmp(go.transform, "Rarity", data.Rarity.ToString(),
                CharacterRarityPalette.GetColor(data.Rarity), TextAlignmentOptions.Right);
            LayoutElement rLe = rarity.gameObject.AddComponent<LayoutElement>();
            rLe.preferredWidth = 56f;

            _poolRows.Add(go);
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            string text,
            Color color,
            TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = UiTypography.Caption;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private void ClearFeaturedPages()
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] != null)
                    Destroy(_pages[i].gameObject);
            }

            _pages.Clear();

            if (featuredContent == null)
                return;
            for (int i = featuredContent.childCount - 1; i >= 0; i--)
                Destroy(featuredContent.GetChild(i).gameObject);
        }

        private void ClearPoolRows()
        {
            for (int i = 0; i < _poolRows.Count; i++)
            {
                if (_poolRows[i] != null)
                    Destroy(_poolRows[i]);
            }

            _poolRows.Clear();
        }

        private void OnOpenOwnedDetail(CharacterData data, OwnedCharacter owned)
        {
            if (detailPopup == null || data == null || owned == null)
                return;
            detailPopup.Open(data, owned);
        }

        private void SetPortalScrollBlocked(bool blocked)
        {
            if (portalScrollToBlock == null)
                return;
            if (blocked)
            {
                _portalScrollWasEnabled = portalScrollToBlock.enabled;
                portalScrollToBlock.enabled = false;
                PortalSnapScroller snap = portalScrollToBlock.GetComponent<PortalSnapScroller>();
                if (snap != null)
                    snap.enabled = false;
            }
            else
            {
                portalScrollToBlock.enabled = _portalScrollWasEnabled;
                PortalSnapScroller snap = portalScrollToBlock.GetComponent<PortalSnapScroller>();
                if (snap != null)
                    snap.enabled = true;
            }
        }

        private void WireBackButton()
        {
            if (backButton == null)
            {
                Transform header = transform.Find("Panel/Header");
                if (header == null)
                    header = transform.Find("Header");
                Transform backTx = header != null ? header.Find("BackButton") : null;
                if (backTx != null)
                    backButton = backTx.GetComponent<Button>();
            }

            if (backButton == null)
                return;

            backButton.onClick.RemoveListener(Close);
            backButton.onClick.AddListener(Close);
            backButton.transition = Selectable.Transition.None;
            backButton.interactable = true;

            PanelSurface surface = backButton.GetComponent<PanelSurface>();
            if (surface != null)
                surface.BlocksRaycasts = true;

            if (backButton.targetGraphic != null)
                backButton.targetGraphic.raycastTarget = true;

            Image[] images = backButton.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                    images[i].raycastTarget = true;
            }

            Transform icon = backButton.transform.Find("Icon");
            if (icon != null)
            {
                Image iconImg = icon.GetComponent<Image>();
                if (iconImg != null)
                    iconImg.raycastTarget = false;
            }
        }

        private void HideImmediate()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
