using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Type d'option présentée dans une sélection roguelike.
    /// </summary>
    public enum RoguelikeOptionType
    {
        ValiseNew,
        ValiseUpgrade,
        Item
    }

    /// <summary>
    /// Représente une option unique de sélection roguelike.
    /// </summary>
    public class RoguelikeOption
    {
        public RoguelikeOptionType Type;
        public ValiseData ValiseData;
        public ValiseImprovementRarity ValiseRarity;
        public ItemData ItemData;

        public static RoguelikeOption CreateValise(ValiseData data, ValiseImprovementRarity rarity, bool isUpgrade)
        {
            return new RoguelikeOption
            {
                Type = isUpgrade ? RoguelikeOptionType.ValiseUpgrade : RoguelikeOptionType.ValiseNew,
                ValiseData = data,
                ValiseRarity = rarity,
                ItemData = null
            };
        }

        public static RoguelikeOption CreateItem(ItemData data)
        {
            return new RoguelikeOption
            {
                Type = RoguelikeOptionType.Item,
                ValiseData = null,
                ValiseRarity = ValiseImprovementRarity.Commune,
                ItemData = data
            };
        }
    }

    /// <summary>
    /// Génère les options de sélection roguelike (valises/items).
    /// </summary>
    public class RoguelikeSelectionPool : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Pools sources")]
        [SerializeField] private List<ValiseData> allValises = new List<ValiseData>();
        [SerializeField] private List<ItemData> allItems = new List<ItemData>();

        [Header("Probabilités de rareté (valises)")]
        [SerializeField] private float communeChance = 0.50f;
        [SerializeField] private float rareChance = 0.30f;
        [SerializeField] private float epicChance = 0.15f;
        [SerializeField] private float legendaireChance = 0.05f;

        [Header("Règles de génération")]
        [SerializeField] [Range(0f, 1f)] private float itemSlotChance = 0.25f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<RoguelikeOption> _generatedOptions = new List<RoguelikeOption>();
        private readonly List<ValiseData> _valiseCandidates = new List<ValiseData>();
        private readonly List<ItemData> _itemCandidates = new List<ItemData>();
        private int _poolsWithoutEpicOrLegendaire;
        private bool _guaranteeEpicOrLegendaireNextPool;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Génère une liste d'options roguelike (valises/items) sans doublon.
        /// </summary>
        public List<RoguelikeOption> GenerateOptions(int count = 3)
        {
            if (count <= 0) count = 3;

            _generatedOptions.Clear();
            RebuildCandidates();
            if (_valiseCandidates.Count == 0) return _generatedOptions;

            bool hasEpicOrLegendaire = false;

            for (int i = 0; i < count; i++)
            {
                bool chooseItem = _itemCandidates.Count > 0 && Random.value < itemSlotChance;
                bool mustKeepValiseSlot = !HasAnyValiseOption(_generatedOptions) &&
                    (count - i) <= 1;
                if (mustKeepValiseSlot) chooseItem = false;

                RoguelikeOption option = chooseItem
                    ? TryCreateItemOption()
                    : TryCreateValiseOption(ref hasEpicOrLegendaire);

                if (option == null)
                    option = TryCreateValiseOption(ref hasEpicOrLegendaire) ?? TryCreateItemOption();
                if (option == null)
                    break;

                _generatedOptions.Add(option);
            }

            if (!HasAnyValiseOption(_generatedOptions))
                ForceOneValiseOption(ref hasEpicOrLegendaire);

            if (hasEpicOrLegendaire)
            {
                _poolsWithoutEpicOrLegendaire = 0;
            }
            else
            {
                _poolsWithoutEpicOrLegendaire++;
                if (ShouldEnableEpicGuarantee())
                    _guaranteeEpicOrLegendaireNextPool = true;
            }

            return _generatedOptions;
        }

        /// <summary>
        /// Notifie le pool du résultat d'une sélection valise pour le suivi de pity.
        /// </summary>
        public void NotifyValiseSelected(ValiseImprovementRarity rarity)
        {
            if (rarity == ValiseImprovementRarity.Epique || rarity == ValiseImprovementRarity.Legendaire)
            {
                _poolsWithoutEpicOrLegendaire = 0;
                _guaranteeEpicOrLegendaireNextPool = false;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void RebuildCandidates()
        {
            _valiseCandidates.Clear();
            _itemCandidates.Clear();

            for (int i = 0; i < allValises.Count; i++)
            {
                ValiseData valise = allValises[i];
                if (valise == null || string.IsNullOrEmpty(valise.Id)) continue;
                _valiseCandidates.Add(valise);
            }

            for (int i = 0; i < allItems.Count; i++)
            {
                ItemData item = allItems[i];
                if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                if (!item.IsDownsideItem && ItemManager.Instance != null && ItemManager.Instance.HasBeenTaken(item.Id))
                    continue;
                _itemCandidates.Add(item);
            }
        }

        private RoguelikeOption TryCreateValiseOption(ref bool hasEpicOrLegendaire)
        {
            int index = PullUniqueValiseCandidateIndex();
            if (index < 0) return null;

            ValiseData data = _valiseCandidates[index];
            bool isUpgrade = ValiseManager.Instance != null &&
                ValiseManager.Instance.GetActiveValise(data.Id) != null;
            ValiseImprovementRarity rarity = isUpgrade
                ? GetValiseRarity()
                : ValiseImprovementRarity.Commune;
            if (rarity == ValiseImprovementRarity.Epique || rarity == ValiseImprovementRarity.Legendaire)
                hasEpicOrLegendaire = true;

            return RoguelikeOption.CreateValise(data, rarity, isUpgrade);
        }

        private RoguelikeOption TryCreateItemOption()
        {
            int index = PullUniqueItemCandidateIndex();
            if (index < 0) return null;

            ItemData data = _itemCandidates[index];
            return RoguelikeOption.CreateItem(data);
        }

        private int PullUniqueValiseCandidateIndex()
        {
            int availableCount = 0;
            for (int i = 0; i < _valiseCandidates.Count; i++)
            {
                ValiseData candidate = _valiseCandidates[i];
                if (candidate == null) continue;
                if (ContainsValiseOption(_generatedOptions, candidate.Id)) continue;
                availableCount++;
            }
            if (availableCount <= 0) return -1;

            int pick = Random.Range(0, availableCount);
            int cursor = 0;
            for (int i = 0; i < _valiseCandidates.Count; i++)
            {
                ValiseData candidate = _valiseCandidates[i];
                if (candidate == null) continue;
                if (ContainsValiseOption(_generatedOptions, candidate.Id)) continue;
                if (cursor == pick) return i;
                cursor++;
            }
            return -1;
        }

        private int PullUniqueItemCandidateIndex()
        {
            int availableCount = 0;
            for (int i = 0; i < _itemCandidates.Count; i++)
            {
                ItemData candidate = _itemCandidates[i];
                if (candidate == null) continue;
                if (ContainsItemOption(_generatedOptions, candidate.Id)) continue;
                availableCount++;
            }
            if (availableCount <= 0) return -1;

            int pick = Random.Range(0, availableCount);
            int cursor = 0;
            for (int i = 0; i < _itemCandidates.Count; i++)
            {
                ItemData candidate = _itemCandidates[i];
                if (candidate == null) continue;
                if (ContainsItemOption(_generatedOptions, candidate.Id)) continue;
                if (cursor == pick) return i;
                cursor++;
            }
            return -1;
        }

        private void ForceOneValiseOption(ref bool hasEpicOrLegendaire)
        {
            int index = PullUniqueValiseCandidateIndex();
            if (index < 0) return;

            ValiseData data = _valiseCandidates[index];
            ValiseImprovementRarity rarity = GetValiseRarity();
            if (rarity == ValiseImprovementRarity.Epique || rarity == ValiseImprovementRarity.Legendaire)
                hasEpicOrLegendaire = true;

            bool isUpgrade = ValiseManager.Instance != null &&
                ValiseManager.Instance.GetActiveValise(data.Id) != null;
            RoguelikeOption option = RoguelikeOption.CreateValise(data, rarity, isUpgrade);

            if (_generatedOptions.Count > 0)
                _generatedOptions[0] = option;
            else
                _generatedOptions.Add(option);
        }

        private ValiseImprovementRarity GetValiseRarity()
        {
            bool guaranteeEpicOrLegend = _guaranteeEpicOrLegendaireNextPool;
            if (guaranteeEpicOrLegend)
                _guaranteeEpicOrLegendaireNextPool = false;

            GetEffectiveRarityWeights(
                out float localCommune,
                out float localRare,
                out float localEpic,
                out float localLegendaire);

            float totalWeight = localCommune + localRare + localEpic + localLegendaire;
            if (totalWeight > 0f)
            {
                Debug.Log(
                    $"[Pool] Chances : C {localCommune / totalWeight * 100f:0.#}% " +
                    $"R {localRare / totalWeight * 100f:0.#}% " +
                    $"E {localEpic / totalWeight * 100f:0.#}% " +
                    $"L {localLegendaire / totalWeight * 100f:0.#}%");
            }

            if (guaranteeEpicOrLegend)
            {
                float legendWeight = localLegendaire;
                float epicWeight = localEpic;
                float total = epicWeight + legendWeight;
                if (total <= 0f) return ValiseImprovementRarity.Epique;
                float rollGuaranteed = Random.value * total;
                return rollGuaranteed <= epicWeight
                    ? ValiseImprovementRarity.Epique
                    : ValiseImprovementRarity.Legendaire;
            }

            if (totalWeight <= 0f) return ValiseImprovementRarity.Commune;

            float roll = Random.value * totalWeight;
            if (roll <= localCommune) return ValiseImprovementRarity.Commune;
            roll -= localCommune;
            if (roll <= localRare) return ValiseImprovementRarity.Rare;
            roll -= localRare;
            if (roll <= localEpic) return ValiseImprovementRarity.Epique;
            return ValiseImprovementRarity.Legendaire;
        }

        private void GetEffectiveRarityWeights(
            out float localCommune,
            out float localRare,
            out float localEpic,
            out float localLegendaire)
        {
            localCommune = communeChance;
            localRare = rareChance;
            localEpic = epicChance;
            localLegendaire = legendaireChance;

            if (ValiseManager.Instance != null &&
                ValiseManager.Instance.IsValiseActive("valise_chance"))
            {
                localCommune = 0f;
                float total = localRare + localEpic + localLegendaire;
                if (total > 0f)
                {
                    localRare /= total;
                    localEpic /= total;
                    localLegendaire /= total;
                }
                else
                {
                    localRare = 0.70f;
                    localEpic = 0.20f;
                    localLegendaire = 0.10f;
                }
            }
        }

        private bool ShouldEnableEpicGuarantee()
        {
            if (_poolsWithoutEpicOrLegendaire < 3) return false;
            if (ValiseManager.Instance == null) return false;
            return ValiseManager.Instance.IsValiseActive("valise_chance") &&
                ValiseManager.Instance.IsValiseActive("valise_interet_compose");
        }

        private static bool HasAnyValiseOption(List<RoguelikeOption> options)
        {
            for (int i = 0; i < options.Count; i++)
            {
                RoguelikeOption option = options[i];
                if (option == null) continue;
                if (option.Type == RoguelikeOptionType.ValiseNew ||
                    option.Type == RoguelikeOptionType.ValiseUpgrade)
                    return true;
            }
            return false;
        }

        private static bool ContainsValiseOption(List<RoguelikeOption> options, string valiseId)
        {
            for (int i = 0; i < options.Count; i++)
            {
                RoguelikeOption option = options[i];
                if (option == null || option.ValiseData == null) continue;
                if (option.ValiseData.Id == valiseId) return true;
            }
            return false;
        }

        private static bool ContainsItemOption(List<RoguelikeOption> options, string itemId)
        {
            for (int i = 0; i < options.Count; i++)
            {
                RoguelikeOption option = options[i];
                if (option == null || option.ItemData == null) continue;
                if (option.ItemData.Id == itemId) return true;
            }
            return false;
        }
    }
}
