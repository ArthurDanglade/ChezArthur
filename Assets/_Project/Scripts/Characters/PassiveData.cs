using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Données d'un passif exécutable en combat (ScriptableObject).
    /// Définit le trigger, l'effet, les valeurs par stack et la règle de reset.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPassive", menuName = "Chez Arthur/Passive Data", order = 1)]
    public class PassiveData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Identité")]
        [SerializeField] private string id;
        [SerializeField] private string passiveName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        [Header("Déclenchement")]
        [SerializeField] private PassiveTrigger trigger;
        [SerializeField] private PassiveEffect effect;

        [Header("Valeurs")]
        [SerializeField] private float value;
        [SerializeField] private int maxStacks = 1;
        [SerializeField] private PassiveResetRule resetRule;

        [HideInInspector]
        [SerializeField] private float[] legacyValues;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string Id => id;
        public string PassiveName => passiveName;
        public string Description => description;
        public Sprite Icon => icon;
        public PassiveTrigger Trigger => trigger;
        public PassiveEffect Effect => effect;
        public float Value => value;
        public int MaxStacks => maxStacks;
        public PassiveResetRule ResetRule => resetRule;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Retourne la description avec les placeholders remplacés :
        /// {value} → valeur en % (value * 100), {maxStacks} → maxStacks, {totalValue} → (value * maxStacks * 100)%.
        /// </summary>
        public string GetFormattedDescription()
        {
            if (string.IsNullOrEmpty(description)) return string.Empty;

            int valuePercent = Mathf.RoundToInt(value * 100f);
            int totalValuePercent = Mathf.RoundToInt(value * maxStacks * 100f);

            return description
                .Replace("{value}", valuePercent.ToString())
                .Replace("{maxStacks}", maxStacks.ToString())
                .Replace("{totalValue}", totalValuePercent.ToString());
        }
    }
}
