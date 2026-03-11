using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Données d'un personnage (ScriptableObject) : identité, visuels, et spécialisations (profil de base + alternatives).
    /// Les stats et passifs sont portés par SpecializationData.
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
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite portrait;
        [SerializeField] private float colliderRadius = 0.5f;
        [TextArea]
        [SerializeField] private string backstory;

        [Header("Spécialisations")]
        [SerializeField] private SpecializationData baseSpecialization;
        [SerializeField] private List<AlternativeSpecialization> alternativeSpecializations = new List<AlternativeSpecialization>();

        [HideInInspector]
        [SerializeField] private CharacterRole legacy_role;
        [HideInInspector]
        [SerializeField] private int legacy_baseHp;
        [HideInInspector]
        [SerializeField] private int legacy_baseAtk;
        [HideInInspector]
        [SerializeField] private int legacy_baseDef;
        [HideInInspector]
        [SerializeField] private int legacy_baseSpeed;
        [HideInInspector]
        [SerializeField] private int legacy_hpPerLevel;
        [HideInInspector]
        [SerializeField] private int legacy_atkPerLevel;
        [HideInInspector]
        [SerializeField] private int legacy_defPerLevel;
        [HideInInspector]
        [SerializeField] private int legacy_speedPerLevel;
        [HideInInspector]
        [SerializeField] private CharacterPassiveSet legacy_passives;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string Id => id;
        public string CharacterName => characterName;
        public CharacterRarity Rarity => rarity;
        public Sprite Icon => icon;
        public Sprite Portrait => portrait;
        public float ColliderRadius => colliderRadius;
        public string Backstory => backstory;

        public CharacterRole Role => baseSpecialization != null ? baseSpecialization.Role : default;
        public int BaseHp => baseSpecialization != null ? baseSpecialization.BaseHp : 0;
        public int BaseAtk => baseSpecialization != null ? baseSpecialization.BaseAtk : 0;
        public int BaseDef => baseSpecialization != null ? baseSpecialization.BaseDef : 0;
        public int BaseSpeed => baseSpecialization != null ? baseSpecialization.BaseSpeed : 0;
        public int HpPerLevel => baseSpecialization != null ? baseSpecialization.HpPerLevel : 0;
        public int AtkPerLevel => baseSpecialization != null ? baseSpecialization.AtkPerLevel : 0;
        public int DefPerLevel => baseSpecialization != null ? baseSpecialization.DefPerLevel : 0;
        public int SpeedPerLevel => baseSpecialization != null ? baseSpecialization.SpeedPerLevel : 0;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Retourne la spécialisation à l'index donné. -1 = base, 0+ = alternative. Invalide → base.
        /// </summary>
        public SpecializationData GetSpecialization(int specIndex)
        {
            if (specIndex < 0) return baseSpecialization;
            if (alternativeSpecializations == null || specIndex >= alternativeSpecializations.Count) return baseSpecialization;
            AlternativeSpecialization alt = alternativeSpecializations[specIndex];
            return alt != null ? alt.Specialization : baseSpecialization;
        }

        /// <summary>
        /// Retourne les spécialisations débloquées au niveau donné (base + alternatives dont unlockLevel &lt;= level).
        /// </summary>
        public List<(SpecializationData spec, int unlockLevel)> GetAvailableSpecializations(int level)
        {
            var list = new List<(SpecializationData spec, int unlockLevel)>();
            if (baseSpecialization != null)
                list.Add((baseSpecialization, 1));

            if (alternativeSpecializations == null) return list;

            for (int i = 0; i < alternativeSpecializations.Count; i++)
            {
                AlternativeSpecialization alt = alternativeSpecializations[i];
                if (alt != null && alt.Specialization != null && alt.UnlockLevel <= level)
                    list.Add((alt.Specialization, alt.UnlockLevel));
            }
            return list;
        }

        /// <summary>
        /// Nombre de spécialisations alternatives.
        /// </summary>
        public int GetSpecializationCount() => alternativeSpecializations != null ? alternativeSpecializations.Count : 0;

        /// <summary>PV au niveau donné (profil de base).</summary>
        public int GetHpAtLevel(int level) => baseSpecialization != null ? baseSpecialization.GetHpAtLevel(level) : 0;

        /// <summary>ATK au niveau donné.</summary>
        public int GetAtkAtLevel(int level) => baseSpecialization != null ? baseSpecialization.GetAtkAtLevel(level) : 0;

        /// <summary>DEF au niveau donné.</summary>
        public int GetDefAtLevel(int level) => baseSpecialization != null ? baseSpecialization.GetDefAtLevel(level) : 0;

        /// <summary>Speed au niveau donné.</summary>
        public int GetSpeedAtLevel(int level) => baseSpecialization != null ? baseSpecialization.GetSpeedAtLevel(level) : 0;

        /// <summary>
        /// Niveau auquel la première spécialisation alternative est débloquée. SR : -1, SSR/LR : unlockLevel de la première alternative.
        /// </summary>
        public int GetSpecializationLevel()
        {
            if (rarity == CharacterRarity.SR) return -1;
            if (alternativeSpecializations == null || alternativeSpecializations.Count == 0) return -1;
            AlternativeSpecialization first = alternativeSpecializations[0];
            return first != null ? first.UnlockLevel : -1;
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
