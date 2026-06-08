using System;
using System.Collections.Generic;
using ChezArthur.Core;
using ChezArthur.Gameplay;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Gère la génération et les achats de la Gare inter-univers.
    /// </summary>
    public class GareManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float DEFAULT_POST_GAME_CHANCE = 0.15f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Pools de données")]
        [SerializeField] private List<ValiseData> allValises = new List<ValiseData>();
        [SerializeField] private List<ItemData> allItems = new List<ItemData>();

        [Header("Configuration slots")]
        [SerializeField] private int minSlots = 5;
        [SerializeField] private int maxSlots = 10;

        [Header("Coûts des soins")]
        [SerializeField] private int healSmallBaseCost = 50;
        [SerializeField] private int healMediumBaseCost = 120;
        [SerializeField] private int healLargeBaseCost = 250;

        [Header("Coûts des offres")]
        [SerializeField] private int valiseNewBaseCost = 80;
        [SerializeField] private int valiseUpgradeBaseCost = 60;
        [SerializeField] private int itemBaseCost = 100;

        [Header("Run infinie")]
        [SerializeField] private float postGameGareChance = DEFAULT_POST_GAME_CHANCE;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<GareSlotData> _currentSlots = new List<GareSlotData>();
        private int _healSmallPurchaseCount;
        private int _healMediumPurchaseCount;
        private int _healLargePurchaseCount;
        private List<ValiseImprovementRarity> _rarityWeightedPool = new List<ValiseImprovementRarity>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static GareManager Instance { get; private set; }
        public float PostGameGareChance => postGameGareChance;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action OnGareGenerated;
        public event Action<GareSlotData> OnSlotPurchased;
        public event Action OnGareClosed;

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
        /// Initialise l'état de la Gare pour une nouvelle run.
        /// </summary>
        public void Initialize()
        {
            _healSmallPurchaseCount = 0;
            _healMediumPurchaseCount = 0;
            _healLargePurchaseCount = 0;
            _currentSlots.Clear();

            if (_rarityWeightedPool.Count == 0)
            {
                BuildRarityWeightedPool();
            }
        }

        /// <summary>
        /// Génère le contenu complet de la Gare courante.
        /// </summary>
        public void GenerateGare()
        {
            _currentSlots.Clear();

            int clampedMin = Mathf.Max(3, minSlots);
            int clampedMax = Mathf.Max(clampedMin, maxSlots);
            int slotCount = UnityEngine.Random.Range(clampedMin, clampedMax + 1);
            int variableSlots = slotCount - 3;

            List<ValiseData> newValiseCandidates = BuildNewValiseCandidates();
            List<ValiseData> upgradeCandidates = BuildUpgradeCandidates();
            List<ItemData> itemCandidates = BuildItemCandidates();

            AddHealSlot(GareSlotType.HealSmall);
            AddHealSlot(GareSlotType.HealMedium);
            AddHealSlot(GareSlotType.HealLarge);

            for (int i = 0; i < variableSlots; i++)
            {
                AddRandomOfferSlot(newValiseCandidates, upgradeCandidates, itemCandidates);
            }

            OnGareGenerated?.Invoke();
        }

        /// <summary>
        /// Tente d'acheter un slot de la Gare.
        /// </summary>
        public bool TryPurchase(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _currentSlots.Count)
            {
                return false;
            }

            GareSlotData slot = _currentSlots[slotIndex];
            if (slot.IsPurchased)
            {
                return false;
            }

            if (RunManager.Instance == null || RunManager.Instance.TalsEarned < slot.Cost)
            {
                return false;
            }

            if (!RunManager.Instance.SpendTals(slot.Cost))
            {
                return false;
            }

            ApplySlotEffect(slot);
            slot.MarkAsPurchased();
            OnSlotPurchased?.Invoke(slot);
            return true;
        }

        /// <summary>
        /// Retourne les slots actuellement générés dans la Gare.
        /// </summary>
        public IReadOnlyList<GareSlotData> GetCurrentSlots()
        {
            return _currentSlots;
        }

        /// <summary>
        /// Ferme la Gare et reprend le flux de run.
        /// </summary>
        public void CloseGare()
        {
            OnGareClosed?.Invoke();

            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnGareClosed();
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private int GetSlotCost(GareSlotData slot)
        {
            switch (slot.SlotType)
            {
                case GareSlotType.NewValise:
                    return valiseNewBaseCost;
                case GareSlotType.ValiseUpgrade:
                    return valiseUpgradeBaseCost;
                case GareSlotType.Item:
                    return itemBaseCost;
                case GareSlotType.HealSmall:
                    return healSmallBaseCost * (1 + _healSmallPurchaseCount);
                case GareSlotType.HealMedium:
                    return healMediumBaseCost * (1 + _healMediumPurchaseCount);
                case GareSlotType.HealLarge:
                    return healLargeBaseCost * (1 + _healLargePurchaseCount);
                default:
                    return 0;
            }
        }

        private void BuildRarityWeightedPool()
        {
            for (int i = 0; i < 50; i++)
            {
                _rarityWeightedPool.Add(ValiseImprovementRarity.Commune);
            }

            for (int i = 0; i < 30; i++)
            {
                _rarityWeightedPool.Add(ValiseImprovementRarity.Rare);
            }

            for (int i = 0; i < 15; i++)
            {
                _rarityWeightedPool.Add(ValiseImprovementRarity.Epique);
            }

            for (int i = 0; i < 5; i++)
            {
                _rarityWeightedPool.Add(ValiseImprovementRarity.Legendaire);
            }
        }

        private List<ValiseData> BuildNewValiseCandidates()
        {
            List<ValiseData> candidates = new List<ValiseData>();
            for (int i = 0; i < allValises.Count; i++)
            {
                ValiseData valise = allValises[i];
                if (valise != null && valise.CanAppearInGare)
                {
                    candidates.Add(valise);
                }
            }

            return candidates;
        }

        private List<ValiseData> BuildUpgradeCandidates()
        {
            List<ValiseData> candidates = new List<ValiseData>();
            for (int i = 0; i < allValises.Count; i++)
            {
                ValiseData valise = allValises[i];
                if (valise == null || string.IsNullOrEmpty(valise.Id))
                {
                    continue;
                }

                if (ValiseManager.Instance != null && ValiseManager.Instance.IsValiseActive(valise.Id))
                {
                    candidates.Add(valise);
                }
            }

            return candidates;
        }

        private List<ItemData> BuildItemCandidates()
        {
            List<ItemData> candidates = new List<ItemData>();
            for (int i = 0; i < allItems.Count; i++)
            {
                ItemData item = allItems[i];
                if (item == null || string.IsNullOrEmpty(item.Id))
                {
                    continue;
                }

                if (item.IsDownsideItem)
                {
                    candidates.Add(item);
                    continue;
                }

                if (ItemManager.Instance == null || !ItemManager.Instance.HasBeenTaken(item.Id))
                {
                    candidates.Add(item);
                }
            }

            return candidates;
        }

        private void AddHealSlot(GareSlotType healType)
        {
            GareSlotData rawSlot = GareSlotData.CreateHeal(healType, 0);
            int cost = GetSlotCost(rawSlot);
            GareSlotData finalSlot = GareSlotData.CreateHeal(healType, cost);
            _currentSlots.Add(finalSlot);
        }

        private void AddRandomOfferSlot(
            List<ValiseData> newValiseCandidates,
            List<ValiseData> upgradeCandidates,
            List<ItemData> itemCandidates)
        {
            int newValiseCount = newValiseCandidates.Count;
            int upgradeCount = upgradeCandidates.Count;
            int itemCount = itemCandidates.Count;
            int totalCandidates = newValiseCount + upgradeCount + itemCount;

            if (totalCandidates <= 0)
            {
                AddHealSlot(GareSlotType.HealSmall);
                return;
            }

            int randomIndex = UnityEngine.Random.Range(0, totalCandidates);

            if (randomIndex < newValiseCount)
            {
                ValiseData selected = newValiseCandidates[randomIndex];
                GareSlotData rawSlot = GareSlotData.CreateNewValise(selected, 0);
                int cost = GetSlotCost(rawSlot);
                _currentSlots.Add(GareSlotData.CreateNewValise(selected, cost));
                return;
            }

            randomIndex -= newValiseCount;
            if (randomIndex < upgradeCount)
            {
                ValiseData selected = upgradeCandidates[randomIndex];
                ValiseImprovementRarity rarity = GetRandomUpgradeRarity();
                GareSlotData rawSlot = GareSlotData.CreateValiseUpgrade(selected, rarity, 0);
                int cost = GetSlotCost(rawSlot);
                _currentSlots.Add(GareSlotData.CreateValiseUpgrade(selected, rarity, cost));
                return;
            }

            randomIndex -= upgradeCount;
            ItemData selectedItem = itemCandidates[randomIndex];
            GareSlotData rawItemSlot = GareSlotData.CreateItem(selectedItem, 0);
            int itemCost = GetSlotCost(rawItemSlot);
            _currentSlots.Add(GareSlotData.CreateItem(selectedItem, itemCost));
        }

        private ValiseImprovementRarity GetRandomUpgradeRarity()
        {
            if (_rarityWeightedPool.Count == 0)
            {
                return ValiseImprovementRarity.Commune;
            }

            int index = UnityEngine.Random.Range(0, _rarityWeightedPool.Count);
            return _rarityWeightedPool[index];
        }

        private void ApplySlotEffect(GareSlotData slot)
        {
            switch (slot.SlotType)
            {
                case GareSlotType.NewValise:
                    if (ValiseManager.Instance != null && slot.ValiseData != null)
                    {
                        ValiseManager.Instance.TryAddValise(slot.ValiseData, ValiseImprovementRarity.Commune);
                    }
                    break;

                case GareSlotType.ValiseUpgrade:
                    if (ValiseManager.Instance != null && slot.ValiseData != null)
                    {
                        ValiseManager.Instance.TryAddValise(slot.ValiseData, slot.UpgradeRarity);
                    }
                    break;

                case GareSlotType.Item:
                    if (ItemManager.Instance != null && slot.ItemData != null)
                    {
                        ItemManager.Instance.TryAddItem(slot.ItemData);
                    }
                    break;

                case GareSlotType.HealSmall:
                    HealTeam(0.10f);
                    _healSmallPurchaseCount++;
                    break;

                case GareSlotType.HealMedium:
                    HealTeam(0.25f);
                    _healMediumPurchaseCount++;
                    break;

                case GareSlotType.HealLarge:
                    HealTeam(0.50f);
                    _healLargePurchaseCount++;
                    break;
            }
        }

        /// <summary>
        /// Applique un soin à toute l'équipe via RunManager.
        /// </summary>
        private void HealTeam(float ratio)
        {
            if (RunManager.Instance != null)
                RunManager.Instance.HealTeam(ratio);
        }
    }
}
