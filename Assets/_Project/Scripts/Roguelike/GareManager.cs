using System;
using System.Collections.Generic;
using System.Text;
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
        private const int GARE_SLOT_COUNT = 7;
        private const int GARE_FAVORED_OFFER_COUNT = 6;
        private const float DEFAULT_POST_GAME_CHANCE = 0.15f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Pools de données")]
        [SerializeField] private List<ValiseData> allValises = new List<ValiseData>();
        [SerializeField] private List<ItemData> allItems = new List<ItemData>();

        [Header("Coûts des soins")]
        [SerializeField] private int healSmallBaseCost = 50;
        [SerializeField] private int healMediumBaseCost = 120;
        [SerializeField] private int healLargeBaseCost = 250;

        [Header("Coûts des offres")]
        [SerializeField] private int valiseNewBaseCost = 80;
        [SerializeField] private int valiseUpgradeBaseCost = 60;
        [SerializeField] private int itemBaseCost = 100;

        [Header("Pondération offres Gare")]
        [SerializeField] private int offerWeightUpgrade = 55;
        [SerializeField] private int offerWeightItem = 30;
        [SerializeField] private int offerWeightNewValise = 15;
        [SerializeField] private int maxNewValisePerGare = 2;

        [Header("Rareté upgrade Gare (favorisée vers le haut)")]
        [SerializeField] private int rarityWeightCommune = 20;
        [SerializeField] private int rarityWeightRare = 35;
        [SerializeField] private int rarityWeightEpique = 30;
        [SerializeField] private int rarityWeightLegendaire = 15;

        [Header("Run infinie")]
        [SerializeField] private float postGameGareChance = DEFAULT_POST_GAME_CHANCE;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<GareSlotData> _currentSlots = new List<GareSlotData>();
        private int _healSmallPurchaseCount;
        private int _healMediumPurchaseCount;
        private int _healLargePurchaseCount;

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
        }

        /// <summary>
        /// Génère le contenu complet de la Gare courante (7 slots : 1 heal + 6 offres favorisées).
        /// </summary>
        public void GenerateGare()
        {
            _currentSlots.Clear();

            List<ValiseData> newValiseCandidates = BuildNewValiseCandidates();
            List<ValiseData> upgradeCandidates = BuildUpgradeCandidates();
            List<ItemData> itemCandidates = BuildItemCandidates();
            int newValiseUsed = 0;

            AddHealSlot(GareSlotType.HealSmall);

            for (int i = 0; i < GARE_FAVORED_OFFER_COUNT; i++)
            {
                AddFavoredOfferSlot(
                    newValiseCandidates,
                    upgradeCandidates,
                    itemCandidates,
                    ref newValiseUsed);
            }

            LogGareComposition();
            OnGareGenerated?.Invoke();
        }

        /// <summary>
        /// Indique si un slot peut être acheté (sans effet de bord).
        /// </summary>
        public bool CanPurchase(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _currentSlots.Count)
                return false;

            GareSlotData slot = _currentSlots[slotIndex];
            if (slot.IsPurchased)
                return false;

            if (IsHealSlotType(slot.SlotType) && !CanPurchaseHeal())
                return false;

            if (RunManager.Instance == null || RunManager.Instance.TalsEarned < slot.Cost)
                return false;

            return true;
        }

        /// <summary>
        /// Tente d'acheter un slot de la Gare.
        /// </summary>
        public bool TryPurchase(int slotIndex)
        {
            if (!CanPurchase(slotIndex))
                return false;

            GareSlotData slot = _currentSlots[slotIndex];

            if (!RunManager.Instance.SpendTals(slot.Cost))
                return false;

            ApplySlotEffect(slot);
            if (IsHealSlotType(slot.SlotType))
                slot.SetCost(GetSlotCost(slot));
            else
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

        private List<ValiseData> BuildNewValiseCandidates()
        {
            List<ValiseData> candidates = new List<ValiseData>();
            for (int i = 0; i < allValises.Count; i++)
            {
                ValiseData valise = allValises[i];
                if (valise != null && valise.CanAppearInGare
                    && (ValiseManager.Instance == null || !ValiseManager.Instance.IsValiseActive(valise.Id)))
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
                    continue;

                ValiseInstance active = ValiseManager.Instance != null
                    ? ValiseManager.Instance.GetActiveValise(valise.Id) : null;
                if (active != null && !active.IsAtMaxLevel)
                    candidates.Add(valise);
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

        private void AddFavoredOfferSlot(
            List<ValiseData> newValiseCandidates,
            List<ValiseData> upgradeCandidates,
            List<ItemData> itemCandidates,
            ref int newValiseUsed)
        {
            bool canUpgrade = upgradeCandidates.Count > 0;
            bool canItem = itemCandidates.Count > 0;
            bool canNew = newValiseCandidates.Count > 0 &&
                (newValiseUsed < maxNewValisePerGare || (!canUpgrade && !canItem));

            if (!canUpgrade && !canItem && !canNew)
            {
                AddHealSlot(GareSlotType.HealSmall);
                return;
            }

            int totalWeight = 0;
            if (canUpgrade) totalWeight += offerWeightUpgrade;
            if (canItem) totalWeight += offerWeightItem;
            if (canNew) totalWeight += offerWeightNewValise;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            if (canUpgrade)
            {
                if (roll < offerWeightUpgrade)
                {
                    AddUpgradeOfferSlot(upgradeCandidates);
                    return;
                }

                roll -= offerWeightUpgrade;
            }

            if (canItem)
            {
                if (roll < offerWeightItem)
                {
                    AddItemOfferSlot(itemCandidates);
                    return;
                }

                roll -= offerWeightItem;
            }

            if (canNew)
            {
                AddNewValiseOfferSlot(newValiseCandidates);
                newValiseUsed++;
            }
        }

        private void AddNewValiseOfferSlot(List<ValiseData> newValiseCandidates)
        {
            ValiseData selected = PullRandomAndRemove(newValiseCandidates);
            GareSlotData rawSlot = GareSlotData.CreateNewValise(selected, 0);
            int cost = GetSlotCost(rawSlot);
            _currentSlots.Add(GareSlotData.CreateNewValise(selected, cost));
        }

        private void AddUpgradeOfferSlot(List<ValiseData> upgradeCandidates)
        {
            ValiseData selected = PullRandomAndRemove(upgradeCandidates);
            ValiseImprovementRarity rarity = GetFavoredUpgradeRarity();
            GareSlotData rawSlot = GareSlotData.CreateValiseUpgrade(selected, rarity, 0);
            int cost = GetSlotCost(rawSlot);
            _currentSlots.Add(GareSlotData.CreateValiseUpgrade(selected, rarity, cost));
        }

        private void AddItemOfferSlot(List<ItemData> itemCandidates)
        {
            ItemData selectedItem = PullRandomAndRemove(itemCandidates);
            GareSlotData rawItemSlot = GareSlotData.CreateItem(selectedItem, 0);
            int itemCost = GetSlotCost(rawItemSlot);
            _currentSlots.Add(GareSlotData.CreateItem(selectedItem, itemCost));
        }

        private ValiseImprovementRarity GetFavoredUpgradeRarity()
        {
            int totalWeight = rarityWeightCommune + rarityWeightRare +
                rarityWeightEpique + rarityWeightLegendaire;
            if (totalWeight <= 0)
                return ValiseImprovementRarity.Commune;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            if (roll < rarityWeightCommune)
                return ValiseImprovementRarity.Commune;

            roll -= rarityWeightCommune;
            if (roll < rarityWeightRare)
                return ValiseImprovementRarity.Rare;

            roll -= rarityWeightRare;
            if (roll < rarityWeightEpique)
                return ValiseImprovementRarity.Epique;

            return ValiseImprovementRarity.Legendaire;
        }

        private static T PullRandomAndRemove<T>(List<T> list)
        {
            int index = UnityEngine.Random.Range(0, list.Count);
            T selected = list[index];
            list.RemoveAt(index);
            return selected;
        }

        private static bool IsHealSlotType(GareSlotType slotType)
        {
            return slotType == GareSlotType.HealSmall ||
                slotType == GareSlotType.HealMedium ||
                slotType == GareSlotType.HealLarge;
        }

        private bool CanPurchaseHeal()
        {
            return RunManager.Instance != null && RunManager.Instance.CanHealTeam();
        }

        private void LogGareComposition()
        {
            var log = new StringBuilder();
            log.AppendLine($"[Gare] Composition ({_currentSlots.Count}/{GARE_SLOT_COUNT} slots) :");

            for (int i = 0; i < _currentSlots.Count; i++)
            {
                log.Append("  [").Append(i).Append("] ").AppendLine(FormatSlotLog(_currentSlots[i]));
            }

            Debug.Log(log.ToString());
        }

        private static string FormatSlotLog(GareSlotData slot)
        {
            switch (slot.SlotType)
            {
                case GareSlotType.NewValise:
                    return $"NewValise | {slot.ValiseData?.ValiseName ?? "?"} | coût {slot.Cost}";
                case GareSlotType.ValiseUpgrade:
                    return $"ValiseUpgrade | {slot.ValiseData?.ValiseName ?? "?"} | {slot.UpgradeRarity} | coût {slot.Cost}";
                case GareSlotType.Item:
                    return $"Item | {slot.ItemData?.ItemName ?? "?"} | coût {slot.Cost}";
                case GareSlotType.HealSmall:
                    return $"HealSmall 10% | coût {slot.Cost}";
                case GareSlotType.HealMedium:
                    return $"HealMedium | coût {slot.Cost}";
                case GareSlotType.HealLarge:
                    return $"HealLarge | coût {slot.Cost}";
                default:
                    return $"{slot.SlotType} | coût {slot.Cost}";
            }
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
                    if (CanPurchaseHeal())
                    {
                        HealTeam(0.10f);
                        _healSmallPurchaseCount++;
                    }
                    break;

                case GareSlotType.HealMedium:
                    if (CanPurchaseHeal())
                    {
                        HealTeam(0.25f);
                        _healMediumPurchaseCount++;
                    }
                    break;

                case GareSlotType.HealLarge:
                    if (CanPurchaseHeal())
                    {
                        HealTeam(0.50f);
                        _healLargePurchaseCount++;
                    }
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
