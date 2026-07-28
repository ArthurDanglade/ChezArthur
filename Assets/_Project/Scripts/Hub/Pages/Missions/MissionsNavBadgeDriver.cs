using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.Hub.Pages.Missions
{
    /// <summary>
    /// Badge nav « missions » vivant dès l'arrivée au Hub (page inactive OK).
    /// Ne modifie pas la structure nav — appelle seulement SetBadge.
    /// </summary>
    [DisallowMultipleComponent]
    public class MissionsNavBadgeDriver : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string NavTabId = "missions";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private HubNavBarUI _nav;
        private bool _listening;

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        /// <summary>
        /// Garantit un driver sur le HubManager (idempotent).
        /// </summary>
        public static void EnsureOn(HubManager hub)
        {
            if (hub == null)
                return;

            MissionsNavBadgeDriver driver = hub.GetComponent<MissionsNavBadgeDriver>();
            if (driver == null)
                driver = hub.gameObject.AddComponent<MissionsNavBadgeDriver>();

            driver.Activate();
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            Activate();
        }

        private void OnDisable()
        {
            StopListening();
        }

        private void OnDestroy()
        {
            StopListening();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Activate()
        {
            MissionsProviderReal.Shared.EnsureBound();
            StartListening();
            RefreshBadge();
        }

        private void StartListening()
        {
            if (_listening)
                return;

            MissionsProviderReal.Shared.OnChanged += RefreshBadge;
            _listening = true;
        }

        private void StopListening()
        {
            if (!_listening)
                return;

            MissionsProviderReal.Shared.OnChanged -= RefreshBadge;
            _listening = false;
        }

        private void RefreshBadge()
        {
            if (_nav == null)
                _nav = FindObjectOfType<HubNavBarUI>();

            if (_nav == null)
                return;

            _nav.SetBadge(NavTabId, MissionsProviderReal.Shared.HasAnyClaimable());
        }
    }
}
