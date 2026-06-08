using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Gère les items actifs, les contraintes de prise et le cache des stats directes.
    /// </summary>
    public class ItemManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int DEFAULT_MAX_SLOTS = 7;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Configuration")]
        [SerializeField] private int maxSlots = DEFAULT_MAX_SLOTS;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private ItemInstance[] _activeSlots;
        private HashSet<string> _takenItemIds = new HashSet<string>();
        private Dictionary<ValiseStatType, float> _cachedStatModifiers = new Dictionary<ValiseStatType, float>();
        private bool _cacheValid;
        private readonly List<ItemInstance> _activeSlotsView = new List<ItemInstance>();
        private ItemEffectContext _sharedItemContext;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static ItemManager Instance { get; private set; }

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action<ItemInstance> OnItemAdded;
        public event Action<ItemData> OnSacrificeRequired;
        public event Action<ItemInstance> OnItemSacrificed;
        public event Action<ItemInstance> OnItemConsumed;
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
        /// Initialise les slots, l'historique de prise et le cache pour un début de run.
        /// </summary>
        public void Initialize()
        {
            if (maxSlots <= 0)
            {
                maxSlots = DEFAULT_MAX_SLOTS;
            }

            _activeSlots = new ItemInstance[maxSlots];
            _takenItemIds.Clear();
            _activeSlotsView.Clear();

            // Réinitialise le contexte partagé des effets d'items
            if (_sharedItemContext == null)
                _sharedItemContext = new ItemEffectContext();
            _sharedItemContext.Clear();

            InvalidateCache();
            OnSlotsChanged?.Invoke();
        }

        /// <summary>
        /// Tente d'ajouter un item dans un slot libre.
        /// </summary>
        public bool TryAddItem(ItemData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id))
            {
                return false;
            }

            EnsureInitialized();

            if (!data.IsDownsideItem && _takenItemIds.Contains(data.Id))
            {
                return false;
            }

            int freeSlotIndex = FindFirstFreeSlotIndex();
            if (freeSlotIndex < 0)
            {
                OnSacrificeRequired?.Invoke(data);
                return false;
            }

            ItemInstance instance = new ItemInstance(data);
            _activeSlots[freeSlotIndex] = instance;

            if (!data.IsDownsideItem)
            {
                _takenItemIds.Add(data.Id);
            }

            InvalidateCache();
            OnItemAdded?.Invoke(instance);
            OnSlotsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Confirme un sacrifice pour libérer un slot et insérer l'item entrant.
        /// </summary>
        public void ConfirmSacrifice(int slotIndex, ItemData incomingData)
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

            if (!incomingData.IsDownsideItem && _takenItemIds.Contains(incomingData.Id))
            {
                return;
            }

            ItemInstance sacrificed = _activeSlots[slotIndex];
            if (sacrificed != null)
            {
                OnItemSacrificed?.Invoke(sacrificed);
            }

            ItemInstance incomingInstance = new ItemInstance(incomingData);
            _activeSlots[slotIndex] = incomingInstance;

            if (!incomingData.IsDownsideItem)
            {
                _takenItemIds.Add(incomingData.Id);
            }

            InvalidateCache();
            OnItemAdded?.Invoke(incomingInstance);
            OnSlotsChanged?.Invoke();
        }

        /// <summary>
        /// Retourne le total de modificateur direct pour une stat.
        /// </summary>
        public float GetDirectStatModifier(ValiseStatType statType)
        {
            EnsureInitialized();

            if (!_cacheValid)
            {
                RecalculateCache();
            }

            return _cachedStatModifiers.TryGetValue(statType, out float value) ? value : 0f;
        }

        /// <summary>
        /// Indique si au moins une instance active de l'item existe.
        /// </summary>
        public bool HasItem(string itemId)
        {
            return GetItemInstance(itemId) != null;
        }

        /// <summary>
        /// Indique si un item standard a déjà été pris pendant la run.
        /// </summary>
        public bool HasBeenTaken(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            EnsureInitialized();
            return _takenItemIds.Contains(itemId);
        }

        /// <summary>
        /// Retourne la première instance active de l'item, ou null si absente.
        /// </summary>
        public ItemInstance GetItemInstance(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            EnsureInitialized();

            for (int i = 0; i < _activeSlots.Length; i++)
            {
                ItemInstance instance = _activeSlots[i];
                if (instance != null && instance.Data != null && instance.Data.Id == itemId)
                {
                    return instance;
                }
            }

            return null;
        }

        /// <summary>
        /// Consomme la première instance active d'un item.
        /// </summary>
        public void ConsumeItem(string itemId)
        {
            ItemInstance instance = GetItemInstance(itemId);
            if (instance == null)
            {
                return;
            }

            instance.Consume();
            OnItemConsumed?.Invoke(instance);
        }

        /// <summary>
        /// Retourne la vue read-only des slots actifs non nuls.
        /// </summary>
        public IReadOnlyList<ItemInstance> GetActiveSlots()
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

        /// <summary>
        /// Notifie tous les items actifs d'un trigger.
        /// Chaque item actif dont le mainEffectId a un handler enregistré reçoit le trigger.
        /// </summary>
        public void NotifyTrigger(ItemTrigger trigger, ItemEffectContext context)
        {
            if (_activeSlots == null || ItemEffectRegistry.Instance == null) return;
            context.Trigger = trigger;
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                ItemInstance instance = _activeSlots[i];
                if (instance == null || !instance.CanTrigger()) continue;
                if (string.IsNullOrEmpty(instance.Data?.MainEffectId)) continue;

                IItemEffectHandler handler =
                    ItemEffectRegistry.Instance.GetHandler(instance.Data.MainEffectId);
                if (handler == null) continue;

                handler.OnTriggered(context, instance);
            }
        }

        /// <summary>
        /// Notifie tous les items actifs du début d'un étage.
        /// </summary>
        public void NotifyStageStart(ItemEffectContext context)
        {
            if (_activeSlots == null || ItemEffectRegistry.Instance == null) return;
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                ItemInstance instance = _activeSlots[i];
                if (instance == null) continue;
                if (string.IsNullOrEmpty(instance.Data?.MainEffectId)) continue;

                IItemEffectHandler handler =
                    ItemEffectRegistry.Instance.GetHandler(instance.Data.MainEffectId);
                if (handler == null) continue;

                handler.OnStageStart(context, instance);
            }
        }

        /// <summary>
        /// Retourne le bonus de régénération entre étages fourni par les items actifs.
        /// Additionne les contributions de tous les handlers PetiteBiere actifs.
        /// </summary>
        public float GetHealBetweenStagesBonus()
        {
            if (_activeSlots == null) return 0f;
            float bonus = 0f;
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                ItemInstance instance = _activeSlots[i];
                if (instance == null || !instance.CanTrigger()) continue;
                if (instance.Data?.MainEffectId != "petite_biere") continue;
                bonus += instance.Data.MainValue;
            }
            return bonus;
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
                ItemInstance instance = _activeSlots[i];
                if (instance == null || instance.Data == null)
                {
                    continue;
                }

                ItemData data = instance.Data;

                if (data.DirectStatType != ValiseStatType.None)
                {
                    AddToCache(data.DirectStatType, data.DirectStatValue);
                }

                if (data.DirectDownsideStatType != ValiseStatType.None)
                {
                    AddToCache(data.DirectDownsideStatType, -data.DirectDownsideStatValue);
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

            _activeSlots = new ItemInstance[Mathf.Max(1, maxSlots)];
            InvalidateCache();
        }
    }
}
