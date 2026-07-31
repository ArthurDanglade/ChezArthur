using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gacha;
using ChezArthur.Core;
using ChezArthur.Hub.Pages;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Page Invocation — portails 6.b + showcase 6.c + tirage.
    /// </summary>
    public class InvocationPageUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Bannieres (legacy scroll)")]
        [SerializeField] private Transform bannersContainer;
        [SerializeField] private BannerCardUI bannerCardPrefab;
        [SerializeField] private List<BannerData> activeBanners = new List<BannerData>();

        [Header("Portails 6.b")]
        [SerializeField] private PortalCardUI[] portalCards = new PortalCardUI[0];
        [SerializeField] private BannerRatesPopup bannerRatesPopup;

        [Header("Showcase 6.c")]
        [SerializeField] private BannerShowcasePanel showcasePanel;
        [SerializeField] private CharacterDetailPopup detailPopup;

        [Header("Popups legacy")]
        [SerializeField] private PullResultPopupUI pullResultPopup;
        [SerializeField] private RatesPopupUI ratesPopup;
        [SerializeField] private RateUpPopupUI rateUpPopup;

        [Header("Animation Gacha")]
        [SerializeField] private GachaAnimationController gachaAnimationController;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<BannerCardUI> _spawnedBanners = new List<BannerCardUI>();

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            RefreshBanners();
            RefreshPortals();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void RefreshPortals()
        {
            if (portalCards == null || portalCards.Length == 0)
                return;

            Color[] placeholders =
            {
                new Color(0.32f, 0.36f, 0.48f, 0.95f),
                new Color(0.42f, 0.32f, 0.44f, 0.95f),
                new Color(0.28f, 0.40f, 0.40f, 0.95f),
                new Color(0.44f, 0.38f, 0.28f, 0.95f),
                new Color(0.34f, 0.30f, 0.48f, 0.95f)
            };

            int bannerCount = activeBanners != null ? activeBanners.Count : 0;
            for (int i = 0; i < portalCards.Length; i++)
            {
                PortalCardUI card = portalCards[i];
                if (card == null)
                    continue;

                BannerData data = i < bannerCount ? activeBanners[i] : null;
                if (data == null || !data.IsActive())
                {
                    card.gameObject.SetActive(false);
                    continue;
                }

                card.gameObject.SetActive(true);
                Color tint = placeholders[i % placeholders.Length];
                card.Bind(
                    data,
                    OnPullSingle,
                    OnPullMulti,
                    OnShowRates,
                    OnShowCharacters,
                    tint);
            }
        }

        public void RefreshBanners()
        {
            foreach (var banner in _spawnedBanners)
            {
                if (banner != null)
                    Destroy(banner.gameObject);
            }
            _spawnedBanners.Clear();

            if (bannersContainer == null
                || !bannersContainer.gameObject.activeInHierarchy
                || bannerCardPrefab == null)
                return;

            foreach (var bannerData in activeBanners)
            {
                if (bannerData == null || !bannerData.IsActive())
                    continue;

                BannerCardUI card = Instantiate(bannerCardPrefab, bannersContainer);
                card.Setup(bannerData, OnPullSingle, OnPullMulti, OnShowRates, OnShowRateUp);
                _spawnedBanners.Add(card);
            }
        }

        // ═══════════════════════════════════════════
        // CALLBACKS
        // ═══════════════════════════════════════════

        private void OnPullSingle(BannerData banner)
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Gacha == null)
            {
                Debug.LogError(
                    "[Invocation] PersistentManager ou Gacha null — tirage x1 impossible.",
                    this);
                return;
            }

            GachaPullResult result = PersistentManager.Instance.Gacha.PullSingle(banner);
            PresentPullResult(result, banner, false);
            RefreshPortalAffordability();
        }

        private void OnPullMulti(BannerData banner)
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Gacha == null)
            {
                Debug.LogError(
                    "[Invocation] PersistentManager ou Gacha null — tirage x10 impossible.",
                    this);
                return;
            }

            GachaPullResult result = PersistentManager.Instance.Gacha.PullMulti(banner);
            PresentPullResult(result, banner, true);
            RefreshPortalAffordability();
        }

        private void PresentPullResult(GachaPullResult result, BannerData banner, bool isMulti)
        {
            if (result == null)
                return;

            if (gachaAnimationController == null)
            {
                Debug.LogError(
                    "[Invocation] Controller absent — resultat sans animation",
                    this);
                return;
            }

            Debug.Log(
                "[Invocation] StartAnimation x" + (isMulti ? "10" : "1")
                + " banner=" + (banner != null ? banner.BannerId : "?"),
                this);

            if (!gachaAnimationController.StartAnimation(result, banner, isMulti))
                gachaAnimationController.ShowResultDirect(result, banner, isMulti);
        }

        private void OnShowRates(BannerData banner)
        {
            if (bannerRatesPopup != null)
            {
                bannerRatesPopup.Show(banner);
                return;
            }

            if (ratesPopup != null)
                ratesPopup.Show(banner);
        }

        private void OnShowCharacters(BannerData banner)
        {
            if (showcasePanel == null)
            {
                Debug.LogWarning("[Invocation] BannerShowcasePanel manquant.", this);
                return;
            }

            showcasePanel.Open(banner);
        }

        private void OnShowRateUp(BannerData banner)
        {
            if (rateUpPopup != null)
                rateUpPopup.Show(banner);
        }

        private void RefreshPortalAffordability()
        {
            if (portalCards == null)
                return;
            for (int i = 0; i < portalCards.Length; i++)
            {
                if (portalCards[i] != null)
                    portalCards[i].RefreshAffordability();
            }
        }
    }
}
