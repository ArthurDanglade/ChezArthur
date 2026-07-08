using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Détecte les synergies actives selon les valises en slot et notifie les transitions.
    /// Aucun effet gameplay : la logique d'effet reste dans les systèmes existants.
    /// </summary>
    public class SynergyManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private List<SynergyData> allSynergies = new List<SynergyData>();

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly HashSet<string> _activeSynergyIds = new HashSet<string>();
        private readonly List<SynergyData> _activeSynergiesView = new List<SynergyData>();
        private readonly HashSet<SynergyData> _warnedInvalidSynergies = new HashSet<SynergyData>();
        private bool _subscribed;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static SynergyManager Instance { get; private set; }

        /// <summary> Synergies actuellement actives (ordre de allSynergies). </summary>
        public IReadOnlyList<SynergyData> ActiveSynergies => _activeSynergiesView;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand une synergie devient active. </summary>
        public event Action<SynergyData> OnSynergyActivated;

        /// <summary> Déclenché quand une synergie cesse d'être active. </summary>
        public event Action<SynergyData> OnSynergyDeactivated;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SubscribeAndSilentRecompute();
        }

        private void OnDestroy()
        {
            UnsubscribeFromValiseManager();

            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Indique si la synergie d'id donné est actuellement active.
        /// </summary>
        public bool IsSynergyActive(string synergyId)
        {
            if (string.IsNullOrEmpty(synergyId))
                return false;

            return _activeSynergyIds.Contains(synergyId);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void SubscribeAndSilentRecompute()
        {
            if (ValiseManager.Instance != null && !_subscribed)
            {
                ValiseManager.Instance.OnSlotsChanged += OnValiseSlotsChanged;
                _subscribed = true;
            }

            RecomputeActiveSynergies(silent: true);
        }

        private void UnsubscribeFromValiseManager()
        {
            if (!_subscribed || ValiseManager.Instance == null)
                return;

            ValiseManager.Instance.OnSlotsChanged -= OnValiseSlotsChanged;
            _subscribed = false;
        }

        private void OnValiseSlotsChanged()
        {
            RecomputeActiveSynergies(silent: false);
        }

        private void RecomputeActiveSynergies(bool silent)
        {
            HashSet<string> activeValiseIds = BuildActiveValiseIdSet();

            if (activeValiseIds.Count == 0)
            {
                _activeSynergyIds.Clear();
                _activeSynergiesView.Clear();
                return;
            }

            HashSet<string> nextActiveSynergyIds = new HashSet<string>();
            List<SynergyData> nextActiveSynergies = new List<SynergyData>();

            if (allSynergies != null)
            {
                for (int i = 0; i < allSynergies.Count; i++)
                {
                    SynergyData data = allSynergies[i];
                    if (data == null || string.IsNullOrEmpty(data.Id))
                        continue;

                    if (!IsSynergyRequirementsMet(data, activeValiseIds))
                        continue;

                    nextActiveSynergyIds.Add(data.Id);
                    nextActiveSynergies.Add(data);
                }
            }

            if (!silent)
            {
                if (allSynergies != null)
                {
                    for (int i = 0; i < allSynergies.Count; i++)
                    {
                        SynergyData data = allSynergies[i];
                        if (data == null || string.IsNullOrEmpty(data.Id))
                            continue;

                        if (!_activeSynergyIds.Contains(data.Id) || nextActiveSynergyIds.Contains(data.Id))
                            continue;

                        Debug.Log($"[Synergy] Rompue : {data.DisplayName} ({data.Id})");
                        OnSynergyDeactivated?.Invoke(data);
                    }

                    for (int i = 0; i < allSynergies.Count; i++)
                    {
                        SynergyData data = allSynergies[i];
                        if (data == null || string.IsNullOrEmpty(data.Id))
                            continue;

                        if (_activeSynergyIds.Contains(data.Id) || !nextActiveSynergyIds.Contains(data.Id))
                            continue;

                        Debug.Log($"[Synergy] Activée : {data.DisplayName} ({data.Id})");
                        OnSynergyActivated?.Invoke(data);
                    }
                }
            }

            _activeSynergyIds.Clear();
            for (int i = 0; i < nextActiveSynergies.Count; i++)
                _activeSynergyIds.Add(nextActiveSynergies[i].Id);

            _activeSynergiesView.Clear();
            for (int i = 0; i < nextActiveSynergies.Count; i++)
                _activeSynergiesView.Add(nextActiveSynergies[i]);
        }

        private HashSet<string> BuildActiveValiseIdSet()
        {
            HashSet<string> activeValiseIds = new HashSet<string>();

            if (ValiseManager.Instance == null)
                return activeValiseIds;

            IReadOnlyList<ValiseInstance> slots = ValiseManager.Instance.GetActiveSlots();
            if (slots == null)
                return activeValiseIds;

            for (int i = 0; i < slots.Count; i++)
            {
                ValiseInstance instance = slots[i];
                if (instance == null || instance.Data == null)
                    continue;

                string valiseId = instance.Data.Id;
                if (string.IsNullOrEmpty(valiseId))
                    continue;

                activeValiseIds.Add(valiseId);
            }

            return activeValiseIds;
        }

        private bool IsSynergyRequirementsMet(SynergyData data, HashSet<string> activeValiseIds)
        {
            IReadOnlyList<string> required = data.RequiredValiseIds;
            if (required == null || required.Count == 0)
            {
                if (!_warnedInvalidSynergies.Contains(data))
                {
                    _warnedInvalidSynergies.Add(data);
                    Debug.LogWarning(
                        $"[Synergy] {data.name} : RequiredValiseIds vide ou null — synergie ignorée.",
                        data);
                }

                return false;
            }

            for (int i = 0; i < required.Count; i++)
            {
                string valiseId = required[i];
                if (string.IsNullOrEmpty(valiseId) || !activeValiseIds.Contains(valiseId))
                    return false;
            }

            return true;
        }
    }
}
