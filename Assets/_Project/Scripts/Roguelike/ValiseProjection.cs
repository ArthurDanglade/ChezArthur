using ChezArthur.Core;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Projette la valeur d'une valise entrante telle qu'elle serait au moment de la prise :
    /// amélioration appliquée + stacks d'état projetés (Tals pour Fortune, items pour Équilibre).
    /// </summary>
    public static class ValiseProjection
    {
        public static float ProjectPrimaryValue(ValiseData incoming, ValiseImprovementRarity rarity)
        {
            if (incoming == null) return 0f;
            ValiseInstance seed = GetSeedInstance(incoming);
            int projectedStacks = ComputeProjectedStacks(seed);
            return seed.PeekStatValueAfterImprovement(rarity, projectedStacks);
        }

        public static float ProjectSecondValue(ValiseData incoming, ValiseImprovementRarity rarity)
        {
            if (incoming == null || !incoming.HasSecondStat) return 0f;
            ValiseInstance seed = GetSeedInstance(incoming);
            int projectedStacks = ComputeProjectedStacks(seed);
            return seed.PeekSecondStatValueAfterImprovement(rarity, projectedStacks);
        }

        public static float ProjectDownsideValue(ValiseData incoming, ValiseImprovementRarity rarity)
        {
            if (incoming == null || !incoming.HasDownside) return 0f;
            ValiseInstance seed = GetSeedInstance(incoming);
            int postLevel = seed.CurrentLevel + 1;
            return incoming.DownsideValuePerLevel * postLevel; // le downside scale au niveau, pas à la rareté (cohérent avec GetTotalDownsideValue)
        }

        /// <summary> Repart de l'état mémorisé si la valise a été sacrifiée, sinon d'une valise neuve. </summary>
        private static ValiseInstance GetSeedInstance(ValiseData incoming)
        {
            if (ValiseManager.Instance != null)
            {
                ValiseInstance memorized = ValiseManager.Instance.GetMemorizedValise(incoming.Id);
                if (memorized != null) return memorized;
            }
            return new ValiseInstance(incoming);
        }

        /// <summary> Stacks projetés selon le driver d'état de la valise (sinon 0). </summary>
        private static int ComputeProjectedStacks(ValiseInstance seed)
        {
            if (seed == null || seed.Data == null || !seed.Data.IsScalingValise) return 0;

            switch (seed.Data.Id)
            {
                case "valise_fortune":
                {
                    int postLevel = seed.CurrentLevel + 1;
                    int threshold = postLevel >= 20 ? 40 : 50;
                    return RunManager.Instance != null ? RunManager.Instance.TalsEarned / threshold : 0;
                }
                case "valise_equilibre":
                    return (ItemManager.Instance != null && ItemManager.Instance.GetActiveSlots() != null)
                        ? ItemManager.Instance.GetActiveSlots().Count : 0;
                default:
                    return 0; // scaling par événements : 0 stack à la prise
            }
        }
    }
}
