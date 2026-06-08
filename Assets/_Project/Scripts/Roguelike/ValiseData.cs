using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Données de configuration d'une valise roguelike.
    /// </summary>
    [CreateAssetMenu(fileName = "New Valise", menuName = "Chez Arthur/Roguelike/Valise Data")]
    public class ValiseData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string VALUE_PER_LEVEL_PLACEHOLDER = "{valuePerLevel}";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Identité")]
        [SerializeField] private string id;
        [SerializeField] private string valiseName;
        [SerializeField] [TextArea(2, 5)] private string description;
        [SerializeField] private Sprite icon;

        [Header("Catégorie")]
        [SerializeField] private ValiseCategory category;

        [Header("Effet de base")]
        [SerializeField] private ValiseStatType baseStatType;
        [SerializeField] private float baseValuePerLevel;
        [SerializeField] private bool baseIsPercentage;

        [Header("Effet de base — stat secondaire (optionnel)")]
        [SerializeField] private bool hasSecondStat;
        [SerializeField] private ValiseStatType secondStatType;
        [SerializeField] private float secondValuePerLevel;
        [SerializeField] private bool secondIsPercentage;

        [Header("Downside (optionnel)")]
        [SerializeField] private bool hasDownside;
        [SerializeField] private ValiseStatType downsideStatType;
        [SerializeField] private float downsideValuePerLevel;
        [SerializeField] private bool downsideIsPercentage;

        [Header("Effet niveau 20")]
        [SerializeField] private string level20EffectId;

        [Header("Scaling interne")]
        [SerializeField] private bool isScalingValise;

        [Header("Synergies")]
        [SerializeField] private List<string> synergyValiseIds = new List<string>();

        [Header("Gare")]
        [SerializeField] private bool canAppearInGare;
        [SerializeField] private int gareCostBase;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string Id => id;
        public string ValiseName => valiseName;
        public string Description => description;
        public Sprite Icon => icon;
        public ValiseCategory Category => category;
        public ValiseStatType BaseStatType => baseStatType;
        public float BaseValuePerLevel => baseValuePerLevel;
        public bool BaseIsPercentage => baseIsPercentage;
        public bool HasSecondStat => hasSecondStat;
        public ValiseStatType SecondStatType => secondStatType;
        public float SecondValuePerLevel => secondValuePerLevel;
        public bool SecondIsPercentage => secondIsPercentage;
        public bool HasDownside => hasDownside;
        public ValiseStatType DownsideStatType => downsideStatType;
        public float DownsideValuePerLevel => downsideValuePerLevel;
        public bool DownsideIsPercentage => downsideIsPercentage;
        public string Level20EffectId => level20EffectId;
        public bool IsScalingValise => isScalingValise;
        public IReadOnlyList<string> SynergyValiseIds => synergyValiseIds;
        public bool CanAppearInGare => canAppearInGare;
        public int GareCostBase => gareCostBase;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Retourne la description avec la valeur par niveau injectée si le placeholder existe.
        /// </summary>
        public string GetFormattedDescription()
        {
            if (string.IsNullOrEmpty(description) || !description.Contains(VALUE_PER_LEVEL_PLACEHOLDER))
            {
                return description;
            }

            string valuePerLevel = baseIsPercentage
                ? $"{baseValuePerLevel * 100:0.##}%"
                : $"{baseValuePerLevel:0.##}";

            return description.Replace(VALUE_PER_LEVEL_PLACEHOLDER, valuePerLevel);
        }
    }
}
