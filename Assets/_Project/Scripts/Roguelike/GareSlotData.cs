namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Type de contenu proposé dans un slot de Gare.
    /// </summary>
    public enum GareSlotType
    {
        NewValise,
        ValiseUpgrade,
        Item,
        HealSmall,
        HealMedium,
        HealLarge
    }

    /// <summary>
    /// Représente un slot de contenu généré dans la Gare.
    /// </summary>
    public class GareSlotData
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _isPurchased;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public GareSlotType SlotType { get; }
        public ValiseData ValiseData { get; }
        public ValiseImprovementRarity UpgradeRarity { get; }
        public ItemData ItemData { get; }
        public int Cost { get; private set; }
        public bool IsPurchased => _isPurchased;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Met à jour le coût affiché (soin répétable).
        /// </summary>
        public void SetCost(int newCost)
        {
            Cost = newCost;
        }

        /// <summary>
        /// Crée un slot contenant une nouvelle valise à acheter.
        /// </summary>
        public static GareSlotData CreateNewValise(ValiseData data, int cost)
        {
            return new GareSlotData(GareSlotType.NewValise, data, ValiseImprovementRarity.Commune, null, cost);
        }

        /// <summary>
        /// Crée un slot contenant une amélioration de valise.
        /// </summary>
        public static GareSlotData CreateValiseUpgrade(ValiseData data, ValiseImprovementRarity rarity, int cost)
        {
            return new GareSlotData(GareSlotType.ValiseUpgrade, data, rarity, null, cost);
        }

        /// <summary>
        /// Crée un slot contenant un item.
        /// </summary>
        public static GareSlotData CreateItem(ItemData data, int cost)
        {
            return new GareSlotData(GareSlotType.Item, null, ValiseImprovementRarity.Commune, data, cost);
        }

        /// <summary>
        /// Crée un slot de soin.
        /// </summary>
        public static GareSlotData CreateHeal(GareSlotType healType, int cost)
        {
            return new GareSlotData(healType, null, ValiseImprovementRarity.Commune, null, cost);
        }

        /// <summary>
        /// Marque le slot comme acheté.
        /// </summary>
        public void MarkAsPurchased()
        {
            _isPurchased = true;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private GareSlotData(
            GareSlotType slotType,
            ValiseData valiseData,
            ValiseImprovementRarity upgradeRarity,
            ItemData itemData,
            int cost)
        {
            SlotType = slotType;
            ValiseData = valiseData;
            UpgradeRarity = upgradeRarity;
            ItemData = itemData;
            Cost = cost;
            _isPurchased = false;
        }
    }
}
