using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Peuple la section Valises du menu pause depuis ValiseManager.
    /// </summary>
    public class ValiseSectionUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject valiseEntryPrefab;
        [SerializeField] private GameObject emptyLabel; // optionnel : "Aucune valise"

        private readonly List<ValiseEntryUI> _entries = new List<ValiseEntryUI>();

        public void Refresh()
        {
            foreach (var e in _entries)
                if (e != null) Destroy(e.gameObject);
            _entries.Clear();

            if (contentParent == null || valiseEntryPrefab == null) return;

            var slots = ValiseManager.Instance != null
                ? ValiseManager.Instance.GetActiveSlots()
                : null;

            int count = slots != null ? slots.Count : 0;
            if (emptyLabel != null) emptyLabel.SetActive(count == 0);
            if (slots == null) return;

            foreach (var valise in slots)
            {
                if (valise == null) continue;
                GameObject go = Instantiate(valiseEntryPrefab, contentParent);
                var entry = go.GetComponent<ValiseEntryUI>();
                if (entry != null) { entry.Setup(valise); _entries.Add(entry); }
            }
        }
    }
}
