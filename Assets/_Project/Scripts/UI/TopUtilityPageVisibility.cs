using ChezArthur.Hub;
using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche TopUtilityRow (Shop / Lofi / News) uniquement sur la page Accueil.
    /// Chrome SafeRoot inchangé — simple SetActive selon HubManager.OnPageChanged.
    /// </summary>
    [DisallowMultipleComponent]
    public class TopUtilityPageVisibility : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string TopUtilityName = "TopUtilityRow";
        private const int AccueilPageIndex = 0;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private HubManager hub;
        [SerializeField] private GameObject topUtilityRow;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _listening;

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        /// <summary>
        /// Garantit le driver sur HubManager (idempotent).
        /// </summary>
        public static void EnsureOn(HubManager hubManager)
        {
            if (hubManager == null)
                return;

            TopUtilityPageVisibility driver =
                hubManager.GetComponent<TopUtilityPageVisibility>();
            if (driver == null)
                driver = hubManager.gameObject.AddComponent<TopUtilityPageVisibility>();

            driver.hub = hubManager;
            driver.ResolveRowIfNeeded();
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
            if (hub == null)
                hub = GetComponent<HubManager>();

            ResolveRowIfNeeded();
            StartListening();

            if (hub != null)
                Apply(hub.CurrentPageIndex);
        }

        private void StartListening()
        {
            if (_listening || hub == null)
                return;

            hub.OnPageChanged += Apply;
            _listening = true;
        }

        private void StopListening()
        {
            if (!_listening || hub == null)
                return;

            hub.OnPageChanged -= Apply;
            _listening = false;
        }

        private void Apply(int pageIndex)
        {
            if (topUtilityRow == null)
                return;

            bool show = pageIndex == AccueilPageIndex;
            if (topUtilityRow.activeSelf != show)
                topUtilityRow.SetActive(show);
        }

        private void ResolveRowIfNeeded()
        {
            if (topUtilityRow != null)
                return;

            // HubManager est souvent racine scène : ne pas limiter à transform.root.
            GameObject found = GameObject.Find(TopUtilityName);
            if (found != null)
            {
                topUtilityRow = found;
                return;
            }

            // Fallback : parcours de toutes les racines chargées (incl. inactifs partiels).
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t.name != TopUtilityName)
                    continue;
                if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded)
                    continue;
                topUtilityRow = t.gameObject;
                return;
            }
        }
    }
}
