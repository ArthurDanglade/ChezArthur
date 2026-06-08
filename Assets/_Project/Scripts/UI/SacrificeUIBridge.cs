using UnityEngine;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Fait le lien entre les events de sacrifice des managers et l'UI de sacrifice.
    /// </summary>
    public class SacrificeUIBridge : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références UI")]
        [SerializeField] private SacrificeUI sacrificeUI;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _initialized;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise le bridge d'écoute des demandes de sacrifice.
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
                UnsubscribeAll();

            if (ValiseManager.Instance != null)
                ValiseManager.Instance.OnSacrificeRequired += OnValiseSacrificeRequired;
            if (ItemManager.Instance != null)
                ItemManager.Instance.OnSacrificeRequired += OnItemSacrificeRequired;

            _initialized = true;
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnValiseSacrificeRequired(ValiseData data, ValiseImprovementRarity rarity)
        {
            if (sacrificeUI == null) return;
            sacrificeUI.ShowForValise(data, rarity);
        }

        private void OnItemSacrificeRequired(ItemData data)
        {
            if (sacrificeUI == null) return;
            sacrificeUI.ShowForItem(data);
        }

        private void UnsubscribeAll()
        {
            if (ValiseManager.Instance != null)
                ValiseManager.Instance.OnSacrificeRequired -= OnValiseSacrificeRequired;
            if (ItemManager.Instance != null)
                ItemManager.Instance.OnSacrificeRequired -= OnItemSacrificeRequired;

            _initialized = false;
        }
    }
}
