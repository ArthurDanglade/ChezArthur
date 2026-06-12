using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Gère les valises actives, leur mémoire de sacrifice et le cache des modificateurs de stats.
    /// </summary>
    public class ValiseManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int DEFAULT_MAX_SLOTS = 3;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Configuration")]
        [SerializeField] private int maxSlots = DEFAULT_MAX_SLOTS;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private ValiseInstance[] _activeSlots;
        private Dictionary<string, ValiseInstance> _memorySlots = new Dictionary<string, ValiseInstance>();
        private Dictionary<ValiseStatType, float> _cachedStatModifiers = new Dictionary<ValiseStatType, float>();
        private bool _cacheValid;
        private readonly List<ValiseInstance> _activeSlotsView = new List<ValiseInstance>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static ValiseManager Instance { get; private set; }

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action<ValiseInstance> OnValiseAdded;
        public event Action<ValiseInstance> OnValiseUpgraded;
        public event Action<ValiseInstance, ValiseImprovementRarity> OnValiseUpgradedWithRarity;
        public event Action<ValiseData, ValiseImprovementRarity> OnSacrificeRequired;
        public event Action<ValiseInstance> OnValisesSacrificed;
        public event Action OnSlotsChanged;

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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Initialise les slots actifs et la mémoire pour un début de run.
        /// </summary>
        public void Initialize()
        {
            if (maxSlots <= 0)
            {
                maxSlots = DEFAULT_MAX_SLOTS;
            }

            _activeSlots = new ValiseInstance[maxSlots];
            _memorySlots.Clear();
            _activeSlotsView.Clear();
            InvalidateCache();
            OnSlotsChanged?.Invoke();
        }

        /// <summary>
        /// Tente d'ajouter ou d'améliorer une valise selon l'état courant des slots et de la mémoire.
        /// </summary>
        public bool TryAddValise(ValiseData data, ValiseImprovementRarity rarity)
        {
            if (data == null || string.IsNullOrEmpty(data.Id))
            {
                return false;
            }

            EnsureInitialized();

            ValiseInstance active = GetActiveValise(data.Id);

            if (active != null)
            {
                active.AddImprovement(rarity);
                InvalidateCache();
                OnValiseUpgradedWithRarity?.Invoke(active, rarity);
                OnValiseUpgraded?.Invoke(active);
                OnSlotsChanged?.Invoke();
                return true;
            }

            int freeSlotIndex = FindFirstFreeSlotIndex();

            if (freeSlotIndex < 0)
            {
                OnSacrificeRequired?.Invoke(data, rarity);
                return false;
            }

            ValiseInstance instanceToInsert;
            if (_memorySlots.TryGetValue(data.Id, out ValiseInstance memorizedInstance))
            {
                instanceToInsert = memorizedInstance;
                instanceToInsert.SetActive(true);
                _memorySlots.Remove(data.Id);
            }
            else
            {
                instanceToInsert = new ValiseInstance(data);
            }

            instanceToInsert.AddImprovement(rarity);
            _activeSlots[freeSlotIndex] = instanceToInsert;

            InvalidateCache();
            OnValiseAdded?.Invoke(instanceToInsert);
            OnSlotsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Confirme un sacrifice pour libérer un slot et insérer la valise entrante.
        /// </summary>
        public void ConfirmSacrifice(int slotIndex, ValiseData incomingData, ValiseImprovementRarity incomingRarity)
        {
            if (incomingData == null || string.IsNullOrEmpty(incomingData.Id))
            {
                return;
            }

            EnsureInitialized();

            if (slotIndex < 0 || slotIndex >= _activeSlots.Length)
            {
                return;
            }

            ValiseInstance sacrificed = _activeSlots[slotIndex];
            if (sacrificed != null && sacrificed.Data != null && !string.IsNullOrEmpty(sacrificed.Data.Id))
            {
                sacrificed.SetActive(false);
                _memorySlots[sacrificed.Data.Id] = sacrificed;
                OnValisesSacrificed?.Invoke(sacrificed);
            }

            ValiseInstance incomingInstance;
            if (_memorySlots.TryGetValue(incomingData.Id, out ValiseInstance memorizedIncoming))
            {
                incomingInstance = memorizedIncoming;
                incomingInstance.SetActive(true);
                _memorySlots.Remove(incomingData.Id);
            }
            else
            {
                incomingInstance = new ValiseInstance(incomingData);
            }

            incomingInstance.AddImprovement(incomingRarity);
            _activeSlots[slotIndex] = incomingInstance;

            InvalidateCache();
            OnValiseAdded?.Invoke(incomingInstance);
            OnSlotsChanged?.Invoke();
        }

        /// <summary>
        /// Retourne le modificateur total pour une stat donnée sur l'ensemble des valises actives.
        /// </summary>
        public float GetStatModifier(ValiseStatType statType)
        {
            EnsureInitialized();

            if (!_cacheValid)
            {
                RecalculateCache();
            }

            return _cachedStatModifiers.TryGetValue(statType, out float value) ? value : 0f;
        }

        /// <summary>
        /// Indique si une valise est active dans les slots.
        /// </summary>
        public bool IsValiseActive(string valiseId)
        {
            return GetActiveValise(valiseId) != null;
        }

        /// <summary>
        /// Indique si une valise est stockée en mémoire après sacrifice.
        /// </summary>
        public bool IsValiseInMemory(string valiseId)
        {
            if (string.IsNullOrEmpty(valiseId))
            {
                return false;
            }

            EnsureInitialized();
            return _memorySlots.ContainsKey(valiseId);
        }

        /// <summary>
        /// Retourne la valise mémorisée (sacrifiée) pour cet id, ou null. Lecture seule.
        /// </summary>
        public ValiseInstance GetMemorizedValise(string valiseId)
        {
            if (string.IsNullOrEmpty(valiseId)) return null;
            EnsureInitialized();
            return _memorySlots.TryGetValue(valiseId, out ValiseInstance instance) ? instance : null;
        }

        /// <summary>
        /// Retourne une valise active par son identifiant, ou null si absente.
        /// </summary>
        public ValiseInstance GetActiveValise(string valiseId)
        {
            if (string.IsNullOrEmpty(valiseId))
            {
                return null;
            }

            EnsureInitialized();

            for (int i = 0; i < _activeSlots.Length; i++)
            {
                ValiseInstance instance = _activeSlots[i];
                if (instance != null && instance.Data != null && instance.Data.Id == valiseId)
                {
                    return instance;
                }
            }

            return null;
        }

        /// <summary>
        /// Ajoute un stack de scaling à une valise active.
        /// </summary>
        public void AddStackToValise(string valiseId)
        {
            ValiseInstance instance = GetActiveValise(valiseId);
            if (instance == null)
            {
                return;
            }

            instance.AddStack();
            InvalidateCache();
            LogValiseStacks(instance);
        }

        /// <summary>
        /// Réinitialise les stacks de scaling d'une valise active.
        /// </summary>
        public void ResetStacksOnValise(string valiseId)
        {
            ValiseInstance instance = GetActiveValise(valiseId);
            if (instance == null)
            {
                return;
            }

            instance.ResetStacks();
            InvalidateCache();
            LogValiseStacks(instance);
        }

        /// <summary>
        /// Retourne la vue read-only des slots actifs non nuls.
        /// </summary>
        public IReadOnlyList<ValiseInstance> GetActiveSlots()
        {
            EnsureInitialized();

            _activeSlotsView.Clear();
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                if (_activeSlots[i] != null)
                {
                    _activeSlotsView.Add(_activeSlots[i]);
                }
            }

            return _activeSlotsView;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private int FindFirstFreeSlotIndex()
        {
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                if (_activeSlots[i] == null)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RecalculateCache()
        {
            _cachedStatModifiers.Clear();

            for (int i = 0; i < _activeSlots.Length; i++)
            {
                ValiseInstance instance = _activeSlots[i];
                if (instance == null || instance.Data == null)
                {
                    continue;
                }

                ValiseData data = instance.Data;

                if (data.Id == "valise_discipline" || data.Id == "valise_cameleon")
                    continue;

                if (data.BaseStatType == ValiseStatType.AllStats)
                {
                    float allStatsValue = instance.GetTotalStatValue();
                    AddToCache(ValiseStatType.ATK, allStatsValue);
                    AddToCache(ValiseStatType.DEF, allStatsValue);
                    AddToCache(ValiseStatType.HP, allStatsValue);
                    AddToCache(ValiseStatType.Speed, allStatsValue);
                }
                else if (data.BaseStatType != ValiseStatType.None)
                {
                    AddToCache(data.BaseStatType, instance.GetTotalStatValue());
                }

                if (data.HasSecondStat && data.SecondStatType != ValiseStatType.None)
                {
                    AddToCache(data.SecondStatType, instance.GetTotalSecondStatValue());
                }

                if (data.HasDownside && data.DownsideStatType != ValiseStatType.None)
                {
                    AddToCache(data.DownsideStatType, -instance.GetTotalDownsideValue());
                }
            }

            _cacheValid = true;
        }

        private void AddToCache(ValiseStatType statType, float value)
        {
            if (_cachedStatModifiers.TryGetValue(statType, out float currentValue))
            {
                _cachedStatModifiers[statType] = currentValue + value;
            }
            else
            {
                _cachedStatModifiers[statType] = value;
            }
        }

        private void InvalidateCache()
        {
            _cacheValid = false;
        }

        private void EnsureInitialized()
        {
            if (_activeSlots != null)
            {
                return;
            }

            _activeSlots = new ValiseInstance[Mathf.Max(1, maxSlots)];
            InvalidateCache();
        }

        private static void LogValiseStacks(ValiseInstance instance)
        {
            if (instance?.Data == null) return;
            Debug.Log($"[Valise] {instance.Data.ValiseName} stacks: {instance.InternalStacks}");
        }

    }
}
