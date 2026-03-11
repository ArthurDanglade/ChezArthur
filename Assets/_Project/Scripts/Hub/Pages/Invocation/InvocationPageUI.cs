using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gacha;
using ChezArthur.Core;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Page principale d'invocation avec la liste des bannières.
    /// </summary>
    public class InvocationPageUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Bannières")]
        [SerializeField] private Transform bannersContainer;
        [SerializeField] private BannerCardUI bannerCardPrefab;
        [SerializeField] private List<BannerData> activeBanners = new List<BannerData>();

        [Header("Popups")]
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
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Rafraîchit la liste des bannières.
        /// </summary>
        public void RefreshBanners()
        {
            // Supprime les anciennes
            foreach (var banner in _spawnedBanners)
            {
                if (banner != null)
                    Destroy(banner.gameObject);
            }
            _spawnedBanners.Clear();

            // Crée les nouvelles
            foreach (var bannerData in activeBanners)
            {
                if (bannerData == null || !bannerData.IsActive()) continue;

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
            if (PersistentManager.Instance == null || PersistentManager.Instance.Gacha == null) return;

            var result = PersistentManager.Instance.Gacha.PullSingle(banner);
            if (result != null && gachaAnimationController != null)
            {
                gachaAnimationController.StartAnimation(result);
            }
        }

        private void OnPullMulti(BannerData banner)
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Gacha == null) return;

            var result = PersistentManager.Instance.Gacha.PullMulti(banner);
            if (result != null && gachaAnimationController != null)
            {
                gachaAnimationController.StartAnimation(result);
            }
        }

        private void OnShowRates(BannerData banner)
        {
            if (ratesPopup != null)
            {
                ratesPopup.Show(banner);
            }
        }

        private void OnShowRateUp(BannerData banner)
        {
            if (rateUpPopup != null)
            {
                rateUpPopup.Show(banner);
            }
        }
    }
}
