using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Données de configuration d'un item roguelike.
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "Chez Arthur/Roguelike/Item Data")]
    public class ItemData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string VALUE_PLACEHOLDER = "{value}";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Identité")]
        [SerializeField] private string id;
        [SerializeField] private string itemName;
        [SerializeField] [TextArea(2, 5)] private string description;
        [SerializeField] private Sprite icon;

        [Header("Catégorie")]
        [SerializeField] private ItemCategory category;

        [Header("Type")]
        [SerializeField] private bool isDownsideItem;

        [Header("Effet principal")]
        [SerializeField] private string mainEffectId;
        [SerializeField] private float mainValue;

        [Header("Downside")]
        [SerializeField] private bool hasDownside;
        [SerializeField] private string downsideEffectId;
        [SerializeField] private float downsideValue;

        [Header("Stats directes")]
        [SerializeField] private ValiseStatType directStatType;
        [SerializeField] private float directStatValue;
        [SerializeField] private bool directStatIsPercentage;
        [SerializeField] private ValiseStatType directDownsideStatType;
        [SerializeField] private float directDownsideStatValue;
        [SerializeField] private bool directDownsideStatIsPercentage;

        [Header("Méta-progression")]
        // Backing field non sérialisé — géré par le SaveSystem
        private bool _isDiscovered;
        [SerializeField] private float hubDisplayWeight = 1f;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string Id => id;
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemCategory Category => category;
        public bool IsDownsideItem => isDownsideItem;
        public string MainEffectId => mainEffectId;
        public float MainValue => mainValue;
        public bool HasDownside => hasDownside;
        public string DownsideEffectId => downsideEffectId;
        public float DownsideValue => downsideValue;
        public ValiseStatType DirectStatType => directStatType;
        public float DirectStatValue => directStatValue;
        public bool DirectStatIsPercentage => directStatIsPercentage;
        public ValiseStatType DirectDownsideStatType => directDownsideStatType;
        public float DirectDownsideStatValue => directDownsideStatValue;
        public bool DirectDownsideStatIsPercentage => directDownsideStatIsPercentage;
        public bool IsDiscovered => _isDiscovered;
        public float HubDisplayWeight => hubDisplayWeight;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Retourne la description avec la valeur principale injectée si le placeholder existe.
        /// </summary>
        public string GetFormattedDescription()
        {
            if (string.IsNullOrEmpty(description) || !description.Contains(VALUE_PLACEHOLDER))
            {
                return description;
            }

            string value = $"{mainValue:0.##}";
            return description.Replace(VALUE_PLACEHOLDER, value);
        }

        /// <summary>
        /// Marque cet item comme découvert. Appelé par le SaveSystem à la première prise.
        /// </summary>
        public void SetDiscovered() => _isDiscovered = true;
    }
}
