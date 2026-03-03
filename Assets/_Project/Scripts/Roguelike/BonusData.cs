using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Données d'un bonus roguelike (stat ou spécial).
    /// </summary>
    [CreateAssetMenu(fileName = "New Bonus", menuName = "Chez Arthur/Roguelike/Bonus Data")]
    public class BonusData : ScriptableObject
    {
        [Header("Identité")]
        [SerializeField] private string bonusName;
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private BonusCategory category;

        [Header("Rareté")]
        [SerializeField] private bool isSpecialBonus;
        [SerializeField] private BonusRarity statRarity;
        [SerializeField] private SpecialBonusRarity specialRarity;

        [Header("Effet principal")]
        [SerializeField] private BonusStatType mainStatType;
        [SerializeField] private float mainValue;
        [SerializeField] private bool isPercentage;

        [Header("Contrepartie (optionnel — pour Rare)")]
        [SerializeField] private bool hasDownside;
        [SerializeField] private BonusStatType downsideStatType;
        [SerializeField] private float downsideValue;
        [SerializeField] private bool downsideIsPercentage;

        [Header("Effet spécial (pour bonus spéciaux)")]
        [SerializeField] private string specialEffectId;
        [SerializeField] private float specialValue;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string BonusName => bonusName;
        public string Description => description;
        public Sprite Icon => icon;
        public BonusCategory Category => category;

        public bool IsSpecialBonus => isSpecialBonus;
        public BonusRarity StatRarity => statRarity;
        public SpecialBonusRarity SpecialRarity => specialRarity;

        public BonusStatType MainStatType => mainStatType;
        public float MainValue => mainValue;
        public bool IsPercentage => isPercentage;

        public bool HasDownside => hasDownside;
        public BonusStatType DownsideStatType => downsideStatType;
        public float DownsideValue => downsideValue;
        public bool DownsideIsPercentage => downsideIsPercentage;

        public string SpecialEffectId => specialEffectId;
        public float SpecialValue => specialValue;

        /// <summary>
        /// Retourne la description formatée avec les valeurs. Placeholders : {value}, {downside}.
        /// downsideValue est stocké en positif ; le signe négatif est géré dans l'UI.
        /// </summary>
        public string GetFormattedDescription()
        {
            string result = description;

            string valueStr = isPercentage ? $"{mainValue * 100:0}%" : $"{mainValue:0}";
            result = result.Replace("{value}", valueStr);

            if (hasDownside)
            {
                string downsideStr = downsideIsPercentage ? $"{downsideValue * 100:0}%" : $"{downsideValue:0}";
                result = result.Replace("{downside}", downsideStr);
            }

            return result;
        }
    }
}
