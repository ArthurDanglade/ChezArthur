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
        // CONSTANTES
        // ═══════════════════════════════════════════
        public const int MAX_LEVEL = 99;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Identité")]
        [SerializeField] private string id;
        [SerializeField] private string characterName;
        [SerializeField] private CharacterRarity rarity;
        [SerializeField] private CharacterRole role;

        [Header("Visuels")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite portrait;

        [Header("Stats de base (niveau 1)")]
        [SerializeField] private int baseHp;
        [SerializeField] private int baseAtk;
        [SerializeField] private int baseDef;
        [SerializeField] private int baseSpeed;

        [Header("Stats de croissance (par niveau)")]
        [SerializeField] private int hpPerLevel;
        [SerializeField] private int atkPerLevel;
        [SerializeField] private int defPerLevel;
        [SerializeField] private int speedPerLevel;

        [Header("Passifs")]
        [SerializeField] private CharacterPassiveSet passives;

        [Header("Physique")]
        [SerializeField] private float colliderRadius = 0.5f;

        [Header("Description")]
        [TextArea]
        [SerializeField] private string backstory;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string Id => id;
        public string CharacterName => characterName;
        public CharacterRarity Rarity => rarity;
        public CharacterRole Role => role;
        public Sprite Icon => icon;
        public Sprite Portrait => portrait;

        public int BaseHp => baseHp;
        public int BaseAtk => baseAtk;
        public int BaseDef => baseDef;
        public int BaseSpeed => baseSpeed;

        public int HpPerLevel => hpPerLevel;
        public int AtkPerLevel => atkPerLevel;
        public int DefPerLevel => defPerLevel;
        public int SpeedPerLevel => speedPerLevel;

        public CharacterPassiveSet Passives => passives;
        public float ColliderRadius => colliderRadius;
        public string Backstory => backstory;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Calcule les stats à un niveau donné.
        /// </summary>
        public int GetHpAtLevel(int level) => baseHp + (hpPerLevel * (level - 1));
        public int GetAtkAtLevel(int level) => baseAtk + (atkPerLevel * (level - 1));
        public int GetDefAtLevel(int level) => baseDef + (defPerLevel * (level - 1));
        public int GetSpeedAtLevel(int level) => baseSpeed + (speedPerLevel * (level - 1));

        /// <summary>
        /// Niveau auquel la spécialisation est débloquée. SR : -1, SSR : 15, LR : 20.
        /// </summary>
        public int GetSpecializationLevel()
        {
            return rarity switch
            {
                CharacterRarity.SR => -1,   // Pas de spé
                CharacterRarity.SSR => 15,  // Spé au lvl 15
                CharacterRarity.LR => 20,    // Spé au lvl 20
                _ => -1
            };
        }

        /// <summary>
        /// Niveau du passif lvl 15. Seuls les LR en ont un ; autres raretés retournent -1.
        /// </summary>
        public int GetLevel15PassiveLevel()
        {
            return rarity == CharacterRarity.LR ? 15 : -1;
        }
    }
}

