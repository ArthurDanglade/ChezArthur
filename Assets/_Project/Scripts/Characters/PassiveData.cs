using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Données d'un passif (ScriptableObject).
    /// </summary>
    [CreateAssetMenu(fileName = "NewPassive", menuName = "Chez Arthur/Passive Data", order = 1)]
    public class PassiveData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Identité")]
        [SerializeField] private string id;
        [SerializeField] private string passiveName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        [Header("Mécanique")]
        [SerializeField] private PassiveType triggerType;
        [SerializeField] private float[] values; // Valeurs configurables (%, flat, etc.)

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string Id => id;
        public string PassiveName => passiveName;
        public string Description => description;
        public Sprite Icon => icon;
        public PassiveType TriggerType => triggerType;
        public float[] Values => values;

        /// <summary>
        /// Retourne la description formatée avec les valeurs.
        /// Ex: "ATK +{0}%" avec values[0] = 20 → "ATK +20%"
        /// </summary>
        public string GetFormattedDescription()
        {
            if (values == null || values.Length == 0)
                return description;

            object[] args = new object[values.Length];
            for (int i = 0; i < values.Length; i++)
                args[i] = values[i];

            return string.Format(description, args);
        }
    }
}
