using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Peuple la section Items du menu pause depuis ItemManager.
    /// </summary>
    public class ItemSectionUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject itemEntryPrefab;
        [SerializeField] private GameObject emptyLabel;

        private readonly List<ItemEntryUI> _entries = new List<ItemEntryUI>();

        public void Refresh()
        {
            foreach (var e in _entries) if (e != null) Destroy(e.gameObject);
            _entries.Clear();

            if (contentParent == null || itemEntryPrefab == null) return;

            var slots = ItemManager.Instance != null ? ItemManager.Instance.GetActiveSlots() : null;
            int count = slots != null ? slots.Count : 0;
            if (emptyLabel != null) emptyLabel.SetActive(count == 0);
            if (slots == null) return;

            foreach (var item in slots)
            {
                if (item == null) continue;
                GameObject go = Instantiate(itemEntryPrefab, contentParent);
                var entry = go.GetComponent<ItemEntryUI>();
                if (entry != null) { entry.Setup(item); _entries.Add(entry); }
            }
        }
    }
}
