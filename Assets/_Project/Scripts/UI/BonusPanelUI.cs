using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ChezArthur.Roguelike;
using TMPro;
namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche la liste des bonus collectés avec leurs stacks.
    /// </summary>
    public class BonusPanelUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject bonusEntryPrefab;
        [SerializeField] private TextMeshProUGUI emptyText; // "Aucun bonus collecté"

        private List<BonusEntryUI> _entries = new List<BonusEntryUI>();

        /// <summary>
        /// Rafraîchit la liste des bonus (groupés par type avec stack).
        /// </summary>
        public void Refresh()
        {
            // Nettoie les anciennes entrées
            foreach (var entry in _entries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }
            _entries.Clear();

            if (BonusManager.Instance == null || contentParent == null || bonusEntryPrefab == null) return;

            var activeBonuses = BonusManager.Instance.ActiveBonuses;

            // Affiche message si aucun bonus
            if (emptyText != null)
                emptyText.gameObject.SetActive(activeBonuses == null || activeBonuses.Count == 0);

            if (activeBonuses == null || activeBonuses.Count == 0) return;

            // Groupe les bonus par référence (même bonus = stack)
            var groupedBonuses = activeBonuses
                .GroupBy(b => b)
                .Select(g => new { Bonus = g.First(), Count = g.Count() });

            foreach (var group in groupedBonuses)
            {
                GameObject entryGO = Instantiate(bonusEntryPrefab, contentParent);
                BonusEntryUI entry = entryGO.GetComponent<BonusEntryUI>();

                if (entry != null)
                {
                    entry.Setup(group.Bonus, group.Count);
                    _entries.Add(entry);
                }
            }
        }
    }
}
