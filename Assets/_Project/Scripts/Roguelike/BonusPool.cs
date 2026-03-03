using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Pool de tous les bonus disponibles. Gère le tirage aléatoire selon les raretés et le pity system.
    /// </summary>
    public class BonusPool : MonoBehaviour
    {
        [Header("Bonus de stat")]
        [SerializeField] private List<BonusData> commonBonuses = new List<BonusData>();
        [SerializeField] private List<BonusData> uncommonBonuses = new List<BonusData>();
        [SerializeField] private List<BonusData> rareBonuses = new List<BonusData>();
        [SerializeField] private List<BonusData> epicBonuses = new List<BonusData>();

        [Header("Bonus spéciaux")]
        [SerializeField] private List<BonusData> specialBonuses = new List<BonusData>();

        [Header("Probabilités")]
        [SerializeField] [Range(0f, 1f)] private float commonChance = 0.50f;
        [SerializeField] [Range(0f, 1f)] private float uncommonChance = 0.30f;
        [SerializeField] [Range(0f, 1f)] private float rareChance = 0.15f;
        [SerializeField] [Range(0f, 1f)] private float epicChance = 0.05f;
        [SerializeField] [Range(0f, 1f)] private float specialBonusChance = 0.10f;

        [Header("Pity System")]
        [SerializeField] private int pityThreshold = 4;

        private int _paliersSinceSpecial;

        /// <summary>
        /// Tire plusieurs bonus aléatoires pour le choix du joueur (sans doublon dans le tirage).
        /// </summary>
        public List<BonusData> GetRandomBonuses(int count = 3)
        {
            var result = new List<BonusData>(count);
            bool forceSpecial = _paliersSinceSpecial >= pityThreshold && specialBonuses != null && specialBonuses.Count > 0;
            int forcedSpecialSlot = forceSpecial ? Random.Range(0, count) : -1;

            for (int slot = 0; slot < count; slot++)
            {
                BonusData picked = null;
                bool wantSpecial = (slot == forcedSpecialSlot) || (Random.value < specialBonusChance);

                if (wantSpecial && specialBonuses != null && specialBonuses.Count > 0)
                    picked = PickOneExcluding(specialBonuses, result);

                if (picked == null)
                    picked = PickOneStatBonus(result);

                if (picked != null)
                    result.Add(picked);
            }

            return result;
        }

        /// <summary>
        /// Appelé quand un bonus spécial est obtenu (reset pity).
        /// </summary>
        public void ResetPity()
        {
            _paliersSinceSpecial = 0;
        }

        /// <summary>
        /// Appelé à chaque palier sans bonus spécial choisi.
        /// </summary>
        public void IncrementPity()
        {
            _paliersSinceSpecial++;
        }

        private BonusData PickOneExcluding(List<BonusData> source, List<BonusData> exclude)
        {
            var available = new List<BonusData>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null && !exclude.Contains(source[i]))
                    available.Add(source[i]);
            }
            if (available.Count == 0) return null;
            return available[Random.Range(0, available.Count)];
        }

        private BonusData PickOneStatBonus(List<BonusData> exclude)
        {
            float total = commonChance + uncommonChance + rareChance + epicChance;
            if (total <= 0f) total = 1f;
            float r = Random.value * total;

            List<BonusData> list = null;
            if (r < commonChance)
                list = commonBonuses;
            else if (r < commonChance + uncommonChance)
                list = uncommonBonuses;
            else if (r < commonChance + uncommonChance + rareChance)
                list = rareBonuses;
            else
                list = epicBonuses;

            if (list == null || list.Count == 0)
                return PickFirstAvailableStatBonus(exclude);

            BonusData picked = PickOneExcluding(list, exclude);
            if (picked != null) return picked;
            return PickFirstAvailableStatBonus(exclude);
        }

        private BonusData PickFirstAvailableStatBonus(List<BonusData> exclude)
        {
            if (commonBonuses != null) { var p = PickOneExcluding(commonBonuses, exclude); if (p != null) return p; }
            if (uncommonBonuses != null) { var p = PickOneExcluding(uncommonBonuses, exclude); if (p != null) return p; }
            if (rareBonuses != null) { var p = PickOneExcluding(rareBonuses, exclude); if (p != null) return p; }
            if (epicBonuses != null) { var p = PickOneExcluding(epicBonuses, exclude); if (p != null) return p; }
            return null;
        }
    }
}
