using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Bouton cyclique de tri collection : Rareté → Niveau → Récent.
    /// </summary>
    public class CollectionSortBar : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // ENUMS
        // ═══════════════════════════════════════════
        public enum SortMode
        {
            Rarity = 0,
            Level = 1,
            Recent = 2
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private Button cycleButton;
        [SerializeField] private TextMeshProUGUI label;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private SortMode _mode = SortMode.Rarity;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS / EVENTS
        // ═══════════════════════════════════════════
        public SortMode CurrentMode => _mode;

        public event Action<SortMode> OnSortModeChanged;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (cycleButton != null)
                cycleButton.onClick.AddListener(OnCycleClicked);
            RefreshLabel();
        }

        private void OnDestroy()
        {
            if (cycleButton != null)
                cycleButton.onClick.RemoveListener(OnCycleClicked);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Force un mode (outil / tests) sans event si inchangé.
        /// </summary>
        public void SetMode(SortMode mode, bool notify)
        {
            if (_mode == mode)
            {
                RefreshLabel();
                return;
            }

            _mode = mode;
            RefreshLabel();
            if (notify)
                OnSortModeChanged?.Invoke(_mode);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnCycleClicked()
        {
            int next = ((int)_mode + 1) % 3;
            _mode = (SortMode)next;
            RefreshLabel();
            OnSortModeChanged?.Invoke(_mode);
        }

        private void RefreshLabel()
        {
            if (label == null)
                return;

            label.fontSize = UiTypography.Label;
            label.color = UiTheme.TextPrimary;
            label.text = _mode switch
            {
                SortMode.Rarity => "Tri : Rareté v",
                SortMode.Level => "Tri : Niveau v",
                SortMode.Recent => "Tri : Récent v",
                _ => "Tri : Rareté v"
            };
        }
    }
}
