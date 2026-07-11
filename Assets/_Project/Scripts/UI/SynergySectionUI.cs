using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Peuple la section Synergies du menu pause (actives + possibles partielles).
    /// </summary>
    public class SynergySectionUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject synergyEntryPrefab;
        [SerializeField] private GameObject emptyLabel;
        [SerializeField] private RoguelikeSelectionPool valisePool;

        private readonly List<SynergyEntryUI> _entries = new List<SynergyEntryUI>();
        private readonly List<string> _nameBuffer = new List<string>();
        private readonly StringBuilder _comboBuilder = new StringBuilder(64);

        public void Refresh()
        {
            foreach (SynergyEntryUI entry in _entries)
                if (entry != null) Destroy(entry.gameObject);
            _entries.Clear();

            if (contentParent == null || synergyEntryPrefab == null)
                return;

            SynergyManager manager = SynergyManager.Instance;
            IReadOnlyList<SynergyData> allSynergies = manager != null ? manager.AllSynergies : null;
            if (allSynergies == null || allSynergies.Count == 0)
            {
                if (emptyLabel != null) emptyLabel.SetActive(true);
                return;
            }

            HashSet<string> activeValiseIds = BuildActiveValiseIdSet();
            int spawned = 0;

            spawned += SpawnGroup(allSynergies, activeValiseIds, manager, isActiveGroup: true);
            spawned += SpawnGroup(allSynergies, activeValiseIds, manager, isActiveGroup: false);

            if (emptyLabel != null) emptyLabel.SetActive(spawned == 0);
        }

        private int SpawnGroup(
            IReadOnlyList<SynergyData> allSynergies,
            HashSet<string> activeValiseIds,
            SynergyManager manager,
            bool isActiveGroup)
        {
            int count = 0;

            for (int i = 0; i < allSynergies.Count; i++)
            {
                SynergyData data = allSynergies[i];
                if (data == null || string.IsNullOrEmpty(data.Id))
                    continue;

                bool isActive = manager != null && manager.IsSynergyActive(data.Id);
                if (isActiveGroup)
                {
                    if (!isActive) continue;
                }
                else
                {
                    if (isActive || !HasAnyRequiredValiseInSlots(data, activeValiseIds))
                        continue;
                }

                string comboLine = BuildComboLine(data, activeValiseIds, isActive);
                GameObject go = Instantiate(synergyEntryPrefab, contentParent);
                SynergyEntryUI entry = go.GetComponent<SynergyEntryUI>();
                if (entry != null)
                {
                    entry.Setup(data, isActive, comboLine);
                    _entries.Add(entry);
                    count++;
                }
            }

            return count;
        }

        private static HashSet<string> BuildActiveValiseIdSet()
        {
            HashSet<string> ids = new HashSet<string>();
            if (ValiseManager.Instance == null)
                return ids;

            IReadOnlyList<ValiseInstance> slots = ValiseManager.Instance.GetActiveSlots();
            if (slots == null)
                return ids;

            for (int i = 0; i < slots.Count; i++)
            {
                ValiseInstance instance = slots[i];
                if (instance?.Data == null || string.IsNullOrEmpty(instance.Data.Id))
                    continue;
                ids.Add(instance.Data.Id);
            }

            return ids;
        }

        private static bool HasAnyRequiredValiseInSlots(SynergyData data, HashSet<string> activeValiseIds)
        {
            IReadOnlyList<string> required = data.RequiredValiseIds;
            if (required == null || required.Count == 0 || activeValiseIds.Count == 0)
                return false;

            for (int i = 0; i < required.Count; i++)
            {
                string id = required[i];
                if (!string.IsNullOrEmpty(id) && activeValiseIds.Contains(id))
                    return true;
            }

            return false;
        }

        private string BuildComboLine(SynergyData data, HashSet<string> activeValiseIds, bool isActive)
        {
            _comboBuilder.Clear();
            _nameBuffer.Clear();

            IReadOnlyList<string> required = data.RequiredValiseIds;
            if (required == null || required.Count == 0)
                return string.Empty;

            for (int i = 0; i < required.Count; i++)
            {
                string id = required[i];
                if (string.IsNullOrEmpty(id))
                    continue;

                if (_comboBuilder.Length > 0)
                    _comboBuilder.Append(" + ");

                _comboBuilder.Append(ResolveValiseName(id));
            }

            if (!isActive)
            {
                _nameBuffer.Clear();
                for (int i = 0; i < required.Count; i++)
                {
                    string id = required[i];
                    if (string.IsNullOrEmpty(id) || activeValiseIds.Contains(id))
                        continue;
                    _nameBuffer.Add(ResolveValiseName(id));
                }

                if (_nameBuffer.Count > 0)
                {
                    _comboBuilder.Append(" — manque : ");
                    for (int i = 0; i < _nameBuffer.Count; i++)
                    {
                        if (i > 0) _comboBuilder.Append(", ");
                        _comboBuilder.Append(_nameBuffer[i]);
                    }
                }
            }

            return _comboBuilder.ToString();
        }

        private string ResolveValiseName(string valiseId)
        {
            if (valisePool != null)
            {
                ValiseData data = valisePool.GetValiseDataById(valiseId);
                if (data != null)
                    return data.ValiseName;
            }

            Debug.LogWarning($"[SynergySectionUI] ValiseData introuvable pour id '{valiseId}'.");
            return valiseId;
        }
    }
}
