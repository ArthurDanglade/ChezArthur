using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Données d'un personnage (ScriptableObject) : identité, stats et paramètres physiques.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "Chez Arthur/Character Data", order = 0)]
    public class CharacterData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Identité")]
        [SerializeField] private string characterName;
        [SerializeField] private CharacterRarity rarity;
        [SerializeField] private CharacterRole role;
        [SerializeField] private Sprite icon;

        [Header("Stats de base (niveau 1)")]
        [SerializeField] private int baseHp;
        [SerializeField] private int baseAtk;
        [SerializeField] private int baseDef;
        [SerializeField] private int baseSpeed;

        [Header("Physique")]
        [SerializeField] private float colliderRadius = 0.5f;

        [Header("Description")]
        [TextArea]
        [SerializeField] private string backstory;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string CharacterName => characterName;
        public CharacterRarity Rarity => rarity;
        public CharacterRole Role => role;
        public Sprite Icon => icon;

        public int BaseHp => baseHp;
        public int BaseAtk => baseAtk;
        public int BaseDef => baseDef;
        public int BaseSpeed => baseSpeed;

        public float ColliderRadius => colliderRadius;
        public string Backstory => backstory;
    }
}

